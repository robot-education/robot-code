import flask
from backend.common import connect, database
from onshape_api.endpoints.documents import (
    copy_workspace,
    delete_document,
    get_document_elements,
    move_elements,
)
from onshape_api.endpoints.part_studios import create_part_studio
from onshape_api.paths.paths import InstancePath

router = flask.Blueprint("add-design", __name__)

# @router.get("/get-elements" + connect.instance_route())
# def get_elements(**kwargs):
#     """Retrieves an array of element `id`s and `name`s in a document."""
#     db = database.Database()
#     api = connect.get_api(db)
#     target_path = connect.get_instance_path()

#     elements = get_document_elements(api, target_path)

#     result = []
#     for element in elements:
#         result.append(
#             {
#                 "name": element["name"],
#                 "id": element["id"],
#                 "elementType": element["elementType"],
#             }
#         )

#     return result


@router.post("/add-design" + connect.instance_route())
def add_design(**kwargs):
    """Adds a design to the current instance by copying the document and then moving one or more tabs over.

    Note: If a subset of tabs are moved, local references from the moved tabs to tabs that are left behind
    get converted into references to a version of the temporary document that was deleted.

    Rebinding those references isn't reliable since the original document might not have a version to rebind to.
    """
    db = database.Database()
    api = connect.get_api(db)

    target_path = connect.get_instance_path()
    design_path = InstancePath(
        connect.get_body("documentId"), connect.get_body("instanceId")
    )
    element_names: list[str] | None = connect.get_optional_body("elementNames")
    version_name: str = connect.get_body("versionName")

    # Copy design document to avoid impacting other users
    copy_data = copy_workspace(api, design_path, "TEMP")
    copy_path = InstancePath(copy_data["newDocumentId"], copy_data["newWorkspaceId"])
    elements = get_document_elements(api, copy_path)

    if element_names != None:
        elements_to_move: list[str] = [
            element["id"] for element in elements if element["name"] in element_names
        ]
    else:
        elements_to_move: list[str] = [element["id"] for element in elements]
    if len(elements_to_move) >= len(elements):
        # Create a temporary part studio to avoid emptying the document completely (which isn't allowed)
        create_part_studio(api, copy_path, "TEMP")

    # Perform the move
    move_elements(api, copy_path, elements_to_move, target_path, version_name)

    # Cleanup copy
    delete_document(api, copy_path)
    return {"message": "Success"}
