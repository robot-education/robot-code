from concurrent import futures
import pathlib
import dataclasses
from typing import Callable, Generator, Iterable

from flask import current_app as app
from flask import request

from library.api import api_base, api_path
from library.api.endpoints import assemblies, assembly_features, part_studios


def execute():
    auth = request.headers.get("Authentication", None)
    if auth == None:
        return {"error": "An onshape oauth token is required."}
    token = auth.removeprefix("Basic").strip()

    body = request.get_json()
    if body == None:
        return {"error": "A request body is required."}

    api = api_base.ApiToken(token, logging=False)

    assembly_path = api_path.make_element_path_from_obj(body)
    document_path = assembly_path.path

    with futures.ThreadPoolExecutor(2) as executor:
        assembly_future = executor.submit(
            assemblies.get_assembly,
            api,
            assembly_path,
            include_mate_features=True,
            include_mate_connectors=True,
        )
        assembly_features_future = executor.submit(
            assemblies.get_assembly_features, api, assembly_path
        )

    assembly = assembly_future.result()
    parts = assembly["parts"]
    part_studio_paths = extract_part_studios(parts, document_path)
    parts_to_mates = get_parts_to_mates(assembly, document_path)

    part_maps = evalute_part_studios(api, part_studio_paths)
    targets_to_mate_connectors = evaluate_targets(api, part_maps.mates_to_targets)

    # wait to resolve assembly features
    assembly_features = assembly_features_future.result()
    app.logger.info(assembly_features)

    instances_to_mates = get_instances_to_mates_factory(
        assembly, assembly_features, assembly_path, parts_to_mates
    )
    count = iterate_mate_ids(
        api,
        assembly_path,
        instances_to_mates(),
        try_add_instance,
        part_maps,
        targets_to_mate_connectors,
    )
    updated_assembly = assemblies.get_assembly(
        api, assembly_path, include_mate_connectors=True
    )
    updated_instances: list[dict] = updated_assembly["rootAssembly"]["instances"]
    new_instances = updated_instances[-count:]
    iterate_mate_ids(
        api,
        assembly_path,
        instances_to_mates(),
        add_mate,
        part_maps,
        targets_to_mate_connectors,
        new_instances,
    )

    return {"message": "Success"}


def make_path(
    document_path: api_path.DocumentPath, instance: dict
) -> api_path.ElementPath:
    return api_path.ElementPath(document_path, instance["elementId"])


def extract_part_studios(
    parts: list[dict], document_path: api_path.DocumentPath
) -> set[api_path.ElementPath]:
    """Groups parts by part studios.

    Returns a set of part studio paths."""
    return set(make_path(document_path, part) for part in parts)


def get_parts_to_mates(
    assembly: dict, document_path: api_path.DocumentPath
) -> dict[api_path.PartPath, list[str]]:
    """Constructs a mapping of"""
    result = {}
    for part in assembly["parts"]:
        if "mateConnectors" not in part:
            continue
        part_path = api_path.PartPath(make_path(document_path, part), part["partId"])
        for mate_connector in part["mateConnectors"]:
            mate_id = mate_connector["featureId"]
            values = result.get(part_path, [])
            values.append(mate_id)
            result[part_path] = values
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
        threads = [
            executor.submit(evalute_part, api, part_studio_path)
            for part_studio_path in part_studio_paths
        ]

        part_maps = PartMaps()
        for future in futures.as_completed(threads):
            result = future.result()
            if not result["valid"]:
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


def evaluate_targets(
    api: api_base.Api, mates_to_targets: dict[str, api_path.ElementPath]
) -> dict[str, str]:
    """TODO: Deduplicate target part studios."""
    with futures.ThreadPoolExecutor() as executor:
        threads = {
            executor.submit(evalute_target, api, part_studio_path): target_mate_id
            for target_mate_id, part_studio_path in mates_to_targets.items()
        }

        targets_to_mate_connectors = {}
        for future in futures.as_completed(threads):
            result = future.result()
            target_mate_id = threads[future]
            targets_to_mate_connectors[target_mate_id] = result["targetMateId"]
        return targets_to_mate_connectors


def evalute_target(api: api_base.Api, assembly_path: api_path.ElementPath) -> dict:
    with pathlib.Path("backend/scripts/parseTarget.fs").open() as file:
        return part_studios.evaluate_feature_script(api, assembly_path, file.read())


def get_instances_to_mates_factory(
    assembly: dict,
    assembly_features: dict,
    assembly_path: api_path.ElementPath,
    parts_to_mates: dict[api_path.PartPath, list[str]],
) -> Callable[[], Generator[tuple[dict, str], None, None]]:
    """Returns a factory which returns a mapping of instances to their mate ids."""

    def gen():
        for instance in assembly["rootAssembly"]["instances"]:
            mate_ids = get_part_mate_ids(instance, assembly_path.path, parts_to_mates)
            for mate_id in mate_ids:
                if is_mate_unused(instance, mate_id, assembly_features):
                    yield (instance, mate_id)

    return gen


def is_mate_unused(instance: dict, mate_id: str, assembly_features: dict) -> bool:
    """Returns true if the specified mate connector on the given part_path isn't used in any mate features.

    Procedure:
        Iterate over features.
        For each fastened mate, iterate over its queries. For each mate connector query, if its feature id equals our mate_id, the mate is used.

    Note this procedure isn't very performant since assembly_features is iterated over many times (and each iteration is also slow).
    Speed could be improved by first collecting all mate_ids and then using the above technique alongside a set of all mate ids.
    """
    for feature in assembly_features["features"]:
        app.logger.info(feature)
        if is_fastened_mate(feature):
            queries = get_query_parameter(feature)
            app.logger.info(queries)
            if any(
                query["featureId"] == mate_id and query["path"][0] == instance["id"]
                for query in queries
            ):
                app.logger.info("Used mate")
                return False
    return True


def is_fastened_mate(feature: dict) -> bool:
    if feature.get("featureType", None) != "mate":
        return False
    app.logger.info("mate")
    for parameter in feature["parameters"]:
        if parameter["parameterId"] == "mateType":
            app.logger.info("mateType")
            if parameter["value"] != "FASTENED":
                return False
            else:
                break
    return True


def get_query_parameter(feature: dict) -> list[dict]:
    for parameter in feature["parameters"]:
        if parameter["parameterId"] == "mateConnectorsQuery":
            return parameter["queries"]
    return []


def get_part_mate_ids(
    instance: dict,
    document_path: api_path.DocumentPath,
    part_to_mates: dict[api_path.PartPath, list[str]],
) -> list[str]:
    """Fetches the mate ids of an instance.

    Returns a list of the mate ids on the instance.
    Returns [] if the instance isn't a valid part or doesn't have any mates.
    """
    if instance["type"] != "Part":
        return []
    part_path = api_path.PartPath(
        make_path(document_path, instance), instance["partId"]
    )
    return part_to_mates.get(part_path, [])


def iterate_mate_ids(
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    instances_to_mates: Iterable[tuple[dict, str]],
    fn: Callable[..., futures.Future | None],
    *args
) -> int:
    """Iterates over instances_to_mates, calling fn on each.

    Args:
        fn: A function taking an executor, an api, an assembly_path, an instance, a mate id, and *args (in that order),
          and which returns a Future or None.

    Returns the number of created threads.
    """
    with futures.ThreadPoolExecutor() as executor:
        threads = []
        for instance, mate_id in instances_to_mates:
            thread = fn(executor, api, assembly_path, instance, mate_id, *args)
            if thread is not None:
                threads.append(thread)

    futures.as_completed(threads)
    return len(threads)


def try_add_instance(
    executor: futures.ThreadPoolExecutor,
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    instance: dict,
    mate_id: str,
    part_maps: PartMaps,
    targets_to_mate_connectors: dict[str, str],
) -> futures.Future | None:
    if mate_id in part_maps.mates_to_targets and mate_id in targets_to_mate_connectors:
        return executor.submit(
            assemblies.add_part_studio_to_assembly,
            api,
            assembly_path,
            part_maps.mates_to_targets[mate_id],
        )
    elif mate_id in part_maps.mirror_mates or mate_id in part_maps.origin_mirror_mates:
        part_studio_path = make_path(assembly_path.path, instance)
        return executor.submit(
            assemblies.add_part_to_assembly,
            api,
            assembly_path,
            part_studio_path,
            instance["partId"],
        )
    return None


def add_mate(
    executor: futures.ThreadPoolExecutor,
    api: api_base.Api,
    assembly_path: api_path.ElementPath,
    instance: dict,
    mate_id: str,
    part_maps: PartMaps,
    targets_to_mate_connectors: dict[str, str],
    new_instances: list[dict],
) -> futures.Future | None:
    if mate_id in part_maps.mates_to_targets and mate_id in targets_to_mate_connectors:
        target_path = part_maps.mates_to_targets[mate_id]
        target_mate_connector = targets_to_mate_connectors[mate_id]
        new_instance = find_new_instance(new_instances, assembly_path.path, target_path)
        queries = (
            assembly_features.part_studio_mate_connector_query(
                new_instance["id"], target_mate_connector
            ),
            assembly_features.part_studio_mate_connector_query(instance["id"], mate_id),
        )
        feature = assembly_features.fasten_mate("Fasten mate", queries)
        return executor.submit(assemblies.add_feature, api, assembly_path, feature)
    elif mate_id in part_maps.mirror_mates:
        start_mate_id = part_maps.mirror_mates[mate_id]
        target_path = make_path(assembly_path.path, instance)
        new_instance = find_new_instance(new_instances, assembly_path.path, target_path)
        queries = (
            assembly_features.part_studio_mate_connector_query(
                new_instance["id"], start_mate_id
            ),
            assembly_features.part_studio_mate_connector_query(instance["id"], mate_id),
        )
        feature = assembly_features.fasten_mate("Mirror mate", queries)
    elif mate_id in part_maps.origin_mirror_mates:
        queries = ({}, {})
        feature = assembly_features.fasten_mate("Mirror mate", queries)
    else:
        return None

    return executor.submit(assemblies.add_feature, api, assembly_path, feature)


def find_new_instance(
    new_instances: list[dict],
    document_path: api_path.DocumentPath,
    target_path: api_path.ElementPath,
) -> dict:
    for i, new_instance in enumerate(new_instances):
        new_path = make_path(document_path, new_instance)
        if new_path == target_path:
            return new_instances.pop(i)
    raise ValueError("Failed to find added instance.")
