import flask

from backend.common import database
from onshape_api.paths.paths import (
    DocumentPath,
    ElementPath,
    api_path_to_element_path,
)

router = flask.Blueprint("get-documents", __name__)


# Index elements in the db by document_id and element_id since that uniquely identifies a tab
def element_db_path(element_path: ElementPath) -> str:
    return element_path.document_id + "/" + element_path.element_id


@router.get("/documents")
def get_documents(**kwargs):
    """Returns a list of the top level documents and elements to display to the user."""
    db = database.Database()

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
        element_id = element_ref.id
        element = element_ref.to_dict()
        result = {
            "id": element_id,
            "name": element["name"],
        }
        path = api_path_to_element_path(element_id.replace("|", "/"))
        result.update(ElementPath.to_api_object(path))
        elements.append(result)

    return {"documents": documents, "elements": elements}


# @router.post("/load-documents" + connect.element_route())
# def push_version(**kwargs):
#     """"""
# return {}
