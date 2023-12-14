from firebase_admin.firestore import firestore
from backend.common import setup


def execute():
    db = setup.get_db()
    document_id = setup.get_document_id()
    parent_id = setup.get_value("parentId")

    documents = db.collection("documents")
    documents.document(document_id).update(
        {"parents": firestore.ArrayUnion([parent_id])}
    )
    documents.document(parent_id).update(
        {"children": firestore.ArrayUnion([document_id])}
    )
    return {"message": "Success"}
