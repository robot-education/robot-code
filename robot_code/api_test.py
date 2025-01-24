from onshape_api.api.key_api import make_key_api
from onshape_api.endpoints.documents import (
    copy_workspace,
    delete_document,
    move_elements,
)
from onshape_api.endpoints.metadata import (
    get_all_element_metadata,
)
from onshape_api.endpoints.part_studios import create_part_studio
from onshape_api.paths.paths import (
    InstancePath,
    url_to_element_path,
)


def main():
    api = make_key_api()
    base_path = url_to_element_path(
        "https://cad.onshape.com/documents/769b556baf61d32b18813fd0/w/e6d6c2b3a472b97a7e352949/e/899a4f9046e8da767dc1cce0"
    )
    # target_path = url_to_instance_path(
    #     "https://cad.onshape.com/documents/22f3d6ea902783d4d2edb393/w/f73628fa0b8129209cec904f"
    # )
    # element_ids = ["603682cd3599b82984c10f91", "20580f6d94f32b6d5345b30a"]
    # input_title = "Test Project"

    # print(get_references(api, ElementPath.from_path(base_path, element_ids[1])))

    return

    # path = ElementPath.from_path(base_path, element_ids[0])
    # update_element_metadata(api, path)

    copy = copy_workspace(api, base_path, "TEMP COPY")
    copy_path = InstancePath(copy["newDocumentId"], copy["newWorkspaceId"])

    metadata_map = get_all_element_metadata(api, copy_path)
    elements_to_copy = []
    for item in metadata_map["items"]:
        title = next(
            (
                property["value"]
                for property in item["properties"]
                if property["name"] == "Title 1"
            ),
            None,
        )
        if title == input_title:
            elements_to_copy.append(item["elementId"])

    # Perform the move
    # references = get_internal_references(api, base_path)
    # element_ids = (reference["id"] for reference in references)
    create_part_studio(api, copy_path, "TEMP")
    move_elements(api, copy_path, elements_to_copy, target_path, "TEST_VERSION")

    delete_document(api, copy_path)
    print("Deleted document?")

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
