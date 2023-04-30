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

    def __str__(self) -> str:
        """Process the contents of the feature studio."""
        return self.children_str(sep="\n")

    def build(self, std_version: str) -> str:
        return HEADER.format(std_version, std_version) + str(self)
