from typing import Iterable


def fasten_mate(
    name: str,
    queries: tuple[dict, dict],
    mate_connectors: list[dict] | None = None
    # instance_id: str,
    # mate_id: str,
    # target_instance_id: str,
    # target_mate_id: str,
) -> dict:
    """A fasten mate.

    Args:
        queries: A tuple of two queries to use. Note Onshape has a tendency to preserve the location of the second query in cases where neither query is constrained.
    """
    return {
        "btType": "BTMMate-64",
        "featureType": "mate",
        "name": name,
        "parameters": [
            mate_type_parameter("FASTENED"),
            query_parameter("mateConnectorsQuery", queries),
            primary_axis_parameter("primaryAxisAlignment"),
        ],
    }


def query_parameter(parameter_id: str, queries: Iterable[dict]) -> dict:
    return {
        "btType": "BTMParameterQueryWithOccurrenceList-67",
        "parameterId": parameter_id,
        "queries": queries,
    }


def mate_type_parameter(value: str) -> dict:
    return {
        "btType": "BTMParameterEnum-145",
        "parameterId": "mateType",
        "value": value,
        "enumName": "Mate type",
    }


def primary_axis_parameter(parameter_id: str, value: bool = False) -> dict:
    return {
        "btType": "BTMParameterBoolean-144",
        "parameterId": parameter_id,
        "value": value,
    }


def part_studio_mate_connector_query(instance_id: str, mate_id: str) -> dict:
    return {
        "btType": "BTMPartStudioMateConnectorQuery-1324",
        "featureId": mate_id,
        "path": [instance_id],
    }


def occurrence_query(instance_id: str) -> dict:
    return {"btType": "BTMIndividualOccurrenceQuery-626", "path": [instance_id]}


def group_mate(name: str, queries: Iterable[dict]) -> dict:
    """A group mate."""
    return {
        "btType": "BTMMateGroup-65",
        "name": name,
        "featureType": "mateGroup",
        "parameters": [query_parameter("occurrencesQuery", queries)],
    }
