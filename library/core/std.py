from library.base import ctxt, expr, msg, node, stmt
from library.core import utils

__all__ = ["Assign", "Const", "Var", "merge_maps"]


class Assign(stmt.Statement):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(**kwargs)
        self.name = name
        self.expr = expr

    def build(self, context: ctxt.Context) -> str:
        if not context.is_statement():
            return msg.warn_statement()
        return stmt.Line(self.name + " = " + self.expr.build(context)).build(context)


class Const(Assign):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(name, expr, **kwargs)

    def build(self, context: ctxt.Context) -> str:
        export = False
        if context.is_definition():
            context.set_statement()
            export = True
        return utils.export(export) + "const " + super().build(context)


class Var(Assign):
    def __init__(self, name: str, expr: expr.Expr, **kwargs) -> None:
        super().__init__(name, expr, **kwargs)

    def build(self, context: ctxt.Context) -> str:
        return "var " + super().build(context)


def merge_maps(defaults: expr.Expr | str, m: expr.Expr | str) -> expr.Expr:
    return expr.Call("mergeMaps", expr.cast_to_expr(defaults), expr.cast_to_expr(m))
