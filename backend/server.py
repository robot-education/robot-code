import os
import dotenv
import flask
from onshape_api.endpoints import users
from backend import api
from backend.common import setup
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

    @app.get("/app/assembly")
    @app.get("/app/part-studio")
    def serve_app():
        api = setup.get_api()
        authorized = api.oauth.authorized and users.ping(api, catch=True)
        if not authorized:
            flask.session["redirect_url"] = flask.request.url
            return flask.redirect("/sign-in")

        if os.getenv("NODE_ENV", "development") == "production":
            return flask.send_from_directory("dist", "index.html")
        else:
            return flask.render_template("index.html")

    @app.get("/grant-denied")
    def serve_grant_denied():
        return flask.render_template("index.html")

    @app.get("/assets/<path:filename>")
    def serve_assets(filename: str):
        return flask.send_from_directory("dist/assets", filename)

    return app
