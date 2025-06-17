import flask

from backend.common import database
from onshape_api.paths.paths import (
    ElementPath,
    path_to_frontend_dict,
)

router = flask.Blueprint("get-documents", __name__)


@router.get("/documents")
def get_documents(**kwargs):
    """Returns a list of the top level documents and elements to display to the user."""
    db = database.Database()

    try:
        documents = []
        for doc_ref in db.documents.stream():
            document = doc_ref.to_dict()
            documents.append(
                {
                    "id": doc_ref.id,
                    "name": document["name"],
                    "elementIds": document["elementIds"],
                }
            )

        elements = []
        for element_ref in db.elements.stream():
            element_db_id = element_ref.id
            element = element_ref.to_dict()

            result = {
                "id": element_db_id,
                "name": element["name"],
                "elementType": element["elementType"],
            }

            # Add documentId, instanceId, instanceType, elementId to result
            path = database.parse_element_db_id(element_db_id, element["instanceId"])
            result.update(path_to_frontend_dict(path))

            elements.append(result)
    except:
        return {"documents": [], "elements": []}

    return {"documents": documents, "elements": elements}
