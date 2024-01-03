"""
This module provides utilities for setting up an OAuth workflow on the backend server using frontend endpoints.

In particular:
The frontend should be hosted using https.
The frontend should have a /sign-in route which redirects to the /sign-in route below.
The frontend should have a /redirect route which calls the /redirect route below.
"""
import flask
from flask import request
from backend.common import oauth_session


router = flask.Blueprint("oauth", __name__)


@router.route("/sign-in", methods=["GET"])
def sign_in():
    """The oauth sign in route."""
    if request.args.get("redirectOnshapeUri"):
        url = request.args.get("redirectOnshapeUri")
        # Handle Onshape bug
        if url == "[object Object]":
            url = "https://cad.onshape.com/user/applications"
        flask.session["redirect_url"] = url

    oauth = oauth_session.get_oauth_session(oauth_session.OAuthType.SIGN_IN)
    # Saving state is unneeded since Onshape saves it for us
    auth_url, _ = oauth.authorization_url(oauth_session.auth_base_url)

    # Send user to Onshape's sign in page
    return flask.redirect(auth_url)


@router.route("/redirect", methods=["GET"])
def redirect():
    """The Onshape redirect route.

    Parameters:
        The code and state parameters received from Onshape.
    """
    if request.args.get("error") == "access_denied":
        return flask.redirect("/grant-denied")

    oauth = oauth_session.get_oauth_session(oauth_session.OAuthType.REDIRECT)

    token = oauth.fetch_token(
        oauth_session.token_url,
        client_secret=oauth_session.client_secret,
        code=request.args["code"],
    )
    oauth_session.save_token(token)

    redirect_url = flask.session["redirect_url"]
    return flask.redirect(redirect_url)
