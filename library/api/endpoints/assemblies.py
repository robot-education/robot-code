from flask import current_app as app
from library.api import api_base, api_path

import numpy as np


def get_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    include_non_solids: bool = False,
    include_mate_features: bool = False,
    include_mate_connectors: bool = False,
    exclude_suppressed: bool = True,
) -> dict:
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
) -> dict:
    return api.get(api_path.api_path("assemblies", assembly_path, "features"))


def make_assembly(
    api: api_base.Api, document_path: api_path.DocumentPath, assembly_name: str
) -> dict:
    """Constructs an assembly with the given name.

    Returns the response from Onshape.
    """
    return api.post(
        api_path.api_path("assemblies", document_path), body={"name": assembly_name}
    )


def add_part_studio_to_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    part_studio_path: api_path.ElementPath,
) -> dict:
    """Adds a part studio to a given assembly.

    Note the response may be malformed due to a (reported) bug with the Onshape API.
    """
    result = api.post(
        api_path.api_path("assemblies", assembly_path, "instances"),
        body={
            "documentId": part_studio_path.path.document_id,
            "workspaceId": part_studio_path.path.workspace_id,
            "elementId": part_studio_path.element_id,
            "includePartTypes": ["PARTS"],
            "isWholePartStudio": True,
        },
    )
    app.logger.info(result)
    return result


def add_part_to_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    part_studio_path: api_path.ElementPath,
    part_id: str,
) -> dict:
    """Adds a part to a given assembly.

    Note the response may be malformed due to a (reported) bug with the Onshape API.
    """
    return api.post(
        api_path.api_path("assemblies", assembly_path, "instances"),
        body={
            "documentId": part_studio_path.path.document_id,
            "workspaceId": part_studio_path.path.workspace_id,
            "elementId": part_studio_path.element_id,
            "includePartTypes": ["PARTS"],
            "isWholePartStudio": False,
            "partId": part_id,
        },
    )


zero_transform = [
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

linear_transform = [
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
    0.5,
    0.5,
    0.5,
    1.0,
]

test = [
    0.0,
    1.0,
    0.0,
    0,
    -1.0,
    0.0,
    0.0,
    1.0,
    0.0,
    0.0,
    1.0,
    0,
    0,
    0,
    0,
    1.0,
]


def fix_instance(
    api: api_base.Api, assembly_path: api_path.ElementPath, instance_id: str
) -> dict:
    """Fixes an instance in an assembly.

    Args:
        assembly_path: The path of the assembly.
    """
    return api.post(
        api_path.api_path("assemblies", assembly_path, "modify"),
        body={"unsuppressInstances": [instance_id]},
    )
    # return api.post(
    #     api_path.api_path("assemblies", assembly_path, "occurrencetransforms"),
    #     body={
    #         "isRelative": False,
    #         "occurrences": [{"path": [instance_id]}],
    #         # "transform": (np.matmul(np.identity(4)).flatten().tolist(),
    #     },
    # )


def transform_instance(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    instance_id: str,
    transform: list[int | float],
    is_relative: bool = False,
):
    """
    Args:
        is_relative: True to apply the transform relative to the instance's existing location, False to apply it relative to the origin.
    """
    return api.post(
        api_path.api_path("assemblies", assembly_path, "occurrencetransforms"),
        body={
            "isRelative": is_relative,
            "occurrences": [{"path": [instance_id]}],
            "transform": test,
        },
    )


def add_feature(
    api: api_base.Api, assembly_path: api_path.ElementPath, feature: dict
) -> dict:
    return api.post(
        api_path.api_path("assemblies", assembly_path, "features"),
        body={"feature": feature},
    )
