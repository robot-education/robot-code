import flask

from backend.common import database
from onshape_api.paths.instance_type import InstanceType

router = flask.Blueprint("get-values", __name__)


@router.get("/documents")
def get_documents(**kwargs):
    """Returns a list of the top level documents and elements to display to the user."""
    db = database.Database()

    documents = []
    elements = []

    for doc_ref in db.documents.stream():
        document = doc_ref.to_dict()
        element_ids = document["elementIds"]
        documents.append(
            {"id": doc_ref.id, "name": document["name"], "elementIds": element_ids}
        )
        for element_id in document["elementIds"]:
            element = db.elements.document(element_id).get().to_dict()
            if element == None:
                raise ValueError(f"Missing element with id {element_id}")

            result = {
                "id": element_id,
                "name": element["name"],
                "elementType": element["elementType"],
                "configurationId": element.get("configurationId"),
                # Copy properties from document so we don't have to parse backreference on client
                "documentId": doc_ref.id,
                "instanceId": document["instanceId"],
                "instanceType": InstanceType.VERSION,
                # Include element id again out of laziness so we can parse it on the client
                "elementId": element_id,
            }
            elements.append(result)

    return {"documents": documents, "elements": elements}


@router.get("/configuration/<configuration_id>")
def get_configuration(configuration_id: str):
    """Returns a specific configuration."""
    db = database.Database()
    result = db.configurations.document(configuration_id).get().to_dict()
    if result == None:
        raise ValueError(f"Failed to find configuration with id {configuration_id}")
    return result
