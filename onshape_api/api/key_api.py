from __future__ import annotations
import json
import re
import secrets
from typing import Unpack, override
import string
import http
import os
import hmac
import hashlib
import base64
import logging
from datetime import datetime, timezone
from urllib import parse

import requests

from onshape_api import exceptions
from onshape_api.api.api_base import Api, ApiArgs, get_api_base_args
from onshape_api.utils import env_utils


def make_key_api(load_dotenv: bool = True) -> KeyApi:
    """
    Constructs an instance of an ApiKey API using credentials read from a .env file.

    The variables API_ACCESS_KEY and API_SECRET_KEY are required.
    The variables API_BASE_URL and API_LOGGING may also be set.
    """
    if load_dotenv:
        env_utils.load_env()
    kwargs = get_api_base_args()
    access_key = os.getenv("API_ACCESS_KEY")
    secret_key = os.getenv("API_SECRET_KEY")

    if not access_key or not secret_key:
        raise KeyError(
            "API_ACCESS_KEY and API_SECRET_KEY must be set, either in the environment or in a .env file"
        )

    return KeyApi(access_key, secret_key, **kwargs)


class KeyApi(Api):
    """Provides access to the Onshape API using API keys."""

    def __init__(
        self, access_key: str, secret_key: str, **kwargs: Unpack[ApiArgs]
    ) -> None:
        super().__init__(**kwargs)
        self._access_key = access_key
        self._secret_key = secret_key
        # Documents anonymous requests were refused for (see `Api._request`)
        self._private_documents: set[str | None] = set()

        if self._logging:
            logging.info(
                "Onshape instance created: access key = {}".format(self._access_key)
            )

    @override
    def _request(
        self,
        method: http.HTTPMethod,
        path: str,
        query: dict | str = "",
        body: dict | str | bytes = "",
        headers: dict[str, str] = {},
        anonymous: bool = False,
    ):
        document_id = _document_id(path)
        if anonymous and document_id not in self._private_documents:
            try:
                return self._send(method, path, query, body, headers, signed=False)
            except exceptions.ApiError as error:
                if error.status_code not in ANONYMOUS_REFUSALS:
                    raise
                # Not public; retry with credentials, and skip the anonymous attempt from now on
                self._private_documents.add(document_id)
                if self._logging:
                    logging.info("anonymous request refused; retrying with credentials")
        return self._send(method, path, query, body, headers, signed=True)

    def _send(
        self,
        method: http.HTTPMethod,
        path: str,
        query: dict | str,
        body: dict | str | bytes,
        headers: dict[str, str],
        signed: bool,
    ):
        query_str = query if isinstance(query, str) else parse.urlencode(query)

        body_str = body if isinstance(body, (str, bytes)) else json.dumps(body)

        url = self._base_url + path + "?" + query_str

        if signed:
            headers = make_headers(method, headers, url, self._access_key, self._secret_key)
        else:
            headers = make_anonymous_headers(headers)

        if self._logging:
            logging.info(("request url: " if signed else "anonymous request url: ") + url)
            logging.info("request headers: " + str(headers))
            if len(body) > 0:
                logging.info(body)

        res = requests.request(
            method,
            url,
            headers=headers,
            data=body_str,
            allow_redirects=False,
            stream=True,
        )
        status = http.HTTPStatus(res.status_code)

        if status.is_success:
            if self._logging:
                if res.text == "":
                    logging.info("request succeeded")
                else:
                    logging.info("request succeeded, details: " + res.text)
        elif status is http.HTTPStatus.TEMPORARY_REDIRECT:
            # The official Onshape app has redirect handling here, we skip because lazy
            if self._logging:
                logging.error("unhandled redirect, details: " + res.text)
            raise exceptions.ApiError(res.text, status)

            # location = parse.urlparse(res.headers["Location"])
            # if self._logging:
            #     logging.info("request redirected to: " + location.geturl())
            # return self._request(
            #     method, location.path, query=location.query, headers=headers
            # )

            # Official handling:
            # location = urlparse(res.headers["Location"])
            # querystring = parse_qs(location.query)
            # if self._logging:
            #     utils.log('request redirected to: ' + location.geturl())
            # new_query = {}
            # new_base_url = location.scheme + '://' + location.netloc
            # for key in querystring:
            #     new_query[key] = querystring[key][0]  # won't work for repeated query params
            # return self.request(method, location.path, query=new_query, headers=headers, base_url=new_base_url)
        else:
            if self._logging:
                logging.error("request failed, details: " + res.text)
            raise exceptions.ApiError(res.text, status)

        try:
            return res.json()
        except:
            return res


# Statuses Onshape answers anonymous requests about documents which aren't public with
ANONYMOUS_REFUSALS = (http.HTTPStatus.UNAUTHORIZED, http.HTTPStatus.FORBIDDEN, http.HTTPStatus.NOT_FOUND)


def _document_id(path: str) -> str | None:
    match = re.search(r"/d/([0-9a-f]{24})", path)
    return match[1] if match else None


def make_anonymous_headers(headers: dict[str, str]) -> dict[str, str]:
    """Headers for a request without credentials."""
    req_headers = {
        "Content-Type": headers.get("Content-Type", "application/json"),
        "User-Agent": "Onshape App",
        "Accept": "application/json",
    }
    req_headers.update(headers)
    return req_headers


def make_headers(
    method: http.HTTPMethod,
    headers: dict[str, str],
    url: str,
    access_key: str,
    secret_key: str,
) -> dict[str, str]:
    """Creates headers to sign the request.

    Args:
        headers: Other headers to pass in.
    """

    date = make_date()
    nonce = make_nonce()
    content_type = headers.get("Content-Type", "application/json")
    auth = make_authorization(
        method, url, nonce, date, access_key, secret_key, content_type
    )
    req_headers = {
        "Date": date,
        "On-Nonce": nonce,
        "Authorization": auth,
        "Content-Type": content_type,
        "User-Agent": "Onshape App",
        "Accept": "application/json",
    }
    req_headers.update(headers)
    return req_headers


def make_date():
    """Returns the current date and time."""
    return datetime.now(timezone.utc).strftime("%a, %d %b %Y %H:%M:%S GMT")


def make_nonce():
    """Generates a nonce suitable for signing an Onshape request."""
    chars = string.digits + string.ascii_letters
    # 24 is arbitrary; just has to be larger than 16
    return "".join(secrets.choice(chars) for _ in range(24))


def make_authorization(
    method: http.HTTPMethod,
    url: str,
    nonce: str,
    date: str,
    access_key: str,
    secret_key: str,
    content_type: str,
) -> str:
    """Constructs the signature portion of the authorization, as described in the docs here:
    https://onshape-public.github.io/docs/auth/apikeys/#request-signature
    """
    parsed = parse.urlparse(url)

    string_to_sign = (
        "\n".join([method, nonce, date, content_type, parsed.path, parsed.query]) + "\n"
    ).lower()
    signature = hmac.new(
        secret_key.encode(), string_to_sign.encode(), digestmod=hashlib.sha256
    ).digest()
    b64_signature = base64.b64encode(signature).decode()

    # Create the final authorization string
    return f"On {access_key}:HmacSHA256:{b64_signature}"


def make_basic_authorization(access_key: str, secret_key: str):
    """Basic authorization encoding.

    Currently unused, but a convenient (and less secure) alternative to encoding date, nonce, etc.
    """
    b64 = base64.b64encode(f"{access_key}:{secret_key}".encode()).decode()
    return f"Basic {b64}"
