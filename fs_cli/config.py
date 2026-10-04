"""Loads featurescripts.toml, which maps Onshape documents to local folders."""

from __future__ import annotations

import dataclasses
import pathlib
import tomllib

from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import InstancePath, url_to_instance_path

CONFIG_FILE = "featurescripts.toml"
STATE_FILE = ".fs-state.json"
DEFAULT_CODE_DIR = "featurescripts"


class ConfigError(Exception):
    pass


@dataclasses.dataclass
class DocumentConfig:
    """A single Onshape document workspace whose Feature Studios are mirrored into a local folder.

    Attributes:
        name: The name of the document, as used on the command line.
        url: The Onshape url of the workspace.
        path: The folder holding the document's .fs files.
    """

    name: str
    url: str
    path: pathlib.Path

    @property
    def instance(self) -> InstancePath:
        return url_to_instance_path(self.url)


@dataclasses.dataclass
class Config:
    root: pathlib.Path
    documents: list[DocumentConfig]

    @property
    def state_path(self) -> pathlib.Path:
        return self.root / STATE_FILE

    def document(self, name: str) -> DocumentConfig | None:
        return next((doc for doc in self.documents if doc.name == name), None)

    def document_for_path(self, path: pathlib.Path) -> DocumentConfig | None:
        """Returns the document whose folder contains path, if any."""
        path = path.resolve()
        for doc in self.documents:
            if path == doc.path.resolve() or doc.path.resolve() in path.parents:
                return doc
        return None


def find_root(start: pathlib.Path | None = None) -> pathlib.Path:
    """Returns the closest directory at or above start containing featurescripts.toml."""
    start = (start or pathlib.Path.cwd()).resolve()
    for directory in [start, *start.parents]:
        if (directory / CONFIG_FILE).is_file():
            return directory
    raise ConfigError(
        f"Could not find {CONFIG_FILE} in {start} or any of its parent directories."
    )


def load_config(root: pathlib.Path | None = None) -> Config:
    root = find_root(root)
    with (root / CONFIG_FILE).open("rb") as file:
        try:
            data = tomllib.load(file)
        except tomllib.TOMLDecodeError as error:
            raise ConfigError(f"Failed to parse {CONFIG_FILE}: {error}") from error
    return parse_config(root, data)


def parse_config(root: pathlib.Path, data: dict) -> Config:
    code_dir = data.get("code_dir", DEFAULT_CODE_DIR)
    documents = []
    for name, entry in data.get("documents", {}).items():
        if not isinstance(entry, dict) or "url" not in entry:
            raise ConfigError(f'Document "{name}" must define a url.')
        url = entry["url"]
        try:
            instance = url_to_instance_path(url)
        except (IndexError, ValueError) as error:
            raise ConfigError(f'Document "{name}" has an invalid url: {url}') from error
        if instance.instance_type != InstanceType.WORKSPACE:
            raise ConfigError(
                f'Document "{name}" must point to a workspace (a url containing /w/), since versions are read-only.'
            )
        path = root / entry.get("path", f"{code_dir}/{name}")
        documents.append(DocumentConfig(name, url, path))
    return Config(root, documents)
