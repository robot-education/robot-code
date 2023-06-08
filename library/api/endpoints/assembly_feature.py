def fasten_mate(
    name: str,
    instance_id: str,
    mate_id: str,
    target_instance_id: str,
    target_mate_id: str,
) -> dict:
    return {
        "type": 64,
        "version": 2,
        "message": {
            "featureType": "mate",
            "name": name,
            "parameters": [
                mate_type_parameter("FASTENED"),
                query_parameter(
                    "mateConnectorsQuery",
                    [
                        part_studio_mate_connector_query(
                            target_instance_id, target_mate_id
                        ),
                        part_studio_mate_connector_query(instance_id, mate_id),
                    ],
                ),
                primary_axis_parameter(),
            ],
        },
    }


def query_parameter(parameter_id: str, queries: list[dict]) -> dict:
    return {
        "type": 67,
        "message": {"parameterId": parameter_id, "queries": queries},
    }


def mate_type_parameter(value: str) -> dict:
    return {
        "type": 145,
        "message": {"parameterId": "mateType", "value": value, "enumName": "Mate type"},
    }


def primary_axis_parameter() -> dict:
    return {
        "type": 144,
        "message": {"parameterId": "primaryAxisAlignment", "value": "false"},
    }


def individual_occurrence_query(instance_id: str) -> dict:
    return {"type": 626, "message": {"path": [instance_id]}}


def part_studio_mate_connector_query(instance_id: str, mate_id: str) -> dict:
    return {"type": 1324, "message": {"featureId": mate_id, "path": [instance_id]}}


def group_mate(name: str, queries: list[dict]) -> dict:
    return {
        "type": 65,
        "version": 2,
        "message": {
            "name": name,
            "featureType": "mateGroup",
            "parameters": [query_parameter("occurrencesQuery", queries)],
        },
    }
