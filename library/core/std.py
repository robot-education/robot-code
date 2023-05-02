from library.base import expr, node, stmt
from library.core import utils

__all__ = ["Const", "merge_maps"]


class Const(stmt.Statement):
    def __init__(
        self, name: str, expr: expr.Expr, export: bool = False, **kwargs
    ) -> None:
        super().__init__(**kwargs)
        self.name = name
        self.expr = expr
        self.export = export

    def build(self, context: node.Context) -> str:
        return stmt.Line(
            utils.export(self.export)
            + "const "
            + self.name
            + " = "
            + self.expr.build(context)
        ).build(context)


def merge_maps(defaults: expr.Expr, m: expr.Expr) -> expr.Expr:
    return expr.Call("mergeMaps", defaults, m)
