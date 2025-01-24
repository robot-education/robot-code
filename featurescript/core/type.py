"""A collection of Onshape types and units."""
import enum as std_enum
from featurescript.base import expr


class Type(std_enum.StrEnum):
    MAP = "map"
    CONTEXT = "Context"
    ID = "Id"
    VALUE = "ValueWithUnits"
    STRING = "string"
    VECTOR = "Vector"
    NUMBER = "number"


class Unit(std_enum.StrEnum):
    INCH = "inch"
    FOOT = "foot"
    YARD = "yard"
    MILLIMETER = "millimeter"
    CENTIMETER = "centimeter"
    METER = "meter"
    DEGREE = "degree"
    RADIAN = "radian"


def value_with_units(
    number: float | int | str, unit: str = Unit.METER
) -> expr.Expression:
    return expr.Id(str(number) + " * " + unit)


def inch(value: float | int | str) -> expr.Expression:
    return value_with_units(value, Unit.INCH)


def millimeter(value: float | int | str) -> expr.Expression:
    return value_with_units(value, Unit.MILLIMETER)


def meter(value: float | int | str) -> expr.Expression:
    return value_with_units(value, Unit.METER)


def degree(value: float | int | str) -> expr.Expression:
    return value_with_units(value, Unit.DEGREE)


def radian(value: float | int | str) -> expr.Expression:
    return value_with_units(value, Unit.RADIAN)
