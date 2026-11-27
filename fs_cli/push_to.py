"""`fs push SCRIPT... --to DOC`: pushes FeatureScripts, and every studio and image they import from the repo, to another
backend document in [tool.fs.documents] than the one synced with the repo (`[tool.fs] backend`), to try them out there.

The target's tabs are matched with files by name (a studio's file name, like `robotFrame.fs`, or its stem), not by
fs-studios.json, which is the backend's: a tab of the same name is overwritten, and a missing one is created. Imports
are pointed at the target's tabs as each is pushed, in import order, so the target holds a working copy of just what
those FeatureScripts need. Nothing local changes: no file, fs-studios.json, or .fs-state.json is written, and nothing
is committed. Nothing in the target is deleted.

A document with keys of its own (an Onshape Enterprise's) is reached with them, on its own domain, and every
request is signed. Otherwise anyone with its link must be able to open it (it's public or link-shared): with the
usual keys, tabs are listed with anonymous requests (see onshape_api/README.md#anonymous-requests), which only work for
those. See docs/fs-cli.md#pushing-to-another-backend.
"""

from __future__ import annotations

import dataclasses
import pathlib
from concurrent import futures

from fs_cli.config import Config, Document
from fs_cli.remote import Remote, RemoteImage, RemoteStudio, image_type, is_local
from fs_cli.state import (
    import_versions,
    load_image_files,
    load_studio_files,
    path_imports,
    resolve_path_imports,
    retarget_imports,
)
from fs_cli.workspace import MAX_WORKERS, UsageError
from onshape_api.paths.paths import InstancePath

@dataclasses.dataclass
class Tab:
    """A file to push, and the target's tab of the same name, if it has one."""

    path: str
    file: pathlib.Path
    element_id: str | None = None

    @property
    def name(self) -> str:
        return pathlib.PurePosixPath(self.path).name

    def matches(self, tab_name: str) -> bool:
        return tab_name in (self.name, pathlib.PurePosixPath(self.path).stem)


@dataclasses.dataclass
class PushToPlan:
    instance: InstancePath
    url: str
    document_name: str
    # In import order: each studio after the studios it imports
    studios: list[Tab]
    images: list[Tab]
    # Each studio's path -> the paths of the studios it imports
    imports: dict[str, list[str]]
    # The backend element id each synced file is imported by (from fs-studios.json), by path
    backend_ids: dict[str, str]
    # Images whose tabs in the target already hold the same file, so aren't uploaded
    unchanged_images: list[Tab] = dataclasses.field(default_factory=list)



def check_target(remote: Remote, target: Document) -> str:
    """Refuses a document reached with the usual keys which anyone with its link can't open (with an anonymous
    request, so free); its name. One with keys of its own is reached with them, and every request signed, so isn't
    checked."""
    if target.keys is not None:
        return target.name
    document = remote.shared_document(target.document_id)
    if document is None:
        raise UsageError(
            f"{target.name} is neither public nor link-shared, so `fs push --to` can't push to it with the usual "
            "keys: fs lists a document's tabs (and their microversions) with anonymous requests, which only work for "
            "documents anyone with the link can open. Turn on link sharing in its Share dialog (or make it public), "
            f"or give it keys of its own ([tool.fs.documents.{target.name}] keys)."
        )
    return document.name


def needed_files(
    config: Config, scripts: list[pathlib.Path]
) -> tuple[list[Tab], list[Tab], dict[str, list[str]]]:
    """The studios and images pushing scripts needs: each script, and every studio and image it imports from the repo
    (by element id, through fs-studios.json, or by path), directly or not. Studios come in import order. Makes no API
    calls."""
    code_dir = config.code_dir
    studio_files = load_studio_files(config.studios_path)
    image_files = load_image_files(config.studios_path)
    # Path -> the studio paths it imports
    imports: dict[str, list[str]] = {}
    images: list[str] = []
    waiting = [path.resolve().relative_to(code_dir.resolve()).as_posix() for path in scripts]
    while waiting:
        path = waiting.pop(0)
        if path in imports:
            continue
        file = code_dir / path
        if not file.is_file():
            raise UsageError(f"{path} doesn't exist.")
        code = file.read_text(encoding="utf-8")
        imported: list[str] = []
        for element_id in import_versions(code):
            if element_id in image_files:
                images.append(image_files[element_id])
            elif element_id in studio_files:
                imported.append(studio_files[element_id])
            else:
                raise UsageError(
                    f"{path} imports the tab {element_id}, which no file here is synced with (see `fs tabs`)."
                )
        for imported_path in path_imports(code):
            if not (code_dir / imported_path).is_file():
                raise UsageError(f"{path} imports {imported_path}, which doesn't exist.")
            (images if image_type(imported_path) else imported).append(imported_path)
        imports[path] = imported
        waiting.extend(imported)
    for path in [*imports, *images]:
        if is_local(path):
            raise UsageError(f"{path} is a local file, which isn't pushed.")
    studios = [Tab(path, code_dir / path) for path in _import_order(imports)]
    image_tabs = [Tab(path, code_dir / path) for path in dict.fromkeys(images)]
    names: dict[str, str] = {}
    for tab in [*studios, *image_tabs]:
        other = names.setdefault(tab.name.lower(), tab.path)
        if other != tab.path:
            raise UsageError(f"{other} and {tab.path} would both be the tab {tab.name}: rename one.")
    return studios, image_tabs, imports


def _import_order(imports: dict[str, list[str]]) -> list[str]:
    ordered: list[str] = []
    visiting: list[str] = []

    def visit(path: str) -> None:
        if path in ordered:
            return
        if path in visiting:
            cycle = visiting[visiting.index(path) :]
            raise UsageError("These FeatureScripts import each other: " + ", ".join(cycle))
        visiting.append(path)
        for imported in imports[path]:
            visit(imported)
        visiting.pop()
        ordered.append(path)

    for path in imports:
        visit(path)
    return ordered


def plan_push_to(
    config: Config, remote: Remote, target: Document, instance: InstancePath, scripts: list[pathlib.Path]
) -> PushToPlan:
    """Matches what pushing scripts needs with the target's tabs: `check_target`, then listings, and downloads of
    images with tabs of the same names, to skip those already the same (anonymous, unless the target has keys of its
    own)."""
    studios, images, imports = needed_files(config, scripts)
    name = check_target(remote, target)
    backend_ids = {
        file: element_id
        for element_id, file in {
            **load_image_files(config.studios_path),
            **load_studio_files(config.studios_path),
        }.items()
    }
    plan = PushToPlan(instance, target.url, name, studios, images, imports, backend_ids)
    _match(studios, remote.list_studios(instance))
    if images:
        _match(images, remote.list_images(instance))
    for image in images:
        if image.element_id and remote.download_image(instance, image.element_id) == image.file.read_bytes():
            plan.unchanged_images.append(image)
    return plan


def _match(tabs: list[Tab], remote_tabs: list[RemoteStudio] | list[RemoteImage]) -> None:
    for tab in tabs:
        found = [each for each in remote_tabs if tab.matches(each.name)]
        if len(found) > 1:
            raise UsageError(f"The target has {len(found)} tabs named {tab.name}; delete or rename all but one.")
        if found:
            tab.element_id = found[0].element_id


def run_push_to(remote: Remote, plan: PushToPlan) -> None:
    """Pushes the plan's images, then its studios in waves, each after the studios it imports, importing the target's
    tabs at the versions just pushed: 1 call per image and studio pushed, and 1 per tab created, plus anonymous
    listings."""
    instance = plan.instance
    # Each file pushed (or already the same) -> its tab in the target, and the version its importers name
    targets: dict[str, tuple[str, str]] = {}
    unchanged = [image.path for image in plan.unchanged_images]
    if unchanged:
        listed_images = {image.element_id: image.microversion_id for image in remote.list_images(instance)}
    for image in plan.images:
        if image.path in unchanged:
            assert image.element_id
            targets[image.path] = (image.element_id, listed_images[image.element_id])
            continue
        data = image.file.read_bytes()
        uploaded = (
            remote.update_image(instance, image.element_id, image.name, data)
            if image.element_id
            else remote.upload_image(instance, image.name, data)
        )
        image.element_id = uploaded.element_id
        targets[image.path] = (uploaded.element_id, uploaded.microversion_id)

    def create(tab: Tab) -> None:
        tab.element_id = remote.create(instance, tab.name).element_id

    with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
        list(executor.map(create, [tab for tab in plan.studios if tab.element_id is None]))

    def push(tab: Tab) -> None:
        assert tab.element_id
        # Imports by path, and of the backend's tabs by id, of the target's tabs instead
        by_backend_id = {plan.backend_ids[path]: target for path, target in targets.items() if path in plan.backend_ids}
        code = retarget_imports(resolve_path_imports(tab.file.read_text(encoding="utf-8"), targets), by_backend_id)
        remote.push(instance, tab.element_id, code)

    pushed: list[Tab] = []
    for wave in _waves(plan):
        if pushed:
            # The new microversions of the studios pushed so far, which their importers' imports name
            listed = {studio.element_id: studio.microversion_id for studio in remote.list_studios(instance)}
            for tab in pushed:
                assert tab.element_id
                targets[tab.path] = (tab.element_id, listed.get(tab.element_id, ""))
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(push, wave))
        pushed.extend(wave)


def _waves(plan: PushToPlan) -> list[list[Tab]]:
    """The studios in waves, each after every studio it imports (they're in import order, so this can't stall)."""
    waves: list[list[Tab]] = []
    done: set[str] = set()
    waiting = list(plan.studios)
    while waiting:
        wave = [tab for tab in waiting if all(path in done for path in plan.imports[tab.path])]
        waves.append(wave)
        done.update(tab.path for tab in wave)
        waiting = [tab for tab in waiting if tab not in wave]
    return waves
