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
    def build(self, attributes: node.Attributes) -> str:
        attributes.contexts.add(node.Context.EXPRESSION)
        return self.expr.build(attributes) + ";\n"


def cast_to_stmt(node: Statement | expr.Expr) -> Statement:
    if isinstance(node, expr.Expr):
        return Line(node)
    return node


class Return(Statement):
    def __init__(self, expression: expr.Expr | str) -> None:
        self.expr = expr.cast_to_expr(expression)

    @override
    def build(self, attributes: node.Attributes) -> str:
        attributes.contexts.add(node.Context.EXPRESSION)
        return "return " + self.expr.build(attributes) + ";\n"


class BlockParent(node.ParentNode):
    @override
    def build_children(self, attributes: node.Attributes, **kwargs) -> str:
        self.children = list(cast_to_stmt(node) for node in self.children)  # type: ignore
        return super().build_children(attributes, **kwargs)


class BlockStatement(Statement, BlockParent):
    """A type of statement which supports nested children."""

    pass
