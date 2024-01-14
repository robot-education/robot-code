from typing import Iterable
from urllib import parse

from api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.instance_type import (
    assert_workspace,
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
    part_studio_path: ElementPath | PartPath,
    # part_id: str | None = None,
) -> None:
    """Adds a part studio to a given assembly.

    If the part_studio_path is an ElementPath, the entire part studio is added. Otherwise, only the specified part is added.

    """
    assert_workspace(assembly_path)
    is_part_path = isinstance(part_studio_path, PartPath)
    body = {
        "documentId": part_studio_path.document_id,
        "elementId": part_studio_path.element_id,
        "includePartTypes": ["PARTS"],
        "isWholePartStudio": not is_part_path,
    }
    if is_part_path:
        body["partId"] = part_studio_path.part_id

    body[get_wmv_key(part_studio_path)] = part_studio_path.instance_id
    api.post(api_path("assemblies", assembly_path, ElementPath, "instances"), body=body)


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
            "features/featureId",
            end_id=feature_id,
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
            "features/featureId",
            end_id=feature_id,
        )
    )
