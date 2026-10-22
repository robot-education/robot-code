from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath


def get_features(api: Api, part_studio_path: ElementPath) -> dict:
    """Fetches a Part Studio's feature list: its `features`, and the `sourceMicroversion` they're at.

    Anonymous for public documents (see `Api._request`).
    """
    return api.get(api_path("partstudios", part_studio_path, ElementPath, "features"), anonymous=True)


def update_feature(api: Api, part_studio_path: ElementPath, feature: dict, source_microversion: str) -> dict:
    """Replaces a feature (by its `featureId`) in a Part Studio, as it was at source_microversion."""
    assert_workspace(part_studio_path)
    return api.post(
        api_path(
            "partstudios", part_studio_path, ElementPath, f"features/featureid/{feature['featureId']}"
        ),
        body={"btType": "BTFeatureDefinitionCall-1406", "feature": feature, "sourceMicroversion": source_microversion},
    )


def create_part_studio(api: Api, workspace_path: InstancePath, name: str) -> dict:
    """Creates an empty Part Studio at the top level of a document; answers its element, with its `id`."""
    assert_workspace(workspace_path)
    return api.post(api_path("partstudios", workspace_path, InstancePath), body={"name": name})


def add_feature(api: Api, part_studio_path: ElementPath, feature: dict) -> dict:
    """Adds a feature at the end of a Part Studio's feature list; answers it with its `featureState`."""
    assert_workspace(part_studio_path)
    return api.post(
        api_path("partstudios", part_studio_path, ElementPath, "features"),
        body={"btType": "BTFeatureDefinitionCall-1406", "feature": feature},
    )
