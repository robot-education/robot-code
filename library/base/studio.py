from typing_extensions import override
from library.base import ctxt, node, imp

FS_HEADER = "FeatureScript {};"
GENERATED_HEADER = "/* Automatically generated file -- DO NOT EDIT */\n\n"

__all__ = ["Studio"]


class Studio(node.ParentNode):
    def __init__(
        self, studio_name: str, document_name: str, import_common: bool = True
    ) -> None:
        super().__init__()
        self.studio_name = studio_name
        self.document_name = document_name

        self.imports = []
        if import_common:
            self.imports.append(imp.Import("common.fs"))

    def build_header(self, context: ctxt.Context) -> str:
        return "\n".join(
            [
                FS_HEADER.format(context.std_version),
                "".join(node.run_build_top(context) for node in self.imports),
                GENERATED_HEADER,
            ]
        )

    @override
    def build(self, context: ctxt.Context) -> str:
        """The top-level build function for the studio."""
        header = self.build_header(context)
        body = "\n".join(
            node.run_build_top(context) for node in self.children  # type: ignore
        )
        return header + body

    # def build_studio(self, context: ctxt.Context) -> str:
    #     context = ctxt.Context(std_version, config, api)
    #     build = self.run_build(context)
    #     return build
