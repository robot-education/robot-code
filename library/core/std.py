import dataclasses
from typing_extensions import override
from library.base import ctxt, expr, node, str_utils, user_error
from library.core import func


class Assign(node.Node):
    def __init__(
        self,
        name: expr.ExprCandidate,
        expression: expr.ExprCandidate,
        parent: node.ParentNode | None = None,
    ) -> None:
        node.handle_parent(self, parent)
        self.name = name
        self.expression = expression

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.STATEMENT:
            string = (
                expr.build_expr(self.name, context)
                + " = "
                + expr.build_expr(self.expression, context)
                + ";\n"
            )
            return string
        return user_error.expected_scope(ctxt.Scope.STATEMENT)


class Const(Assign, expr.Expression):
    def __init__(
        self,
        name: expr.ExprCandidate,
        expression: expr.ExprCandidate,
        export: bool = False,
        **kwargs
    ) -> None:
        """
        args:
            export: Whether to mark the constant as exported. Does nothing if the constant is not a top level constant.
        """
        super().__init__(name, expression, **kwargs)
        self.export = export

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP or context.scope == ctxt.Scope.STATEMENT:
            append = (
                "export " if (self.export and context.scope == ctxt.Scope.TOP) else ""
            )
            context.scope = ctxt.Scope.STATEMENT
            return append + "const " + super().build(context)
        return expr.build_expr(self.name, context)


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
        if context.scope == ctxt.Scope.STATEMENT:
            string = "var {}".format(expr.build_expr(self.name, context))
            if self.expression is not None:
                string += " = " + expr.build_expr(self.expression, context)
            return string + ";\n"
        return user_error.expected_scope(ctxt.Scope.STATEMENT)


@dataclasses.dataclass
class Ternary(expr.Expression):
    """Represents a ternary operator.

    Args:
        parens: Whether to wrap the ternary in parentheses.
    """

    test: expr.Expression
    false_operand: expr.ExprCandidate
    true_operand: expr.ExprCandidate
    parens: bool = False

    @override
    def build(self, context: ctxt.Context) -> str:
        result = "{} ? {} : {}".format(
            self.test.run_build(context),
            expr.build_expr(self.false_operand, context),
            expr.build_expr(self.true_operand, context),
        )
        return str_utils.parens(result) if self.parens else result


def merge_maps(defaults: expr.ExprCandidate, m: expr.ExprCandidate) -> func.Call:
    return func.Call("mergeMaps", defaults, m)
