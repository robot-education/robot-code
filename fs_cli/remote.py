"""A thin layer over onshape_api exposing exactly what the CLI needs from Onshape."""

from __future__ import annotations

import dataclasses
import pathlib
from typing import Protocol

from fs_cli.configured_studios import Built, ConfiguredStudio, Diagnosis, build, diagnose
from onshape_api.api.api_base import Api
from onshape_api.endpoints import blob_elements, documents, elements, feature_studios, metadata, versions, workspaces
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.paths.paths import DocumentPath, ElementPath, InstancePath
from onshape_api.types import ElementGroup, ElementType, MergeStrategy


@dataclasses.dataclass
class RemoteStudio:
    """A Feature Studio in Onshape.

    Attributes:
        microversion_id: Changes whenever the studio changes in Onshape.
    """

    element_id: str
    name: str
    microversion_id: str


# The images which can be synced, by extension: an image tab's file can be imported for a feature's or parameter's
# "Icon", or a "Description Image"
IMAGE_TYPES = {".svg": "image/svg+xml", ".png": "image/png"}


@dataclasses.dataclass
class RemoteImage:
    """An image in Onshape: a blob element (tab) holding an image file."""

    element_id: str
    name: str
    microversion_id: str
    # Its file's MIME type
    data_type: str = "image/svg+xml"

    @property
    def extension(self) -> str:
        return next((extension for extension, type in IMAGE_TYPES.items() if type == self.data_type), ".svg")


# Files named like `screenshot.local.png` or `example.local.fs` stay in the repo, and aren't synced with tabs
LOCAL_SUFFIX = ".local"


def is_local(name: str) -> bool:
    """Whether a file name is a local one (like `example.local.fs`), which isn't synced."""
    return pathlib.PurePosixPath(name).stem.lower().endswith(LOCAL_SUFFIX)


def image_type(name: str) -> str | None:
    """The MIME type of an image file name, if it's an image which can be synced (not a `.local.png` or `.local.svg`)."""
    if is_local(name):
        return None
    return IMAGE_TYPES.get(pathlib.PurePosixPath(name).suffix.lower())


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


@dataclasses.dataclass
class RemoteWorkspace:
    """A workspace (a branch) of a document."""

    id: str
    name: str
    description: str = ""


class Remote(Protocol):
    def list_studios(self, instance: InstancePath) -> list[RemoteStudio]:
        """Lists the Feature Studios in a document, with their microversions (but not folders)."""
        ...

    def studio_folders(self, instance: InstancePath) -> dict[str, tuple[str, ...]]:
        """Maps element ids to the folders containing them, outermost first."""
        ...

    def pull(self, instance: InstancePath, element_id: str) -> str: ...

    def push(self, instance: InstancePath, element_id: str, code: str) -> None: ...

    def create(self, instance: InstancePath, name: str) -> RemoteStudio:
        """Creates a Feature Studio at the top level of the document."""
        ...

    def delete(self, instance: InstancePath, element_id: str) -> None:
        """Deletes a Feature Studio's tab."""
        ...

    def rename(self, instance: InstancePath, element_id: str, name: str) -> None:
        """Renames a tab (2 calls)."""
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

    def default_workspace(self, document_id: str) -> InstancePath: ...

    def list_images(self, instance: InstancePath) -> list[RemoteImage]:
        """Lists the images (blob elements holding SVGs or PNGs) in a document, with their microversions."""
        ...

    def download_image(self, instance: InstancePath, element_id: str) -> bytes: ...

    def upload_image(self, instance: InstancePath, name: str, data: bytes) -> RemoteImage:
        """Creates an image tab named name (a file name, like robotIcon.svg) at the top level of the document."""
        ...

    def update_image(self, instance: InstancePath, element_id: str, name: str, data: bytes) -> RemoteImage:
        """Replaces an image tab's file, returning its new microversion."""
        ...

    def element_ids(self, instance: InstancePath) -> set[str]:
        """Every tab's element id (anonymous for public documents)."""
        ...

    def workspaces(self, document_id: str) -> list[RemoteWorkspace]: ...

    def create_workspace(self, document_id: str, version_id: str, name: str, description: str) -> RemoteWorkspace:
        """Branches a workspace from a version (1 call)."""
        ...

    def delete_workspace(self, instance: InstancePath) -> None: ...

    def merge(self, target: InstancePath, source_workspace_id: str, strategy: MergeStrategy) -> None:
        """Merges a workspace of the same document into target (1 call)."""
        ...

    def build_studio(
        self, instance: InstancePath, studio: ConfiguredStudio, feature_studio_id: str, microversion_id: str
    ) -> Built:
        """Makes a configured Part Studio in a workspace, from a Feature Studio there (see `configured_studios.build`;
        4 or 5 calls)."""
        ...

    def diagnose_studio(self, instance: InstancePath, studio: ConfiguredStudio, built_tab: str) -> Diagnosis:
        """Which part of a studio's configuration Onshape refuses (see `configured_studios.diagnose`; a call per stage,
        and 2 or 3 more)."""
        ...


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

    def studio_folders(self, instance: InstancePath) -> dict[str, tuple[str, ...]]:
        # As of Onshape 1.221 (Oct 2026) the contents endpoint returns 400 "Invalid JSON input."
        # for every document, regardless of auth, API version or query; callers fall back to
        # the top level folder until Onshape fixes it
        contents = documents.get_document_contents(self.api, instance)
        return folder_paths(contents.get("folders"))

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

    def delete(self, instance: InstancePath, element_id: str) -> None:
        elements.delete_element(self.api, ElementPath.from_path(instance, element_id))

    def rename(self, instance: InstancePath, element_id: str, name: str) -> None:
        metadata.rename_element(self.api, ElementPath.from_path(instance, element_id), name)

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

    def default_workspace(self, document_id: str) -> InstancePath:
        return documents.get_default_workspace(self.api, DocumentPath(document_id))

    def list_images(self, instance: InstancePath) -> list[RemoteImage]:
        elements = documents.get_document_elements(self.api, instance, ElementType.BLOB)
        return [
            RemoteImage(element["id"], element["name"], element["microversionId"], element.get("dataType", ""))
            for element in elements
            if element.get("dataType") in IMAGE_TYPES.values()
        ]

    def download_image(self, instance: InstancePath, element_id: str) -> bytes:
        return blob_elements.download_file(self.api, ElementPath.from_path(instance, element_id))

    def upload_image(self, instance: InstancePath, name: str, data: bytes) -> RemoteImage:
        content_type = _image_type(name)
        response = blob_elements.upload_file_create_element(self.api, instance, name, data, content_type)
        return RemoteImage(response["id"], response["name"], response["microversionId"], content_type)

    def update_image(self, instance: InstancePath, element_id: str, name: str, data: bytes) -> RemoteImage:
        path = ElementPath.from_path(instance, element_id)
        content_type = _image_type(name)
        response = blob_elements.upload_file_update_element(self.api, path, name, data, content_type)
        return RemoteImage(response["id"], response["name"], response["microversionId"], content_type)

    def element_ids(self, instance: InstancePath) -> set[str]:
        return {element["id"] for element in documents.get_document_elements(self.api, instance)}

    def workspaces(self, document_id: str) -> list[RemoteWorkspace]:
        return [
            RemoteWorkspace(workspace["id"], workspace["name"], workspace.get("description") or "")
            for workspace in workspaces.get_workspaces(self.api, DocumentPath(document_id))
        ]

    def create_workspace(self, document_id: str, version_id: str, name: str, description: str) -> RemoteWorkspace:
        created = workspaces.create_workspace(self.api, DocumentPath(document_id), version_id, name, description)
        return RemoteWorkspace(created["id"], created["name"], description)

    def delete_workspace(self, instance: InstancePath) -> None:
        workspaces.delete_workspace(self.api, instance)

    def merge(self, target: InstancePath, source_workspace_id: str, strategy: MergeStrategy) -> None:
        merged = workspaces.merge_into_workspace(self.api, target, source_workspace_id, strategy)
        if merged.get("libraryVersionMismatch"):
            raise ValueError("Onshape won't merge workspaces at different FeatureScript versions.")

    def build_studio(
        self, instance: InstancePath, studio: ConfiguredStudio, feature_studio_id: str, microversion_id: str
    ) -> Built:
        return build(self.api, instance, studio, feature_studio_id, microversion_id)

    def diagnose_studio(self, instance: InstancePath, studio: ConfiguredStudio, built_tab: str) -> Diagnosis:
        return diagnose(self.api, instance, studio, built_tab)


def _image_type(name: str) -> str:
    content_type = image_type(name)
    if content_type is None:
        raise ValueError(f"{name} isn't an image which can be synced ({', '.join(IMAGE_TYPES)}).")
    return content_type


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
