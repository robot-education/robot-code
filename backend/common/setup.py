from typing import Iterable
import firebase_admin
from firebase_admin import firestore
from flask import request
import flask
from api import api_base, api_path, exceptions


def get_document_id() -> str:
    try:
        return request.args["documentId"]
    except:
        raise exceptions.ApiException("Failed to parse document path.")


def get_document_path() -> api_path.DocumentPath:
    try:
        return api_path.DocumentPath.from_obj(request.args)
    except:
        raise exceptions.ApiException("Failed to parse document path.")


def get_element_path() -> api_path.ElementPath:
    try:
        return api_path.ElementPath.from_obj(request.args)
    except:
        raise exceptions.ApiException("Failed to parse element path.")


def get_db() -> firestore.firestore.Client:
    firebase_admin.initialize_app()
    return firestore.client()


# def _extract_token(auth: str) -> str:
#     return auth.removeprefix("Basic").strip()


def save_redirect_url(redirect_url: str) -> None:
    flask.session["redirect_url"] = redirect_url


def get_redirect_url() -> str | None:
    return flask.session.get("redirect_url")


def save_session_state(state) -> None:
    flask.session["oauth_state"] = state


def get_session_state():
    return flask.session["oauth_state"]


def save_token(token) -> None:
    flask.session["oauth_token"] = token


def get_token():
    return flask.session["oauth_token"]


def get_api() -> api_base.Api | None:
    return None


#     auth = request.headers.get("Authentication", None)
#     if not auth:
#         raise exceptions.ApiException(
#             "An auth token is required.", http.HTTPStatus.UNAUTHORIZED
#         )
# return api_base.O(_extract_token(auth), logging=logging)


def get_value(key: str) -> str:
    value = request.get_json().get(key, None)
    if not value:
        raise exceptions.ApiException("Missing required key {}.".format(key))
    return value


def get_optional_value(key: str) -> str | None:
    return request.get_json().get(key, None)


def get_body(
    required_keys: Iterable[str] = [],
    optional_keys: Iterable[str] = [],
) -> dict:
    required_key_set = set(required_keys)
    optional_key_set = set(optional_keys)

    body = request.get_json()
    for key in body:
        if key in required_key_set:
            required_key_set.remove(key)
            continue
        elif key in optional_key_set:
            continue
        message = "Required arguments are missing: {}".format(
            ", ".join(required_key_set)
        )
        raise exceptions.ApiException(message)
    return body
