from flask import current_app as app
from flask import request

from library.api import api_base, api_path
from library.api.endpoint import assemblies


def execute():
    token = request.args["token"]
    if token == None:
        return {"error": "An onshape oauth token is required."}

    body = request.get_json()
    if body == None:
        return {"error": "A request body is required."}
    part_studio_path = api_path.make_element_path_from_obj(body)
    name = body["name"]

    api = api_base.ApiToken(token, logging=True)

    assembly = assemblies.make_assembly(api, part_studio_path.path, name)
    app.logger.info(assembly)

    # api_call.get_document_elements(api, path.path)

    return {"message": "Success"}
