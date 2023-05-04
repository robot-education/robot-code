from typing import Any, Iterable, Sequence
from typing_extensions import override
from library.base import ctxt, expr, str_utils, node
from library.core import utils
from library.ui import enum

__all__ = [
    "Map",
    "MapAccess",
    "definition_map",
    "enum_map",
    "lookup_enum_map",
]


class Map(expr.Expr):
    """Defines a map literal."""

    def __init__(
        self,
        dict: dict[str, Any],
        quote_keys: bool = True,
        quote_values: bool = False,
        exclude_keys: Iterable[str] = [],
        inline: bool = True,
    ):
        """
        quote_values: Whether to add quotation marks around each value.
        exclude_keys: Specifies keys to ignore when quoting values. Does nothing if quote_values is False.
        """
        self.dict = dict
        self.quote_keys = quote_keys
        self.quote_values = quote_values
        self.exclude_keys = exclude_keys
        self.inline = inline

    def _get_pairs(self, context: ctxt.Context) -> Sequence[node.Node]:
        pairs = []
        for key, value in self.dict.items():
            if value is None:
                continue
            value = expr.cast_to_expr(value).build(context)
            if self.quote_values and key not in self.exclude_keys:
                value = str_utils.quote(value)
            if self.quote_keys:
                key = str_utils.quote(key)
            pairs.append(expr.Id("{} : {}".format(key, value)))
        return pairs

    def build(self, context: ctxt.Context) -> str:
        pairs = self._get_pairs(context)
        if len(pairs) == 0:
            return "{}"
        if self.inline:
            return "{{ {} }}".format(node.build_nodes(pairs, context, sep=", "))
        string = "{\n"
        string += node.build_nodes(pairs, context, sep=",", indent=True, end="\n")
        return string + "}"


def definition_map(*values: str, definition: str = "definition", **kwargs) -> Map:
    map_dict = dict([(value, utils.definition(value, definition)) for value in values])
    return Map(map_dict, **kwargs)


class MapAccess(expr.Expr):
    def __init__(self, map: str | expr.Expr, *keys: str | expr.Expr) -> None:
        self.map = expr.cast_to_expr(map)
        self.keys = keys

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.map.build(context) + "".join(
            ("[{}]".format(expr.cast_to_expr(key).build(context)) for key in self.keys)
        )


def enum_map(
    enum: enum.EnumDict, *values: str | expr.Expr, inline: bool = False, **kwargs
) -> Map:
    map_dict = dict(
        (enum_value.enum.name + "." + enum_value.value, value)
        for enum_value, value in zip(enum.values(), tuple(values))
    )
    return Map(map_dict, quote_keys=False, inline=inline, **kwargs)


def lookup_enum_map(enum: enum.EnumDict[enum.LookupEnumValue]) -> Map:
    map_dict = dict(
        (enum_value.enum.name + "." + enum_value.value, enum_value.lookup_value)
        for enum_value in enum.values()
    )
    return Map(map_dict, quote_keys=False)
