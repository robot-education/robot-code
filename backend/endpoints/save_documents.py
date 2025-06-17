import flask
import json5

from backend.common import connect, database
from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents
from onshape_api.endpoints.configurations import get_configuration
from onshape_api.endpoints.documents import ElementType
from onshape_api.endpoints.versions import get_latest_version_path
from onshape_api.paths.paths import (
    ElementPath,
    InstancePath,
    url_to_instance_path,
)

router = flask.Blueprint("save-documents", __name__)


def save_document(api: Api, db: database.Database, version_path: InstancePath) -> int:
    """Loads all of the elements of a given document into the database."""
    contents = documents.get_contents(api, version_path)

    element_paths: list[str] = []
    for element in contents["elements"]:
        element_type: ElementType = element["elementType"]
        if element_type not in [ElementType.PART_STUDIO, ElementType.ASSEMBLY]:
            continue

        name = element["name"]  # Use the name of the tab
        path = ElementPath.from_path(version_path, element["id"])

        configuration = get_configuration(api, path)
        configurable = configuration["configurationParameters"] != []
        if configurable:
            # TODO: Parse and save configuration
            pass

        # encodedConfigurationParams = element.get("configurationParameters")
        # configurable = encodedConfigurationParams != None

        element_db_id = database.make_element_db_id(path)
        element_db_value = {
            "name": name,
            "type": element_type,
            "instanceId": version_path.instance_id,
            "elementType": element_type,
            "configurable": configurable,
        }
        if configurable:
            # Re-use element db ids since configurations can't be shared
            element_db_value["configurationId"] = element_db_id

        element_ref = db.elements.document(element_db_id)
        element_ref.set(element_db_value)

        element_paths.append(element_db_id)

    document = documents.get_document(api, version_path)
    if document["documentThumbnailElementId"] == None:
        # TODO: Report an error
        pass

    document_name = document["name"]
    db.documents.document(version_path.document_id).set(
        {
            "name": document_name,
            "elementIds": element_paths,
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
    database.delete_collection(db.configurations)

    with open("config.json") as file:
        config = json5.load(file)

    documents_list = config["documents"]

    count = 0
    for document_url in documents_list:
        path = url_to_instance_path(document_url)
        latest_version_path = get_latest_version_path(api, path)
        count += save_document(api, db, latest_version_path)

    return {"savedElements": count}
