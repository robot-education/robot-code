from __future__ import annotations
from typing import Self
from library.base import expr, node

__all__ = ["Return"]


class Statement(node.ChildNode):
    """An class representing a statement.

    A statement is a singular code construct which composes one or more lines.
    """

    pass


class Line(Statement):
    """Represents an statement which spans a single line."""

    def __init__(self, expression: expr.Expr | str) -> None:
        self.expr = expression

    def __str__(self) -> str:
        return str(self.expr) + ";\n"


class Return(Statement):
    def __init__(self, expression: expr.Expr | str) -> None:
        self.expr = expression

    def __str__(self) -> str:
        return "return " + str(self.expr) + ";\n"


def convert_expr_to_lines(*nodes: Statement | expr.Expr) -> list[Statement]:
    return [Line(node) if isinstance(node, expr.Expr) else node for node in nodes]


class BlockParent(node.ParentNode):
    def add(self, *children: Statement | expr.Expr) -> Self:
        super().add(*convert_expr_to_lines(*children))
        return self


class BlockStatement(Statement, BlockParent):
    """A type of statement which supports nested children."""

    pass