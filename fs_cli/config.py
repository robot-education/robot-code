"""Loads the [tool.fs] table from the repo's pyproject.toml."""

from __future__ import annotations

import dataclasses
import pathlib
import tomllib

from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import InstancePath, url_to_instance_path

CONFIG_FILE = "pyproject.toml"
STATE_FILE = ".fs-state.json"


class ConfigError(Exception):
    pass


@dataclasses.dataclass
class Config:
    """
    Attributes:
        root: The repo root (the folder containing pyproject.toml).
        code_dir: The folder Feature Studios in the backend document are mirrored to.
        backend: The workspace holding the source of every FeatureScript.
        frontend: The public workspace released FeatureScripts are published to.
        frontend_beta: The workspace beta releases are published to.
    """

    root: pathlib.Path
    code_dir: pathlib.Path
    backend: InstancePath
    frontend: InstancePath | None = None
    frontend_beta: InstancePath | None = None

    @property
    def state_path(self) -> pathlib.Path:
        return self.root / STATE_FILE


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
        backend=_workspace(data, "backend"),
        frontend=_workspace(data, "frontend") if "frontend" in data else None,
        frontend_beta=(
            _workspace(data, "frontend_beta") if "frontend_beta" in data else None
        ),
    )


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
