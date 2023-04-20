from typing import Self
from src.library import base, expr, argument, stmt


class Predicate(stmt.Statement):
    def __init__(
        self,
        name: str,
        arguments: argument.Argument | argument.Arguments = argument.Arguments(),
        export: bool = True,
    ):
        self.name = name
        self.arguments = arguments
        self.statements = []
        self.export = export

    def __add__(self, statement: expr.Expr | stmt.Statement) -> Self:
        if isinstance(statement, expr.Expr):
            statement = stmt.Statement(statement)
        self.statements.append(statement)
        return self

    def call(self, *parameters: tuple[str, str]) -> expr.Expr:
        """Creates a predicate call.

        parameters: A list of tuples mapping argument names to the identifier to use.
        """
        arg_dict = dict(
            (argument.name, argument.default_parameter) for argument in self.arguments
        )
        for arg_name, parameter in parameters:
            if arg_name not in arg_dict:
                raise ValueError(
                    "{} did not match any arguments in predicate.".format(arg_name)
                )
            arg_dict[arg_name] = parameter

        return expr.Id("{}({})".format(self.name, ", ".join(arg_dict.values())))

    def __str__(self) -> str:
        string = base.export(self.export)
        string += "predicate {}({})\n{{\n".format(self.name, str(self.arguments))
        string += "".join(base.tab(str(statement)) for statement in self.statements)
        string += " }\n"
        return string
