from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import InstancePath
from onshape_api.types import DocumentContents, DocumentElement, ElementType


def get_document_elements(
    api: Api,
    instance_path: InstancePath,
    element_type: ElementType | None = None,
) -> list[DocumentElement]:
    """Fetches all elements (tabs) in a document, with each one's microversion.

    Args:
        element_type: The type of element to get. If None, all elements are returned.
    """
    query: dict = {"withThumbnails": False}
    if element_type is not None:
        query["elementType"] = element_type
    return api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query=query,
    )


def get_document_contents(api: Api, instance_path: InstancePath) -> DocumentContents:
    """Fetches every element (tab) in a document along with the document's folder structure.

    Requires credentials, even for public documents.
    """
    return api.get(api_path("documents", instance_path, InstancePath, "contents"))
