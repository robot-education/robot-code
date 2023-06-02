"""
Provides access to the Onshape REST API
"""
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


class Api:
    """
    Provides access to the Onshape REST API.

    Attributes:
        - stack (str): Base URL
        - creds (str, default='creds.json'): Credentials location
        - logging (bool, default=True): Turn logging on or off
    """

    def __init__(
        self,
        stack: str = "https://cad.onshape.com",
        creds: str = "creds.json",
        logging: bool = True,
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
                stacks = json5.load(f)
                if stack in stacks:  # type: ignore
                    self._url = stack
                    self._access_key = stacks[stack]["access_key"].encode("utf-8")  # type: ignore
                    self._secret_key = stacks[stack]["secret_key"].encode("utf-8")  # type: ignore
                    self._logging = logging
                else:
                    raise ValueError("specified stack not in file")
            except TypeError:
                raise ValueError("%s is not valid json" % creds)

        if self._logging:
            log(
                "onshape instance created: url = %s, access key = %s"
                % (self._url, self._access_key)
            )

    def _make_nonce(self):
        """
        Generate a unique ID for the request, 25 chars in length

        Returns:
            - str: Cryptographic nonce
        """

        chars = string.digits + string.ascii_letters
        nonce = "".join(random.choice(chars) for _ in range(25))

        if self._logging:
            log("nonce created: %s" % nonce)

        return nonce

    def _make_auth(
        self,
        method: str,
        date: str,
        nonce: str,
        path: str,
        query: dict = {},
        ctype: str = "application/json",
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
            .encode("utf-8")
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

        if self._logging:
            log(
                {
                    "query": query,
                    "hmac_str": hmac_str,
                    "signature": signature,
                    "auth": auth,
                }
            )

        return auth

    def _make_headers(self, method, path, query={}, headers={}):
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
        nonce = self._make_nonce()
        ctype = (
            headers.get("Content-Type")
            if headers.get("Content-Type")
            else "application/json"
        )

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
        if not 200 <= res.status_code <= 206:
            if self._logging:
                log("request failed, details: " + res.text, level=1)
        elif res.status_code == 307:
            if self._logging:
                log("request was supposed to be redirected.")
        else:
            if self._logging:
                log("request succeeded, details: " + res.text)

        try:
            return res.json()
        except:
            return res


def get(
    api: Api,
    path: str,
    query: dict = {},
    headers: dict = {},
) -> Any:
    return api.request("get", path, query, headers)


def post(
    api: Api,
    path: str,
    query: dict = {},
    headers: dict = {},
    body: dict | str = {},
) -> Any:
    return api.request("post", path, query, headers, body)
