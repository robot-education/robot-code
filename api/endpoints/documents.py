import enum
from api import api_base, api_path
from api.endpoints.versions import get_latest_version


class ElementType(enum.StrEnum):
    PART_STUDIO = "PARTSTUDIO"
    ASSEMBLY = "ASSEMBLY"
    DRAWING = "DRAWING"
    FEATURE_STUDIO = "FEATURESTUDIO"


def get_document_elements(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
    element_type: ElementType | None = None,
) -> dict[str, api_path.ElementPath]:
    """Fetches all elements in a document.

    Returns a dict mapping element names to their paths.

    Args:
        element_type: The type of element (tab) to get. If None, all elements are returned.
    """
    query: dict = {"withThumbnails": False}
    if element_type:
        query["elementType"] = element_type

    return api.get(
        api_path.document_api_path("documents", document_path, "elements"),
        query=query,
    )


def get_microversion_id(api: api_base.Api, element_path: api_path.ElementPath) -> str:
    """Fetches the microversion id of a specific element."""
    # The actual microversion id endpoint is only document level...
    path = api_path.element_api_path("documents", element_path, "elements")
    return api.get(path, query={"elementId": element_path.element_id})[0][
        "microversionId"
    ]


def get_external_references(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
) -> dict:
    return api.get(
        api_path.document_api_path("documents", document_path, "externalreferences")
    )


# @deprecated("Possibly broken endpoint")
# def update_latest_references(
#     api: api_base.Api, element_path: api_path.ElementPath, element: str
# ) -> dict:
#     """Updates a tab's references to the latest versions."""
#     body = {"elements": [element]}
#     return api.post(
#         api_path.element_api_path(
#             "documents", element_path, "latestdocumentreferences"
#         ),
#         body=body,
#     )


def update_to_latest_reference(
    api: api_base.Api,
    element_path: api_path.ElementPath,
    reference_path: api_path.ElementPath,
) -> None:
    """Updates a reference to the latest version."""
    latest_version = get_latest_version(api, reference_path)["id"]
    update_reference(api, element_path, reference_path, latest_version)


def update_reference(
    api: api_base.Api,
    element_path: api_path.ElementPath,
    reference_path: api_path.ElementPath,
    version_id: str,
) -> None:
    """Updates a reference to the version specified by version_id."""
    body = {
        "referenceUpdates": [
            {
                "fromReference": {
                    "documentId": reference_path.document_id,
                    "versionId": reference_path.workspace_id,
                    "elementId": reference_path.element_id,
                },
                "toReference": {
                    "documentId": reference_path.document_id,
                    "versionId": version_id,
                    "elementId": reference_path.element_id,
                },
            }
        ]
    }
    api.post(
        api_path.element_api_path("elements", element_path, "updatereferences"),
        body=body,
    )
