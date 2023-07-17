from dataclasses import dataclass
import dataclasses
from typing_extensions import override
from library.base import ctxt, expr, node

__all__ = ["Assign", "Const", "Var", "Ternary", "merge_maps"]


class Assign(node.Node):
    def __init__(
        self,
        name: expr.ExprCandidate,
        expression: expr.ExprCandidate,
        parent: node.ParentNode | None = None,
    ) -> None:
        node.handle_parent(self, parent)
        self.name = expr.cast_to_expr(name)
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        old_scope = context.scope
        context.scope = ctxt.Scope.EXPRESSION
        string = (
            self.name.run_build(context) + " = " + self.expression.run_build(context)
        )
        return expr.expr_or_stmt(string, old_scope)


class Const(Assign, expr.Expression):
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
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP:
            append = "export " if self.export else ""
            context.scope = ctxt.Scope.STATEMENT
        else:
            append = ""
        return append + "const " + super().build(context)


# Does not inherit from Assign since expression may be None
class Var(node.Node):
    def __init__(
        self,
        name: expr.ExprCandidate,
        expression: expr.ExprCandidate | None = None,
    ) -> None:
        self.name = name
        self.expression = expression

    @override
    def build(self, context: ctxt.Context) -> str:
        string = "var {}".format(expr.build_expr(self.name, context))
        if self.expression is not None:
            string += " = " + expr.build_expr(self.expression, context)
        return string + ";\n"


@dataclasses.dataclass
class Ternary(expr.Expression):
    """Represents a ternary operator."""

    test: expr.Expression
    false_operand: expr.ExprCandidate
    true_operand: expr.ExprCandidate

    @override
    def build(self, context: ctxt.Context) -> str:
        return "{} ? {} : {}".format(
            self.test.run_build(context),
            expr.build_expr(self.false_operand, context),
            expr.build_expr(self.true_operand, context),
        )


def merge_maps(defaults: expr.ExprCandidate, m: expr.ExprCandidate) -> expr.Expression:
    return expr.Call("mergeMaps", defaults, m)


class Return(node.Node):
    def __init__(self, expression: expr.Expression | str) -> None:
        self.expression = expr.cast_to_expr(expression)

    @override
    def build(self, context: ctxt.Context) -> str:
        return "return " + self.expression.run_build(context) + ";\n"
