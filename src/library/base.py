from abc import ABC, abstractmethod
from typing_extensions import override


class Node(ABC):
    def __init__(self):
        pass

    def __str__(self) -> str:
        raise NotImplementedError


class DummyNode(Node):
    """An empty node."""

    def __str__(self) -> str:
        return ""


dummy_node = DummyNode()


def enter(*nodes: Node) -> tuple[str, ...]:
    return tuple(str(node) for node in nodes)


def tab(string: str) -> str:
    lines = string.splitlines(keepends=True)
    return "".join(["    " + line for line in lines])
