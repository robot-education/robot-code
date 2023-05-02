from __future__ import annotations
from abc import ABC, abstractmethod
from typing import Iterable, Self, Type

import dataclasses
import enum as std_enum

from library.base import str_utils


class NodeType(std_enum.Enum):
    STATEMENT = std_enum.auto()
    EXPRESSION = std_enum.auto()
    TOP_LEVEL = std_enum.auto()
    ENUM = std_enum.auto()


@dataclasses.dataclass()
class Context:
    std_version: str
    type: NodeType = NodeType.TOP_LEVEL
    ui: bool = False
    indent: int = 0

    def copy(self) -> Self:
        return Context(**dataclasses.asdict(self))

    def become(self, context: Self) -> None:
        for field in dataclasses.fields(context):
            setattr(self, field.name, getattr(context, field.name))


def call_build(node: Node, context: Context, **context_kwargs):
    original = context.copy()
    for key, value in context_kwargs:
        context.__setattr__(key, value)
    node.build(context)
    context.become(original)


class Node(ABC):
    def __new__(cls: Type[Self], *args, **kwargs) -> Type[Node]:
        saved_build = cls.build

        def run_build(self, context: Context, **build_kwargs) -> str:
            original = context.copy()
            string = saved_build(self, context, **build_kwargs)
            context.become(original)
            return string

        cls.build = run_build
        return super().__new__(cls)

    @abstractmethod
    def build(self, context: Context, **kwargs) -> str:
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
