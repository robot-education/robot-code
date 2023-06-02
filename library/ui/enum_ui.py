from library.base import expr
from library.ui import enum as lib_enum

__all__ = [
    "any",
    "not_any",
]


def _any(
    enum: lib_enum.EnumDict,
    *keys: str,
    invert: bool,
    add_parentheses: bool,
) -> expr.Expr:
    expression = enum[keys[0]](invert=invert)
    for key in keys[1:]:
        expression |= enum[key](invert=invert)
    return expr.Parens(expression) if add_parentheses else expression


def any(
    enum: lib_enum.EnumDict, *keys: str, add_parentheses: bool = False
) -> expr.Expr:
    """Generates an expression which is true when this parameter matches any value in values."""
    return _any(enum, *keys, invert=False, add_parentheses=add_parentheses)


def not_any(
    enum: lib_enum.EnumDict, *keys: str, add_parentheses: bool = False
) -> expr.Expr:
    """Generates an expression which is true when this parameter doesn't match any value in values."""
    return _any(enum, *keys, invert=True, add_parentheses=add_parentheses)
