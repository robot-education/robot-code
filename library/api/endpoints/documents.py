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
    path = api_path.api_path("documents", element_path, "elements")
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
