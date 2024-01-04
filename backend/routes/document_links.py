import flask
from firebase_admin.firestore import firestore
from api import api_path
from backend.common import setup


router = flask.Blueprint("document_links", __name__)


@router.get("/document-links/<document_id>/<workspace_id>")
def get_document_links(document_id: str, workspace_id: str):
    """Returns the linked parents and children of a given document."""
    return {}


@router.post("/document-links/<document_id>/<workspace_id>")
def add_document_link(document_id: str, workspace_id: str):
    """Links a document to the given document."""
    db = setup.get_db()
    path = document_id + "/" + workspace_id
    # document_path = api_path.make_document_path(document_id, workspace_id)
    parent_id = setup.get_value("parentId")

    documents = db.collection("documents")
    documents.document(path).update({"parents": firestore.ArrayUnion([parent_id])})
    documents.document(path).update({"children": firestore.ArrayUnion([document_id])})
    return {"message": "Success"}


@router.delete("/document-links/<document_id>/<workspace_id>")
def delete_document_link(document_id: str, workspace_id: str):
    """Deletes a document link from the given document."""
    db = setup.get_db()
    path = document_id + "/" + workspace_id
    # document_path = api_path.make_document_path(document_id, workspace_id)
    parent_id = setup.get_value("parentId")

    documents = db.collection("documents")
    documents.document(path).update({"parents": firestore.ArrayUnion([parent_id])})
    documents.document(path).update({"children": firestore.ArrayUnion([document_id])})
    return {"message": "Success"}
