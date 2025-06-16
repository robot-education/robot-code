import flask

from backend.common import connect, database
from backend.endpoints import document_list
from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents
from onshape_api.endpoints.documents import ElementType
from onshape_api.endpoints.versions import get_latest_version_path
from onshape_api.paths.paths import (
    DocumentPath,
    ElementPath,
    url_to_instance_path,
)

router = flask.Blueprint("save-documents", __name__)


def save_document(api: Api, db: database.Database, document_path: DocumentPath) -> int:
    """Loads all of the elements of a given document into the database."""
    version_path = get_latest_version_path(api, document_path)

    contents = documents.get_contents(api, version_path)

    document = documents.get_document(api, document_path)
    if document["documentThumbnailElementId"] == None:
        # TODO: This should be an error message
        pass

    document_name = document["name"]

    element_paths: list[str] = []
    for element in contents["elements"]:
        element_type: ElementType = element["elementType"]
        if element_type not in [ElementType.PART_STUDIO, ElementType.ASSEMBLY]:
            continue

        name = element["name"]  # Use the name of the tab
        element_id = element["id"]
        path = ElementPath.from_path(version_path, element_id)

        # TODO: Parse and save configuration
        # encodedConfigurationParams = element.get("configurationParameters")
        # configurable = encodedConfigurationParams != None

        # "/" is reserved by google cloud
        safe_id = ElementPath.to_api_path(path).replace("/", "|")
        element_ref = db.elements.document(safe_id)
        element_ref.set(
            {
                "name": name,
                "type": element_type,
            }
        )
        element_paths.append(safe_id)

    db.documents.document(version_path.document_id).set(
        {
            "name": document_name,
            "elementIds": element_paths,
            "path": ElementPath.to_api_path(path),
        }
    )
    return len(element_paths)


@router.post("/save-all-documents")
def save_all_documents(**kwargs):
    """Saves the contents of the latest versions of all documents managed by FRC Design Lib into the database."""
    db = database.Database()
    api = connect.get_api(db)

    database.delete_collection(db.documents)
    database.delete_collection(db.elements)

    total = 0
    for document_url in document_list.documents_list:
        path = url_to_instance_path(document_url)
        latest_version_path = get_latest_version_path(api, path)
        total += save_document(api, db, latest_version_path)
    return {"savedElements": total}


# @router.post("/load-documents" + connect.element_route())
# def push_version(**kwargs):
#     """"""
# return {}
