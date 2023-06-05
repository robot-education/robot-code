from flask import current_app as app
from flask import request

from library.api import api_base, api_path


def execute():
    token = request.args["token"]
    if token == None:
        return {"error": "An onshape oauth token is required."}

    body = request.get_json()
    if body == None:
        return {"error": "A request body is required."}

    api = api_base.ApiToken(token, logging=True)

    path = api_path.make_element_path_from_obj(body)
    app.logger.info(str(path))

    # api_call.get_document_elements(api, path.path)

    return {"message": "Success"}
