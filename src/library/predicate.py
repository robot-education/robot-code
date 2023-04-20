from typing import Iterator, Self
from typing_extensions import override
from src.library import base, expr, statement


class Argument(base.Node):
    def __init__(
        self,
        name: str,
        type: str | None = None,
        use_name_as_type: bool = True,
        default_parameter: str | None = None,
    ):
        """An argument to a predicate or function.

        type: The type of the argument.
        use_name_as_type: If `True` and type is `None`, then `name` is capitalized and used as `type` automatically.
        default_parameter: A string to use as the default parameter when this function is called. If None, name is used.
        """
        self.name = name
        self.default_parameter = (
            default_parameter if default_parameter is not None else self.name
        )

        if not use_name_as_type:
            self.type = None
        else:
            self.type = self.name.capitalize() if type is None else type

    def __len__(self) -> int:
        return 1

    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()

    def __str__(self) -> str:
        if self.type is None:
            return self.name
        return "{} is {}".format(self.name, self.type)


class Arguments(base.Node):
    def __init__(self, *arguments: Argument):
        self.arguments = arguments

    def __len__(self) -> int:
        return len(self.arguments)

    def __iter__(self) -> Iterator[Argument]:
        return self.arguments.__iter__()

    def __str__(self) -> str:
        return ", ".join(base.enter(*self.arguments))


dummy_argument = Arguments()

definition = Argument("definition", "map")
id = Argument("id")
context = Argument("context")
feature = Arguments(context, id, definition)


class Predicate(base.Node):
    def __init__(self, name: str, arguments: Argument | Arguments = Arguments()):
        self.name = name
        self.arguments = arguments
        self.statements = []

    def __add__(
        self, state: statement.Statements | statement.Statement | expr.Expr
    ) -> Self:
        if isinstance(state, expr.Expr):
            state = statement.Statement(state)
        self.statements.append(state)
        return self

    def call(self, *parameters: tuple[str, str]) -> expr.Expr:
        """Creates a predicate call.

        parameters: A list of tuples mapping argument names to the identifier to use.
        """
        arg_dict = dict(
            (argument.name, argument.default_parameter) for argument in self.arguments
        )
        for arg_name, parameter in parameters:
            if arg_dict.get(arg_name, None) == None:
                raise ValueError(
                    "{} did not match to any arguments to predicate.".format(arg_name)
                )
            arg_dict[arg_name] = parameter

        return expr.Id("{}({})".format(self.name, ", ".join(arg_dict.values())))

    def __str__(self) -> str:
        string = "predicate {}({})".format(self.name, str(self.arguments))
        string += " {\n"
        string += "".join("\t" + str(statement) for statement in self.statements)
        string += "};\n"
        return string


class UiPredicate(Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always appended.
    """

    def __init__(self, name: str):
        super().__init__(name + "Predicate", arguments=definition)
