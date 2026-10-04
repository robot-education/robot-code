"""Names and numbering of released FeatureScript versions.

Versions are named "<Feature name> - v<semver>", e.g. "Robot frame - v1.2.3", and are created in
the backend document whenever a FeatureScript is released.

A major release is a major event, typically constituting major changes to documentation or
significant changes to functionality. A minor release introduces minor new features, and a patch
fixes bugs.

Beta releases (prereleases) start a "beta campaign" by bumping the major, minor, or patch version
and adding a beta tag, e.g. 1.2.3 -> 2.0.0-beta.1. Further beta releases just bump the beta number
(2.0.0-beta.2), and the campaign ends with a regular release of the same version (2.0.0).
"""

from __future__ import annotations

import dataclasses
import enum
import re

from semver import Version as SemVersion

from fs_cli.remote import Version

START_VERSION = SemVersion(0, 0, 0)


class VersionType(enum.StrEnum):
    MAJOR = "major"
    MINOR = "minor"
    PATCH = "patch"


def feature_name_for(studio_name: str) -> str:
    """Converts a camel case studio name to the feature's display name, e.g. robotFrame.fs -> Robot frame."""
    words = re.findall(r"[a-zA-Z][^A-Z]*", studio_name.removesuffix(".fs"))
    if not words:
        return studio_name
    return " ".join([words[0].capitalize(), *(word.lower() for word in words[1:])])


def version_name(feature_name: str, version: SemVersion) -> str:
    return f"{feature_name} - v{version}"


@dataclasses.dataclass
class ReleasedVersion:
    feature_name: str
    version: SemVersion

    @property
    def name(self) -> str:
        return version_name(self.feature_name, self.version)

    @property
    def is_prerelease(self) -> bool:
        return self.version.prerelease is not None


_NUMBER = r"(?:0|[1-9]\d*)"
_IDENTIFIER = r"(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)"
_SEMVER = rf"({_NUMBER}\.{_NUMBER}\.{_NUMBER}(?:-{_IDENTIFIER}(?:\.{_IDENTIFIER})*)?)"


def parse_version_name(name: str) -> ReleasedVersion | None:
    match = re.fullmatch(rf"(.+) - v{_SEMVER}", name)
    if match is None:
        return None
    return ReleasedVersion(match.group(1), SemVersion.parse(match.group(2)))


def latest_release(
    feature_name: str, versions: list[Version]
) -> ReleasedVersion | None:
    """Returns the most recent release of a feature, given a document's versions (oldest first)."""
    for version in reversed(versions):
        released = parse_version_name(version.name)
        if released and released.feature_name == feature_name:
            return released
    return None


def next_version(
    previous: ReleasedVersion | None, bump: VersionType | None, beta: bool
) -> SemVersion:
    """Computes the version of the next release."""
    previous_is_beta = previous is not None and previous.is_prerelease
    if beta:
        if bump is None and not previous_is_beta:
            raise ValueError(
                "There is no beta in progress; start one by also passing --major, --minor, or --patch."
            )
        if bump is not None and previous_is_beta:
            raise ValueError(
                "A beta is already in progress; continue it by omitting --major/--minor/--patch."
            )
    elif bump is None:
        raise ValueError("Pass --major, --minor, or --patch (or --beta).")

    version = previous.version if previous else START_VERSION
    if bump is not None:
        version = version.next_version(bump)
    if beta:
        # Don't use next_version's behavior for prereleases
        version = version.bump_prerelease("beta")
    return version
