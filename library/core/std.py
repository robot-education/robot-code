from dataclasses import dataclass
import dataclasses
from typing_extensions import override
from library.base import ctxt, expr, node, stmt

__all__ = ["Assign", "Const", "Var", "Ternary", "merge_maps"]


class Assign(stmt.Statement):
    def __init__(
        self, name: expr.ExprCandidate, expression: expr.ExprCandidate, **kwargs
    ) -> None:
        super().__init__(**kwargs)
        self.name = expr.cast_to_expr(name)
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        string = (
            self.name.run_build(context) + " = " + self.expression.run_build(context)
        )
        return stmt.Line(string).run_build(context)


class Const(Assign, node.TopStatement, expr.Expr):
    def __init__(
        self, name: str, expression: expr.ExprCandidate, export: bool = False, **kwargs
    ) -> None:
        """
        args:
            export: Whether to mark the constant as exported. Does nothing if the constant is not a top level constant.
        """
        super().__init__(name, expression, **kwargs)
        self.export = export

    @override
    def build_top(self, context: ctxt.Context) -> str:
        append = "export " if self.export else ""
        return append + "const " + super().build(context)

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.name.run_build(context)


# Does not inherit from Assign since expression may be None
class Var(stmt.Statement):
    def __init__(
        self,
        name: expr.ExprCandidate,
        expression: expr.ExprCandidate | None = None,
        **kwargs
    ) -> None:
        self.name = name
        self.expression = expression

    @override
    def build(self, context: ctxt.Context) -> str:
        string = "var {}".format(expr.build_expr(self.name, context))
        if self.expression is not None:
            string += " = " + expr.build_expr(self.expression, context)
        return stmt.Line(string).run_build(context)


@dataclasses.dataclass
class Ternary(expr.Expr):
    """Represents a ternary operator."""

    test: expr.Expr
    false_operand: expr.ExprCandidate
    true_operand: expr.ExprCandidate

    @override
    def build(self, context: ctxt.Context) -> str:
        return "{} ? {} : {}".format(
            self.test.run_build(context),
            expr.build_expr(self.false_operand, context),
            expr.build_expr(self.true_operand, context),
        )


def merge_maps(defaults: expr.ExprCandidate, m: expr.ExprCandidate) -> expr.Expr:
    return expr.Call("mergeMaps", defaults, m)
