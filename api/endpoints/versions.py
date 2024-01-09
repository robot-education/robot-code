from api import api_base, api_path


def get_versions(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
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
        api_path.api_path("documents", document_path.to_document_base(), "versions"),
        query={offset: offset, limit: limit},
    )


def get_latest_version(api: api_base.Api, document_path: api_path.DocumentPath) -> dict:
    return get_versions(api, document_path)[-1]


def create_version(
    api: api_base.Api,
    document_path: api_path.DocumentPath,
    version_name: str,
    description: str,
) -> dict:
    """Creates a new version of a document."""
    body = {
        "name": version_name,
        "description": description,
        "documentId": document_path.document_id,
    }
    # Allows versioning from different parts of the tree
    if document_path.workspace_or_version == "w":
        body["workspaceId"] = document_path.workspace_id
    elif document_path.workspace_or_version == "v":
        body["versionId"] = document_path.workspace_id
    else:
        body["microversionId"] = document_path.workspace_id
    return api.post(
        api_path.api_path("documents", document_path.to_document_base(), "versions"),
        body=body,
    )