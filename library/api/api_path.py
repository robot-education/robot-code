import pathlib
from typing import Self, TypedDict
from urllib import parse
import dataclasses


@dataclasses.dataclass(unsafe_hash=True)
class DocumentPath:
    document_id: str
    workspace_id: str
    workspace_or_version: str = "w"

    def copy(self) -> Self:
        return DocumentPath(self.document_id, self.workspace_id)

    def as_document(self) -> str:
        """Returns a version of the path suitable for use as a version."""
        return "/d/" + self.document_id

    def __str__(self) -> str:
        return (
            "/d/{}/".format(self.document_id)
            + self.workspace_or_version
            + "/{}".format(self.workspace_id)
        )


def make_document_path(url: str) -> DocumentPath:
    path = pathlib.Path(parse.urlparse(url).path)
    return DocumentPath(path.parts[2], path.parts[4])


@dataclasses.dataclass(unsafe_hash=True)
class ElementPath:
    path: DocumentPath
    element_id: str

    def copy(self) -> Self:
        return ElementPath(self.path.copy(), self.element_id)

    def __str__(self) -> str:
        return str(self.path) + "/e/" + self.element_id

    def as_link(self) -> str:
        """Returns a link to the element."""
        return "https://cad.onshape.com/documents/{}".format(
            str(self).removeprefix("/d/")
        )

    def as_path(self) -> str:
        """Returns a version of the path suitable for use as the path property of Feature Studio imports."""
        return "{}/{}/{}".format(
            self.path.document_id, self.path.workspace_id, self.element_id
        )


class ElementPathObject(TypedDict):
    documentId: str
    workspaceId: str
    elementId: str


def make_element_path_from_obj(object: ElementPathObject) -> ElementPath:
    return make_element_path(
        object["documentId"], object["workspaceId"], object["elementId"]
    )


def make_element_path(
    document_id: str,
    workspace_id: str,
    element_id: str,
    workspace_or_version: str = "w",
) -> ElementPath:
    return ElementPath(
        DocumentPath(document_id, workspace_id, workspace_or_version), element_id
    )


@dataclasses.dataclass(unsafe_hash=True)
class PartPath:
    path: ElementPath
    part_id: str

    def copy(self) -> Self:
        return PartPath(self.path.copy(), self.part_id)


def api_path(
    service: str,
    path: ElementPath | DocumentPath | str | None = None,
    secondary_service: str | None = None,
) -> str:
    api_path = service
    if path:
        api_path += str(path)
    if secondary_service:
        api_path += "/" + secondary_service
    return api_path
