"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""
from __future__ import annotations

from abc import ABC
from src.library import base


class Expr(base.Node, ABC):
    def __invert__(self) -> Expr:
        return UnaryOp(self, "!")

    def __and__(self, other: Expr) -> Expr:
        return BoolOp(self, "&&", other)

    def __or__(self, other: Expr) -> Expr:
        return BoolOp(self, "||", other)


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
    def __init__(self, left: Expr, operator: str, right: Expr) -> None:
        self.left = left
        self.operator = operator
        self.right = right

    def __str__(self) -> str:
        return " ".join([str(self.left), self.operator, str(self.right)])


# class Parens(BoolExpr):
#     def __init__(self, expr: BoolExpr) -> None:
#         self.token = "(" + expr.token + ")"
