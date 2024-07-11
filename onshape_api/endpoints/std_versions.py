from onshape_api.api.api_base import Api
from onshape_api.endpoints.versions import get_latest_version, get_versions
from onshape_api.model.constants import STD_STUDIO_PATH


def get_latest_std_version(api: Api) -> str:
    """Returns the name of the latest version of the Onshape std."""
    version = get_latest_version(api, STD_STUDIO_PATH)
    return version["name"]


def get_std_versions(api: Api) -> list[str]:
    """Returns a list of the names of all versions of the Onshape std.

    The versions are in reverse chronological order, with the oldest version first.
    """
    # Omit "Start" version
    versions = get_versions(api, STD_STUDIO_PATH)[1:]
    return [version["name"] for version in versions]
