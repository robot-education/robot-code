import warnings
from typing import Iterable
from src.library import base, expr, argument, stmt, utils


class Predicate(base.ParentNode[stmt.Statement]):
    def __init__(
        self,
        name: str,
        arguments: Iterable[argument.Argument] = [],
        statements: Iterable[stmt.Statement] = [],
        export: bool = True,
    ):
        super().__init__(*statements)
        self.name = name
        self.arguments = arguments
        self.export = export

        if self.arguments == []:
            warnings.warn("Predicate has 0 arguments.")

    def add(self, statement: expr.Expr | stmt.Statement) -> stmt.Statement:
        if isinstance(statement, expr.Expr):
            statement = stmt.Statement(statement)
        super().add(statement)
        return statement

    def call(self, *args: dict[str, str]) -> expr.Expr:
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
        string += "predicate {}({})\n{{\n".format(self.name, str(self.arguments))
        string += utils.to_str(self.child_nodes, tab=True)
        string += " }\n"
        return string
