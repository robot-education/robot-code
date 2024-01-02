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


# @router.route("/authorized", methods=["GET"])
# def authorized():
#     """
#     Returns:
#         authorized: true if the client is authenticated.
#             If false, the client should call /sign-in.
#     """
#     api = setup.get_api()
#     authorized = api.oauth.authorized and users.ping(api, catch=True)
#     return {"authorized": authorized}


@router.route("/sign-in", methods=["GET"])
def sign_in():
    """The oauth sign in route.

    Parameters:
        redirectUrl:
            The url to redirect to after the grant.
            Note if an "redirectOnshapeUri" parameter was sent by Onshape to the original /sign-in route,
            this parameter should take that value.
        grantDeniedUrl:
            The url to redirect to on grant denial.
    """
    oauth = oauth_session.get_oauth_session(oauth_session.OAuthType.SIGN_IN)
    auth_url, state = oauth.authorization_url(oauth_session.auth_base_url)

    oauth_session.save_session_state(state)

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
    flask.session["oauth_state"] = None

    token = oauth.fetch_token(
        oauth_session.token_url,
        client_secret=oauth_session.client_secret,
        code=request.args.get("code"),
    )
    oauth_session.save_token(token)

    redirect_url = flask.session["redirect_url"]
    return flask.redirect(redirect_url)


# @router.route("/temp", methods=["GET"])
# def temp():
#     onshape = oauth_session.get_oauth_session()
#     return onshape.get(
#         "https://cad.onshape.com/api/v6/documents?q=Untitled&ownerType=1&sortColumn=createdAt&sortOrder=desc&offset=0&limit=5"
#     ).json()
