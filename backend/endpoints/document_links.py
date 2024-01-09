import flask
from firebase_admin.firestore import firestore
from api import api_path
from backend.common import setup


def path_to_db_id(path: api_path.DocumentPath) -> str:
    return path.document_id + "/" + path.workspace_id


def to_db_id(document_id: str, workspace_id: str) -> str:
    return document_id + "/" + workspace_id


def from_db_id(db_id: str) -> api_path.DocumentPath:
    slash = db_id.find("/")
    document_id = db_id[:slash]
    workspace_id = db_id[slash + 1 :]
    return api_path.make_document_path(document_id, workspace_id)


router = flask.Blueprint("document_links", __name__)


@router.get("/document-links/d/<document_id>/w/<workspace_id>")
def get_document_links(document_id: str, workspace_id: str):
    """Returns the parents and children linked to a given document."""
    return {}


@router.delete("/document-links/d/<document_id>/w/<workspace_id>")
def delete_document_link(document_id: str, workspace_id: str):
    """Deletes the document specified in the query from the document specified in the link.
    The query is expected to include the parent.
    """
    db = setup.get_db()
    path = to_db_id(document_id, workspace_id)
    parent_path = to_db_id(setup.get_arg("documentId"), setup.get_arg("workspaceId"))

    documents = db.collection("documents")
    documents.document(path).update({"parents": firestore.ArrayRemove([parent_path])})
    return {"message": "Success"}


@router.post("/document-links/d/<document_id>/w/<workspace_id>")
def add_document_link(document_id: str, workspace_id: str):
    """Adds the document specified in the query parameters to the document specified in the url."""
    db = setup.get_db()
    path = to_db_id(document_id, workspace_id)
    parent_path = to_db_id(setup.get_arg("documentId"), setup.get_arg("workspaceId"))

    documents = db.collection("documents")
    documents.document(path).update({"parents": firestore.ArrayUnion([parent_path])})
    return {"message": "Success"}
