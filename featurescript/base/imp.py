import dataclasses
from typing import override
import warnings
from onshape_api.endpoints import versions
from featurescript import endpoints
from featurescript.base import node, ctxt, user_error
from featurescript.core import utils


@dataclasses.dataclass
class Import(node.Node):
    """Represents an import statement.

    Args:
        std: Whether the import references the Onshape standard library.
        document_name:
            The name of the document (as defined in config.json) to reference.
            If the document_name is undefined, the name of the current studio will be used.
        version_id:
            The id of the version to use in the event document_name is for an external library.
            If None, the latest version is automatically computed and used.
        std:
            Whether the import references the Onshape standard library.
            Takes precedence over document_name.
    """

    studio_name: str
    document_name: str | None = None
    version_id: str | None = None
    std: bool = False
    export: bool = False

    def resolve_path(self, context: ctxt.Context) -> tuple[str, str]:
        if self.std:
            return ("onshape/std/" + self.studio_name, context.std_version + ".0")

        self.document_name = self.document_name or context.document_name

        is_external = self.document_name != context.document_name
        document = context.config.get_document(self.document_name)
        if document is None:
            raise ValueError(
                "Failed to find document in config named {}.".format(self.document_name)
            )
        if is_external:
            if self.version_id:
                document.instance_id = self.version_id
            else:
                document.instance_id = versions.get_versions(context.api, document)[-1][
                    "id"
                ]
            # set last to avoid affecting get_versions call
            document.workspace_or_version = "v"

        studio = endpoints.get_feature_studio(context.api, document, self.studio_name)
        version = "0" * 24
        if studio is None:
            warnings.warn(
                "Failed to find feature studio in {} named {}.".format(
                    context.document_name, self.studio_name
                )
            )
            path = user_error.code_message("Invalid document name")
            return (path, version)

        if is_external:
            path = "/".join(
                (
                    document.document_id,
                    document.instance_id,
                    studio.path.element_id,
                )
            )
            version = studio.microversion_id
        else:
            path = studio.path.element_id
        return (path, version)

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope != ctxt.Scope.TOP:
            return user_error.expected_scope(ctxt.Scope.TOP)
        return utils.export(
            self.export
        ) + 'import(path : "{}", version : "{}");\n'.format(*self.resolve_path(context))


@dataclasses.dataclass
class BaseImport(node.Node):
    """A simplified version of import."""

    path: str
    version: str
    export: bool = False

    @override
    def build(self, context: ctxt.Context):
        if context.scope == ctxt.Scope.TOP:
            return utils.export(
                self.export
            ) + 'import(path : "{}", version : "{}");\n'.format(self.path, self.version)
        return user_error.expected_scope(ctxt.Scope.TOP)
