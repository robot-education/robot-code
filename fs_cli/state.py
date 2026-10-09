"""Bookkeeping about the Feature Studios in the backend document.

Which file each Feature Studio is synced with lives in fs-studios.json, which is checked in, so
every clone (and `fs check`, which never calls Onshape) can resolve imports by element id. It also
lists the studios which are released (see `load_released`), and which file each image tab (see
fs_cli/images.py) is synced with.

What each studio looked like the last time it was synced lives in .fs-state.json, which is not
checked in. It lets the CLI tell apart "I changed this file locally" from "someone changed this
Feature Studio in Onshape"; when it is missing (e.g. on a fresh clone) the CLI falls back to
comparing against git history.
"""

from __future__ import annotations

import contextlib
import dataclasses
import fcntl
import hashlib
import json
import pathlib
import re
import sys
from collections.abc import Iterator

STATE_VERSION = 4
STUDIOS_VERSION = 1
# Before version 4, the state also held each studio's file
_STATE_VERSION_WITH_FILES = 3

# An import of another tab in the same document: import(path : "<element id>", version : "<id>")
_SAME_DOCUMENT_IMPORT = re.compile(
    r'(\bimport\s*\(\s*path\s*:\s*"([0-9a-f]{24})"\s*,\s*version\s*:\s*")([0-9a-f]{24})(")'
)


def import_versions(code: str) -> dict[str, str]:
    """Maps the element ids of same-document imports to their versions."""
    return {match[2]: match[3] for match in _SAME_DOCUMENT_IMPORT.finditer(code)}


def apply_import_versions(code: str, versions: dict[str, str]) -> str:
    """Replaces the versions of same-document imports with those in versions."""
    return _SAME_DOCUMENT_IMPORT.sub(
        lambda match: match[1] + versions.get(match[2], match[3]) + match[4], code
    )


# An import of another studio or an image by its path in the code folder, e.g.
# `import(path : "core/utils.fs", version : "");` or `Icon::import(path : "core/robotIcon.svg", version : "");`,
# which `fs push` resolves to an element id
_PATH_IMPORT = re.compile(
    r'(\bimport\s*\(\s*path\s*:\s*")((?!onshape/)[^"]+\.(?:fs|svg|png))("\s*,\s*version\s*:\s*")([^"]*)(")'
)


def path_imports(code: str) -> list[str]:
    """The code folder paths imported by path, which `fs push` resolves to element ids."""
    return [match[2] for match in _PATH_IMPORT.finditer(code)]


def resolve_path_imports(code: str, targets: dict[str, tuple[str, str]]) -> str:
    """Replaces imports by path with imports by element id.

    Args:
        targets: The (element id, version) of each imported path.
    """
    return _PATH_IMPORT.sub(
        lambda match: match[1] + targets[match[2]][0] + match[3] + targets[match[2]][1] + match[5],
        code,
    )


def retarget_imports(code: str, targets: dict[str, tuple[str, str]]) -> str:
    """Replaces same-document imports of element ids in targets with imports of their (element id, version)."""
    return _SAME_DOCUMENT_IMPORT.sub(
        lambda match: (
            match[1].replace(match[2], targets[match[2]][0]) + targets[match[2]][1] + match[4]
            if match[2] in targets
            else match[0]
        ),
        code,
    )


def content_hash(code: str) -> str:
    """Hashes Feature Studio contents for comparison.

    Line endings and the versions of same-document imports are ignored. Onshape manages those
    versions itself (they may change when the imported tab changes), so a difference in them
    alone isn't an edit to either side.
    """
    normalized = _SAME_DOCUMENT_IMPORT.sub(r"\1\4", code.replace("\r\n", "\n"))
    return hashlib.sha256(normalized.encode()).hexdigest()


@dataclasses.dataclass
class StudioState:
    """A synced Feature Studio.

    Attributes:
        file: The path of the local file, relative to the code folder (using /).
        hash: The content hash of the studio when it was last synced, or "" if it hasn't been
            synced on this machine.
        microversion_id: The element microversion in Onshape when it was last synced, or "".
    """

    file: str
    hash: str = ""
    microversion_id: str = ""


class State:
    def __init__(
        self,
        path: pathlib.Path,
        studios_path: pathlib.Path,
        studios: dict[str, StudioState],
        released: set[str] | None = None,
        images: dict[str, StudioState] | None = None,
    ) -> None:
        self.path = path
        self.studios_path = studios_path
        # element id -> state
        self.studios = studios
        # The element ids of released studios (see `load_released`)
        self.released = released if released is not None else set()
        # element id -> state, for image tabs; their hashes are of their bytes (see `image_hash`)
        self.images = images if images is not None else {}

    @classmethod
    def load(cls, path: pathlib.Path, studios_path: pathlib.Path) -> State:
        files = load_studio_files(studios_path)
        synced = _read_json(path)
        if synced.get("version") == _STATE_VERSION_WITH_FILES:
            for element_id, studio in synced.get("studios", {}).items():
                if isinstance(studio, dict) and "file" in studio:
                    files.setdefault(element_id, studio["file"])
        elif synced.get("version") != STATE_VERSION:
            synced = {}
        def states(files: dict[str, str], key: str) -> dict[str, StudioState]:
            result = {}
            for element_id, file in files.items():
                entry = synced.get(key, {}).get(element_id)
                entry = entry if isinstance(entry, dict) else {}
                result[element_id] = StudioState(file, entry.get("hash", ""), entry.get("microversion_id", ""))
            return result

        return cls(
            path,
            studios_path,
            states(files, "studios"),
            load_released(studios_path),
            states(load_image_files(studios_path), "images"),
        )

    def save(self) -> None:
        studios = sorted(self.studios.items())
        images = sorted(self.images.items())
        save_studio_files(
            self.studios_path,
            {element_id: studio.file for element_id, studio in studios},
            self.released,
            {element_id: image.file for element_id, image in images},
        )

        def synced(entries: list[tuple[str, StudioState]]) -> dict:
            return {
                element_id: {"hash": entry.hash, "microversion_id": entry.microversion_id}
                for element_id, entry in entries
                if entry.hash or entry.microversion_id
            }

        data = {"version": STATE_VERSION, "studios": synced(studios)}
        if images:
            data["images"] = synced(images)
        _write_json(self.path, data)


def migrate(path: pathlib.Path, studios_path: pathlib.Path) -> None:
    """Moves which file each studio is synced with out of a state file from before version 4, holding the state's
    lock if it does (see `state_lock`), so call it before taking the lock."""
    if _read_json(path).get("version") == _STATE_VERSION_WITH_FILES:
        with state_lock(studios_path):
            State.load(path, studios_path).save()


def load_studio_files(studios_path: pathlib.Path) -> dict[str, str]:
    """Maps element ids to files in the code folder, from fs-studios.json."""
    data = _read_json(studios_path)
    if data.get("version") != STUDIOS_VERSION:
        return {}
    return {
        element_id: file
        for element_id, file in data.get("studios", {}).items()
        if isinstance(file, str)
    }


def load_image_files(studios_path: pathlib.Path) -> dict[str, str]:
    """Maps the element ids of image tabs to files in the code folder, from fs-studios.json."""
    data = _read_json(studios_path)
    if data.get("version") != STUDIOS_VERSION:
        return {}
    return {
        element_id: file
        for element_id, file in data.get("images", {}).items()
        if isinstance(file, str)
    }


def image_hash(data: bytes) -> str:
    """Hashes an image's bytes for comparison."""
    return hashlib.sha256(data).hexdigest()


def load_released(studios_path: pathlib.Path) -> set[str]:
    """The element ids of the released studios, from fs-studios.json.

    A released studio defines a feature in the frontend document, which re-exports a version of it. Its tab
    can't be deleted, recreated, or renamed: Part Studios using the feature only update to newer versions of it
    if it has the same element id, and `fs release` finds its frontend studio by its name. Retire it with
    `fs deprecate` instead.
    """
    data = _read_json(studios_path)
    if data.get("version") != STUDIOS_VERSION:
        return set()
    return {element_id for element_id in data.get("released", []) if isinstance(element_id, str)}


def save_studio_files(
    studios_path: pathlib.Path,
    files: dict[str, str],
    released: set[str] | None = None,
    images: dict[str, str] | None = None,
) -> None:
    """Writes fs-studios.json (see `load_studio_files`, `load_released`, and `load_image_files`), keeping its
    released studios and images unless they're given."""
    if released is None:
        released = load_released(studios_path)
    if images is None:
        images = load_image_files(studios_path)
    data: dict = {
        "version": STUDIOS_VERSION,
        "studios": dict(sorted(files.items())),
        "released": sorted(released),
    }
    if images:
        data["images"] = dict(sorted(images.items()))
    _write_json(studios_path, data)


@contextlib.contextmanager
def state_lock(studios_path: pathlib.Path) -> Iterator[None]:
    """Holds the lock on fs-studios.json and .fs-state.json (a `.fs.lock` file beside fs-studios.json) while reading
    and changing them.

    Commands load the state when they start and save it when they finish, so two at once (from two terminals, or two
    agents) would each write back what they loaded, losing the other's changes, like new studios' element ids. With
    the lock, the second waits for the first.
    """
    path = studios_path.with_name(".fs.lock")
    with path.open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print("fs: waiting for another fs command (or the editor) to finish...", file=sys.stderr)
            fcntl.flock(lock, fcntl.LOCK_EX)
        try:
            yield
        finally:
            fcntl.flock(lock, fcntl.LOCK_UN)


def _read_json(path: pathlib.Path) -> dict:
    if not path.is_file():
        return {}
    try:
        data = json.loads(path.read_text())
    except (OSError, json.JSONDecodeError):
        print(f"Warning: ignoring unreadable file {path}.")
        return {}
    return data if isinstance(data, dict) else {}


def _write_json(path: pathlib.Path, data: dict) -> None:
    text = json.dumps(data, indent=2) + "\n"
    if not path.is_file() or path.read_text() != text:
        path.write_text(text)
