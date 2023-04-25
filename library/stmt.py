from typing import Iterable, Self
from library import base, expr


class Statement(base.Node):
    """An class representing a statement which takes up one or more lines.

    By default, a statement is assumed to be an expression. However, many classes which are statements
    override this behavior.
    """

    pass


class Line(Statement):
    """Represents an expression which spans a line."""

    def __init__(self, expr: expr.Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return str(self.expr) + ";\n"


def convert_expr_to_lines(*nodes: Statement | expr.Expr) -> list[Statement]:
    return [Line(node) if isinstance(node, expr.Expr) else node for node in nodes]


class Block(base.ParentNode[Statement], Statement):
    def __init__(self, *child_nodes: Statement | expr.Expr) -> None:
        super().__init__(*convert_expr_to_lines(*child_nodes))

    def add(self, *nodes: Statement | expr.Expr) -> Self:
        super().add(*convert_expr_to_lines(*nodes))
        return self
