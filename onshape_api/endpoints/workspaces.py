from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import DocumentPath, InstancePath
from onshape_api.types import MergeInfo, MergeStrategy, WorkspaceInfo


def get_workspaces(api: Api, document_path: DocumentPath) -> list[WorkspaceInfo]:
    """Fetches a document's workspaces (its branches), main included."""
    return api.get(api_path("documents", document_path, DocumentPath, "workspaces"))


def create_workspace(
    api: Api, document_path: DocumentPath, version_id: str, name: str
) -> WorkspaceInfo:
    """Branches a new workspace from a version, as Onshape's UI branches only from versions."""
    return api.post(
        api_path("documents", document_path, DocumentPath, "workspaces"),
        body={"name": name, "versionId": version_id},
    )


def delete_workspace(api: Api, workspace_path: InstancePath) -> None:
    """Deletes a workspace (a branch) and its history; versions made from it stay."""
    api.delete(
        api_path(
            "documents",
            DocumentPath(workspace_path.document_id),
            DocumentPath,
            f"workspaces/{workspace_path.instance_id}",
        )
    )


def merge_into_workspace(
    api: Api,
    target: InstancePath,
    source_workspace_id: str,
    strategy: MergeStrategy = MergeStrategy.MERGE,
) -> MergeInfo:
    """Merges a workspace of the same document into `target`, a workspace.

    Args:
        strategy: For tabs changed in both since they branched: `MERGE` merges Part Studios and assemblies, `REPLACE`
            takes the source's, and `KEEP` the target's.
    """
    return api.post(
        api_path(
            "documents",
            DocumentPath(target.document_id),
            DocumentPath,
            f"workspaces/{target.instance_id}/merge",
            skip_document_d=True,
        ),
        body={
            "type": "workspace",
            "id": source_workspace_id,
            "defaultMergeStrategy": strategy,
        },
    )
