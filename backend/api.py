import flask
from api import exceptions
from api.endpoints import documents
from backend.common import setup

from backend.routes import (
    add_parent,
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


@router.route("/add-parent", methods=["POST"])
def add_parent_route():
    """Adds a parent to the current document."""
    return add_parent.execute()


# @app.route("/auto-assembly", methods=["POST"])
# def auto_assembly_route():
#     return auto_assembly.execute()


@router.route("/update-references", methods=["POST"])
def update_references_route():
    """Updates references in a document.

    Args:
        fromDocumentIds: A list of documentIds to look for when deciding what to update.
        If included, only references to the specified documents will be updated.
        Otherwise, all outdated references will be updated.
    Returns:
        updates: The number of references which were updated.
    """
    return update_references.execute()


# @app.route("/create-version", methods=["POST"])
# def create_version_route():
#     """Creates a version.

#     Returns the id of the created version.
#     """
#     api = setup.get_api()
#     document_path = setup.get_document_path()
#     result = documents.create_version(
#         api, document_path, setup.get_value("name"), setup.get_value("description")
#     )
#     return {"id": result["id"]}


@router.route("/next-version-name", methods=["GET"])
def next_version_name_route():
    """Returns the next default version name for a given document."""
    api = setup.get_api()
    document_path = setup.get_document_path()
    versions = documents.get_versions(api, document_path)
    # len(versions) is correct due to Start version
    return {"name": "V{}".format(len(versions))}


@router.route("/next-assembly-name", methods=["GET"])
def next_assembly_name_route():
    """Returns the next default assembly name for a given document."""
    api = setup.get_api()
    document_path = setup.get_document_path()
    assemblies = documents.get_document_elements(
        api, document_path, element_type=documents.ElementType.ASSEMBLY
    )
    return {"name": "Assembly {}".format(len(assemblies) + 1)}
