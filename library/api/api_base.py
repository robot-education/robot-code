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
        - stack (str): Base URL
        - creds (str, default='creds.json'): Credentials location
        - logging (bool, default=True): Turn logging on or off
    """

    def __init__(self, url: str, logging: bool):
        self._url = url
        self._logging = logging

    @abstractmethod
    def request(
        self,
        method: str,
        path: str,
        query: dict = {},
        headers: dict = {},
        body: dict | str = {},
        base_url: str | None = None,
    ):
        """
        Issues a request to Onshape
        Args:
            - method (str): HTTP method
            - path (str): Api path for request
            - query (dict, default={}): Query params in key-value pairs
            - headers (dict, default={}): Key-value pairs of headers
            - body (dict, default={}): Body for POST request
            - base_url (str, default=None): Host, including scheme and port (if different from creds file)

        Returns:
            - requests.Response: Object containing the response from Onshape
        """
        ...

    def get(
        self,
        path: str,
        query: dict = {},
        headers: dict = {},
    ) -> Any:
        return self.request("get", path, query, headers)

    def post(
        self,
        path: str,
        query: dict = {},
        headers: dict = {},
        body: dict | str = {},
    ) -> Any:
        return self.request("post", path, query, headers, body)


class ApiKey(Api):
    """Provides access to the Onshape API via api keys."""

    def __init__(
        self,
        stack: str = "https://cad.onshape.com",
        creds: str = "creds.json",
        logging: bool = False,
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
                    self._access_key = stacks[stack]["access_key"].encode()
                    self._secret_key = stacks[stack]["secret_key"].encode()
                else:
                    raise ValueError("specified stack not in file")
            except TypeError:
                raise ValueError("%s is not valid json" % creds)

        super().__init__(url, logging)

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
        *,
        query: dict,
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

        query_str: str = parse.urlencode(query)

        hmac_str = (
            ("\n".join([method, nonce, date, ctype, path, query_str]) + "\n")
            .lower()
            .encode()
        )

        signature = base64.b64encode(
            hmac.new(self._secret_key, hmac_str, digestmod=hashlib.sha256).digest()
        )
        auth = (
            "On "
            + self._access_key.decode("utf-8")
            + ":HmacSHA256:"
            + signature.decode("utf-8")
        )
        return auth

    def _make_headers(self, method, path, query, headers):
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
        ctype = headers.get("Content-Type") or "application/json"

        auth = self._make_auth(method, date, nonce, path, query=query, ctype=ctype)

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

    def request(
        self,
        method: str,
        path: str,
        query: dict = {},
        headers: dict = {},
        body: dict | str = {},
        base_url: str | None = None,
    ):
        req_headers = self._make_headers(method, path, query, headers)
        if base_url is None:
            base_url = self._url
        url = base_url + path + "?" + parse.urlencode(query)

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

        if res.status_code == 307:
            location = parse.urlparse(res.headers["Location"])
            query_dict = parse.parse_qs(location.query)

            if self._logging:
                log("request redirected to: " + location.geturl())

            new_base_url = location.scheme + "://" + location.netloc

            new_query = {}
            for key in query_dict:
                # won't work for repeated query params
                new_query[key] = query_dict[key][0]

            return self.request(
                method,
                location.path,
                query=new_query,
                headers=headers,
                base_url=new_base_url,
            )
        elif not 200 <= res.status_code <= 206:
            if self._logging:
                log("request failed, details: " + res.text, level=1)
        else:
            if self._logging:
                log("request succeeded, details: " + res.text)

        try:
            return res.json()
        except:
            return res


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

    def _make_headers(self, headers):
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

    def request(
        self,
        method: str,
        path: str,
        query: dict = {},
        headers: dict = {},
        body: dict | str = {},
        base_url: str | None = None,
    ):
        req_headers = self._make_headers(headers)
        if base_url is None:
            base_url = self._url
        url = base_url + path + "?" + parse.urlencode(query)

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

        if res.status_code == 307:
            location = parse.urlparse(res.headers["Location"])
            query_dict = parse.parse_qs(location.query)

            if self._logging:
                log("request redirected to: " + location.geturl())

            new_base_url = location.scheme + "://" + location.netloc

            new_query = {}
            for key in query_dict:
                # won't work for repeated query params
                new_query[key] = query_dict[key][0]

            return self.request(
                method,
                location.path,
                query=new_query,
                headers=headers,
                base_url=new_base_url,
            )
        elif not 200 <= res.status_code <= 206:
            if self._logging:
                log("request failed, details: " + res.text, level=1)
        else:
            if self._logging:
                log("request succeeded, details: " + res.text)

        try:
            return res.json()
        except:
            return res
