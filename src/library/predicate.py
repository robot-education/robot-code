from __future__ import annotations

from typing import Self
from typing_extensions import override
from src.library import base, statement, expr


class Arguments(base.InlineNode):
    def __init__(self, *arguments: Argument):
        self.arguments = arguments

    def __len__(self) -> int:
        return len(self.arguments)

    @override
    def enter(self) -> str:
        return ", ".join(base.enter(*self.arguments))


class Argument(Arguments):
    def __init__(self, name: str, type: str | None = None, add_type: bool = True):
        self.name = name
        if not add_type:
            self.type = None
        else:
            if self.type == None:
                self.type = self.name.capitalize()
            else:
                self.type = type

    def __len__(self) -> int:
        return 1

    @override
    def enter(self) -> str:
        if self.type is None:
            return self.name
        return "{} is {}".format(self.name, self.type)


dummy_argument = Arguments()

definition = Argument("definition", "map")
id = Argument("id")
context = Argument("context")
feature = Arguments(context, id, definition)


class Predicate(base.Node):
    def __init__(
        self,
        name: str,
        arguments: Arguments = Arguments(),
        statements: statement.Statements = statement.Statements(),
    ):
        self.name = name
        self.arguments = arguments
        self.statements = []

    def __add__(self, statement: base.Node) -> Self:
        self.statements.append(statement)
        return self

    def use(self, *parameters: str) -> expr.BoolExpr:
        if len(parameters) != len(self.arguments):
            raise ValueError(
                "Expected {} parameters, recieved {}".format(
                    len(self.arguments), len(parameters)
                )
            )

        return expr.BoolExpr(", ".join(parameters))

    @override
    def enter(self) -> str:
        return "predicate {}({})\n{\n".format(
            name=self.name, args=self.arguments.enter()
        )

    @override
    def exit(self) -> str:
        return "};\n"


class UiPredicate(Predicate):
    def __init__(self, name: str):
        super().__init__(name, arguments=definition)
