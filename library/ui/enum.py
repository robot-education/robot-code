from __future__ import annotations
from abc import ABC
import string
from typing import Any, Generic, Iterable, Self, Type, TypeVar
from typing_extensions import override

import copy
import warnings

from library.core import control, func, utils, arg, map
from library.base import ctxt, node, stmt, expr, str_utils

__all__ = [
    "EnumFactory",
    "Enum",
    "enum_lookup_function",
    "LookupEnumValue",
]


class EnumValue(expr.Expr):
    def __init__(
        self,
        value: str,
        enum: Enum,
        annotate: bool = True,
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
        self.enum.add(self)
        self.annotate = annotate
        self.user_name = user_name or str_utils.value_user_name(self.value)
        self.invert = False
        self.predicate = None

    def predicate_name(self, name_template: str) -> str:
        """Generates a name for a predicate based on the enum value.

        Args:
            name_template:
                A template for the name. May contain the substitutions {name} and {value},
                which will be replaced with the formatted names of the enum and the value, respectively.
        """

        format_dict = {}
        first = True
        for tuple in string.Formatter().parse(name_template):
            if tuple[1] == "name":
                if first and tuple[0] == "":
                    format_dict["name"] = str_utils.lower_first(self.enum.name)
                else:
                    format_dict["name"] = self.enum.name
            elif tuple[1] == "value":
                format_dict["value"] = str_utils.camel_case(
                    self.value, capitalize=not (first and tuple[0] == "")
                )
            else:
                warnings.warn(
                    "Invalid name_template: Expected substitutions to be {name} or {value} only."
                )
            first = False

        return name_template.format(**format_dict)

    def register_predicate(self, predicate: func.UiTestPredicate) -> Self:
        self.predicate = predicate
        return self

    @override
    def __invert__(self) -> expr.Expr:
        value = copy.copy(self)
        value.invert = not value.invert
        return value

    def __call__(
        self,
        definition: str = "definition",
        parameter_name: str | None = None,
        invert: bool = False,
    ) -> expr.Expr:
        """Represents a call to the enum which tests its value.

        Args:
            definition: A different name for the definition map.
            parameter_name: The name of the enum parameter. Defaults to `default_parameter_name`.
            invert: Invert the call. Used by functions such as `any`.
        """
        if parameter_name == None:
            parameter_name = self.enum.default_parameter_name
        if invert:
            self.invert = not self.invert

        # can only use predicate if everything is default since ui test predicate doesn't have a call method
        if self.predicate != None and parameter_name != None:
            call = self.predicate.__call__({"definition": definition})
            return ~call if self.invert else call

        operator = expr.Operator.NOT_EQUAL if self.invert else expr.Operator.EQUAL

        return expr.Equal(
            utils.definition(parameter_name, definition),
            operator,
            "{}.{}".format(self.enum.name, self.value),
        )

    def build_value(self, context: ctxt.Context) -> str:
        dict = {}
        if self.user_name is not None:
            dict["Name"] = self.user_name
        if self.hidden:
            dict["Hidden"] = "true"

        if self.annotate and dict != {}:
            return "annotation {}\n{}".format(
                map.Map(dict, quote_values=True).run_build(context), self.value
            )
        return self.value

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.enum:
            return self.build_value(context)
        return self.__call__().run_build(context)


class LookupEnumValue(EnumValue):
    def __init__(self, *args, lookup_value: str, **kwargs):
        super().__init__(*args, **kwargs)
        self.lookup_value = lookup_value


V = TypeVar("V", bound=EnumValue)


# Avoid block parent since that has automatic expr->statement conversion
class Enum(
    node.TopStatement, node.ChildNode, node.ParentNode, dict[str, V], Generic[V]
):
    def __init__(
        self,
        name: str,
        parent: node.ParentNode | None = None,
        default_parameter_name: str | None = None,
        export: bool = True,
    ) -> None:
        """An enum.

        name: A capital case (LikeThis) string.
        values: A list of strings which are used to construct enum values. EnumValues may also be registered afterwards.
        default_parameter_name: A default parameter name to use. If not specified, the default is generated automatically by lowercasing the first letter of name.
        """
        super().__init__(parent=parent)
        dict.__init__(self)

        self.name = name
        self.default_parameter_name = default_parameter_name or str_utils.lower_first(
            name
        )
        self.export = export

    @override
    def add(self, value: V) -> Self:
        self[value.value] = value
        return super().add(value)

    @override
    def build_top(self, context: ctxt.Context) -> str:
        context.enum = True
        string = utils.export(self.export) + "enum {} \n{{\n".format(self.name)
        string += self.build_children(context, sep=",\n", indent=True)
        return string + "\n}\n"


class EnumFactory:
    def __init__(
        self,
        name: str,
        parent: node.ParentNode | None = None,
        default_parameter_name: str | None = None,
        export: bool = True,
        annotate: bool = True,
        enum_type: Type[Enum] = Enum,
        value_type: Type[EnumValue] = EnumValue,
        generate_predicates: bool = False,
        predicate_name_template: str = "is{name}{value}",
    ) -> None:
        """Begins the construction of an enum.

        Args:
            name: The name of the enum. Should be in capital case.
            parent: The parent of the enum.
            default_parameter_name: The default name of the enum parameter.
            generate_predicates: Whether to also generate predicates for each value.
            predicate_name_template: The default template to use when generating predicate names.
        """
        self.parent = parent  # save parent so predicates can also register
        self.value_type = value_type
        self.annotate = annotate
        self.generate_predicates = generate_predicates
        self.predicate_name_template = predicate_name_template

        self.has_custom = False
        self.enum = enum_type(
            name,
            parent=parent,
            default_parameter_name=default_parameter_name,
            export=export,
        )

    def add_value(
        self,
        value: str,
        user_name: str | None = None,
        generate_predicate: bool | None = None,
        name_template: str | None = None,
        **kwargs,
    ) -> Self:
        if self.enum is None:
            raise ValueError("add_enum must be called before add_value")

        # value adds itself to enum
        enum_value = self.value_type(
            value, self.enum, user_name=user_name, annotate=self.annotate, **kwargs
        )

        # or operator handles None case
        if generate_predicate or self.generate_predicates:
            predicate = func.UiTestPredicate(
                enum_value.predicate_name(
                    name_template or self.predicate_name_template
                ),
                enum_value(),
                parent=self.parent,
            )
            enum_value.register_predicate(predicate)

        return self

    def add_custom(self, **kwargs) -> Self:
        if self.has_custom:
            raise ValueError("Cannot add a custom value multiple times")
        self.add_value("CUSTOM", **kwargs)
        self.has_custom = True
        return self

    def make(self) -> Enum[Any]:
        if self.enum is None:
            raise ValueError("add_enum must be called before make")
        elif len(self.enum) < 1:
            raise ValueError("Cannot create an enum with no values")

        return self.enum


def lookup_block(
    enum_dict: Enum[LookupEnumValue],
    *,
    parent: node.ParentNode,
) -> control.IfBlock:
    """Constructs an if block which accesses each lookup value in `enum_dict`."""
    tests = []
    statements = []
    for enum_value in enum_dict.values():
        tests.append(enum_value)
        lookup_value = enum_value.lookup_value
        statements.append(stmt.Return(lookup_value))
    return control.make_if_block(tests=tests, statements=statements, parent=parent)


def enum_lookup_function(
    name: str,
    enum_dict: Enum[LookupEnumValue],
    parent: node.ParentNode | None = None,
    additional_arguments: Iterable[arg.Argument] = [],
    return_type: str | None = None,
    export: bool = False,
) -> func.Function:
    """
    Args:
        return_type: The return type of the function.
        predicate_dict: A dictionary mapping enum values to expressions to use in the place of standard enum calls.
    """
    arguments: list[arg.Argument] = [arg.definition_arg]
    arguments.extend(additional_arguments)
    function = func.Function(
        name,
        parent=parent,
        arguments=arguments,
        return_type=return_type,
        export=export,
    )
    lookup_block(enum_dict, parent=function)
    return function
