from api.endpoints import documents
from api import api_path

from backend.common import setup


def execute():
    api = setup.get_api()
    document_path = setup.get_document_path()
    document_ids = setup.get_optional_value("fromDocumentIds")

    updates = 0
    refs = documents.get_external_references(api, document_path)
    externalRefs = refs["elementExternalReferences"]
    for element_id, paths in externalRefs:
        target_path = api_path.ElementPath(document_path, element_id)
        for path in paths:
            if not path["isOutOfDate"]:
                continue
            document_id = path["documentId"]
            if document_ids != None and document_id not in document_ids:
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
