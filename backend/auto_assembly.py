from concurrent import futures
import pathlib
import dataclasses

from flask import current_app as app
from flask import request

from library.api import api_base, api_path
from library.api.endpoints import assemblies, part_studios


def execute():
    token = request.args["token"]
    if token == None:
        return {"error": "An onshape oauth token is required."}

    body = request.get_json()
    if body == None:
        return {"error": "A request body is required."}

    api = api_base.ApiToken(token, logging=True)

    assembly_path = api_path.make_element_path_from_obj(body)
    document_path = assembly_path.path

    assembly = assemblies.get_assembly(
        api, assembly_path, include_non_solids=False, include_mate_features=True
    )
    parts = assembly["parts"]
    part_studio_paths = extract_part_studios(parts, document_path)
    paths_to_mates = get_paths_to_mates_dict(parts, document_path)

    # for part_studio_path in part_studio_paths:

    return {"message": "Success"}


def part_studio_path(
    part: dict, document_path: api_path.DocumentPath
) -> api_path.ElementPath:
    return api_path.ElementPath(document_path, part["elementId"])


def extract_part_studios(
    parts: list[dict], document_path: api_path.DocumentPath
) -> set[api_path.ElementPath]:
    """Groups parts by part studios.

    Returns a set of part studio paths."""
    return set(part_studio_path(part, document_path) for part in parts)


def get_paths_to_mates_dict(
    parts: list[dict], document_path: api_path.DocumentPath
) -> dict[str, list[api_path.ElementPath]]:
    result = {}
    for part in parts:
        if not part["mateConnectors"]:
            continue
        path = part_studio_path(part, document_path)
        for mate_connector in part["mateConnectors"]:
            values = result.get(path, [])
            values.append(mate_connector)
            result[path] = values
    return result


@dataclasses.dataclass
class PartMaps:
    mates_to_targets: dict[str, api_path.ElementPath] = dataclasses.field(
        default_factory=dict
    )
    mirror_mates: dict[str, str] = dataclasses.field(default_factory=dict)
    origin_mirror_mates: set[str] = dataclasses.field(default_factory=set)


def evalute_part_studios(
    api: api_base.Api, part_studio_paths: set[api_path.ElementPath]
):
    with futures.ThreadPoolExecutor() as executor:
        part_maps = PartMaps()
        threads = [
            executor.submit(evalute_part, api, part_studio_path)
            for part_studio_path in part_studio_paths
        ]
        for future in futures.as_completed(threads):
            result = future.result()
            if result["valid"]:
                continue

            for values in result["mates"]:
                part_maps.mates_to_targets[
                    values["mateId"]
                ] = api_path.make_element_path_from_obj(values)

            for values in result["mirrors"]:
                if values["mateToOrigin"]:
                    part_maps.origin_mirror_mates.add(values["endMateId"])
                else:
                    part_maps.mirror_mates[values["endMateId"]] = values["startMateId"]

        return part_maps


def evalute_part(api: api_base.Api, part_studio_path: api_path.ElementPath) -> dict:
    with pathlib.Path("backend/scripts/parseBase.fs").open() as file:
        return part_studios.evaluate_feature_script(api, part_studio_path, file.read())


def evalute_target(api: api_base.Api, part_studio_path: api_path.ElementPath) -> dict:
    with pathlib.Path("backend/scripts/parseTarget.fs").open() as file:
        return part_studios.evaluate_feature_script(api, part_studio_path, file.read())
