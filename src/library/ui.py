from abc import ABC
from typing import Self
import warnings
from src.library import predicate, argument, base, enum, expr, stmt, ui_hint, utils

definition = argument.Argument("definition", "map")
id = argument.Argument("id")
context = argument.Argument("context")
feature = argument.Arguments(context, id, definition)


class Annotation(stmt.Statement, ABC):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: ui_hint.UiHints = ui_hint.UiHints(),
        args: dict[str, str] = {},
    ) -> None:
        """
        A dict containing additional strings to add to the annotation map.
        """
        self.parameter_name = parameter_name

        if user_name is None:
            self.user_name = self.parameter_name
        else:
            self.user_name = user_name

        args["Name"] = utils.quote(self.user_name)
        if len(ui_hints) > 0:
            args["UiHint"] = str(ui_hints)
        self.map = base.Map(args)

    def __str__(self) -> str:
        return "annotation " + self.map + "\n"


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

        if parameter_name is None:
            self.parameter_name = enum.name[0].lower() + enum.name[1:]
        else:
            self.parameter_name = parameter_name

        if kwargs["ui_hints"].show_label and kwargs["ui_hints"].horizontal_enum:
            warnings.warn("show_label and horizontal enum don't work together.")

        args = {}
        if default is not None:
            args["Default"] = default

        super().__init__(self.parameter_name, type=self.enum.name, args=args, **kwargs)


class UiPredicate(predicate.Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
    """

    def __init__(self, name: str, export: bool = True):
        super().__init__(name + "Predicate", arguments=definition, export=export)

    def __add__(self, annotation: Annotation | stmt.Statement) -> Self:
        return super().__add__(annotation)


def equal(
    self, parameter_name: str, value: enum.EnumValue, definition: str = "definition"
) -> expr.Expr:
    """Generates an expression which tests whether this parameter matches the value."""
    return expr.Compare(
        expr.Id(definition + "." + self.parameter_name),
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
