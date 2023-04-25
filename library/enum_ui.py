from typing import Callable
from library import enum, expr, stmt, utils
from library import pred
from library.pred import UiTestPredicate

__all__ = [
    "equal",
    "not_equal",
    "any",
    "not_any",
    "predicate_name",
    "EnumPredicates",
    "custom_predicate",
]


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


def custom_predicate(enum: enum.CustomEnum, name: str | None = None) -> pred.Predicate:
    if name is None:
        name = "is" + enum.name + "Custom"
    return pred.UiTestPredicate(name, equal(enum.CUSTOM))


class EnumPredicates(dict, stmt.Statement):
    """A class defining a set of predicates used to check specific enum members."""

    def __init__(
        self,
        enum: enum.Enum,
        parameter_name: str | None = None,
        prepend: str | None = None,
        append: str = "",
        export: bool = True,
    ) -> None:
        self.parameter_name = (
            enum.default_parameter_name if parameter_name is None else parameter_name
        )
        if prepend is None:
            prepend = "is" + enum.name
        self.enum = enum
        self.predicates = [
            UiTestPredicate(
                predicate_name(value, prepend=prepend, append=append),
                equal(value),
                export=export,
            )
            for value in self.enum
        ]

        value_names = [value.value for value in self.enum]
        predicate_calls = [predicate.call() for predicate in self.predicates]
        super().__init__(dict(zip(value_names, predicate_calls)))

    def __str__(self) -> str:
        return utils.to_str(self.predicates, sep="\n")
