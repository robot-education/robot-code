from __future__ import annotations

from typing_extensions import override
import warnings
import enum as std_enum
from typing import Iterable, Self
from library.core import utils, arg
from library.base import ctxt, expr, node, stmt, str_utils

__all__ = [
    "Function",
    "Predicate",
    "UiPredicate",
    "UiTestPredicate",
]


class CallableType(std_enum.StrEnum):
    FUNCTION = "function"
    PREDICATE = "predicate"


class _Callable(node.TopStatement, stmt.BlockStatement, expr.Expr):
    def __init__(
        self,
        name: str,
        *,
        parent: node.ParentNode | None = None,
        callable_type: CallableType,
        arguments: Iterable[arg.Argument] = [],
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        return_type: str | None = None,
        export: bool = True,
        is_lambda: bool = False,
    ) -> None:
        super().__init__(parent=parent)
        self.add(*statements)

        self.name = name
        self.callable_type = callable_type
        self.arguments = arguments
        self.return_type = return_type
        self.export = export
        self.is_lambda = is_lambda

    def _prepare_call(self, arg_overrides: dict[str, str]) -> Iterable[str]:
        arg_dict = dict(
            (argument.name, argument.default_parameter) for argument in self.arguments
        )
        for arg_name, parameter in arg_overrides.items():
            if arg_name not in arg_dict:
                warnings.warn(
                    "{} did not match any arguments in predicate.".format(arg_name)
                )
                continue
            arg_dict[arg_name] = parameter
        return arg_dict.values()

    def __call__(self, arg_overrides: dict[str, str] = {}) -> expr.Expr:
        """Generates an expression which represents a call to the corresponding predicate or function.

        parameters: A list of tuples mapping argument names to the identifier to use.
        """
        return expr.Call(self.name, *self._prepare_call(arg_overrides))

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.__call__().run_build(context)

    def _get_start(self) -> str:
        if self.is_lambda:
            return "const {} = function".format(self.name)
        return utils.export(self.export) + self.callable_type + " " + self.name

    def _build_header(self, context: ctxt.Context) -> str:
        string = self._get_start()
        string += "({})".format(node.build_nodes(self.arguments, context))
        if self.return_type is not None:
            string += " returns " + self.return_type
        string += "\n{\n"
        return string

    def _build_footer(self) -> str:
        string = "}"
        if self.is_lambda:
            string += ";"
        string += "\n"
        return string

    @override
    def build_top(self, context: ctxt.Context) -> str:
        string = self._build_header(context)
        sep = "\n" if context.ui else ""
        string += self.build_children(context, indent=True, sep=sep)
        string += self._build_footer()
        return string


class Function(_Callable):
    def __init__(
        self,
        name: str,
        *,
        parent: node.ParentNode | None = None,
        arguments: Iterable[arg.Argument] = [],
        return_type: str | None = None,
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        export: bool = True,
        is_lambda: bool = False,
    ) -> None:
        super().__init__(
            name,
            parent=parent,
            callable_type=CallableType.FUNCTION,
            arguments=arguments,
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
        arguments: Iterable[arg.Argument] = [],
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        export: bool = True,
    ) -> None:
        super().__init__(
            name,
            parent=parent,
            callable_type=CallableType.PREDICATE,
            arguments=arguments,
            statements=statements,
            export=export,
        )

        if self.arguments == []:
            warnings.warn("Predicate has 0 arguments.")


class UiPredicate(Predicate):
    """
    A predicate defining elements for use in the UI.

    Args:
        name: The name of the predicate.
        append: A string to append to name. Defaults to "Predicate".
        statements: Statements to start the predicate with.
    """

    def __init__(
        self,
        name: str,
        parent: node.ParentNode | None = None,
        append: str = "Predicate",
        statements: Iterable[stmt.Statement | expr.Expr] = [],
    ) -> None:
        super().__init__(
            name + append,
            parent=parent,
            arguments=arg.definition_arg,
            statements=statements,
            export=True,
        )

    @override
    def build_top(self, context: ctxt.Context) -> str:
        context.ui = True
        return super().build_top(context)


class UiTestPredicateCall(expr.InlineableCall):
    def __init__(self, parent: UiTestPredicate, *args, **kwargs) -> None:
        self.parent = parent
        super().__init__(*args, **kwargs)

    @override
    def build_inline(self, context: ctxt.Context) -> str:
        result = None
        for statement in self.parent.children:
            if not isinstance(statement, stmt.Line):
                warnings.warn(
                    "Cannot inline predicate which contains statements which aren't lines."
                )
                return "<INLINE_FAILED>"
            expression = expr.add_parens(statement.expression)
            if result is None:
                result = expression
            else:
                result &= expression

        if result is None:
            warnings.warn("Cannot inline an empty predicate.")
            return "<INLINE_FAILED>"

        return expr.add_parens(result).run_build(context)


class UiTestPredicate(Predicate, expr.Expr):
    def __init__(
        self,
        name: str,
        *statements: expr.Expr,
        **kwargs,
    ) -> None:
        super().__init__(
            name, arguments=arg.definition_arg, statements=statements, **kwargs
        )

    def __call__(self, arg_overrides: dict[str, str] = {}) -> expr.Expr:
        """Generates a predicate call which will automatically inline based on context."""
        values = self._prepare_call(arg_overrides)
        return UiTestPredicateCall(self, self.name, *values)

    @override
    def build_top(self, context: ctxt.Context) -> str:
        # TODO: Remove need for three builds
        string = self._build_header(context)

        context.test_predicate = False
        standard_expr = node.build_nodes(self.children, context)
        context.test_predicate = True
        inlined_expr = node.build_nodes(self.children, context)
        # leave test_predicate to True for remaining calls

        if standard_expr == inlined_expr:
            string += self.build_children(context, indent=True)
        else:
            string += str_utils.indent(
                "/* " + standard_expr + " */\n" + self.build_children(context)
            )
        string += self._build_footer()
        return string

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.test_predicate:
            return self.__call__().run_build(context)
        return super().build(context)
