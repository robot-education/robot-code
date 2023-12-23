import flask
import os
import dotenv
from requests_oauthlib import OAuth2Session
from api import exceptions

from api.endpoints import documents
from backend.routes import (
    add_parent,
    assembly_mirror,
    generate_assembly,
    update_references,
)
from backend.common import setup

dotenv.load_dotenv()


app = flask.Flask(__name__)

app.secret_key = os.getenv("SESSION_KEY")
os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

client_id = os.getenv("ONSHAPE_CLIENT_ID")
client_secret = os.getenv("ONSHAPE_CLIENT_SECRET")

# May need to be http://localhost:8080?
redirect_url = os.getenv("CALLBACK_URL")

base_url = "https://oauth.onshape.com/oauth"
auth_base_url = base_url + "/authorize"
token_url = base_url + "/token"


@app.errorhandler(exceptions.ApiException)
def api_exception(e: exceptions.ApiException):
    return e.to_dict(), e.status_code


@app.route("/")
def sign_in():
    onshape = OAuth2Session(client_id, redirect_url=redirect_url)
    auth_url, state = onshape.authorization_url(auth_base_url)
    flask.session["oauth_state"] = state
    return flask.redirect(auth_url)


@app.route("/token", methods=["GET"])
def token():
    onshape = OAuth2Session(
        client_id, state=flask.session["oauth_state"], redirect_uri=redirect_url
    )
    token = onshape.fetch_token(
        token_url, client_secret=client_secret, authorization_response=flask.request.url
    )
    flask.session["oauth_token"] = token
    return flask.redirect(flask.url_for(".documents"))


@app.route("/documents", methods=["GET"])
def get_documents():
    onshape = OAuth2Session(
        client_id, token=flask.session["oauth_token"], redirect_uri=redirect_url
    )
    # Refresh flow, I guess
    flask.session["oauth_token"] = onshape.refresh_token(
        token_url, client_id=client_id, client_secret=client_secret
    )
    return onshape.get(
        "https://cad.onshape.com/api/v6/documents?q=Untitled&ownerType=1&sortColumn=createdAt&sortOrder=desc&offset=0&limit=20"
    ).json()


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


@app.route("/create-version", methods=["POST"])
def create_version_route():
    """Creates a version.

    Returns the id of the created version.
    """
    api = setup.get_api()
    document_path = setup.get_document_path()
    result = documents.create_version(
        api, document_path, setup.get_value("name"), setup.get_value("description")
    )
    return {"id": result["id"]}


@app.route("/next-version-name", methods=["GET"])
def next_version_name_route():
    """Returns the next default version name for a given document."""
    api = setup.get_api()
    document_path = setup.get_document_path()
    versions = documents.get_versions(api, document_path)
    # len(versions) is correct due to Start version
    return {"name": "V{}".format(len(versions))}


if __name__ == "__main__":
    app.run(
        debug=True,
        port=int(os.environ.get("PORT", 8080)),
    )
