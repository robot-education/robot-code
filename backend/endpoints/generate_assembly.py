import flask
from api import api_path
from api.endpoints import assemblies, assembly_features

from backend.common import setup

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

    id = assemblies.create_assembly(api, part_studio_path, name)["id"]
    assembly_path = api_path.ElementPath(part_studio_path, id)

    assemblies.add_parts_to_assembly(api, assembly_path, part_studio_path)
    assembly = assemblies.get_assembly(api, assembly_path)
    instance_ids = [
        instance["id"] for instance in assembly["rootAssembly"]["instances"]
    ]

    queries = [
        assembly_features.occurrence_query(instance_id) for instance_id in instance_ids
    ]
    group_mate = assembly_features.group_mate("Group", queries)
    assemblies.add_feature(api, assembly_path, group_mate)

    return {
        "documentId": assembly_path.document_id,
        "workspaceId": assembly_path.workspace_id,
        "workspaceOrVersion": assembly_path.workspace_or_version,
        "elementId": assembly_path.element_id,
    }
