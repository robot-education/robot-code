from abc import ABC, abstractmethod
from typing import Iterator, Self, TypeVar, Generic


class Node(ABC):
    @abstractmethod
    def __str__(self) -> str:
        raise NotImplementedError


T = TypeVar("T", bound=Node)
# S is assumed to be superclass of T
# S = TypeVar("S", bound=Node)


class ParentNode(Node, ABC, Generic[T]):
    """A node which supports an array of (possibly nested) children.

    Note __str__ is not defined for this class; parents should implement themselves.
    """

    def __init__(self, *child_nodes: T) -> None:
        self.child_nodes = list(child_nodes)

    # def register(self, node: S) -> S:
    #     """Adds a node to the class.

    #     Returns the node which was passed in."""
    #     # We assume S extends T
    #     self.child_nodes.append(node)  # type: ignore
    #     return node

