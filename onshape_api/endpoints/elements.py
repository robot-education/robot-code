from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath


def delete_element(api: Api, element_path: ElementPath) -> None:
    """Deletes an element (tab) from a workspace."""
    assert_workspace(element_path)
    api.delete(api_path("elements", element_path, ElementPath))


def update_configuration(api: Api, element_path: ElementPath, configuration: dict) -> dict:
    """Replaces an element's (a Part Studio's or an assembly's) configuration inputs.

    See https://onshape-public.github.io/docs/api-adv/configs/.
    """
    assert_workspace(element_path)
    return api.post(api_path("elements", element_path, ElementPath, "configuration"), body=configuration)
