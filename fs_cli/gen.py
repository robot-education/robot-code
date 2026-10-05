"""Generates Feature Studios (the `.gen.fs` files) from Python definitions.

A definition is a Python file in the code folder, such as belt/robotBeltTables.py, which sets
CONTENTS to a list of items: lookup tables and enums (fs_cli.tables), sketch profiles
(fs_cli.sketches), and Imports of other studios they use. `fs gen` writes them to the .gen.fs file
beside it (belt/robotBeltTables.gen.fs), which `fs push` then pushes like any other file.
"""

from __future__ import annotations

import dataclasses
import pathlib
import re
import runpy
from typing import Callable, Mapping, Protocol, Sequence

from fs_cli.state import apply_import_versions, import_versions

GENERATED_SUFFIX = ".gen.fs"

# Generated imports note the file they import, so they can be regenerated without the sync state
_ANNOTATED_IMPORT = re.compile(
    r'import\(path : "([0-9a-f]{24})", version : "([0-9a-f]{24})"\); // (\S+)'
)


class GenerateError(Exception):
    pass


Resolver = Callable[[str], tuple[str, str]]


class Item(Protocol):
    def render(self) -> str:
        """Returns the item's FeatureScript code."""
        ...


@dataclasses.dataclass
class Import:
    """An import of another Feature Studio in the code folder, e.g. Import("core/sketchData.fs").

    Studios are imported by element id. One which isn't synced on this machine (or in Onshape yet) is imported by
    its path instead, which `fs push` resolves.
    """

    path: str
    export: bool = False

    def render_with(self, resolve: Resolver) -> str:
        element_id, version = resolve(self.path)
        export = "export " if self.export else ""
        return f'{export}import(path : "{element_id}", version : "{version}"); // {self.path}\n'


@dataclasses.dataclass
class Constant:
    """An exported constant set to a FeatureScript expression."""

    name: str
    expression: str

    def render(self) -> str:
        return f"export const {self.name} = {self.expression};\n"


@dataclasses.dataclass
class Code:
    """FeatureScript code, as written (e.g. a predicate built from a definition's data)."""

    code: str

    def render(self) -> str:
        return self.code.rstrip("\n") + "\n"


def render(
    contents: Sequence[Item | Import],
    version: str,
    source_name: str,
    resolve: Resolver | None = None,
) -> str:
    """Returns the code of a Feature Studio holding contents."""
    imports = [item for item in contents if isinstance(item, Import)]
    if imports and resolve is None:
        raise GenerateError(f"{source_name} has imports, which need a resolver.")
    header = (
        f"FeatureScript {version};\n"
        f'import(path : "onshape/std/common.fs", version : "{version}.0");\n'
        + "".join(item.render_with(resolve) for item in imports)  # type: ignore[arg-type]
        + f"\n/* Generated from {source_name} by `fs gen` -- DO NOT EDIT */\n"
    )
    items = [item.render() for item in contents if not isinstance(item, Import)]
    return "\n".join([header, *items])


@dataclasses.dataclass
class Generated:
    source: pathlib.Path
    output: pathlib.Path
    code: str
    changed: bool


def generate(
    code_dir: pathlib.Path,
    default_version: str | None,
    synced: Mapping[str, tuple[str, str]] | None = None,
) -> list[Generated]:
    """Renders every definition in code_dir. Doesn't write anything.

    Args:
        default_version: The FeatureScript version of new outputs. Existing outputs keep theirs
            (`fs update-std` changes it).
        synced: The (element id, microversion) of each studio in Onshape, by path in the code
            folder, used to resolve Imports.
    """
    results = []
    for source in sorted(code_dir.rglob("*.py")):
        contents = runpy.run_path(str(source)).get("CONTENTS")
        if contents is None:
            raise GenerateError(f"{source} doesn't define CONTENTS.")
        output = source.with_name(source.stem + GENERATED_SUFFIX)
        old = output.read_text() if output.is_file() else None
        version = (_version(old) if old else None) or default_version
        if version is None:
            raise GenerateError(
                f"Can't tell which FeatureScript version {output.name} should use; run `fs pull-std` first."
            )
        code = render(contents, version, source.name, _resolver(source, old, synced or {}))
        if old:
            # Onshape manages the versions of imports, so keep its
            code = apply_import_versions(code, import_versions(old))
        results.append(Generated(source, output, code, code != old))
    return results


def _resolver(
    source: pathlib.Path, old: str | None, synced: Mapping[str, tuple[str, str]]
) -> Resolver:
    previous = {match[3]: (match[1], match[2]) for match in _ANNOTATED_IMPORT.finditer(old or "")}

    def resolve(path: str) -> tuple[str, str]:
        if path in synced:
            return synced[path]
        if path in previous:
            return previous[path]
        if not (source.parent / path).is_file() and not _code_dir_has(source, path):
            raise GenerateError(f"{source.name} imports {path}, which doesn't exist.")
        # By path, which `fs push` resolves
        return path, ""

    return resolve


def _code_dir_has(source: pathlib.Path, path: str) -> bool:
    """Whether a folder containing source (e.g. the code folder) has the file at path."""
    return any((folder / path).is_file() for folder in source.parents)


def _version(code: str) -> str | None:
    match = re.match(r"\s*FeatureScript\s+(\d+)\s*;", code)
    return match.group(1) if match else None
