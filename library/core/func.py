from typing_extensions import override
import warnings
import enum as std_enum
from typing import Iterable
from library.core import utils, arg
from library.base import expr, node, stmt

__all__ = [
    "Function",
    "Predicate",
    "UiPredicate",
    "ui_test_predicate",
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
                raise ValueError(
                    "{} did not match any arguments in predicate.".format(arg_name)
                )
            arg_dict[arg_name] = parameter

        return expr.Id("{}({})".format(self.name, ", ".join(arg_dict.values())))

    def build_call(self, context: node.Context) -> str:
        return self.__call__().build(context)

    def build_def(self, context: node.Context, sep="") -> str:
        string = utils.export(self.export) + self._get_start()
        context.type = node.NodeType.EXPRESSION
        string += "({})".format(node.build_nodes(self.arguments, context))
        if self.return_type is not None:
            string += " returns " + self.return_type
        string += "\n{\n"
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
        if context.type == node.NodeType.TOP_LEVEL:
            return self.build_def(context, **kwargs)
        elif context.type == node.NodeType.EXPRESSION:
            return self.build_call(context)
        else:
            warnings.warn(
                "Expected callable to be used as a statement or an expression."
            )
            return "<ERROR>"


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

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
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
        context.ui = True
        return super().build(context, sep="\n")


def ui_test_predicate(
    name: str, *statements: stmt.Statement | expr.Expr, **kwargs
) -> Predicate:
    return UiPredicate(name, append="", statements=statements, **kwargs)


# def test_predicate(
#     name: str,
#     *,
#     arguments: Iterable[arg.Argument] = [],
#     statements: Iterable[stmt.Statement | expr.Expr],
#     parent: base.ParentNode,
#     export: bool = True,
# ) -> expr.Expr:
#     """Adds a predicate to parent with the given statements.

#     Returns an expression which calls the predicate.
#     """
#     return Predicate(
#         name, parent=parent, arguments=arguments, statements=statements, export=export
#     )()
