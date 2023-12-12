from library.api import api_base, conf
from library.api.endpoints import documents, assemblies, assembly_features


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
