"""Blob elements: tabs holding an uploaded file, like an image a Feature Studio imports for a feature's icon."""

import urllib3

from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath
from onshape_api.types import BlobElementInfo


def _form(file_name: str, data: bytes, content_type: str) -> tuple[bytes, dict[str, str]]:
    body, form_type = urllib3.encode_multipart_formdata({"file": (file_name, data, content_type)})
    return body, {"Content-Type": form_type}


def upload_file_create_element(
    api: Api, workspace_path: InstancePath, file_name: str, data: bytes, content_type: str
) -> BlobElementInfo:
    """Creates a blob element holding a file, at the top level of a document. It's named after the file."""
    assert_workspace(workspace_path)
    body, headers = _form(file_name, data, content_type)
    return api.post(api_path("blobelements", workspace_path, InstancePath), body=body, headers=headers)


def upload_file_update_element(
    api: Api, element_path: ElementPath, file_name: str, data: bytes, content_type: str
) -> BlobElementInfo:
    """Replaces the file in a blob element."""
    assert_workspace(element_path)
    body, headers = _form(file_name, data, content_type)
    return api.post(api_path("blobelements", element_path, ElementPath), body=body, headers=headers)


def download_file(api: Api, element_path: ElementPath) -> bytes:
    """Fetches the file in a blob element."""
    response = api.get(
        api_path("blobelements", element_path, ElementPath),
        headers={"Accept": "application/octet-stream"},
    )
    # Files which aren't JSON come back as the response itself
    return response.content if hasattr(response, "content") else bytes(response)
