from hmac import new
from math import e, log
from typing import Iterable
import flask
from requests import get

from backend.common.backend_exceptions import require_permissions

from backend.common import connect, database
from backend.endpoints.linked_documents import (
    LinkType,
    db_id_to_path,
    get_linked_documents,
    make_document,
    path_to_db_id,
)
from onshape_api.api.api_base import Api
from onshape_api.endpoints.permissions import Permission

from flask import current_app
from onshape_api.endpoints import documents, versions
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import ElementPath, InstancePath
from onshape_api.utils.str_utils import parens

router = flask.Blueprint("references", __name__)


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
    db = database.Database()
    api = connect.get_api(db)
    instance_path = connect.get_route_instance_path()
    require_permissions(api, instance_path, Permission.WRITE)
    child_document_ids = connect.get_body_optional("childDocumentIds")
    if child_document_ids != None:
        for document_id in child_document_ids:
            require_permissions(api, document_id, Permission.LINK)

    updated_elements = do_update_references(api, instance_path, child_document_ids)
    return {"updatedElements": updated_elements}


def do_update_references(
    api: Api,
    instance_path: InstancePath,
    child_document_ids: Iterable[str] | None = None,
):
    """Updates all references from elements in instance_path to any document with child_document_ids to point to the latest version of that reference."""
    refs = documents.get_external_references(api, instance_path)
    external_refs: dict = refs["elementExternalReferences"]

    # Maps documentIds to their latest versionId
    latest_versions: list = refs["latestVersions"]
    latest_version_dict = {}
    for latest_version in latest_versions:
        latest_version_dict[latest_version["documentId"]] = latest_version["id"]

    # For some reason running this asynchronously causes problems...
    updated_elements = 0
    for element_id, paths in external_refs.items():
        target_path = ElementPath.from_path(instance_path, element_id)
        for path in paths:
            if not path["isOutOfDate"]:
                continue
            document_id = path["documentId"]
            if child_document_ids != None:
                if document_id not in child_document_ids:
                    continue
            # References are always to external versions
            current_instance_path = InstancePath(
                document_id, path["id"], InstanceType.VERSION
            )

            reference_updates = []
            for referenced_element in path["referencedElements"]:
                # Runs once for each tab and each outdated document reference
                current_path = ElementPath.from_path(
                    current_instance_path, referenced_element
                )
                update = documents.VersionUpdate(
                    current_path, latest_version_dict[current_path.document_id]
                )
                reference_updates.append(update)

            # Sometimes externalReferences returns invalid data/updates?
            try:
                documents.update_references(api, target_path, reference_updates)
                updated_elements += 1
            except:
                current_app.logger.warning("Failed to update references")
                continue

    return updated_elements


@router.post("/push-version" + connect.instance_route())
def push_version(**kwargs):
    """Creates a version, then pushes that new version to all instancesToUpdate.

    Args:
        name: The name of the version to create.
        description: The description to create.
        instancesToUpdate: A list of workspace instance objects {documentId, instanceId} to push the version to.

    Returns:
        updatedReferences: The number of tabs which had references updated.
    """
    db = database.Database()
    api = connect.get_api(db)
    curr_instance = connect.get_route_instance_path()
    require_permissions(api, curr_instance, Permission.WRITE, Permission.LINK)
    name = connect.get_body("name")
    description = connect.get_body_optional("description", "")
    body = connect.get_body("instancesToUpdate")
    instances_to_update = [
        InstancePath(temp["documentId"], temp["instanceId"]) for temp in body
    ]
    for instance in instances_to_update:
        require_permissions(api, instance, Permission.WRITE)

    versions.create_version(api, curr_instance, name, description)

    updated_references = 0
    for update_instance in instances_to_update:
        updated_references += do_update_references(
            api, update_instance, [curr_instance.document_id]
        )

    return {"updatedReferences": updated_references}


@router.post("/push-version-recursive" + connect.instance_route())
def push_version_recursive(**kwargs):
    """Creates a version, then pushes that new version to all instancesToUpdate.

    Args:
        name: The name of the version to create.
        description: The description to create.
        instancesToUpdate: A list of workspace instance objects {documentId, instanceId} to push the version to.

    Returns:
        updatedReferences: The number of tabs which had references updated.
    """
    db = database.Database()
    api = connect.get_api(db)
    curr_instance = connect.get_route_instance_path()
    require_permissions(api, curr_instance, Permission.WRITE, Permission.LINK)
    name = connect.get_body("name")
    description = connect.get_body_optional("description", "")
    def get_linked_parents(db, instance, mock_db):
        if instance in mock_db:
            return mock_db[instance]
        document_db_id = path_to_db_id(instance)
        doc = db.linked_documents.document(document_db_id).get()
        linked_parents = []
        if doc.exists and (data := doc.to_dict()):
            for document_db_id in data.get(LinkType.PARENTS, []):
                linked_parents.append(db_id_to_path(document_db_id))
        mock_db[instance] = linked_parents

        return linked_parents

    unvisited_nodes = []

    sorted_list = []

    route = []
    route.append(curr_instance)

    curr_parents = []

    mock_db = dict()

    while route:
        print(f"Route: {route}")
        print(f"Unvisited Nodes: {unvisited_nodes}")

        curr_parents = get_linked_parents(db, route[-1], mock_db)

        for parent in sorted_list:
            if parent in curr_parents:
                curr_parents.remove(parent)

        if not curr_parents:
            sorted_list.append(route.pop())

        for parent in curr_parents:
            if parent in unvisited_nodes:
                unvisited_nodes.remove(parent)

        unvisited_nodes.extend(curr_parents)

        if curr_parents:
            if unvisited_nodes:
                if unvisited_nodes[-1] in route:
                    raise Exception("Cycle detected")
                route.append(unvisited_nodes.pop())

    sorted_list.reverse()

    with open("backend/endpoints/logfile.txt", "a") as log_file:

        log_file.write("sorted_list begin\n")
        for node in sorted_list:
            log_file.write(f"{documents.get_document(api, node)['name']}\n")
        log_file.write("sorted_list end\n\n")

    for instance in sorted_list:
        require_permissions(api, instance, Permission.WRITE, Permission.LINK)

    versions.create_version(api, curr_instance, name, description)

    updated_references = 0
    for update_instance in sorted_list:

        updated_references += do_update_references(
            api, update_instance, [doc.document_id for doc in sorted_list]
        )
        versions.create_version(api, update_instance, name, description)

    with open("backend/endpoints/logfile.txt", "a") as log_file:

        log_file.write("updated_references begin\n")
        log_file.write(f"{updated_references}\n")
        log_file.write("updated_references end\n\n")

    return {"updatedReferences": updated_references}
