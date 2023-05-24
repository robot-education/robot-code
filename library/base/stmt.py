from __future__ import annotations
from typing_extensions import override
from library.base import expr, node, ctxt

__all__ = ["Return"]


class Statement(node.ChildNode):
    """An class representing a statement.

    A statement is a singular code construct which spans (at least) one entire line.
    """

    pass


class StmtId(Statement):
    def __init__(self, expression: str) -> None:
        self.expression = expression

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.expression


class Line(Statement):
    """Represents an statement which spans a single line."""

    def __init__(self, expression: expr.Expr | str) -> None:
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.expression.run_build(context) + ";\n"


def cast_to_stmt(node: Statement | expr.Expr) -> Statement:
    if isinstance(node, expr.Expr):
        return Line(node)
    return node


class Return(Statement):
    def __init__(self, expression: expr.Expr | str) -> None:
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        return "return " + self.expression.run_build(context) + ";\n"


class BlockParent(node.ParentNode):
    """A node which supports one or more children."""

    @override
    def build_children(self, context: ctxt.Context, cast: bool = True, **kwargs) -> str:
        self.children = list(cast_to_stmt(node) for node in self.children)
        return super().build_children(context, **kwargs)


class BlockStatement(Statement, BlockParent):
    """A type of statement which supports nested children."""

    pass
