"""
Code to release Robot FeatureScripts from the backend to the frontend repo.
"""

import enum, re
from featurescript.base.ctxt import make_context
from featurescript.conf import Config
from onshape_api import api_base
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.endpoints.versions import create_version
from onshape_api.paths.paths import ElementPath, path_to_url
from onshape_api.utils import str_utils

from featurescript import *
from featurescript.feature_studio import FeatureStudio
from onshape_api.utils.endpoint_utils import confirm_version_creation

RELEASE_PREAMBLE = """/**
 * {}
 * Source (public): {}
 * FeatureScript by Alex Kempen.
 */"""


def release_preamble(version_name: str, studio_path: ElementPath) -> str:
    return RELEASE_PREAMBLE.format(version_name, path_to_url(studio_path))


def match_version_types(feature_name: str, version_name: str) -> re.Match | None:
    return re.fullmatch(feature_name + r" - v(\d)\.(\d)\.(\d)", version_name)


def is_release_version(feature_name: str, version_name: str) -> bool:
    """True if version_name is a for a release of feature_name."""
    return match_version_types(feature_name, version_name) != None


class VersionType(enum.Enum):
    MAJOR = enum.auto()
    MINOR = enum.auto()
    PATCH = enum.auto()
    BETA = enum.auto()


def release(
    api: api_base.Api, studio_name: str, version_type: VersionType, description: str
):
    """Releases a new version of a feature studio.

    Args:
        studio_name: The name of the Feature Studio containing the FeatureScript to release.
        version_type: The semantic portion of the version to increment.
        description: The description of the release version.
    """
    config = Config()
    backend = config.documents["backend"]
    frontend = config.documents["frontend"]
    feature_name = str_utils.display_name(studio_name.removesuffix(".fs"))
    studio = get_feature_studio(api, backend, studio_name)
    if not studio:
        raise ValueError("Could not find studio {} in backend.".format(studio_name))

    new_version_name = get_new_version_name(api, config, feature_name, version_type)

    confirm_version_creation(new_version_name)
    new_version = create_version(api, backend, new_version_name, description)

    update_release_studio(api, config, studio, new_version_name, new_version["id"])
    create_version(api, frontend, new_version_name, description)


def get_new_version_name(
    api: api_base.Api, config: Config, feature_name: str, version_type: VersionType
) -> str:
    backend_versions = versions.get_versions(api, config.documents["backend"])
    previous_version_name = None
    for version in reversed(backend_versions):
        if not is_release_version(feature_name, version["name"]):
            continue
        previous_version_name = version["name"]
        break

    if not previous_version_name:
        previous_version_name = feature_name + " - v0.0.0"
        if version_type.PATCH:
            raise ValueError("Initial release cannot be a patch")

    return increment_version(feature_name, previous_version_name, version_type)


def increment_version(
    feature_name: str, version_name: str, version_type: VersionType
) -> str:
    version_match = match_version_types(feature_name, version_name)
    if version_match == None:
        raise AssertionError("Failed to find version numbers in version")
    major_version = int(version_match[1])
    minor_version = int(version_match[2])
    patch_version = int(version_match[3])

    if version_type == VersionType.MAJOR:
        major_version += 1
        minor_version = 0
        patch_version = 0
    elif version_type == VersionType.MINOR:
        minor_version += 1
        patch_version = 0
    elif version_type == VersionType.PATCH:
        patch_version += 1
    elif version_type == VersionType.BETA:
        pass

    return "{} - v{}.{}.{}".format(
        feature_name, major_version, minor_version, patch_version
    )


def update_release_studio(
    api: api_base.Api,
    config: Config,
    studio: FeatureStudio,
    version_name: str,
    version_id: str,
) -> None:
    """Constructs a studio which exposes a backend Feature Script.

    To do so, we need a path to the backend studio.
    """
    version_path = ElementPath(
        studio.path.document_id,
        version_id,
        studio.path.element_id,
        instance_type="v",
    )
    release_studio = Studio(studio.name, "frontend", import_common=False).add(
        Id(release_preamble(version_name, version_path)),
        BaseImport(
            version_path.to_feature_studio_path(),
            studio.microversion_id,
            export=True,
        ),
    )
    std_version = get_latest_std_version(api)
    release_code = release_studio.build(make_context(std_version, config, api))

    studio.push(api, release_code)
