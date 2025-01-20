import enum

import flask
from google.cloud import firestore

from backend.common import backend_exceptions, connect, database
from onshape_api.api.api_base import Api
from onshape_api.endpoints.metadata import get_instance_metadata
from onshape_api.endpoints.permissions import (
    Permission,
    get_permissions,
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
        connect.get_query("documentId"), connect.get_query("instanceId")
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
    Returns:
        The documentId, workspaceId, and name of the newly linked document.
    """
    db = database.Database()
    api = connect.get_api(db)

    curr_path = connect.get_route_instance_path()
    curr_db_id = path_to_db_id(curr_path)
    backend_exceptions.require_permissions(api, curr_path, Permission.WRITE)

    link_path = InstancePath(
        connect.get_query("documentId"), connect.get_query("instanceId")
    )
    backend_exceptions.require_permissions(api, link_path, Permission.READ)

    link_db_id = path_to_db_id(link_path)

    if curr_db_id == link_db_id:
        raise backend_exceptions.ClientException("Cannot link a document to itself.")
    link_document = make_document(api, link_path)

    link_types = get_link_types(link_type)
    add_document_link(db.linked_documents, link_types[0], curr_db_id, link_db_id)
    add_document_link(db.linked_documents, link_types[1], link_db_id, curr_db_id)
    return link_document


@router.get("/linked-documents/<link_type>" + connect.instance_route())
def get_linked_documents(link_type: str, **kwargs):
    """Gets a list of documents (technically, workspaces) linked to the current document.

    Returns a list of linked documents with the fields:
        documentId:
        instanceId:
        name: The name of the document, or undefined if the document cannot be read.
        workspaceName: The name of the workspace.
    """
    if link_type not in LinkType:
        raise backend_exceptions.BackendException(
            "Invalid link_type {}.".format(link_type)
        )
    db = database.Database()
    api = connect.get_api(db)
    curr_path = connect.get_route_instance_path()
    backend_exceptions.require_permissions(api, curr_path, Permission.READ)
    document_db_id = path_to_db_id(curr_path)

    doc = db.linked_documents.document(document_db_id).get()
    linked_documents = []
    if doc.exists and (data := doc.to_dict()):
        for document_db_id in data.get(link_type, []):
            path = db_id_to_path(document_db_id)
            linked_documents.append(make_document(api, path))

    return linked_documents


def make_document(api: Api, path: InstancePath) -> dict:
    if not has_permissions(api, path, Permission.READ):
        return {
            "documentId": path.document_id,
            "instanceId": path.instance_id,
            "isOpenable": False,
        }
    try:
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
        "name": name,
        "workspaceName": workspace_name,
    }


def get_link_types(link_type: LinkType) -> tuple[LinkType, LinkType]:
    if link_type == LinkType.PARENTS:
        return (LinkType.PARENTS, LinkType.CHILDREN)
    elif link_type == LinkType.CHILDREN:
        return (LinkType.CHILDREN, LinkType.PARENTS)
    raise backend_exceptions.BackendException("Invalid link_type {}.".format(link_type))
