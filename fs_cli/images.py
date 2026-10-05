"""Syncs the images in the code folder (SVGs and PNGs, for icons) with image tabs in the backend document.

A Feature Studio imports an image tab like it imports another studio, then uses its `BLOB_DATA`:

    RobotIcon::import(path : "233760ca14ddd2085de9c219", version : "f7c25b250b6b31d37d4e95ff");

    annotation { "Feature Type Name" : "Robot grid", "Icon" : RobotIcon::BLOB_DATA }

To use a new image, import it by its path in the code folder instead (`RobotIcon::import(path :
"core/robotIcon.svg", version : "")`); `fs push` uploads it to a new tab, and replaces the path with the tab's
element id, as it does for studios.

Images are tracked like studios (see fs_cli/workspace.py): by element id, in fs-studios.json's "images", with what
each looked like when it was last synced in .fs-state.json, and with the same statuses. So the repo is the source of
truth: `fs push` uploads images which changed locally (and updates the versions of their imports), and `fs pull`
downloads images which changed in Onshape, placing new ones beside the files which import them.
"""

from __future__ import annotations

import collections
import dataclasses
import pathlib
import re
from concurrent import futures

from fs_cli import git
from fs_cli.remote import RemoteImage, image_type, safe_file_name
from fs_cli.state import StudioState, image_hash, retarget_imports
from fs_cli.workspace import Status, Targets, UsageError, Workspace

MAX_WORKERS = 8


@dataclasses.dataclass
class Image:
    """An image, as it exists locally and/or in Onshape.

    Attributes:
        path: The local path relative to the code folder (using /), whether or not it exists.
        old_element_id: The element id of its tab before the tab was deleted in Onshape, whose imports move to the
            tab `fs push` recreates.
    """

    path: str
    file: pathlib.Path
    remote: RemoteImage | None
    saved: StudioState | None
    local_data: bytes | None
    status: Status
    deleted_in_onshape: bool = False
    old_element_id: str | None = None
    remote_data: bytes | None = None
    # The path it was synced with, if its file has been renamed or moved since
    renamed_from: str | None = None
    # Images are never released; this lets code shared with studios ask
    released: bool = False
    # False for images only in Onshape, which are placed beside their importers once those are pulled (see `pull`)
    located: bool = True

    @property
    def name(self) -> str:
        return self.remote.name if self.remote else pathlib.PurePosixPath(self.path).name


def is_image_target(target: str) -> bool:
    return image_type(target) is not None


class Images:
    def __init__(self, workspace: Workspace) -> None:
        self.workspace = workspace
        self.config = workspace.config
        self.state = workspace.state
        self.remote = workspace.remote
        self.instance = workspace.instance

    # Scanning

    def scan(self, targets: Targets | None = None, discover: bool = True) -> list[Image]:
        """Loads and classifies every image (or just those in targets). Takes 1 call, plus downloading any which
        changed in Onshape. Unless discover is set, the call is saved when there are no images (since pushing only
        needs the images which exist locally or are tracked), so images only in Onshape aren't found."""
        if not discover and not self.state.images and not self._local_paths():
            return []
        images = self._match(self.remote.list_images(self.instance))
        if targets is not None:
            selected = [image for image in images if _targeted(targets, image)]
            for target in sorted(targets.paths | targets.names):
                if is_image_target(target) and not any(_matches(target, image) for image in images):
                    raise UsageError(f'No image matches "{target}".')
            images = selected
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(self._classify, images))
        for image in images:
            if image.status == Status.IN_SYNC:
                assert image.remote and image.local_data is not None
                self._record(image, image.local_data, image.remote.microversion_id)
        return sorted(images, key=lambda image: image.path)

    def _match(self, remote_images: list[RemoteImage]) -> list[Image]:
        """Pairs image tabs with local files, as `Workspace._match` does for studios: by the file recorded in
        fs-studios.json, following it if it's moved (to a file with the contents last synced), or else by name."""
        saved = self.state.images
        remote_ids = {remote.element_id for remote in remote_images}
        code_dir = self.config.code_dir
        local = {path: (code_dir / path).read_bytes() for path in self._local_paths()}
        # The files of images whose tabs were deleted in Onshape, and their old element ids. They stay recorded
        # until `push` recreates them, so their importers can be pointed at their new tabs.
        deleted_in_onshape = {}
        for element_id in list(saved):
            if element_id not in remote_ids:
                if saved[element_id].file in local:
                    deleted_in_onshape[saved[element_id].file] = element_id
                else:
                    del saved[element_id]  # Deleted on both sides
        images: list[Image] = []
        claimed: set[str] = set()

        def add(path: str, remote: RemoteImage | None, **kwargs) -> Image:
            claimed.add(path)
            image = Image(
                path,
                code_dir / path,
                remote,
                saved.get(remote.element_id) if remote else None,
                local.get(path),
                Status.IN_SYNC,
                **kwargs,
            )
            images.append(image)
            return image

        unmatched = []
        for remote in remote_images:
            entry = saved.get(remote.element_id)
            if entry and entry.file in local and entry.file not in claimed:
                add(entry.file, remote)
            else:
                unmatched.append(remote)

        mapped = {entry.file for entry in saved.values()}
        claimed.update(deleted_in_onshape)
        by_name = collections.defaultdict(list)
        for path in local:
            if path not in claimed and path not in mapped:
                by_name[pathlib.PurePosixPath(path).name].append(path)
        for remote in unmatched:
            entry = saved.get(remote.element_id)
            if entry:
                # Moved locally: an untracked file holds the contents last synced
                moved = [
                    path
                    for path in local
                    if path not in claimed and path not in mapped and image_hash(local[path]) == entry.hash
                ]
                if len(moved) == 1:
                    add(moved[0], remote, renamed_from=entry.file)
                    entry.file = moved[0]
                else:
                    add(entry.file, remote)  # Deleted locally
                continue
            candidates = [path for path in by_name.get(file_name_for(remote), []) if path not in claimed]
            if len(candidates) == 1:
                add(candidates[0], remote)
            else:
                add(self._place(remote, set(local) | claimed), remote, located=False)

        for path, element_id in deleted_in_onshape.items():
            add(path, None, deleted_in_onshape=True, old_element_id=element_id)
        for path in sorted(local):
            if path not in claimed:
                add(path, None)
        return images

    def _local_paths(self) -> list[str]:
        """The images in the code folder, relative to it."""
        code_dir = self.config.code_dir
        if not code_dir.is_dir():
            return []
        return sorted(
            path.relative_to(code_dir).as_posix()
            for path in code_dir.rglob("*")
            if path.is_file() and image_type(path.name)
        )

    def _place(self, remote: RemoteImage, taken: set[str]) -> str:
        """Where an image new to the repo is pulled to: beside the first file which imports it, or else in the top
        level folder."""
        importers = self.workspace.importers(remote.element_id)
        folder = pathlib.PurePosixPath(importers[0]).parent if importers else pathlib.PurePosixPath()
        path = (folder / file_name_for(remote)).as_posix()
        if path in taken:
            stem, suffix = path.rsplit(".", 1)
            path = f"{stem} ({remote.element_id}).{suffix}"
        return path

    def _classify(self, image: Image) -> None:
        image.status = self._compute_status(image)

    def _compute_status(self, image: Image) -> Status:
        remote, saved, local = image.remote, image.saved, image.local_data
        if remote is None:
            return Status.DELETED_IN_ONSHAPE if image.deleted_in_onshape else Status.LOCAL_ONLY
        if local is None:
            if not saved:
                return Status.REMOTE_ONLY
            if saved.microversion_id != remote.microversion_id and image_hash(self.fetch(image)) != saved.hash:
                return Status.DELETE_CONFLICT
            return Status.DELETED_LOCALLY
        local_hash = image_hash(local)
        if saved and saved.hash and saved.microversion_id == remote.microversion_id:
            # Untouched in Onshape since it was last synced; no need to download it
            remote_hash = saved.hash
        else:
            remote_hash = image_hash(self.fetch(image))
        if local_hash == remote_hash:
            return Status.IN_SYNC
        if (saved and saved.hash == remote_hash) or remote_hash in self._history(image):
            return Status.LOCAL_CHANGES
        if saved and saved.hash == local_hash:
            return Status.REMOTE_CHANGES
        return Status.CONFLICT

    def _history(self, image: Image) -> set[str]:
        if not self.workspace.use_git:
            return set()
        return git.committed_image_hashes(self.config.root, image.file)

    def fetch(self, image: Image) -> bytes:
        """Returns (and caches) the image's file in Onshape."""
        assert image.remote
        if image.remote_data is None:
            image.remote_data = self.remote.download_image(self.instance, image.remote.element_id)
        return image.remote_data

    def _record(self, image: Image, data: bytes, microversion_id: str) -> None:
        assert image.remote
        self.state.images[image.remote.element_id] = StudioState(image.path, image_hash(data), microversion_id)

    # Actions

    def push(self, images: list[Image]) -> dict[str, tuple[str, str]]:
        """Uploads images to their tabs, creating any which don't exist yet (at the top level of the document, named
        after their files).

        Returns the (element id, version) importers of each pushed image should now import, by the element id they
        import: its old one, for images whose tabs were deleted in Onshape.
        """

        def upload(image: Image) -> None:
            assert image.local_data is not None
            if image.remote is None:
                name = pathlib.PurePosixPath(image.path).name
                image.remote = self.remote.upload_image(self.instance, name, image.local_data)
            else:
                image.remote = self.remote.update_image(
                    self.instance, image.remote.element_id, image.remote.name, image.local_data
                )

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(upload, images))
        # The microversions uploads leave them at, to import
        current = {remote.element_id: remote for remote in self.remote.list_images(self.instance)}
        imports = {}
        for image in images:
            assert image.remote and image.local_data is not None
            image.remote = current.get(image.remote.element_id, image.remote)
            self._record(image, image.local_data, image.remote.microversion_id)
            version = (image.remote.element_id, image.remote.microversion_id)
            imports[image.old_element_id or image.remote.element_id] = version
            if image.old_element_id:
                self.state.images.pop(image.old_element_id, None)
        return imports

    def pull(self, images: list[Image]) -> None:
        """Writes the Onshape files of images to their local files. Pull studios first, so images new to the repo
        are placed beside the studios which import them."""
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(self.fetch, images))
        taken = set(self._local_paths())
        for image in images:
            if not image.located:
                assert image.remote
                image.path = self._place(image.remote, taken)
                image.file = self.config.code_dir / image.path
                image.located = True
            taken.add(image.path)
        for image in images:
            assert image.remote and image.remote_data is not None
            image.file.parent.mkdir(parents=True, exist_ok=True)
            image.file.write_bytes(image.remote_data)
            image.local_data = image.remote_data
            self._record(image, image.remote_data, image.remote.microversion_id)

    def delete(self, images: list[Image]) -> None:
        """Deletes the tabs of images whose files were deleted locally."""

        def delete(image: Image) -> None:
            assert image.remote
            self.remote.delete(self.instance, image.remote.element_id)

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(delete, images))
        for image in images:
            assert image.remote
            self.state.images.pop(image.remote.element_id, None)

    def targets(self) -> dict[str, tuple[str, str]]:
        """The (element id, version) of each synced image, by its path, to resolve imports by path."""
        return {entry.file: (element_id, entry.microversion_id) for element_id, entry in self.state.images.items()}

    def retarget(self, imports: dict[str, tuple[str, str]]) -> list[str]:
        """Points the imports of images in local files at the (element id, version) in imports, by the element id
        they import (see `push`). Makes no API calls. Returns the files changed, relative to the code folder."""
        if not imports:
            return []
        pattern = re.compile("|".join(re.escape(f'"{element_id}"') for element_id in imports))
        changed = []
        code_dir = self.config.code_dir
        for path in sorted(code_dir.rglob("*.fs")):
            with path.open(newline="") as file:
                code = file.read()
            if not pattern.search(code):
                continue
            new_code = retarget_imports(code, imports)
            if new_code != code:
                path.write_text(new_code, newline="")
                changed.append(path.relative_to(code_dir).as_posix())
        return changed


def file_name_for(remote: RemoteImage) -> str:
    """The local file name used for an image tab: its name, with an extension for its type if it has none."""
    name = safe_file_name(remote.name)
    return name if image_type(name) else name + remote.extension


def _matches(target: str, image: Image) -> bool:
    return target in (image.path, image.name, pathlib.PurePosixPath(image.path).name)


def _targeted(targets: Targets, image: Image) -> bool:
    if any(folder == "" or image.path.startswith(folder + "/") for folder in targets.folders):
        return True
    return any(_matches(target, image) for target in targets.paths | targets.names)
