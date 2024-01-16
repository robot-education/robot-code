"""Serves as an abstraction layer for connecting with the Onshape API, the backend database, and the current flask request."""
import enum
from typing import Any
import os
from uuid import uuid4

from google.cloud import firestore
import flask
from requests_oauthlib import OAuth2Session
import onshape_api
from backend.common import backend_exceptions, oauth_utils


def get_session_id() -> str:
    session_id = flask.session.get("session_id")
    if session_id is None:
        session_id = str(uuid4())
        flask.session["session_id"] = session_id
    return session_id


def save_token(token) -> None:
    set_session_data(db.transaction(), {"token": token})


def get_token() -> dict | None:
    return get_session_data().get("token")


def get_session_data() -> dict:
    session_id = get_session_id()
    doc_ref = db_sessions().document(document_id=session_id)
    doc = doc_ref.get()
    if not doc.exists or (session_data := doc.to_dict()) is None:
        session_data = {"token": None}
    return session_data


@firestore.transactional
def set_session_data(transaction, session_data: dict) -> None:
    session_id = get_session_id()
    doc_ref = db_sessions().document(document_id=session_id)
    # doc = doc_ref.get(transaction=transaction)
    transaction.set(doc_ref, session_data)


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


def document_route(wvm_param: str = "w"):
    return f"/d/<document_id>/<{wvm_param}>/<workspace_id>"


def element_route(wvm_param: str = "w"):
    return document_route(wvm_param) + "/e/<element_id>"


def get_document_id() -> str:
    try:
        return flask.request.args["documentId"]
    except:
        raise backend_exceptions.UserException("Expected documentId.")


db = firestore.Client()
sessions = db.collection("sessions")
linked_documents = db.collection("linked-documents")


def get_db() -> firestore.Client:
    return db


def db_linked_documents() -> firestore.CollectionReference:
    return linked_documents


def db_sessions() -> firestore.CollectionReference:
    return sessions


def get_api() -> onshape_api.OAuthApi:
    return onshape_api.make_oauth_api(get_oauth_session())


def get_instance_path(wvm_param: str = "w") -> onshape_api.InstancePath:
    return onshape_api.InstancePath(
        get_route("document_id"),
        get_route("workspace_id"),
        get_route(wvm_param),
    )


def get_element_path(wvm_param: str = "w") -> onshape_api.ElementPath:
    return onshape_api.ElementPath.from_path(
        get_instance_path(wvm_param),
        get_route("element_id"),
    )


def get_route(route_param: str) -> str:
    """Returns the value of a path parameter.

    Throws if it doesn't exist.
    """
    view_args = flask.request.view_args
    if view_args is None or (param := view_args.get(route_param)) is None:
        raise backend_exceptions.UserException(
            "Missing required path parameter {}.".format(route_param)
        )
    return param


def get_query(key: str) -> str:
    """Returns a value from the request query.

    Throws if it doesn't exist.
    """
    value = flask.request.args.get(key)
    if value is None:
        raise backend_exceptions.UserException(
            "Missing required query parameter {}.".format(key)
        )
    return value


def get_body(key: str) -> Any:
    """Returns a value from the request body.

    Throws if it doesn't exist.
    """
    value = flask.request.get_json().get(key, None)
    if not value:
        raise backend_exceptions.UserException(
            "Missing required body parameter {}.".format(key)
        )
    return value


def get_optional_body(key: str) -> Any | None:
    """Returns a value from the request body."""
    return flask.request.get_json().get(key, None)


# def extract_body(
#     required_keys: Iterable[str] = [],
#     optional_keys: Iterable[str] = [],
# ) -> dict:
#     required_key_set = set(required_keys)
#     optional_key_set = set(optional_keys)

#     body = request.get_json()
#     for key in body:
#         if key in required_key_set:
#             required_key_set.remove(key)
#             continue
#         elif key in optional_key_set:
#             continue
#         message = "Required arguments are missing: {}".format(
#             ", ".join(required_key_set)
#         )
#         raise exceptions.ApiException(message)
#     return body
