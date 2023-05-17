"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""
from __future__ import annotations

from abc import ABC
import enum as std_enum
from typing import Iterator, Self
from typing_extensions import override
from library.base import ctxt, node, expr

__all__ = ["Parens", "Id", "Call"]


class Operator(std_enum.StrEnum):
    EQUAL = "=="
    NOT_EQUAL = "!="


class Expr(node.Node, ABC):
    def __invert__(self) -> Expr:
        return UnaryOp(self, "!")

    def __and__(self, other: Expr) -> Expr:
        return BoolOp(self, "&&", other)

    def __or__(self, other: Expr) -> Expr:
        return BoolOp(self, "||", other)

    def __add__(self, other: Expr) -> Expr:
        return BoolOp(self, "+", other)

    def __sub__(self, other: Expr) -> Expr:
        return BoolOp(self, "-", other)

    def __mul__(self, other: ExprCandidate) -> Expr:
        return BoolOp(self, "*", other)

    def __truediv__(self, other: ExprCandidate) -> Expr:
        return BoolOp(self, "/", other)

    def __radd__(self, lhs: ExprCandidate) -> Expr:
        return BoolOp(lhs, "+", self)

    def __rsub__(self, lhs: ExprCandidate) -> Expr:
        return BoolOp(lhs, "-", self)

    def __rmul__(self, lhs: ExprCandidate) -> Expr:
        return BoolOp(lhs, "*", self)

    def __rtruediv__(self, lhs: ExprCandidate) -> Expr:
        return BoolOp(lhs, "/", self)

    # Add iter to support use as a statement for predicate
    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()


class Id(Expr):
    def __init__(self, identifier: str) -> None:
        self.identifier = identifier

    @override
    def build(self, _: ctxt.Context) -> str:
        return self.identifier


ExprCandidate = Expr | bool | str | int | float


def cast_to_expr(node: ExprCandidate) -> Expr:
    if not isinstance(node, Expr):
        if isinstance(node, bool):
            node = "true" if node else "false"
        elif isinstance(node, int | float):
            node = str(node)
        return Id(node)
    return node


def build_expr(node: ExprCandidate, context: ctxt.Context) -> str:
    return cast_to_expr(node).run_build(context)


class Compare(Expr):
    def __init__(
        self, lhs: ExprCandidate, operator: Operator, rhs: ExprCandidate
    ) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    @override
    def __invert__(self) -> Expr:
        """Overload inversion to flip from == to !=."""
        self.operator = (
            Operator.NOT_EQUAL if self.operator == Operator.EQUAL else Operator.EQUAL
        )
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        return " ".join(
            [
                build_expr(self.lhs, context),
                self.operator,
                build_expr(self.rhs, context),
            ]
        )


class Parens(Expr):
    def __init__(self, expr: Expr) -> None:
        self.expr = expr

    @override
    def build(self, context: ctxt.Context) -> str:
        return "({})".format(self.expr.run_build(context))


def add_parens(expression: Expr):
    if isinstance(expression, Parens):
        return expression
    return Parens(expression)


class Call(Expr):
    def __init__(
        self, name: str, *args: expr.ExprCandidate, inline: bool = True
    ) -> None:
        self.name = name
        self.args = args
        self.inline = inline

    @override
    def build(self, context: ctxt.Context) -> str:
        join_str = ", " if self.inline else ",\n"
        return "{}({})".format(
            self.name,
            node.build_nodes(
                (cast_to_expr(expr) for expr in self.args), context, sep=join_str
            ),
        )


class UnaryOp(Expr):
    def __init__(self, operand: Expr, operator: str) -> None:
        self.operand = operand
        self.operator = operator

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.operator + self.operand.run_build(context)


class BoolOp(Expr):
    def __init__(self, lhs: ExprCandidate, operator: str, rhs: ExprCandidate) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    @override
    def build(self, context: ctxt.Context) -> str:
        return " ".join(
            [
                build_expr(self.lhs, context),
                self.operator,
                build_expr(self.rhs, context),
            ]
        )
