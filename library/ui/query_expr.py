from abc import ABC
import enum
from library.base import expr


class QueryFilterExpr(expr.Expr):
    """Represents an expression which can be used as a query filter."""

    def __init__(self) -> None:
        pass


class QueryEnum(enum.Enum, QueryFilterExpr, ABC):
    pass


class EntityType(QueryEnum):
    VERTEX = enum.auto()
    NO = enum.auto()


class ConstructionObject(QueryEnum):
    YES = enum.auto()
    NO = enum.auto()
