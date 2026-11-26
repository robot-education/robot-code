"""Releasing FeatureScripts from the backend document to the frontend document (`[tool.fs] frontend`), or to a library.

A release (after pushing what it releases, see `cli.push_for_release`):
    1. Creates a version named e.g. "Robot frame - v1.3.0" in the backend document.
    2. Points the frontend document's Feature Studio of the same name at that version, using a
       small generated studio that re-exports the backend studio.
    3. Keeps the frontend document's side of FRCDesignApp's FeatureScripts tab (see fs_cli/frontend.py): creates an
       empty example Part Studio named for the feature if it has none, writes its icon, and its entry in
       featurescripts.json.
    4. Optionally creates a version in the frontend document too, which publishes the release: it's final once it's
       there (with `--publish`, or later by `fs publish`), and then it's tagged in git (see `tag_release`).
       `--publish` is refused while the feature has no example, so an empty one is never published.

A release to a library document (`fs release NAME --to DOC`), for a test FRCDesignApp, does the same there, from any
branch, but isn't numbered: its versions are named for the branch and commit (see `test_version_name`), the library
is versioned at once so FRCDesignApp can load it, and nothing is marked released or tagged.

A part set's FeatureScript (`[tool.fs.parts]`) is released with its Part Studios instead, by `fs release SET` (see
parts.py).

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

from fs_cli import frontend
from fs_cli.branches import branch_name
from fs_cli.config import Document, Role
from fs_cli.frontend import FrontendTab
from fs_cli.git import commit_with_contents, create_tag, has_tag, head_commit, is_repository, push_tag
from fs_cli.remote import Remote, RemoteStudio, Version
from fs_cli.versions import (
    ReleasedVersion,
    SemVersion,
    VersionType,
    feature_name_for,
    latest_release,
    next_version,
    parse_version_name,
)
from fs_cli.workspace import Status, Studio, UsageError, Workspace
from onshape_api.exceptions import ApiError
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


def test_version_name(workspace: Workspace, title: str) -> str:
    """The name of a release's versions in a library, which isn't numbered: named for the branch and commit it's
    released from (`Robot frame test - my-branch 1a2b3c4`), which `parse_version_name` doesn't take for a release."""
    commit = (head_commit(workspace.config.root) or "")[:7]
    return f"{title} test - {branch_name(workspace.config) or 'no branch'} {commit}".rstrip()


def check_release_document(document: Document, bump: VersionType | None, publish: bool) -> None:
    """Refuses what a release to a library doesn't take: it's unnumbered, and versioned at once."""
    if document.role is Role.LIBRARY:
        if bump is not None:
            raise UsageError(f"A release to a library ({document.name}) isn't numbered, so it takes no --major, --minor, or --patch.")
        if publish:
            raise UsageError(f"A release to a library ({document.name}) is versioned there at once, so it takes no --publish.")


@dataclasses.dataclass
class ReleasePlan:
    """A FeatureScript's release: to the frontend document, numbered (`released`), or to a library, to try it out.

    Attributes:
        document: Where it's released to.
        target: That document's workspace.
        released: Its version number; None for a library's, which isn't numbered, marked released, or tagged.
    """

    studio: Studio
    feature_name: str
    previous: ReleasedVersion | None
    version_name: str
    document: Document
    target: InstancePath
    released: ReleasedVersion | None
    # The name the app shows: its Feature Type Name, which its example is named for
    display_name: str = ""
    example: FrontendTab | None = None

    @property
    def label(self) -> str:
        return f"{self.document.role} document ({self.document.name})"


def plan_release(
    workspace: Workspace, script: str, bump: VersionType | None, document: Document, publish: bool = False
) -> ReleasePlan:
    check_release_document(document, bump, publish)
    studio = find_studio(workspace, script)
    _check_in_sync(studio, "releasing")
    assert studio.remote
    part_set = workspace.config.part_set_for(studio.path)
    if part_set:
        sets = ", ".join(each.name for each in workspace.config.parts if each.holds(studio.path))
        raise UsageError(f"{studio.path} is a part set's FeatureScript; release the part set ({sets}) instead, e.g. `fs release {part_set.name}`.")
    _check_one_feature(workspace, studio)
    remote = workspace.remote_for(document)
    target = workspace.instance_of(document)

    feature_name = feature_name_for(studio.remote.name)
    previous = latest_release(feature_name, workspace.remote.versions(workspace.instance))
    released = None
    if document.role is Role.LIBRARY:
        name = test_version_name(workspace, feature_name)
    else:
        try:
            released = ReleasedVersion(feature_name, next_version(previous, bump))
        except ValueError as error:
            raise UsageError(str(error)) from error
        name = released.name
    if publish and previous is None:
        # A new feature's frontend examples need making before it's final
        raise UsageError(
            f"{studio.path} has never been released, so its first release isn't published at once: release it "
            "without --publish, and `fs publish` publishes it once its frontend examples are ready."
        )
    display_name = frontend.feature_type_name(studio.local_code or "") or feature_name
    tabs = remote.frontend_tabs(target)
    studio_id = next(
        (tab.element_id for tab in tabs if tab.name == studio.remote.name and tab.element_type == "FEATURESTUDIO"),
        None,
    )
    example = frontend.find_example(tabs, studio_id, display_name)
    if example is None and publish:
        raise UsageError(
            f"{display_name} has no example in the frontend document, so it can't be published yet: "
            f"release it without --publish, which creates an empty Part Studio named {display_name}, model an "
            "example in it, then `fs publish` publishes it."
        )
    return ReleasePlan(studio, feature_name, previous, name, document, target, released, display_name, example)


def run_release(
    workspace: Workspace,
    plan: ReleasePlan,
    description: str,
    publish: bool,
    log: Callable[[str], None] = print,
) -> None:
    remote = workspace.remote_for(plan.document)
    assert plan.studio.remote
    studio_name = plan.studio.remote.name
    code = version_backend(workspace, plan.studio, plan.version_name, description, log)
    target_studio = find_frontend_studio(remote, plan.target, studio_name)
    if target_studio is None:
        log(f"Creating {studio_name} in the {plan.label}...")
        target_studio = remote.create(plan.target, studio_name)
    log(f"Updating {studio_name} in the {plan.label}...")
    remote.push(plan.target, target_studio.element_id, code)
    write_for_app(workspace, plan, target_studio.element_id, log)

    # A library is versioned at once, for FRCDesignApp to load
    if publish or plan.released is None:
        log(f"Creating version {plan.version_name} in the {plan.label}...")
        remote.create_version(plan.target, plan.version_name, description)
    if plan.released is not None:
        # Its tab has to stay put from now on (see `load_released`)
        workspace.state.released.add(plan.studio.remote.element_id)


def source_url(workspace: Workspace, path: str) -> str | None:
    """The file on GitHub as it is on the main branch: the latest code, not necessarily as released (its release's tag
    has that, but older releases have none)."""
    config = workspace.config
    if config.github is None:
        return None
    file = (config.code_dir / path).relative_to(config.root).as_posix()
    return f"{config.github}/blob/{config.main_branch or 'main'}/{file}"


def write_for_app(
    workspace: Workspace, plan: ReleasePlan, studio_element_id: str, log: Callable[[str], None] = print
) -> None:
    """The document's side of FRCDesignApp's FeatureScripts tab: the example, the icon, and the manifest."""
    remote, target, name = workspace.remote_for(plan.document), plan.target, plan.display_name
    code = plan.studio.local_code or ""
    if plan.example is None:
        log(f"Created {name}; model an example in it before publishing.")
        remote.create_part_studio(target, name)
    icon_id = None
    images = {element_id: image.file for element_id, image in workspace.state.images.items()}
    studios = {element_id: entry.file for element_id, entry in workspace.state.studios.items()}
    icon = frontend.icon_file(code, workspace.config.code_dir, images, studios) or default_icon(workspace)
    if icon is not None:
        log(f"Writing {frontend.icon_tab_name(name)} in the {plan.label}...")
        icon_id = remote.put_file(target, frontend.icon_tab_name(name), icon.read_bytes(), "image/svg+xml")
    entry = frontend.manifest_entry(
        code=code,
        studio_element_id=studio_element_id,
        fallback_name=plan.feature_name,
        version=_app_version(workspace, plan),
        icon_element_id=icon_id,
        source_url=source_url(workspace, plan.studio.path),
    )
    if entry is None:
        log(f"{plan.studio.path} exports no defineFeature, so FRCDesignApp won't list it.")
        return
    log(f"Writing its entry in {frontend.MANIFEST_NAME}...")
    manifest = frontend.with_entry(frontend.parse_manifest(remote.read_file(target, frontend.MANIFEST_NAME)), entry)
    remote.put_file(target, frontend.MANIFEST_NAME, frontend.dump_manifest(manifest), "application/json")


def _app_version(workspace: Workspace, plan: ReleasePlan) -> str:
    """The version FRCDesignApp shows: the release's; for a library's, the last release's with the commit as its build
    (`1.3.0+test.1a2b3c4`), a version which sorts as the last release does."""
    if plan.released is not None:
        return str(plan.released.version)
    commit = (head_commit(workspace.config.root) or "")[:7]
    version = plan.previous.version if plan.previous else SemVersion(0, 0, 0)
    return str(version.replace(build=f"test.{commit}" if commit else "test"))


def default_icon(workspace: Workspace) -> pathlib.Path | None:
    """The icon a feature with none of its own gets (`[tool.fs] default_icon`), if it's set and an `.svg`."""
    icon = workspace.config.default_icon
    return icon if icon is not None and icon.suffix.lower() == ".svg" and icon.is_file() else None


def unwrite_for_app(workspace: Workspace, plan: DeprecatePlan, name: str, log: Callable[[str], None] = print) -> None:
    """Takes a deprecated feature out of the app: its manifest entry goes, and its icon and example (unless another
    feature's example is it too) are renamed DEPRECATED."""
    remote, target = workspace.remote_for(plan.document), plan.target
    studio_id = plan.frontend_studio.element_id
    manifest = frontend.parse_manifest(remote.read_file(target, frontend.MANIFEST_NAME))
    others = [feature for feature in manifest["features"] if feature["studioElementId"] != studio_id]
    if len(others) != len(manifest["features"]):
        log(f"Removing its entry from {frontend.MANIFEST_NAME}...")
        remote.put_file(
            target,
            frontend.MANIFEST_NAME,
            frontend.dump_manifest(frontend.without_studio(manifest, studio_id)),
            "application/json",
        )
    tabs = remote.frontend_tabs(target)
    example = frontend.find_example(tabs, studio_id, name)
    shared = example is not None and any(
        frontend.find_example(tabs, other["studioElementId"], other["name"]) == example for other in others
    )
    if example is not None and example.name.strip().lower() == name.strip().lower() and not shared:
        log(f"Renaming {example.name} {deprecated_name(example.name)}...")
        remote.rename(target, example.element_id, deprecated_name(example.name))
    icon = next(
        (tab for tab in tabs if tab.element_type == "BLOB" and tab.name == frontend.icon_tab_name(name)), None
    )
    if icon is not None:
        log(f"Renaming {icon.name} {deprecated_name(icon.name)}...")
        remote.rename(target, icon.element_id, deprecated_name(icon.name))


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
    document: Document
    target: InstancePath
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
            f"released, run `fs mark-released {studio.path}` first."
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
    _check_one_feature(workspace, studio)
    document = frontend_document(workspace)
    target = workspace.instance_of(document)
    frontend_studio = find_frontend_studio(workspace.remote_for(document), target, studio.remote.name)
    if frontend_studio is None:
        raise UsageError(
            f"The frontend document has no {studio.remote.name}, so there's nothing to deprecate; delete the "
            f"file and run `fs mark-released --remove {studio.path}` instead."
        )
    return DeprecatePlan(
        studio,
        deprecated_name(feature_name_for(studio.remote.name)),
        document,
        target,
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
    remote, studio = workspace.remote_for(plan.document), plan.studio
    assert studio.remote
    # Before its feature is renamed DEPRECATED, so its example is found by the name it had
    name = frontend.feature_type_name(studio.local_code or "") or feature_name_for(studio.remote.name)
    if plan.code != studio.local_code:
        log(f"Marking the feature in {studio.path} deprecated and pushing it...")
        studio.file.write_text(plan.code, newline="")
        studio.local_code = plan.code
        workspace.push_studios([studio])
    code = version_backend(workspace, studio, plan.version_name, description, log)
    log(f"Updating {plan.frontend_studio.name} in the frontend document...")
    remote.push(plan.target, plan.frontend_studio.element_id, code)
    unwrite_for_app(workspace, plan, name, log)
    log(f"Renaming it {plan.frontend_name}...")
    remote.rename(plan.target, plan.frontend_studio.element_id, plan.frontend_name)
    if make_version:
        log(f"Creating version {plan.version_name} in the frontend document...")
        remote.create_version(plan.target, plan.version_name, description)

    # The frontend studio imports a version, so the backend tab isn't needed anymore
    workspace.state.released.discard(studio.remote.element_id)
    if plan.keep_backend:
        log(f"Renaming {studio.remote.name} in the backend document {deprecated_name(studio.remote.name)}...")
        workspace.remote.rename(workspace.instance, studio.remote.element_id, deprecated_name(studio.remote.name))
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


def _check_one_feature(workspace: Workspace, studio: Studio) -> None:
    """Refuses a studio which doesn't define exactly one feature, which is what's released."""
    assert studio.remote
    feature_names = workspace.remote.feature_names(workspace.instance, studio.remote.element_id)
    if len(feature_names) != 1:
        found = ", ".join(feature_names) or "none"
        raise UsageError(
            f"{studio.path} must define exactly one custom feature to be released (found {found})."
        )


def frontend_document(workspace: Workspace) -> Document:
    """The frontend document FeatureScripts are released to."""
    document = workspace.config.frontend
    if document is None:
        raise UsageError("Set [tool.fs] frontend to the frontend document FeatureScripts are released to.")
    return document


def find_frontend_studio(remote: Remote, target: InstancePath, name: str) -> RemoteStudio | None:
    """The studio in a frontend (or library) document re-exporting the backend studio named `name`, which has the same
    name."""
    return next((studio for studio in remote.list_studios(target) if studio.name == name), None)


def version_backend(
    workspace: Workspace, studio: Studio, name: str, description: str, log: Callable[[str], None]
) -> str:
    """Creates a version of the backend document. Returns the code of a studio re-exporting studio from it, for a
    frontend or library document."""
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


def unsynced_versions(workspace: Workspace, features: set[str] | None = None) -> list[Version]:
    """Returns the backend's release versions missing from the frontend, oldest first: each feature's newest, a release
    or its deprecation (`DEPRECATED <Feature name>`), unless the frontend has it. Its older releases are left out, since
    a frontend version made now holds the frontend as it is, importing the newest. A part set's releases aren't the
    frontend's, so they're left out too. With `features` (feature names), only theirs."""
    config, remote = workspace.config, workspace.remote
    document = frontend_document(workspace)
    frontend_names = {
        version.name for version in workspace.remote_for(document).versions(workspace.instance_of(document))
    }
    part_sets = {part_set.title for part_set in config.parts}
    missing = []
    seen = set()
    for version in reversed(remote.versions(workspace.instance)):
        feature = _versioned_feature(version.name)
        if feature is None or feature in part_sets or feature in seen:
            continue
        if features is not None and feature not in features:
            continue
        seen.add(feature)
        if version.name not in frontend_names:
            missing.append(version)
    return missing[::-1]


def _versioned_feature(name: str) -> str | None:
    """The feature a backend version releases (`Robot frame - v1.3.0`) or deprecates (`DEPRECATED Robot frame`), or
    None for other versions."""
    released = parse_version_name(name)
    if released:
        return released.feature_name
    if name.startswith(DEPRECATED) and len(name) > len(DEPRECATED):
        return name[len(DEPRECATED) :]
    return None


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
    workspace: Workspace,
    path: str,
    released: ReleasedVersion,
    contents: str,
    push: bool,
    log: Callable[[str], None] = print,
    tagged: str | None = None,
) -> str | None:
    """Tags the newest commit in which `path` (relative to the code folder) has `contents` (the released code) as the
    release, and pushes the tag to origin if `push`. Returns the tag, or None if no commit has it. Releases are tagged
    once they're final: once their version is in the frontend document.

    Args:
        tagged: What the tag is named for, in place of `path`'s file: a part set, since sets share a FeatureScript.
    """
    root = workspace.config.root
    name = release_tag(tagged or path, released.version)
    if has_tag(root, name):
        return name
    commit = commit_with_contents(root, workspace.config.code_dir / path, contents)
    if commit is None:
        log(f"Couldn't tag {name}: no commit has {path} as it was released.")
        return None
    if not create_tag(root, name, commit, released.name):
        log(f"Couldn't tag {name}.")
        return None
    log(f"Tagged {name} ({commit[:7]}).")
    if push and not push_tag(root, name):
        log(f"Couldn't push {name}; push it with `git push origin {name}`.")
    return name


def tag_versions(
    workspace: Workspace, versions: list[Version], push: bool, log: Callable[[str], None] = print
) -> int:
    """Tags the newest of each feature's final release versions (see `tag_release`; versions are oldest first), if it
    isn't tagged yet, from its studio's code in the backend version (an API call). Versions of studios which aren't
    synced with a file (like a deprecated one's) are skipped. Returns how many were tagged."""
    root = workspace.config.root
    if not is_repository(root):
        log("Not in a git repository, so releases aren't tagged.")
        return 0
    remote = workspace.remote
    studios = {feature_name_for(studio.name): studio for studio in remote.list_studios(workspace.instance)}
    files = {element_id: entry.file for element_id, entry in workspace.state.studios.items()}
    newest: dict[str, Version] = {}
    for version in versions:
        released = parse_version_name(version.name)
        # A deprecation has no file to tag
        if released:
            newest[released.feature_name] = version
    tagged = 0
    for version in newest.values():
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
        at_version = InstancePath(workspace.instance.document_id, version.id, InstanceType.VERSION)
        code = _released_code(workspace, at_version, studio.element_id, released.feature_name)
        if code is None:
            log(f"Skipping {version.name}: couldn't read its code in that version.")
            continue
        if tag_release(workspace, path, released, code, push, log) is not None:
            tagged += 1
    return tagged


def _released_code(workspace: Workspace, at_version: InstancePath, element_id: str, feature_name: str) -> str | None:
    """A feature's code in a backend version, by its studio's element id now; else (when its tab was recreated since,
    so the version holds another) by its name in the version. None if neither can be read."""
    remote = workspace.remote
    try:
        return remote.pull(at_version, element_id)
    except ApiError:
        pass
    try:
        studio = next(
            (each for each in remote.list_studios(at_version) if feature_name_for(each.name) == feature_name), None
        )
        return remote.pull(at_version, studio.element_id) if studio is not None else None
    except ApiError:
        return None
