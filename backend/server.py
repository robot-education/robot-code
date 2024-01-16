import os
import dotenv
import flask
from onshape_api.endpoints import users
from backend import api
from backend.common import connect
from backend.endpoints import oauth

dotenv.load_dotenv()


def create_app():
    app = flask.Flask(__name__, static_folder="dist")
    app.config.update(
        SESSION_COOKIE_NAME="robot-manager", SECRET_KEY=os.getenv("SESSION_SECRET")
    )
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

    app.register_blueprint(api.router)
    app.register_blueprint(oauth.router)

    production = os.getenv("NODE_ENV", "development") == "production"

    @app.get("/app")
    def serve_app():
        api = connect.get_api()
        authorized = api.oauth.authorized and users.ping(api, catch=True)
        if not authorized:
            flask.session["redirect_url"] = flask.request.url
            return flask.redirect("/sign-in")

        # if os.getenv("NODE_ENV", "development") == "production":

        return flask.render_template("index.html", production=production)
        # return flask.send_from_directory("dist", "index.html")

    @app.get("/grant-denied")
    def serve_grant_denied():
        return flask.render_template("index.html")

    @app.get("/robot-icon.svg")
    def serve_icon():
        return flask.url_for("static", filename="robot-icon.svg")

    @app.get("/assets/<path:filename>")
    def serve_assets(filename: str):
        return flask.send_from_directory("dist/assets", filename)

    return app
