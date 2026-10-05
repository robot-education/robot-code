"""Bookkeeping about the Feature Studios in the backend document.

Which file each Feature Studio is synced with lives in fs-studios.json, which is checked in, so
every clone (and `fs check`, which never calls Onshape) can resolve imports by element id. It also
lists the studios which are released (see `load_released`).

What each studio looked like the last time it was synced lives in .fs-state.json, which is not
checked in. It lets the CLI tell apart "I changed this file locally" from "someone changed this
Feature Studio in Onshape"; when it is missing (e.g. on a fresh clone) the CLI falls back to
comparing against git history.
"""

from __future__ import annotations

import dataclasses
import hashlib
import json
import pathlib
import re

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


# An import of another studio by its path in the code folder, e.g.
# `import(path : "core/utils.fs", version : "");`, which `fs push` resolves to an element id
_PATH_IMPORT = re.compile(
    r'(\bimport\s*\(\s*path\s*:\s*")((?!onshape/)[^"]+\.fs)("\s*,\s*version\s*:\s*")([^"]*)(")'
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
    ) -> None:
        self.path = path
        self.studios_path = studios_path
        # element id -> state
        self.studios = studios
        # The element ids of released studios (see `load_released`)
        self.released = released if released is not None else set()

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
        studios = {}
        for element_id, file in files.items():
            studio = synced.get("studios", {}).get(element_id)
            studio = studio if isinstance(studio, dict) else {}
            studios[element_id] = StudioState(
                file, studio.get("hash", ""), studio.get("microversion_id", "")
            )
        return cls(path, studios_path, studios, load_released(studios_path))

    def save(self) -> None:
        studios = sorted(self.studios.items())
        save_studio_files(
            self.studios_path,
            {element_id: studio.file for element_id, studio in studios},
            self.released,
        )
        _write_json(
            self.path,
            {
                "version": STATE_VERSION,
                "studios": {
                    element_id: {"hash": studio.hash, "microversion_id": studio.microversion_id}
                    for element_id, studio in studios
                    if studio.hash or studio.microversion_id
                },
            },
        )


def migrate(path: pathlib.Path, studios_path: pathlib.Path) -> None:
    """Moves which file each studio is synced with out of a state file from before version 4."""
    if _read_json(path).get("version") == _STATE_VERSION_WITH_FILES:
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
    studios_path: pathlib.Path, files: dict[str, str], released: set[str] | None = None
) -> None:
    """Writes fs-studios.json (see `load_studio_files` and `load_released`), keeping its released studios unless
    `released` is given."""
    if released is None:
        released = load_released(studios_path)
    _write_json(
        studios_path,
        {
            "version": STUDIOS_VERSION,
            "studios": dict(sorted(files.items())),
            "released": sorted(released),
        },
    )


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
