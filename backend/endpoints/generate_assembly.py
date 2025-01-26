import flask

from backend.common.backend_exceptions import require_permissions
from backend.common import connect, database
from onshape_api import model
from onshape_api.endpoints.permissions import Permission
from onshape_api.endpoints import assemblies
from onshape_api.paths.paths import ElementPath

router = flask.Blueprint("generate-assembly", __name__)


@router.post("/generate-assembly" + connect.element_route())
def generate_assembly(**kwargs: str):
    """Generates a new assembly from the given part studio.

    Returns:
        elementId: The element id of the generated assembly.
    """
    db = database.Database()
    api = connect.get_api(db)
    name = connect.get_body_arg("name")
    part_studio_path = connect.get_route_element_path()
    require_permissions(api, part_studio_path, Permission.READ, Permission.WRITE)

    element_id = assemblies.create_assembly(api, part_studio_path, name)["id"]
    assembly_path = ElementPath.from_path(part_studio_path, element_id)

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
