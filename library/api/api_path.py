import pathlib
from typing import Self, TypedDict
from urllib import parse
import dataclasses


@dataclasses.dataclass
class DocumentPath:
    document_id: str
    workspace_id: str

    def copy(self) -> Self:
        return DocumentPath(self.document_id, self.workspace_id)

    def __str__(self) -> str:
        return "/d/{}/w/{}".format(self.document_id, self.workspace_id)


def make_document_path(url: str) -> DocumentPath:
    path = pathlib.Path(parse.urlparse(url).path)
    return DocumentPath(path.parts[2], path.parts[4])


@dataclasses.dataclass
class ElementPath:
    path: DocumentPath
    element_id: str

    def copy(self) -> Self:
        return ElementPath(self.path.copy(), self.element_id)

    def __str__(self) -> str:
        return str(self.path) + "/e/" + self.element_id


class ElementPathObject(TypedDict):
    documentId: str
    workspaceId: str
    elementId: str


def make_element_path_from_obj(object: ElementPathObject):
    return make_element_path(
        object["documentId"], object["workspaceId"], object["elementId"]
    )


def make_element_path(
    document_id: str, workspace_id: str, element_id: str
) -> ElementPath:
    return ElementPath(DocumentPath(document_id, workspace_id), element_id)


def api_path(
    service: str,
    path: ElementPath | DocumentPath | None = None,
    secondary_service: str | None = None,
) -> str:
    api_path = "/api/" + service
    if path:
        api_path += str(path)
    if secondary_service:
        api_path += "/" + secondary_service
    return api_path
