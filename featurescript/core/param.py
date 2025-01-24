from typing import Iterator, Self
from featurescript.base import ctxt, expr, node
from featurescript.core import type


class Parameter(node.Node):
    def __init__(
        self,
        name: str,
        type: str | None = None,
        use_name_as_type: bool = True,
        default_arg: expr.ExprCandidate | None = None,
    ):
        """An parameter for a predicate or function.

        Args:
            type: The type of the parameter.
            use_name_as_type: If `True` and type is `None`, then `name` is capitalized and used as `type` automatically.
            default_arg: A string to use as the default argument when this function is called. If None, name is used.
        """
        self.name = name
        self.default_arg = expr.cast_to_expr(
            default_arg if default_arg is not None else self.name
        )

        if not use_name_as_type:
            self.type = None
        else:
            self.type = self.name.capitalize() if type is None else type

    def __len__(self) -> int:
        return 1

    def __iter__(self) -> Iterator[Self]:
        return [self].__iter__()

    def build(self, context: ctxt.Context) -> str:
        if self.type is None:
            return self.name
        return "{} is {}".format(self.name, self.type)


definition_param = Parameter("definition", type.Type.MAP)
id_param = Parameter("id", type=type.Type.ID)
context_param = Parameter("context", type=type.Type.CONTEXT)
feature_params = [context_param, id_param, definition_param]
