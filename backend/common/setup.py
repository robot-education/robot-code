from typing import Iterable
from google.cloud import firestore
from flask import request
from api import api_base, api_path
import http


class ApiException(Exception):
    def __init__(
        self, message: str, status_code: http.HTTPStatus = http.HTTPStatus.BAD_REQUEST
    ):
        super().__init__()
        self.message = message
        self.status_code = status_code

    def to_dict(self):
        return {"message": self.message}


def get_element_path() -> api_path.ElementPath:
    try:
        return api_path.ElementPath.from_path(request.args["element_path"])
    except:
        raise ApiException("Failed to parse element path.")


def get_document_id() -> str:
    try:
        document_path = request.args["document_id"]
        return document_path.removeprefix("/")
    except:
        raise ApiException("Failed to parse document path.")


def get_document_path() -> api_path.DocumentPath:
    try:
        return api_path.DocumentPath.from_path(request.args["document_path"])
    except:
        raise ApiException("Failed to parse document path.")


def get_db() -> firestore.Client:
    return firestore.Client("robot-manager-123")


def _extract_token(auth: str) -> str:
    return auth.removeprefix("Basic").strip()


def get_api(logging=False) -> api_base.Api:
    auth = request.headers.get("Authentication", None)
    if not auth:
        raise ApiException("An auth token is required.", http.HTTPStatus.UNAUTHORIZED)
    return api_base.ApiToken(_extract_token(auth), logging=logging)


def get_value(key: str) -> str:
    value = request.get_json().get(key, None)
    if not value:
        raise ApiException("Missing required key {}.".format(key))
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
        raise ApiException(message)
    return body
