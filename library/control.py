from __future__ import annotations

from typing import Self, Sequence
from library import base, stmt, expr

__all__ = ["If", "if_block"]


class _If(stmt.BlockStatement):
    def __init__(self, test: expr.Expr) -> None:
        super().__init__()
        self.test = test

    def __str__(self) -> str:
        string = "if ({})\n{{\n".format(self.test)
        string += self.children_str(tab=True, sep="\n")
        return string + "}\n"


class _ElseIf(stmt.BlockStatement):
    def __init__(self, test: expr.Expr) -> None:
        super().__init__()
        self.test = test

    def __str__(self) -> str:
        string = "else if ({})\n{{\n".format(self.test)
        string += self.children_str(tab=True, sep="\n")
        return string + "}\n"


class _Else(stmt.BlockStatement):
    def __str__(self) -> str:
        string = "else\n{\n"
        string += self.children_str(tab=True, sep="\n")
        return string + "}\n"


class If(stmt.BlockStatement):
    """Represents an if statement.
    Nesting works as follows: an If composes an if statement, followed by one or more else_ifs, followed by an else. At any given point an if is only one statement.
    Adding to an If class always adds to the most recently activated condition.
    """

    def __init__(self, test: expr.Expr, *, parent: base.ParentNode) -> None:
        super().__init__(parent=parent)
        self.children.append(_If(test))

    def add(self, *nodes: stmt.Statement | expr.Expr) -> Self:
        self.children[-1].add(*nodes)  # type: ignore
        return self

    def _register(self, node: stmt.Statement | expr.Expr) -> Self:
        self.children[-1]._register(node)  # type: ignore
        return self

    def else_if(self, test: expr.Expr) -> Self:
        self.children.append(_ElseIf(test))
        return self

    def or_else(self) -> Self:
        self.children.append(_Else())
        return self

    def __str__(self) -> str:
        print(self.children)
        return self.children_str()


def if_block(
    *,
    tests: Sequence[expr.Expr],
    statements: Sequence[stmt.Statement],
    else_statement: stmt.Statement | None = None,
    add_else: bool = True,
    parent: base.ParentNode,
) -> None:
    if len(tests) != len(statements):
        raise ValueError(
            "Cannot generate if block with {} tests and {} expressions".format(
                len(tests), len(statements)
            )
        )

    base = If(tests[0], parent=parent).add(statements[0])
    for i, test in enumerate(tests[1:], start=1):
        base.else_if(test).add(statements[i])

    if else_statement is not None:
        if add_else:
            base.or_else().add(else_statement)
        else:
            parent.add(else_statement)
