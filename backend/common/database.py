from google.cloud import firestore
from google.cloud.firestore import CollectionReference


class Database:
    def __init__(self):
        self.db = firestore.Client()

    @property
    def sessions(self) -> CollectionReference:
        return self.db.collection("sessions")

    @property
    def documents(self) -> CollectionReference:
        return self.db.collection("documents")

    @property
    def elements(self) -> CollectionReference:
        return self.db.collection("elements")


def delete_collection(coll_ref: CollectionReference, batch_size=500):
    if batch_size == 0:
        return

    docs = coll_ref.list_documents(page_size=batch_size)
    deleted = 0

    for doc in docs:
        doc.delete()
        deleted = deleted + 1

    if deleted >= batch_size:
        return delete_collection(coll_ref, batch_size)
