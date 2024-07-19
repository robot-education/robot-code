"""
Code for creating and parsing Robot FeatureScript versions.

Note the following behaviors:
All standard releases are always added on to main.
Prereleases are added to a new workspace branched from the most recent version on main.
An exception is if there is already a prerelease branched from main.
In that case, a new workspace is branched from that prerelease instead.

Note the above only applies to Frontend releases. Backend releases should always add a version directly to main.

By default, versions are bumped from the most recent previous version. 

When a prerelease version is first released, it is expected for a major or minor version to also be bumped.
For example, going from 1.2.3 to 2.0.0-beta. 
Future releases of that prerelease should then simply bump the prerelease, e.g. 2.0.0-beta.2.
The next major release should then be 2.0.0.
"""

from enum import StrEnum
import re
from semver import Version as SemVersion
from onshape_api.model.constants import START_VERSION_NAME

START_VERSION = SemVersion(0, 0, 0)


class VersionType(StrEnum):
    MAJOR = "major"
    MINOR = "minor"
    PATCH = "patch"


def bump_version(version: SemVersion, version_type: VersionType) -> SemVersion:
    return version.next_version(version_type)


def bump_prerelease(version: SemVersion) -> SemVersion:
    # Don't use semver next_version behavior for prereleases
    return version.bump_prerelease("beta")


def version_name(feature_name: str, version: SemVersion) -> str:
    return feature_name + " - v" + str(version)


class RobotVersion:
    """Represents a specific version of a Robot FeatureScript.

    Attributes:
        version_id: The id of the version in Onshape.
    """

    def __init__(self, feature_name: str, version: SemVersion, version_id: str) -> None:
        self.feature_name = feature_name
        self.version = version
        self.version_id = version_id

    def version_name(self) -> str:
        return version_name(self.feature_name, self.version)

    def is_prerelease(self) -> bool:
        """Returns true if the version is a prerelease."""
        return self.version.prerelease != None


ZERO_OR_NUMBER_REGEX = r"(?:0|[1-9]\d*)"
LEADING_VERSION_REGEX = r"\.".join([ZERO_OR_NUMBER_REGEX] * 3)
SEMVER_REGEX = (
    r"("
    + LEADING_VERSION_REGEX
    + r"(?:-(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?)"
)
"""A regex for matching entire semvars."""


def match_robot_version(version_name: str) -> tuple[str, str] | None:
    """Matches the robot version. Returns the name that was matched and the semver portion."""
    match = re.fullmatch(r"(.+) - v" + SEMVER_REGEX, version_name)
    if match == None:
        return None
    return match.group(1), match.group(2)


def parse_versions(versions_response: list[dict]) -> list[RobotVersion]:
    """Parses the versions in versions_response into a list of RobotVersions.

    Versions are returned in reverse-chronological order, with the newest version first.

    Invalid versions are removed.
    """
    versions = versions_response[::-1]
    robot_versions = []
    for version in versions:
        if version["name"] == START_VERSION_NAME:
            continue
        name_match = match_robot_version(version["name"])
        if name_match == None:
            continue
        robot_versions.append(
            RobotVersion(name_match[0], SemVersion.parse(name_match[1]), version["id"])
        )

    return robot_versions
