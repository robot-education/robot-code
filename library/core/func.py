from typing_extensions import override
import warnings
import enum as std_enum
from typing import Iterable
from library.core import utils, arg
from library.base import expr, msg, node, stmt

__all__ = [
    "Function",
    "Predicate",
    "UiPredicate",
    "TestPredicate",
]


class CallableType(std_enum.StrEnum):
    FUNCTION = "function"
    PREDICATE = "predicate"


class _Callable(stmt.BlockStatement, expr.Expr):
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

        return expr.Id("{}({})".format(self.name, ", ".join(arg_dict.values())))

    def build_def(self, context: node.Context, sep="") -> str:
        context.set_expression()
        string = utils.export(self.export) + self._get_start()
        string += "({})".format(node.build_nodes(self.arguments, context))
        if self.return_type is not None:
            string += " returns " + self.return_type
        string += "\n{\n"
        context.set_statement()
        string += self.build_children(context, indent=True, sep=sep)
        string += "}"
        if self.is_lambda:
            string += ";"
        string += "\n"
        return string

    def _get_start(self) -> str:
        if self.is_lambda:
            return "const {} = function".format(self.name)
        return self.callable_type + " " + self.name

    @override
    def build(self, context: node.Context, **kwargs) -> str:
        if context.is_definition():
            return self.build_def(context, **kwargs)
        elif context.is_expression():
            return self.__call__().build(context)
        return msg.warn_context(msg.ContextType.EXPRESSION, msg.ContextType.DEFINITION)


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
        *,
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
        append: str = "Predicate",
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        **kwargs,
    ) -> None:
        super().__init__(
            name + append,
            arguments=arg.definition_arg,
            statements=statements,
            **kwargs,
        )

    @override
    def build(self, context: node.Context) -> str:
        if context.is_expression() and not context.ui:
            msg.warn_context("ui predicate body")
        context.ui = True
        return super().build(context, sep="\n")


class TestPredicate(Predicate):
    def __init__(
        self,
        name: str,
        *statements: expr.Expr,
        arguments: Iterable[arg.Argument] = arg.definition_arg,
        **kwargs,
    ) -> None:
        super().__init__(name, arguments=arguments, statements=statements, **kwargs)

    def build_inline_call(self, context: node.Context) -> str:
        result = None
        for statement in self.children:
            if not isinstance(statement, stmt.Line):
                warnings.warn("Cannot inline predicate which contains statements")
                return msg.ERROR_SNIPPET
            expression = expr.add_parens(statement.expression)
            if result is None:
                result = expression
            else:
                result &= expression
        if result is None:
            warnings.warn("Empty predicate")
            return msg.ERROR_SNIPPET

        return result.build(context)

    @override
    def build(self, context: node.Context) -> str:
        if context.is_expression() and context.test_predicate:
            return self.build_inline_call(context)

        context.test_predicate = True
        return super().build(context)
