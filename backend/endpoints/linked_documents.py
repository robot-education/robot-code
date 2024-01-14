import asyncio
import enum

import flask
from google.cloud import firestore

import onshape_api
from backend.common import backend_exceptions, setup
from onshape_api import endpoints


class LinkType(enum.StrEnum):
    PARENTS = "parents"
    CHILDREN = "children"


def object_to_db_id(path: onshape_api.InstancePath) -> str:
    return make_db_id(path.document_id, path.instance_id)


def route_to_db_id() -> str:
    return object_to_db_id(setup.get_instance_path())


def make_db_id(document_id: str, workspace_id: str) -> str:
    # Use a vertical bar so firestore doesn't assume it's a path
    return document_id + "|" + workspace_id


def from_db_id(db_id: str) -> onshape_api.InstancePath:
    slash = db_id.find("|")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return onshape_api.InstancePath(document_id, workspace_id)


def make_document(
    api: onshape_api.Api, path: onshape_api.InstancePath
) -> dict[str, str]:
    try:
        name = endpoints.get_document(api, path)["name"]
    except:
        raise backend_exceptions.UserException(
            "Provided documentId and/or workspaceId were invalid."
        )
    return {
        "documentId": path.document_id,
        "instanceId": path.instance_id,
        "name": name,
    }


router = flask.Blueprint("linked_documents", __name__)


@router.get("/linked-documents/<link_type>" + setup.document_route())
async def get_linked_documents(link_type: str, **kwargs):
    """Returns the parents and children linked to a given document."""
    if link_type not in LinkType:
        raise backend_exceptions.UserException(
            "Invalid link_type {}.".format(link_type)
        )

    db = setup.get_db()
    api = setup.get_api()
    document_id = route_to_db_id()
    doc = db.collection("documents").document(document_id).get()
    tasks = []
    if doc.exists and (data := doc.to_dict()):
        async with asyncio.TaskGroup() as tg:
            for document_id in data.get(link_type, []):
                path = from_db_id(document_id)

                async def call():
                    return make_document(api, path)

                tasks.append(tg.create_task(call()))
    return {"documents": [task.result() for task in tasks]}


@router.delete("/linked-documents/<link_type>" + setup.document_route())
def delete_linked_document(link_type: LinkType, **kwargs):
    """Deletes a link from the url to the document specified in the query params.

    Query Args:
        documentId, workspaceId: The id of the document link to delete.
    """
    link_types = get_link_types(link_type)

    api = setup.get_api()
    db = setup.get_db()

    curr_id = route_to_db_id()
    link_path = onshape_api.InstancePath(
        setup.get_query("documentId"), setup.get_query("workspaceId")
    )
    link_id = object_to_db_id(link_path)

    db_ref = db.collection("documents")
    db_ref.document(curr_id).update({link_types[0]: firestore.ArrayRemove([link_id])})
    db_ref.document(link_id).update({link_types[1]: firestore.ArrayRemove([curr_id])})
    return make_document(api, link_path)


def add_document_link(
    db_ref: firestore.CollectionReference, linkType: LinkType, path_id: str, new_id: str
):
    doc_ref = db_ref.document(path_id)
    if not doc_ref.get().exists:
        db_ref.add({linkType: [new_id]}, path_id)
    else:
        doc_ref.update({linkType: firestore.ArrayUnion([new_id])})


@router.post("/linked-documents/<link_type>" + setup.document_route())
def add_linked_document(link_type: LinkType, **kwargs):
    """Adds the document specified in the query parameters to the document specified in the url.

    This endpoint throws if the documentId and workspaceId passed as query params are invalid.

    Query Args:
        documentId, workspaceId: The id of the document to link.
    Returns:
        The documentId, workspaceId, and name of the newly linked document.
    """
    db = setup.get_db()
    api = setup.get_api()
    curr_id = route_to_db_id()
    link_path = onshape_api.InstancePath(
        setup.get_query("documentId"), setup.get_query("workspaceId")
    )
    link_id = object_to_db_id(link_path)
    link_types = get_link_types(link_type)

    if curr_id == link_id:
        raise backend_exceptions.UserException("Cannot link a document to itself.")
    # Additional error handling - ensures link is valid
    link_document = make_document(api, link_path)

    db_ref = db.collection("documents")
    add_document_link(db_ref, link_types[0], curr_id, link_id)
    add_document_link(db_ref, link_types[1], link_id, curr_id)
    return link_document


def get_link_types(link_type: LinkType) -> tuple[LinkType, LinkType]:
    if link_type == LinkType.PARENTS:
        return (LinkType.PARENTS, LinkType.CHILDREN)
    elif link_type == LinkType.CHILDREN:
        return (LinkType.CHILDREN, LinkType.PARENTS)
    raise backend_exceptions.UserException("Invalid link_type {}.".format(link_type))
