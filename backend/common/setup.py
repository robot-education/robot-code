from http import HTTPStatus
from google.cloud import firestore
from flask import request
from api import api_path, exceptions
from api import oauth_api
from backend.common import oauth_session


def document_route(wvm_param: str = "w"):
    return f"/d/<document_id>/<{wvm_param}>/<workspace_id>"


def element_route(wvm_param: str = "w"):
    return document_route(wvm_param) + "/e/<element_id>"


def get_document_id() -> str:
    try:
        return request.args["documentId"]
    except:
        raise exceptions.ApiException("Failed to parse document path.")


def get_db() -> firestore.Client:
    return firestore.Client()


def get_api() -> oauth_api.OAuthApi:
    return oauth_api.make_oauth_api(oauth_session.get_oauth_session())


def get_document_path(wvm_param: str = "w") -> api_path.DocumentPath:
    return api_path.make_document_path(
        get_route("document_id"),
        get_route("workspace_id"),
        get_route(wvm_param),
    )


def get_element_path(wvm_param: str = "w") -> api_path.ElementPath:
    return api_path.ElementPath(
        get_document_path(wvm_param),
        get_route("element_id"),
    )


def get_route(route_param: str) -> str:
    """Returns the value of a path parameter.

    Throws if it doesn't exist.
    """
    view_args = request.view_args
    if view_args is None or (param := view_args.get(route_param)) is None:
        raise exceptions.ApiException(
            "Missing required path parameter {}.".format(route_param),
            HTTPStatus.NOT_FOUND,
        )
    return param


def get_query(key: str) -> str:
    """Returns a value from the request query.

    Throws if it doesn't exist.
    """
    value = request.args.get(key)
    if value is None:
        raise exceptions.ApiException(
            "Missing required query parameter {}.".format(key)
        )
    return value


def get_body(key: str) -> str:
    """Returns a value from the request body.

    Throws if it doesn't exist.
    """
    value = request.get_json().get(key, None)
    if not value:
        raise exceptions.ApiException("Missing required body parameter {}.".format(key))
    return value


def get_optional_body(key: str) -> str | None:
    """Returns a value from the request body."""
    return request.get_json().get(key, None)


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
