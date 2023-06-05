from library.api import api_base, api_path, constant, conf
import re


def get_microversion_id(api: api_base.Api, element_path: api_path.ElementPath) -> str:
    """Fetches the microversion id of an element."""
    path = api_path.api_path("documents", element_path, "elements")
    return api.get(path, query={"elementId": element_path.element_id})[0][
        "microversionId"
    ]


def get_document_elements(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    """Fetches all elements in a document.

    Returns a dict mapping element names to their paths.
    """
    elements = api.get(api_path.api_path("documents", document_path, "elements"))
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


def get_studios(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, conf.FeatureStudio]:
    """Returns an array of feature studios in a document."""
    elements = api.get(
        api_path.api_path("documents", document_path, "elements"),
        query={"elementType": "FEATURESTUDIO"},
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


def make_feature_studio(
    api: api_base.Api, document_path: api_path.DocumentPath, studio_name: str
) -> conf.FeatureStudio:
    """Constructs a feature studio with the given name.

    Returns a FeatureStudio representing the new studio.
    """
    response = api.post(
        api_path.api_path("featurestudios", document_path),
        body={"name": studio_name},
    )

    return conf.FeatureStudio(
        studio_name,
        api_path.ElementPath(document_path, response["id"]),
        response["microversionId"],
    )


def get_code(api: api_base.Api, path: api_path.ElementPath) -> str:
    """Fetches code from a feature studio specified by path."""
    return api.get(api_path.api_path("featurestudios", path))["contents"]


def update_code(api: api_base.Api, path: api_path.ElementPath, code: str) -> dict:
    """Sends code to the given feature studio specified by path."""
    return api.post(api_path.api_path("featurestudios", path), body={"contents": code})


def std_version(api: api_base.Api) -> str:
    """Fetches the latest version of the onshape std."""
    code = get_code(api, constant.STD_STUDIO_PATH)
    parsed = re.search("\\d{4,6}", code)
    if parsed is None:
        raise ValueError("Failed to find latest version of onshape std.")
    return parsed.group(0)
