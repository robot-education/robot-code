import enum

import flask
from google.cloud import firestore

import onshape_api
from backend.common import backend_exceptions, connect
from onshape_api import endpoints
from onshape_api.endpoints.permissions import Permission, get_permissions


class LinkType(enum.StrEnum):
    PARENTS = "parents"
    CHILDREN = "children"


def path_to_db_id(path: onshape_api.InstancePath) -> str:
    return make_db_id(path.document_id, path.instance_id)


def route_to_db_id() -> str:
    return path_to_db_id(connect.get_instance_path())


def make_db_id(document_id: str, workspace_id: str) -> str:
    # Use a vertical bar so firestore doesn't assume it's a path
    return document_id + "|" + workspace_id


def db_id_to_path(db_id: str) -> onshape_api.InstancePath:
    slash = db_id.find("|")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return onshape_api.InstancePath(document_id, workspace_id)


def make_document(api: onshape_api.Api, path: onshape_api.InstancePath) -> dict:
    permissions = get_permissions(api, path)
    if Permission.READ not in permissions:
        return {
            "documentId": path.document_id,
            "instanceId": path.instance_id,
            "isOpenable": False,
        }
    try:
        linked_document = endpoints.get_document(api, path)
        name = linked_document["name"]

        default_workspace_instance_id = linked_document["defaultWorkspace"]["id"]
        is_default_workspace = path.instance_id == default_workspace_instance_id
        if is_default_workspace:
            workspace_name = linked_document["defaultWorkspace"]["name"]
        else:
            instance_data = endpoints.get_instance_metadata(api, path)
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
        "isDefaultWorkspace": is_default_workspace,
        "workspaceName": workspace_name,
    }


router = flask.Blueprint("linked_documents", __name__)


@router.get("/linked-documents/<link_type>" + connect.instance_route())
def get_linked_documents(link_type: str, **kwargs):
    """Gets a list of documents (technically, workspaces) linked to the current document.

    Returns a list of linked documents with the fields:
        documentId:
        instanceId:
        name: The name of the document, or undefined if the document cannot be read.
        isDefaultWorkspace: True if the linked document is the default workspace.
        workspaceName: The name of the workspace.
    """
    if link_type not in LinkType:
        raise backend_exceptions.BackendException(
            "Invalid link_type {}.".format(link_type)
        )

    api = connect.get_api()
    curr_path = connect.get_instance_path()
    document_db_id = path_to_db_id(curr_path)
    backend_exceptions.require_permissions(api, curr_path, Permission.READ)

    doc = connect.db_linked_documents().document(document_db_id).get()
    linked_documents = []
    invalid_links = 0
    if doc.exists and (data := doc.to_dict()):
        for document_db_id in data.get(link_type, []):
            path = db_id_to_path(document_db_id)
            linked_documents.append(make_document(api, path))

    return linked_documents

    # tasks: list[asyncio.Task] = []
    # if doc.exists and (data := doc.to_dict()):
    #     async with asyncio.TaskGroup() as tg:
    #         for document_id in data.get(link_type, []):
    #             path = from_db_id(document_id)

    #             async def call():
    #                 return make_document(api, path)

    #             tasks.append(tg.create_task(call()))
    # return {"documents": [task.result() for task in tasks]}


@router.delete("/linked-documents/<link_type>" + connect.instance_route())
def delete_linked_document(link_type: LinkType, **kwargs):
    """Deletes a link from the url to the document specified in the query params.

    Query Args:
        documentId, workspaceId: The id of the document link to delete.
    """
    link_types = get_link_types(link_type)

    api = connect.get_api()
    curr_path = connect.get_instance_path()
    backend_exceptions.require_permissions(api, curr_path, Permission.WRITE)

    curr_id = path_to_db_id(curr_path)
    link_path = onshape_api.InstancePath(
        connect.get_query("documentId"), connect.get_query("instanceId")
    )
    link_id = path_to_db_id(link_path)

    db_ref = connect.db_linked_documents()
    db_ref.document(curr_id).update({link_types[0]: firestore.ArrayRemove([link_id])})
    db_ref.document(link_id).update({link_types[1]: firestore.ArrayRemove([curr_id])})
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
    api = connect.get_api()
    curr_path = connect.get_instance_path()
    curr_db_id = path_to_db_id(curr_path)
    backend_exceptions.require_permissions(api, curr_path, Permission.WRITE)

    link_path = onshape_api.InstancePath(
        connect.get_query("documentId"), connect.get_query("instanceId")
    )
    backend_exceptions.require_permissions(api, link_path, Permission.READ)

    link_db_id = path_to_db_id(link_path)
    link_types = get_link_types(link_type)

    if curr_db_id == link_db_id:
        raise backend_exceptions.UserException("Cannot link a document to itself.")
    link_document = make_document(api, link_path)

    db_ref = connect.db_linked_documents()
    add_document_link(db_ref, link_types[0], curr_db_id, link_db_id)
    add_document_link(db_ref, link_types[1], link_db_id, curr_db_id)
    return link_document


def get_link_types(link_type: LinkType) -> tuple[LinkType, LinkType]:
    if link_type == LinkType.PARENTS:
        return (LinkType.PARENTS, LinkType.CHILDREN)
    elif link_type == LinkType.CHILDREN:
        return (LinkType.CHILDREN, LinkType.PARENTS)
    raise backend_exceptions.BackendException("Invalid link_type {}.".format(link_type))
