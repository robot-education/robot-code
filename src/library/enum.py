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
            dict = base.Map(
                {"Name": self.name, "Hidden": "True" if self.hidden else None}
            )
            return "annotation {}\n {},\n".format(str(dict), self.value)
        else:
            return "{},\n".format(self.value)


class Enum(base.Node):
    def __init__(
        self, name: str, *values: EnumValue | str, export: bool = True, ui: bool = True
    ):
        """An enum.

        name: A capital case (LikeThis) string.
        """
        self.name = name
        self.values = [
            value if isinstance(value, EnumValue) else EnumValue(value)
            for value in values
        ]
        self.values = [value.set_ui(ui) for value in self.values]

        self.export = export

    def __str__(self) -> str:
        string = base.export(self.export)
        string += "enum {} {{\n".format(self.name)
        string += "".join(base.tab(str(value)) for value in self.values)
        return string + "}\n"


class Annotation(base.Node):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[str] = [],
        args: list[tuple[str, str]] = [],
    ) -> None:
        self.parameter_name = parameter_name

        if user_name is None:
            self.user_name = self.parameter_name
        else:
            self.user_name = user_name

        map = dict(*args)
        map["Name"] = user_name

        if len(ui_hints) > 0:
            map["UIHints"] = "[{}]".format(", ".join(ui_hints))

        self.map = base.Map(map)

    def __str__(self) -> str:
        return "annotation " + self.map + "\n"


class ValueAnnotation(Annotation):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[str] = [],
    ):
        super().__init__(parameter_name, user_name=user_name, ui_hints=ui_hints)


class TypeAnnotation(Annotation):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: list[str] = [],
        args: list[tuple[str, str]] = [],
        type: str | None = None,
    ) -> None:
        super().__init__(
            parameter_name, user_name=user_name, ui_hints=ui_hints, args=args
        )

        if type is None:
            self.type = self.parameter_name
        else:
            self.type = type

    def __str__(self) -> str:
        return str(super()) + "definition.{} is {};\n\n".format(
            self.parameter_name, self.type
        )


class EnumUi(base.Node):
    def __init__(
        self,
        enum: Enum,
        parameter_name: str | None = None,
        user_name: str | None = None,
        horizontal: bool = False,
        remember_previous_value: bool = True,
        default: str | None = None,
    ):
        if parameter_name is None:
            parameter_name = enum.name[0].lower() + enum.name[1:]

        ui_hints = []
        if horizontal:
            ui_hints.append("HORIZONTAL_ENUM")
        if remember_previous_value:
            ui_hints.append("REMEMBER_PREVIOUS_VALUE")

        if default is not None:
            args = [("Default", default)]
        else:
            args = []

        self.annotation = TypeAnnotation(
            parameter_name,
            user_name=user_name,
            ui_hints=ui_hints,
            args=args,
            type=enum.name,
        )

    def __str__(self) -> str:
        return str(self.annotation)
