"""
Code to release Robot FeatureScripts from the backend to the frontend repo.
"""

from featurescript.base.ctxt import Context
from onshape_api.model.constants import START_VERSION_NAME
from robot_code.conf import Config
from onshape_api.api.api_base import Api
from onshape_api.endpoints.documents import (
    create_new_workspace,
    create_new_workspace_from_instance,
    delete_workspace,
)
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.endpoints.versions import create_version, get_versions
from onshape_api.paths.paths import ElementPath, InstancePath, path_to_url

from featurescript import *
from featurescript.feature_studio import (
    FeatureStudio,
    get_feature_studio,
    pull_feature_studio,
)
from robot_code.robot_version import (
    START_VERSION,
    RobotVersion,
    VersionType,
    bump_version,
    bump_prerelease,
    parse_versions,
    version_name,
)


def get_previous_version(
    feature_name: str, versions: list[RobotVersion]
) -> RobotVersion | None:
    return next(
        filter(lambda version: version.feature_name == feature_name, versions), None
    )


def verify_studio_exists(api: Api, workspace_path: InstancePath, studio_name: str):
    backend_studio = get_feature_studio(api, workspace_path, studio_name)
    if backend_studio == None:
        raise ValueError("Could not find studio {} in backend.".format(studio_name))


def get_new_version_name(
    feature_name: str,
    previous_version: RobotVersion | None,
    version_type: VersionType | None,
    is_prerelease: bool,
) -> str:
    """Computes the name of the next version for a given release."""
    if is_prerelease:
        previous_is_prerelease = (
            previous_version != None and previous_version.is_prerelease()
        )
        if version_type == None and not previous_is_prerelease:
            raise ValueError(
                "Cannot continue prerelease campaign when prerelease campaign is not ongoing."
            )
        elif version_type != None and previous_is_prerelease:
            raise ValueError(
                "Cannot start new prerelease campaign when existing prerelease campaign is still ongoing."
            )

    if previous_version == None:
        version = START_VERSION
    else:
        version = previous_version.version

    if version_type != None:
        version = bump_version(version, version_type)

    if is_prerelease:
        version = bump_prerelease(version)

    return version_name(feature_name, version)


def release(
    api: Api,
    studio_name: str,
    description: str,
    version_type: VersionType | None = None,
    is_prerelease: bool = False,
    test: bool = False,
):
    """Releases a new version of a feature studio.

    Args:
        studio_name: The name of the Feature Studio containing the FeatureScript to release.
        version_type: The semantic portion of the version to increment.
        description: The description of the release version.
        test: Whether to use the test documents.
    """
    if version_type == None and not is_prerelease:
        raise ValueError("Must enter a version or make a prerelease.")
    feature_name = str_utils.display_name(studio_name.removesuffix(".fs"))

    config = Config()
    if test:
        backend = config.documents["test-backend"]
        frontend = config.documents["test-frontend"]
    else:
        backend = config.documents["backend"]
        frontend = config.documents["frontend"]

    verify_studio_exists(api, backend, studio_name)

    frontend_versions = parse_versions(get_versions(api, frontend))
    previous_version = get_previous_version(feature_name, frontend_versions)

    new_version_name = get_new_version_name(
        feature_name, previous_version, version_type, is_prerelease
    )

    confirm_release(new_version_name, previous_version, test)

    if is_prerelease:
        version_id = None
        if len(frontend_versions) > 0 and frontend_versions[0].is_prerelease():
            # Branch from previous version
            version_id = frontend_versions[0].version_id
            version_path = InstancePath.from_path(
                frontend, version_id, InstanceType.VERSION
            )
            response = create_new_workspace_from_instance(api, version_path, "TEMP")
        else:
            response = create_new_workspace(api, frontend, "TEMP")
        workspace_to_use = InstancePath.from_path(frontend, response["id"])
    else:
        workspace_to_use = frontend

    # Create a version in the backend
    backend_version = create_version(api, backend, new_version_name, description)
    backend_version_path = InstancePath.from_path(
        backend, backend_version["id"], InstanceType.VERSION
    )
    backend_studio = get_feature_studio(api, backend_version_path, studio_name)
    if backend_studio == None:
        raise AssertionError(
            "Studio is unexpectedly missing from created version. Code left in bad state. Aborting."
        )

    # Generate release studio and create new version in frontend
    std_version = get_latest_std_version(api)
    release_studio_code = get_release_studio_code(
        new_version_name, std_version, backend_studio
    )

    frontend_studio = pull_feature_studio(api, workspace_to_use, studio_name)
    frontend_studio.push(api, release_studio_code)
    create_version(api, workspace_to_use, new_version_name, description)

    if is_prerelease:
        # cleanup workspace
        delete_workspace(api, workspace_to_use)


def confirm_release(
    version_name: str, previous_version: RobotVersion | None, test: bool
):
    """Prompts the user to confirm the release."""
    previous_version_name = (
        previous_version.version_name()
        if previous_version != None
        else START_VERSION_NAME
    )
    if test:
        message = f'You are about to do a TEST release of "{version_name}".'
    else:
        message = f'You are about to IRREVERSIBLY release "{version_name}".'

    message += f' The previous version is "{previous_version_name}".'

    value = input(message + " Please confirm (y/N):")
    if value == "" or value.lower() != "y":
        raise ValueError("Aborted version creation.")


RELEASE_PREAMBLE = """/**
 * {}
 * Source code (public): {}
 * FeatureScript by Alex Kempen.
 */"""


def release_preamble(version_name: str, studio_path: ElementPath) -> str:
    return RELEASE_PREAMBLE.format(version_name, path_to_url(studio_path))


def get_release_studio_code(
    version_name: str,
    std_version: int,
    backend_studio: FeatureStudio,
) -> str:
    """Generates a studio which exposes a backend Feature Script.

    To do so, we need a path to the backend studio.
    """
    feature_studio = Studio(backend_studio.name, import_common=False).add(
        Id(release_preamble(version_name, backend_studio.path)),
        external_import(
            backend_studio,
            export=True,
        ),
    )
    return feature_studio.build(Context(std_version))
