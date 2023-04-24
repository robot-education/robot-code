from abc import ABC, abstractmethod
from typing import Iterable, Iterator, TypeVar


class Node(ABC):
    def __radd__(self, string: str) -> str:
        return string + str(self)

    @abstractmethod
    def __str__(self) -> str:
        raise NotImplementedError



# T = TypeVar("T", bound=Node)

class ParentNode(Node, ABC, T = TypeVar('T')):
    """A node which supports an array of (possibly nested) children.

    Note __str__ is not defined for this class; parents should implement themselves.
    """

    def __init__(self, child_nodes: Iterable[T] | None = []) -> None:
        self.child_nodes: list[T] = list(*child_nodes)

    def add(self, node: T) -> T:
        """Adds a node as a child.

        Returns the node which was passed in."""
        self.child_nodes.append(node)
        return node

    def __iter__(self) -> Iterator[Node]:
        return self.child_nodes.__iter__()


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
