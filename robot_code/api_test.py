from library.api import api_base, api_path, conf
from library.api.endpoint import assemblies, documents


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
            "fixed": True,
            "isFixed": True,
        },
    )


def main():
    api = api_base.ApiKey(logging=True)
    config = conf.Config()
    backend = config.get_document("backend")
    if backend == None:
        raise ValueError("Failed to find backend?")

    document = documents.get_document_elements(api, backend)
    assembly_path = document["My Assembly"]
    instance_id = "MBDXz6k5DYz61pvFY"
    # part_studio_path = document["Part Studio 10"]
    # add_part_studio_to_assembly(api, assembly_path, part_studio_path)

    assemblies.fix_instance(api, assembly_path, instance_id)


if __name__ == "__main__":
    main()
