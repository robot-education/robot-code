from typing import Iterator, Self
from src.library import base, utils


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


class Arguments(base.ParentNode[Argument]):
    def __init__(self, *arguments: Argument) -> None:
        super().__init__(child_nodes=arguments)

    def __str__(self) -> str:
        return utils.to_str(self.child_nodes, sep=", ")


def promote(node: Argument | Arguments) -> Arguments:
    if isinstance(node, Argument):
        return Arguments(node)
    return node