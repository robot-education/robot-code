from abc import ABC
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


class ClientException(Exception):
    """An unexpected exception thrown by the backend which stems from an issue with the client code."""

    def __init__(
        self,
        message: str,
    ):
        super().__init__()
        self.message = message
        self.status_code = HTTPStatus.EXPECTATION_FAILED

    def to_dict(self):
        return {"type": "CLIENT_EXCEPTION", "message": self.message}


class ReportedException(ABC, Exception):
    """An exception which is exposed to the user."""

    def __init__(self, type: str, status_code: HTTPStatus = HTTPStatus.BAD_REQUEST):
        super().__init__(type)
        self.type = type
        self.status_code = status_code

    def to_dict(self):
        return {"type": self.type}


class MissingPermissionException(ReportedException):
    """An exception indicating a user does not have the necessary permissions for one or more resources used by an endpoint."""

    def __init__(
        self,
        missing_permission: Permission,
        document_id: str,
        document_name: str | None = None,
    ):
        super().__init__("MISSING_PERMISSION", HTTPStatus.UNAUTHORIZED)
        self.missing_permission = missing_permission
        self.document_id = document_id
        self.document_name = document_name

    def to_dict(self):
        return {
            "type": self.type,
            "permission": self.missing_permission,
            "documentId": self.document_id,
            "documentName": self.document_name,
        }


def require_permissions(api: Api, path: DocumentPath, *needed_permissions: Permission):
    """Throws an exception if the current user doesn't have given permissions for the given document."""
    permissions = get_permissions(api, path)
    if permissions == []:
        raise MissingPermissionException(Permission.READ, path.document_id)

    for permission in needed_permissions:
        if permission not in permissions:
            try:
                document_name = get_document(api, path)["name"]
            except:
                document_name = None
            raise MissingPermissionException(
                permission, path.document_id, document_name
            )


class LinkedCycleException(ReportedException):
    """An exception indicating the current document's linked document graph contains a cycle."""

    def __init__(self):
        super().__init__("LINKED_CYCLE")
