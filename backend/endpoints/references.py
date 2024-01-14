from typing import Iterable
import flask

import onshape_api
from onshape_api import endpoints

from backend.common import setup


router = flask.Blueprint("references", __name__)


def update_refs(
    api: onshape_api.Api,
    instance_path: onshape_api.InstancePath,
    child_document_ids: Iterable[str] | None = None,
):
    refs = endpoints.get_external_references(api, instance_path)
    externalRefs = refs["elementExternalReferences"]

    # For some reason running this asynchronously causes problems...
    updated_elements = 0
    for element_id, paths in externalRefs.items():
        target_path = onshape_api.ElementPath.from_path(instance_path, element_id)
        for path in paths:
            flask.current_app.logger.info(str(path))
            if not path["isOutOfDate"]:
                continue
            document_id = path["documentId"]
            if child_document_ids != None and document_id not in child_document_ids:
                continue
            # References are always to external versions
            current_document_path = onshape_api.InstancePath(
                document_id, path["id"], onshape_api.InstanceType.VERSION
            )
            for referenced_element in path["referencedElements"]:
                # Runs once for each tab and each outdated document reference
                current_path = onshape_api.ElementPath.from_path(
                    current_document_path, referenced_element
                )
                endpoints.update_to_latest_reference(api, target_path, current_path)

            updated_elements += 1
    return updated_elements


@router.post("/update-references" + setup.document_route())
def update_references(*args, **kwargs):
    """Updates references in a given document.

    Args:
        childDocumentIds: A list of documentIds to look for when deciding what to update.
        If included, only references stemming from the specified documents will be updated.
        Otherwise, all outdated references will be updated.

    Returns:
        updatedElements: The number of tabs which had old references that were updated.
    """
    api = setup.get_api()
    instance_path = setup.get_instance_path()
    child_document_ids = setup.get_optional_body("childDocumentIds")
    updated_elements = update_refs(api, instance_path, child_document_ids)
    return {"updatedElements": updated_elements}


@router.post("/push-version" + setup.document_route())
def push_version(**kwargs):
    """Updates references in a given document.

    Args:
        name: The name of the version to create.
        description: The description to create.
        instancesToUpdate: A list of workspace instances (documentId, instanceId) to push the version to.

    Returns:
        updatedReferences: The number of tabs which had references updated.
    """
    api = setup.get_api()
    curr_instance = setup.get_instance_path()
    name = setup.get_body("name")
    description = setup.get_body("description")
    body = setup.get_body("instancesToUpdate")
    instances_to_update = [
        onshape_api.InstancePath(temp["documentId"], temp["instanceId"])
        for temp in body
    ]

    endpoints.create_version(api, curr_instance, name, description)["id"]

    updated_references = 0
    for update_instance in instances_to_update:
        updated_references += update_refs(
            api, update_instance, [curr_instance.document_id]
        )

    return {"updatedReferences": updated_references}
