import os
import dotenv
import flask
from flask import current_app
from api.endpoints import users
from backend import api
from backend.common import setup
from backend.routes import oauth

dotenv.load_dotenv()


def create_app():
    app = flask.Flask(__name__, static_folder="dist")
    app.config.update(
        SESSION_COOKIE_NAME="robot-manager", SECRET_KEY=os.getenv("SESSION_SECRET")
    )
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

    app.register_blueprint(api.router)

    app.register_blueprint(oauth.router)

    @app.route("/app/assembly", methods=["GET"])
    @app.route("/app/part-studio", methods=["GET"])
    def app_route():
        api = setup.get_api()
        authorized = api.oauth.authorized and users.ping(api, catch=True)
        if not authorized:
            flask.session["redirect_url"] = flask.request.url
            return flask.redirect("/sign-in")

        if os.getenv("NODE_ENV", "development") == "production":
            return flask.send_from_directory("dist", "index.html")
        else:
            return flask.render_template("index.html")

    @app.route("/grant-denied", methods=["GET"])
    def grant_denied_route():
        return flask.render_template("index.html")

    @app.route("/assets/<path:filename>", methods=["GET"])
    def serve_assets_route(filename: str):
        return flask.send_from_directory("dist/assets", filename)

    return app
