from abc import ABC, abstractmethod
from typing import Iterable, TypeVar
import re


class Node(ABC):
    def __radd__(self, string: str) -> str:
        return string + str(self)

    @abstractmethod
    def __str__(self) -> str:
        raise NotImplementedError


T = TypeVar("T", bound=Node)


class ParentNode(Node, ABC):
    """A node which supports an array of (possibly nested) children."""

    def __init__(self):
        self.child_nodes: list[Node] = []

    def add(self, node: T) -> T:
        """Adds a node as a child.

        Returns the node which was passed in."""
        self.child_nodes.append(node)
        return node

    def __str__(self) -> str:
        return "".join(str(node) for node in self.child_nodes)


class DummyNode(Node):
    """An empty node."""

    def __str__(self) -> str:
        return ""


class Map(Node):
    """Defines a map literal."""

    def __init__(self, dict: dict[str, str]):
        self.dict = dict

    def __str__(self) -> str:
        format_string = ' "{}" : {}'
        pairs = [
            format_string.format(key, value)
            for key, value in self.dict.items()
            if value is not None
        ]

        if len(pairs) == 0:
            return "{}"

        return "{{{}}}".format(",".join(pairs) + " ")


def export(export: bool) -> str:
    return "export " if export else ""


def to_str(nodes: Iterable[Node]) -> tuple[str, ...]:
    return tuple(str(node) for node in nodes)


def tab(string: str) -> str:
    lines = string.splitlines(keepends=True)
    return "".join(["    " + line for line in lines])


def quote(string: str) -> str:
    """Adds quotes around string."""
    return '"' + string + '"'


def user_name(parameter_name: str) -> str:
    """Converts a camel case parameter name to a user facing name which is in sentence case.

    Example: myEnum (or MyEnum) -> My enum
    """
    words = re.findall("[a-zA-Z][^A-Z]*", parameter_name)
    words[0] = words[0].capitalize()
    words[1:] = [word.lower() for word in words[1:]]
    return " ".join(words)
