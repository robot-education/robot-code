from library.api import api_base, api_path


def make_assembly(
    api: api_base.Api, document_path: api_path.DocumentPath, assembly_name: str
) -> object:
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
) -> object:
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
) -> object:
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
