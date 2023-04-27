import warnings
from typing import Iterable
from library import arg, base, expr, stmt, utils

__all__ = [
    "Predicate",
    "UiPredicate",
    "UiTestPredicate",
]


class Function(stmt.BlockStatement):
    def __init__(
        self,
        name: str,
        *,
        parent: base.ParentNode,
        arguments: Iterable[arg.Argument] = [],
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        export: bool = True,
        is_lambda: bool = False,
    ) -> None:
        pass


class Predicate(stmt.BlockStatement):
    def __init__(
        self,
        name: str,
        *,
        parent: base.ParentNode,
        arguments: Iterable[arg.Argument] = [],
        statements: Iterable[stmt.Statement | expr.Expr] = [],
        export: bool = True,
    ) -> None:
        super().__init__(parent=parent)
        self.name = name
        self.arguments = arguments
        self.add(*statements)
        self.export = export

        if self.arguments == []:
            warnings.warn("Predicate has 0 arguments.")

    def __call__(self, *args: dict[str, str]) -> expr.Expr:
        """Creates a predicate call.

        parameters: A list of tuples mapping argument names to the identifier to use.
        """
        arg_dict = dict(
            (argument.name, argument.default_parameter) for argument in self.arguments
        )
        for arg_name, parameter in args:
            if arg_name not in arg_dict:
                raise ValueError(
                    "{} did not match any arguments in predicate.".format(arg_name)
                )
            arg_dict[arg_name] = parameter

        return expr.Id("{}({})".format(self.name, ", ".join(arg_dict.values())))

    def __str__(self) -> str:
        string = utils.export(self.export)
        string += "predicate {}({})\n{{\n".format(
            self.name, utils.to_str(self.arguments)
        )
        string += self.children_str(tab=True, sep="\n")
        string += " }\n"
        return string


class UiPredicate(Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
    """

    def __init__(
        self,
        name: str,
        *statements: stmt.Statement | expr.Expr,
        **kwargs,
    ) -> None:
        super().__init__(
            name + "Predicate",
            arguments=arg.definition_arg,
            statements=statements,
            **kwargs,
        )


class UiTestPredicate(Predicate):
    def __init__(
        self, name: str, *statements: stmt.Statement | expr.Expr, **kwargs
    ) -> None:
        super().__init__(
            name, arguments=arg.definition_arg, statements=statements, **kwargs
        )
