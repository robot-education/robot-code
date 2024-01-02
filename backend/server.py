import os
import dotenv
from flask import (
    Flask,
    current_app,
    redirect,
    render_template,
    request,
    send_from_directory,
    session,
)
from api import exceptions
from api.endpoints import users
from backend.common import setup


# import logging
# import sys

# log = logging.getLogger("oauthlib")
# log.addHandler(logging.StreamHandler(sys.stdout))
# log.setLevel(logging.DEBUG)

dotenv.load_dotenv()


def create_app():
    app = Flask(__name__, static_folder="dist")
    app.config.update(
        SESSION_COOKIE_NAME="robot-manager", SECRET_KEY=os.getenv("SESSION_SECRET")
    )
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

    from backend.routes import (
        add_parent,
        assembly_mirror,
        generate_assembly,
        oauth,
        update_references,
    )

    app.register_blueprint(oauth.router)
    app.register_blueprint(generate_assembly.router)
    app.register_blueprint(assembly_mirror.router)

    @app.errorhandler(exceptions.ApiException)
    def api_exception(e: exceptions.ApiException):
        return e.to_dict(), e.status_code

    @app.route("/app", methods=["GET"])
    def app_route():
        current_app.logger.info("Server app")
        api = setup.get_api()
        current_app.logger.info(api.oauth)
        authorized = api.oauth.authorized and users.ping(api, catch=True)
        if not authorized:
            session["redirect_url"] = request.url
            return redirect("/sign-in")
        return render_template("index.html")
        # return flask.send_from_directory("dist", "index.html")

    @app.route("/assets/<path:filename>")
    def serve_assets_route(filename: str):
        return send_from_directory("dist/assets", filename)

    @app.route("/api/<path:route>", methods=["POST", "GET"])
    def api_route(route: str):
        # 307 redirect keeps same method but drops query parameters
        return redirect("/" + route + "?" + request.query_string.decode(), code=307)

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


# if __name__ == "__main__":
#     app.run(
#         debug=True,
#         ssl_context=("./credentials/cert.pem", "./credentials/key.pem"),
#         port=int(os.getenv("PORT", 3000)),
#     )
