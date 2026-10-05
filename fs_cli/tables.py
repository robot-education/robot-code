"""Lookup tables and enums for generated Feature Studios (see fs_cli.gen).

A lookup table is a tree of Nodes, each a choice shown to the user between Values. Choosing a
Value either leads to another Node or ends at a leaf. Each leaf's result is the map of
FeatureScript expressions from every Value on the way to it.
"""

from __future__ import annotations

import dataclasses
from typing import Mapping, Sequence

# Expressions


def string(value: str) -> str:
    """A FeatureScript string literal."""
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def number(value: float) -> str:
    """A FeatureScript number, e.g. 2 or 0.3125."""
    if isinstance(value, float) and value.is_integer():
        value = int(value)
    return repr(value)


def inch(value: float) -> str:
    return f"{number(value)} * inch"


def mm(value: float) -> str:
    return f"{number(value)} * millimeter"


# Tables


@dataclasses.dataclass
class Value:
    """An option in a Node.

    Attributes:
        name: The name shown to the user.
        values: FeatureScript expressions, by key, added to the result of every leaf below this.
        next: The Node to choose from next, or None if this is a leaf.
    """

    name: str
    values: Mapping[str, str] = dataclasses.field(default_factory=dict)
    next: Node | None = None


@dataclasses.dataclass
class Node:
    """A choice between Values.

    Attributes:
        name: The key the choice is stored under.
        display_name: The name shown to the user; defaults to `name` capitalized.
        default: The name of the Value chosen by default; defaults to the first.
    """

    name: str
    entries: Sequence[Value]
    display_name: str | None = None
    default: str | None = None

    def __post_init__(self) -> None:
        if self.display_name is None:
            self.display_name = self.name[:1].upper() + self.name[1:]
        names = [entry.name for entry in self.entries]
        if len(set(names)) != len(names):
            raise ValueError(f"Node {self.name} has duplicate entries: {names}")
        if self.default is not None and self.default not in names:
            raise ValueError(
                f"Node {self.name} has default {self.default!r}, which isn't one of {names}"
            )

    def with_next(self, next: Node | Mapping[str, Node]) -> Node:
        """Returns a copy whose entries lead to next: one Node for every entry, or a Node per entry name."""
        if isinstance(next, Node):
            next = {entry.name: next for entry in self.entries}
        unknown = set(next) - {entry.name for entry in self.entries}
        if unknown:
            raise ValueError(f"Node {self.name} has no entries named {sorted(unknown)}")
        entries = [
            dataclasses.replace(entry, next=next[entry.name])
            if entry.name in next
            else entry
            for entry in self.entries
        ]
        return dataclasses.replace(self, entries=entries)


@dataclasses.dataclass
class Table:
    """An exported constant holding a lookup table."""

    name: str
    node: Node

    def render(self) -> str:
        lines = [f"export const {self.name} = {{"]
        _render_node(self.node, 8, {}, lines)
        lines.append("    };")
        return "\n".join(lines) + "\n"


@dataclasses.dataclass
class Enum:
    name: str
    values: Sequence[str]

    def __getitem__(self, value: str) -> str:
        """Returns the FeatureScript expression for one of the enum's values."""
        if value not in self.values:
            raise KeyError(f"{self.name} has no value {value}")
        return f"{self.name}.{value}"

    def render(self) -> str:
        values = "".join(f"    {value},\n" for value in self.values)
        return f"export enum {self.name}\n{{\n{values}}}\n"


def _render_node(
    node: Node, indent: int, inherited: Mapping[str, str], lines: list[str]
) -> None:
    pad = " " * indent
    lines.append(f"{pad}{string('name')} : {string(node.name)},")
    lines.append(f"{pad}{string('displayName')} : {string(node.display_name or '')},")
    if node.default is not None and node.default != node.entries[0].name:
        lines.append(f"{pad}{string('default')} : {string(node.default)},")
    lines.append(f"{pad}{string('entries')} : {{")
    for entry in node.entries:
        # Values chosen closer to the root take precedence
        values = {**entry.values, **inherited}
        key = f"{pad}    {string(entry.name)} : "
        if entry.next is None:
            pairs = ", ".join(
                f"{string(name)} : {values[name]}" for name in sorted(values)
            )
            lines.append(key + (f"{{ {pairs} }}," if pairs else "{},"))
        else:
            lines.append(key + "{")
            _render_node(entry.next, indent + 8, values, lines)
            lines.append(f"{pad}    }},")
    lines.append(f"{pad}}},")
