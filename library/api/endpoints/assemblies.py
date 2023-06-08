from library.api import api_base, api_path

import numpy as np


def get_assembly(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    include_non_solids: bool = False,
    include_mate_features: bool = False,
    exclude_suppressed: bool = True,
) -> dict:
    return api.get(
        api_path.api_path("assemblies", assembly_path),
        query={
            "includeMateFeatures": include_mate_features,
            "includeNonSolids": include_non_solids,
            "excludeSuppressed": exclude_suppressed,
        },
    )


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
    return api.post(
        api_path.api_path("assemblies", assembly_path, "instances"),
        body={
            "documentId": part_studio_path.path.document_id,
            "workspaceId": part_studio_path.path.workspace_id,
            "elementId": part_studio_path.element_id,
            "includePartTypes": ["PARTS"],
            "isWholePartStudio": True,
        },
    )


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


zero_transform = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
move = [
    [1, 0, 0, 0],
    [0, 1, 0, 0],
    [0, 0, 1, 0],
    [0.05, 0.1, 0.3, 1],
]


def fix_instance(
    api: api_base.Api, assembly_path: api_path.ElementPath, instance_id: str
) -> dict:
    """Fixes an instance in an assembly.

    Args:
        assembly_path: The path of the assembly.
    """

    # return api.post(
    #     api_path.api_path("assemblies", assembly_path, "transformedinstances"),
    #     body={
    #         "transformGroups": [
    #             {
    #                 "instances": [
    #                     {
    #                         "path": [instance_id],
    #                         "rootOccurrence": False,
    #                         "isHidden": True,
    #                         "isFixed": True,
    #                         "fix": True,
    #                         "fixed": True,
    #                     }
    #                 ],
    #                 "transform": zero_transform,
    #             }
    #         ],
    #     },
    # )
    transform = np.reshape(np.array(zero_transform), (4, 4))
    transform = np.matmul(transform, np.array(move))
    transform = transform.flatten().tolist()
    print(transform)
    return api.post(
        api_path.api_path("assemblies", assembly_path, "occurrencetransforms"),
        body={
            "isRelative": True,
            "occurrences": [{"fullPathAsString": instance_id, "path": [instance_id]}],
            "transform": transform,
        },
    )


def add_feature(
    api: api_base.Api, assembly_path: api_path.ElementPath, feature: dict
) -> dict:
    return api.post(
        api_path.api_path("assemblies", assembly_path, "features"),
        body={"feature": feature},
    )
