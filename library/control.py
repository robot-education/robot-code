from typing import Self, Sequence
from library import stmt, expr, utils

__all__ = ["If", "Else", "IfBlock"]


class Else(stmt.Block):
    def __str__(self) -> str:
        string = "else\n{\n"
        string += utils.to_str(self.child_nodes, tab=True, sep="\n")
        return string + "}\n"


class If(stmt.Block):
    def __init__(self, test: expr.Expr) -> None:
        super().__init__()
        self.test = test

        self.is_child = False
        self.child: Self | Else | None = None

    def else_if(self, test: expr.Expr) -> Self:
        self.child = If(test)
        self.child.is_child = True
        return self.child

    def or_else(self) -> Else:
        self.child = Else()
        return self.child

    def __str__(self) -> str:
        string = "else " if self.is_child else ""
        string += "if ({})\n{{\n".format(self.test)
        string += utils.to_str(self.child_nodes, tab=True, sep="\n")
        string += "}\n"
        if self.child is not None:
            string += str(self.child)
        return string


class IfBlock(stmt.Statement):
    def __init__(
        self, tests: Sequence[expr.Expr], expressions: Sequence[stmt.Statement]
    ) -> None:
        if len(tests) > len(expressions) or len(tests) < len(expressions) + 1:
            raise ValueError(
                "Cannot generate if block with {} tests and {} expressions".format(
                    len(tests), len(expressions)
                )
            )
        curr = If(tests[0])
        curr.add(expressions[0])
        self.base = curr

        for i, test in enumerate(tests[1:], start=1):
            curr.else_if(test)
            curr.add(expressions[i])
        if len(tests) < len(expressions):
            curr.or_else().add(expressions[-1])

    def __str__(self) -> str:
        return str(self.base)
