from library.api import api_base, api_path, conf, constants
import re


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


def pull_code(api: api_base.Api, path: api_path.ElementPath) -> str:
    """Fetches code from a feature studio specified by path."""
    return api.get(api_path.api_path("featurestudios", path))["contents"]


def push_code(api: api_base.Api, path: api_path.ElementPath, code: str) -> dict:
    """Sends code to the given feature studio specified by path."""
    return api.post(api_path.api_path("featurestudios", path), body={"contents": code})


def std_version(api: api_base.Api) -> str:
    """Fetches the latest version of the onshape std."""
    code = pull_code(api, constants.STD_STUDIO_PATH)
    parsed = re.search("\\d{4,6}", code)
    if parsed is None:
        raise ValueError("Failed to find latest version of onshape std.")
    return parsed.group(0)
