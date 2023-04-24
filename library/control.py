from typing import Self
from library import stmt, base, expr, utils

__all__ = ["If", "Else"]


class Else(stmt.Statement):
    def __init__(self) -> None:
        pass


class If(base.ParentNode[stmt.Statement]):
    def __init__(self, test: expr.Expr, or_else: Self | Else | None = None) -> None:
        self.test = test
        super().__init__()

    def __str__(self) -> str:
        string = "if ({})\n{{\n".format(self.test)
        string += utils.to_str(self.child_nodes, tab=True)
        return string + "}\n"
