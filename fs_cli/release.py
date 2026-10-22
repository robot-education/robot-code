"""Releasing FeatureScripts from the backend document to the public frontend document.

A release:
    1. Creates a version named e.g. "Robot frame - v1.3.0" in the backend document.
    2. Points the frontend document's Feature Studio of the same name at that version, using a
       small generated studio that re-exports the backend studio.
    3. Optionally creates a version in the frontend document too, which publishes the release: it's final once it's
       there (with `--publish`, or later by `fs sync-versions`), and then it's tagged in git (see `tag_release`).

Beta releases go to the separate frontend_beta document instead, and a library's FeatureScripts (`[tool.fs.libraries]`)
to its library document: they're only for that library's parts, not for everyone. A library release also builds the
Part Studios its definitions configure (see studio_builds.py) from the released studio, then always versions the
library, and records where each Part Studio is beside its definition.

Deprecating a released FeatureScript retires it without breaking Part Studios using it: they keep working, and
still update to newer versions of the frontend document, since its frontend studio is kept (with the same element
id). Its feature is renamed "DEPRECATED <name>", released one last time as "DEPRECATED <Feature name>", and its
frontend tab is renamed to match; then its backend tab can be deleted, since the frontend studio imports a version.
Folders can't be renamed through the API, so any holding it are renamed by hand.
"""

from __future__ import annotations

import dataclasses
import pathlib
import re
from typing import Callable

from fs_cli.configured_studios import ConfiguredStudio, write_locations
from fs_cli.git import commit_with_contents, create_tag, has_tag, is_repository, push_tag
from fs_cli.remote import RemoteStudio, Version
from fs_cli.studio_builds import build_studios, library_definitions, write_exports
from fs_cli.versions import (
    ReleasedVersion,
    SemVersion,
    VersionType,
    feature_name_for,
    latest_release,
    next_version,
    parse_version_name,
    version_name,
)
from fs_cli.workspace import Status, Studio, UsageError, Workspace
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import ElementPath, InstancePath, path_to_url

RELEASE_TEMPLATE = """FeatureScript {std_version};

/* Automatically generated file -- DO NOT EDIT */

/**
 * {version_name}
 * Source code (public): {source_url}{github_line}
 * FeatureScript by Alex Kempen.
 */
export import(path : "{import_path}", version : "{microversion_id}");
"""


def release_studio_code(
    version_name: str,
    std_version: str,
    studio_path: ElementPath,
    microversion_id: str,
    github_url: str | None = None,
) -> str:
    """Generates a frontend studio which re-exports a released (versioned) backend studio, linking its source in the
    backend document, and on GitHub (`github_url`, the repo's page), if given."""
    return RELEASE_TEMPLATE.format(
        std_version=std_version,
        version_name=version_name,
        source_url=path_to_url(studio_path),
        github_line=f"\n * Source code (GitHub): {github_url}" if github_url else "",
        import_path="/".join(
            [studio_path.document_id, studio_path.instance_id, studio_path.element_id]
        ),
        microversion_id=microversion_id,
    )


@dataclasses.dataclass
class ReleasePlan:
    studio: Studio
    feature_name: str
    previous: ReleasedVersion | None
    version_name: str
    target: InstancePath
    target_label: str
    # A library's definitions configuring Part Studios from the studio, each with its studios
    part_studios: list[tuple[pathlib.Path, list[ConfiguredStudio]]] = dataclasses.field(default_factory=list)


def plan_release(
    workspace: Workspace, script: str, bump: VersionType | None, beta: bool
) -> ReleasePlan:
    remote = workspace.remote
    studio = find_studio(workspace, script)
    _check_in_sync(studio, "releasing")
    assert studio.remote
    library = workspace.config.library_for(studio.path)
    if beta and library:
        raise UsageError(f"{studio.path} is released to the {library.name} library, which has no beta releases.")
    is_beta_feature = _is_beta(workspace, studio)
    if beta and not is_beta_feature:
        raise UsageError(
            "Beta releases must be of features whose name contains 'beta'."
        )
    if not beta and is_beta_feature:
        raise UsageError(
            "Features whose name contains 'beta' can only be released with --beta."
        )
    target, target_label = _target(workspace, studio, beta)

    feature_name = feature_name_for(studio.remote.name)
    previous = latest_release(feature_name, remote.versions(workspace.instance))
    try:
        version = next_version(previous, bump, beta)
    except ValueError as error:
        raise UsageError(str(error)) from error
    part_studios = (
        library_definitions(workspace.config, library, studio.local_code or "") if library else []
    )
    return ReleasePlan(
        studio,
        feature_name,
        previous,
        version_name(feature_name, version),
        target,
        target_label,
        part_studios,
    )


def run_release(
    workspace: Workspace,
    plan: ReleasePlan,
    description: str,
    make_version: bool,
    log: Callable[[str], None] = print,
) -> None:
    remote = workspace.remote
    assert plan.studio.remote
    studio_name = plan.studio.remote.name
    code = _version_backend(workspace, plan.studio, plan.version_name, description, log)
    target_studio = _frontend_studio(workspace, plan.target, studio_name)
    if target_studio is None:
        log(f"Creating {studio_name} in the {plan.target_label} document...")
        target_studio = remote.create(plan.target, studio_name)
    log(f"Updating {studio_name} in the {plan.target_label} document...")
    remote.push(plan.target, target_studio.element_id, code)

    built = {}
    if plan.part_studios:
        # Its microversion as pushed, which the Part Studios insert its features at
        pushed = next(each for each in remote.list_studios(plan.target) if each.element_id == target_studio.element_id)
        for definition, studios in plan.part_studios:
            built[definition] = build_studios(remote, plan.target, studios, pushed, log)

    if make_version:
        log(
            f"Creating version {plan.version_name} in the {plan.target_label} document..."
        )
        version = remote.create_version(plan.target, plan.version_name, description)
        for definition, studios in built.items():
            write_locations(
                definition,
                plan.target.document_id,
                version.id,
                {name: each.element_id for name, each in studios.items()},
            )
            for exported in write_exports(definition):
                log(f"Updated {exported.name}")
    # Its tab has to stay put from now on (see `load_released`)
    workspace.state.released.add(plan.studio.remote.element_id)


# Deprecating

# The start of the names of a deprecated FeatureScript's feature, tabs, and last version, e.g. "DEPRECATED Robot frame"
DEPRECATED = "DEPRECATED "


def deprecated_name(name: str) -> str:
    """e.g. robotFrame.fs -> DEPRECATED robotFrame.fs"""
    return name if name.startswith(DEPRECATED) else DEPRECATED + name
_FEATURE_TYPE_NAME = re.compile(r'("Feature Type Name"\s*:\s*")([^"]*)(")')


def deprecated_code(code: str) -> str | None:
    """Marks the feature defined in code deprecated in its name, or returns None if it doesn't have one."""
    match = _FEATURE_TYPE_NAME.search(code)
    if match is None:
        return None
    return code[: match.start(2)] + deprecated_name(match[2]) + code[match.end(2) :]


@dataclasses.dataclass
class DeprecatePlan:
    studio: Studio
    version_name: str
    target: InstancePath
    target_label: str
    frontend_studio: RemoteStudio
    code: str
    keep_backend: bool

    @property
    def frontend_name(self) -> str:
        return deprecated_name(self.frontend_studio.name)


def plan_deprecate(workspace: Workspace, script: str, keep_backend: bool) -> DeprecatePlan:
    studio = find_studio(workspace, script)
    if not studio.released:
        raise UsageError(
            f"{studio.path} isn't released, so it can just be deleted (delete the file, then `fs push`). If it is "
            f"released, run `fs released {studio.path}` first."
        )
    _check_in_sync(studio, "deprecating")
    assert studio.remote and studio.local_code is not None
    importers = workspace.importers(studio.remote.element_id)
    if importers and not keep_backend:
        raise UsageError(
            f"{studio.path} is imported by {', '.join(importers)}, so its tab can't be deleted; remove those "
            "imports, or pass --keep-backend."
        )
    code = deprecated_code(studio.local_code)
    if code is None:
        raise UsageError(f'{studio.path} has no "Feature Type Name" to mark deprecated.')
    target, target_label = _target(workspace, studio, _is_beta(workspace, studio))
    frontend_studio = _frontend_studio(workspace, target, studio.remote.name)
    if frontend_studio is None:
        raise UsageError(
            f"The {target_label} document has no {studio.remote.name}, so there's nothing to deprecate; delete the "
            f"file and run `fs released --remove {studio.path}` instead."
        )
    return DeprecatePlan(
        studio,
        deprecated_name(feature_name_for(studio.remote.name)),
        target,
        target_label,
        frontend_studio,
        code,
        keep_backend,
    )


def run_deprecate(
    workspace: Workspace,
    plan: DeprecatePlan,
    description: str,
    make_version: bool,
    log: Callable[[str], None] = print,
) -> None:
    remote, studio = workspace.remote, plan.studio
    assert studio.remote
    if plan.code != studio.local_code:
        log(f"Marking the feature in {studio.path} deprecated and pushing it...")
        studio.file.write_text(plan.code, newline="")
        studio.local_code = plan.code
        workspace.push_studios([studio])
    code = _version_backend(workspace, studio, plan.version_name, description, log)
    log(f"Updating {plan.frontend_studio.name} in the {plan.target_label} document...")
    remote.push(plan.target, plan.frontend_studio.element_id, code)
    log(f"Renaming it {plan.frontend_name}...")
    remote.rename(plan.target, plan.frontend_studio.element_id, plan.frontend_name)
    if make_version:
        log(f"Creating version {plan.version_name} in the {plan.target_label} document...")
        remote.create_version(plan.target, plan.version_name, description)

    # The frontend studio imports a version, so the backend tab isn't needed anymore
    workspace.state.released.discard(studio.remote.element_id)
    if plan.keep_backend:
        log(f"Renaming {studio.remote.name} in the backend document {deprecated_name(studio.remote.name)}...")
        remote.rename(workspace.instance, studio.remote.element_id, deprecated_name(studio.remote.name))
    else:
        log(f"Deleting {studio.remote.name} in the backend document, and {studio.path}...")
        workspace.delete_studios([studio])
        studio.file.unlink()


# Helpers


def _check_in_sync(studio: Studio, doing: str) -> None:
    if studio.status != Status.IN_SYNC:
        raise UsageError(
            f"{studio.path} is {studio.status.value}; run `fs push` (or `fs status`) before {doing} it."
        )


def _is_beta(workspace: Workspace, studio: Studio) -> bool:
    """Whether the one feature studio defines is a beta (released to the frontend_beta document). A library's studio
    is never a beta, and may define several features: they're its parts' features, released together."""
    assert studio.remote
    if workspace.config.library_for(studio.path):
        return False
    feature_names = workspace.remote.feature_names(workspace.instance, studio.remote.element_id)
    if len(feature_names) != 1:
        found = ", ".join(feature_names) or "none"
        raise UsageError(
            f"{studio.path} must define exactly one custom feature to be released (found {found})."
        )
    return "beta" in feature_names[0].lower()


def _target(workspace: Workspace, studio: Studio, beta: bool) -> tuple[InstancePath, str]:
    config = workspace.config
    library = config.library_for(studio.path)
    if library is not None:
        return workspace.remote.default_workspace(library.document_id), f"{library.name} library"
    target = config.frontend_beta if beta else config.frontend
    target_label = "frontend_beta" if beta else "frontend"
    if target is None:
        raise UsageError(f"Set [tool.fs] {target_label} in pyproject.toml to release.")
    return target, target_label


def _frontend_studio(workspace: Workspace, target: InstancePath, name: str) -> RemoteStudio | None:
    """The frontend studio re-exporting the backend studio named `name`, which has the same name."""
    return next((studio for studio in workspace.remote.list_studios(target) if studio.name == name), None)


def _version_backend(
    workspace: Workspace, studio: Studio, name: str, description: str, log: Callable[[str], None]
) -> str:
    """Creates a version of the backend document. Returns the code of a frontend studio re-exporting studio
    from it."""
    remote, backend = workspace.remote, workspace.instance
    assert studio.remote
    log(f"Creating version {name} in the backend document...")
    version = remote.create_version(backend, name, description)
    version_instance = InstancePath.from_path(backend, version.id, InstanceType.VERSION)
    released = next(
        (
            versioned
            for versioned in remote.list_studios(version_instance)
            if versioned.element_id == studio.remote.element_id
        ),
        None,
    )
    if released is None:
        raise RuntimeError(
            f"{studio.remote.name} is unexpectedly missing from the new backend version; nothing was published."
        )
    return release_studio_code(
        name,
        remote.latest_std_version(),
        ElementPath.from_path(version_instance, released.element_id),
        released.microversion_id,
        workspace.config.github,
    )


def _library_features(workspace: Workspace) -> set[str]:
    """The feature names of the FeatureScripts released to libraries, which their release versions are named by."""
    return {
        feature_name_for(studio.remote.name)
        for studio in workspace.match()
        if studio.remote and workspace.config.library_for(studio.path)
    }


def unsynced_versions(workspace: Workspace) -> list[Version]:
    """Returns the release versions in the backend which are missing from the frontend, oldest first. A library's
    releases aren't the frontend's, so they're left out."""
    config, remote = workspace.config, workspace.remote
    if config.frontend is None:
        raise UsageError("Set [tool.fs] frontend in pyproject.toml to sync versions.")
    frontend_names = {
        version.name
        for version in remote.versions(config.frontend)
        if parse_version_name(version.name)
    }
    library_features = _library_features(workspace) if config.libraries else set()
    missing = []
    # Walk back from the newest backend version until reaching one already in the frontend
    for version in reversed(remote.versions(workspace.instance)):
        released = parse_version_name(version.name)
        if not released or released.feature_name in library_features:
            continue
        if version.name in frontend_names:
            break
        missing.append(version)
    return missing[::-1]


def find_studio(workspace: Workspace, script: str) -> Studio:
    studios = workspace.scan(workspace.resolve_targets([script]))
    if len(studios) != 1:
        names = ", ".join(studio.path for studio in studios)
        raise UsageError(f'"{script}" matches several FeatureScripts: {names}.')
    studio = studios[0]
    if studio.remote is None:
        raise UsageError(
            f"{studio.path} doesn't exist in Onshape yet; run `fs push` first."
        )
    return studio


# Tagging releases in git


def release_tag(path: str, version: SemVersion) -> str:
    """A release's git tag: its file's name and its version, e.g. robotShaft/v2.3.0."""
    return f"{pathlib.PurePosixPath(path).stem}/v{version}"


def tag_release(
    workspace: Workspace, path: str, released: ReleasedVersion, contents: str, push: bool, log: Callable[[str], None] = print
) -> str | None:
    """Tags the newest commit in which `path` (relative to the code folder) has `contents` (the released code) as the
    release, and pushes the tag to origin if `push`. Returns the tag, or None if no commit has it. Releases are tagged
    once they're final: once their version is in the frontend document."""
    root = workspace.config.root
    name = release_tag(path, released.version)
    if has_tag(root, name):
        return name
    commit = commit_with_contents(root, workspace.config.code_dir / path, contents)
    if commit is None:
        log(f"Couldn't tag {name}: no commit has {path} as it was released. Commit it, then run `fs release-tags`.")
        return None
    if not create_tag(root, name, commit, released.name):
        log(f"Couldn't tag {name}.")
        return None
    log(f"Tagged {name} ({commit[:7]}).")
    if push and not push_tag(root, name):
        log(f"Couldn't push {name}; push it with `git push origin {name}`.")
    return name


def finalized_versions(workspace: Workspace) -> list[Version]:
    """The backend document's release versions which are in the frontend (or frontend_beta) document too, so final,
    oldest first."""
    config, remote = workspace.config, workspace.remote
    published = set()
    targets = [config.frontend, config.frontend_beta] + [
        remote.default_workspace(library.document_id) for library in config.libraries
    ]
    for target in targets:
        if target is not None:
            published.update(version.name for version in remote.versions(target))
    return [
        version
        for version in remote.versions(workspace.instance)
        if parse_version_name(version.name) and version.name in published
    ]


def tag_versions(
    workspace: Workspace, versions: list[Version], push: bool, dry_run: bool = False, log: Callable[[str], None] = print
) -> int:
    """Tags final release versions (see `tag_release`) which aren't tagged yet, each from its studio's code in the
    backend version (an API call each). Versions of studios which aren't synced with a file (like a deprecated one's)
    are skipped. Returns how many were tagged (or would be, with `dry_run`)."""
    root = workspace.config.root
    if not is_repository(root):
        log("Not in a git repository, so releases aren't tagged.")
        return 0
    remote = workspace.remote
    studios = {feature_name_for(studio.name): studio for studio in remote.list_studios(workspace.instance)}
    files = {element_id: entry.file for element_id, entry in workspace.state.studios.items()}
    tagged = 0
    for version in versions:
        released = parse_version_name(version.name)
        assert released
        studio = studios.get(released.feature_name)
        if studio is None or studio.element_id not in files:
            log(f"Skipping {version.name}: it isn't synced with a file.")
            continue
        path = files[studio.element_id]
        name = release_tag(path, released.version)
        if has_tag(root, name):
            continue
        if dry_run:
            log(f"Would tag {name}.")
            tagged += 1
            continue
        at_version = InstancePath(workspace.instance.document_id, version.id, InstanceType.VERSION)
        if tag_release(workspace, path, released, remote.pull(at_version, studio.element_id), push, log) is not None:
            tagged += 1
    return tagged
