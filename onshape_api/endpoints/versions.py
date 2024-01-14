from onshape_api.paths.api_path import api_path
from api.api_base import Api
from onshape_api.paths.instance_type import get_wmv_key
from onshape_api.paths.paths import DocumentPath, InstancePath


def get_versions(
    api: Api,
    document_path: DocumentPath,
    offset: int = 0,
    limit: int = 0,
) -> list[dict]:
    """Fetches a list of versions of a document.

    Versions are returned in reverse chronological order (oldest - newest).

    Args:
        offset: A starting offset to apply. Does not support negative indexing.
        limit: The max number of versions to return.
    """
    return api.get(
        api_path("documents", document_path, DocumentPath, "versions"),
        query={offset: offset, limit: limit},
    )


def get_latest_version(api: Api, document_path: DocumentPath) -> dict:
    return get_versions(api, document_path)[-1]


def create_version(
    api: Api,
    instance_path: InstancePath,
    version_name: str,
    description: str,
) -> dict:
    """Creates a new version of a document from a given instance."""
    body = {
        "name": version_name,
        "description": description,
        "documentId": instance_path.document_id,
    }
    # Allows versioning from different workspaces/previous versions
    body[get_wmv_key(instance_path)] = instance_path.instance_id
    return api.post(
        api_path("documents", instance_path, DocumentPath, "versions"),
        body=body,
    )
