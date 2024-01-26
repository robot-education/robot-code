from typing import Iterable
import flask

import onshape_api
from onshape_api import endpoints

from backend.common import connect


router = flask.Blueprint("references", __name__)


def update_refs(
    api: onshape_api.Api,
    instance_path: onshape_api.InstancePath,
    child_document_ids: Iterable[str] | None = None,
):
    refs = endpoints.get_external_references(api, instance_path)
    external_refs: dict = refs["elementExternalReferences"]
    latest_versions: list = refs["latestVersions"]
    # Maps documentIds to their latest versionId
    latest_version_dict = {}
    for latest_version in latest_versions:
        latest_version_dict[latest_version["documentId"]] = latest_version["id"]

    # For some reason running this asynchronously causes problems...
    updated_elements = 0
    for element_id, paths in external_refs.items():
        target_path = onshape_api.ElementPath.from_path(instance_path, element_id)
        for path in paths:
            if not path["isOutOfDate"]:
                continue
            document_id = path["documentId"]
            if child_document_ids is not None and document_id not in child_document_ids:
                continue
            # References are always to external versions
            current_instance_path = onshape_api.InstancePath(
                document_id, path["id"], onshape_api.InstanceType.VERSION
            )
            made_update = False
            for referenced_element in path["referencedElements"]:
                # Runs once for each tab and each outdated document reference
                current_path = onshape_api.ElementPath.from_path(
                    current_instance_path, referenced_element
                )
                # Sometimes externalReferences returns invalid data?
                try:
                    endpoints.update_reference(
                        api,
                        target_path,
                        current_path,
                        latest_version_dict[current_path.document_id],
                    )
                    made_update = True
                except:
                    continue

            if made_update:
                updated_elements += 1
    return updated_elements


@router.post("/update-references" + connect.instance_route())
def update_references(*args, **kwargs):
    """Updates references in a given document.

    Args:
        childDocumentIds: A list of documentIds to look for when deciding what to update.
        If included, only references stemming from the specified documents will be updated.
        Otherwise, all outdated references will be updated.

    Returns:
        updatedElements: The number of tabs which had old references that were updated.
    """
    api = connect.get_api()
    instance_path = connect.get_instance_path()
    child_document_ids = connect.get_optional_body("childDocumentIds")
    updated_elements = update_refs(api, instance_path, child_document_ids)
    return {"updatedElements": updated_elements}


@router.post("/push-version" + connect.instance_route())
def push_version(**kwargs):
    """Updates references in a given document.

    Args:
        name: The name of the version to create.
        description: The description to create.
        instancesToUpdate: A list of workspace instances (documentId, instanceId) to push the version to.

    Returns:
        updatedReferences: The number of tabs which had references updated.
    """
    api = connect.get_api()
    curr_instance = connect.get_instance_path()
    name = connect.get_body("name")
    description = connect.get_optional_body("description") or ""
    body = connect.get_body("instancesToUpdate")
    instances_to_update = [
        onshape_api.InstancePath(temp["documentId"], temp["instanceId"])
        for temp in body
    ]

    endpoints.create_version(api, curr_instance, name, description)

    updated_references = 0
    for update_instance in instances_to_update:
        updated_references += update_refs(
            api, update_instance, [curr_instance.document_id]
        )

    return {"updatedReferences": updated_references}
