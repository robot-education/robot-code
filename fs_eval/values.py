"""FeatureScript's values, in Python.

| FeatureScript | Python |
| --- | --- |
| undefined | `None` |
| boolean | `bool` |
| number | `float` (always: `1` is `1.0`) |
| string | `str` |
| array | `FSArray` (a tuple, and maybe a type tag) |
| map | `FSMap` (a dict, and maybe a type tag) |
| enum value | `EnumValue` |
| box | `Box` (the only mutable value: shared, by reference) |
| function | `Function` (see `fs_eval.interpreter`) |
| Context | `Context` |

Values (but boxes and contexts) are immutable here: FeatureScript has value semantics, so changing an array or map
makes a new one (`FSArray.with_item`, `FSMap.with_entry`), and nothing else sees the change.

A type tag (`as MyType`) is kept on arrays and maps (`.tag`), and on other values by wrapping them in `Tagged`.
"""

from __future__ import annotations

import math
from typing import Any, Iterator


class FSType:
    """A type declared with `type Name typecheck predicate;`, or an enum (`EnumType`)."""

    __slots__ = ("name", "module", "typecheck")

    def __init__(self, name: str, module, typecheck):
        self.name = name
        self.module = module
        # The typecheck's value (a function), resolved when it's first used
        self.typecheck = typecheck

    def __repr__(self) -> str:
        return f"<type {self.name}>"


class EnumType(FSType):
    __slots__ = ("members",)

    def __init__(self, name: str, module, member_names: list[str]):
        super().__init__(name, module, None)
        self.members = {member: EnumValue(self, member, i) for i, member in enumerate(member_names)}

    def __repr__(self) -> str:
        return f"<enum {self.name}>"


class EnumValue:
    __slots__ = ("enum", "name", "ordinal")

    def __init__(self, enum: EnumType, name: str, ordinal: int):
        self.enum = enum
        self.name = name
        self.ordinal = ordinal

    def __repr__(self) -> str:
        return f"{self.enum.name}.{self.name}"


class Tagged:
    """A value other than an array or map, with a type tag (`"x" as MyString`)."""

    __slots__ = ("value", "tag")

    def __init__(self, value, tag: FSType):
        self.value = value
        self.tag = tag

    def __repr__(self) -> str:
        return f"{self.value!r} as {self.tag.name}"


class Box:
    __slots__ = ("value",)

    def __init__(self, value):
        self.value = value

    def __repr__(self) -> str:
        return f"box({self.value!r})"


class FSArray:
    __slots__ = ("items", "tag")

    def __init__(self, items: tuple = (), tag: FSType | None = None):
        self.items = items
        self.tag = tag

    def with_item(self, index: int, value) -> FSArray:
        items = list(self.items)
        items[index] = value
        return FSArray(tuple(items), self.tag)

    def __len__(self) -> int:
        return len(self.items)

    def __iter__(self) -> Iterator:
        return iter(self.items)

    def __repr__(self) -> str:
        inner = ", ".join(repr(item) for item in self.items)
        return f"[{inner}]" + (f" as {self.tag.name}" if self.tag else "")


class FSMap:
    """A map: `entries` maps each key's `key_of` to (key, value). Iterated in key order, as FeatureScript's are."""

    __slots__ = ("entries", "tag")

    def __init__(self, entries: dict | None = None, tag: FSType | None = None):
        self.entries = entries if entries is not None else {}
        self.tag = tag

    @staticmethod
    def of(pairs, tag: FSType | None = None) -> FSMap:
        return FSMap({key_of(key): (key, value) for key, value in pairs if value is not None}, tag)

    def get(self, key):
        entry = self.entries.get(key_of(key))
        return entry[1] if entry is not None else None

    def get_str(self, key: str):
        entry = self.entries.get((3, key))
        return entry[1] if entry is not None else None

    def with_entry(self, key, value) -> FSMap:
        """A copy with `key` set to `value`, or removed, if it's undefined (maps don't hold undefined)."""
        entries = dict(self.entries)
        if value is None:
            entries.pop(key_of(key), None)
        else:
            entries[key_of(key)] = (key, value)
        return FSMap(entries, self.tag)

    def without(self, key) -> FSMap:
        entries = dict(self.entries)
        entries.pop(key_of(key), None)
        return FSMap(entries, self.tag)

    def sorted_items(self) -> list[tuple[Any, Any]]:
        return [self.entries[k] for k in sorted(self.entries)]

    def __len__(self) -> int:
        return len(self.entries)

    def __repr__(self) -> str:
        inner = ", ".join(f"{k!r}: {v!r}" for k, v in self.sorted_items())
        return "{" + inner + "}" + (f" as {self.tag.name}" if self.tag else "")


class Builtin:
    """A builtin value (`is builtin`), like a context or a sketch: shared, by reference, and tagged in place."""

    tag: FSType | None = None


class Context(Builtin):
    """A Part Studio's context: here, only what the evaluator's built-ins record (sketches, attributes,
    variables), since nothing's modeled."""

    def __init__(self, version):
        self.tag: FSType | None = None
        self.version = version
        self.sketches: dict = {}
        self.variables: dict = {}
        self.attributes: list = []

    def __repr__(self) -> str:
        return "<Context>"


def untag(value):
    return value.value if type(value) is Tagged else value


def tag_of(value) -> FSType | None:
    kind = type(value)
    if kind is FSMap or kind is FSArray or kind is Tagged:
        return value.tag
    if kind is EnumValue:
        return value.enum
    if isinstance(value, Builtin):
        return value.tag
    return None


def key_of(value):
    """A hashable stand-in for a value, equal for values FeatureScript considers equal (ignoring type tags), and
    ordered as FeatureScript orders map keys."""
    kind = type(value)
    if kind is str:
        return (3, value)
    if kind is float:
        return (2, value)
    if value is None:
        return (0,)
    if kind is bool:
        return (1, value)
    if kind is int:
        return (2, float(value))
    if kind is FSArray:
        return (5, tuple(key_of(item) for item in value.items))
    if kind is FSMap:
        return (6, tuple((k, key_of(v)) for k, (_, v) in sorted(value.entries.items())))
    if kind is EnumValue:
        return (4, value.enum.name, value.ordinal)
    if kind is Tagged:
        return key_of(value.value)
    # Boxes, functions, contexts, and types: by identity
    return (7, id(value))


def equal(a, b) -> bool:
    ta = type(a)
    if ta is type(b):
        if ta is float or ta is str or ta is bool:
            return a == b
        if a is None:
            return True
        if ta is EnumValue:
            return a.enum is b.enum and a.name == b.name
    return key_of(a) == key_of(b)


def format_number(value: float) -> str:
    if math.isinf(value):
        return "inf" if value > 0 else "-inf"
    if value == int(value) and abs(value) < 1e16:
        return str(int(value))
    return repr(value)


def type_name(value) -> str:
    """The builtin type of a value, as `is` names it."""
    kind = type(value)
    if kind is Tagged:
        return type_name(value.value)
    return {
        type(None): "undefined",
        bool: "boolean",
        float: "number",
        int: "number",
        str: "string",
        FSArray: "array",
        FSMap: "map",
        Box: "box",
        EnumValue: "enum",
    }.get(kind) or ("builtin" if isinstance(value, Builtin) else "function")
