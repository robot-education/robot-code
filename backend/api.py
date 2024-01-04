import flask
from api import api_path, exceptions
from api.endpoints import documents
from backend.common import setup

from backend.routes import (
    assembly_mirror,
    generate_assembly,
    update_references,
)


router = flask.Blueprint("api", __name__, url_prefix="/api")


@router.errorhandler(exceptions.ApiException)
def api_exception(e: exceptions.ApiException):
    return e.to_dict(), e.status_code


router.register_blueprint(generate_assembly.router)
router.register_blueprint(assembly_mirror.router)
router.register_blueprint(update_references.router)


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


@router.get("/default-name/<document_id>/<workspace_id>")
def default_name(document_id: str, workspace_id: str):
    """Returns the next default name for a given element type in a document.

    Args:
        elementType: The type of element to fetch.
            Either PART_STUDIO, ASSEMBLY, or VERSION.
    """
    api = setup.get_api()
    document_path = api_path.DocumentPath(document_id, workspace_id)
    element_type = setup.get_arg("elementType")
    if element_type == "VERSION":
        versions = documents.get_versions(api, document_path)
        # len(versions) is correct due to Start version
        return {"name": "V{}".format(len(versions))}
    elif element_type == "ASSEMBLY":
        assemblies = documents.get_document_elements(
            api, document_path, element_type=documents.ElementType.ASSEMBLY
        )
        return {"name": "Assembly {}".format(len(assemblies) + 1)}
    elif element_type == "PART_STUDIO":
        part_studios = documents.get_document_elements(
            api, document_path, element_type=documents.ElementType.PART_STUDIO
        )
        return {"name": "Part Studio {}".format(len(part_studios) + 1)}

    raise exceptions.ApiException(
        "Received invalid value for query parameter elementType: {}".format(
            element_type
        )
    )
