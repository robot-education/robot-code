from __future__ import annotations
from abc import ABC, abstractmethod
import copy
from math import comb
from typing import Any, Iterable, Self, Type

import dataclasses, collections
import enum as std_enum

from library.base import str_utils


class Context(std_enum.Enum):
    CONSTRUCT = std_enum.auto()
    STATEMENT = std_enum.auto()
    ENUM = std_enum.auto()
    EXPRESSION = std_enum.auto()
    UI = std_enum.auto()


@dataclasses.dataclass()
class Attributes:
    std_version: str
    contexts: set[Context] = dataclasses.field(
        default_factory=lambda: {Context.CONSTRUCT}
    )
    indent: int = 0
    stack: collections.deque[dict] = dataclasses.field(
        default_factory=lambda: collections.deque()
    )

    def as_dict(self) -> dict[str, Any]:
        return dict(
            # shallow copy
            (field.name, copy.copy(getattr(self, field.name)))
            for field in dataclasses.fields(self)
            if field.name != "stack"
        )

    def save(self) -> None:
        self.stack.append(self.as_dict())

    def restore(self) -> None:
        for key, value in self.stack.pop().items():
            setattr(self, key, value)


class Node(ABC):
    def __new__(cls: Type[Self], *args, **kwargs) -> Type[Node]:
        saved_build = cls.build

        def run_build(self, attributes: Attributes, **build_kwargs) -> str:
            attributes.save()
            string = saved_build(self, attributes, **build_kwargs)
            attributes.restore()
            return string

        cls.build = run_build
        return super().__new__(cls)

    @abstractmethod
    def build(self, attributes: Attributes, **kwargs) -> str:
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

    def build_children(self, attributes: Attributes, **kwargs) -> str:
        return build_nodes(self.children, attributes, **kwargs)


def build_nodes(
    nodes: Iterable[Node],
    attributes: Attributes,
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
        attributes.indent += 1
    strings = [node.build(attributes) for node in nodes]
    # if end_after_sep:
    combined = (sep + end).join(strings) + end
    # else:
    #     combined = sep.join(string + end for string in strings)
    return str_utils.tab_lines(combined) if indent else combined


class Construct(ChildNode):
    """A class representing a top-level construct."""

    pass


class BlockConstruct(Construct, ParentNode):
    pass
