import flask
from api.endpoints import documents
from api import api_path

from backend.common import setup


router = flask.Blueprint("update-references", __name__)


@router.post("/update-references/d/<document_id>/w/<workspace_id>")
def update_references(document_id: str, workspace_id: str):
    """Updates references in a given document.

    Args:
        childDocumentIds: A list of documentIds to look for when deciding what to update.
        If included, only references stemming from the specified documents will be updated.
        Otherwise, all outdated references will be updated.
    Returns:
        updates: The number of references which were updated.
    """
    api = setup.get_api()
    document_path = api_path.make_document_path(document_id, workspace_id)
    child_document_ids = setup.get_optional_value("childDocumentIds")

    updates = 0
    refs = documents.get_external_references(api, document_path)
    externalRefs = refs["elementExternalReferences"]
    for element_id, paths in externalRefs:
        target_path = api_path.ElementPath(document_path, element_id)
        for path in paths:
            if not path["isOutOfDate"]:
                continue
            document_id = path["documentId"]
            if child_document_ids != None and document_id not in child_document_ids:
                continue
            current_document_path = api_path.DocumentPath(document_id, path["id"], "v")
            for referenced_element in path["referencedElements"]:
                current_path = api_path.ElementPath(
                    current_document_path, referenced_element
                )
                # TODO: multithread
                documents.update_to_latest_reference(api, target_path, current_path)
                updates += 1

    return {"updates": updates}
