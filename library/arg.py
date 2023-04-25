from typing import Iterator, Self
from library import base

__all__ = [
    "Argument",
    "definition_arg",
    "id_arg",
    "context_arg",
    "feature_args",
]


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


definition_arg = Argument("definition", "map")
id_arg = Argument("id")
context_arg = Argument("context")
feature_args = [context_arg, id_arg, definition_arg]