"""Loads the [tool.fs] table from the repo's pyproject.toml."""

from __future__ import annotations

import dataclasses
import pathlib
import re
import tomllib

from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import InstancePath, url_to_instance_path

CONFIG_FILE = "pyproject.toml"
STATE_FILE = ".fs-state.json"
STUDIOS_FILE = "fs-studios.json"


class ConfigError(Exception):
    pass


@dataclasses.dataclass
class PartSet:
    """A set of FeatureScript-backed parts, like FRCDesign's fasteners: Part Studios built from a definition, whose
    FeatureScript only they use (see docs/parts.md). Its FeatureScript is in the backend document like any other, but it's
    tried and released by `fs parts`, not `fs release`: into its main document, as Part Studios people insert.

    Attributes:
        name: Its key in [tool.fs.parts], which commands name it by, like `fasteners`.
        title: What its versions are named by, like `Fasteners` (`Fasteners - v1.2.0`).
        folder: The folder in the code folder holding its FeatureScript and definition.
        main_document_id: The FRCDesignLib document its releases go to.
    """

    name: str
    title: str
    folder: str
    main_document_id: str

    def holds(self, path: str) -> bool:
        """Whether the file at `path` (relative to the code folder) is the set's."""
        return path.startswith(self.folder + "/")


@dataclasses.dataclass
class Config:
    """
    Attributes:
        root: The repo root (the folder containing pyproject.toml).
        code_dir: The folder Feature Studios in the backend document are mirrored to.
        std_dir: The folder holding a copy of the Onshape std library.
        backend: The workspace holding the source of every FeatureScript.
        frontend: The public workspace released FeatureScripts are published to.
        frontend_beta: The workspace beta releases are published to.
        parts: Part sets, released by `fs parts` rather than `fs release`.
        github: The repo's GitHub url, which released FeatureScripts link to as their source.
    """

    root: pathlib.Path
    code_dir: pathlib.Path
    std_dir: pathlib.Path
    backend: InstancePath
    frontend: InstancePath | None = None
    frontend_beta: InstancePath | None = None
    parts: tuple[PartSet, ...] = ()
    github: str | None = None

    def find_part_set(self, name: str) -> PartSet:
        found = next((part_set for part_set in self.parts if part_set.name == name), None)
        if found is None:
            names = ", ".join(part_set.name for part_set in self.parts) or "none"
            raise ConfigError(f"No part set named {name} in [tool.fs.parts] (there's {names}).")
        return found

    def part_set_for(self, path: str) -> PartSet | None:
        """The part set holding a file (relative to the code folder); None for user-facing FeatureScript."""
        return next((part_set for part_set in self.parts if part_set.holds(path)), None)

    @property
    def state_path(self) -> pathlib.Path:
        """The local (untracked) record of when each Feature Studio was last synced."""
        return self.root / STATE_FILE

    @property
    def studios_path(self) -> pathlib.Path:
        """The checked in map of element ids to files in the code folder."""
        return self.root / STUDIOS_FILE


def find_root(start: pathlib.Path | None = None) -> pathlib.Path:
    """Returns the closest directory at or above start whose pyproject.toml has a [tool.fs] table."""
    start = (start or pathlib.Path.cwd()).resolve()
    for directory in [start, *start.parents]:
        path = directory / CONFIG_FILE
        if path.is_file() and "fs" in _read(path).get("tool", {}):
            return directory
    raise ConfigError(
        f"Could not find a {CONFIG_FILE} with a [tool.fs] table in {start} or any of its parents."
    )


def load_config(start: pathlib.Path | None = None) -> Config:
    root = find_root(start)
    return parse_config(root, _read(root / CONFIG_FILE)["tool"]["fs"])


def parse_config(root: pathlib.Path, data: dict) -> Config:
    if "backend" not in data:
        raise ConfigError("[tool.fs] must set backend to the backend document's url.")
    return Config(
        root=root,
        code_dir=root / data.get("path", "featurescripts"),
        std_dir=root / data.get("std", "std"),
        backend=_workspace(data, "backend"),
        frontend=_workspace(data, "frontend") if "frontend" in data else None,
        frontend_beta=(
            _workspace(data, "frontend_beta") if "frontend_beta" in data else None
        ),
        parts=tuple(_part_set(name, entry) for name, entry in data.get("parts", {}).items()),
        github=data["github"].rstrip("/") if "github" in data else None,
    )


_DOCUMENT_URL = re.compile(r"/documents/([0-9a-f]{24})(?:/|$)")


def _part_set(name: str, entry: dict) -> PartSet:
    where = f"[tool.fs.parts.{name}]"
    main = _DOCUMENT_URL.search(str(entry.get("main", "")))
    if main is None:
        raise ConfigError(f"{where} main must be its main document's url.")
    folder = entry.get("folder")
    if not isinstance(folder, str) or not folder.strip("/"):
        raise ConfigError(f"{where} folder must name the folder holding its FeatureScript.")
    title = entry.get("title")
    if not isinstance(title, str) or not title:
        raise ConfigError(f"{where} title must name its versions, like Fasteners.")
    return PartSet(name, title, folder.strip("/"), main.group(1))


def _workspace(data: dict, key: str) -> InstancePath:
    url = data[key]
    try:
        instance = url_to_instance_path(url)
    except (IndexError, ValueError, TypeError) as error:
        raise ConfigError(
            f"[tool.fs] {key} is not a valid Onshape url: {url}"
        ) from error
    if instance.instance_type != InstanceType.WORKSPACE:
        raise ConfigError(
            f"[tool.fs] {key} must link to a workspace (a url containing /w/), not a version."
        )
    return instance


def _read(path: pathlib.Path) -> dict:
    with path.open("rb") as file:
        try:
            return tomllib.load(file)
        except tomllib.TOMLDecodeError as error:
            raise ConfigError(f"Failed to parse {path}: {error}") from error
