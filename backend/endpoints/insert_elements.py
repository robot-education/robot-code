import flask

from backend.common import connect, database
from onshape_api.endpoints.assemblies import add_element_to_assembly
from onshape_api.paths.paths import (
    ElementPath,
)

router = flask.Blueprint("insert-elements", __name__)


@router.post("/add-to-assembly" + connect.element_route())
def add_to_assembly(**kwargs):
    """Adds the contents of an element to the current assembly."""
    db = database.Database()
    api = connect.get_api(db)
    assembly_path = connect.get_route_element_path()
    path_to_add = connect.get_body_element_path()
    element_type = connect.get_body_arg("elementType")

    add_element_to_assembly(api, assembly_path, path_to_add, element_type)
    return {"success": True}


@router.post("/add-to-part-studio" + connect.element_route())
def add_to_part_studio(**kwargs):
    """Adds the contents of an element to the current part studio."""
    db = database.Database()
    api = connect.get_api(db)
    part_studio_path = connect.get_route_element_path()
    path_to_add = connect.get_body_element_path()

    # add_element_to_assembly(api, path_to_modify, path_to_add)
    # TODO: Create a derive feature in the part studio
    return {"success": True}
