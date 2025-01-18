from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import InstancePath


def get_instance_metadata(api: Api, path: InstancePath) -> dict:
    query = {"includeComputedProperties": False}
    return api.get(api_path("metadata", path, InstancePath), query=query)
