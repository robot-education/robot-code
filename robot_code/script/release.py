import enum
from library.api import api_base


class VersionType(enum.Enum):
    MAJOR = enum.auto()
    MINOR = enum.auto()
    PATCH = enum.auto()


def release(
    api: api_base.Api, script_name: str, version_type: VersionType, description: str
):
    pass
