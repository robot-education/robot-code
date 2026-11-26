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
        definition: The file in `folder` configuring its Part Studios, where the folder holds another set's too; every
            definition in the folder otherwise.
        targets: Other documents it can be released to by name, to try a release out, like a library loaded locally:
            `[tool.fs.parts.<name>.targets]`, each name a document's url.
    """

    name: str
    title: str
    folder: str
    main_document_id: str
    targets: dict[str, str] = dataclasses.field(default_factory=dict)
    definition: str | None = None

    def target_document_id(self, target: str) -> str:
        """The document a target names: one of `targets`, or a document's url."""
        url = self.targets.get(target, target)
        found = _DOCUMENT_URL.search(url)
        if found is None:
            names = ", ".join(self.targets) or "none"
            raise ConfigError(f"{target} is neither a target of {self.name} ({names}) nor a document's url.")
        if found.group(1) == self.main_document_id:
            raise ConfigError(f"{target} is {self.name}'s main document, which only its releases go to.")
        return found.group(1)

    def holds(self, path: str) -> bool:
        """Whether the file at `path` (relative to the code folder) is the set's."""
        return path.startswith(self.folder + "/")


@dataclasses.dataclass
class PushTarget:
    """A document `fs push --test` pushes FeatureScripts to, with what they import, to try them out (see
    docs/fs-cli.md#test-documents): `[tool.fs.test_documents.<name>]`.

    Attributes:
        name: Its key in [tool.fs.test_documents], which `--to` names it by.
        instance: Its workspace.
        base_url: The Onshape it's on, like https://cad.onshape.com, or an Enterprise's own domain.
        keys: Where its API keys are: the prefix of `<keys>_API_ACCESS_KEY` and `<keys>_API_SECRET_KEY`, like an
            Enterprise's, whose every request is signed; None for the usual keys, with which it must be public.
    """

    name: str
    instance: InstancePath
    base_url: str
    keys: str | None = None

    @property
    def url(self) -> str:
        return f"{self.base_url}/documents/{self.instance.document_id}/w/{self.instance.instance_id}"


@dataclasses.dataclass
class Config:
    """
    Attributes:
        root: The repo root (the folder containing pyproject.toml).
        code_dir: The folder Feature Studios in the backend document are mirrored to.
        std_dir: The folder holding a copy of the Onshape std library.
        backend: The workspace holding the source of every FeatureScript.
        frontend: The public workspace released FeatureScripts are published to.
        parts: Part sets, released by `fs parts` rather than `fs release`.
        github: The repo's GitHub url, which released FeatureScripts link to as their source.
        main_branch: The git branch releases run on, and only once it's what origin has (see
            docs/fs-cli.md#branches); None to release from any branch, unchecked.
        default_icon: The `.svg` FRCDesignApp shows for a released feature with no icon of its own.
        test_documents: The documents `fs push --test` can push to, the default (`test_document`) first.
    """

    root: pathlib.Path
    code_dir: pathlib.Path
    std_dir: pathlib.Path
    backend: InstancePath
    frontend: InstancePath | None = None
    parts: tuple[PartSet, ...] = ()
    github: str | None = None
    main_branch: str | None = None
    default_icon: pathlib.Path | None = None
    test_documents: tuple[PushTarget, ...] = ()

    def find_test_document(self, name: str | None) -> PushTarget:
        """The test document of that name, or the default one."""
        if not self.test_documents:
            raise ConfigError("Name a test document to push to in [tool.fs.test_documents] (see docs/fs-cli.md).")
        if name is None:
            return self.test_documents[0]
        found = next((each for each in self.test_documents if each.name == name), None)
        if found is None:
            names = ", ".join(each.name for each in self.test_documents)
            raise ConfigError(f"No test document named {name} in [tool.fs.test_documents] (there's {names}).")
        return found

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
    config = Config(
        root=root,
        code_dir=root / data.get("path", "featurescripts"),
        std_dir=root / data.get("std", "std"),
        backend=_workspace(data, "backend"),
        frontend=_workspace(data, "frontend") if "frontend" in data else None,
        parts=tuple(_part_set(name, entry) for name, entry in data.get("parts", {}).items()),
        github=data["github"].rstrip("/") if "github" in data else None,
        main_branch=data.get("main_branch"),
        default_icon=(
            root / data.get("path", "featurescripts") / data["default_icon"] if "default_icon" in data else None
        ),
    )
    config.test_documents = _test_documents(config, data)
    return config


def _test_documents(config: Config, data: dict) -> tuple[PushTarget, ...]:
    entries = data.get("test_documents", {})
    if not isinstance(entries, dict):
        raise ConfigError("[tool.fs.test_documents] must hold a table for each test document.")
    targets = [_test_document(config, name, entry) for name, entry in entries.items()]
    default = data.get("test_document")
    if default is None:
        if len(targets) > 1:
            raise ConfigError("[tool.fs] test_document must name the default of [tool.fs.test_documents].")
        return tuple(targets)
    found = next((each for each in targets if each.name == default), None)
    if found is None:
        raise ConfigError(f"[tool.fs] test_document names {default}, which isn't in [tool.fs.test_documents].")
    return (found, *(each for each in targets if each is not found))


def _test_document(config: Config, name: str, entry: object) -> PushTarget:
    where = f"[tool.fs.test_documents.{name}]"
    url = entry.get("url") if isinstance(entry, dict) else None
    found = _WORKSPACE_URL.match(url) if isinstance(url, str) else None
    if found is None or not isinstance(entry, dict):
        raise ConfigError(f"{where} url must be its workspace's url, like https://cad.onshape.com/documents/<id>/w/<id>.")
    keys = entry.get("keys")
    if keys is not None and (not isinstance(keys, str) or not re.fullmatch(r"[A-Z][A-Z0-9_]*", keys)):
        raise ConfigError(f"{where} keys must be the prefix of its keys' variables, like ENTERPRISE.")
    document_id = found.group(2)
    # Only releases write these
    released = {
        config.frontend.document_id if config.frontend else None: "the frontend document",
        **{part_set.main_document_id: f"the {part_set.name} part set's main document" for part_set in config.parts},
        config.backend.document_id: "the backend document",
    }
    if document_id in released:
        raise ConfigError(f"{where} is {released[document_id]}, which isn't for testing.")
    return PushTarget(name, InstancePath(document_id, found.group(3)), found.group(1), keys)


_WORKSPACE_URL = re.compile(r"(https://[a-z0-9-]+\.onshape\.com)/documents/([0-9a-f]{24})/w/([0-9a-f]{24})(?:/|$)")


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
    targets = entry.get("targets", {})
    if not isinstance(targets, dict) or not all(_DOCUMENT_URL.search(str(url)) for url in targets.values()):
        raise ConfigError(f"{where} targets must map names to documents' urls.")
    definition = entry.get("definition")
    if definition is not None and (not isinstance(definition, str) or not definition.endswith(".py") or "/" in definition):
        raise ConfigError(f"{where} definition must name a .py file in its folder, like fastenerTables.py.")
    return PartSet(name, title, folder.strip("/"), main.group(1), dict(targets), definition)


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
