"""Tests for KeyApi's anonymous requests, without calling Onshape."""

import http

import pytest

from onshape_api.api import key_api
from onshape_api.endpoints import blob_elements
from onshape_api.exceptions import ApiError
from onshape_api.paths.paths import ElementPath

DOCUMENT = "/documents/d/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f/elements"
OTHER = "/documents/d/111111111111111111111111/w/6c20cd994b174cc99668701f/elements"


class Response:
    def __init__(self, status: int, text: str = "[]") -> None:
        self.status_code = status
        self.text = text
        self.content = text.encode()

    def json(self):
        return [] if self.status_code == 200 else {"message": self.text}


@pytest.fixture
def sent(monkeypatch):
    """The requests made: (url, whether they were signed). Documents in `private` refuse anonymous ones."""
    sent = []
    private = {"111111111111111111111111"}

    def request(method, url, headers, **kwargs):
        signed = "Authorization" in headers
        sent.append((url.split("?")[0].removeprefix("https://cad.onshape.com/api/v16"), signed))
        if not signed and any(document in url for document in private):
            return Response(401, "Unauthenticated API request")
        return Response(200)

    monkeypatch.setattr(key_api.requests, "request", request)
    return sent


def test_anonymous_requests_have_no_credentials(sent):
    api = key_api.KeyApi("access", "secret")
    api.get(DOCUMENT, anonymous=True)
    api.get(DOCUMENT)
    assert sent == [(DOCUMENT, False), (DOCUMENT, True)]


def test_private_documents_fall_back_to_credentials_once(sent):
    api = key_api.KeyApi("access", "secret")
    assert api.get(OTHER, anonymous=True) == []
    api.get(OTHER, anonymous=True)
    # Refused anonymously once, then only signed; other documents are still tried anonymously
    api.get(DOCUMENT, anonymous=True)
    assert sent == [(OTHER, False), (OTHER, True), (OTHER, True), (DOCUMENT, False)]


def test_other_errors_arent_retried(sent, monkeypatch):
    monkeypatch.setattr(key_api.requests, "request", lambda *args, **kwargs: Response(400, "Bad"))
    with pytest.raises(ApiError) as error:
        key_api.KeyApi("access", "secret").get(DOCUMENT, anonymous=True)
    assert error.value.status_code == http.HTTPStatus.BAD_REQUEST


def test_logging_leaves_out_credentials(sent, caplog):
    caplog.set_level("INFO")
    api = key_api.KeyApi("the-access-key", "the-secret-key", logging=True)
    api.get(DOCUMENT)
    assert "request headers" in caplog.text
    assert "the-access-key" not in caplog.text
    assert "Authorization': '<redacted>'" in caplog.text


def test_json_files_download_as_bytes(sent, monkeypatch):
    # A blob element holding a .json file (like the frontend's featurescripts.json) comes back unparsed
    stored = '{"schemaVersion": 1, "features": []}'
    monkeypatch.setattr(key_api.requests, "request", lambda *args, **kwargs: Response(200, stored))
    path = ElementPath("00dd11dabe44da2db458f898", "6c20cd994b174cc99668701f", "0123456789abcdef01234567")
    assert blob_elements.download_file(key_api.KeyApi("access", "secret"), path) == stored.encode()


def test_anonymous_only_requests_raise_refusals_without_credentials(sent):
    api = key_api.KeyApi("access", "secret", anonymous=False)
    with pytest.raises(ApiError) as error:
        api.get(OTHER, anonymous=True, fallback=False)
    assert error.value.status_code == http.HTTPStatus.UNAUTHORIZED
    # Even with keys which sign everything else
    api.get(DOCUMENT, anonymous=True, fallback=False)
    api.get(DOCUMENT, anonymous=True)
    assert sent == [(OTHER, False), (DOCUMENT, False), (DOCUMENT, True)]
