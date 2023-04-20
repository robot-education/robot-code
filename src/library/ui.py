import enum as _enum

from abc import ABC
from typing import Any, Literal, Self, cast
import warnings
from src.library import predicate, argument, base, enum, expr, stmt

definition = argument.Argument("definition", "map")
id = argument.Argument("id")
context = argument.Argument("context")
feature = argument.Arguments(context, id, definition)


class UiHint(_enum.StrEnum):
    ALWAYS_HIDDEN = '"ALWAYS_HIDDEN"'
    READ_ONLY = '"READ_ONLY"'
    UNCONFIGURABLE = '"UNCONFIGURABLE"'
    REMEMBER_PREVIOUS_VALUE = '"REMEMBER_PREVIOUS_VALUE"'
    HORIZONTAL_ENUM = '"HORIZONTAL_ENUM"'
    SHOW_LABEL = '"SHOW_LABEL"'
    SHOW_EXPRESSION = '"SHOW_EXPRESSION"'
    OPPOSITE_DIRECTION = '"OPPOSITE_DIRECTION"'
    OPPOSITE_DIRECTION_CIRCULAR = '"OPPOSITE_DIRECTION_CIRCULAR"'


BaseUiHints = list[
    Literal[UiHint.ALWAYS_HIDDEN, UiHint.READ_ONLY, UiHint.UNCONFIGURABLE]
]


def cast_hints(ui_hints: list[Any]) -> list[UiHint]:
    return cast(list[UiHint], ui_hints)


class Annotation(stmt.Statement, ABC):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[UiHint] = [],
        args: list[tuple[str, str]] = [],
    ) -> None:
        self.parameter_name = parameter_name

        if user_name is None:
            self.user_name = self.parameter_name
        else:
            self.user_name = user_name

        map = dict(*args)
        map["Name"] = base.quote(self.user_name)

        if len(ui_hints) > 0:
            map["UIHint"] = "[{}]".format(", ".join(ui_hints))

        self.map = base.Map(map)

    def __str__(self) -> str:
        return "annotation " + self.map + "\n"


class ValueAnnotation(Annotation, ABC):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[UiHint] = [],
    ):
        super().__init__(parameter_name, user_name=user_name, ui_hints=ui_hints)


class TypeAnnotation(Annotation, ABC):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[UiHint] = [],
        args: list[tuple[str, str]] = [],
        type: str | None = None,
    ) -> None:
        super().__init__(
            parameter_name, user_name=user_name, ui_hints=ui_hints, args=args
        )

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
        parameter_name: str | None = None,
        user_name: str | None = None,
        base_hints: BaseUiHints = [],
        horizontal: bool = False,
        remember_previous_value: bool = True,
        show_label: bool = False,
        default: str | None = None,
    ):
        self.enum = enum

        if parameter_name is None:
            self.parameter_name = enum.name[0].lower() + enum.name[1:]
        else:
            self.parameter_name = parameter_name

        ui_hints = cast_hints(base_hints)

        if show_label and horizontal:
            warnings.warn("show_label and horizontal don't work together.")

        if horizontal:
            ui_hints.append(UiHint.HORIZONTAL_ENUM)
        if remember_previous_value:
            ui_hints.append(UiHint.REMEMBER_PREVIOUS_VALUE)
        if show_label:
            ui_hints.append(UiHint.SHOW_LABEL)

        if default is not None:
            args = [("Default", default)]
        else:
            args = []

        super().__init__(
            self.parameter_name,
            user_name=user_name,
            ui_hints=ui_hints,
            args=args,
            type=self.enum.name,
        )

    def equal(self, value: enum.EnumValue) -> expr.Expr:
        """Generates an expression which tests whether this parameter matches the value."""
        return expr.Compare(
            expr.Id("definition." + self.parameter_name),
            expr.Operator.EQUAL,
            expr.Id("{}.{}".format(self.enum.name, value)),
        )

    def not_equal(self, value: enum.EnumValue) -> expr.Expr:
        """Generates an expression which tests whether this parameter does not match value."""
        return expr.Compare(
            expr.Id("definition." + self.parameter_name),
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


class UiPredicate(predicate.Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
    """

    def __init__(self, name: str, export: bool = True):
        super().__init__(name + "Predicate", arguments=definition, export=export)

    def __add__(self, annotation: Annotation | stmt.Statement) -> Self:
        return super().__add__(annotation)
