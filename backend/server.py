import flask
import os
import dotenv
from api import exceptions

from backend.routes import (
    add_parent,
    assembly_mirror,
    generate_assembly,
    oauth,
    update_references,
)

import logging
import sys

log = logging.getLogger("oauthlib")
log.addHandler(logging.StreamHandler(sys.stdout))
log.setLevel(logging.DEBUG)

dotenv.load_dotenv()

app = flask.Flask(__name__)


@app.errorhandler(exceptions.ApiException)
def api_exception(e: exceptions.ApiException):
    return e.to_dict(), e.status_code


app.register_blueprint(oauth.router)
app.register_blueprint(generate_assembly.router)
app.register_blueprint(assembly_mirror.router)


@app.route("/add-parent", methods=["POST"])
def add_parent_route():
    """Adds a parent to the current document."""
    return add_parent.execute()


# @app.route("/auto-assembly", methods=["POST"])
# def auto_assembly_route():
#     return auto_assembly.execute()


@app.route("/update-references", methods=["POST"])
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


# @app.route("/next-version-name", methods=["GET"])
# def next_version_name_route():
#     """Returns the next default version name for a given document."""
#     api = setup.get_api()
#     document_path = setup.get_document_path()
#     versions = documents.get_versions(api, document_path)
#     # len(versions) is correct due to Start version
#     return {"name": "V{}".format(len(versions))}


if __name__ == "__main__":
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"
    # Only works with firefox setting sameSite.noneRequiresSecure to false
    app.config.update(
        # SESSION_COOKIE_SECURE=True,
        SESSION_COOKIE_NAME="robot-manager",
        # SESSION_COOKIE_SAMESITE="none",
        # SESSION_COOKIE_HTTPONLY=False,
        # SESSION_COOKIE_DOMAIN="localhost",
    )
    app.secret_key = os.getenv("SESSION_SECRET")

    app.run(
        debug=True,
        # ssl_context=("./credentials/cert.pem", "./credentials/key.pem"),
        port=int(os.getenv("PORT", 8080)),
    )
