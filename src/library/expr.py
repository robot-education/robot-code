from typing import Self
from src.library import base


class BoolExpr(base.InlineNode):
    """A boolean expression which evalutes to true or false.

    Supports the boolean operations & (logical and), | (logical or), and ~ (logical not).
    """

    def __init__(self, token: str = "") -> None:
        self.token = token

    def __invert__(self) -> None:
        self.token = "!" + self.token

    def __and__(self, expr: Self) -> None:
        self.token = self.token + " && " + expr.token

    def __sub__(self, expr: Self) -> None:
        self.token = self.token + " || " + expr.token

    def enter(self) -> str:
        return self.token


class Parens(BoolExpr):
    def __init__(self, expr: BoolExpr) -> None:
        self.token = "(" + expr.token + ")"


# is_enum("value") | is_enum("value") & !is_enum("value");
# (is_enum("value") | is_enum("value")) & (!is_enum("value") & is_enum("value"));
# oh yeah predicates are also valid here...
