import os
import flask
from onshape_api.endpoints import users
from backend import api
from backend.common import connect, env
from backend import oauth


def create_app():
    app = flask.Flask(__name__)
    app.config.update(
        SESSION_COOKIE_NAME="robot-manager",
        SECRET_KEY=env.session_secret,
        SESSION_COOKIE_SECURE=True,
        SESSION_COOKIE_HTTPONLY=True,
        SESSION_COOKIE_SAMESITE="None",
    )
    os.environ["OAUTHLIB_INSECURE_TRANSPORT"] = "1"

    app.register_blueprint(api.router)
    app.register_blueprint(oauth.router)

    def serve_index():
        if env.is_production:
            return flask.send_from_directory("dist", "index.html")
        else:
            return flask.render_template("index.html")

    @app.get("/app")
    def serve_app():
        api = connect.get_api()
        authorized = api.oauth.authorized and users.ping(api, catch=True)
        if not authorized:
            flask.session["redirect_url"] = flask.request.url
            return flask.redirect("/sign-in")

        connect.save_user()
        return serve_index()

    @app.get("/license")
    @app.get("/grant-denied")
    def serve_static_pages():
        return serve_index()

    # Production only handlers:
    if env.is_production:

        @app.get("/robot-icon.svg")
        def serve_icon():
            return flask.send_from_directory("dist", "robot-icon.svg")

        @app.get("/assets/<path:filename>")
        def serve_assets(filename: str):
            return flask.send_from_directory("dist/assets", filename)

    return app
