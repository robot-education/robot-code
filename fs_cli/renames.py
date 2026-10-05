"""Following files and folders renamed or moved in the code folder.

A Feature Studio is synced with the file fs-studios.json maps its element id to. When that file is
renamed, the map has to follow it, or the next push would delete the tab and create a new one, which
breaks every document importing it (imports are by element id). Imports by path
(`import(path : "core/utils.fs", ...)`) have to follow it too.

Renames are recorded as they happen by the language server (from the editor's file operations) and
`fs mv`; for anything else, `fs` pairs a missing file with the new file most like it (see
`most_similar`).
"""

from __future__ import annotations

import difflib
import pathlib
import re
from typing import Iterable, Mapping

from fs_cli.state import load_image_files, load_studio_files, save_studio_files

# An import by path in the code folder: group 2 is the path
_PATH_IMPORT = re.compile(
    r'(\bimport\s*\(\s*path\s*:\s*")((?!onshape/)[^"]+\.(?:fs|svg|png))("\s*,\s*version\s*:\s*"[^"]*")'
)

# How alike a new file has to be to a missing one to be taken as it renamed, as in git
SIMILARITY = 0.5


def renamed(path: str, renames: Mapping[str, str]) -> str:
    """Where `path` (relative to the code folder, using /) is after `renames`, each of a file or a
    folder (from its old path to its new one)."""
    for old, new in renames.items():
        if path == old:
            return new
        if path.startswith(old + "/"):
            return new + path[len(old) :]
    return path


def rename_studio_files(studios_path: pathlib.Path, renames: Mapping[str, str]) -> list[str]:
    """Follows `renames` in fs-studios.json (for studios and images). Returns the files whose mapping changed."""
    files = load_studio_files(studios_path)
    images = load_image_files(studios_path)
    changed = [file for file in [*files.values(), *images.values()] if renamed(file, renames) != file]
    if changed:
        save_studio_files(
            studios_path,
            {element_id: renamed(file, renames) for element_id, file in files.items()},
            images={element_id: renamed(file, renames) for element_id, file in images.items()},
        )
    return changed


def path_import_edits(code: str, renames: Mapping[str, str]) -> list[tuple[int, int, str]]:
    """The (start, end, new path) of each import by path in `code` which `renames` moves."""
    edits = []
    for match in _PATH_IMPORT.finditer(code):
        new = renamed(match[2], renames)
        if new != match[2]:
            edits.append((match.start(2), match.end(2), new))
    return edits


def rename_path_imports(code: str, renames: Mapping[str, str]) -> str:
    for start, end, new in reversed(path_import_edits(code, renames)):
        code = code[:start] + new + code[end:]
    return code


def most_similar(code: str, candidates: Mapping[str, str]) -> str | None:
    """The candidate (by path) most like `code`, if it's alike enough to be `code` renamed and no
    other candidate is as alike."""
    old = code.splitlines()
    scores = sorted(
        ((difflib.SequenceMatcher(None, old, other.splitlines(), autojunk=False).ratio(), path) for path, other in candidates.items()),
        reverse=True,
    )
    if not scores or scores[0][0] < SIMILARITY:
        return None
    if len(scores) > 1 and scores[1][0] == scores[0][0]:
        return None
    return scores[0][1]


def relative_paths(code_dir: pathlib.Path, paths: Iterable[pathlib.Path]) -> list[str | None]:
    """Paths relative to the code folder (using /), or None for those outside it."""
    root = code_dir.resolve()
    result = []
    for path in paths:
        try:
            result.append(path.resolve().relative_to(root).as_posix())
        except ValueError:
            result.append(None)
    return result
