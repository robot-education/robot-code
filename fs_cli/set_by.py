"""Hidden parameters editing logic sets from a function of the definition, which their annotations name (`"Set By" :
"isNoTopHolesFrame"`; see `docs/featurescript-style.md`'s "Hidden parameters set by editing logic"). The dialog tools
don't run editing logic, so they work these out the way it would, running the function with `fs_eval`: `fs ui` for its
settings (`apply`), and `fs audit`'s page (and the preview) for every combination of the choices the function reads
(`tree`).

A tree is found by running the function with nothing chosen: when it reads an enum, boolean, or lookup table parameter
which isn't chosen yet, it's run again for each of that parameter's values. So it branches only on what's read, in the
order it's read (`isCustomFrame(definition) && ...` reads no frame when the frame isn't custom). A lookup table's path
is chosen a level at a time, as the function reads its levels (`path.supplier == "Custom"` branches on the supplier
alone, not on every belt each supplier sells), so a table of hundreds of rows costs only the levels read.
"""

from __future__ import annotations

import dataclasses
import pathlib
from typing import Any, Iterator

from fs_cli.ui import DialogBuilder, EnumValue, Parameter

# How many times a function may run for a tree, before giving up on it
MAX_RUNS = 5000

# An evaluator for each repo (by its code folder), with the times the files it's loaded were changed, to reuse while
# they're unchanged: loading std takes a second or two, and the preview renders on every change
_evaluators: dict[pathlib.Path, tuple[Any, dict[pathlib.Path, float]]] = {}


def _mtime(path: pathlib.Path) -> float:
    try:
        return path.stat().st_mtime
    except OSError:
        return -1.0


def _evaluator(path: pathlib.Path) -> Any:
    """An evaluator for the repo `path` is in, reused if no file it's loaded has changed."""
    from fs_cli.config import load_config
    from fs_eval import Evaluator

    code_dir = load_config(path.parent).code_dir
    cached = _evaluators.get(code_dir)
    if cached is not None and all(_mtime(loaded) == mtime for loaded, mtime in cached[1].items()):
        return cached[0]
    evaluator = Evaluator(path.parent)
    _evaluators[code_dir] = (evaluator, {})
    return evaluator


def _loaded(evaluator: Any) -> None:
    """Notes when the files `evaluator` has loaded were changed (see `_evaluator`)."""
    for cached, times in _evaluators.values():
        if cached is evaluator:
            for loaded in evaluator.interpreter.modules:
                times.setdefault(loaded, _mtime(loaded))


class NeedKey(Exception):
    """A parameter the function read which isn't chosen yet."""

    def __init__(self, key: str) -> None:
        super().__init__(key)
        self.key = key


class NeedLevel(Exception):
    """A level of a lookup table parameter's (`parameter`) path the function read which isn't chosen yet."""

    def __init__(self, parameter: str) -> None:
        super().__init__(parameter)
        self.parameter = parameter


@dataclasses.dataclass(frozen=True)
class PartialPath:
    """A lookup table parameter's path, chosen a level at a time: its levels chosen so far (name to choice), and
    whether it's whole (its last choice is a leaf), when reading a level it doesn't have gives undefined."""

    levels: tuple[tuple[str, str], ...] = ()
    whole: bool = False


class Reads(dict):
    """A definition map's entries (see `fs_eval.values.FSMap`), which raise `NeedKey` for a parameter in `open` (not
    chosen yet) when it's read."""

    def __init__(self, entries: dict, open: frozenset) -> None:
        super().__init__(entries)
        self.open = open

    def copied(self) -> Reads:
        return Reads(self, self.open)

    def _check(self, key: Any) -> None:
        if key in self.open and not dict.__contains__(self, key):
            raise NeedKey(key[1])

    def get(self, key: Any, default: Any = None) -> Any:
        self._check(key)
        return dict.get(self, key, default)

    def __getitem__(self, key: Any) -> Any:
        self._check(key)
        return dict.__getitem__(self, key)

    def __contains__(self, key: Any) -> bool:
        self._check(key)
        return dict.__contains__(self, key)


class LevelReads(Reads):
    """A lookup table parameter's (`parameter`) partial path's levels (see `PartialPath`), which raise `NeedLevel` for
    a level in `open` when it's read."""

    def __init__(self, entries: dict, open: frozenset, parameter: str) -> None:
        super().__init__(entries, open)
        self.parameter = parameter

    def copied(self) -> LevelReads:
        return LevelReads(self, self.open, self.parameter)

    def _check(self, key: Any) -> None:
        if key in self.open and not dict.__contains__(self, key):
            raise NeedLevel(self.parameter)


class SetBy:
    """A feature's "Set By" parameters (`parameters`: each one's key and function), worked out for its dialog as
    `builder` declared it."""

    def __init__(self, path: pathlib.Path, builder: DialogBuilder) -> None:
        self.path = path
        self.builder = builder
        self.parameters = {
            key: str(parameter.annotation["Set By"])
            for key, parameter in builder.parameters.items()
            if "." not in key and isinstance(parameter.annotation.get("Set By"), str)
        }
        self._evaluator: Any = None
        self._module: Any = None
        self._enums: dict[tuple[str, str], Any] = {}
        self._path_tag: Any = None

    def _load(self) -> None:
        if self._module is None:
            self._evaluator = _evaluator(self.path)
            self._module = self._evaluator.module(self.path.resolve())
            _loaded(self._evaluator)

    def _enum(self, type_name: str, value: str) -> Any:
        if (type_name, value) not in self._enums:
            self._enums[type_name, value] = self._evaluator.eval(f"{type_name}.{value}", self._module)
        return self._enums[type_name, value]

    def _fs_value(self, value: Any) -> Any:
        """A dialog value (see `DialogBuilder.declare`) as FeatureScript's, or None for one there's none of."""
        if isinstance(value, EnumValue):
            return self._enum(value.type, value.value)
        if isinstance(value, (bool, str)):
            return value
        if isinstance(value, dict) and all(isinstance(item, str) for item in value.values()):
            return self._lookup_path(value)
        return None

    def _lookup_path(self, value: dict[str, str]) -> Any:
        """A lookup table's value (each level's name and choice), as a LookupTablePath."""
        from fs_eval import to_fs

        return self._evaluator.eval("path as LookupTablePath", self._module, path=to_fs(value))

    def run(self, function: str, chosen: dict[str, Any], open: frozenset[str] = frozenset()) -> Any:
        """The function's result for a definition with `chosen`'s values (FeatureScript's), raising `NeedKey` if it
        reads a parameter in `open`."""
        from fs_eval.values import FSMap, key_of

        self._load()
        entries = {}
        for key, value in chosen.items():
            if isinstance(value, PartialPath):
                value = self._partial_path(key, value)
            if value is not None:
                entries[key_of(key)] = (key, value)
        definition = FSMap(Reads(entries, frozenset(key_of(key) for key in open)))
        return self._evaluator.eval(f"{function}(definition)", self._module, definition=definition)

    def _partial_path(self, key: str, partial: PartialPath) -> Any:
        """A partial path as a LookupTablePath which raises `NeedLevel` when one of its table's levels it hasn't chosen
        is read (unless it's whole)."""
        from fs_eval.values import FSMap, key_of

        if self._path_tag is None:
            self._path_tag = self._evaluator.eval("{} as LookupTablePath", self._module).tag
        chosen = dict(partial.levels)
        levels = set() if partial.whole else _level_names(self.builder.parameters[key].annotation.get("Lookup Table")) - set(chosen)
        entries = {key_of(level): (level, choice) for level, choice in chosen.items()}
        return FSMap(LevelReads(entries, frozenset(key_of(level) for level in levels), key), self._path_tag)

    def _setting(self, key: str, result: Any) -> str | None:
        """A function's result as the parameter's setting (as `fs ui --set` takes it), or None if it isn't one."""
        parameter = self.builder.parameters[key]
        if parameter.kind == "boolean" and isinstance(result, bool):
            return "true" if result else "false"
        if parameter.kind == "enum" and parameter.enum is not None:
            name = getattr(result, "name", None)
            return name if name in parameter.enum.values else None
        return None

    # fs ui

    def apply(self, overrides: dict[str, str]) -> list[str]:
        """Sets each parameter (unless it's `--set`) in the builder's definition to its function's result for the
        values chosen, and returns warnings for those it couldn't work out (which keep their defaults)."""
        warnings = []
        for key, function in self.parameters.items():
            if key in overrides:
                continue
            self._load()
            chosen = {name: self._fs_value(value) for name, value in self.builder.definition.values.items()}
            try:
                setting = self._setting(key, self.run(function, chosen))
            except Exception as error:  # noqa: BLE001 - anything the function throws, or the evaluator can't do
                warnings.append(f"Couldn't work out {key} with {function}: {error}; it's its default.")
                continue
            if setting is None:
                warnings.append(f"{function} didn't return a value of {key}'s; it's its default.")
                continue
            self.builder.definition.values[key] = self._dialog_value(key, setting)
        return warnings

    def _dialog_value(self, key: str, setting: str) -> Any:
        parameter = self.builder.parameters[key]
        if parameter.kind == "enum" and parameter.enum is not None:
            return EnumValue(parameter.enum.name, setting)
        return setting == "true"

    # fs audit

    def _choices(self, parameter: Parameter) -> Iterator[tuple[str, Any]]:
        """Each value of a parameter a tree can branch on: its setting (as the page has it), and its value."""
        from fs_cli.ui import lookup_value

        if parameter.kind == "enum" and parameter.enum is not None:
            for value in parameter.enum.values:
                yield value, self._enum(parameter.enum.name, value)
        elif parameter.kind == "boolean":
            yield "true", True
            yield "false", False
        elif parameter.kind == "lookup":
            table = parameter.annotation.get("Lookup Table")
            for choices in _lookup_paths(table):
                yield " > ".join(choices), self._lookup_path(lookup_value(table, choices))

    def tree(self, key: str) -> dict | None:
        """The parameter's setting for every combination of what its function reads: a leaf `{"v": setting}` (null
        where it couldn't be worked out), or a branch `{"k": key, "b": {setting: tree}}` on a parameter it read, by
        that parameter's settings. None if it takes more than MAX_RUNS runs."""
        self._load()
        function = self.parameters[key]
        branchable = {
            name
            for name, parameter in self.builder.parameters.items()
            if "." not in name and name not in self.parameters and parameter.kind in ("enum", "boolean", "lookup")
        }
        runs = 0

        lookups = {name for name in branchable if self.builder.parameters[name].kind == "lookup"}

        def explore(chosen: dict[str, Any]) -> dict | None:
            nonlocal runs
            runs += 1
            if runs > MAX_RUNS:
                return None
            try:
                return {"v": self._setting(key, self.run(function, chosen, frozenset(branchable - set(chosen))))}
            except NeedKey as need:
                if need.key in lookups:
                    # Read: its levels are chosen as they're read
                    return explore({**chosen, need.key: PartialPath()})
                branches = {}
                for setting, value in self._choices(self.builder.parameters[need.key]):
                    branch = explore({**chosen, need.key: value})
                    if branch is None:
                        return None
                    branches[setting] = branch
                return {"k": need.key, "b": branches}
            except NeedLevel as need:
                # The next level of its path which isn't chosen (the one read, or one above it)
                name = need.parameter
                table = self.builder.parameters[name].annotation.get("Lookup Table")
                partial = chosen[name]
                node = _node_at(table, partial.levels)
                assert node is not None
                branches = {}
                for choice, entry in node["entries"].items():
                    levels = (*partial.levels, (str(node["name"]), str(choice)))
                    whole = not (isinstance(entry, dict) and isinstance(entry.get("entries"), dict) and entry["entries"])
                    branch = explore({**chosen, name: PartialPath(levels, whole)})
                    if branch is None:
                        return None
                    branches[str(choice)] = branch
                return {"k": name, "l": str(node["name"]), "b": branches}
            except Exception:  # noqa: BLE001 - anything the function throws: the page uses the default
                return {"v": None}

        return explore({})


def _level_names(table: Any) -> set[str]:
    """Every level's name in a lookup table."""
    names: set[str] = set()

    def visit(node: Any) -> None:
        entries = node.get("entries") if isinstance(node, dict) else None
        if isinstance(entries, dict) and entries:
            names.add(str(node.get("name")))
            for entry in entries.values():
                visit(entry)

    visit(table)
    return names


def _node_at(table: Any, levels: tuple[tuple[str, str], ...]) -> dict | None:
    """The level a path chosen this far chooses next, or None if it's whole."""
    node = table
    for _, choice in levels:
        node = node["entries"][choice]
    entries = node.get("entries") if isinstance(node, dict) else None
    return node if isinstance(entries, dict) and entries else None


def _lookup_paths(table: Any, prefix: tuple[str, ...] = ()) -> Iterator[list[str]]:
    """Every whole path through a lookup table, as each level's choice."""
    entries = table.get("entries") if isinstance(table, dict) else None
    if not isinstance(entries, dict) or not entries:
        if prefix:
            yield list(prefix)
        return
    for name, entry in entries.items():
        yield from _lookup_paths(entry, (*prefix, str(name)))
