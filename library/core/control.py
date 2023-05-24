from __future__ import annotations
import enum
from typing import Self, Sequence
from typing_extensions import override
import warnings
from library.base import ctxt, node, stmt, expr

__all__ = ["IfBlock", "make_if_block"]


class _If(stmt.BlockStatement):
    def __init__(self, test: expr.Expr, else_if: bool = False) -> None:
        super().__init__()
        self.test = test
        self.else_if = else_if

    @override
    def build(self, context: ctxt.Context) -> str:
        string = "else " if self.else_if else ""
        string += "if ({})\n{{\n".format(self.test.run_build(context))
        string += self.build_children(
            context, indent=True, sep="\n" if context.ui else ""
        )
        return string + "}\n"


class _Else(stmt.BlockStatement):
    @override
    def build(self, context: ctxt.Context) -> str:
        string = "else\n{\n"
        string += self.build_children(
            context, indent=True, sep="\n" if context.ui else ""
        )
        return string + "}\n"


class IfBlock(stmt.BlockStatement):
    """Represents a block of if statements.

    Calling `else_if` or `or_else` tweaks the behavior.
     composes an if statement, followed by one or more else_ifs, followed by an else. At any given point an if is only one statement.
    Adding to an If class always adds to the most recently activated condition.
    """

    def __init__(
        self, test: expr.Expr, *, parent: node.ParentNode | None = None
    ) -> None:
        super().__init__(parent=parent)
        self.else_called = False
        super().add(_If(test))

    @override
    def add(self, *nodes: stmt.Statement | expr.Expr) -> Self:
        self.children[-1].add(*nodes)  # type: ignore
        return self

    def else_if(self, test: expr.Expr) -> Self:
        if self.else_called:
            raise ValueError("Cannot add else if to an IfBlock after an else.")
        super().add(_If(test, else_if=True))
        return self

    def or_else(self) -> Self:
        if self.else_called:
            raise ValueError("Cannot add else to an IfBlock multiple times.")
        self.else_called = True
        super().add(_Else())
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.build_children(context)


def make_if_block(
    tests: Sequence[expr.Expr],
    statements: Sequence[stmt.Statement],
    else_statement: stmt.Statement | None = None,
    add_else: bool = True,
    *,
    parent: node.ParentNode,
) -> IfBlock:
    """Constructs an IfBlock directly from tests and statements. Assumes one statement per test."""
    if len(tests) != len(statements):
        raise ValueError(
            "Cannot generate if block with {} tests and {} expressions.".format(
                len(tests), len(statements)
            )
        )

    base = IfBlock(tests[0], parent=parent).add(statements[0])
    for i, test in enumerate(tests[1:], start=1):
        base.else_if(test).add(statements[i])

    if else_statement is not None:
        if add_else:
            base.or_else().add(else_statement)
        else:
            parent.add(else_statement)

    return base
