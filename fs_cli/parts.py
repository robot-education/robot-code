"""Part sets (`[tool.fs.parts]`): Part Studios built from a definition's configured studios, like FRCDesign's fasteners,
whose FeatureScript only they use. Their FeatureScript is in the backend document like any other, and its imports work
the same, but it's tried and released here rather than by `fs release`:

- `fs parts <set> --test` builds each Part Studio in the branch's backend workspace, named `<tab> (TEST)`, from the
  FeatureScript as pushed, to try a change. Nothing else is written.
- `fs parts <set> --release` versions the backend document, points the set's main document (an FRCDesignLib document)
  at that version, builds each Part Studio there and writes the set's manifest beside them (`<set>.parts.json`),
  versions the main document, records where they are beside their definition (`<definition>.studios.json`), writes the
  definition's exports again, and tags the release in git. With `--target`, it does the same to another document, to
  try it out, but numbers, records, and tags nothing.

See docs/parts.md.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import runpy
from collections.abc import Callable

from fs_cli.branches import branch_name
from fs_cli.config import ConfigError, PartSet
from fs_cli.configured_studios import (
    Built,
    ConfiguredStudio,
    ConfigureError,
    Diagnosis,
    configured_studios,
    write_locations,
)
from fs_cli.git import head_commit
from fs_cli.release import find_frontend_studio, version_backend
from fs_cli.remote import Remote, RemoteStudio, Version
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
    found = []
    for path in sorted(folder.rglob("*.py")):
        studios = configured_studios(runpy.run_path(str(path)))
        if studios:
            found.append((path, studios))
    if not found:
        raise UsageError(f"Nothing in {part_set.folder}/ configures a Part Studio (a definition's STUDIOS).")
    problems = [problem for _, studios in found for studio in studios for problem in studio.problems()]
    if problems:
        raise UsageError("The configurations break their rules (see docs/fs-cli.md's \"Configured Part Studios\"):\n  " + "\n  ".join(problems))
    return found


def feature_studio(workspace: Workspace, part_set: PartSet, studios: list[ConfiguredStudio]) -> Studio:
    """The part set's Feature Studio defining every studio's feature, which they're built from. Every studio in the
    set's folder must be pushed and in sync, its tables included."""
    scanned = workspace.scan(workspace.resolve_targets([str(workspace.config.code_dir / part_set.folder)]))
    unsynced = [studio.path for studio in scanned if studio.remote is None or studio.status != Status.IN_SYNC]
    if unsynced:
        raise UsageError(
            f"{', '.join(unsynced)} isn't in sync with the backend document; run `fs push` (or `fs status`) first."
        )
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


def run_test(
    workspace: Workspace, part_set: PartSet, log: Callable[[str], None] = print
) -> list[tuple[ConfiguredStudio, Built]]:
    """Builds a part set's test Part Studios in the backend document, from its FeatureScript as pushed."""
    definitions = part_set_definitions(workspace, part_set)
    every = [studio for _, studios in definitions for studio in test_studios(studios)]
    feature = feature_studio(workspace, part_set, every)
    assert feature.remote
    built = build_studios(workspace.remote, workspace.instance, every, feature.remote, log)
    return [(studio, built[studio.name]) for studio in every]


def run_diagnose(
    workspace: Workspace, part_set: PartSet, log: Callable[[str], None] = print
) -> list[tuple[ConfiguredStudio, Diagnosis]]:
    """Diagnoses each of a part set's configurations in the backend document, stopping at the first Onshape refuses."""
    diagnosed = []
    for _, studios in part_set_definitions(workspace, part_set):
        for studio in studios:
            log(f"Diagnosing {studio.tab_name}...")
            diagnosis = workspace.remote.diagnose_studio(workspace.instance, studio, studio.tab_name + TEST_SUFFIX)
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
    """A release of a part set: to its main document, numbered (`released`), or to a target, to try it out.

    Attributes:
        main: The workspace of the document it's released to.
        released: Its version number; None for a target's, which isn't numbered, recorded, or tagged.
    """

    part_set: PartSet
    studio: Studio
    definitions: list[tuple[pathlib.Path, list[ConfiguredStudio]]]
    previous: ReleasedVersion | None
    released: ReleasedVersion | None
    main: InstancePath
    version_name: str

    @property
    def label(self) -> str:
        return "main document" if self.released else "target document"


def plan_part_release(
    workspace: Workspace, part_set: PartSet, bump: VersionType | None, target: str | None = None
) -> PartReleasePlan:
    definitions = part_set_definitions(workspace, part_set)
    studio = feature_studio(workspace, part_set, [each for _, studios in definitions for each in studios])
    if target is not None:
        if bump is not None:
            raise UsageError("A release to a target isn't numbered, so it takes no --major, --minor, or --patch.")
        try:
            document_id = part_set.target_document_id(target)
        except ConfigError as error:
            raise UsageError(str(error)) from error
        main = workspace.remote.default_workspace(document_id)
        commit = (head_commit(workspace.config.root) or "")[:7]
        name = f"{part_set.title} test - {branch_name(workspace.config) or 'no branch'} {commit}".rstrip()
        return PartReleasePlan(part_set, studio, definitions, None, None, main, name)
    main = workspace.remote.default_workspace(part_set.main_document_id)
    previous = latest_release(part_set.title, workspace.remote.versions(main))
    try:
        version = next_version(previous, bump, beta=False)
    except ValueError as error:
        raise UsageError(str(error)) from error
    released = ReleasedVersion(part_set.title, version)
    return PartReleasePlan(part_set, studio, definitions, previous, released, main, released.name)


def released_manifests(definition: pathlib.Path, built: dict[str, Built]) -> dict[str, bytes]:
    """A definition's manifests as released beside its Part Studios: each studio with its tab in that document
    (`elementId`), in place of where it was last released (`location`), and its feature's mate connector as a fasten
    mate names it (`mateConnectorId`), in place of the id under the feature's (`mateConnector`). By the name of the
    tab each is uploaded to."""
    exports = runpy.run_path(str(definition)).get("EXPORTS", {})
    manifests = {}
    for name, text in exports.items():
        if not name.endswith(MANIFEST_SUFFIX):
            continue
        manifest = json.loads(text)
        for key, studio in manifest.get("studios", {}).items():
            studio.pop("location", None)
            studio["elementId"] = built[key].element_id
            mate_connector = studio.pop("mateConnector", None)
            if mate_connector:
                studio["mateConnectorId"] = f"{built[key].feature_id}.{mate_connector}"
        manifests[name.removesuffix(MANIFEST_SUFFIX) + RELEASED_MANIFEST_SUFFIX] = json.dumps(manifest).encode()
    return manifests


def run_part_release(
    workspace: Workspace, plan: PartReleasePlan, description: str, log: Callable[[str], None] = print
) -> Version:
    remote = workspace.remote
    assert plan.studio.remote
    name = plan.studio.remote.name
    code = version_backend(workspace, plan.studio, plan.version_name, description, log)
    main_studio = find_frontend_studio(workspace, plan.main, name)
    if main_studio is None:
        log(f"Creating {name} in the {plan.label}...")
        main_studio = remote.create(plan.main, name)
    log(f"Updating {name} in the {plan.label}...")
    remote.push(plan.main, main_studio.element_id, code)
    # Its microversion as pushed, which the Part Studios insert its features at
    pushed = next(each for each in remote.list_studios(plan.main) if each.element_id == main_studio.element_id)
    built = {
        definition: build_studios(remote, plan.main, studios, pushed, log) for definition, studios in plan.definitions
    }
    for definition, studios in built.items():
        for tab, manifest in released_manifests(definition, studios).items():
            log(f"Writing {tab} in the {plan.label}...")
            remote.put_file(plan.main, tab, manifest, "application/json")

    log(f"Creating version {plan.version_name} in the {plan.label}...")
    version = remote.create_version(plan.main, plan.version_name, description)
    if plan.released is None:
        return version
    for definition, studios in built.items():
        write_locations(
            definition, plan.main.document_id, version.id, {name: each.element_id for name, each in studios.items()}
        )
        for exported in write_exports(definition):
            log(f"Updated {exported.name}")
    # Its tab has to stay put from now on (see `load_released`)
    workspace.state.released.add(plan.studio.remote.element_id)
    return version
