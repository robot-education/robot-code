from abc import ABC, abstractmethod
from datetime import datetime
import random
import string
from typing import Any, NotRequired, TypedDict, Unpack
import os
import http
import json
from urllib import parse

import requests

from api.logger import log
from api import exceptions


class ApiBaseArgs(TypedDict):
    base_url: NotRequired[str]
    logging: NotRequired[bool]
    version: NotRequired[int | None]


class ApiQueryArgs(TypedDict):
    query: NotRequired[str | dict | None]
    headers: NotRequired[dict | None]


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
        self._base_url = base_url
        self._logging = logging
        self._path_base = "/api/"
        if version:
            self._path_base += "v{}/".format(version)

    @abstractmethod
    def _make_headers(self, headers, *, method, path, query_str):
        ...

    def _request(
        self,
        method: http.HTTPMethod,
        path: str,
        query: dict | str | None = None,
        body: dict | str | None = None,
        headers: dict | None = None,
    ) -> Any:
        """
        Issues a request to Onshape.
        Args:
            method: An HTTP method.
            path: An api path for request.
            query: Query params in key-value pairs.
            body: A body for the POST request.
            headers: Extra headers to add to the request.

        Returns:
            The response from Onshape parsed as json, or the Response itself.

        Throws:
            ApiException: If Onshape returns an invalid response.
        """
        path = self._path_base + path

        if query is None:
            query = ""
        query_str = query if isinstance(query, str) else parse.urlencode(query)

        if body is None:
            body = ""
        # only parse as json string if we have to
        body_str = body if isinstance(body, str) else json.dumps(body)

        url = self._base_url + path + "?" + query_str

        if headers is None:
            headers = {}

        req_headers = self._make_headers(
            headers, method=method, path=path, query_str=query_str
        )

        if self._logging:
            if len(body) > 0:
                log(body)
            log(req_headers)
            log("request url: " + url)

        res = requests.request(
            method,
            url,
            headers=req_headers,
            data=body_str,
            allow_redirects=False,
            stream=True,
        )
        status = http.HTTPStatus(res.status_code)

        if status.is_success:
            if self._logging:
                log("request succeeded, details: " + res.text)
        elif status is http.HTTPStatus.is_redirection:
            # TODO: Handle redirect
            pass
        else:
            if self._logging:
                log("request failed, details: " + res.text, level=1)
            raise exceptions.ApiException(res.text, status)

        try:
            return res.json()
        except:
            return res

    def get(self, path: str, **kwargs: Unpack[ApiQueryArgs]) -> Any:
        return self._request(http.HTTPMethod.GET, path=path, **kwargs)

    def post(
        self, path: str, body: dict | str | None = None, **kwargs: Unpack[ApiQueryArgs]
    ) -> Any:
        return self._request(http.HTTPMethod.POST, path, body=body, **kwargs)

    def delete(self, path: str, **kwargs: Unpack[ApiQueryArgs]) -> Any:
        return self._request(http.HTTPMethod.DELETE, path=path, **kwargs)


def make_date():
    """Returns the current date and time."""
    return datetime.utcnow().strftime("%a, %d %b %Y %H:%M:%S GMT")


def make_nonce():
    """Generates a unique ID for a request. The ID has a length of 25."""
    chars = string.digits + string.ascii_letters
    return "".join(random.choice(chars) for _ in range(25))
