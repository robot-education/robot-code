from typing import Self
from src.library import base, stmt, utils


class EnumValue(base.Node):
    def __init__(self, value: str, name: str | None = None, hidden: bool = False):
        """A possible value of an enum.

        value: The value of the enum. Should be all-caps.
        name: A user facing name. Overrides automatic conversion from `value`.
        hidden: Whether to mark the enum value as hidden. Has no effect if the enum is not ui.
        """
        self.ui = True

        self.value = value.upper()
        self.hidden = hidden
        if name is None:
            values = self.value.lower().split(sep="_")
            values[0] = values[0].capitalize()
            self.name = " ".join(values)
        else:
            self.name = name

    def set_ui(self, ui: bool) -> Self:
        self.ui = ui
        return self

    def __str__(self) -> str:
        if self.ui:
            dict = {"Name": self.name}
            if self.hidden:
                dict["Hidden"] = "true"
            return "annotation {}\n {}".format(str(base.Map(dict)), self.value)
        else:
            return "{}".format(self.value)


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
            value if isinstance(value, EnumValue) else EnumValue(value)
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

    def __str__(self) -> str:
        string = utils.export(self.export)
        string += "enum {} {{\n".format(self.name)
        string += utils.to_str(self.values, end=",\n", tab=True)
        return string + "}\n"
