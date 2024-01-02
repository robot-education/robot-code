import enum
import os
import flask
from requests_oauthlib import OAuth2Session


def save_session_state(state) -> None:
    flask.session["oauth_state"] = state


def get_session_state():
    return flask.session["oauth_state"]


def save_token(token) -> None:
    flask.session["oauth_token"] = token


def has_token() -> bool:
    return "oauth_token" in flask.session


def get_token() -> dict | None:
    return flask.session.get("oauth_token")


client_id = os.getenv("OAUTH_CLIENT_ID")
client_secret = os.getenv("OAUTH_CLIENT_SECRET")

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
        return OAuth2Session(client_id, state=flask.request.args["state"])

    refresh_kwargs = {
        "client_id": client_id,
        "client_secret": client_secret,
    }
    return OAuth2Session(
        client_id,
        token=get_token(),
        auto_refresh_url=token_url,
        auto_refresh_kwargs=refresh_kwargs,
        token_updater=save_token,
    )
