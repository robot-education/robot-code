import enum
from onshape_api.assertions import (
    assert_instance_type,
    assert_version,
    assert_workspace,
)
from onshape_api.endpoints.versions import get_latest_version
from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.instance_type import (
    InstanceType,
)
from onshape_api.paths.paths import (
    DocumentPath,
    InstancePath,
    ElementPath,
)


def get_document(api: Api, document_path: DocumentPath) -> dict:
    """Retrieves a given document's metadata."""
    # documents endpoints are not idiomatic - no /d/
    return api.get(api_path("documents", end_id=document_path.document_id))


class ElementType(enum.StrEnum):
    """Describes possible element (tab) types in a document."""

    PART_STUDIO = "PARTSTUDIO"
    ASSEMBLY = "ASSEMBLY"
    DRAWING = "DRAWING"
    FEATURE_STUDIO = "FEATURESTUDIO"


def get_document_elements(
    api: Api,
    instance_path: InstancePath,
    element_type: ElementType | None = None,
) -> dict[str, ElementPath]:
    """Fetches all elements in a document.

    Args:
        element_type: The type of element (tab) to get. If None, all elements are returned.
    """
    query: dict = {"withThumbnails": False}
    if element_type:
        query["elementType"] = element_type

    return api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query=query,
    )


def get_workspace_microversion_id(api: Api, instance_path: InstancePath) -> str:
    """Fetches the latest microversion id of a given workspace.

    Note this is the microversion associated with the workspace as a whole.
    Individual elements also have their own microversion ids which are unrelated to the workspace's.
    """
    assert_instance_type(instance_path, InstanceType.WORKSPACE, InstanceType.VERSION)
    return api.get(
        api_path("documents", instance_path, InstancePath, "currentmicroversion")
    )["microversion"]


def get_references(api: Api, element_path: ElementPath) -> dict:
    """An undocumented endpoint which returns a list of specific external references in a tab."""
    return api.get(api_path("elements", element_path, ElementPath, "references"))


def get_external_references(
    api: Api,
    instance_path: InstancePath,
) -> dict:
    """An undocumented endpoint which returns all external references in a document.

    Generally speaking, this returns a list of the external workspaces referenced by each tab in the instance.
    """
    return api.get(
        api_path("documents", instance_path, InstancePath, "externalreferences")
    )


# @deprecated("Possibly broken endpoint")
# def update_latest_references(
#     api: Api, element_path: api_path.ElementPath, element: str
# ) -> dict:
#     """Updates a tab's references to the latest versions."""
#     body = {"elements": [element]}
#     return api.post(
#         api_path.element_api_path(
#             "documents", element_path, "latestdocumentreferences"
#         ),
#         body=body,
#     )


def update_to_latest_reference(
    api: Api,
    element_path: ElementPath,
    old_reference_path: ElementPath,
) -> None:
    """Updates a reference to the latest version.

    This updates all features in the tab specified by element which reference
    objects in old_reference to use the latest version of that reference.
    """
    latest_version = get_latest_version(api, old_reference_path)["id"]
    update_reference(api, element_path, old_reference_path, latest_version)


def update_reference(
    api: Api,
    element_path: ElementPath,
    old_reference_path: ElementPath,
    version_id: str,
) -> None:
    """Updates all features in element which reference old_reference to the version
    of old_reference specified by version_id.
    """
    assert_workspace(element_path)
    assert_version(old_reference_path)
    body = {
        "referenceUpdates": [
            {
                "fromReference": {
                    "documentId": old_reference_path.document_id,
                    "versionId": old_reference_path.instance_id,
                    "elementId": old_reference_path.element_id,
                },
                "toReference": {
                    "documentId": old_reference_path.document_id,
                    "versionId": version_id,
                    "elementId": old_reference_path.element_id,
                },
            }
        ]
    }
    api.post(
        api_path("elements", element_path, ElementPath, "updatereferences"),
        body=body,
    )
