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
class Library:
    """A library document, which the FeatureScripts only its parts use are released to instead of the frontend document.

    Attributes:
        name: Its key in [tool.fs.libraries], which messages name it by.
        document_id: The library document; releases go to its default workspace.
        folders: The folders in the code folder whose FeatureScripts it holds.
    """

    name: str
    document_id: str
    folders: tuple[str, ...]

    def holds(self, path: str) -> bool:
        """Whether the FeatureScript at `path` (relative to the code folder) is released to it."""
        return any(path.startswith(folder + "/") for folder in self.folders)


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
        libraries: Library documents, which their own FeatureScripts are released to instead.
        github: The repo's GitHub url, which released FeatureScripts link to as their source.
    """

    root: pathlib.Path
    code_dir: pathlib.Path
    std_dir: pathlib.Path
    backend: InstancePath
    frontend: InstancePath | None = None
    frontend_beta: InstancePath | None = None
    libraries: tuple[Library, ...] = ()
    github: str | None = None

    def library_for(self, path: str) -> Library | None:
        """The library a FeatureScript is released to; None for the frontend document."""
        return next((library for library in self.libraries if library.holds(path)), None)

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
        libraries=tuple(_library(name, entry) for name, entry in data.get("libraries", {}).items()),
        github=data["github"].rstrip("/") if "github" in data else None,
    )


_DOCUMENT_URL = re.compile(r"/documents/([0-9a-f]{24})(?:/|$)")


def _library(name: str, entry: dict) -> Library:
    match = _DOCUMENT_URL.search(str(entry.get("document", "")))
    if match is None:
        raise ConfigError(f"[tool.fs.libraries.{name}] document must be an Onshape document's url.")
    folders = entry.get("folders")
    if not folders or not all(isinstance(folder, str) and folder for folder in folders):
        raise ConfigError(f"[tool.fs.libraries.{name}] folders must list the folders it holds.")
    return Library(name, match.group(1), tuple(folder.strip("/") for folder in folders))


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
