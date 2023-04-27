from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Self

from library import utils


class Node(ABC):
    @abstractmethod
    def __str__(self) -> str:
        raise NotImplementedError


class ChildNode(Node, ABC):
    def __init__(self, *args, parent: ParentNode | None = None, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        if parent is not None:
            parent._register(self)


class ParentNode(Node, ABC):
    """A node which supports an array of (possibly nested) children."""

    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self.children: list[ChildNode] = []

    def add(self, *children: ChildNode) -> Self:
        """Adds one or more children to the class."""
        self.children.extend(children)
        return self

    def _register(self, node: ChildNode) -> Self:
        """Adds a node to the class.

        This method is intended to be invoked by child nodes which are registering themselves with a parent node.
        """
        self.children.append(node)
        return self

    def children_str(self, **kwargs) -> str:
        return utils.to_str(self.children, **kwargs)
