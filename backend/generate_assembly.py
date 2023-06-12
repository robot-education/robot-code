# import marshmallow

from flask import current_app as app
from flask import request

from library.api import api_base, api_path
from library.api.endpoints import assemblies, assembly_features


def execute():
    auth = request.headers.get("Authentication", None)
    if auth == None:
        return {"error": "An onshape oauth token is required."}
    token = auth.removeprefix("Basic").strip()

    body = request.get_json()
    if body == None:
        return {"error": "A request body is required."}
    part_studio_path = api_path.make_element_path_from_obj(body)
    name = body["name"]

    api = api_base.ApiToken(token, logging=False)

    id = assemblies.make_assembly(api, part_studio_path.path, name)["id"]
    assembly_path = api_path.ElementPath(part_studio_path.path, id)

    assemblies.add_part_studio_to_assembly(api, assembly_path, part_studio_path)
    assembly = assemblies.get_assembly(api, assembly_path)
    instance_ids = [
        instance["id"] for instance in assembly["rootAssembly"]["instances"]
    ]

    queries = [
        assembly_features.occurrence_query(instance_id) for instance_id in instance_ids
    ]
    group_mate = assembly_features.group_mate("Group", queries)
    assemblies.add_feature(api, assembly_path, group_mate)

    return {"elementId": assembly_path.element_id}
