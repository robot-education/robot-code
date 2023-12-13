from api import api_base, api_path
from api.endpoints import feature_studios
from featurescript import conf


def get_feature_studios(
    api: api_base.Api, document_path: api_path.DocumentPath
) -> dict[str, conf.FeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path.document_api_path("documents", document_path, "elements"),
        query={"elementType": "FEATURESTUDIO", "withThumbnails": False},
    )
    return _extract_studios(elements, document_path)


def get_feature_studio(
    api: api_base.Api, document_path: api_path.DocumentPath, studio_name: str
) -> conf.FeatureStudio | None:
    """Fetches a single feature studio by name, or None if no such studio exists."""
    return get_feature_studios(api, document_path).get(studio_name, None)


def _extract_studios(
    elements: list[dict],
    document_path: api_path.DocumentPath,
) -> dict[str, conf.FeatureStudio]:
    """Constructs a list of FeatureStudios from a list of elements returned by a get documents request."""
    return dict(
        (
            element["name"],
            conf.FeatureStudio(
                element["name"],
                # Copy to avoid saving reference
                api_path.ElementPath(document_path, element["id"]),
                element["microversionId"],
            ),
        )
        for element in elements
    )


def create_feature_studio(
    api: api_base.Api, document_path: api_path.DocumentPath, studio_name: str
) -> conf.FeatureStudio:
    """Constructs a feature studio with the given name.

    Returns a FeatureStudio representing the new studio.
    """
    response = api.post(
        api_path.document_api_path("featurestudios", document_path),
        body={"name": studio_name},
    )

    return conf.FeatureStudio(
        studio_name,
        api_path.ElementPath(document_path, response["id"]),
        response["microversionId"],
    )


def push_studio(
    api: api_base.Api, document_path: api_path.DocumentPath, studio_name: str, code: str
) -> dict:
    """Push code to a given document. If the studio does not exist in the document, it is first created."""
    studio = get_feature_studio(api, document_path, studio_name)
    if not studio:
        studio = create_feature_studio(api, document_path, studio_name)
    return feature_studios.push_code(api, studio.path, code)
