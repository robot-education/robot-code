from __future__ import annotations

from typing import Iterator, Self
from src.library import base, stmt, utils


class EnumValue(base.Node):
    # Enum type is circular?
    def __init__(
        self, value: str, enum: Enum, user_name: str | None = None, hidden: bool = False
    ):
        """A possible value of an enum.

        value: The value of the enum. Should be all-caps seperated by underscores.
        name: A user facing name. Overrides automatic conversion from `value`.
        hidden: Whether to mark the enum value as hidden. Has no effect if the enum is not ui.
        """
        self.ui = True
        self.value = value.upper()
        self.hidden = hidden
        self.enum = enum

        if user_name is None:
            self.user_name = self.make_user_name()
        else:
            self.user_name = user_name

    def make_user_name(self) -> str:
        words = self.value.lower().split(sep="_")
        words[0] = words[0].capitalize()
        return " ".join(words)

    def set_ui(self, ui: bool) -> Self:
        self.ui = ui
        return self

    def __str__(self) -> str:
        if self.ui:
            dict = {"Name": self.user_name}
            if self.hidden:
                dict["Hidden"] = "true"
            return "annotation {}\n {}".format(
                str(base.Map(dict, quote_values=True)), self.value
            )
        else:
            return "{}".format(self.value)

    def camel_case(self) -> str:
        words = self.make_user_name().split()
        words[1:] = [word.lower().capitalize() for word in words[1:]]
        return "".join(words)


class Enum(stmt.Statement):
    def __init__(
        self, name: str, *values: EnumValue | str, export: bool = True, ui: bool = True
    ):
        """An enum.

        name: A capital case (LikeThis) string.
        values: A list of EnumValues or strings (which are set to enum values).
        """
        self.name = name
        self.values = [
            value if isinstance(value, EnumValue) else EnumValue(value, self)
            for value in values
        ]

        self.values = [value.set_ui(ui) for value in self.values]

        for enum_value in self.values:
            setattr(self, enum_value.value, enum_value)

        self.export = export

    def __getattribute__(self, name: str) -> EnumValue:
        """
        Overload get_attribute to type hint EnumValue access for users.
        """
        return super().__getattribute__(name)

    def __contains__(self, value: EnumValue) -> bool:
        return value in self.values

    def __iter__(self) -> Iterator[EnumValue]:
        return self.values.__iter__()

    def __str__(self) -> str:
        string = utils.export(self.export)
        string += "enum {} {{\n".format(self.name)
        string += utils.to_str(self.values, end=",\n", tab=True)
        return string + "}\n"
