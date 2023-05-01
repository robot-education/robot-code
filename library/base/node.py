from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Iterable, Self, Type

import dataclasses

from library.base import str_utils


@dataclasses.dataclass()
class Context:
    std_version: str
    enum: bool = False
    ui: bool = False
    expr: bool = False
    stmt: bool = True
    top_stmt: bool = True
    indent: int = 0


class Node(ABC):
    def __new__(cls: Type[Self], *args, **kwargs) -> Type[Node]:
        build_func = cls.build

        def run_build(self, context: Context, **kwargs) -> str:
            self.pre_build(context)
            # avoid infinite recursion
            string = build_func(self, context, **kwargs)
            # transformers = cls.transformers(self)
            # for transform in transformers:
            #     string = transform(context, string)

            self.post_build(context)
            return string

        cls.build = run_build
        return super().__new__(cls)

    @abstractmethod
    def build(self, context: Context, **kwargs) -> str:
        ...

    def pre_build(self, context: Context) -> None:
        ...

    # def transformers(self) -> Iterable[Callable[[Context, str], str]]:
    #     return []

    def post_build(self, context: Context) -> None:
        ...


# def apply_indent(context: Context, string: str) -> str:
#     lines = string.splitlines(keepends=True)
#     return "".join([("    " * context.indent) + line for line in lines])


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

    def build_children(self, context: Context, **kwargs) -> str:
        return build_nodes(self.children, context, **kwargs)


def build_nodes(
    nodes: Iterable[Node],
    context: Context,
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
    strings = [node.build(context) + end for node in nodes]
    if indent:
        context.indent -= 1
        return str_utils.tab_lines(sep.join(strings))
    else:
        return sep.join(strings)
