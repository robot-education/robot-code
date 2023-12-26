import http
import json
import logging
from typing import Unpack, override
from urllib import parse
import dotenv
from requests_oauthlib import OAuth2Session
from api import api_base, exceptions, utils
from api.api_base import Api, ApiBaseArgs, make_api_base_args


def make_oauth_api(load_dotenv: bool = True):
    if load_dotenv:
        utils.load_env()
    kwargs = make_api_base_args()


class OauthApi(Api):
    """Provides access to the Onshape api via an Oauth token."""

    def __init__(self, oauth: OAuth2Session, **kwargs: Unpack[ApiBaseArgs]):
        super().__init__(**kwargs)
        self._oauth = oauth

    @override
    def _request(
        self,
        method: http.HTTPMethod,
        path: str,
        query: dict | str = {},
        body: dict | str = {},
        headers: dict[str, str] = {},
    ):
        base_url = self._base_url + path

        if self._logging:
            if len(body) > 0:
                logging.info(body)
            logging.info("request url: " + base_url)

        headers = {"Content-Type": headers.get("Content-Type", "application/json")}

        res = self._oauth.request(
            method,
            base_url,
            headers=headers,
            params=query,
            data=body,
        )
        status = http.HTTPStatus(res.status_code)
        if status.is_success:
            if self._logging:
                logging.info("request succeeded, details: " + res.text)
        else:
            if self._logging:
                logging.error("request failed, details: " + res.text)
            raise exceptions.ApiException(res.text, status)

        try:
            return res.json()
        except:
            return res
