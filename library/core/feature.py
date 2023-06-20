from typing import Self
from typing_extensions import override
from library.base import ctxt, expr, node, str_utils
from library.ui import annotation_map

FEATURE_BODY = """export const {} = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {{
        {}
    }}
    {{
        {}
    }});\n"""


class Feature(node.TopStatement):
    def __init__(
        self,
        name: str,
        map: annotation_map.AnnotationMap,
        ui: node.Node | None = None,
        body: node.Node | None = None,
    ) -> None:
        self.name = name
        self.map = map
        self.ui = ui or expr.Id(self.name + "Predicate(definition);")
        self.body = body or expr.Id(
            "do" + str_utils.upper_first(self.name) + "(context, id, definition);"
        )

    # def default_function(self) -> str:

    @override
    def build_top(self, context: ctxt.Context) -> str:
        header = self.map.run_build(context)
        return header + FEATURE_BODY.format(
            self.name, self.ui.run_build(context), self.body.run_build(context)
        )


class FeatureFactory:
    def __init__(self) -> None:
        self.name = None
        self.args = None

    def start(self, name: str, user_name: str | None = None, **feature_kwargs) -> Self:
        self.name = name
        feature_kwargs["user_name"] = user_name
        self.feature_kwargs = feature_kwargs
        return self

    # def add_editing_logic(self, function_name: str) -> Self:
    #     if self.map is None:
    #         raise ValueError("start must be called first.")
    #     self.args["editing_logic_function"] = function_name
    #     return self

    def make(self) -> Feature:
        if self.name is None or self.feature_kwargs is None:
            raise ValueError("start must be called first.")
        map = annotation_map.feature_annotation_map(self.name, **self.feature_kwargs)
        return Feature(self.name, map)
