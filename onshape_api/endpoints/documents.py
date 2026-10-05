import enum

from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import InstancePath


class ElementType(enum.StrEnum):
    """Describes possible element (tab) types in a document."""

    PART_STUDIO = "PARTSTUDIO"
    ASSEMBLY = "ASSEMBLY"
    DRAWING = "DRAWING"
    FEATURE_STUDIO = "FEATURESTUDIO"
    BLOB = "BLOB"


def get_document_elements(
    api: Api,
    instance_path: InstancePath,
    element_type: ElementType | None = None,
) -> list[dict]:
    """Fetches all elements (tabs) in a document.

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


def get_document_contents(api: Api, instance_path: InstancePath) -> dict:
    """Fetches the elements (tabs) in a document along with the document's folder structure.

    Returns a dict with:
        elements: The elements, as returned by get_document_elements.
        folders: The root folder, a tree of {"groupName", "groups"} folders whose groups
            also contain {"elementId"} references to elements.
    """
    return api.get(api_path("documents", instance_path, InstancePath, "contents"))
