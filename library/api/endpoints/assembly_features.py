from typing import Iterable


def part_studio_mate_connector_query(instance_id: str, mate_id: str) -> dict:
    return {
        "btType": "BTMPartStudioMateConnectorQuery-1324",
        "featureId": mate_id,
    }


def occurrence_query(instance_id: str) -> dict:
    """A query for a specific instance."""
    return {"btType": "BTMIndividualOccurrenceQuery-626", "path": [instance_id]}


def feature_query(feature_id: str, query_data: str = "") -> dict:
    return {
        "btType": "BTMFeatureQueryWithOccurrence-157",
        "queryData": query_data,
        "featureId": feature_id,
    }


ORIGIN_QUERY = feature_query("Origin", "ORIGIN_Z")


def fasten_mate(
    name: str,
    queries: Iterable[dict],
    mate_connectors: Iterable[dict] | None = None
    # instance_id: str,
    # mate_id: str,
    # target_instance_id: str,
    # target_mate_id: str,
) -> dict:
    """A fasten mate.

    Args:
        queries: A tuple of two queries to use. Note Onshape has a tendency to preserve the location of the second query in cases where neither query is constrained.
    """
    fasten_mate = {
        "btType": "BTMMate-64",
        "featureType": "mate",
        "name": name,
        "parameters": [
            mate_type_parameter("FASTENED"),
            query_parameter("mateConnectorsQuery", queries),
            primary_axis_parameter("primaryAxisAlignment"),
        ],
    }
    if mate_connectors:
        fasten_mate["mateConnectors"] = mate_connectors
    return fasten_mate


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


def group_mate(name: str, queries: Iterable[dict]) -> dict:
    """A group mate."""
    return {
        "btType": "BTMMateGroup-65",
        "name": name,
        "featureType": "mateGroup",
        "parameters": [query_parameter("occurrencesQuery", queries)],
    }


def mate_connector(name: str, originQuery: dict, implicit: bool = False) -> dict:
    """A mate connector feature."""

    return {
        "btType": "BTMMateConnector-66",
        "name": name,
        "implicit": implicit,
        "parameters": [
            {
                "btType": "BTMParameterEnum-145",
                "value": "ON_ENTITY",
                "enumName": "Origin type",
                "parameterId": "originType",
            },
            query_parameter("originQuery", [originQuery]),
        ],
    }


def implicit_mate_connector(originQuery: dict) -> dict:
    """A mate connector which is owned by another mate."""
    return mate_connector("Mate connector", originQuery, implicit=True)
