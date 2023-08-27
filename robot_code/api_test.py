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


def add_mate(api, assembly_path) -> None:
    fasten_mate = assembly_features.fasten_mate("My fasten", [])

    fasten_result = assemblies.add_feature(api, assembly_path, fasten_mate)
    fasten_id = fasten_result["feature"]["featureId"]

    mate_connector = assembly_features.implicit_mate_connector(
        assembly_features.ORIGIN_QUERY
    )
    fasten_mate["mateConnectors"] = [mate_connector]

    fasten_update = assemblies.add_feature(
        api, assembly_path, fasten_mate, feature_id=fasten_id
    )

    new_fasten_mate = fasten_update["feature"]
    mate_connector_id = new_fasten_mate["mateConnectors"][0]["featureId"]
    mate_query = assembly_features.feature_query(mate_connector_id)

    new_fasten_mate["parameters"][1]["queries"].append(mate_query)
    assemblies.add_feature(api, assembly_path, new_fasten_mate)


def main():
    api = api_base.ApiKey(logging=True)
    config = conf.Config()
    backend = config.documents["backend"]

    document = documents.get_document_elements(api, backend)
    assembly_path = document["Assembly 1"]

    mate_id = "A" * 17
    mate_connector = assembly_features.implicit_mate_connector(
        assembly_features.ORIGIN_QUERY
    )
    mate_connector["featureId"] = mate_id

    fasten_mate = assembly_features.fasten_mate(
        "My fasten",
        [assembly_features.feature_query(mate_id)],
        mate_connectors=[mate_connector],
    )

    fasten_result = assemblies.add_feature(api, assembly_path, fasten_mate)


if __name__ == "__main__":
    main()
