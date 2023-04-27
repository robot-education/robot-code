from __future__ import annotations

from typing import Self, Sequence
from library import stmt, expr, utils

__all__ = ["If", "IfBlock"]


class _If(stmt.BlockStatement):
    def __init__(self, test: expr.Expr) -> None:
        self.test = test

    def __str__(self) -> str:
        string = "if ({})\n{{\n".format(self.test)
        string += utils.to_str(self.child_nodes, tab=True, sep="\n")
        return string + "}\n"


class _ElseIf(stmt.BlockStatement):
    def __init__(self, test: expr.Expr) -> None:
        self.test = test

    def __str__(self) -> str:
        string = "else if ({})\n{{\n".format(self.test)
        string += utils.to_str(self.child_nodes, tab=True, sep="\n")
        return string + "}\n"


class _Else(stmt.BlockStatement):
    def __str__(self) -> str:
        string = "else\n{\n"
        string += utils.to_str(self.child_nodes, tab=True, sep="\n")
        return string + "}\n"


class If(stmt.ParentBase[_If | _ElseIf | _Else], stmt.Statement):
    """Represents an if statement.
    Nesting works as follows: an If composes an if statement, followed by one or more else_ifs, followed by an else. At any given point an if is only one statement.
    Adding to an If class always adds to the most recently activated condition.
    """

    def __init__(self, test: expr.Expr, *, parent: stmt.ParentBase) -> None:
        super().__init__()
        self.register_parent(parent)
        self.child_nodes.append(_If(test))

    def add(self, *nodes: stmt.Statement | expr.Expr) -> Self:
        self.child_nodes[-1].add(*nodes)
        return self

    def else_if(self, test: expr.Expr) -> Self:
        self.child_nodes.append(_ElseIf(test))
        return self

    def or_else(self) -> Self:
        self.child_nodes.append(_Else())
        return self

    def __str__(self) -> str:
        return utils.to_str(self.child_nodes)


class IfBlock:
    def __init__(
        self,
        *,
        tests: Sequence[expr.Expr],
        statements: Sequence[stmt.Statement],
        else_statement: stmt.Statement | None = None,
        add_else: bool = True,
        parent: stmt.Parent,
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
