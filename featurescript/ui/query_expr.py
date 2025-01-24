import enum
from featurescript.base import ctxt, expr


class QueryEnum(expr.Expression, enum.Enum):
    """Represents an expression which can be used as a query filter."""

    def build(self, context: ctxt.Context) -> str:
        return self.__class__.__name__ + "." + self.name


class BodyType(QueryEnum):
    SOLID = enum.auto()
    SHEET = enum.auto()
    WIRE = enum.auto()
    POINT = enum.auto()
    MATE_CONNECTOR = enum.auto()
    COMPOSITE = enum.auto()


class EntityType(QueryEnum):
    VERTEX = enum.auto()
    EDGE = enum.auto()
    FACE = enum.auto()
    BODY = enum.auto()


class EdgeTopology(QueryEnum):
    WIRE = enum.auto()
    ONE_SIDED = enum.auto()
    TWO_SIDED = enum.auto()


class SketchObject(QueryEnum):
    YES = enum.auto()
    NO = enum.auto()


class ConstructionObject(QueryEnum):
    YES = enum.auto()
    NO = enum.auto()


class ModifiableEntityOnly(QueryEnum):
    YES = enum.auto()
    NO = enum.auto()


SKETCH_VERTEX_FILTER = (SketchObject.YES & EntityType.VERTEX) | BodyType.MATE_CONNECTOR
VERTEX_FILTER = EntityType.VERTEX | BodyType.MATE_CONNECTOR
