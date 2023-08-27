"""
Provides access to the Onshape REST API
"""
from abc import ABC, abstractmethod
from typing import Any
from library.api.logger import log

import os
import random
import string

# json for dumping api output, json5 for reading config (so comments are allowed)
import json, json5
import hmac
import hashlib
import base64
import datetime
import requests
from urllib import parse


class Api(ABC):
    """
    Provides access to the Onshape REST API.

    Attributes:
        logging: Turn logging on or off
    """

    def __init__(self, url: str, logging: bool, version: int = 6):
        """
        Args:
            url: The base url. Should generally be `https://cad.onshape.com`.
            logging: True to enable logging.
        """
        self._url = url
        self._path_base = "/api/v{}/".format(version)
        self._logging = logging

    @abstractmethod
    def _make_headers(self, headers, *, method, path, query_str):
        ...

    def request(
        self,
        method: str,
        path: str,
        query: dict | str = {},
        body: dict | str = {},
        headers: dict = {},
    ):
        """
        Issues a request to Onshape
        Args:
            method: HTTP method
            path: Api path for request
            query: Query params in key-value pairs
            doseq: Whether to convert arrays in query into individual parameters.
            headers: Key-value pairs of headers
            body: Body for POST request

        Returns:
            requests.Response: Object containing the response from Onshape
        """
        path = self._path_base + path
        query_str = query if isinstance(query, str) else parse.urlencode(query)
        req_headers = self._make_headers(
            headers, method=method, path=path, query_str=query_str
        )
        url = self._url + path + "?" + query_str

        if self._logging:
            log(body)
            log(req_headers)
            log("request url: " + url)

        # only parse as json string if we have to
        body = body if isinstance(body, str) else json.dumps(body)

        res = requests.request(
            method,
            url,
            headers=req_headers,
            data=body,
            allow_redirects=False,
            stream=True,
        )

        if not 200 <= res.status_code <= 206:
            if self._logging:
                log("request failed, details: " + res.text, level=1)
        else:
            if self._logging:
                log("request succeeded, details: " + res.text)

        try:
            return res.json()
        except:
            return res

    def get(
        self,
        path: str,
        query: dict | str = {},
        headers: dict = {},
    ) -> Any:
        return self.request("get", path, query=query, headers=headers)

    def post(
        self,
        path: str,
        query: dict = {},
        body: dict | str = {},
        headers: dict = {},
    ) -> Any:
        return self.request(
            "post",
            path,
            query=query,
            body=body,
            headers=headers,
        )

    def delete(self, path: str, query: dict | str = {}, headers: dict = {}) -> Any:
        return self.request("delete", path, query, headers=headers)


class ApiKey(Api):
    """Provides access to the Onshape API via api keys."""

    def __init__(
        self,
        stack: str = "https://cad.onshape.com",
        creds: str = "creds.json",
        logging: bool = False,
        **kwargs
    ) -> None:
        """
        Instantiates an instance of the Onshape class. Reads credentials from a JSON file
        of this format:

            {
                "http://cad.onshape.com": {
                    "access_key": "YOUR KEY HERE",
                    "secret_key": "YOUR KEY HERE"
                },
                etc... add new object for each stack to test on
            }

        The creds.json file should be stored in the root project folder; optionally,
        you can specify the location of a different file.
        """

        if not os.path.isfile(creds):
            raise IOError("{} is not a file".format(creds))

        with open(creds) as f:
            try:
                stacks: Any = json5.load(f)
                if stack in stacks:
                    url = stack
                    self._access_key: bytes = stacks[stack]["access_key"].encode()
                    self._secret_key: bytes = stacks[stack]["secret_key"].encode()
                else:
                    raise ValueError("specified stack not in file")
            except TypeError:
                raise ValueError("%s is not valid json" % creds)

        super().__init__(url, logging, **kwargs)

        if self._logging:
            log(
                "onshape instance created: url = %s, access key = %s"
                % (self._url, self._access_key)
            )

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
            hmac.new(self._secret_key, hmac_str, digestmod=hashlib.sha256).digest()
        )
        auth = "On " + self._access_key.decode() + ":HmacSHA256:" + signature.decode()
        return auth

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

        date = datetime.datetime.utcnow().strftime("%a, %d %b %Y %H:%M:%S GMT")
        nonce = make_nonce()
        ctype = headers.get("Content-Type", "application/json")
        auth = self._make_auth(method, date, nonce, path, query_str, ctype)

        req_headers = {
            "Content-Type": "application/json",
            "Date": date,
            "On-Nonce": nonce,
            "Authorization": auth,
            "User-Agent": "Onshape Python Sample App",
            "Accept": "application/json",
        }

        # add in user-defined headers
        for h in headers:
            req_headers[h] = headers[h]

        return req_headers


def make_nonce():
    """Generate a unique ID for a request, 25 chars in length.

    Returns:
        str: Cryptographic nonce
    """
    chars = string.digits + string.ascii_letters
    return "".join(random.choice(chars) for _ in range(25))


class ApiToken(Api):
    """Provides access to the Onshape api via an Oauth token."""

    def __init__(self, token: str, logging: bool = False):
        super().__init__("https://cad.onshape.com", logging)
        self._token = token

    def _make_headers(self, headers, **_kwargs):
        """
        Creates a headers object to sign the request

        Args:
            - method (str): HTTP method
            - headers (dict, default={}): Other headers to pass in
            - _kwargs: Unused keyword arguments

        Returns:
            - dict: Dictionary containing all headers
        """
        date = datetime.datetime.utcnow().strftime("%a, %d %b %Y %H:%M:%S GMT")
        req_headers = {
            "Content-Type": "application/json",
            "Date": date,
            "Authorization": "Bearer " + self._token,
            "User-Agent": "Onshape Python Sample App",
            "Accept": "application/json",
        }

        # add in user-defined headers
        for h in headers:
            req_headers[h] = headers[h]

        return req_headers
