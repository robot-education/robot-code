from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath


def delete_element(api: Api, element_path: ElementPath) -> None:
    """Deletes an element (tab) from a workspace."""
    assert_workspace(element_path)
    api.delete(api_path("elements", element_path, ElementPath))
