from library.api import api_base, api_path, conf


def get_document_elements(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    """Fetches all elements in a document.

    Returns a dict mapping element names to their paths.
    """
    elements = api.get(
        api_path.api_path("documents", document_path, "elements"),
        query={"withThumbnails": False},
    )
    return _extract_paths(elements, document_path)


def _extract_paths(
    elements: list[dict], document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    return dict(
        (
            element["name"],
            api_path.ElementPath(document_path.copy(), element["id"]),
        )
        for element in elements
    )


def get_microversion_id(api: api_base.Api, element_path: api_path.ElementPath) -> str:
    """Fetches the microversion id of an element."""
    path = api_path.api_path("documents", element_path.document_path(), "elements")
    return api.get(path, query={"elementId": element_path.element_id})[0][
        "microversionId"
    ]


def get_feature_studios(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, conf.FeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path.api_path("documents", document_path, "elements"),
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
                api_path.ElementPath(document_path.copy(), element["id"]),
                element["microversionId"],
            ),
        )
        for element in elements
    )


def get_versions(api: api_base.Api, document_path: api_path.DocumentPath) -> list[dict]:
    """Fetches a list of versions of a document.

    Versions are returned in chronological order, from oldest to newest.
    """
    return api.get(
        api_path.api_path("documents", document_path.as_document(), "versions")
    )


def make_version(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
    version_name: str,
    description: str,
    confirm: bool = False,
) -> dict:
    """Creates a new version in document.

    Args:
        confirm: Whether to confirm explicitly with the user before creating a version.
    """
    if confirm:
        value = input(
            'You are about to irreversibly create a version named "{}". Versions cannot be deleted. Enter "yes" to confirm: '.format(
                version_name
            )
        )
        if value != "yes":
            raise ValueError("Aborted version creation.")

    body = {
        "name": version_name,
        "description": description,
        "documentId": document_path.document_id,
        "workspaceId": document_path.workspace_id,
    }
    return api.post(
        api_path.api_path("documents", document_path.as_document(), "versions"),
        body=body,
    )
