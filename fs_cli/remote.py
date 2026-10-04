"""A thin layer over onshape_api exposing exactly what the CLI needs from Onshape."""

from __future__ import annotations

import dataclasses
from typing import Protocol

from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents, feature_studios
from onshape_api.endpoints.documents import ElementType
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.paths.paths import ElementPath, InstancePath


@dataclasses.dataclass
class RemoteStudio:
    element_id: str
    name: str
    microversion_id: str


class Remote(Protocol):
    def list_studios(self, instance: InstancePath) -> list[RemoteStudio]: ...

    def pull(self, instance: InstancePath, element_id: str) -> str: ...

    def push(self, instance: InstancePath, element_id: str, code: str) -> list[str]:
        """Pushes code to a Feature Studio. Returns any error/warning notices Onshape reported."""
        ...

    def create(self, instance: InstancePath, name: str) -> RemoteStudio: ...

    def latest_std_version(self) -> str: ...


class OnshapeRemote:
    def __init__(self, api: Api) -> None:
        self.api = api

    def list_studios(self, instance: InstancePath) -> list[RemoteStudio]:
        elements = documents.get_document_elements(
            self.api, instance, ElementType.FEATURE_STUDIO
        )
        return [
            RemoteStudio(element["id"], element["name"], element["microversionId"])
            for element in elements
        ]

    def pull(self, instance: InstancePath, element_id: str) -> str:
        return feature_studios.pull_code(
            self.api, ElementPath.from_path(instance, element_id)
        )

    def push(self, instance: InstancePath, element_id: str, code: str) -> list[str]:
        response = feature_studios.push_code(
            self.api, ElementPath.from_path(instance, element_id), code
        )
        return extract_notices(response)

    def create(self, instance: InstancePath, name: str) -> RemoteStudio:
        response = feature_studios.create_feature_studio(self.api, instance, name)
        return RemoteStudio(
            response["id"], response["name"], response["microversionId"]
        )

    def latest_std_version(self) -> str:
        return get_latest_std_version(self.api)


def extract_notices(response: object) -> list[str]:
    """Pulls human readable error and warning messages out of a Feature Studio update response."""
    if not isinstance(response, dict):
        return []
    notices = []
    for notice in response.get("notices") or []:
        if not isinstance(notice, dict):
            continue
        level = str(notice.get("level", "")).upper()
        if level not in ("ERROR", "WARNING"):
            continue
        message = notice.get("message")
        if isinstance(message, dict):
            message = message.get("message")
        if message:
            notices.append(f"{level.lower()}: {message}")
    return notices
