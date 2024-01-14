import flask

import onshape_api
from onshape_api import endpoints

from backend.common import setup
from onshape_api import model

router = flask.Blueprint("generate-assembly", __name__)


@router.post("/generate-assembly" + setup.element_route())
def generate_assembly(**kwargs: str):
    """Generates a new assembly from the given part studio.

    Returns:
        elementId: The element id of the generated assembly.
    """
    api = setup.get_api()
    name = setup.get_body("name")
    part_studio_path = setup.get_element_path()

    id = endpoints.create_assembly(api, part_studio_path, name)["id"]
    assembly_path = onshape_api.ElementPath.from_path(part_studio_path, id)

    endpoints.add_parts_to_assembly(api, assembly_path, part_studio_path)
    assembly_data = endpoints.get_assembly(api, assembly_path)
    instance_ids = [
        instance["id"] for instance in assembly_data["rootAssembly"]["instances"]
    ]

    queries = [model.occurrence_query(instance_id) for instance_id in instance_ids]
    group_mate = model.group_mate("Group", queries)
    endpoints.add_feature(api, assembly_path, group_mate)

    return {
        "elementId": assembly_path.element_id,
    }
