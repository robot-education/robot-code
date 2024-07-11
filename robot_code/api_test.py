from featurescript.conf import Config
from onshape_api.api.key_api import make_key_api
from onshape_api.endpoints import documents, assemblies, users, versions


def main():
    api = make_key_api()
    users.ping(api)

    config = Config()
    target = config.documents["target"]
    base = config.documents["base"]

    document = documents.get_document_elements(api, target)
    target_path = None

    base_document = documents.get_document_elements(api, base)
    base_path = None

    # base_path.instance_id = versions.get_latest_version(api, base_path)["id"]

    # assemblies.add_parts_to_assembly(api, target_path, base_path)

    # ref = documents.get_external_references(api, target_path)[
    #     "elementExternalReferences"
    # ][target_path.element_id]
    # for path in ref:
    #     print(path)
    #     if not path["isOutOfDate"]:
    #         continue
    #     current_path = path.ElementPath(
    #         path.DocumentPath(path["documentId"], path["id"], "v"),
    #         path["referencedElements"][0],
    #     )
    #     documents.update_to_latest_reference(api, target_path, current_path)


if __name__ == "__main__":
    main()
