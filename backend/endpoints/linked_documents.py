import enum
from typing import Callable, cast

import flask
from google.cloud import firestore

from backend.common import backend_exceptions, connect, database
from onshape_api.api.api_base import Api
from onshape_api.endpoints import permissions
from onshape_api.endpoints.metadata import get_instance_metadata
from onshape_api.endpoints.permissions import (
    Permission,
    has_permissions,
)
from onshape_api.endpoints import documents
from onshape_api.paths.paths import InstancePath


class LinkType(enum.StrEnum):
    PARENTS = "parents"
    CHILDREN = "children"


def path_to_db_id(path: InstancePath) -> str:
    return make_db_id(path.document_id, path.instance_id)


def get_db_id_from_route() -> str:
    """Returns the database id from the current route."""
    return path_to_db_id(connect.get_route_instance_path())


def db_id_to_path(db_id: str) -> InstancePath:
    """Converts a database id to an Onshape path."""
    slash = db_id.find("|")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return InstancePath(document_id, workspace_id)


def make_db_id(document_id: str, workspace_id: str) -> str:
    # Use a vertical bar so firestore doesn't assume it's a path
    return document_id + "|" + workspace_id


router = flask.Blueprint("linked-documents", __name__)


@router.delete("/linked-documents/<link_type>" + connect.instance_route())
def delete_linked_document(link_type: LinkType, **kwargs):
    """Deletes a link from the url to the document specified in the query params.

    Query Args:
        documentId, workspaceId: The id of the document link to delete.
    """

    db = database.Database()
    api = connect.get_api(db)
    curr_path = connect.get_route_instance_path()
    curr_id = path_to_db_id(curr_path)
    backend_exceptions.require_permissions(api, curr_path, Permission.WRITE)

    link_types = get_link_types(link_type)
    link_path = InstancePath(
        connect.get_query_arg("documentId"), connect.get_query_arg("instanceId")
    )
    link_id = path_to_db_id(link_path)

    db.linked_documents.document(curr_id).update(
        {link_types[0]: firestore.ArrayRemove([link_id])}
    )
    db.linked_documents.document(link_id).update(
        {link_types[1]: firestore.ArrayRemove([curr_id])}
    )
    return make_document(api, link_path)


def add_document_link(
    db_ref: firestore.CollectionReference, linkType: LinkType, path_id: str, new_id: str
):
    doc_ref = db_ref.document(path_id)
    if not doc_ref.get().exists:
        doc_ref.set({linkType: [new_id]})
    else:
        doc_ref.update({linkType: firestore.ArrayUnion([new_id])})


@router.post("/linked-documents/<link_type>" + connect.instance_route())
def add_linked_document(link_type: LinkType, **kwargs):
    """Adds the document specified in the query parameters to the document specified in the url.

    This endpoint throws if the documentId and workspaceId passed as query params are invalid.

    Query Args:
        documentId, workspaceId: The id of the document to link.
    Returns the linked document (see also get_linked_documents).
    """
    db = database.Database()
    api = connect.get_api(db)

    curr_path = connect.get_route_instance_path()
    curr_db_id = path_to_db_id(curr_path)
    backend_exceptions.require_permissions(api, curr_path, Permission.WRITE)

    link_path = InstancePath(
        connect.get_query_arg("documentId"), connect.get_query_arg("instanceId")
    )
    backend_exceptions.require_permissions(api, link_path, Permission.READ)

    link_db_id = path_to_db_id(link_path)

    if curr_db_id == link_db_id:
        raise backend_exceptions.ReportedException("Cannot link a document to itself.")

    link_document = make_document(api, link_path)

    link_types = get_link_types(link_type)
    add_document_link(db.linked_documents, link_types[0], curr_db_id, link_db_id)
    add_document_link(db.linked_documents, link_types[1], link_db_id, curr_db_id)
    return link_document


def get_link_types(link_type: LinkType) -> tuple[LinkType, LinkType]:
    if link_type == LinkType.PARENTS:
        return (LinkType.PARENTS, LinkType.CHILDREN)
    elif link_type == LinkType.CHILDREN:
        return (LinkType.CHILDREN, LinkType.PARENTS)
    raise backend_exceptions.BackendException("Invalid link_type {}.".format(link_type))


@router.get("/linked-documents/<link_type>" + connect.instance_route())
def get_document_paths(link_type: str, **kwargs):
    """Gets a list of documents (technically, workspaces) linked to the current document.

    Query args:
        recursive:
            If true (and link_type is PARENTS), the list of documents will recursively include parents linked to the current document.
            The result will be topologically sorted such that linked dependencies are respected.

    Returns a list of linked documents with the fields:
        documentId:
        instanceId:
        isOpenable: Whether the document can be opened.
        isLinkable: Whether the user has link permissions.
        name: If the document can be opened, the name of the document.
        workspaceName: If the document can be opened, the name of the workspace.
    """
    if link_type not in LinkType:
        raise backend_exceptions.BackendException(
            "Invalid link_type {}.".format(link_type)
        )
    link_type = cast(LinkType, link_type)

    db = database.Database()
    api = connect.get_api(db)

    curr_path = connect.get_route_instance_path()
    backend_exceptions.require_permissions(api, curr_path, Permission.READ)

    recursive: bool = connect.get_optional_query_arg("recursive") == "true"
    if recursive:
        if link_type == LinkType.CHILDREN:
            raise backend_exceptions.ReportedException(
                "Cannot retrieve children recursively"
            )

        def parent_function(node: InstancePath) -> list[InstancePath]:
            return get_linked_document_paths(db, node)

        linked_document_paths = get_all_linked_parents(parent_function, curr_path)
    else:
        linked_document_paths = get_linked_document_paths(db, curr_path, link_type)
    return [make_document(api, path) for path in linked_document_paths]


def make_document(api: Api, path: InstancePath) -> dict:
    if not has_permissions(api, path, Permission.READ):
        return {
            "documentId": path.document_id,
            "instanceId": path.instance_id,
            "isOpenable": False,
            "isLinkable": False,
        }
    try:
        is_linkable = permissions.has_permissions(api, path, Permission.LINK)
        linked_document = documents.get_document(api, path)
        name = linked_document["name"]

        default_workspace_instance_id = linked_document["defaultWorkspace"]["id"]
        is_default_workspace = path.instance_id == default_workspace_instance_id
        if is_default_workspace:
            # Technically saves an Onshape call
            workspace_name = linked_document["defaultWorkspace"]["name"]
        else:
            instance_data = get_instance_metadata(api, path)
            workspace_name = next(
                data["value"]
                for data in instance_data["properties"]
                if data["name"] == "Name"
            )
    except:
        raise backend_exceptions.BackendException(
            "Unexpectedly failed to get name of linked document."
        )

    return {
        "documentId": path.document_id,
        "instanceId": path.instance_id,
        "isOpenable": True,
        "isLinkable": is_linkable,
        "name": name,
        "workspaceName": workspace_name,
    }


def get_linked_document_paths(
    db: database.Database,
    document_path: InstancePath,
    link_type: LinkType = LinkType.PARENTS,
):
    """Returns a list of documents linked from the given document_path."""
    document_db_id = path_to_db_id(document_path)
    doc = db.linked_documents.document(document_db_id).get()
    linked_paths = []
    if doc.exists and (data := doc.to_dict()):
        for document_db_id in data.get(link_type, []):
            linked_paths.append(db_id_to_path(document_db_id))
    return linked_paths


def get_all_linked_parents(
    parent_function: Callable[[InstancePath], list[InstancePath]], root: InstancePath
):
    """Returns a topologically sorted list of parents of the given document_path."""
    visited = set()
    stack = set()
    result = []

    def dfs(curr: InstancePath):
        if curr in stack:
            raise backend_exceptions.LinkedCycleException()
        if curr in visited:
            return

        stack.add(curr)
        visited.add(curr)

        linked_paths = parent_function(curr)
        for node in linked_paths:
            dfs(node)
        stack.remove(curr)
        result.append(curr)

    dfs(root)
    # Root ends up at the back since it's a dfs
    result.pop()
    return result[::-1]
