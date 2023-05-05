from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Iterable, Self, Type

from library.base import str_utils, ctxt


class Node(ABC):
    def __new__(cls: Type[Self], *args, **kwargs) -> Type[Node]:
        saved_build = cls.build

        # inject level argument to build during class init
        def run_build(
            self,
            context: ctxt.Context,
            **build_kwargs,
        ) -> str:
            context.save()
            string = saved_build(self, context, **build_kwargs)
            context.restore()
            return string

        cls.build = run_build
        return super().__new__(cls)

    @abstractmethod
    def build(self, context: ctxt.Context) -> str:
        ...


class ChildNode(Node, ABC):
    def __init__(self, parent: ParentNode | None = None, **kwargs) -> None:
        super().__init__(**kwargs)
        if parent is not None:
            parent.add(self)


class ParentNode(Node, ABC):
    """A node which supports an array of (possibly nested) children."""

    def __init__(self) -> None:
        self.children: list[Node] = []

    def add(self, *children: Node) -> Self:
        """Adds one or more children to the class."""
        self.children.extend(children)
        return self

    def build_children(self, context: ctxt.Context, **kwargs) -> str:
        return build_nodes(self.children, context, **kwargs)


# def build(node: Node, context: ctxt.Context) -> str:
#     context.save()
#     string = node.build(context)
#     context.restore()
#     return string


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
    strings = [node.build(context) for node in nodes]
    # if end_after_sep:
    combined = (sep + end).join(strings) + end
    # else:
    #     combined = sep.join(string + end for string in strings)
    return str_utils.tab_lines(combined) if indent else combined
