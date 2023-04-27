from __future__ import annotations

from typing import Generic, Self, TypeVar
from library import annotation, base, expr, stmt, utils
from library import control

__all__ = [
    "EnumFactory",
    "LookupEnumFactory",
    "lookup_function",
]


class EnumValue(base.Node):
    def __init__(
        self,
        value: str,
        enum: Enum,
        user_name: str | None = None,
        hidden: bool = False,
    ) -> None:
        """A possible value of an enum.

        value: The value of the enum. Should be all-caps seperated by underscores.
        user_name: A user facing name.
        hidden: Whether to mark the enum value as hidden. Has no effect if the enum is not ui.
        """
        self.value = value.upper()
        self.hidden = hidden
        self.enum = enum

        if user_name is None:
            self.user_name = self.make_user_name()
        else:
            self.user_name = user_name

    def make_user_name(self) -> str:
        words = self.value.lower().split(sep="_")
        words[0] = words[0].capitalize()
        return " ".join(words)

    def __str__(self) -> str:
        dict = {}
        if self.user_name is not None:
            dict["Name"] = self.user_name
        if self.hidden:
            dict["Hidden"] = "true"

        if dict != {}:
            return "annotation {}\n {}".format(
                str(annotation.Map(dict, quote_values=True)), self.value
            )
        return self.value

    def camel_case(self, capitalize: bool = False) -> str:
        words = self.make_user_name().split()
        words = [word.capitalize() for word in words]
        result = "".join(words)
        if capitalize:
            return result
        return utils.lower_first(result)

    def __call__(
        self,
        parameter_name: str | None = None,
        definition: str = "definition",
        invert: bool = False,
    ) -> expr.Expr:
        if parameter_name is None:
            parameter_name = self.enum.default_parameter_name
        operator = expr.Operator.NOT_EQUAL if invert else expr.Operator.EQUAL
        return expr.Compare(
            expr.Id(utils.definition(parameter_name, definition=definition)),
            operator,
            expr.Id("{}.{}".format(self.enum.name, self.value)),
        )


T = TypeVar("T", bound=EnumValue)


class Enum(stmt.BlockStatement, Generic[T], dict[str, T]):
    def __init__(
        self,
        name: str,
        *,
        parent: base.ParentNode,
        default_parameter_name: str | None = None,
        export: bool = True,
    ) -> None:
        """An enum.

        name: A capital case (LikeThis) string.
        values: A list of strings which are used to construct enum values. EnumValues may also be registered afterwards.
        default_parameter_name: A default parameter name to use. If not specified, the default is generated automatically by lowercasing the first letter of name.
        """
        super().__init__(parent=parent)

        self.name = name
        self.default_parameter_name = (
            utils.lower_first(name)
            if default_parameter_name is None
            else default_parameter_name
        )
        self.export = export

    def add(self, *enum_values: T) -> Self:
        for enum_value in enum_values:
            self[enum_value.value] = enum_value
        return self

    def __str__(self) -> str:
        string = utils.export(self.export)
        string += "enum {} \n{{\n".format(self.name)
        string += utils.to_str(self.values(), sep=",\n", tab=True)
        return string + "\n}\n"


class EnumFactory(Enum):
    def add_value(
        self, value: str, user_name: str | None = None, hidden: bool = False, **kwargs
    ) -> Self:
        enum_value = EnumValue(
            value, self, user_name=user_name, hidden=hidden, **kwargs
        )
        self.add(enum_value)
        return self

    def add_custom(self, **kwargs) -> Self:
        return self.add_value("CUSTOM", **kwargs)


class LookupEnumValue(EnumValue):
    def __init__(self, *args, lookup_value: str, **kwargs):
        self.lookup_value = lookup_value
        super().__init__(*args, **kwargs)


class LookupEnum(Enum[LookupEnumValue]):
    """Defines an enum with a default value for each value.

    A corresponding constant map and lookup function may then be automatically
    generated which returns the values according to when the function is accessed.
    """

    pass


class LookupEnumFactory(EnumFactory, LookupEnum):
    def add_value(self, value: str, **kwargs) -> Self:
        enum_value = LookupEnumValue(value, self, **kwargs)
        self.add(enum_value)
        return self


def lookup_function(
    enum: LookupEnum,
    *,
    parent: base.ParentNode,
    predicate_dict: dict[str, expr.Expr] = {},
) -> None:
    """
    predicate_dict: A dictionary mapping enum values to expressions to use in the place of standard enum calls.
    """
    tests = []
    statements = []
    for value, enum_value in enum.items():
        predicate = predicate_dict.get(value, enum_value())
        tests.append(predicate)
        lookup_value = enum_value.lookup_value
        statements.append(stmt.Line("return " + lookup_value))

    control.if_block(tests=tests, statements=statements, parent=parent)
