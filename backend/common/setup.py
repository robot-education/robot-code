from typing import Iterable
import firebase_admin
from firebase_admin import firestore
from flask import request
from api import api_base, api_path, exceptions
from api import oauth_api
from api.oauth_api import make_oauth_api
from backend.common import oauth_session


def get_document_id() -> str:
    try:
        return request.args["documentId"]
    except:
        raise exceptions.ApiException("Failed to parse document path.")


def get_db() -> firestore.firestore.Client:
    firebase_admin.initialize_app()
    return firestore.client()


def get_api() -> oauth_api.OAuthApi:
    return make_oauth_api(oauth_session.get_oauth_session())


def get_arg(key: str) -> str:
    """Returns a value from the request query.

    Throws if it doesn't exist.
    """
    value = request.args.get(key, None)
    if not value:
        raise exceptions.ApiException(
            "Missing required query parameter {}.".format(key)
        )
    return value


def get_value(key: str) -> str:
    """Returns a value from the request body.

    Throws if it doesn't exist.
    """
    value = request.get_json().get(key, None)
    if not value:
        raise exceptions.ApiException("Missing required body parameter {}.".format(key))
    return value


def get_optional_value(key: str) -> str | None:
    """Returns a value from the request body."""
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
