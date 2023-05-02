from __future__ import annotations
from typing import Self
from typing_extensions import override
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
        self.expr = expr.cast_to_expr(expression)

    @override
    def build(self, context: node.Context) -> str:
        context.type = node.NodeType.EXPRESSION
        return self.expr.build(context) + ";\n"


def cast_to_stmt(node: Statement | expr.Expr) -> Statement:
    if isinstance(node, expr.Expr):
        return Line(node)
    return node


class Return(Statement):
    def __init__(self, expression: expr.Expr | str) -> None:
        self.expr = expr.cast_to_expr(expression)

    @override
    def build(self, context: node.Context) -> str:
        context.type = node.NodeType.EXPRESSION
        return "return " + self.expr.build(context) + ";\n"


class BlockParent(node.ParentNode):
    def add(self, *children: Statement | expr.Expr) -> Self:
        node.ParentNode.add(self, *[cast_to_stmt(node) for node in children])
        return self


class BlockStatement(Statement, BlockParent):
    """A type of statement which supports nested children."""

    pass
