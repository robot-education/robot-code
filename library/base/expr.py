"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""
from __future__ import annotations

from abc import ABC
import enum as std_enum
from typing import Iterator, Self
from typing_extensions import override
from library.base import ctxt, node

__all__ = ["Parens", "Id"]


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

    # Add iter to support use as a statement for predicate
    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()


class Id(Expr):
    def __init__(self, identifier: str) -> None:
        self.identifier = identifier

    @override
    def build(self, _: ctxt.Context) -> str:
        return self.identifier


def cast_to_expr(node: Expr | str) -> Expr:
    if not isinstance(node, Expr):
        return Id(node)
    return node


class Compare(Expr):
    def __init__(self, lhs: Expr, operator: Operator, rhs: Expr) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    def __invert__(self) -> Expr:
        """Overload inversion to flip from == to !=."""
        self.operator = (
            Operator.NOT_EQUAL if self.operator == Operator.EQUAL else Operator.EQUAL
        )
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        return " ".join(
            [self.lhs.build(context), self.operator, self.rhs.build(context)]
        )


class Parens(Expr):
    def __init__(self, expr: Expr) -> None:
        self.expr = expr

    @override
    def build(self, context: ctxt.Context) -> str:
        return "({})".format(self.expr.build(context))


def add_parens(expression: Expr):
    if isinstance(expression, Parens):
        return expression
    return Parens(expression)


class Call(Expr):
    def __init__(self, name: str, *exprs: Expr | str, inline: bool = True) -> None:
        self.name = name
        self.exprs = exprs
        self.inline = inline

    @override
    def build(self, context: ctxt.Context) -> str:
        join_str = ", " if self.inline else ",\n"
        return "{}({})".format(
            self.name,
            node.build_nodes(
                (cast_to_expr(expr) for expr in self.exprs), context, sep=join_str
            ),
        )


class UnaryOp(Expr):
    def __init__(self, operand: Expr, operator: str) -> None:
        self.operand = operand
        self.operator = operator

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.operator + self.operand.build(context)


class BoolOp(Expr):
    def __init__(self, lhs: Expr, operator: str, rhs: Expr) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    @override
    def build(self, context: ctxt.Context) -> str:
        return " ".join(
            [self.lhs.build(context), self.operator, self.rhs.build(context)]
        )
