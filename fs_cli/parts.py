"""Part sets (`[tool.fs.parts]`): Part Studios built from a definition's configured studios, like FRCDesign's fasteners,
whose FeatureScript only they use. Their FeatureScript is in the backend document like any other, and its imports work
the same, but it's built and released with its Part Studios:

- `fs build SET` builds each Part Studio in the backend workspace (or with `--to`, another backend), named
  `<tab> (TEST)`, from the FeatureScript as pushed, to try a change. Nothing else is written.
- `fs release SET` versions the backend document, points the set's frontend document (one of FRCDesignLib's or
  ConfigLib's) at that version, builds each Part Studio there and writes the set's manifest beside them
  (`<set>.parts.json`), records where they are beside their definition (`<definition>.studios.json`), and writes the
  definition's exports again. That's a soft release; a hard one (`--hard`) also versions the frontend and tags the
  release in git, as `fs publish` does for a soft one later. With `--to`, it does the same to a library document, a
  test release, and versions it, but numbers, records, and tags nothing.

See docs/parts.md.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import runpy
from collections.abc import Callable

from fs_cli.config import Document, PartSet, Role
from fs_cli.configured_studios import (
    Built,
    ConfiguredStudio,
    ConfigureError,
    Diagnosis,
    configured_studios,
    write_locations,
)
from fs_cli.release import check_release_document, find_frontend_studio, test_version_name, version_backend
from fs_cli.remote import Remote, RemoteStudio
from fs_cli.versions import (
    ReleasedVersion,
    VersionType,
    latest_release,
    next_version,
)
from fs_cli.workspace import Status, Studio, UsageError, Workspace
from onshape_api.paths.paths import InstancePath

# What a test Part Studio's tab is called after its studio's, e.g. `Screw (TEST)`
TEST_SUFFIX = " (TEST)"


def defines(code: str, feature: str) -> bool:
    """Whether a Feature Studio's code defines a feature, by the constant it's exported as."""
    return re.search(rf"\bexport\s+const\s+{re.escape(feature)}\s*=\s*defineFeature\b", code) is not None


def part_set_definitions(workspace: Workspace, part_set: PartSet) -> list[tuple[pathlib.Path, list[ConfiguredStudio]]]:
    """The definitions in a part set's folder which configure Part Studios, each with its studios, checked offline
    against the configuration rules before anything is sent to Onshape."""
    folder = workspace.config.code_dir / part_set.folder
    paths = [folder / part_set.definition] if part_set.definition else sorted(folder.rglob("*.py"))
    found = []
    for path in paths:
        studios = configured_studios(runpy.run_path(str(path)))
        if studios:
            found.append((path, studios))
    if not found:
        raise UsageError(f"Nothing in {part_set.folder}/ configures a Part Studio (a definition's STUDIOS).")
    problems = [problem for _, studios in found for studio in studios for problem in studio.problems()]
    if problems:
        raise UsageError("The configurations break their rules (see docs/fs-cli.md's \"Configured Part Studios\"):\n  " + "\n  ".join(problems))
    return found


def part_set_script(workspace: Workspace, part_set: PartSet) -> str:
    """The FeatureScript in a part set's folder defining every feature its studios use, read from the repo."""
    features = sorted({studio.feature for _, studios in part_set_definitions(workspace, part_set) for studio in studios})
    folder = workspace.config.code_dir / part_set.folder
    found = [
        path for path in sorted(folder.rglob("*.fs")) if all(defines(path.read_text(), feature) for feature in features)
    ]
    if len(found) != 1:
        raise UsageError(f"Expected one FeatureScript in {part_set.folder}/ defining {', '.join(features)}; found {len(found)}.")
    return str(found[0])


def feature_studio(workspace: Workspace, part_set: PartSet, studios: list[ConfiguredStudio]) -> Studio:
    """The part set's Feature Studio defining every studio's feature, which they're built from. Every studio in the
    set's folder must be pushed and in sync, its tables included."""
    scanned = workspace.scan(workspace.resolve_targets([str(workspace.config.code_dir / part_set.folder)]))
    unsynced = [studio.path for studio in scanned if studio.remote is None or studio.status != Status.IN_SYNC]
    if unsynced:
        raise UsageError(
            f"{', '.join(unsynced)} isn't in sync with the backend document; run `fs push` (or `fs status`) first."
        )
    # Onshape has refused to configure a feature from a Feature Studio whose import versions it changed since the last
    # sync ("Feature not found", observed), until they were written locally and the studio recorded again
    for updated in workspace.apply_import_updates(scanned):
        print(f"Updated import versions in {updated.path} to match Onshape")
    candidates = [
        studio
        for studio in scanned
        if studio.local_code is not None and all(defines(studio.local_code, each.feature) for each in studios)
    ]
    features = ", ".join(sorted({each.feature for each in studios}))
    if len(candidates) != 1:
        raise UsageError(f"Expected one FeatureScript in {part_set.folder}/ defining {features}; found {len(candidates)}.")
    return candidates[0]


def build_studios(
    remote: Remote,
    instance: InstancePath,
    studios: list[ConfiguredStudio],
    feature: RemoteStudio,
    log: Callable[[str], None] = print,
) -> dict[str, Built]:
    """Builds each studio in a workspace from a Feature Studio there. A feature that fails to regenerate in its default
    configuration stops the build."""
    built: dict[str, Built] = {}
    for studio in studios:
        log(f"Building {studio.tab_name}...")
        try:
            result = remote.build_studio(instance, studio, feature.element_id, feature.microversion_id)
        except ConfigureError as error:
            raise UsageError(str(error)) from error
        if result.status == "ERROR":
            raise UsageError(
                f"{studio.tab_name}'s {studio.feature} fails to regenerate in its default configuration; "
                "its Part Studio says why."
            )
        built[studio.name] = result
    return built


def write_exports(path: pathlib.Path) -> list[pathlib.Path]:
    """Writes a definition's EXPORTS again, now that its studios' locations changed; answers those that changed."""
    definition = runpy.run_path(str(path))
    changed = []
    for name, text in definition.get("EXPORTS", {}).items():
        exported = path.with_name(name)
        if not exported.is_file() or exported.read_text() != text:
            exported.write_text(text, newline="")
            changed.append(exported)
    return changed


# Trying a change


def test_studios(studios: list[ConfiguredStudio]) -> list[ConfiguredStudio]:
    """The studios as their test Part Studios, named `<tab> (TEST)`."""
    return [dataclasses.replace(studio, tab=studio.tab_name + TEST_SUFFIX) for studio in studios]


def run_build(
    workspace: Workspace,
    part_set: PartSet,
    remote: Remote | None = None,
    instance: InstancePath | None = None,
    feature: RemoteStudio | None = None,
    log: Callable[[str], None] = print,
) -> list[tuple[ConfiguredStudio, Built]]:
    """Builds a part set's test Part Studios from its FeatureScript as pushed: in the backend document, or with `remote`,
    `instance`, and `feature`, in another backend, from its copy of the FeatureScript there."""
    definitions = part_set_definitions(workspace, part_set)
    every = [studio for _, studios in definitions for studio in test_studios(studios)]
    if feature is None:
        found = feature_studio(workspace, part_set, every)
        assert found.remote
        feature = found.remote
    built = build_studios(remote or workspace.remote, instance or workspace.instance, every, feature, log)
    return [(studio, built[studio.name]) for studio in every]


def run_diagnose(
    workspace: Workspace,
    part_set: PartSet,
    remote: Remote | None = None,
    instance: InstancePath | None = None,
    log: Callable[[str], None] = print,
) -> list[tuple[ConfiguredStudio, Diagnosis]]:
    """Diagnoses each of a part set's configurations in the backend document (or another backend), stopping at the
    first Onshape refuses."""
    remote, instance = remote or workspace.remote, instance or workspace.instance
    diagnosed = []
    for _, studios in part_set_definitions(workspace, part_set):
        for studio in studios:
            log(f"Diagnosing {studio.tab_name}...")
            diagnosis = remote.diagnose_studio(instance, studio, studio.tab_name + TEST_SUFFIX)
            diagnosed.append((studio, diagnosis))
            if diagnosis.refused is not None:
                return diagnosed
    return diagnosed


# Releasing

# What a part set's manifest is called in a document it's released to, beside its Part Studios: its definition's
# manifest export (`fasteners.gen.json`) as `fasteners.parts.json`
MANIFEST_SUFFIX = ".gen.json"
RELEASED_MANIFEST_SUFFIX = ".parts.json"


@dataclasses.dataclass
class PartReleasePlan:
    """A release of a part set: to its frontend document, numbered (`released`), or to a library, to try it out.

    Attributes:
        document: Where it's released to.
        target: That document's workspace.
        released: Its version number; None for a library's, which isn't numbered, recorded, or tagged.
    """

    part_set: PartSet
    studio: Studio
    definitions: list[tuple[pathlib.Path, list[ConfiguredStudio]]]
    previous: ReleasedVersion | None
    released: ReleasedVersion | None
    document: Document
    target: InstancePath
    version_name: str

    @property
    def label(self) -> str:
        return f"{self.document.role} document ({self.document.name})"


def frontend_of(part_set: PartSet) -> Document:
    """The frontend document a part set is released to."""
    if part_set.frontend is None:
        raise UsageError(
            f"The {part_set.name} part set has no frontend document yet: name the one it's released to in "
            f"[tool.fs.parts.{part_set.name}] frontend (`--to DOC` releases it to a library meanwhile)."
        )
    return part_set.frontend


def plan_part_release(
    workspace: Workspace, part_set: PartSet, bump: VersionType | None, document: Document
) -> PartReleasePlan:
    check_release_document(document, bump)
    definitions = part_set_definitions(workspace, part_set)
    studio = feature_studio(workspace, part_set, [each for _, studios in definitions for each in studios])
    target = workspace.instance_of(document)
    if document.role is Role.LIBRARY:
        name = test_version_name(workspace, part_set.title)
        return PartReleasePlan(part_set, studio, definitions, None, None, document, target, name)
    # Numbered by the backend's versions, as the frontend has only its hard releases'
    previous = latest_release(part_set.title, workspace.remote.versions(workspace.instance))
    try:
        version = next_version(previous, bump)
    except ValueError as error:
        raise UsageError(str(error)) from error
    released = ReleasedVersion(part_set.title, version)
    return PartReleasePlan(part_set, studio, definitions, previous, released, document, target, released.name)


def released_manifests(definition: pathlib.Path, built: dict[str, Built]) -> dict[str, bytes]:
    """A definition's manifests as released beside its Part Studios: each studio with its tab in that document
    (`elementId`), in place of where it was last released (`location`). By the name of the tab each is uploaded to."""
    exports = runpy.run_path(str(definition)).get("EXPORTS", {})
    manifests = {}
    for name, text in exports.items():
        if not name.endswith(MANIFEST_SUFFIX):
            continue
        manifest = json.loads(text)
        for key, studio in manifest.get("studios", {}).items():
            studio.pop("location", None)
            studio["elementId"] = built[key].element_id
        # Indented, for people reading it in Onshape
        manifests[name.removesuffix(MANIFEST_SUFFIX) + RELEASED_MANIFEST_SUFFIX] = (
            json.dumps(manifest, indent=4) + "\n"
        ).encode()
    return manifests


def run_part_release(
    workspace: Workspace, plan: PartReleasePlan, description: str, hard: bool, log: Callable[[str], None] = print
) -> None:
    remote = workspace.remote_for(plan.document)
    assert plan.studio.remote
    name = plan.studio.remote.name
    code = version_backend(workspace, plan.studio, plan.version_name, description, log)
    target_studio = find_frontend_studio(remote, plan.target, name)
    if target_studio is None:
        log(f"Creating {name} in the {plan.label}...")
        target_studio = remote.create(plan.target, name)
    log(f"Updating {name} in the {plan.label}...")
    remote.push(plan.target, target_studio.element_id, code)
    # Its microversion as pushed, which the Part Studios insert its features at
    pushed = next(each for each in remote.list_studios(plan.target) if each.element_id == target_studio.element_id)
    built = {
        definition: build_studios(remote, plan.target, studios, pushed, log) for definition, studios in plan.definitions
    }
    for definition, studios in built.items():
        for tab, manifest in released_manifests(definition, studios).items():
            log(f"Writing {tab} in the {plan.label}...")
            remote.put_file(plan.target, tab, manifest, "application/json")

    # A library is always versioned, for a test FRCDesignApp to load
    if hard or plan.released is None:
        log(f"Creating version {plan.version_name} in the {plan.label}...")
        remote.create_version(plan.target, plan.version_name, description)
    if plan.released is None:
        return
    for definition, studios in built.items():
        write_locations(definition, plan.target.document_id, {name: each.element_id for name, each in studios.items()})
        for exported in write_exports(definition):
            log(f"Updated {exported.name}")
    # Its tab has to stay put from now on (see `load_released`)
    workspace.state.released.add(plan.studio.remote.element_id)
