from abc import ABC
import re
from typing import Iterable, Sequence
from library import predicate, argument, base, enum, expr, stmt, ui_hint, utils

__all__ = [
    "definition_arg",
    "id_arg",
    "context_arg",
    "feature_args",
    "EnumAnnotation",
    "UiPredicate",
    "equal",
    "not_equal",
    "any",
    "not_any",
    "make_enum_test_predicate",
]

definition_arg = argument.Argument("definition", "map")
id_arg = argument.Argument("id")
context_arg = argument.Argument("context")
feature_args = [context_arg, id_arg, definition_arg]


class Annotation(stmt.Statement, ABC):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: Sequence[ui_hint.UiHint] = [],
        args: dict[str, str] = {},
    ) -> None:
        """
        A dict containing additional strings to add to the annotation map.
        """
        self.parameter_name = parameter_name

        if user_name is None:
            self.user_name = utils.user_name(self.parameter_name)
        else:
            self.user_name = user_name

        # always put name and ui hints first
        map_args = {"Name": self.user_name}
        if len(ui_hints) > 0:
            map_args["UIHint"] = "[{}]".format(", ".join(ui_hints))
        map_args.update(args)

        self.map = base.Map(map_args, quote_values=True, exclude_keys="UIHint")

    def __str__(self) -> str:
        return "annotation " + str(self.map) + "\n"


class ValueAnnotation(Annotation, ABC):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    # def __init__(self, *args, **kwargs):
    #     super().__init__(*args, **kwargs)


class TypeAnnotation(Annotation, ABC):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(self, parameter_name: str, type: str | None = None, **kwargs) -> None:
        super().__init__(parameter_name, **kwargs)

        if type is None:
            self.type = self.parameter_name
        else:
            self.type = type

    def __str__(self) -> str:
        return super().__str__() + "definition.{} is {};\n".format(
            self.parameter_name, self.type
        )


class EnumAnnotation(TypeAnnotation):
    def __init__(
        self,
        enum: enum.Enum,
        default: str | None = None,
        parameter_name: str | None = None,
        **kwargs
    ):
        self.enum = enum
        parameter_name = (
            enum.default_parameter_name if parameter_name is None else parameter_name
        )

        # if kwargs["ui_hints"].show_label and kwargs["ui_hints"].horizontal_enum:
        #     warnings.warn("show_label and horizontal enum don't work together.")

        args = {}
        if default is not None:
            args["Default"] = default

        super().__init__(parameter_name, type=self.enum.name, args=args, **kwargs)


class UiPredicate(predicate.Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
    """

    def __init__(self, name: str, export: bool = True):
        super().__init__(name + "Predicate", arguments=definition_arg, export=export)


# class UiTestPredicate(predicate.Predicate):
#     def __init__(self, name: str, **kwargs):
#         super().__init__(name, arguments=definition_arg, **kwargs)


def equal(
    value: enum.EnumValue,
    parameter_name: str | None = None,
    definition: str = "definition",
) -> expr.Expr:
    """Generates an expression which tests whether this parameter matches the value."""
    if parameter_name is None:
        parameter_name = value.enum.default_parameter_name
    return expr.Compare(
        expr.Id(utils.definition(definition, parameter_name)),
        expr.Operator.EQUAL,
        expr.Id("{}.{}".format(value.enum.name, value.value)),
    )


def not_equal(
    self, parameter_name: str, value: enum.EnumValue, definition: str = "definition"
) -> expr.Expr:
    """Generates an expression which tests whether this parameter does not match value."""
    return expr.Compare(
        expr.Id(utils.definition(definition, parameter_name)),
        expr.Operator.NOT_EQUAL,
        expr.Id("{}.{}".format(self.enum.name, value)),
    )


def any(self, *values: enum.EnumValue) -> expr.Expr:
    """Generates an expression which is true when this parameter matches any value in values."""
    expression = self.equal(values[0])
    for value in values[1:]:
        expression |= self.equal(value)
    return expr.Parens(expression)


def not_any(self, *values: enum.EnumValue) -> expr.Expr:
    """Generates an expression which is true when this parameter doesn't match any value in values."""
    expression = self.not_equal(values[0])
    for value in values[1:]:
        expression &= self.not_equal(value)
    return expr.Parens(expression)


def make_enum_test_predicate(
    value: enum.EnumValue,
    parameter_name: str | None = None,
    name_prepend: str = "is",
    name_append: str = "",
    export: bool = True,
) -> predicate.Predicate:
    name = name_prepend + value.camel_case(capitalize=True) + name_append
    return predicate.Predicate(
        name,
        arguments=definition_arg,
        statements=equal(value, parameter_name=parameter_name),
        export=export,
    )
