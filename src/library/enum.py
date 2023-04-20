from src.library import base

class EnumValue(base.Node):
    def __init__(self, value: str, name: str | None = None):
        """A possible value of an enum.

        value: The value of the enum. Should be all-caps.
        name: A user facing name. Overrides automatic conversion from `value`.
        """
        self.value = value
        if name is None:
            values = self.value.lower().split(sep="_")
            values[0] = values[0].capitalize()
            self.name = "".join(values)
        else:
            self.name = name
    
    def __str__(self) -> str:
        return ""


class Enum(base.Node):
    def __init__(self, *members: EnumValue | str):


