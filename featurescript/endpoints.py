from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.endpoints import feature_studios, documents
from onshape_api.paths.paths import InstancePath, ElementPath
from featurescript import conf


def get_feature_studios(
    api: Api, instance_path: InstancePath
) -> dict[str, conf.FeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query={"elementType": "FEATURESTUDIO", "withThumbnails": False},
    )
    return _extract_studios(elements, instance_path)


def _extract_studios(
    elements: list[dict],
    instance_path: InstancePath,
) -> dict[str, conf.FeatureStudio]:
    """Constructs a dict of names to FeatureStudios from a list of elements returned by a get documents request."""
    return dict(
        (
            element["name"],
            conf.FeatureStudio(
                element["name"],
                ElementPath.from_path(instance_path, element["id"]),
                element["microversionId"],
            ),
        )
        for element in elements
    )


def get_feature_studio(
    api: Api, document_path: InstancePath, studio_name: str
) -> conf.FeatureStudio | None:
    """Fetches a single feature studio by name, or None if no such studio exists."""
    return get_feature_studios(api, document_path).get(studio_name, None)


def create_feature_studio(
    api: Api, instance_path: InstancePath, studio_name: str
) -> conf.FeatureStudio:
    """Constructs a feature studio with the given name.

    Returns a FeatureStudio representing the new studio.
    """
    response = api.post(
        api_path("featurestudios", instance_path, InstancePath),
        body={"name": studio_name},
    )

    return conf.FeatureStudio(
        studio_name,
        ElementPath.from_path(instance_path, response["id"]),
        response["microversionId"],
    )


def push_studio(
    api: Api, instance_path: InstancePath, studio_name: str, code: str
) -> dict:
    """Push code to a given document. If the studio does not exist in the document, it is first created."""
    studio = get_feature_studio(api, instance_path, studio_name)
    if not studio:
        studio = create_feature_studio(api, instance_path, studio_name)
    return feature_studios.push_code(api, studio.path, code)
