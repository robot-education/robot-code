from typing import Iterator, Self
from src.library import base


class Argument(base.Node):
    def __init__(
        self,
        name: str,
        type: str | None = None,
        use_name_as_type: bool = True,
        default_parameter: str | None = None,
    ):
        """An argument to a predicate or function.

        type: The type of the argument.
        use_name_as_type: If `True` and type is `None`, then `name` is capitalized and used as `type` automatically.
        default_parameter: A string to use as the default parameter when this function is called. If None, name is used.
        """
        self.name = name
        self.default_parameter = (
            default_parameter if default_parameter is not None else self.name
        )

        if not use_name_as_type:
            self.type = None
        else:
            self.type = self.name.capitalize() if type is None else type

    def __len__(self) -> int:
        return 1

    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()

    def __str__(self) -> str:
        if self.type is None:
            return self.name
        return "{} is {}".format(self.name, self.type)


class Arguments(base.Node):
    def __init__(self, *arguments: Argument):
        self.arguments = arguments

    def __len__(self) -> int:
        return len(self.arguments)

    def __iter__(self) -> Iterator[Argument]:
        return self.arguments.__iter__()

    def __str__(self) -> str:
        return ", ".join(base.to_str(self.arguments))
