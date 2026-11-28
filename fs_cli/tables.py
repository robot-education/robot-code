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


VENDOR = "vendor"


def _nodes(node: Node) -> list[Node]:
    """A node and every node below it."""
    found = [node]
    for entry in node.entries:
        if entry.next is not None:
            found.extend(_nodes(entry.next))
    return found


def _without_single_leaves(node: Node) -> Node:
    """`node`, with each last level that has one option folded into the option above it, from the leaves up, so a
    level left with one option is folded too. `node` itself stays, as a table needs a level."""
    entries = []
    for entry in node.entries:
        if entry.next is not None:
            below = _without_single_leaves(entry.next)
            if len(below.entries) == 1 and below.entries[0].next is None:
                # Values chosen closer to the root take precedence, as `_render_node` gives them
                entry = dataclasses.replace(entry, values={**below.entries[0].values, **entry.values}, next=None)
            else:
                entry = dataclasses.replace(entry, next=below)
        entries.append(entry)
    return dataclasses.replace(node, entries=entries)


def _single_option(node: Node, path: list[str]) -> list[str]:
    """The path to the first level below `node` with one option, ending `level = option`, or [] for none."""
    for entry in node.entries:
        below = entry.next
        if below is None:
            continue
        if len(below.entries) == 1:
            return path + [entry.name, f"{below.display_name} = {below.entries[0].name}"]
        found = _single_option(below, path + [entry.name])
        if found:
            return found
    return []


@dataclasses.dataclass
class Table:
    """An exported constant holding a lookup table."""

    name: str
    node: Node

    def __post_init__(self) -> None:
        # Vendor is always the first choice, in a table as in a feature's dialog (docs/featurescript-style.md)
        for node in _nodes(self.node):
            if node is not self.node and node.name == VENDOR:
                raise ValueError(f"{self.name}: its vendor is a level below its first; vendor goes first")
        # A last choice with one option isn't a choice (docs/featurescript-style.md, "One option")
        self.node = _without_single_leaves(self.node)
        single = _single_option(self.node, [])
        if single:
            raise ValueError(f"{self.name}: {' > '.join(single)} is its level's one option; leave the level out")

    def render(self) -> str:
        lines = [f"export const {self.name} = {{"]
        _render_node(self.node, 8, {}, lines)
        lines.append("    };")
        return "\n".join(lines) + "\n"


@dataclasses.dataclass
class Enum:
    """An exported enum; `names` optionally gives values a name to show in the UI."""

    name: str
    values: Sequence[str]
    names: Mapping[str, str] | None = None

    def __getitem__(self, value: str) -> str:
        """Returns the FeatureScript expression for one of the enum's values."""
        if value not in self.values:
            raise KeyError(f"{self.name} has no value {value}")
        return f"{self.name}.{value}"

    def render(self) -> str:
        lines = []
        for index, value in enumerate(self.values):
            if self.names and value in self.names:
                lines.append(f"    annotation {{ {string('Name')} : {string(self.names[value])} }}\n")
            lines.append(f"    {value}{',' if self.names is None or index < len(self.values) - 1 else ''}\n")
        return f"export enum {self.name}\n{{\n{''.join(lines)}}}\n"


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
