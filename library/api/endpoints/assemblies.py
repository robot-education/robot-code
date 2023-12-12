from typing import Iterable
from library.api import api_base, api_path
from urllib import parse


def get_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    include_non_solids: bool = False,
    include_mate_features: bool = False,
    include_mate_connectors: bool = False,
    exclude_suppressed: bool = True,
) -> dict:
    """Retrieves information about an assembly."""
    return api.get(
        api_path.api_path("assemblies", assembly_path),
        query={
            "includeMateFeatures": include_mate_features,
            "includeNonSolids": include_non_solids,
            "excludeSuppressed": exclude_suppressed,
            "includeMateConnectors": include_mate_connectors,
        },
    )


def get_assembly_features(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    feature_ids: Iterable[str] = [],
) -> dict:
    """Returns features in an assembly.

    Args:
        feature_ids: Feature ids to retrieve. If omitted, all features are returned.
    """
    query = parse.urlencode({"featureId": feature_ids}, doseq=True)
    return api.get(api_path.api_path("assemblies", assembly_path, "features"), query)


def make_assembly(
    api: api_base.Api, document_path: api_path.DocumentPath, assembly_name: str
) -> dict:
    """Constructs an assembly with the given name."""
    return api.post(
        api_path.api_path("assemblies", document_path), body={"name": assembly_name}
    )


def add_parts_to_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    part_studio_path: api_path.ElementPath,
    part_id: str | None = None,
) -> dict:
    """Adds a part studio to a given assembly.

    Note the response may be malformed due to a (reported) bug with the Onshape API.

    Args:
        part_id: If it is included, only the specified part is added, rather than the entire part studio.
    """
    result = api.post(
        api_path.api_path("assemblies", assembly_path, "instances"),
        body={
            "documentId": part_studio_path.document_id,
            "workspaceId": part_studio_path.workspace_id,
            "elementId": part_studio_path.element_id,
            "includePartTypes": ["PARTS"],
            "isWholePartStudio": (part_id == None),
            "partId": part_id,
        },
    )
    return result


def add_part_to_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    part_path: api_path.PartPath,
) -> dict:
    """Adds a part to a given assembly.

    Note the response may be malformed due to a (reported) bug with the Onshape API.
    """
    result = api.post(
        api_path.api_path("assemblies", assembly_path, "instances"),
        body={
            "documentId": part_path.document_id,
            "workspaceId": part_path.workspace_id,
            "elementId": part_path.element_id,
            "partId": part_path.part_id,
            "includePartTypes": ["PARTS"],
            "isWholePartStudio": False,
        },
    )
    return result


IDENTITY_TRANSFORM = [
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
]


def transform_instance(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    instance_id: str,
    transform: list[int | float],
    is_relative: bool = False,
):
    """
    Args:
        is_relative: True to apply the transform relative to the instance's existing location. False to apply it relative to the origin.
    """
    return api.post(
        api_path.api_path("assemblies", assembly_path, "occurrencetransforms"),
        body={
            "isRelative": is_relative,
            "occurrences": [{"path": [instance_id]}],
            "transform": transform,
        },
    )


def add_feature(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    feature: dict,
    feature_id: str | None = None,
) -> dict:
    """
    Args:
        feature_id: If specified, the given feature is updated rather than being created.
    """
    return api.post(
        api_path.api_path(
            "assemblies", assembly_path, "features", feature_id=feature_id
        ),
        body={"feature": feature},
    )


def delete_feature(
    api: api_base.Api, assembly_path: api_path.ElementPath, feature_id: str
) -> dict:
    return api.delete(
        api_path.api_path(
            "assemblies", assembly_path, "features", feature_id=feature_id
        )
    )
