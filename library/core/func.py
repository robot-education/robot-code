from typing_extensions import override
import warnings
import enum as std_enum
from typing import Iterable
from library.core import utils, arg
from library.base import ctxt, expr, node, stmt

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

    def __call__(self, arg_overrides: dict[str, str] = {}) -> expr.Expr:
        """Generates an expression which represents a call to the corresponding predicate or function.

        parameters: A list of tuples mapping argument names to the identifier to use.
        """
        arg_dict = dict(
            (argument.name, argument.default_parameter) for argument in self.arguments
        )
        for arg_name, parameter in arg_overrides:
            if arg_name not in arg_dict:
                warnings.warn(
                    "{} did not match any arguments in predicate".format(arg_name)
                )
                continue
            arg_dict[arg_name] = parameter

        return expr.Call(self.name, *arg_dict.values())

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.__call__().build(context)

    def _get_start(self) -> str:
        if self.is_lambda:
            return "const {} = function".format(self.name)
        return self.callable_type + " " + self.name

    @override
    def build_top(self, context: ctxt.Context) -> str:
        string = utils.export(self.export) + self._get_start()
        string += "({})".format(node.build_nodes(self.arguments, context))
        if self.return_type is not None:
            string += " returns " + self.return_type
        string += "\n{\n"
        sep = "\n" if context.ui else ""
        string += self.build_children(context, indent=True, sep=sep)
        string += "}"
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

    def build_inline_call(self, context: ctxt.Context) -> str:
        result = None
        for statement in self.children:
            if not isinstance(statement, stmt.Line):
                warnings.warn("Cannot inline predicate which contains non-lines")
                return "<INLINE_FAILED>"
            expression = expr.add_parens(statement.expression)
            if result is None:
                result = expression
            else:
                result &= expression
        if result is None:
            warnings.warn("Cannot inline empty predicate")
            return "<INLINE_FAILED>"

        return expr.add_parens(result).build(context)

    @override
    def build_top(self, context: ctxt.Context) -> str:
        context.test_predicate = True
        context.ui = True
        return super().build_top(context)

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.ui and context.test_predicate:
            return self.build_inline_call(context)
        return super().build(context)
