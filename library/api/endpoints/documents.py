from library.api import api_base, api_path, conf


def get_document_elements(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    """Fetches all elements in a document.

    Returns a dict mapping element names to their paths.
    """
    elements = api.get(
        api_path.document_api_path("documents", document_path, "elements"),
        query={"withThumbnails": False},
    )
    return _extract_paths(elements, document_path)


def _extract_paths(
    elements: list[dict], document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    return dict(
        (
            element["name"],
            # Copy to prevent saving reference to input
            api_path.ElementPath(document_path.to_document_path(), element["id"]),
        )
        for element in elements
    )


def get_microversion_id(api: api_base.Api, element_path: api_path.ElementPath) -> str:
    """Fetches the microversion id of an element."""
    path = api_path.element_api_path("documents", element_path, "elements")
    return api.get(path, query={"elementId": element_path.element_id})[0][
        "microversionId"
    ]


def get_feature_studios(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, conf.FeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path.document_api_path("documents", document_path, "elements"),
        query={"elementType": "FEATURESTUDIO", "withThumbnails": False},
    )
    return _extract_studios(elements, document_path)


def get_feature_studio(
    api: api_base.Api, document_path: api_path.DocumentPath, studio_name: str
) -> conf.FeatureStudio | None:
    """Fetches a single feature studio by name, or None if no such studio exists."""
    return get_feature_studios(api, document_path).get(studio_name, None)


def _extract_studios(
    elements: list[dict],
    document_path: api_path.DocumentPath,
) -> dict[str, conf.FeatureStudio]:
    """Constructs a list of FeatureStudios from a list of elements returned by a get documents request."""
    return dict(
        (
            element["name"],
            conf.FeatureStudio(
                element["name"],
                # Copy to avoid saving reference
                api_path.ElementPath(document_path.to_document_path(), element["id"]),
                element["microversionId"],
            ),
        )
        for element in elements
    )


def get_versions(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
    offset: int = 0,
    limit: int = 0,
) -> list[dict]:
    """Fetches a list of versions of a document.

    Versions are returned in revese chronological order (oldest - newest).

    Args:
        offset: A starting offset to apply.
        limit: The max number of versions to return.
    """
    return api.get(
        api_path.api_path("documents", document_path.to_document_base(), "versions"),
        query={offset: offset, limit: limit},
    )


def get_latest_version(api: api_base.Api, document_path: api_path.DocumentPath) -> dict:
    return get_versions(api, document_path)[-1]


def confirm_version_creation(version_name: str):
    """Prompts the python user to confirm the creation of a version."""
    value = input(
        'You are about to irreversibly create a version named "{}". Versions cannot be deleted. Enter "yes" to confirm: '.format(
            version_name
        )
    )
    if value != "yes":
        raise ValueError("Aborted version creation.")


def make_version(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
    version_name: str,
    description: str,
) -> dict:
    """Creates a new version of a document."""
    body = {
        "name": version_name,
        "description": description,
        "documentId": document_path.document_id,
        "workspaceId": document_path.workspace_id,
    }
    return api.post(
        api_path.document_api_path("documents", document_path, "versions"),
        body=body,
    )


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
