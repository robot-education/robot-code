from typing import Iterable
from library.base import stmt, node
from library.ui import enum

__all__ = ["Map", "enum_map"]


class Map(stmt.Statement):
    """Defines a map literal."""

    def __init__(
        self,
        dict: dict[str, str],
        parent: node.ParentNode | None = None,
        quote_values: bool = False,
        exclude_keys: Iterable[str] = [],
    ):
        """
        quote_values: Whether to add quotation marks around each value.
        exclude_keys: Specifies keys to ignore when quoting values. Does nothing if quote_values is False.
        """
        super().__init__(parent=parent)
        self.dict = dict
        self.quote_values = quote_values
        self.exclude_values = exclude_keys

    def _quote_format_str(self, quote_value: bool) -> str:
        return ' "{}" : "{}"' if quote_value else ' "{}" : {}'

    def __str__(self) -> str:
        pairs = [
            self._quote_format_str(
                self.quote_values and key not in self.exclude_values
            ).format(key, value)
            for key, value in self.dict.items()
            if value is not None
        ]

        if len(pairs) == 0:
            return "{}"

        return "{{{}}}".format(",".join(pairs) + " ")


def enum_map(value_dict: dict[enum.EnumValue, str], **kwargs) -> Map:
    map_dict = dict(
        (enum_value.enum.name + "." + enum_value.value, value)
        for enum_value, value in value_dict.items()
    )
    return Map(map_dict, **kwargs)


def lookup_enum_map(enum: enum.EnumDict[enum.LookupEnumValue]) -> Map:
    map_dict = dict(
        (enum_value.enum.name + "." + enum_value.value, enum_value.lookup_value)
        for enum_value in enum.values()
    )
    return Map(map_dict)
