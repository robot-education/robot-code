import enum as std_enum

__all__ = ["Type"]


class Type(std_enum.StrEnum):
    MAP = ("map",)
    CONTEXT = "Context"
    ID = "Id"
    VALUE = "ValueWithUnits"
    STRING = "string"
    VECTOR = "Vector"
