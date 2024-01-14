from enum import StrEnum

from onshape_api.paths.paths import InstancePath


class InstanceType(StrEnum):
    """Represents the type of a specific instance of a document."""

    WORKSPACE = "w"
    """Represents an editable workspace in a document."""
    VERSION = "v"
    """Represents a version of a document."""
    MICROVERSION = "m"
    """Represents an entry in the edit history of a document."""


def get_wmv_key(path: InstancePath) -> str:
    """Returns workspaceId if workspace_or_version is w, else versionId."""
    match path.instance_type:
        case InstanceType.WORKSPACE:
            return "workspaceId"
        case InstanceType.VERSION:
            return "versionId"
        case InstanceType.MICROVERSION:
            return "microversionId"


def assert_workspace(path: InstancePath) -> None:
    """Asserts the given path is a workspace."""
    return assert_instance_type(path, InstanceType.WORKSPACE)


def assert_version(path: InstancePath) -> None:
    """Asserts the given path is a version."""
    return assert_instance_type(path, InstanceType.VERSION)


def assert_instance_type(path: InstancePath, *instance_types: InstanceType) -> None:
    if path.instance_type not in instance_types:
        expected_types = " or ".join(type.name for type in instance_types)
        raise ValueError(
            f"The given path must be {expected_types}, got {path.instance_type.name}"
        )
