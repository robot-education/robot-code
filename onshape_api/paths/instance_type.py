from enum import StrEnum


class InstanceType(StrEnum):
    """Represents the type of a specific instance of a document."""

    WORKSPACE = "w"
    """Represents an editable workspace in a document."""
    VERSION = "v"
    """Represents a version of a document."""
    MICROVERSION = "m"
    """Represents an entry in the edit history of a document."""

    @classmethod
    def __contains__(cls, item):
        try:
            cls(item)
        except ValueError:
            return False
        return True
