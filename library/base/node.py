from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Generic, Iterable, Self, TypeVar
from typing_extensions import override

from library.base import str_utils, ctxt


class Node(ABC):
    def run_build(self, context: ctxt.Context) -> str:
        context.save()
        string = self.build(context)
        context.restore()
        return string

    @abstractmethod
    def build(self, context: ctxt.Context) -> str:
        ...


N = TypeVar("N", bound=Node)


class ChildNode(Node, ABC):
    def __init__(self, parent: ParentNode | None = None, **kwargs) -> None:
        super().__init__(**kwargs)
        if parent is not None:
            parent.add(self)


class ParentNode(Node, Generic[N], ABC):
    """A node which supports an array of (possibly nested) children."""

    def __init__(self) -> None:
        self.children: list[N] = []

    def add(self, *children: N) -> Self:
        """Adds one or more children to the class."""
        self.children.extend(children)
        return self

    def build_children(self, context: ctxt.Context, **kwargs) -> str:
        return build_nodes(self.children, context, **kwargs)


class TopStatement(Node, ABC):
    def run_build_top(
        self,
        context: ctxt.Context,
    ) -> str:
        context.save()
        # context.top = False?
        string = self.build_top(context)
        context.restore()
        return string

    @abstractmethod
    def build_top(self, context: ctxt.Context) -> str:
        ...

    @override
    def build(self, context: ctxt.Context) -> str:
        return ""


def build_nodes(
    nodes: Iterable[Node],
    context: ctxt.Context,
    # level: ctxt.Level | None = None,
    sep: str = "",
    end: str = "",
    indent: bool = False,
) -> str:
    """Converts an iterable of nodes to a tuple of strings.

    sep: The seperator to put in between strings.
    end: A string to append to each node.
    tab: Whether to tab strings over.
    """
    if indent:
        context.indent += 1
    strings = [node.run_build(context) for node in nodes]
    combined = (sep + end).join(strings) + end
    return str_utils.indent(combined) if indent else combined
