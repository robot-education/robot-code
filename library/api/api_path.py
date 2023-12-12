import pathlib
from typing_extensions import override
from urllib import parse
import dataclasses
from typing import Iterator, Literal, Self, TypedDict


class DocumentPath:
    def __init__(
        self,
        document_id: str,
        workspace_id: str,
        workspace_or_version: Literal["w", "m", "v"] = "w",
    ) -> None:
        self.document_id = document_id
        self.workspace_id = workspace_id
        self.workspace_or_version: Literal["w", "m", "v"] = workspace_or_version

    def copy(self) -> Self:
        return DocumentPath(
            self.document_id, self.workspace_id, self.workspace_or_version
        )

    def as_document(self) -> str:
        """Returns just the document portion of the path."""
        return "/d/" + self.document_id

    def __str__(self) -> str:
        return (
            "/d/{}/".format(self.document_id)
            + self.workspace_or_version
            + "/{}".format(self.workspace_id)
        )

    def __hash__(self) -> int:
        return hash((self.document_id, self.workspace_id, self.workspace_or_version))

    def __iter__(self) -> Iterator:
        yield self.document_id
        yield self.workspace_id
        yield self.workspace_or_version


def make_document_path(url: str) -> DocumentPath:
    path = pathlib.Path(parse.urlparse(url).path)
    return DocumentPath(path.parts[2], path.parts[4])


@dataclasses.dataclass(unsafe_hash=True)
class ElementPath(DocumentPath):
    def __init__(self, document_path: DocumentPath, element_id: str) -> None:
        super().__init__(*document_path)
        self.element_id = element_id

    @override
    def copy(self) -> Self:
        return ElementPath(
            super().copy(),
            self.element_id,
        )

    def document_path(self) -> DocumentPath:
        return super()

    def __str__(self) -> str:
        return str(super()) + "/e/" + self.element_id

    def __hash__(self) -> int:
        return hash((super(), self.element_id))

    def __iter__(self) -> Iterator:
        yield self.document_id
        yield self.workspace_id
        yield self.element_id
        yield self.workspace_or_version

    def as_link(self) -> str:
        """Returns a link to the element."""
        return "https://cad.onshape.com/documents/{}".format(
            str(self).removeprefix("/d/")
        )

    def as_path(self) -> str:
        """Returns a version of the path suitable for use as the path property of Feature Studio imports."""
        return "{}/{}/{}".format(self.document_id, self.workspace_id, self.element_id)


class ElementPathObject(TypedDict):
    documentId: str
    workspaceId: str
    elementId: str
    workspaceOrVersion: Literal["w", "m", "v"] | None


def make_element_path_from_obj(object: ElementPathObject) -> ElementPath:
    return make_element_path(
        object["documentId"],
        object["workspaceId"],
        object["elementId"],
        object.get("workspaceOrVersion") or "w",
    )


def make_element_path(
    document_id: str,
    workspace_id: str,
    element_id: str,
    workspace_or_version: Literal["w", "m", "v"] = "w",
) -> ElementPath:
    return ElementPath(
        DocumentPath(document_id, workspace_id, workspace_or_version),
        element_id,
    )


class PartPath(ElementPath):
    def __init__(
        self,
        element_path: ElementPath,
        part_id: str,
    ) -> None:
        super().__init__(
            element_path.document_path(),
            element_path.element_id,
        )
        self.part_id = part_id

    @override
    def copy(self) -> Self:
        return PartPath(
            super().copy(),
            self.part_id,
        )

    def element_path(self) -> DocumentPath:
        return super()

    def __hash__(self) -> int:
        return hash((super(), self.part_id))

    def __iter__(self) -> Iterator:
        yield self.document_id
        yield self.workspace_id
        yield self.element_id
        yield self.part_id
        yield self.workspace_or_version


def api_path(
    service: str,
    path: ElementPath | DocumentPath | str | None = None,
    secondary_service: str | None = None,
    feature_id: str | None = None,
) -> str:
    """Constructs a path suitable for consumption by an API."""
    api_path = service
    if path:
        api_path += str(path)
    if secondary_service:
        api_path += "/" + secondary_service
    if feature_id:
        # feature_ids can have slashes, quote prevents problems
        api_path += "/featureid/" + parse.quote(feature_id, safe="")

    return api_path
