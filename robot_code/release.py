"""
Code to release Robot FeatureScripts from the backend to the frontend repo.
"""

from featurescript.base.ctxt import Context
from onshape_api.api.api_base import Api
from onshape_api.endpoints.feature_studios import get_feature_specs
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.endpoints.versions import create_version, get_versions
from onshape_api.model.constants import START_VERSION_NAME
from onshape_api.paths.paths import ElementPath, InstancePath, path_to_url

from featurescript import *
from featurescript.feature_studio import (
    FeatureStudio,
    get_feature_studio,
    pull_feature_studio,
)
from robot_code.confirm import confirm
from robot_code.documents import (
    BACKEND,
    FRONTEND,
    BETA_FRONTEND,
    TEST_BACKEND,
    TEST_FRONTEND,
    Document,
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


def verify_feature_name(
    api: Api, backend_studio_path: ElementPath, is_prerelease: bool
):
    specs = get_feature_specs(api, backend_studio_path)["featureSpecs"]
    if len(specs) > 1:
        names = list(spec["featureTypeName"] for spec in specs)
        raise ValueError(
            "The feature studio has multiple custom features defined: "
            + ", ".join(names)
        )
    elif len(specs) <= 0:
        raise ValueError("The feature studio doesn't have any valid custom features.")
    feature_name: str = specs[0]["featureTypeName"]
    name_has_beta = "beta" in feature_name.lower()
    if is_prerelease and not name_has_beta:
        raise ValueError("Prerelease feature names must contain the word beta")
    elif not is_prerelease and name_has_beta:
        raise ValueError("Regular release feature names cannot contain the word beta")


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
    script_name: str,
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

    feature_name = str_utils.display_name(script_name)
    studio_name = script_name + ".fs"

    backend = Document(api, TEST_BACKEND if test else BACKEND)
    frontend = Document(api, TEST_FRONTEND if test else FRONTEND)

    # Verify studio exists
    backend_studio_path = backend.get_element_path(studio_name)
    verify_feature_name(api, backend_studio_path, is_prerelease)

    frontend_studio = frontend.get_element(studio_name)
    if frontend_studio == None:
        print(
            "WARNING: The frontend studio does not currently exist and will be created from scratch."
        )

    backend_versions = parse_versions(get_versions(api, backend.path))
    previous_version = get_previous_version(feature_name, backend_versions)

    new_version_name = get_new_version_name(
        feature_name, previous_version, version_type, is_prerelease
    )

    confirm_release(new_version_name, previous_version, test)

    if is_prerelease:
        workspace_to_use = BETA_FRONTEND
        # if len(frontend_versions) > 0 and frontend_versions[0].is_prerelease():
        #     # Most recent version is pre-release - use existing Beta workspace
        #     workspace_to_use = FRONTEND_BETA
        #     # version_id = frontend_versions[0].version_id
        #     # version_path = InstancePath.from_path(
        #     #     frontend.path, version_id, InstanceType.VERSION
        #     # )
        #     # response = create_new_workspace_from_instance(api, version_path, "Beta")
        # else:
        #     # Delete Beta workspace and make it again to make it current with Main
        #     delete_workspace(api, FRONTEND_BETA)
        #     response = create_new_workspace(api, frontend.path, "Beta")
        # workspace_to_use = InstancePath.from_path(frontend.path, response["id"])
    else:
        workspace_to_use = frontend.path

    # Create a version in the backend
    backend_version = create_version(api, backend.path, new_version_name, description)
    backend_version_path = InstancePath.from_path(
        backend.path, backend_version["id"], InstanceType.VERSION
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

    # if is_prerelease:
    #     # cleanup workspace
    #     delete_workspace(api, workspace_to_use)


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
    return confirm(message, "Aborted version creation.")


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
