import enum
from http import HTTPStatus
import flask
from api import api_path, exceptions
from google.cloud import firestore
from api.endpoints import documents
from backend.common import setup


class LinkType(enum.StrEnum):
    PARENTS = "parents"
    CHILDREN = "children"


def path_to_db_id(path: api_path.DocumentPath) -> str:
    return make_db_id(path.document_id, path.workspace_id)


def route_to_db_id() -> str:
    return path_to_db_id(setup.get_document_path())


def make_db_id(document_id: str, workspace_id: str) -> str:
    # Use a vertical bar so firestore doesn't assume it's a path
    return document_id + "|" + workspace_id


def from_db_id(db_id: str) -> api_path.DocumentPath:
    slash = db_id.find("|")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return api_path.make_document_path(document_id, workspace_id)


def make_document(path: api_path.DocumentPath) -> dict[str, str]:
    api = setup.get_api()
    name = documents.get_document(api, path)["name"]
    return {
        "documentId": path.document_id,
        "workspaceId": path.workspace_id,
        "name": name,
    }


def get_document_keys(link_type: LinkType) -> tuple[str, str]:
    if link_type == LinkType.PARENTS:
        return ("parents", "children")
    elif link_type == LinkType.CHILDREN:
        return ("children", "parents")
    raise exceptions.ApiException(
        "Invalid link_type {}.".format(link_type), HTTPStatus.NOT_FOUND
    )


router = flask.Blueprint("linked_documents", __name__)


@router.get("/linked-documents/<link_type>" + setup.document_route())
def get_linked_documents(link_type: LinkType, **kwargs):
    """Returns the parents and children linked to a given document."""
    db = setup.get_db()
    document_id = route_to_db_id()
    doc = db.collection("documents").document(document_id).get()
    documents = []
    if doc.exists and (data := doc.to_dict()):
        # TODO: Execute in parallel
        for document_id in data.get(link_type, []):
            path = from_db_id(document_id)
            documents.append(make_document(path))
    return {"documents": documents}


@router.delete("/linked-documents/<link_type>" + setup.document_route())
def delete_linked_document(link_type: LinkType, **kwargs):
    """Deletes a link from the url to the document specified in the query params.

    Query Args:
        documentId, workspaceId: The id of the document link to delete.
    """
    keys = get_document_keys(link_type)

    db = setup.get_db()
    path = setup.get_document_path()
    path_id = path_to_db_id(path)
    link_path = api_path.make_document_path(
        setup.get_query("documentId"), setup.get_query("workspaceId")
    )
    link_id = path_to_db_id(link_path)

    db_ref = db.collection("documents")
    db_ref.document(path_id).update({keys[0]: firestore.ArrayRemove([link_id])})
    db_ref.document(link_id).update({keys[1]: firestore.ArrayRemove([path_id])})

    return make_document(link_path)


@router.post("/linked-documents/<link_type>" + setup.document_route())
def add_linked_document(link_type: LinkType, **kwargs):
    """Adds the document specified in the query parameters to the document specified in the url.

    Query Args:
        documentId, workspaceId: The id of the document to link.
        isParent: `true` if the link is a parent, false if it's a child.
    Returns:
        The documentId, workspaceId, and name of the linked document.
    """
    db = setup.get_db()
    path = setup.get_document_path()
    path_id = path_to_db_id(path)
    link_path = api_path.make_document_path(
        setup.get_query("documentId"), setup.get_query("workspaceId")
    )
    link_id = path_to_db_id(link_path)
    keys = get_document_keys(link_type)

    db_ref = db.collection("documents")
    # Link path_id to link_id
    doc_ref = db_ref.document(path_id)
    if not doc_ref.get().exists:
        db_ref.add({keys[0]: [link_id]}, path_id)
    else:
        doc_ref.update({keys[0]: firestore.ArrayUnion([link_id])})

    # Link link_id to path_id
    link_doc_ref = db_ref.document(link_id)
    if not link_doc_ref.get().exists:
        db_ref.add({keys[1]: [path_id]}, link_id)
    else:
        link_doc_ref.update({keys[1]: firestore.ArrayUnion([path_id])})

    return make_document(link_path)
