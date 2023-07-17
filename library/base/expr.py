"""
A module defining expressions. Expressions refer to boolean logic and mathematical operations.
"""
from __future__ import annotations

import enum as std_enum
from typing import Iterator, Self
from typing_extensions import override
from library.base import ctxt, node, expr, user_error

__all__ = ["Parens", "Id", "Call", "ui_predicate_call"]


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
        return UnaryOp(self, "!")

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
            node = "true" if node else "false"
        elif isinstance(node, int | float):
            node = str(node)
        return Id(node)
    return node


def build_expr(node: ExprCandidate, context: ctxt.Context) -> str:
    return cast_to_expr(node).run_build(context)


class Id(Expression):
    def __init__(self, identifier: str) -> None:
        # Cannot be ExprCandidate due to recursion
        self.identifier = identifier

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.identifier


class Operator(std_enum.StrEnum):
    EQUAL = "=="
    NOT_EQUAL = "!="


class Equal(Expression):
    def __init__(
        self, lhs: Expression | str, operator: Operator, rhs: Expression | str
    ) -> None:
        self.lhs = lhs
        self.operator = operator
        self.rhs = rhs

    @override
    def __invert__(self) -> Expression:
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


class Parens(Expression):
    def __init__(self, expr: Expression) -> None:
        self.expr = expr

    @override
    def build(self, context: ctxt.Context) -> str:
        return "({})".format(self.expr.run_build(context))


def add_parens(expression: Expression):
    if isinstance(expression, Parens):
        return expression
    return Parens(expression)


def ui_predicate_call(name: str) -> Call:
    return Call(name + "Predicate", "definition")


class Call(Expression):
    def __init__(
        self, name: str, *args: expr.ExprCandidate, inline: bool = True
    ) -> None:
        self.name = name
        self.args = (cast_to_expr(arg) for arg in args)
        self.inline = inline

    @override
    def build(self, context: ctxt.Context) -> str:
        join_str = ", " if self.inline else ",\n"
        result = "{}({})".format(
            self.name,
            node.build_nodes(
                self.args, context, sep=join_str, scope=ctxt.Scope.EXPRESSION
            ),
        )
        return expr_or_stmt(result, context.scope)


class UnaryOp(Expression):
    def __init__(self, operand: ExprCandidate, operator: str) -> None:
        self.operand = operand
        self.operator = operator

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.operator + build_expr(self.operand, context)


class BoolOp(Expression):
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
