from __future__ import annotations

from typing_extensions import override
import warnings
import enum as std_enum
from typing import Iterable, Self
from library.base.expr import cast_to_expr, expr_or_stmt
from library.core import param, utils, func
from library.base import ctxt, expr, node, str_utils, user_error


class _CallableType(std_enum.StrEnum):
    FUNCTION = "function"
    PREDICATE = "predicate"


class Call(expr.Expression):
    def __init__(
        self, name: str, *args: expr.ExprCandidate, inline: bool = True
    ) -> None:
        self.name = name
        self.args = (cast_to_expr(arg) for arg in args)
        self.inline = inline

    @override
    def build(self, context: ctxt.Context) -> str:
        join_str = ", " if self.inline else ",\n"
        result = "{}({})".format(
            self.name,
            node.build_nodes(
                self.args, context, sep=join_str, scope=ctxt.Scope.EXPRESSION
            ),
        )
        return expr_or_stmt(result, context.scope)


class _Callable(node.ParentNode, expr.Expression):
    def __init__(
        self,
        name: str,
        *,
        parent: node.ParentNode | None = None,
        callable_type: _CallableType,
        parameters: Iterable[param.Parameter] = [],
        statements: Iterable[node.Node] = [],
        return_type: str | None = None,
        export: bool = True,
        is_lambda: bool = False,
    ) -> None:
        super().__init__()
        node.handle_parent(self, parent)
        self.add(*statements)
        self.name = name
        self.callable_type = callable_type
        self.parameters = parameters
        self.return_type = return_type
        self.export = export
        self.is_lambda = is_lambda

    def _get_arguments(
        self, arg_overrides: dict[str, expr.ExprCandidate]
    ) -> Iterable[expr.ExprCandidate]:
        """Constructs an iterable of arguments for a call."""
        arg_dict = dict(
            (parameter.name, parameter.default_arg) for parameter in self.parameters
        )
        for parameter_name, argument in arg_overrides.items():
            if parameter_name not in arg_dict:
                warnings.warn(
                    "{} did not match any arguments in callable {}".format(
                        parameter_name, self.name
                    )
                )
                continue
            arg_dict[parameter_name] = expr.cast_to_expr(argument)
        return arg_dict.values()

    def __call__(self, arg_overrides: dict[str, expr.ExprCandidate] = {}) -> func.Call:
        """Generates an expression which represents a call to the corresponding predicate or function.

        Args:
            arg_overrides: A dict mapping parameter names to their argument values. If a parameter is left out, its default arg will be used instead.
        """
        return func.Call(self.name, *self._get_arguments(arg_overrides))

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP:
            return self._build_callable(context)
        return self.__call__().run_build(context)

    def _build_callable(self, context: ctxt.Context) -> str:
        string = self._build_header(context)
        sep = "\n" if context.ui else ""
        string += self.build_children(
            context, indent=True, sep=sep, scope=ctxt.Scope.STATEMENT
        )
        string += self._build_footer()
        return string

    def _build_header(self, context: ctxt.Context) -> str:
        string = self._get_start()
        string += "({})".format(
            node.build_nodes(
                self.parameters, context, sep=", ", scope=ctxt.Scope.EXPRESSION
            )
        )
        if self.return_type is not None:
            string += " returns " + self.return_type
        string += "\n{\n"
        return string

    def _get_start(self) -> str:
        if self.is_lambda:
            return "const {} = function".format(self.name)
        return utils.export(self.export) + self.callable_type + " " + self.name

    def _build_footer(self) -> str:
        string = "}"
        if self.is_lambda:
            string += ";"
        string += "\n"
        return string


class Function(_Callable):
    def __init__(
        self,
        name: str,
        *,
        parent: node.ParentNode | None = None,
        parameters: Iterable[param.Parameter] = [],
        return_type: str | None = None,
        statements: Iterable[node.Node] = [],
        export: bool = False,
        is_lambda: bool = False,
    ) -> None:
        super().__init__(
            name,
            parent=parent,
            callable_type=_CallableType.FUNCTION,
            parameters=parameters,
            return_type=return_type,
            statements=statements,
            export=export,
            is_lambda=is_lambda,
        )


class Predicate(_Callable):
    def __init__(
        self,
        name: str,
        parent: node.ParentNode | None = None,
        parameters: Iterable[param.Parameter] = [],
        statements: Iterable[node.Node] = [],
        export: bool = True,
    ) -> None:
        super().__init__(
            name,
            parent=parent,
            callable_type=_CallableType.PREDICATE,
            parameters=parameters,
            statements=statements,
            export=export,
        )

        if self.parameters == []:
            warnings.warn("Predicate {} has 0 parameters".format(name))


class UiPredicate(Predicate):
    """
    A predicate defining elements for use in the UI.

    Args:
        name: The name of the predicate.
        suffix: A string to append to name. Defaults to "Predicate".
        statements: Statements to start the predicate with.
    """

    def __init__(
        self,
        name: str,
        parent: node.ParentNode | None = None,
        suffix: str = "Predicate",
        statements: Iterable[node.Node] = [],
    ) -> None:
        """
        Args:
            create_group: Wrap the group in a ParameterGroup with the same name as the parameter.
        """
        super().__init__(
            name + suffix,
            parent=parent,
            parameters=param.definition_param,
            statements=statements,
            export=True,
        )
        self.base_name = name

    def add_with_group(self, *children: node.Node) -> Self:
        """
        Adds elements to a parameter, enclosed in a ParameterGroup.
        """
        # avoid circular import
        from library.ui import parameter

        group = parameter.ParameterGroup(self.base_name).add(*children)
        return super().add(group)

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == context.scope.TOP:
            context.ui = True
        return super().build(context)


class _UiTestPredicateCall(func.Call):
    def __init__(self, parent: UiTestPredicate, *args, **kwargs) -> None:
        self.parent = parent
        super().__init__(parent.name, *args, **kwargs)

    def build_inline(self, context: ctxt.Context) -> str:
        result = None
        for expression in self.parent.children:
            expression = expr.add_parens(expression)
            if result is None:
                result = expression
            else:
                result &= expression

        if result is None:
            warnings.warn("Cannot inline an empty predicate")
            return user_error.code_message("Cannot inline empty predicate")

        return expr.add_parens(result).run_build(context, ctxt.Scope.EXPRESSION) + ";\n"

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.test_predicate:
            return self.build_inline(context)
        return super().build(context)


class UiTestPredicate(Predicate, expr.Expression):
    def __init__(
        self,
        name: str,
        *expressions: expr.Expression,
        **kwargs,
    ) -> None:
        super().__init__(
            name, parameters=param.definition_param, statements=expressions, **kwargs
        )

    def __call__(self, arg_overrides: dict[str, expr.ExprCandidate] = {}) -> func.Call:
        """Generates a predicate call which will automatically inline based on context."""
        return _UiTestPredicateCall(self, *self._get_arguments(arg_overrides))

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP:
            return self._build_ui_test_predicate(context)
        return self.__call__().run_build(context)

    def _build_ui_test_predicate(self, context: ctxt.Context) -> str:
        """Builds the predicate, adding the non-inlined version of each call before each expression as appropriate."""
        string = self._build_header(context)

        context.scope = ctxt.Scope.STATEMENT
        context.test_predicate = True
        inlined_expr = self.build_children(context)
        context.test_predicate = False
        standard_expr = self.build_children(context)

        if standard_expr == inlined_expr:
            string += str_utils.indent(standard_expr)
        else:
            string += str_utils.indent(
                "/* "
                # Build standard_expr again as expression (can't be expr first time due to comparison)
                + self.build_children(context, scope=ctxt.Scope.EXPRESSION)
                + " */\n"
                + inlined_expr
            )
        string += self._build_footer()
        return string


def ui_predicate_call(name: str, suffix: str = "Predicate") -> Call:
    """Calls a ui predicate, passing definition as an argument."""
    return Call(name + suffix, "definition")


class Return(node.Node):
    """Represents a return statement."""

    def __init__(self, expression: expr.ExprCandidate) -> None:
        self.expression = expression

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.STATEMENT:
            return "return " + expr.build_expr(self.expression, context) + ";\n"
        return user_error.expected_scope(ctxt.Scope.STATEMENT)
