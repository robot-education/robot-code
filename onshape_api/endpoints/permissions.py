from enum import StrEnum
from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import DocumentPath


class Permission(StrEnum):
    READ = "READ"
    WRITE = "WRITE"
    COMMENT = "COMMENT"
    RESHARE = "RESHARE"
    EXPORT = "EXPORT"
    DELETE = "DELETE"
    LINK = "LINK"
    COPY = "COPY"
    OWNER = "OWNER"


def has_permissions(
    api: Api, document_path: DocumentPath, *needed_permissions: Permission
) -> bool:
    permissions = get_permissions(api, document_path)
    for permission in needed_permissions:
        if permission not in permissions:
            return False
    return True


def get_permissions(api: Api, document_path: DocumentPath) -> list[Permission]:
    permissions = api.get(
        api_path(
            "documents",
            document_path,
            DocumentPath,
            "permissionset",
            skip_document_d=True,
        )
    )
    return [Permission(permission) for permission in permissions]
