from __future__ import annotations

from typing import Self, Sequence
from typing_extensions import override
from library.base import node, stmt, expr

__all__ = ["IfBlock", "make_if_block"]


class _If(stmt.BlockStatement):
    def __init__(self, test: expr.Expr, else_if: bool = False) -> None:
        super().__init__()
        self.test = test
        self.else_if = else_if

    @override
    def build(self, attributes: node.Attributes) -> str:
        attributes.set_expression()
        string = "else " if self.else_if else ""
        string += "if ({})\n{{\n".format(self.test.build(attributes))
        attributes.set_statement()
        string += self.build_children(attributes, indent=True, sep="\n")
        return string + "}\n"


class _Else(stmt.BlockStatement):
    @override
    def build(self, attributes: node.Attributes) -> str:
        attributes.set_statement()
        string = "else\n{\n"
        string += self.build_children(attributes, indent=True, sep="\n")
        return string + "}\n"


class IfBlock(stmt.BlockStatement):
    """Represents an if statement.
    Nesting works as follows: an If composes an if statement, followed by one or more else_ifs, followed by an else. At any given point an if is only one statement.
    Adding to an If class always adds to the most recently activated condition.
    """

    def __init__(
        self, test: expr.Expr, *, parent: node.ParentNode | None = None
    ) -> None:
        super().__init__(parent=parent)
        super().add(_If(test))

    @override
    def add(self, *nodes: stmt.Statement | expr.Expr) -> Self:
        self.children[-1].add(*nodes)  # type: ignore
        return self

    def else_if(self, test: expr.Expr) -> Self:
        super().add(_If(test, else_if=True))
        return self

    def or_else(self) -> Self:
        super().add(_Else())
        return self

    @override
    def build(self, attributes: node.Attributes) -> str:
        return self.build_children(attributes)


def make_if_block(
    *,
    parent: node.ParentNode,
    tests: Sequence[expr.Expr],
    statements: Sequence[stmt.Statement],
    else_statement: stmt.Statement | None = None,
    add_else: bool = True,
) -> IfBlock:
    if len(tests) != len(statements):
        raise ValueError(
            "Cannot generate if block with {} tests and {} expressions".format(
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
