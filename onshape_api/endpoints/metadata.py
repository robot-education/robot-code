from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath


def get_element_metadata(api: Api, element_path: ElementPath) -> dict:
    """Fetches an element's (tab's) metadata: its properties, like its name.

    Anonymous for public documents (see `Api._request`).
    """
    return api.get(api_path("metadata", element_path, ElementPath), anonymous=True)


def update_element_metadata(api: Api, element_path: ElementPath, properties: list[dict]) -> dict:
    """Updates an element's (tab's) metadata properties, each a map of `propertyId` and `value`.

    See https://onshape-public.github.io/docs/api-adv/metadata/.
    """
    assert_workspace(element_path)
    return api.post(api_path("metadata", element_path, ElementPath), body={"properties": properties})


def rename_element(api: Api, element_path: ElementPath, name: str) -> None:
    """Renames an element (tab), through its Name property. Takes 2 calls: the property's id isn't fixed."""
    metadata = get_element_metadata(api, element_path)
    property_id = next(
        (
            property["propertyId"]
            for property in metadata.get("properties", [])
            if property.get("name") == "Name" and property.get("editable", True)
        ),
        None,
    )
    if property_id is None:
        raise ValueError(f"Element {element_path.element_id} has no editable Name property.")
    update_element_metadata(api, element_path, [{"propertyId": property_id, "value": name}])
