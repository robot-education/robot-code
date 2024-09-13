import enum
from typing import Iterable
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
from onshape_api.utils.endpoint_utils import get_instance_type_key


def get_document(api: Api, document_path: DocumentPath) -> dict:
    """Retrieves a given document's metadata."""
    # documents endpoints are not idiomatic - no /d/
    return api.get(api_path("documents", end_id=document_path.document_id))


def create_new_workspace(
    api: Api, document_path: DocumentPath, name: str, description: str | None = None
) -> dict:
    """Creates a new workspace in a given document."""
    body = {"name": name}
    if description != None:
        body["description"] = description
    return api.post(
        api_path("documents", document_path, DocumentPath, "workspaces"), body=body
    )


def copy_workspace(
    api: Api, instance_path: InstancePath, new_name: str, is_public: bool = False
) -> dict:
    assert_workspace(instance_path)
    path = f"/documents/{instance_path.document_id}/workspaces/{instance_path.instance_id}/copy"
    body = {"isPublic": is_public, "newName": new_name}
    return api.post(path, body)


def create_new_workspace_from_instance(
    api: Api, path: InstancePath, name: str, description: str | None = None
) -> dict:
    """Creates a new workspace in a given document."""
    key = get_instance_type_key(path)
    body = {"name": name, key: path.instance_id}
    if description != None:
        body["description"] = description
    return api.post(api_path("documents", path, DocumentPath, "workspaces"), body=body)


def delete_workspace(api: Api, workspace_path: InstancePath) -> dict:
    """Deletes a workspace."""
    assert_instance_type(workspace_path, InstanceType.WORKSPACE)
    return api.delete(
        api_path(
            "documents",
            workspace_path,
            DocumentPath,
            "workspaces",
            workspace_path.instance_id,
        )
    )


def delete_document(api: Api, document_path: DocumentPath) -> dict:
    """Deletes an entire document."""
    return api.delete(f"/documents/{document_path.document_id}")


class ElementType(enum.StrEnum):
    """Describes possible element (tab) types in a document."""

    PART_STUDIO = "PARTSTUDIO"
    ASSEMBLY = "ASSEMBLY"
    DRAWING = "DRAWING"
    FEATURE_STUDIO = "FEATURESTUDIO"
    BLOB = "BLOB"


def get_document_elements(
    api: Api,
    instance_path: InstancePath,
    element_type: ElementType | None = None,
) -> list[dict]:
    """Fetches all elements in a document.

    Args:
        element_type: The type of element (tab) to get. If None, all elements are returned.
    """
    query: dict = {"withThumbnails": False}
    if element_type != None:
        query["elementType"] = element_type

    return api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query=query,
    )


def get_document_element(api: Api, element_path: ElementPath) -> dict | None:
    """Fetches an element in a document, or None if it doesn't exist."""
    query = {"withThumbnails": False, "elementId": element_path.element_id}
    response = api.get(
        api_path("documents", element_path, InstancePath, "elements"),
        query=query,
    )
    if len(response) == 1:
        return response[0]
    return None


def get_workspace_microversion_id(api: Api, instance_path: InstancePath) -> str:
    """Fetches the latest microversion id of a given workspace.

    Note this is the microversion associated with the workspace as a whole.
    Individual elements also have their own microversion ids which are unrelated to the workspace's.
    """
    assert_instance_type(instance_path, InstanceType.WORKSPACE, InstanceType.VERSION)
    return api.get(
        api_path("documents", instance_path, InstancePath, "currentmicroversion")
    )["microversion"]


def get_external_references(
    api: Api,
    instance_path: InstancePath,
) -> dict:
    """An undocumented OAuth only endpoint which returns all external references in a document.

    Generally speaking, this returns a list of the external workspaces referenced by each tab in the instance.
    """
    return api.get(
        api_path("documents", instance_path, InstancePath, "externalreferences")
    )


def update_to_latest_reference(
    api: Api,
    element_path: ElementPath,
    old_reference_path: ElementPath,
) -> None:
    """Updates references from element_path to old_reference_path automatically.

    Specifically, all features in element_path which reference
    objects in old_reference_path are updated to use the latest version of that reference.
    """
    latest_version = get_latest_version(api, old_reference_path)["id"]
    update_reference(api, element_path, old_reference_path, latest_version)


def update_reference(
    api: Api,
    element_path: ElementPath,
    old_reference_path: ElementPath,
    version_id: str,
) -> None:
    """Updates all features in element which reference old_reference_path to the version
    of old_reference_path specified by version_id.
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


def move_reference(
    api: Api,
    element_path: ElementPath,
    old_reference_path: ElementPath,
    new_reference_path: ElementPath,
) -> None:
    """Updates all features in element which reference old_reference_path to new_reference_path."""
    assert_workspace(element_path)
    assert_version(old_reference_path)
    assert_version(new_reference_path)
    body = {
        "referenceUpdates": [
            {
                "fromReference": {
                    "documentId": old_reference_path.document_id,
                    "versionId": old_reference_path.instance_id,
                    "elementId": old_reference_path.element_id,
                },
                "toReference": {
                    "documentId": new_reference_path.document_id,
                    "versionId": new_reference_path.instance_id,
                    "elementId": new_reference_path.element_id,
                },
            }
        ]
    }
    api.post(
        api_path("elements", element_path, ElementPath, "updatereferences"),
        body=body,
    )


def move_elements(
    api: Api,
    source_path: InstancePath,
    element_ids: Iterable[str],
    target_path: InstancePath,
    target_version_name: str,
) -> dict:
    """Moves one or more tabs from the source to the target."""
    assert_workspace(source_path)
    assert_workspace(target_path)
    body = {
        "elements": element_ids,
        "isPublic": False,
        "sourceDocumentId": source_path.document_id,
        "sourceWorkspaceId": source_path.instance_id,
        "targetDocumentId": target_path.document_id,
        "targetWorkspaceId": target_path.instance_id,
        "versionName": target_version_name,
    }
    return api.post(
        api_path("documents", source_path, InstancePath, "moveelement"), body
    )
