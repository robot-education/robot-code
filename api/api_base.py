from abc import ABC, abstractmethod
from typing import Any, NotRequired, TypedDict, Unpack
import os
import http
import logging

# logging.basicConfig(level=logging.INFO)


class ApiBaseArgs(TypedDict):
    base_url: NotRequired[str]
    logging: NotRequired[bool]
    version: NotRequired[int | None]


class ApiQueryArgs(TypedDict):
    query: NotRequired[str | dict]
    headers: NotRequired[dict[str, str]]


def make_api_base_args() -> ApiBaseArgs:
    """Constructs ApiBaseArgs from environment variables."""
    # True if API_LOGGING exists and isn't "false". Default is "false".
    kwargs: ApiBaseArgs = {"logging": os.getenv("API_LOGGING", "false") == "true"}
    if temp := os.getenv("API_VERSION"):
        kwargs["version"] = int(temp)
    if base_url := os.getenv("API_BASE_URL"):
        kwargs["base_url"] = base_url
    return kwargs


class Api(ABC):
    """
    Provides generic access to the Onshape REST API.

    An instance of this class may be used with any of the endpoints in the endpoints folder.

    Attributes:
        _base_url: The base url to use.
        _logging: Whether to log or not.
        _path_base: The /api/v portion of the url.
    """

    def __init__(
        self,
        base_url: str = "https://cad.onshape.com",
        logging: bool = False,
        version: int | None = 6,
    ):
        """
        Args:
            base_url: The base url to use.
            logging: Whether to enable logging.
            version: The version to use.
                If the version is None, no version is specified in the url of API calls.
                Note this does not result in using the latest version of the API automatically.
        """
        self._logging = logging
        self._base_url = base_url + "/api/"
        if version:
            self._base_url += "v{}/".format(version)

    @abstractmethod
    def _request(
        self,
        method: http.HTTPMethod,
        path: str,
        query: dict | str = "",
        body: dict | str = "",
        headers: dict[str, str] = {},
    ) -> Any:
        """
        Issues a request to Onshape.
        Args:
            method: An HTTP method.
            path: A path for the request, e.g. "documents/...".
            query: Query parameters for the request.
            body: A body for the POST request.
            headers: Extra headers to add to the request.

        Returns:
            The response from Onshape parsed as json, or the Response itself.

        Throws:
            ApiException: If Onshape returns an invalid response.
        """
        ...

    def get(self, path: str, **kwargs: Unpack[ApiQueryArgs]) -> Any:
        return self._request(http.HTTPMethod.GET, path=path, **kwargs)

    def post(
        self, path: str, body: dict | str = "", **kwargs: Unpack[ApiQueryArgs]
    ) -> Any:
        return self._request(http.HTTPMethod.POST, path, body=body, **kwargs)

    def delete(self, path: str, **kwargs: Unpack[ApiQueryArgs]) -> Any:
        return self._request(http.HTTPMethod.DELETE, path=path, **kwargs)
