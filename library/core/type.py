import enum as std_enum

__all__ = ["Type", "Unit", "value_with_units", "inch", "millimeter", "meter"]


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


def value_with_units(number: float | int, unit: str = Unit.METER) -> str:
    return str(number) + " * " + unit


def inch(value: float | int) -> str:
    return value_with_units(value, Unit.INCH)


def millimeter(value: float | int) -> str:
    return value_with_units(value, Unit.MILLIMETER)


def meter(value: float | int) -> str:
    return value_with_units(value, Unit.METER)
