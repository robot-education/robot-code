"""A collection of utilities for working with FeatureStudio objects.

Operates at a higher level than the basic utilities in onshape_api.
"""

import dataclasses
from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents, feature_studios
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath


@dataclasses.dataclass
class FeatureStudio:
    """Represents a FeatureStudio in Onshape.

    Attributes:
        name: The name of the feature studio.
        path: The path to the feature studio.
        microversion_id: The most recent microverison id of the feature studio.
            Used for external FeatureScript imports.
    """

    name: str
    path: ElementPath
    microversion_id: str

    def push(self, api: Api, code: str) -> dict:
        """Pushes this Studio to Onshape. The studio is created if it does not already exist."""
        element = documents.get_document_element(api, self.path)
        if element == None:
            return feature_studios.create_feature_studio(api, self.path, self.name)
        return feature_studios.push_code(api, self.path, code)


def pull_feature_studio(
    api: Api, instance_path: InstancePath, studio_name: str
) -> FeatureStudio:
    """Fetches a single feature studio by name, creating it if necessary."""
    feature_studio = get_feature_studio(api, instance_path, studio_name)
    if feature_studio == None:
        response = feature_studios.create_feature_studio(
            api, instance_path, studio_name
        )
        return FeatureStudio(
            studio_name,
            ElementPath.from_path(instance_path, response["id"]),
            response["microversionId"],
        )
    return feature_studio


def get_feature_studios(
    api: Api, instance_path: InstancePath
) -> dict[str, FeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query={"elementType": "FEATURESTUDIO", "withThumbnails": False},
    )
    return _extract_studios(elements, instance_path)


def get_feature_studio(
    api: Api, document_path: InstancePath, studio_name: str
) -> FeatureStudio | None:
    """Fetches a single feature studio by name, or None if no such studio exists."""
    return get_feature_studios(api, document_path).get(studio_name, None)


def _extract_studios(
    elements: list[dict],
    instance_path: InstancePath,
) -> dict[str, FeatureStudio]:
    """Constructs a list of FeatureStudios from a list of elements returned by a get documents request."""
    return dict(
        (
            element["name"],
            FeatureStudio(
                element["name"],
                ElementPath.from_path(instance_path, element["id"]),
                element["microversionId"],
            ),
        )
        for element in elements
    )
