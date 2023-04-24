from typing import Self
from src.library import base, expr, utils


class Statement(base.Node):
    def __init__(self, expr: expr.Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return str(self.expr) + ";\n"

    def __iter__(self):
        return [self].__iter__()


class Else(Statement):
    def __init__(self) -> None:
        pass


class If(base.ParentNode[Statement]):
    def __init__(self, test: expr.Expr, or_else: Self | Else | None = None) -> None:
        self.test = test
        super().__init__()

    def __str__(self) -> str:
        string = "if ({})\n{{\n".format(self.test)
        string += utils.to_str(self.child_nodes, tab=True)
        return string + "}\n"
