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
        "type": 64,
        "version": 2,
        "message": {
            "featureType": "mate",
            "name": name,
            "parameters": [
                mate_type_parameter("FASTENED"),
                query_parameter("mateConnectorsQuery", queries),
                primary_axis_parameter("primaryAxisAlignment"),
            ],
        },
    }


def query_parameter(parameter_id: str, queries: Iterable[dict]) -> dict:
    return {
        "type": 67,
        "message": {"parameterId": parameter_id, "queries": queries},
    }


def mate_type_parameter(value: str) -> dict:
    return {
        "type": 145,
        "message": {"parameterId": "mateType", "value": value, "enumName": "Mate type"},
    }


def primary_axis_parameter(parameter_id: str, value: bool = False) -> dict:
    return {
        "type": 144,
        "message": {"parameterId": parameter_id, "value": value},
    }


def individual_occurrence_query(instance_id: str) -> dict:
    return {"type": 626, "message": {"path": [instance_id]}}


def part_studio_mate_connector_query(instance_id: str, mate_id: str) -> dict:
    return {"type": 1324, "message": {"featureId": mate_id, "path": [instance_id]}}


def group_mate(name: str, queries: Iterable[dict]) -> dict:
    """A group mate."""
    return {
        "type": 65,
        "version": 2,
        "message": {
            "name": name,
            "featureType": "mateGroup",
            "parameters": [query_parameter("occurrencesQuery", queries)],
        },
    }
