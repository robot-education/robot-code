"""A thin layer over onshape_api exposing exactly what the CLI needs from Onshape."""

from __future__ import annotations

import dataclasses
import pathlib
from typing import Protocol

from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents, feature_studios, versions
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.exceptions import ApiError
from onshape_api.paths.paths import ElementPath, InstancePath
from onshape_api.types import ElementGroup, ElementType


@dataclasses.dataclass
class RemoteStudio:
    """A Feature Studio in Onshape.

    Attributes:
        microversion_id: Changes whenever the studio changes in Onshape.
        folders: The folders containing the studio, outermost first. Empty if unknown.
    """

    element_id: str
    name: str
    microversion_id: str
    folders: tuple[str, ...] = ()


def safe_file_name(name: str) -> str:
    name = name.replace("/", "_").replace("\\", "_").strip()
    return "_" if name in ("", ".", "..") else name


def relative_path_for(studio_name: str, folders: tuple[str, ...] = ()) -> str:
    """The path a studio is first pulled to, relative to the code folder (using /)."""
    parts = [safe_file_name(folder) for folder in folders]
    parts.append(file_name_for(studio_name))
    return pathlib.PurePosixPath(*parts).as_posix()


def file_name_for(studio_name: str) -> str:
    """Returns the local file name used for a Feature Studio."""
    name = safe_file_name(studio_name)
    return name if name.endswith(".fs") else name + ".fs"


@dataclasses.dataclass
class Version:
    id: str
    name: str
    description: str = ""


class Remote(Protocol):
    def list_studios(self, instance: InstancePath) -> list[RemoteStudio]:
        """Lists the Feature Studios in a document, with their folders when available."""
        ...

    def pull(self, instance: InstancePath, element_id: str) -> str: ...

    def push(self, instance: InstancePath, element_id: str, code: str) -> None: ...

    def create(self, instance: InstancePath, name: str) -> RemoteStudio:
        """Creates a Feature Studio at the top level of the document."""
        ...

    def feature_names(self, instance: InstancePath, element_id: str) -> list[str]:
        """Returns the names of the custom features defined in a Feature Studio."""
        ...

    def versions(self, instance: InstancePath) -> list[Version]:
        """Returns every version of a document, oldest first."""
        ...

    def create_version(
        self, instance: InstancePath, name: str, description: str
    ) -> Version: ...

    def latest_std_version(self) -> str: ...


class OnshapeRemote:
    def __init__(self, api: Api) -> None:
        self.api = api
        self.contents_failed = False

    def list_studios(self, instance: InstancePath) -> list[RemoteStudio]:
        # One call gives every tab's microversion and the folder structure. If it fails (it's
        # been seen to return 400), fall back to the elements endpoint without folders; failed
        # calls don't count against Onshape's API limits.
        if not self.contents_failed:
            try:
                contents = documents.get_document_contents(self.api, instance)
            except ApiError as error:
                self.contents_failed = True
                print(
                    f"Warning: couldn't read the document's folders, so newly pulled files go in the top level folder ({error})."
                )
            else:
                folders = folder_paths(contents.get("folders"))
                return [
                    RemoteStudio(
                        element["id"],
                        element["name"],
                        element["microversionId"],
                        folders.get(element["id"], ()),
                    )
                    for element in contents["elements"]
                    if element["elementType"] == ElementType.FEATURE_STUDIO
                ]
        elements = documents.get_document_elements(
            self.api, instance, ElementType.FEATURE_STUDIO
        )
        return [
            RemoteStudio(element["id"], element["name"], element["microversionId"])
            for element in elements
        ]

    def pull(self, instance: InstancePath, element_id: str) -> str:
        path = ElementPath.from_path(instance, element_id)
        return feature_studios.get_contents(self.api, path)["contents"]

    def push(self, instance: InstancePath, element_id: str, code: str) -> None:
        path = ElementPath.from_path(instance, element_id)
        feature_studios.update_contents(self.api, path, code)

    def create(self, instance: InstancePath, name: str) -> RemoteStudio:
        response = feature_studios.create_feature_studio(self.api, instance, name)
        return RemoteStudio(
            response["id"], response["name"], response["microversionId"]
        )

    def feature_names(self, instance: InstancePath, element_id: str) -> list[str]:
        path = ElementPath.from_path(instance, element_id)
        specs = feature_studios.get_feature_specs(self.api, path)
        return [spec["featureTypeName"] for spec in specs["featureSpecs"]]

    def versions(self, instance: InstancePath) -> list[Version]:
        return [
            Version(version["id"], version["name"], version.get("description") or "")
            for version in versions.get_versions(self.api, instance)
        ]

    def create_version(
        self, instance: InstancePath, name: str, description: str
    ) -> Version:
        response = versions.create_version(self.api, instance, name, description)
        return Version(response["id"], response["name"], description)

    def latest_std_version(self) -> str:
        return get_latest_std_version(self.api)


def folder_paths(root: ElementGroup | None) -> dict[str, tuple[str, ...]]:
    """Maps element ids to the folders containing them, given the contents endpoint's folder tree."""
    paths: dict[str, tuple[str, ...]] = {}

    def walk(group: dict, path: tuple[str, ...]) -> None:
        for node in group.get("groups") or []:
            if not isinstance(node, dict):
                continue
            if node.get("elementId"):
                paths[node["elementId"]] = path
            elif "groups" in node or "groupName" in node:
                walk(node, path + (node.get("groupName") or "",))

    if isinstance(root, dict):
        walk(root, ())
    return paths
