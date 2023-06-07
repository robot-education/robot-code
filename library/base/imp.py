from typing_extensions import override
import warnings
from library.api.endpoint import documents
from library.base import node, ctxt
from library.core import utils


class Import(node.TopStatement):
    """Represents an import statement."""

    def __init__(
        self, studio_name: str, document_name: str | None = None, export: bool = False
    ) -> None:
        self.export = export
        self.studio_name = studio_name
        self.document_name = document_name

    def resolve_path(self, context: ctxt.Context) -> tuple[str, str]:
        if self.document_name == None:
            return ("onshape/std/" + self.studio_name, context.std_version + ".0")

        if self.document_name != context.document_name:
            return ("<EXTERNAL_DOCUMENT_REF>", "0" * 24)

        document = context.config.get_document(self.document_name)
        if document is None:
            warnings.warn(
                "Failed to find document in config.json named {}. Valid names are: {}".format(
                    self.document_name,
                    ", ".join(context.config.documents.keys()),
                )
            )
            path = "<INVALID_DOCUMENT_NAME>"
        else:
            studios = documents.get_studios(context.api, document)
            studio = studios.get(self.studio_name, None)
            if studio is None:
                warnings.warn(
                    "Failed to find feature studio in {} named {}.".format(
                        self.document_name, self.studio_name
                    )
                )
                path = "<INVALID_STUDIO_NAME>"
            else:
                path = studio.path.element_id

        return (path, "0" * 24)

    @override
    def build_top(self, context: ctxt.Context) -> str:
        return utils.export(
            self.export
        ) + 'import(path : "{}", version : "{}");\n'.format(*self.resolve_path(context))
