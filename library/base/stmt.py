from __future__ import annotations
from typing_extensions import override
from library.base import expr, node, ctxt

__all__ = ["Return"]


class Definition(node.ChildNode):
    pass


class BlockDefinition(node.ChildNode, node.ParentNode):
    pass


class Statement(node.ChildNode):
    """An class representing a statement.

    A statement is a singular code construct which composes one or more lines.
    """

    pass


class Line(Statement):
    """Represents an statement which spans a single line."""

    def __init__(self, expression: expr.Expr | str) -> None:
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        context.set_expression()
        return self.expression.build(context) + ";\n"


def cast_to_stmt(node: Statement | expr.Expr) -> Statement:
    if isinstance(node, expr.Expr):
        return Line(node)
    return node


class Return(Statement):
    def __init__(self, expression: expr.Expr | str) -> None:
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        context.set_expression()
        return "return " + self.expression.build(context) + ";\n"


class BlockParent(node.ParentNode):
    @override
    def build_children(self, context: ctxt.Context, **kwargs) -> str:
        if context.is_statement():
            self.children = list(cast_to_stmt(node) for node in self.children)  # type: ignore
        return super().build_children(context, **kwargs)


class BlockStatement(Statement, BlockParent):
    """A type of statement which supports nested children."""

    pass
