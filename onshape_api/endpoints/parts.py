"""Parts: the parts in a document's Part Studios."""

from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import InstancePath
from onshape_api.types import PartInfo


def get_parts(api: Api, instance_path: InstancePath) -> list[PartInfo]:
    """Fetches the parts of every Part Studio in a document, in one call."""
    return api.get(api_path("parts", instance_path, InstancePath), query={"withThumbnails": False})
