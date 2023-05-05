from library.base import ctxt, expr, node, stmt
from library.core import utils

__all__ = ["Assign", "Const", "Var", "merge_maps"]


class Assign(stmt.Statement):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(**kwargs)
        self.name = name
        self.expr = expr

    def build(self, context: ctxt.Context) -> str:
        return stmt.Line(self.name + " = " + self.expr.build(context)).build(context)


class Const(node.TopStatement, Assign):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(name, expr, **kwargs)

    def build_top(self, context: ctxt.Context) -> str:
        return "export " + self.build(context)

    def build(self, context: ctxt.Context) -> str:
        return "const " + super().build(context)


class Var(Assign):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(name, expr, **kwargs)

    def build(self, context: ctxt.Context) -> str:
        return "var " + super().build(context)


def merge_maps(defaults: expr.Expr | str, m: expr.Expr | str) -> expr.Expr:
    return expr.Call("mergeMaps", expr.cast_to_expr(defaults), expr.cast_to_expr(m))
