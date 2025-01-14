import enum

import flask
from google.cloud import firestore

import onshape_api
from backend.common import backend_exceptions, connect, database
from onshape_api.endpoints import documents


class LinkType(enum.StrEnum):
    PARENTS = "parents"
    CHILDREN = "children"


def path_to_db_id(path: onshape_api.InstancePath) -> str:
    """Converts an Onshape path to a database id."""
    return make_db_id(path.document_id, path.instance_id)


def get_db_id_from_route() -> str:
    """Returns the database id from the current route."""
    return path_to_db_id(connect.get_route_instance_path())


def db_id_to_path(db_id: str) -> onshape_api.InstancePath:
    """Converts a database id to an Onshape path."""
    slash = db_id.find("|")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return onshape_api.InstancePath(document_id, workspace_id)


def make_db_id(document_id: str, workspace_id: str) -> str:
    # Use a vertical bar so firestore doesn't assume it's a path
    return document_id + "|" + workspace_id


def make_document(
    api: onshape_api.Api, path: onshape_api.InstancePath
) -> dict[str, str]:
    try:
        name = documents.get_document(api, path)["name"]
    except:
        raise backend_exceptions.UserException(
            "Provided documentId and/or instanceId were invalid."
        )
    return {
        "documentId": path.document_id,
        "instanceId": path.instance_id,
        "name": name,
    }


router = flask.Blueprint("linked_documents", __name__)


@router.get("/linked-documents/<link_type>" + connect.instance_route())
def get_linked_documents(link_type: str, **kwargs):
    """Returns the documents linked to a given document."""
    if link_type not in LinkType:
        raise backend_exceptions.UserException(
            "Invalid link_type {}.".format(link_type)
        )

    db = database.Database()
    api = connect.get_api(db)
    document_id = get_db_id_from_route()
    doc = db.linked_documents.document(document_id).get()
    documents = []
    if doc.exists and (data := doc.to_dict()):
        for document_id in data.get(link_type, []):
            path = db_id_to_path(document_id)
            documents.append(make_document(api, path))

    return {"documents": documents}

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

    db = database.Database()
    api = connect.get_api(db)

    curr_id = get_db_id_from_route()
    link_path = onshape_api.InstancePath(
        connect.get_query("documentId"), connect.get_query("instanceId")
    )
    link_id = path_to_db_id(link_path)

    db_ref = db.linked_documents
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
    db = database.Database()
    api = connect.get_api(db)
    curr_id = get_db_id_from_route()
    link_path = onshape_api.InstancePath(
        connect.get_query("documentId"), connect.get_query("instanceId")
    )
    link_id = path_to_db_id(link_path)
    link_types = get_link_types(link_type)

    if curr_id == link_id:
        raise backend_exceptions.UserException("Cannot link a document to itself.")
    # Additional error handling - ensures link is valid
    link_document = make_document(api, link_path)

    db_ref = db.linked_documents
    add_document_link(db_ref, link_types[0], curr_id, link_id)
    add_document_link(db_ref, link_types[1], link_id, curr_id)
    return link_document


def get_link_types(link_type: LinkType) -> tuple[LinkType, LinkType]:
    if link_type == LinkType.PARENTS:
        return (LinkType.PARENTS, LinkType.CHILDREN)
    elif link_type == LinkType.CHILDREN:
        return (LinkType.CHILDREN, LinkType.PARENTS)
