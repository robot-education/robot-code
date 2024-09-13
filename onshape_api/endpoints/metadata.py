from typing import Any
from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath


def get_all_element_metadata(api: Api, instance_path: InstancePath):
    return api.get(
        api_path("metadata", instance_path, InstancePath, "e"),
        query={"includeComputedProperties": False},
    )


def get_element_metadata(api: Api, element_path: ElementPath):
    query = {"includeComputedProperties": False}
    return api.get(api_path("metadata", element_path, ElementPath), query=query)


def update_element_metadata(
    api: Api, element_path: ElementPath, property_id: str, value: Any
):
    body = {
        "jsonType": "metadata-element",
        "properties": [{"propertyId": property_id, "value": value}],
    }
    return api.post(api_path("metadata", element_path, ElementPath), body=body)
