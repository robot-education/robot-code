import http
import flask

import onshape_api
from onshape_api import endpoints

from backend.common import backend_exceptions, connect
from backend.endpoints import (
    assembly_mirror,
    generate_assembly,
    linked_documents,
    references,
)


router = flask.Blueprint("api", __name__, url_prefix="/api", static_folder="dist")


@router.errorhandler(onshape_api.ApiError)
def api_exception(e: onshape_api.ApiError):
    return e.to_dict(), e.status_code


@router.errorhandler(backend_exceptions.UserException)
def user_exception(e: backend_exceptions.UserException):
    return e.to_dict(), e.status_code


router.register_blueprint(generate_assembly.router)
router.register_blueprint(assembly_mirror.router)
router.register_blueprint(linked_documents.router)
router.register_blueprint(references.router)


# @app.post("/auto-assembly")
# def auto_assembly_route():
#     return auto_assembly.execute()


# @app.post("/create-version")
# def create_version():
#     """Creates a version.

#     Returns the id of the created version.
#     """
#     api = setup.get_api()
#     document_path = setup.get_document_path()
#     result = documents.create_version(
#         api, document_path, setup.get_value("name"), setup.get_value("description")
#     )
#     return {"id": result["id"]}


@router.get("/default-name/<element_type>" + connect.document_route("wv"))
def default_name(element_type: str, **kwargs):
    """Returns the next default name for a given element type in a document.

    Route Args:
        element_type: The type of element to fetch. One of part-studio, assembly, or version.
    """
    api = connect.get_api()
    document_path = connect.get_instance_path("wv")
    if element_type == "version":
        version_list = endpoints.get_versions(api, document_path)
        # len(versions) is correct due to Start version
        return {"name": "V{}".format(len(version_list))}
    elif element_type == "assembly":
        assemblies = endpoints.get_document_elements(
            api, document_path, element_type=endpoints.ElementType.ASSEMBLY
        )
        return {"name": "Assembly {}".format(len(assemblies) + 1)}
    elif element_type == "part-studio":
        part_studios = endpoints.get_document_elements(
            api, document_path, element_type=endpoints.ElementType.PART_STUDIO
        )
        return {"name": "Part Studio {}".format(len(part_studios) + 1)}

    raise onshape_api.ApiError(
        "Received invalid value for route arg element_type: {}".format(element_type),
        http.HTTPStatus.NOT_FOUND,
    )
