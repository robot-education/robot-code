from http import HTTPStatus
from onshape_api.api.api_base import Api
from onshape_api.endpoints.documents import get_document
from onshape_api.endpoints.permissions import Permission, get_permissions
from onshape_api.paths.paths import DocumentPath


class BackendException(Exception):
    """An unexpected exception thrown by the backend."""

    def __init__(
        self,
        message: str,
        status_code: HTTPStatus = HTTPStatus.INTERNAL_SERVER_ERROR,
    ):
        super().__init__()
        self.message = message
        self.status_code = status_code

    def to_dict(self):
        return {"type": "BACKEND_EXCEPTION", "message": self.message}


class UserException(Exception):
    """An exception thrown by the backend which is presumably the user's fault.
    The message associated with a UserException is generally displayed directly to the user.
    """

    def __init__(
        self,
        message: str,
    ):
        super().__init__()
        self.message = message
        self.status_code = HTTPStatus.BAD_REQUEST

    def to_dict(self):
        return {"type": "USER_EXCEPTION", "message": self.message}


class MissingPermission(Exception):
    """An exception indicating a user does not have the necessary permissions for one or more resources used by an endpoint."""

    def __init__(
        self,
        missing_permission: Permission,
        document_name: str | None = None,
    ):
        super().__init__("Missing necessary permission: " + missing_permission)
        self.missing_permission = missing_permission
        self.document_name = document_name
        self.status_code = HTTPStatus.UNAUTHORIZED

    def to_dict(self):
        result = {
            "type": "MISSING_PERMISSION",
            "permission": self.missing_permission,
        }
        if self.document_name != None:
            result["documentName"] = self.document_name
        return result


def require_permissions(api: Api, path: DocumentPath, *needed_permissions: Permission):
    """Throws an exception if the current user doesn't have given permissions for the given document."""
    permissions = get_permissions(api, path)
    for permission in needed_permissions:
        if permission not in permissions:
            if Permission.READ in permissions:
                try:
                    document_name = get_document(api, path)["name"]
                except:
                    continue
            raise MissingPermission(permission, document_name)
