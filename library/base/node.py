from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Generic, Iterable, Self, TypeVar

from library.base import str_utils, ctxt


# should be ABC, but ABC breaks enum useage?
class Node:
    def run_build(self, context: ctxt.Context, scope: ctxt.Scope | None = None) -> str:
        """Saves the context state onto the stack, executes build, and then pops the context back.

        Note this method should not be invoked in a super() call to prevent infinite recursion; invoke build() instead.

        Args:
            scope: Set the scope for the build. Note the scope persists even after the build.
        """
        context.save()
        if scope:
            context.scope = scope
        string = self.build(context=context)
        context.restore()
        return string

    @abstractmethod
    def build(self, context: ctxt.Context) -> str:
        """An abstract method which should build the node into a string and return it.

        Note this method should not be invoked directly except via a super() call; call run_build instead.
        """
        ...

    def add_to_parent(self, parent: ParentNode) -> Self:
        """A method which is invoked to add the current node to a parent."""
        parent.children.append(self)
        return self


def handle_parent(node: Node, parent: ParentNode | None):
    """If parent is not None, adds node to parent."""
    if parent:
        node.add_to_parent(parent)


N = TypeVar("N", bound=Node)


class ParentNode(Node, Generic[N], ABC):
    """A node which supports an array of children."""

    def __init__(self, children: Iterable[N] = []) -> None:
        self.children = list(children)

    def add(self, *nodes: N) -> Self:
        """Adds one or more nodes to the class."""
        for node in nodes:
            node.add_to_parent(self)
        return self

    def build_children(self, context: ctxt.Context, **kwargs) -> str:
        """Builds the children added to the class.

        Args:
            **kwargs: kwargs to pass to build_nodes.
        """
        return build_nodes(self.children, context, **kwargs)


def build_nodes(
    nodes: Iterable[Node],
    context: ctxt.Context,
    sep: str = "",
    end: str = "",
    indent: bool = False,
    scope: ctxt.Scope | None = None,
) -> str:
    """Converts an iterable of nodes to a tuple of strings.

    Args:
        sep: A seperator to place between each node.
        end: A separator placed between each node and at the end.
        indent: Whether to indent each node.
    """
    if indent:
        context.indent += 1
    strings = [node.run_build(context, scope=scope) for node in nodes]
    combined = (sep + end).join(strings) + end
    return str_utils.indent(combined) if indent else combined
