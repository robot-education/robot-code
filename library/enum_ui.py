from library import enum, expr, stmt, utils
from library import pred
from library.pred import UiTestPredicate

__all__ = [
    "any",
    "not_any",
    "predicate_name",
    "EnumPredicates",
    "custom_predicate",
]


def _any(
    *values: enum.EnumValue,
    invert: bool,
    add_parentheses: bool,
) -> expr.Expr:
    expression = values[0](invert=invert)
    for value in values[1:]:
        expression |= value()
    return expr.Parens(expression) if add_parentheses else expression


def any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter matches any value in values."""
    return _any(*values, invert=True, add_parentheses=add_parentheses)


def not_any(*values: enum.EnumValue, add_parentheses: bool = False) -> expr.Expr:
    """Generates an expression which is true when this parameter doesn't match any value in values."""
    return _any(*values, invert=False, add_parentheses=add_parentheses)


def predicate_name(value: enum.EnumValue, prepend: str = "is", append: str = "") -> str:
    return prepend + value.camel_case(capitalize=True) + append


def custom_predicate(
    enum: enum.Enum, *, name: str | None = None, parent: stmt.Parent
) -> expr.Expr:
    """Generates a predicate which tests if an enum is custom."""
    if name is None:
        name = "is" + enum.name + "Custom"
    return pred.UiTestPredicate(name, enum["CUSTOM"](), parent=parent)()


class EnumPredicates(dict[str, expr.Expr], stmt.Parent):
    """A class defining a set of predicates used to check specific enum members."""

    def __init__(
        self,
        enum: enum.Enum,
        *,
        parent: stmt.Parent,
        parameter_name: str | None = None,
        prepend: str | None = None,
        append: str = "",
        export: bool = True,
    ) -> None:
        stmt.Parent.__init__(self)

        self.register_parent(parent)

        self.parameter_name = (
            enum.default_parameter_name if parameter_name is None else parameter_name
        )
        if prepend is None:
            prepend = "is" + enum.name
        self.enum = enum
        value_dict = dict(
            [
                (
                    value.value,
                    UiTestPredicate(
                        predicate_name(value, prepend=prepend, append=append),
                        value(),
                        export=export,
                        parent=self,
                    )(),
                )
                for value in self.enum.values()
            ]
        )
        super().__init__(value_dict)

    def __str__(self) -> str:
        return utils.to_str(self.child_nodes, sep="\n")
