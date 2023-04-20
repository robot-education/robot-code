import re

from abc import ABC


class Node(ABC):
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


dummy_node = DummyNode()


class Map(Node):
    """Defines a map literal."""

    def __init__(self, dict: dict[str, str | None] | dict[str, str]):
        self.dict = dict

    def __str__(self) -> str:
        pairs = [
            " {} : {}".format(key, value)
            for key, value in self.dict.items()
            if value is not None
        ]

        if len(pairs) == 0:
            return "{}"

        return "{{{}}}".format(",".join(pairs) + " ")


def export(export: bool) -> str:
    return "export " if export else ""


def to_str(*nodes: Node) -> tuple[str, ...]:
    return tuple(str(node) for node in nodes)


def tab(string: str) -> str:
    lines = string.splitlines(keepends=True)
    return "".join(["    " + line for line in lines])


def user_name(parameter_name: str) -> str:
    """Converts a camel case parameter name to a user facing name which is in sentence case.

    Example: myEnum (or MyEnum) -> My enum
    """
    # re.findall("[A-Z][a-z]")
    # parameter_name.split()
    return ""
