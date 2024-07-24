from onshape_api.api.key_api import make_key_api
from onshape_api.endpoints import part_studios, users
from onshape_api.endpoints.feature_studios import get_feature_spec
from onshape_api.paths.paths import url_to_element_path
from robot_code.documents import Documents

# import logging

# logging.basicConfig(level=logging.INFO, filename=None)


def main():
    api = make_key_api()
    users.ping(api)

    documents = Documents(test=True)

    test = url_to_element_path(
        "https://cad.onshape.com/documents/9cffa92db8b62219498f89af/v/d1389fa34235160533671706/e/50410223cd7c6fe15bb37171"
    )

    # response = documents.get_document_elements(api, target)
    # document = make_name_to_path_map(response, target)

    # response = documents.get_document_elements(api, base)
    # base_path = make_name_to_path_map(response, base)

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
