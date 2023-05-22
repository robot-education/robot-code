from typing_extensions import override
import warnings
from library.api import api_utils
from library.base import node, ctxt


class Import(node.TopStatement):
    """Represents an import statement."""

    def __init__(self, studio_name: str, document_name: str | None = None) -> None:
        self.studio_name = studio_name
        self.document_name = document_name

    def resolve_path(self, context: ctxt.Context) -> tuple[str, str]:
        if self.document_name == None:
            return ("onshape/std/" + self.studio_name, context.std_version + ".0")

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
            studios = api_utils.get_studios(context.api, document)
            studio = studios.get(self.studio_name, None)
            if studio is None:
                warnings.warn(
                    "Failed to find feature studio in {} named {}.".format(
                        self.document_name, self.studio_name
                    )
                )
                path = "<INVALID_STUDIO_NAME>"
            else:
                path = studio.name

        return (path, "0" * 24)

    @override
    def build_top(self, context: ctxt.Context) -> str:
        return 'import(path : "{}", version : "{}");\n'.format(
            *self.resolve_path(context)
        )


def std_import(studio_name: str) -> Import:
    return Import(studio_name, document_name="std")
