import json

from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath


def evaluate_feature_script(api: Api, part_studio_path: ElementPath, script: str) -> dict:
    """Evaluate a FeatureScript script against a given part studio.

    Returns the printed output of the script parsed as JSON.
    """
    result = api.post(
        api_path("partstudios", part_studio_path, ElementPath, "featurescript"),
        body={"script": script},
    )
    return json.loads(result["console"])
