from typing import Any
import re

from api import api_base, api_path, constants


def pull_code(
    api: api_base.Api, path: api_path.ElementPath, raw_response: bool = False
) -> Any:
    """Fetches code from a feature studio specified by path.

    Args:
        raw_response: True to get the entire response, False to get just the code.
    """
    response = api.get(api_path.element_api_path("featurestudios", path))
    return response if raw_response else response["contents"]


def push_code(api: api_base.Api, path: api_path.ElementPath, code: str) -> dict:
    """Sends code to the given feature studio specified by path."""
    return api.post(
        api_path.element_api_path("featurestudios", path), body={"contents": code}
    )


def std_version(api: api_base.Api) -> str:
    """Fetches the latest version of the onshape std."""
    code = pull_code(api, constants.STD_STUDIO_PATH)
    parsed = re.search("\\d{4,6}", code)
    if parsed is None:
        raise ValueError("Failed to find latest version of onshape std.")
    return parsed.group(0)
