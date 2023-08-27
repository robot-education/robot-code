from library.api import api_base, conf
from library.api.endpoints import documents, assemblies, assembly_features


# def add_part_studio_to_assembly(
#     api: api_base.Api,
#     assembly_path: api_path.ElementPath,
#     part_studio_path: api_path.ElementPath,
# ) -> dict:
#     """Adds a part studio to a given assembly.

#     Note the response may be malformed due to a (reported) bug with the Onshape API.
#     """
#     return api.post(
#         api_path.api_path("assemblies", assembly_path, "instances"),
#         body={
#             "documentId": part_studio_path.path.document_id,
#             "workspaceId": part_studio_path.path.workspace_id,
#             "elementId": part_studio_path.element_id,
#             "includePartTypes": ["PARTS"],
#             "isWholePartStudio": True,
#         },
#     )


def main():
    api = api_base.ApiKey(logging=True)
    config = conf.Config()
    backend = config.documents["backend"]

    document = documents.get_document_elements(api, backend)
    assembly_path = document["Assembly 1"]

    mate_connector = assembly_features.implicit_mate_connector(
        assembly_features.ORIGIN_QUERY
    )

    fasten_mate = (
        assembly_features.FastenMateBuilder("My fasten")
        .add_mate_connector(mate_connector)
        .build()
    )

    assemblies.add_feature(api, assembly_path, fasten_mate)


if __name__ == "__main__":
    main()
