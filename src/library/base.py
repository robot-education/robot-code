from abc import ABC
from typing import Iterable


class Node(ABC, object):
    def __init__(self):
        pass

    def __radd__(self, string: str) -> str:
        return string + str(self)

    def __str__(self) -> str:
        raise NotImplementedError


class DummyNode(Node):
    """An empty node."""

    def __str__(self) -> str:
        return ""


class Map(Node):
    """Defines a map literal."""

    def __init__(self, dict: dict[str, str | None]):
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
    return "".join(["\t" + line for line in lines])


def quote(string: str) -> str:
    """Adds quotes around string."""
    return '"' + string + '"'


def user_name(parameter_name: str) -> str:
    """Converts a camel case parameter name to a user facing name which is in sentence case.

    Example: myEnum (or MyEnum) -> My enum
    """
    # re.findall("[A-Z][a-z]")
    # parameter_name.split()
    return ""
