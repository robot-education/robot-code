from library.base import expr, stmt
from library.core import utils

__all__ = ["Const", "merge_maps"]


class Const(stmt.Statement):
    def __init__(
        self, name: str, expr: expr.Expr, export: bool = False, **kwargs
    ) -> None:
        super().__init__(**kwargs)
        self.line = stmt.Line(
            utils.export(export) + "const " + name + " = " + str(expr)
        )

    def __str__(self) -> str:
        return str(self.line)


def merge_maps(defaults: expr.Expr, m: expr.Expr) -> expr.Expr:
    return expr.Call("mergeMaps", defaults, m)
