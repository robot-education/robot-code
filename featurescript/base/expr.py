"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""

from __future__ import annotations
import dataclasses

import enum as std_enum
from typing import Iterator, Self, override
from featurescript.base import ctxt, node, expr, user_error

__all__ = ["Parens", "Id"]


def expr_or_stmt(expression_string: str, scope: ctxt.Scope) -> str:
    """Converts expression_string to a statement based on context."""
    if scope == ctxt.Scope.EXPRESSION:
        return expression_string
    elif scope == ctxt.Scope.STATEMENT:
        return expression_string + ";\n"
    return user_error.expected_scope(ctxt.Scope.EXPRESSION, ctxt.Scope.STATEMENT)


class Expression(node.Node):
    def __and__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "&&", other)

    def __or__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "||", other)

    def __invert__(self) -> Expression:
        return UnaryOp("!", self)

    def __add__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "+", other)

    def __sub__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "-", other)

    def __mul__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "*", other)

    def __truediv__(self, other: ExprCandidate) -> Expression:
        return BoolOp(self, "/", other)

    def __radd__(self, lhs: ExprCandidate) -> Expression:
        return BoolOp(lhs, "+", self)

    def __rsub__(self, lhs: ExprCandidate) -> Expression:
        return BoolOp(lhs, "-", self)

    def __rmul__(self, lhs: ExprCandidate) -> Expression:
        return BoolOp(lhs, "*", self)

    def __rtruediv__(self, lhs: ExprCandidate) -> Expression:
        return BoolOp(lhs, "/", self)

    # Add iter to support use as a statement for predicate
    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()


ExprCandidate = expr.Expression | bool | str | int | float


def cast_to_expr(node: ExprCandidate) -> Expression:
    if not isinstance(node, Expression):
        if isinstance(node, bool):
            node = str(node).lower()
        elif isinstance(node, int | float):
            node = str(node)
        return Id(node)
    return node


def build_expr(node: ExprCandidate, context: ctxt.Context) -> str:
    """Builds a given ExprCandidate as an expression."""
    return cast_to_expr(node).run_build(context, scope=context.scope.EXPRESSION)


class Id(Expression):
    """Wraps a string into an identifier node."""

    def __init__(self, identifier: str) -> None:
        # Cannot be ExprCandidate due to recursion
        self.identifier = identifier

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.identifier


class EqualOperator(std_enum.StrEnum):
    EQUAL = "=="
    NOT_EQUAL = "!="


class Equal(Expression):
    def __init__(
        self, lhs: ExprCandidate, operator: EqualOperator, rhs: ExprCandidate
    ) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    @override
    def __invert__(self) -> Expression:
        """Overload inversion to flip from == to !=."""
        self.operator = (
            EqualOperator.NOT_EQUAL
            if self.operator == EqualOperator.EQUAL
            else EqualOperator.EQUAL
        )
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        return expr_or_stmt(
            " ".join(
                [
                    build_expr(self.lhs, context),
                    self.operator,
                    build_expr(self.rhs, context),
                ]
            ),
            context.scope,
        )


@dataclasses.dataclass
class Parens(Expression):
    expr: Expression

    @override
    def build(self, context: ctxt.Context) -> str:
        return expr_or_stmt(
            "({})".format(build_expr(self.expr, context)), context.scope
        )


def add_parens(expression: Expression) -> Expression:
    """If expression is not Parens, encloses Expression in Parens."""
    if isinstance(expression, Parens):
        return expression
    return Parens(expression)


@dataclasses.dataclass
class UnaryOp(Expression):
    """An operation with a single operand."""

    operator: str
    operand: ExprCandidate

    @override
    def build(self, context: ctxt.Context) -> str:
        return expr_or_stmt(
            self.operator + build_expr(self.operand, context), context.scope
        )


@dataclasses.dataclass
class BoolOp(Expression):
    """An operation with two operands."""

    lhs: ExprCandidate
    operator: str
    rhs: ExprCandidate

    @override
    def build(self, context: ctxt.Context) -> str:
        return expr_or_stmt(
            " ".join(
                [
                    build_expr(self.lhs, context),
                    self.operator,
                    build_expr(self.rhs, context),
                ]
            ),
            context.scope,
        )
