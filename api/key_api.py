from __future__ import annotations
import os
from typing import Unpack

import dotenv
from api import api_base
from api.api_base import Api, ApiBaseArgs, make_api_base_args
from api.logger import log
import hmac
import hashlib
import base64


def make_key_api() -> KeyApi:
    """
    Constructs an instance of an ApiKey API using credentials read from a .env file.

    The variables API_ACCESS_KEY and API_SECRET_KEY are required.
    The variables API_BASE_URL, API_VERSION, and API_LOGGING may also be set.
    """
    loaded = dotenv.load_dotenv()
    kwargs = make_api_base_args()
    access_key = os.getenv("API_ACCESS_KEY")
    secret_key = os.getenv("API_SECRET_KEY")

    if (access_key is None or secret_key is None) and not loaded:
        raise ValueError("Failed to load .env file")
    if access_key is None:
        raise KeyError("API_ACCESS_KEY is a required env variable")
    if secret_key is None:
        raise KeyError("API_SECRET_KEY is a required env variable")

    return KeyApi(access_key, secret_key, **kwargs)


class KeyApi(Api):
    """Provides access to the Onshape API using API keys."""

    def __init__(
        self, access_key: str, secret_key: str, **kwargs: Unpack[ApiBaseArgs]
    ) -> None:
        super().__init__(**kwargs)
        self._access_key = access_key
        self._secret_key = secret_key

        if self._logging:
            log("onshape instance created: access key = {}".format(self._access_key))

    def _make_auth(
        self,
        method: str,
        date: str,
        nonce: str,
        path: str,
        query_str: str,
        ctype: str,
    ):
        """
        Create the request signature to authenticate

        Args:
            - method (str): HTTP method
            - date (str): HTTP date header string
            - nonce (str): Cryptographic nonce
            - path (str): URL pathname
            - query (dict, default={}): URL query string in key-value pairs
            - ctype (str, default='application/json'): HTTP Content-Type
        """
        hmac_str = (
            ("\n".join([method, nonce, date, ctype, path, query_str]) + "\n")
            .lower()
            .encode()
        )

        signature = base64.b64encode(
            hmac.new(
                self._secret_key.encode(), hmac_str, digestmod=hashlib.sha256
            ).digest()
        )
        return "On " + self._access_key + ":HmacSHA256:" + signature.decode()

    def _make_headers(self, headers, *, method, path, query_str):
        """
        Creates a headers object to sign the request

        Args:
            - method (str): HTTP method
            - path (str): Request path, e.g. /api/documents. No query string
            - query (dict, default={}): Query string in key-value format
            - headers (dict, default={}): Other headers to pass in

        Returns:
            - dict: Dictionary containing all headers
        """

        date = api_base.make_date()
        nonce = api_base.make_nonce()
        ctype = headers.get("Content-Type", "application/json")
        auth = self._make_auth(method, date, nonce, path, query_str, ctype)

        req_headers = {
            "Content-Type": "application/json",
            "Date": date,
            "On-Nonce": nonce,
            "Authorization": auth,
            "User-Agent": "Onshape App",
            "Accept": "application/json",
        }

        # add in user-defined headers
        for h in headers:
            req_headers[h] = headers[h]

        return req_headers
