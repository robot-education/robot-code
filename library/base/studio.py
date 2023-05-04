from typing_extensions import override
from library.base import ctxt, node

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

    @override
    def build(self, context: ctxt.Context) -> str:
        """Process the contents of the feature studio."""
        return HEADER.format(
            context.std_version, context.std_version
        ) + self.build_children(context, sep="\n")

    def build_studio(self, std_version: str) -> str:
        context = ctxt.Context(std_version)
        return self.build(context)
