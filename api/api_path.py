from __future__ import annotations
import pathlib
from urllib import parse
from typing import Literal


class DocumentPath:
    def __init__(
        self,
        document_id: str,
        workspace_id: str,
        workspace_or_version: str = "w",
    ) -> None:
        self.document_id = document_id
        self.workspace_id = workspace_id
        self.workspace_or_version = workspace_or_version

    @staticmethod
    def from_url(url: str) -> DocumentPath:
        """
        Args:
            url: A url for a document.
        """
        path = parse.urlparse(url).path
        path = path.removeprefix("/documents")
        return DocumentPath.from_path(path)

    @staticmethod
    def from_path(path: str) -> DocumentPath:
        """
        Args:
            path: A path of the form "/d/<documentId>/<w|v|m>/<workspaceId>/e/<elementId>".
        """
        parts = pathlib.Path(path).parts
        return DocumentPath(parts[1], parts[3], parts[2])  # type: ignore

    def to_document_path(self) -> DocumentPath:
        return DocumentPath(
            self.document_id, self.workspace_id, self.workspace_or_version
        )

    def __str__(self) -> str:
        return (
            "/d/{}/".format(self.document_id)
            + self.workspace_or_version
            + "/{}".format(self.workspace_id)
        )

    def __hash__(self) -> int:
        return hash((self.document_id, self.workspace_id, self.workspace_or_version))

    def to_document_base(self) -> str:
        """Returns just the document portion of the path."""
        return "/d/" + self.document_id


def make_document_path(
    document_id: str,
    workspace_id: str,
    workspace_or_version: str = "w",
) -> DocumentPath:
    return DocumentPath(document_id, workspace_id, workspace_or_version)


class ElementPath(DocumentPath):
    def __init__(self, document_path: DocumentPath, element_id: str) -> None:
        super().__init__(
            document_path.document_id,
            document_path.workspace_id,
            document_path.workspace_or_version,
        )
        self.element_id = element_id

    @staticmethod
    def from_url(url: str) -> ElementPath:
        """
        Args:
            url: A url for a document.
        """
        path = parse.urlparse(url).path
        path = path[path.find("/") :]
        return ElementPath.from_path(path)

    @staticmethod
    def from_path(path: str) -> ElementPath:
        """
        Args:
            path: A path of the form "/d/<documentId>/<w|v|m>/<workspaceId>/e/<elementId>".
        """
        parts = pathlib.Path(path).parts
        return ElementPath(DocumentPath.from_path(path), parts[5])

    def to_element_path(self) -> ElementPath:
        return ElementPath(
            self.to_document_path(),
            self.element_id,
        )

    def __str__(self) -> str:
        return str(super().to_document_path()) + "/e/" + self.element_id

    def __hash__(self) -> int:
        return hash((super(), self.element_id))

    def to_link(self) -> str:
        """Returns a link to the element."""
        return "https://cad.onshape.com/documents/{}".format(
            str(self).removeprefix("/d/")
        )

    def to_feature_studio_path(self) -> str:
        """Returns a version of the path suitable for use as the path property of Feature Studio imports."""
        return "{}/{}/{}".format(
            self.document_id,
            self.workspace_id,
            self.element_id,
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
            DocumentPath(
                element_path.document_id,
                element_path.workspace_id,
                element_path.workspace_or_version,
            ),
            element_path.element_id,
        )
        self.part_id = part_id

    def part_path(self) -> PartPath:
        return PartPath(
            self.to_element_path(),
            self.part_id,
        )

    def __hash__(self) -> int:
        return hash((super(), self.part_id))


def element_api_path(
    service: str,
    path: ElementPath,
    secondary_service: str | None = None,
    feature_id: str | None = None,
) -> str:
    """Constructs a path suitable for consumption by an API."""
    return api_path(service, str(path.to_element_path()), secondary_service, feature_id)


def document_api_path(
    service: str,
    path: DocumentPath,
    secondary_service: str | None = None,
    feature_id: str | None = None,
) -> str:
    """Constructs a path suitable for consumption by an API."""
    return api_path(
        service, str(path.to_document_path()), secondary_service, feature_id
    )


def api_path(
    service: str,
    path: str | None = None,
    secondary_service: str | None = None,
    feature_id: str | None = None,
) -> str:
    """Constructs a path suitable for consumption by an API."""
    if not service.startswith("/"):
        service = "/" + service
    api_path = service
    if path:
        api_path += str(path)
    if secondary_service:
        api_path += "/" + secondary_service
    if feature_id:
        # feature_ids can have slashes, quote prevents problems
        api_path += "/featureid/" + parse.quote(feature_id, safe="")

    return api_path


def get_wmv_key(path: DocumentPath) -> str:
    return "workspaceId" if path.workspace_or_version == "w" else "versionId"
