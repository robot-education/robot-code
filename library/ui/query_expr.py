import abc
import enum
from typing_extensions import override
from library.base import ctxt, expr, node


# class Dummy(abc.ABC):
#     pass


# class Extend(Dummy):
#     pass


# class QueryFilterExpr(expr.Expr):
#     """Represents an expression which can be used as a query filter."""

#     def __init__(self, enum_name: str, name: str) -> None:
#         self.enum_name = enum_name
#         self.name = name

#     @override
#     def build(self, context: ctxt.Context) -> str:
#         return self.enum_name + "." + self.name

# @staticmethod
# def _generate_next_value_(name, start, count, last_values):
#     return QueryFilterExpr(__class__.__name__, name)

# def __new__(cls):
#     # *_, last = cls.__members__.keys()
#     return QueryFilterExpr(cls.__name__, "AHHH")


# class BodyType(QueryFilterExpr, QueryEnum):
#     @staticmethod
#     def _generate_next_value_(name, start, count, last_values):
#         return QueryFilterExpr(__class__.__name__, name)

#     SOLID = enum.auto()
#     SHEET = enum.auto()
#     WIRE = enum.auto()
#     POINT = enum.auto()
#     MATE_CONNECTOR = enum.auto()
#     COMPOSITE = enum.auto()

# @staticmethod
# def make(name) -> QueryFilterExpr:
#     return QueryFilterExpr(cls.__class__.__name__, name)


class QueryEnum(enum.StrEnum):
    @staticmethod
    def _generate_next_value_(name, start, count, last_values):
        return __class__.__name__ + "." + name


class EntityType(QueryEnum):
    VERTEX = enum.auto()
    EDGE = enum.auto()
    FACE = enum.auto()
    BODY = enum.auto()


# class EdgeTopology(QueryFilterExpr, enum.Enum):
#     WIRE = enum.auto()
#     ONE_SIDED = enum.auto()
#     TWO_SIDED = enum.auto()


# class SketchObject(QueryFilterExpr, enum.Enum):
#     YES = enum.auto()
#     NO = enum.auto()


# class ConstructionObject(QueryFilterExpr, enum.Enum):
#     YES = enum.auto()
#     NO = enum.auto()


# class ModifiableEntityOnly(QueryFilterExpr, enum.Enum):
#     YES = enum.auto()
#     NO = enum.auto()
