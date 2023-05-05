from library.core import func
from library.base import expr, node, str_utils
from library.ui import enum

__all__ = [
    "any",
    "not_any",
    "predicate_name",
    "custom_enum_predicate",
    "enum_predicates",
]


def _any(
    *values: enum.EnumValue,
    invert: bool,
    add_parentheses: bool,
) -> expr.Expr:
    expression = values[0](invert=invert)
    for value in values[1:]:
        expression |= value(invert=invert)
    return expr.Parens(expression) if add_parentheses else expression


def any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter matches any value in values."""
    return _any(*values, invert=True, add_parentheses=add_parentheses)


def not_any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter doesn't match any value in values."""
    return _any(*values, invert=False, add_parentheses=add_parentheses)


def predicate_name(value: enum.EnumValue, prepend: str = "is", append: str = "") -> str:
    return prepend + str_utils.camel_case(value.value, capitalize=True) + append


def custom_enum_predicate(
    enum: enum.EnumDict, *, name: str | None = None, parent: node.ParentNode
) -> func.Predicate:
    """Generates a predicate which tests if an enum is CUSTOM."""
    if name is None:
        name = "is" + enum.name + "Custom"
    return func.UiTestPredicate(name, enum["CUSTOM"](), parent=parent)


def enum_predicates(
    enum: enum.EnumDict,
    *,
    parent: node.ParentNode,
    parameter_name: str | None = None,
    prepend: str | None = None,
    append: str = "",
    export: bool = True,
) -> dict[str, func.Predicate]:
    parameter_name = parameter_name or enum.default_parameter_name
    prepend = prepend or "is" + enum.name
    return dict(
        [
            (
                value.value,
                func.UiTestPredicate(
                    predicate_name(value, prepend=prepend, append=append),
                    value(),
                    export=export,
                    parent=parent,
                ),
            )
            for value in enum.values()
        ]
    )
