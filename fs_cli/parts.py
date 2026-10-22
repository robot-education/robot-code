"""Part sets (`[tool.fs.parts]`): Part Studios built from a definition's configured studios, like FRCDesign's fasteners,
whose FeatureScript only they use. Their FeatureScript is in the backend document like any other, and its imports work
the same, but it's tried and released here rather than by `fs release`:

- `fs parts test <set>` builds each Part Studio in the backend document, named `<tab> (TEST)`, from the FeatureScript as
  pushed, to try a change. Nothing else is written.
- `fs parts release <set>` versions the backend document, points the set's main document (an FRCDesignLib document) at
  that version, builds each Part Studio there, versions the main document, records where they are beside their
  definition (`<definition>.studios.json`), writes the definition's exports again, and tags the release in git.

See docs/parts.md.
"""

from __future__ import annotations

import dataclasses
import pathlib
import re
import runpy
from collections.abc import Callable

from fs_cli.config import PartSet
from fs_cli.configured_studios import (
    Built,
    ConfiguredStudio,
    configured_studios,
    write_locations,
)
from fs_cli.release import find_frontend_studio, version_backend
from fs_cli.remote import Remote, RemoteStudio, Version
from fs_cli.versions import (
    ReleasedVersion,
    VersionType,
    latest_release,
    next_version,
    version_name,
)
from fs_cli.workspace import Status, Studio, UsageError, Workspace
from onshape_api.paths.paths import InstancePath

# What a test Part Studio's tab is called after its studio's, e.g. `Screw (TEST)`
TEST_SUFFIX = " (TEST)"


def defines(code: str, feature: str) -> bool:
    """Whether a Feature Studio's code defines a feature, by the constant it's exported as."""
    return re.search(rf"\bexport\s+const\s+{re.escape(feature)}\s*=\s*defineFeature\b", code) is not None


def part_set_definitions(workspace: Workspace, part_set: PartSet) -> list[tuple[pathlib.Path, list[ConfiguredStudio]]]:
    """The definitions in a part set's folder which configure Part Studios, each with its studios."""
    folder = workspace.config.code_dir / part_set.folder
    found = []
    for path in sorted(folder.rglob("*.py")):
        studios = configured_studios(runpy.run_path(str(path)))
        if studios:
            found.append((path, studios))
    if not found:
        raise UsageError(f"Nothing in {part_set.folder}/ configures a Part Studio (a definition's STUDIOS).")
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
        result = remote.build_studio(instance, studio, feature.element_id, feature.microversion_id)
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


# Releasing


@dataclasses.dataclass
class PartReleasePlan:
    part_set: PartSet
    studio: Studio
    definitions: list[tuple[pathlib.Path, list[ConfiguredStudio]]]
    previous: ReleasedVersion | None
    released: ReleasedVersion
    main: InstancePath

    @property
    def version_name(self) -> str:
        return self.released.name


def plan_part_release(workspace: Workspace, part_set: PartSet, bump: VersionType | None) -> PartReleasePlan:
    definitions = part_set_definitions(workspace, part_set)
    studio = feature_studio(workspace, part_set, [each for _, studios in definitions for each in studios])
    main = workspace.remote.default_workspace(part_set.main_document_id)
    previous = latest_release(part_set.title, workspace.remote.versions(main))
    try:
        version = next_version(previous, bump, beta=False)
    except ValueError as error:
        raise UsageError(str(error)) from error
    return PartReleasePlan(part_set, studio, definitions, previous, ReleasedVersion(part_set.title, version), main)


def run_part_release(
    workspace: Workspace, plan: PartReleasePlan, description: str, log: Callable[[str], None] = print
) -> Version:
    remote = workspace.remote
    assert plan.studio.remote
    name = plan.studio.remote.name
    code = version_backend(workspace, plan.studio, plan.version_name, description, log)
    main_studio = find_frontend_studio(workspace, plan.main, name)
    if main_studio is None:
        log(f"Creating {name} in the main document...")
        main_studio = remote.create(plan.main, name)
    log(f"Updating {name} in the main document...")
    remote.push(plan.main, main_studio.element_id, code)
    # Its microversion as pushed, which the Part Studios insert its features at
    pushed = next(each for each in remote.list_studios(plan.main) if each.element_id == main_studio.element_id)
    built = {
        definition: build_studios(remote, plan.main, studios, pushed, log) for definition, studios in plan.definitions
    }

    log(f"Creating version {plan.version_name} in the main document...")
    version = remote.create_version(plan.main, plan.version_name, description)
    for definition, studios in built.items():
        write_locations(
            definition, plan.main.document_id, version.id, {name: each.element_id for name, each in studios.items()}
        )
        for exported in write_exports(definition):
            log(f"Updated {exported.name}")
    # Its tab has to stay put from now on (see `load_released`)
    workspace.state.released.add(plan.studio.remote.element_id)
    return version
