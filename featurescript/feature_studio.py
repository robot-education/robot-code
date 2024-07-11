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
    name: str
    path: ElementPath
    microversion_id: str
    modified: bool = False
    generated: bool = False

    def create(self, api: Api) -> dict:
        """Creates this FeatureStudio in Onshape."""
        return feature_studios.create_feature_studio(api, self.path, self.name)

    def push(self, api: Api, code: str) -> dict:
        """Pushes this Studio to Onshape. The studio is created if it does not already exist."""
        # onshape_studio = get_feature_studio(api, self.path, self.name)
        element = documents.get_document_element(api, self.path)
        if element == None:
            self.create(api)
        return feature_studios.push_code(api, self.path, code)


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
