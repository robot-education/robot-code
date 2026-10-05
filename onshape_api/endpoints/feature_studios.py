from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath
from onshape_api.types import FeatureSpecs, FeatureStudioContents, FeatureStudioInfo


def get_contents(api: Api, feature_studio_path: ElementPath) -> FeatureStudioContents:
    """Fetches the code in a Feature Studio."""
    return api.get(api_path("featurestudios", feature_studio_path, ElementPath))


def update_contents(
    api: Api, feature_studio_path: ElementPath, code: str
) -> FeatureStudioContents:
    """Replaces the code in a Feature Studio."""
    assert_workspace(feature_studio_path)
    return api.post(
        api_path("featurestudios", feature_studio_path, ElementPath),
        body={"contents": code},
    )


def create_feature_studio(
    api: Api, workspace_path: InstancePath, studio_name: str
) -> FeatureStudioInfo:
    """Creates a Feature Studio at the top level of a document."""
    assert_workspace(workspace_path)
    return api.post(
        api_path("featurestudios", workspace_path, InstancePath),
        body={"name": studio_name},
    )


def get_feature_specs(api: Api, feature_studio_path: ElementPath) -> FeatureSpecs:
    """Fetches the specs of the custom features defined in a Feature Studio."""
    return api.get(
        api_path("featurestudios", feature_studio_path, ElementPath, "featurespecs")
    )
