from __future__ import annotations
from typing import Self, Sequence
from typing_extensions import override
from featurescript.base import ctxt, node, expr, user_error

__all__ = ["IfBlock", "make_if_block"]


class _If(node.ParentNode):
    def __init__(self, test: expr.Expression, else_if: bool = False) -> None:
        """
        Args:
            else_if: Whether the statement is an else if statement.
        """
        super().__init__()
        self.test = test
        self.else_if = else_if

    @override
    def build(self, context: ctxt.Context) -> str:
        string = "else " if self.else_if else ""
        string += "if ({})\n{{\n".format(
            self.test.run_build(context, scope=ctxt.Scope.EXPRESSION)
        )
        string += self.build_children(
            context, indent=True, sep="\n" if context.ui else ""
        )
        return string + "}\n"


class _Else(node.ParentNode):
    @override
    def build(self, context: ctxt.Context) -> str:
        string = "else\n{\n"
        string += self.build_children(
            context, indent=True, sep="\n" if context.ui else ""
        )
        return string + "}\n"


class IfBlock(node.ParentNode):
    """Represents a block of if statements.

    Calling `else_if` or `or_else` tweaks the behavior.
     composes an if statement, followed by one or more else_ifs, followed by an else. At any given point an if is only one statement.
    Adding to an If class always adds to the most recently activated condition.
    """

    def __init__(
        self, test: expr.Expression, *, parent: node.ParentNode | None = None
    ) -> None:
        super().__init__()
        node.handle_parent(self, parent)
        self.else_called = False
        super().add(_If(test))

    @override
    def add(self, *nodes: node.Node) -> Self:
        self.children[-1].add(*nodes)
        return self

    def else_if(self, test: expr.Expression) -> Self:
        if self.else_called:
            raise ValueError("Cannot add else if to an IfBlock after an else.")
        super().add(_If(test, else_if=True))
        return self

    def or_else(self, *nodes: node.Node) -> Self:
        if self.else_called:
            raise ValueError("Cannot add else to an IfBlock multiple times.")
        self.else_called = True
        super().add(_Else().add(*nodes))
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.STATEMENT:
            return self.build_children(context)
        return user_error.expected_scope(ctxt.Scope.STATEMENT)


def make_if_block(
    tests: Sequence[expr.Expression],
    statements: Sequence[node.Node],
    else_statement: node.Node | None = None,
    # add_else: bool = True,
    parent: node.ParentNode | None = None,
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

    if else_statement:
        base.or_else().add(else_statement)
        # else:
        #     parent.add(else_statement)

    return base
