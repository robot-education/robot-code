"""
This module provides utilities for setting up an OAuth workflow on the backend server using frontend endpoints.

In particular:
The frontend should be hosted using https.
The frontend should have a /sign-in route which redirects to the /sign-in route below.
The frontend should have a /redirect route which calls the /redirect route below.
"""
import enum
import http
import os
import flask
from flask import request
from requests_oauthlib import OAuth2Session
from backend.common import setup


router = flask.Blueprint("oauth", __name__)

client_id = os.getenv("OAUTH_CLIENT_ID")
client_secret = os.getenv("OAUTH_CLIENT_SECRET")

# May need to be http://localhost:8080?
# redirect_url = os.getenv("CALLBACK_URL")

base_url = "https://oauth.onshape.com/oauth"
auth_base_url = base_url + "/authorize"
token_url = base_url + "/token"


class OAuthType(enum.Enum):
    SIGN_IN = enum.auto()
    REDIRECT = enum.auto()
    USE = enum.auto()


def get_oauth_session(oauth_type: OAuthType = OAuthType.USE) -> OAuth2Session:
    if oauth_type == OAuthType.SIGN_IN:
        return OAuth2Session(client_id)
    elif oauth_type == OAuthType.REDIRECT:
        return OAuth2Session(client_id, state=setup.get_session_state())

    refresh_kwargs = {"client_id": client_id, "client_secret": client_secret}
    return OAuth2Session(
        client_id,
        token=setup.get_token(),
        auto_refresh_url=token_url,
        auto_refresh_kwargs=refresh_kwargs,
        token_updater=setup.save_token,
    )


@router.route("/authorized", methods=["GET"])
def authorized():
    """
    Returns:
        authorized: true if the client is authenticated.
            If false, the client should call /sign-in.
    """
    onshape = get_oauth_session()
    return {"authorized": onshape.authorized}


@router.route("/sign-in", methods=["GET"])
def sign_in():
    """The oauth sign in route.

    Parameters:
        onshapeRedirectUri: A url to redirect to afterwards.
        redirectUrl:
            The url to redirect to after the grant.
            Note if an onshapeRedirectUri was sent by Onshape to the original /sign-in route,
            that parameter should take the value.
        grantDeniedUrl:
            The url to redirect to on grant denial.
    """
    onshape = get_oauth_session(OAuthType.SIGN_IN)
    auth_url, state = onshape.authorization_url(auth_base_url)
    setup.save_session_state(state)

    redirect_url = flask.request.args.get("onshapeRedirectUri")
    if redirect_url:
        setup.save_redirect_url(redirect_url)

    # Send user to Onshape's sign in page
    return flask.redirect(auth_url)


@router.route("/redirect", methods=["GET"])
def redirect():
    """The Onshape redirect route.

    Parameters:
        url: The complete url of the redirect received from Onshape.
            Used for authentication purposes.
    """
    try:
        url = request.args.get("url")

        onshape = get_oauth_session(OAuthType.REDIRECT)
        token = onshape.fetch_token(
            token_url,
            client_secret=client_secret,
            authorization_response=url,
        )
        setup.save_token(token)

        onshape = get_oauth_session()
        # data = onshape.get(
        #     "https://cad.onshape.com/api/v6/documents?q=Untitled&ownerType=1&sortColumn=createdAt&sortOrder=desc&offset=0&limit=5"
        # ).json()
        # flask.current_app.logger.info(data)

        redirect_url = setup.get_redirect_url()
        response = {"message": "Sign in successful"}
        if redirect_url:
            response["redirectUrl"] = redirect_url
        return response
    except:
        return flask.make_response(
            {"message": "Grant denied"}, http.HTTPStatus.UNAUTHORIZED
        )


# def refresh_token():
#     onshape = get_oauth_session()
#     flask.session["oauth_token"] = onshape.refresh_token(
#         token_url, client_id=client_id, client_secret=client_secret
#     )
#     return {"message": "Tokens refreshed"}


@router.route("/temp", methods=["GET"])
def temp():
    onshape = get_oauth_session()
    return onshape.get(
        "https://cad.onshape.com/api/v6/documents?q=Untitled&ownerType=1&sortColumn=createdAt&sortOrder=desc&offset=0&limit=5"
    ).json()
