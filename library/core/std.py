from library.base import ctxt, expr, node, stmt
from library.core import utils

__all__ = ["Assign", "Const", "Var", "merge_maps"]


class Assign(stmt.Statement):
    def __init__(
        self, name: expr.Expr | str, expression: expr.Expr | str, **kwargs
    ) -> None:
        super().__init__(**kwargs)
        self.name = expr.cast_to_expr(name)
        self.expression = expr.cast_to_expr(expression)
        self.inline = True

    def build(self, context: ctxt.Context) -> str:
        string = (
            self.name.build(context)
            + " ={}".format(" " if self.inline else "\n")
            + self.expression.build(context)
        )
        return stmt.Line(string).build(context)


class Const(Assign, node.TopStatement):
    def __init__(
        self, name: str, expression: expr.Expr | str, export: bool = True, **kwargs
    ) -> None:
        """
        args:
            export: Whether to mark the constant as exported. Does nothing if the constant is not a top level constant.
        """
        super().__init__(name, expression, **kwargs)
        self.export = export

    def build_top(self, context: ctxt.Context) -> str:
        self.inline = False
        if self.export:
            return "export " + self.build(context)
        return self.build(context)

    def build(self, context: ctxt.Context) -> str:
        return "const " + super().build(context)


class Var(stmt.Statement):
    def __init__(
        self, name: str, expression: expr.Expr | str | None = None, **kwargs
    ) -> None:
        self.name = name
        self.expression = expression

    def build(self, context: ctxt.Context) -> str:
        string = "var {}".format(self.name)
        if self.expression is not None:
            string += " = " + expr.cast_to_expr(self.expression).build(context)
        return stmt.Line(string).build(context)


def merge_maps(defaults: expr.Expr | str, m: expr.Expr | str) -> expr.Expr:
    return expr.Call("mergeMaps", expr.cast_to_expr(defaults), expr.cast_to_expr(m))
