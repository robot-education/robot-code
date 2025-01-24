from google.cloud import firestore


class Database:
    def __init__(self):
        self.db = firestore.Client()

    @property
    def sessions(self) -> firestore.CollectionReference:
        return self.db.collection("sessions")

    @property
    def linked_documents(self) -> firestore.CollectionReference:
        return self.db.collection("linked-documents")
