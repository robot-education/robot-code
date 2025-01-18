from enum import StrEnum, auto
from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import DocumentPath


class Permission(StrEnum):
    READ = auto()
    WRITE = auto()
    COMMENT = auto()
    RESHARE = auto()
    EXPORT = auto()
    DELETE = auto()
    LINK = auto()
    COPY = auto()
    OWNER = auto()


def has_permissions(
    api: Api, document_path: DocumentPath, *needed_permissions: Permission
) -> bool:
    permissions = get_permissions(api, document_path)
    for permission in needed_permissions:
        if permission not in permissions:
            return False
    return True


def get_permissions(api: Api, document_path: DocumentPath) -> list[Permission]:
    permissions = api.get(api_path("documents", document_path, DocumentPath))
    return [Permission(permission) for permission in permissions]
