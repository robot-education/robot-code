import dataclasses
from typing import override
from featurescript.base import node, ctxt, user_error
from featurescript.core import utils
from featurescript.feature_studio import FeatureStudio
from onshape_api.assertions import assert_instance_type
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import ElementPath


EMPTY_VERSION = "0" * 24
"""A version which will get filled in by Onshape automatically."""


@dataclasses.dataclass
class Import(node.Node):
    """An import.

    Attributes:
        path: The path of the import.
        version: The version of the import. If None, the version defaults to a value which will get filled in by Onshape automatically.
    """

    path: str
    version: str = EMPTY_VERSION
    export: bool = False

    @override
    def build(self, context: ctxt.Context):
        if context.scope != ctxt.Scope.TOP:
            return user_error.expected_scope(ctxt.Scope.TOP)
        return utils.export(
            self.export
        ) + 'import(path : "{}", version : "{}");\n'.format(self.path, self.version)


class StdImport(node.Node):
    def __init__(self, studio_name: str, export: bool = False):
        self.studio_name = studio_name
        self.export = export

    @override
    def build(self, context: ctxt.Context):
        return Import(
            "onshape/std/" + self.studio_name,
            str(context.std_version) + ".0",
            export=self.export,
        ).build(context)


def external_import(studio: FeatureStudio, export: bool = False) -> Import:
    """An import of a feature studio from an external document."""
    path = studio.path
    assert_instance_type(path, InstanceType.VERSION)
    import_path = "/".join(
        (
            path.document_id,
            path.instance_id,
            path.element_id,
        )
    )
    return Import(import_path, studio.microversion_id, export=export)


def studio_import(element_path: ElementPath, export: bool = False):
    """An import of another feature studio in the current workspace."""
    return Import(element_path.element_id, export=export)
