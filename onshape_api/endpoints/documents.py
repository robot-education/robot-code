from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import DocumentPath, InstancePath
from onshape_api.types import DocumentContents, DocumentElement, DocumentInfo, ElementType


def get_document_elements(
    api: Api,
    instance_path: InstancePath,
    element_type: ElementType | None = None,
) -> list[DocumentElement]:
    """Fetches all elements (tabs) in a document, with each one's microversion.

    Anonymous for public documents (see `Api._request`).

    Args:
        element_type: The type of element to get. If None, all elements are returned.
    """
    query: dict = {"withThumbnails": False}
    if element_type is not None:
        query["elementType"] = element_type
    return api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query=query,
        anonymous=True,
    )


def get_document(api: Api, document_path: DocumentPath, fallback: bool = True) -> DocumentInfo:
    """A document's information: whether it's public, and the workspace it opens to.

    Anonymous for documents anyone with the link can open (see `Api._request`), public or link-shared.

    Args:
        fallback: False to raise Onshape's refusal of the anonymous request (an ApiError: 401, 403, or 404) for a
            document which isn't one of those, rather than ask again with credentials (see `Api._request`).
    """
    return api.get(
        api_path("documents", document_path, DocumentPath, skip_document_d=True), anonymous=True, fallback=fallback
    )


def get_default_workspace(api: Api, document_path: DocumentPath) -> InstancePath:
    """The workspace a document opens to."""
    document = get_document(api, document_path)
    return InstancePath(document_path.document_id, document["defaultWorkspace"]["id"])


def get_document_contents(api: Api, instance_path: InstancePath) -> DocumentContents:
    """Fetches every element (tab) in a document along with the document's folder structure.

    Requires credentials, even for public documents.
    """
    return api.get(api_path("documents", instance_path, InstancePath, "contents"))
