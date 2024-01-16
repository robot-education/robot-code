from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path


def ping(api: Api, catch: bool = False) -> bool:
    """Pings the Onshape API's users/sessioninfo endpoint.

    Returns true if the ping was successful, and false if it was not.

    Args:
        catch: True to return False in place of any thrown exceptions.
    """
    try:
        api.get(api_path("users", end_route="sessioninfo"))
        return True
    except Exception as e:
        if catch:
            return False
        raise e
