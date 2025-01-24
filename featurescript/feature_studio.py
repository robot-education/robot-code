"""A collection of utilities for working with FeatureStudio objects.

Operates at a higher level than the basic utilities in onshape_api.
"""

import dataclasses
from onshape_api.api.api_base import Api
from onshape_api.endpoints import documents, feature_studios
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath, InstancePath


@dataclasses.dataclass
class LocalFeatureStudio:
    """Represents a FeatureStudio in Onshape.

    Attributes:
        name: The name of the feature studio.
        path: The path to the feature studio.
        microversion_id: The most recent microverison id of the feature studio in the cloud.
            Used for external FeatureScript imports.
        created: True if the FeatureStudio actually exists in the cloud.
        generated: True if the FeatureStudio is locally generated.
        modified: True if the FeatureStudio has been modified since being pulled.
    """

    name: str
    path: ElementPath
    microversion_id: str
    created: bool
    generated: bool = False
    modified: bool = False

    def push(self, api: Api, code: str) -> dict:
        """Pushes this Studio to Onshape. The studio is created if it does not already exist."""
        element = documents.get_document_element(api, self.path)
        if element == None:
            result = feature_studios.create_feature_studio(api, self.path, self.name)
            self.created = False
            return result
        return feature_studios.push_code(api, self.path, code)


def pull_feature_studio(
    api: Api, instance_path: InstancePath, studio_name: str
) -> LocalFeatureStudio:
    """Fetches a single feature studio by name, creating it if necessary."""
    feature_studio = get_feature_studio(api, instance_path, studio_name)
    if feature_studio == None:
        response = feature_studios.create_feature_studio(
            api, instance_path, studio_name
        )
        return LocalFeatureStudio(
            studio_name,
            ElementPath.from_path(instance_path, response["id"]),
            response["microversionId"],
            True,
        )
    return feature_studio


def get_feature_studios(
    api: Api, instance_path: InstancePath
) -> dict[str, LocalFeatureStudio]:
    """Returns a dict mapping feature studio names to feature studios."""
    elements = api.get(
        api_path("documents", instance_path, InstancePath, "elements"),
        query={"elementType": "FEATURESTUDIO", "withThumbnails": False},
    )
    return _extract_studios(elements, instance_path)


def get_feature_studio(
    api: Api, document_path: InstancePath, studio_name: str
) -> LocalFeatureStudio | None:
    """Fetches a single feature studio by name, or None if no such studio exists."""
    return get_feature_studios(api, document_path).get(studio_name, None)


def _extract_studios(
    elements: list[dict],
    instance_path: InstancePath,
) -> dict[str, LocalFeatureStudio]:
    """Constructs a list of FeatureStudios from a list of elements returned by a get documents request."""
    return dict(
        (
            element["name"],
            LocalFeatureStudio(
                element["name"],
                ElementPath.from_path(instance_path, element["id"]),
                element["microversionId"],
                False,
            ),
        )
        for element in elements
    )
