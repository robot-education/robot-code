from library.base import node

HEADER = """FeatureScript {};
import(path : "onshape/std/common.fs", version : "{}.0");

/* Automatically generated file -- DO NOT EDIT */\n
"""

__all__ = ["Studio"]


class Studio(node.ParentNode):
    def __init__(self, studio_name: str, document_name: str) -> None:
        super().__init__()
        self.studio_name = studio_name
        self.document_name = document_name

    def build(self, context: node.Context) -> str:
        """Process the contents of the feature studio."""
        return HEADER.format(
            context.std_version, context.std_version
        ) + self.build_children(context, sep="\n")

    def build_studio(self, std_version: str) -> str:
        context = node.Context(std_version)
        return self.build(context)
