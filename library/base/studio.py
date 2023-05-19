from typing_extensions import override
from library.base import ctxt, node

FS_HEADER = "FeatureScript {};\n"
GENERATED_HEADER = "/* Automatically generated file -- DO NOT EDIT */\n"

# HEADER = """FeatureScript {};
DEFAULT_STRING = r'import(path : "onshape/std/common.fs", version : "{}.0");'

# /* Automatically generated file -- DO NOT EDIT */\n
# """

__all__ = ["Studio"]


class Studio(node.ParentNode):
    def __init__(self, studio_name: str, document_name: str) -> None:
        super().__init__()
        self.studio_name = studio_name
        self.document_name = document_name
        self.imports = []

    def build_header(self, context: ctxt.Context) -> str:
        return (
            FS_HEADER.format(context.std_version)
            + DEFAULT_STRING.format(context.std_version)
            + "\n".join(node.build(context) for node in self.imports)
            + GENERATED_HEADER
        )

    @override
    def build(self, context: ctxt.Context) -> str:
        """The top-level build function for the studio."""
        header = self.build_header(context)
        body = "\n".join(
            node.run_build_top(context) for node in self.children  # type: ignore
        )
        return header + body

    def build_studio(self, std_version: str) -> str:
        context = ctxt.Context(std_version)
        build = self.run_build(context)
        return build
