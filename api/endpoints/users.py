from api import api_path
from api.api_base import Api


def ping(api: Api, catch: bool = False) -> bool:
    """Pings the Onshape API's users/sessioninfo endpoint.

    Returns true if the ping was successful, and false if it was not.

    Args:
        catch: True to catch any thrown exceptions.
    """
    try:
        api.get(api_path.api_path("users", secondary_service="sessioninfo"))
        return True
    except Exception as e:
        if catch:
            return False
        raise e
