from typing import Iterable
from urllib import parse

from onshape_api.api.api_base import Api
from onshape_api.assertions import assert_workspace
from onshape_api.paths.api_path import api_path
from onshape_api.utils.endpoint_utils import (
    get_wmv_key,
)
from onshape_api.paths.paths import ElementPath, InstancePath, PartPath


def get_assembly(
    api: Api,
    assembly_path: ElementPath,
    include_non_solids: bool = False,
    include_mate_features: bool = False,
    include_mate_connectors: bool = False,
    exclude_suppressed: bool = True,
) -> dict:
    """Retrieves information about an assembly."""
    return api.get(
        api_path("assemblies", assembly_path, ElementPath),
        query={
            "includeMateFeatures": include_mate_features,
            "includeNonSolids": include_non_solids,
            "excludeSuppressed": exclude_suppressed,
            "includeMateConnectors": include_mate_connectors,
        },
    )


def get_assembly_features(
    api: Api,
    assembly_path: ElementPath,
    feature_ids: Iterable[str] = [],
) -> dict:
    """Returns features in an assembly.

    Args:
        feature_ids: Feature ids to retrieve. If omitted, all features are returned.
    """
    query = parse.urlencode({"featureId": feature_ids}, doseq=True)
    return api.get(
        api_path("assemblies", assembly_path, ElementPath, "features"), query=query
    )


def create_assembly(api: Api, workspace_path: InstancePath, assembly_name: str) -> dict:
    """Constructs an assembly with the given name."""
    assert_workspace(workspace_path)
    return api.post(
        api_path("assemblies", workspace_path, InstancePath),
        body={"name": assembly_name},
    )


def add_parts_to_assembly(
    api: Api,
    assembly_path: ElementPath,
    part_studio_path: ElementPath,
    part_id: str | None = None,
) -> None:
    """Adds a part studio to a given assembly.

    If the part_studio_path is an ElementPath, the entire part studio is added. Otherwise, only the specified part is added.

    This endpoint has no response since Onshape doesn't give one.
    """
    assert_workspace(assembly_path)
    body = {
        "documentId": part_studio_path.document_id,
        "elementId": part_studio_path.element_id,
        "includePartTypes": ["PARTS"],
        "isWholePartStudio": part_id is None,
    }
    body[get_wmv_key(part_studio_path)] = part_studio_path.instance_id
    if part_id:
        body["partId"] = part_id

    api.post(api_path("assemblies", assembly_path, ElementPath, "instances"), body=body)


def add_part_to_assembly(
    api: Api,
    assembly_path: ElementPath,
    part_path: PartPath,
) -> None:
    add_parts_to_assembly(api, assembly_path, part_path, part_path.part_id)


def transform_instance(
    api: Api,
    assembly_path: ElementPath,
    instance_id: str,
    transform: list[int | float],
    is_relative: bool = False,
):
    """
    Args:
        is_relative: True to apply the transform relative to the instance's existing location, False to apply it relative to the assembly origin.
    """
    assert_workspace(assembly_path)
    return api.post(
        api_path("assemblies", assembly_path, ElementPath, "occurrencetransforms"),
        body={
            "isRelative": is_relative,
            "occurrences": [{"path": [instance_id]}],
            "transform": transform,
        },
    )


def add_feature(
    api: Api,
    assembly_path: ElementPath,
    feature: dict,
    feature_id: str | None = None,
) -> dict:
    """
    Args:
        feature_id: If specified, the given feature is updated rather than being created.
    """
    assert_workspace(assembly_path)
    return api.post(
        api_path(
            "assemblies",
            assembly_path,
            ElementPath,
            "features",
            feature_id=feature_id,
        ),
        body={"feature": feature},
    )


def delete_feature(api: Api, assembly_path: ElementPath, feature_id: str) -> dict:
    """Deletes a feature from an assembly."""
    assert_workspace(assembly_path)
    return api.delete(
        api_path(
            "assemblies",
            assembly_path,
            ElementPath,
            "features",
            feature_id=feature_id,
        )
    )
