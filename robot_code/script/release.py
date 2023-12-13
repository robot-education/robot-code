import enum, re
from featurescript.api import api_base, api_path, conf
from featurescript.api.endpoints import documents, feature_studios
from common import str_utils

from featurescript import *
from featurescript.base.ctxt import make_context

RELEASE_PREAMBLE = """/**
 * {}
 * Source (public): {}
 * FeatureScript by Alex Kempen.
 */"""


def release_preamble(version_name: str, studio_path: api_path.ElementPath) -> str:
    return RELEASE_PREAMBLE.format(version_name, studio_path.to_link())


def extract_versions(feature_name: str, version_name: str) -> re.Match | None:
    return re.fullmatch(feature_name + r" - (\d)\.(\d)\.(\d)", version_name)


def is_feature_release(feature_name: str, version_name: str) -> bool:
    """True if version_name is a release of feature_name."""
    return extract_versions(feature_name, version_name) != None


class VersionType(enum.Enum):
    MAJOR = enum.auto()
    MINOR = enum.auto()
    PATCH = enum.auto()


def release(
    api: api_base.Api, studio_name: str, version_type: VersionType, description: str
):
    """Releases a new version of a feature studio.

    Args:
        studio: The name of the Feature Studio containing the FeatureScript to release.
        version_type: The part of the version to increment.
        description: The description of the release version.
    """
    config = conf.Config()
    backend = config.documents["backend"]
    frontend = config.documents["frontend"]
    feature_name = str_utils.display_name(studio_name.removesuffix(".fs"))
    studio = documents.get_feature_studio(api, backend, studio_name)
    if not studio:
        raise ValueError("Could not find studio {} in backend.".format(studio_name))

    new_version_name = get_new_version_name(api, config, feature_name, version_type)

    documents.confirm_version_creation(new_version_name)
    new_version = documents.create_version(api, backend, new_version_name, description)

    update_release_studio(api, config, studio, new_version_name, new_version["id"])
    documents.create_version(api, frontend, new_version_name, description)


def get_new_version_name(
    api: api_base.Api, config: conf.Config, feature_name: str, version_type: VersionType
) -> str:
    backend_versions = documents.get_versions(api, config.documents["backend"])
    previous_version_name = None
    for version in reversed(backend_versions):
        if not is_feature_release(feature_name, version["name"]):
            continue
        previous_version_name = version["name"]
        break

    if not previous_version_name:
        previous_version_name = feature_name + " - 0.0.0"
        if version_type.PATCH:
            raise ValueError("Initial release cannot be a patch")

    return increment_version(feature_name, previous_version_name, version_type)


def increment_version(
    feature_name: str, version_name: str, version_type: VersionType
) -> str:
    version_match = extract_versions(feature_name, version_name)
    if not version_match:
        raise AssertionError("Failed to extract version numbers from version")
    major_version = int(version_match[1])
    minor_version = int(version_match[2])
    patch_version = int(version_match[3])
    if version_type == version_type.MAJOR:
        major_version += 1
        minor_version = 0
        patch_version = 0
    elif version_type == version_type.MINOR:
        minor_version += 1
        patch_version = 0
    else:
        patch_version += 1

    return "{} - {}.{}.{}".format(
        feature_name, major_version, minor_version, patch_version
    )


def update_release_studio(
    api: api_base.Api,
    config: conf.Config,
    studio: conf.FeatureStudio,
    version_name: str,
    version_id: str,
) -> None:
    """Constructs a studio which exposes a backend Feature Script.

    To do so, we need a path to the backend studio.
    """
    version_path = api_path.make_element_path(
        studio.path.document_id,
        version_id,
        studio.path.element_id,
        workspace_or_version="v",
    )
    release_studio = Studio(studio.name, "frontend", import_common=False).add(
        Id(release_preamble(version_name, version_path)),
        BaseImport(
            version_path.to_feature_studio_path(),
            studio.microversion_id,
            export=True,
        ),
    )
    std_version = feature_studios.std_version(api)
    release_code = release_studio.build(make_context(std_version, config, api))
    feature_studios.push_studio(
        api, config.documents["frontend"], studio.name, release_code
    )
