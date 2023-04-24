"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""
from __future__ import annotations

from abc import ABC
import enum as _enum
from library import stmt

__all__ = ["Parens", "Id"]


class Operator(_enum.StrEnum):
    EQUAL = "=="
    NOT_EQUAL = "!="


class Expr(stmt.Statement, ABC):
    def __invert__(self) -> Expr:
        return UnaryOp(self, "!")

    def __and__(self, other: Expr) -> Expr:
        return BoolOp(self, "&&", other)

    def __or__(self, other: Expr) -> Expr:
        return BoolOp(self, "||", other)


class Line(stmt.Statement):
    """Represents an expression which spans a line."""

    def __init__(self, expr: Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return str(self.expr) + ";\n"


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

    def __str__(self) -> str:
        return " ".join([str(self.lhs), self.operator, str(self.rhs)])


class Parens(Expr):
    def __init__(self, expr: Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return "({})".format(str(self.expr))


class Id(Expr):
    def __init__(self, identifier: str) -> None:
        self.identifier = identifier

    def __str__(self) -> str:
        return self.identifier


class UnaryOp(Expr):
    def __init__(self, operand: Expr, operator: str) -> None:
        self.operand = operand
        self.operator = operator

    def __str__(self) -> str:
        return self.operator + str(self.operand)


class BoolOp(Expr):
    def __init__(self, lhs: Expr, operator: str, rhs: Expr) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    def __str__(self) -> str:
        return " ".join([str(self.lhs), self.operator, str(self.rhs)])
