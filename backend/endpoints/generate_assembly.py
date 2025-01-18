import flask

from backend.common.backend_exceptions import require_permissions
import onshape_api
from onshape_api import endpoints

from backend.common import connect
from onshape_api import model
from onshape_api.endpoints.permissions import Permission

router = flask.Blueprint("generate-assembly", __name__)


@router.post("/generate-assembly" + connect.element_route())
def generate_assembly(**kwargs: str):
    """Generates a new assembly from the given part studio.

    Returns:
        elementId: The element id of the generated assembly.
    """
    api = connect.get_api()
    name = connect.get_body("name")
    part_studio_path = connect.get_element_path()
    require_permissions(api, part_studio_path, Permission.READ, Permission.WRITE)

    element_id = endpoints.create_assembly(api, part_studio_path, name)["id"]
    assembly_path = onshape_api.ElementPath.from_path(part_studio_path, element_id)

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
