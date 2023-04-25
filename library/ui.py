from abc import ABC
from typing import Callable, Iterable, Sequence
from library import (
    predicate,
    argument,
    bounds,
    base,
    enum,
    expr,
    stmt,
    ui_hint,
    utils,
)

__all__ = [
    "definition_arg",
    "id_arg",
    "context_arg",
    "feature_args",
    "EnumAnnotation",
    "BooleanAnnotation",
    "LengthAnnotation",
    "UiPredicate",
    "UiTestPredicate",
    "equal",
    "not_equal",
    "any",
    "not_any",
    "predicate_name",
]


class UiPredicate(predicate.Predicate):
    """
    A predicate defining elements for use in the UI.

    name: The name of the predicate. To match convention, the word `Predicate` is always automatically appended.
    """

    def __init__(
        self, name: str, statements: Iterable[stmt.Statement] = [], export: bool = True
    ):
        super().__init__(
            name + "Predicate",
            arguments=definition_arg,
            statements=statements,
            export=export,
        )


class UiTestPredicate(predicate.Predicate):
    def __init__(self, name: str, statement: stmt.Statement, export: bool = True):
        super().__init__(
            name, arguments=definition_arg, statements=[statement], export=export
        )


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


class TypeAnnotation(Annotation, ABC):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(self, parameter_name: str, type: str, **kwargs) -> None:
        super().__init__(parameter_name, **kwargs)
        self.type = type

    def __str__(self) -> str:
        return (
            super().__str__()
            + utils.definition(self.parameter_name)
            + " is {};\n".format(self.type)
        )


class EnumAnnotation(TypeAnnotation):
    def __init__(
        self,
        enum: enum.Enum,
        parameter_name: str | None = None,
        user_name: str | None = None,
        default: str | None = None,
        ui_hints: Iterable[ui_hint.UiHint] | None = ui_hint.remember_hint,
    ) -> None:
        self.enum = enum
        parameter_name = (
            enum.default_parameter_name if parameter_name is None else parameter_name
        )
        args = {} if not default else {"Default": default}
        super().__init__(
            parameter_name,
            type=self.enum.name,
            user_name=user_name,
            args=args,
            ui_hints=ui_hints,
        )


class BooleanAnnotation(TypeAnnotation):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        default: bool = False,
        ui_hints: Iterable[ui_hint.UiHint] = ui_hint.remember_hint,
    ) -> None:
        args = {} if not default else {"Default": "true"}
        super().__init__(
            parameter_name,
            type="boolean",
            user_name=user_name,
            args=args,
            ui_hints=ui_hints,
        )


class ValueAnnotation(Annotation, ABC):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self, parameter_name: str, *, bound_spec: str, predicate: str, **kwargs
    ):
        super().__init__(parameter_name, **kwargs)
        self.bound_spec = bound_spec
        self.predicate = predicate

    def __str__(self) -> str:
        return super().__str__() + "{}({}, {});\n".format(
            self.predicate, utils.definition(self.parameter_name), self.bound_spec
        )


class LengthAnnotation(ValueAnnotation):
    def __init__(
        self,
        parameter_name: str,
        *,
        bound_spec: bounds.LengthBound,
        user_name: str | None = None,
        ui_hints: Iterable[ui_hint.UiHint] = [
            ui_hint.UiHint.SHOW_EXPRESSION,
            ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        ],
    ) -> None:
        super().__init__(
            parameter_name,
            bound_spec=bound_spec,
            user_name=user_name,
            ui_hints=ui_hints,
            predicate="isLength",
        )


def _equal(
    operator: expr.Operator,
    value: enum.EnumValue,
    parameter_name: str | None = None,
    definition: str = "definition",
) -> expr.Expr:
    """Generates an expression which tests whether this parameter matches the value."""
    if parameter_name is None:
        parameter_name = value.enum.default_parameter_name
    return expr.Compare(
        expr.Id(utils.definition(parameter_name, definition=definition)),
        operator,
        expr.Id("{}.{}".format(value.enum.name, value.value)),
    )


def equal(
    value: enum.EnumValue,
    parameter_name: str | None = None,
    definition: str = "definition",
) -> expr.Expr:
    """Generates an expression which tests whether this parameter matches the value."""
    return _equal(expr.Operator.EQUAL, value, parameter_name, definition)


def not_equal(
    value: enum.EnumValue,
    parameter_name: str | None = None,
    definition: str = "definition",
) -> expr.Expr:
    """Generates an expression which tests whether this parameter does not match value."""
    return _equal(expr.Operator.NOT_EQUAL, value, parameter_name, definition)


def _any(
    func: Callable[[enum.EnumValue], expr.Expr],
    *values: enum.EnumValue,
    add_parentheses: bool,
) -> expr.Expr:
    expression = func(values[0])
    for value in values[1:]:
        expression |= func(value)
    return expr.Parens(expression) if add_parentheses else expression


def any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter matches any value in values."""
    return _any(equal, *values, add_parentheses=add_parentheses)


def not_any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter doesn't match any value in values."""
    return _any(not_equal, *values, add_parentheses=add_parentheses)


def predicate_name(value: enum.EnumValue, prepend: str = "is", append: str = "") -> str:
    return prepend + value.camel_case(capitalize=True) + append
