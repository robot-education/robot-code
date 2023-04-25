from __future__ import annotations
from typing import Iterator, Self
from library import base, expr


class Statement(base.Node):
    """An class representing a statement.

    A statement is a singular code construct which composes one or more lines.
    """

    def register_parent(self, parent: Parent) -> Self:
        parent.child_nodes.append(self)
        # parent.register(self)
        return self


class Parent(Statement):
    """
    A class representing a parent node which can be passed to children to register them.
    The child is responsible for registering the parent.
    """

    def __init__(self, *child_nodes: Statement) -> None:
        self.child_nodes = list(child_nodes)

    # def register(self, statement: Statement) -> Self:
    #     self.child_nodes.append(statement)
    #     return self

    def __iter__(self) -> Iterator[Statement]:
        return self.child_nodes.__iter__()

    def __len__(self) -> int:
        return len(self.child_nodes)


class Line(Statement):
    """Represents an statement which spans a single line."""

    def __init__(self, expr: expr.Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return str(self.expr) + ";\n"


def convert_expr_to_lines(*nodes: Statement | expr.Expr) -> list[Statement]:
    return [Line(node) if isinstance(node, expr.Expr) else node for node in nodes]


class BlockStatement(Parent):
    """A type of statement which supports children."""

    def __init__(self, *child_nodes: Statement | expr.Expr) -> None:
        super().__init__(*convert_expr_to_lines(*child_nodes))

    def add(self, *nodes: Statement | expr.Expr) -> Self:
        self.child_nodes.extend(convert_expr_to_lines(*nodes))
        return self
