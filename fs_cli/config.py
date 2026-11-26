"""Loads the [tool.fs] table from the repo's pyproject.toml.

fs works with a few named Onshape documents (`[tool.fs.documents.<name>]`), each in one of three roles (see `Role`),
on cad.onshape.com or in an Enterprise, reached with its own keys (see docs/fs-cli.md#documents).
"""

from __future__ import annotations

import dataclasses
import enum
import pathlib
import re
import tomllib

from onshape_api.paths.paths import InstancePath

CONFIG_FILE = "pyproject.toml"
STATE_FILE = ".fs-state.json"
STUDIOS_FILE = "fs-studios.json"


class ConfigError(Exception):
    pass


class Role(enum.StrEnum):
    """What a document is for."""

    # The source code of FeatureScripts and part sets, to try them out in. The backend (`[tool.fs] backend`) is synced
    # with the code folder and holds every release's version; another holds copies of what's pushed to it by name.
    BACKEND = "backend"
    # For loading into a test FRCDesignApp: releases to it aren't numbered, and are made from any branch.
    LIBRARY = "library"
    # User facing: numbered releases, made on the main branch, are what people use.
    FRONTEND = "frontend"


@dataclasses.dataclass(frozen=True)
class Document:
    """An Onshape document fs reads or writes: `[tool.fs.documents.<name>]`.

    Attributes:
        name: Its key in [tool.fs.documents], which config and commands (`--to`) name it by.
        base_url: The Onshape it's on: https://cad.onshape.com, or an Enterprise's own domain.
        workspace_id: The workspace its url names; None to use its default workspace (looked up with a call).
        keys: Where its API keys are: the prefix of `<keys>_API_ACCESS_KEY` and `<keys>_API_SECRET_KEY`, like an
            Enterprise's, with which every request is signed; None for the usual keys.
    """

    name: str
    role: Role
    base_url: str
    document_id: str
    workspace_id: str | None = None
    keys: str | None = None

    @property
    def url(self) -> str:
        url = f"{self.base_url}/documents/{self.document_id}"
        return url + f"/w/{self.workspace_id}" if self.workspace_id else url

    @property
    def instance(self) -> InstancePath | None:
        """Its workspace, when its url names one."""
        return InstancePath(self.document_id, self.workspace_id) if self.workspace_id else None

    def describe(self) -> str:
        return f"{self.name} ({self.url})"


@dataclasses.dataclass
class PartSet:
    """A set of FeatureScript-backed parts, like FRCDesign's fasteners: Part Studios built from a definition, whose
    FeatureScript only they use (see docs/parts.md). Its FeatureScript is in the backend document like any other, but it's
    built by `fs build`, and released with its Part Studios, into its own frontend document.

    Attributes:
        name: Its key in [tool.fs.parts], which commands name it by, like `fasteners`.
        title: What its versions are named by, like `Fasteners` (`Fasteners - v1.2.0`).
        folder: The folder in the code folder holding its FeatureScript and definition.
        frontend: The document its releases go to (one of FRCDesignLib's or ConfigLib's); None until it has one, when it
            can only be built and released to a library.
        definition: The file in `folder` configuring its Part Studios, where the folder holds another set's too; every
            definition in the folder otherwise.
    """

    name: str
    title: str
    folder: str
    frontend: Document | None = None
    definition: str | None = None

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
        documents: Every document in [tool.fs.documents].
        backend_document: The backend synced with the code folder (`[tool.fs] backend`).
        frontend: The document FeatureScripts are released to (`[tool.fs] frontend`).
        parts: Part sets, each released with its Part Studios into its own frontend.
        github: The repo's GitHub url, which released FeatureScripts link to as their source.
        main_branch: The git branch releases to frontends run on, and only once it's what origin has (see
            docs/fs-cli.md#branches); None to release from any branch, unchecked.
        default_icon: The `.svg` FRCDesignApp shows for a released feature with no icon of its own.
    """

    root: pathlib.Path
    code_dir: pathlib.Path
    std_dir: pathlib.Path
    documents: tuple[Document, ...]
    backend_document: Document
    frontend: Document | None = None
    parts: tuple[PartSet, ...] = ()
    github: str | None = None
    main_branch: str | None = None
    default_icon: pathlib.Path | None = None

    @property
    def backend(self) -> InstancePath:
        """The backend's workspace."""
        assert self.backend_document.instance
        return self.backend_document.instance

    def find_document(self, name: str, *roles: Role) -> Document:
        """The document of that name, which must be in one of `roles`."""
        found = next((document for document in self.documents if document.name == name), None)
        listed = [document.name for document in self.documents if document.role in roles]
        kinds = " or ".join(f"{role} document" for role in roles)
        if found is None:
            raise ConfigError(f"No document named {name} in [tool.fs.documents] (its {kinds}s: {', '.join(listed) or 'none'}).")
        if found.role not in roles:
            raise ConfigError(f"{name} is a {found.role} document, not a {kinds} ({', '.join(listed) or 'none'}).")
        return found

    def find_part_set(self, name: str) -> PartSet:
        found = self.part_set(name)
        if found is None:
            names = ", ".join(part_set.name for part_set in self.parts) or "none"
            raise ConfigError(f"No part set named {name} in [tool.fs.parts] (there's {names}).")
        return found

    def part_set(self, name: str) -> PartSet | None:
        return next((part_set for part_set in self.parts if part_set.name == name), None)

    def part_set_for(self, path: str) -> PartSet | None:
        """The first part set holding a file (relative to the code folder); None for user-facing FeatureScript."""
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
    entries = data.get("documents", {})
    if not isinstance(entries, dict) or not all(isinstance(entry, dict) for entry in entries.values()):
        raise ConfigError("[tool.fs.documents] must hold a table for each document.")
    documents = tuple(_document(name, entry) for name, entry in entries.items())
    ids: dict[str, str] = {}
    for document in documents:
        other = ids.setdefault(document.document_id, document.name)
        if other != document.name:
            raise ConfigError(f"[tool.fs.documents] {other} and {document.name} are the same document.")
    config = Config(
        root=root,
        code_dir=root / data.get("path", "featurescripts"),
        std_dir=root / data.get("std", "std"),
        documents=documents,
        backend_document=_backend(data, documents),
        github=data["github"].rstrip("/") if "github" in data else None,
        main_branch=data.get("main_branch"),
        default_icon=(
            root / data.get("path", "featurescripts") / data["default_icon"] if "default_icon" in data else None
        ),
    )
    if "frontend" in data:
        config.frontend = _named(config, "[tool.fs] frontend", data["frontend"], Role.FRONTEND)
    parts = data.get("parts", {})
    if not isinstance(parts, dict):
        raise ConfigError("[tool.fs.parts] must hold a table for each part set.")
    config.parts = tuple(_part_set(config, name, entry) for name, entry in parts.items())
    return config


_URL = re.compile(r"(https://[a-z0-9-]+\.onshape\.com)/documents/(\w+)(?:/w/(\w+))?/?$")


def _document(name: str, entry: dict) -> Document:
    where = f"[tool.fs.documents.{name}]"
    unknown = set(entry) - {"role", "url", "keys"}
    if unknown:
        raise ConfigError(f"{where} has unknown keys: {', '.join(sorted(unknown))} (it takes role, url, and keys).")
    role = entry.get("role")
    if role not in [each.value for each in Role]:
        raise ConfigError(f"{where} role must be {', '.join(Role)}.")
    url = entry.get("url")
    found = _URL.match(url) if isinstance(url, str) else None
    if found is None:
        raise ConfigError(
            f"{where} url must be its url, like https://cad.onshape.com/documents/<id>/w/<id> (the workspace is "
            "optional, except for the backend)."
        )
    keys = entry.get("keys")
    if keys is not None and (not isinstance(keys, str) or not re.fullmatch(r"[A-Z][A-Z0-9_]*", keys)):
        raise ConfigError(f"{where} keys must be the prefix of its keys' variables, like FRCDESIGN.")
    return Document(name, Role(role), found.group(1), found.group(2), found.group(3), keys)


def _named(config: Config, where: str, name: object, role: Role) -> Document:
    if not isinstance(name, str) or "/" in name:
        raise ConfigError(f"{where} must name a {role} document in [tool.fs.documents].")
    try:
        return config.find_document(name, role)
    except ConfigError as error:
        raise ConfigError(f"{where}: {error}") from error


def _backend(data: dict, documents: tuple[Document, ...]) -> Document:
    backends = [document for document in documents if document.role is Role.BACKEND]
    name = data.get("backend")
    if name is None:
        if len(backends) != 1:
            raise ConfigError(
                "[tool.fs] backend must name the backend document (the one synced with the code folder) in "
                "[tool.fs.documents]."
            )
        found = backends[0]
    else:
        found = next((document for document in backends if document.name == name), None)
        if found is None:
            names = ", ".join(document.name for document in backends) or "none"
            raise ConfigError(f"[tool.fs] backend must name a backend document in [tool.fs.documents] ({names}).")
    if found.workspace_id is None:
        raise ConfigError(f"[tool.fs.documents.{found.name}] url must name its workspace (/w/<id>): it's the backend.")
    return found


def _part_set(config: Config, name: str, entry: object) -> PartSet:
    where = f"[tool.fs.parts.{name}]"
    if not isinstance(entry, dict):
        raise ConfigError(f"{where} must be a table.")
    unknown = set(entry) - {"title", "folder", "definition", "frontend"}
    if unknown:
        raise ConfigError(
            f"{where} has unknown keys: {', '.join(sorted(unknown))} (it takes title, folder, definition, and frontend)."
        )
    folder = entry.get("folder")
    if not isinstance(folder, str) or not folder.strip("/"):
        raise ConfigError(f"{where} folder must name the folder holding its FeatureScript.")
    title = entry.get("title")
    if not isinstance(title, str) or not title:
        raise ConfigError(f"{where} title must name its versions, like Fasteners.")
    definition = entry.get("definition")
    if definition is not None and (not isinstance(definition, str) or not definition.endswith(".py") or "/" in definition):
        raise ConfigError(f"{where} definition must name a .py file in its folder, like fastenerTables.py.")
    frontend = _named(config, f"{where} frontend", entry["frontend"], Role.FRONTEND) if "frontend" in entry else None
    if frontend is not None and frontend == config.frontend:
        raise ConfigError(f"{where} frontend is FeatureScripts' frontend; a part set's releases go to a document of their own.")
    return PartSet(name, title, folder.strip("/"), frontend, definition)


def _read(path: pathlib.Path) -> dict:
    with path.open("rb") as file:
        try:
            return tomllib.load(file)
        except tomllib.TOMLDecodeError as error:
            raise ConfigError(f"Failed to parse {path}: {error}") from error
