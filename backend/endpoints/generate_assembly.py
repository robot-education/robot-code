import flask

import onshape_api

from backend.common import connect, database
from onshape_api import model
from onshape_api.endpoints import assemblies

router = flask.Blueprint("generate-assembly", __name__)


@router.post("/generate-assembly" + connect.element_route())
def generate_assembly(**kwargs: str):
    """Generates a new assembly from the given part studio.

    Returns:
        elementId: The element id of the generated assembly.
    """
    db = database.Database()
    api = connect.get_api(db)
    name = connect.get_body("name")
    part_studio_path = connect.get_element_path()

    element_id = assemblies.create_assembly(api, part_studio_path, name)["id"]
    assembly_path = onshape_api.ElementPath.from_path(part_studio_path, element_id)

    assemblies.add_parts(api, assembly_path, part_studio_path)
    assembly_data = assemblies.get_assembly(api, assembly_path)
    instance_ids = [
        instance["id"] for instance in assembly_data["rootAssembly"]["instances"]
    ]
    queries = [model.occurrence_query(instance_id) for instance_id in instance_ids]
    group_mate = model.group_mate("Group", queries)
    assemblies.add_feature(api, assembly_path, group_mate)

    return {
        "elementId": assembly_path.element_id,
    }
