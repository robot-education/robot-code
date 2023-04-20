from typing import Self
from src.library import base


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
            hidden_str = ', "Hidden" : True ' if self.hidden else ""
            return 'annotation {{ "Name" : "{}"{} }}\n {},\n'.format(
                self.name, hidden_str, self.value
            )
        else:
            return "{},\n".format(self.value)


class Enum(base.Node):
    def __init__(
        self, name: str, *values: EnumValue | str, export: bool = True, ui: bool = True
    ):
        self.name = name
        self.values = [
            value if isinstance(value, EnumValue) else EnumValue(value)
            for value in values
        ]
        self.values = [value.set_ui(ui) for value in self.values]

        self.export = export

    def __str__(self) -> str:
        string = "export " if self.export else ""
        string += "enum {} {{\n".format(self.name)
        string += "".join(base.tab(str(value)) for value in self.values)
        return string + "}\n"


class EnumUi(base.Node):
    def __init__(self, enum: Enum):
        pass
