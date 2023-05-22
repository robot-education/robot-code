from library.api import api, api_path, constant, storage
import re


def get_microversion_id(api: api.Api, path: api_path.StudioPath) -> str:
    """Fetches the microversion id of a studio."""
    query = {"elementId": path.id}
    return api.request(
        api_path.ApiRequest("get", "documents", path.path, "elements", query=query)
    ).json()[0]["microversionId"]


def get_studios(
    api: api.Api, document_path: api_path.DocumentPath
) -> list[storage.FeatureStudio]:
    """Returns an array of feature studios in a document."""
    query = {"elementType": "FEATURESTUDIO"}
    elements = api.request(
        api_path.ApiRequest("get", "documents", document_path, "elements", query=query)
    ).json()
    return _extract_studios(elements, document_path)


# Extracts paths from elements
def _extract_studios(
    elements: list[dict],
    document_path: api_path.DocumentPath,
) -> list[storage.FeatureStudio]:
    """Constructs a list of FeatureStudios from a list of elements returned by a get documents request."""
    return [
        storage.FeatureStudio(
            element["name"],
            api_path.StudioPath(document_path.copy(), element["id"]),
            element["microversionId"],
        )
        for element in elements
    ]


def make_feature_studio(
    api: api.Api, document_path: api_path.DocumentPath, studio_name: str
) -> storage.FeatureStudio:
    """Constructs a feature studio with the given name.

    Returns a FeatureStudio representing the new studio.
    """
    response = api.request(
        api_path.ApiRequest(
            "post",
            "featurestudios",
            document_path,
            body={"name": studio_name},
        ),
    ).json()

    return storage.FeatureStudio(
        studio_name,
        api_path.StudioPath(document_path, response["id"]),
        response["microversionId"],
    )


def get_code(api: api.Api, path: api_path.StudioPath) -> str:
    """Fetches code from a feature studio specified by path."""
    result = api.request(api_path.ApiRequest("get", "featurestudios", path)).json()
    return result["contents"]


def update_code(api: api.Api, path: api_path.StudioPath, code: str) -> dict:
    """Sends code to the given feature studio specified by path."""
    return api.request(
        api_path.ApiRequest("post", "featurestudios", path, body={"contents": code})
    ).json()


def std_version(api: api.Api) -> str:
    """Fetches the latest version of the onshape std."""
    code = get_code(api, constant.STD_PATH)
    parsed = re.search("\\d{4,6}", code)
    if parsed is None:
        raise RuntimeError("Failed to find latest version of onshape std.")
    return parsed.group(0)