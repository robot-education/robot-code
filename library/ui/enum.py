from __future__ import annotations
from abc import ABC

from typing import Generic, Iterable, Self, Type, TypeVar
from library.core import control, func, str_utils, utils, arg, map
from library.base import node, stmt, expr

__all__ = [
    "enum_factory",
    "custom_enum_factory",
    "enum_lookup_function",
    "LookupEnumValue",
]


class EnumValue(node.Node):
    def __init__(
        self,
        value: str,
        enum: _Enum,
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
        self.user_name = user_name or str_utils.value_user_name(self.value)

    def __str__(self) -> str:
        dict = {}
        if self.user_name is not None:
            dict["Name"] = self.user_name
        if self.hidden:
            dict["Hidden"] = "true"

        if dict != {}:
            return "annotation {}\n{}".format(
                str(map.Map(dict, quote_values=True)), self.value
            )
        return self.value

    def __call__(
        self,
        definition: str = "definition",
        parameter_name: str | None = None,
        invert: bool = False,
    ) -> expr.Expr:
        if parameter_name is None:
            parameter_name = self.enum.default_parameter_name
        operator = expr.Operator.NOT_EQUAL if invert else expr.Operator.EQUAL
        return expr.Compare(
            utils.definition(parameter_name, definition),
            operator,
            expr.Id("{}.{}".format(self.enum.name, self.value)),
        )


class LookupEnumValue(EnumValue):
    def __init__(self, *args, lookup_value: str, **kwargs):
        super().__init__(*args, **kwargs)
        self.lookup_value = lookup_value


T = TypeVar("T", bound=EnumValue)


class _Enum(stmt.BlockStatement):
    def __init__(
        self,
        name: str,
        *,
        parent: node.ParentNode,
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
        self.default_parameter_name = default_parameter_name or str_utils.lower_first(
            name
        )
        self.export = export

    def __str__(self) -> str:
        string = utils.export(self.export)
        string += "enum {} \n{{\n".format(self.name)
        string += self.children_str(sep=",\n", tab=True)
        return string + "\n}\n"


V = TypeVar("V", bound=EnumValue)


class EnumDict(dict[str, V], Generic[V]):
    def __init__(self, enum: _Enum, values: dict[str, V]):
        self.name = enum.name
        self.default_parameter_name = enum.default_parameter_name
        super().__init__(values)


class EnumFactoryBase(ABC):
    def __init__(
        self,
        enum_factory,
    ):
        self.enum_factory = enum_factory
        self.reset()

    def reset(self) -> None:
        self.enum = None
        self.result = {}

    def add_enum(
        self,
        name: str,
        *,
        parent: node.ParentNode,
        default_parameter_name: str | None = None,
        export: bool = True,
        value_type: Type = EnumValue,
    ) -> Self:
        self.value_factory = value_type
        self.enum = self.enum_factory(
            name,
            parent=parent,
            default_parameter_name=default_parameter_name,
            export=export,
        )
        return self

    def add_value(self, value: str, *args, **kwargs) -> Self:
        enum_value = self.value_factory(value, self.enum, *args, **kwargs)
        self.result[value] = enum_value
        return self

    def make(self) -> EnumDict:
        if self.enum is None:
            raise ValueError("add_enum must be called before make")
        if len(self.result.values()) is None:
            raise ValueError("Cannot create an enum with no values")

        self.enum.add(*self.result.values())
        enum = EnumDict(self.enum, self.result)
        self.reset()
        return enum


class EnumFactory(EnumFactoryBase):
    def __init__(self):
        super().__init__(enum_factory=_Enum)


class CustomEnumFactory(EnumFactory):
    def reset(self) -> None:
        super().reset()
        self.has_custom = False

    def add_custom(self, **kwargs) -> Self:
        if self.has_custom:
            raise ValueError("Cannot add a custom value multiple times.")
        self.add_value("CUSTOM", **kwargs)
        self.has_custom = True
        return self

    def make(self) -> EnumDict:
        if not self.has_custom:
            self.add_value("CUSTOM")
        return super().make()


enum_factory = EnumFactory()
custom_enum_factory = CustomEnumFactory()


def lookup_block(
    enum_dict: EnumDict[LookupEnumValue],
    *,
    parent: node.ParentNode,
    predicate_dict: dict[str, expr.Expr] = {},
) -> None:
    tests = []
    statements = []
    for value, enum_value in enum_dict.items():
        predicate = predicate_dict.get(value, enum_value())
        tests.append(predicate)
        lookup_value = enum_value.lookup_value
        statements.append(stmt.Return(lookup_value))

    control.if_block(tests=tests, statements=statements, parent=parent)


def enum_lookup_function(
    name: str,
    enum_dict: EnumDict[LookupEnumValue],
    *,
    parent: node.ParentNode,
    additional_arguments: Iterable[arg.Argument] = [],
    predicate_dict: dict[str, expr.Expr] = {},
    return_type: str | None = None,
) -> func.Function:
    """
    predicate_dict: A dictionary mapping enum values to expressions to use in the place of standard enum calls.
    """
    arguments: list[arg.Argument] = [arg.definition_arg]
    arguments.extend(additional_arguments)
    function = func.Function(
        name, parent=parent, arguments=arguments, return_type=return_type
    )
    lookup_block(enum_dict, parent=function, predicate_dict=predicate_dict)
    return function
