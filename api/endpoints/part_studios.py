import json
from library.api import api_base, api_path


def evaluate_feature_script(
    api: api_base.Api, part_studio_path: api_path.ElementPath, script: str
) -> dict:
    """Evaluate a FeatureScript against a given part studio.

    Returns the printed output of the script parsed as JSON.
    """
    result = api.post(
        api_path.element_api_path("partstudios", part_studio_path, "featurescript"),
        body={"script": script},
    )
    return json.loads(result["console"])
