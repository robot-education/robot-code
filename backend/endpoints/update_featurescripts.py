import asyncio
import re
from typing import Callable
import flask

from backend.common.backend_exceptions import require_permissions
from backend.common import connect, database
from onshape_api.api.api_base import Api
from onshape_api.endpoints.documents import ElementType, get_document_elements
from onshape_api.endpoints.feature_studios import pull_code, push_code
from onshape_api.endpoints.permissions import Permission
from onshape_api.endpoints.std_versions import get_latest_std_version
from onshape_api.paths.paths import ElementPath

router = flask.Blueprint("update-featurescripts", __name__)


@router.get("/latest-std-version")
def latest_std_version(*args, **kwargs):
    db = database.Database()
    api = connect.get_api(db)
    return {"stdVersion": get_latest_std_version(api)}


@router.post("/update-featurescript-version" + connect.instance_route("w"))
async def update_references(*args, **kwargs):
    """Updates the versions of all standard library imports in a given document to the latest library version.

    Args:
        stdVersion: The std version to update to, e.g. "123".

    Returns:
        updatedStudios: The number of Feature Studios which were modified.
    """
    db = database.Database()
    api = connect.get_api(db)
    instance_path = connect.get_route_instance_path("w")
    require_permissions(api, instance_path, Permission.WRITE)
    std_version = connect.get_body_arg("stdVersion")

    elements = get_document_elements(api, instance_path, ElementType.FEATURE_STUDIO)

    feature_studio_paths: list[ElementPath] = []
    tasks: list[asyncio.Task[str]] = []
    # Pull Feature Studios asynchronously to improve performance
    async with asyncio.TaskGroup() as task_group:
        for studio in elements:
            studio_path = ElementPath.from_path(instance_path, studio["id"])
            feature_studio_paths.append(studio_path)
            tasks.append(task_group.create_task(pull_code_async(api, studio_path)))

    # We can't push studios asynchronously since Onshape doesn't handle the overlapping calls very well
    updated_studios = 0
    for studio_path, task in zip(feature_studio_paths, tasks):
        if update_feature_studio(api, studio_path, task.result(), std_version):
            updated_studios += 1

    return {"updatedStudios": updated_studios}


OUTDATED_VERSION_MATCH: re.Pattern[str] = re.compile(
    r'version : "(\d{2,7})\.0"|FeatureScript (\d{2,7});'
)

VERSION_SUB_MATCH = re.compile(r"\d{2,7}")


async def pull_code_async(api: Api, feature_studio_path: ElementPath):
    return pull_code(api, feature_studio_path)


def update_feature_studio(
    api: Api, studio_path: ElementPath, code: str, std_version: str
) -> bool:
    updated_code = update_std_versions(code, std_version)
    if code == updated_code:
        return False
    push_code(api, studio_path, updated_code)
    return True


def update_std_versions(code: str, std_version: str) -> str:
    return re.sub(
        pattern=OUTDATED_VERSION_MATCH,
        repl=generate_update_replace(std_version),
        string=code,
    )


def generate_update_replace(std_version: str) -> Callable[[re.Match], str]:
    def replace_number(match: re.Match) -> str:
        return re.sub(
            pattern=VERSION_SUB_MATCH, repl=std_version, string=match.group(0)
        )

    return replace_number
