from api import api_base
from api.api_base import Api


class OauthApi(Api):
    """Provides access to the Onshape api via an Oauth token."""

    def __init__(self, token: str, logging: bool = False):
        super().__init__("https://cad.onshape.com", logging)
        self._token = token

    def _make_headers(self, headers, **_kwargs):
        """
        Creates a headers object to sign the request.

        Args:
            - method (str): HTTP method
            - headers (dict, default={}): Other headers to pass in
            - _kwargs: Unused keyword arguments

        Returns:
            - dict: Dictionary containing all headers
        """
        date = api_base.make_date()
        req_headers = {
            "Content-Type": "application/json",
            "Date": date,
            "Authorization": "Bearer " + self._token,
            "User-Agent": "Onshape App",
            "Accept": "application/json",
        }

        # add in user-defined headers
        for h in headers:
            req_headers[h] = headers[h]

        return req_headers
