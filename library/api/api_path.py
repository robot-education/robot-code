import pathlib
from typing import Self
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
class StudioPath:
    path: DocumentPath
    id: str

    def copy(self) -> Self:
        return StudioPath(self.path.copy(), self.id)

    def __str__(self) -> str:
        return str(self.path) + "/e/" + self.id


def make_studio_path(document_id: str, workspace_id: str, id: str) -> StudioPath:
    return StudioPath(DocumentPath(document_id, workspace_id), id)


@dataclasses.dataclass
class ApiRequest:
    method: str
    service: str
    path: StudioPath | DocumentPath | None = None
    secondary_service: str | None = None

    def __str__(self) -> str:
        path = "/api/" + self.service
        if self.path is not None:
            path += str(self.path)
        if self.secondary_service is not None:
            path += "/" + self.secondary_service
        return path
