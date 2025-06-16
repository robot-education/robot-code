from enum import StrEnum
from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_instance_type
from onshape_api.paths.api_path import api_path
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import DocumentPath, ElementPath, InstancePath


class ThumbnailSize(StrEnum):
    """Represents the possible sizes of a thumbnail."""

    STANDARD = "300x300"
    LARGE = "600x340"
    SMALL = "300x170"
    TINY = "70x40"

    @classmethod
    def __contains__(cls, item):
        try:
            cls(item)
        except ValueError:
            return False
        return True


def get_document_thumbnail(
    api: Api, instance_path: InstancePath, size: ThumbnailSize
) -> dict:
    """Returns the thumbnail of a given document."""
    assert_instance_type(instance_path, InstanceType.WORKSPACE, InstanceType.VERSION)
    path = api_path("thumbnails", instance_path, InstancePath) + "/s/" + size
    return api.get(path)


def get_element_thumbnail(
    api: Api,
    element_path: ElementPath,
    size: ThumbnailSize,
    configuration: str | None = None,
) -> dict:
    """Returns the thumbnail of a given element."""
    path = api_path("thumbnails", element_path, ElementPath)
    if configuration:
        path += "/ac/" + configuration
    path += "/s/" + size
    return api.get(path)
