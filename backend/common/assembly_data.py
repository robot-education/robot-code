from __future__ import annotations
from typing import Any, Iterable
from api import api_path, api_base
from api.endpoints import assemblies


class Assembly:
    """Contains information from Onshape about an assembly."""

    def __init__(self, assembly_data: dict, path: api_path.ElementPath) -> None:
        self.assembly_data = assembly_data
        self.path = path

    def get_parts(self) -> list:
        return self.assembly_data.get("parts", [])

    def get_part_from_instance(self, instance: dict) -> dict:
        """Returns the part corresponding to a given instance.

        The instance must be for a part.
        """
        return next(
            part
            for part in self.get_parts()
            if are_keys_equal(
                instance,
                part,
                "partId",
                "configuration",
                "documentId",
                "elementId",
                "documentVersion",
            )
        )

    def get_instances(self) -> list[dict]:
        return self.assembly_data["rootAssembly"].get("instances", [])

    def extract_unique_part_studios(self) -> set[api_path.ElementPath]:
        """Constructs a set of unique part studio paths in the assembly."""
        return set(self.resolve_path(part) for part in self.get_parts())

    def get_part_paths_to_mate_ids(self) -> dict[api_path.PartPath, list[str]]:
        """Constructs a dict which maps part paths to a list of the unique mate ids owned by each part."""

        result = {}
        for part in self.get_parts():
            part_path = api_path.PartPath(self.resolve_path(part), part["partId"])
            for mate_connector in part.get("mateConnectors", []):
                mate_id = mate_connector["featureId"]
                values = result.get(part_path, [])
                values.append(mate_id)
                result[part_path] = values
        return result

    def resolve_part_path(self, instance_or_part: dict) -> api_path.PartPath:
        """Constructs a part path to a given instance or part.

        Args:
            instance_or_part: An instance or part in an assembly.
        """
        return api_path.PartPath(
            self.resolve_path(instance_or_part), instance_or_part["partId"]
        )

    def resolve_path(self, instance_or_part: dict) -> api_path.ElementPath:
        """Constructs an element path from a given instance or a part.

        Args:
            instance_or_part: An instance or part in an assembly.
        """
        if "documentVersion" in instance_or_part:
            return api_path.ElementPath(
                api_path.DocumentPath(
                    instance_or_part["documentId"],
                    instance_or_part["documentVersion"],
                    "v",
                ),
                instance_or_part["elementId"],
            )
        return api_path.ElementPath(
            api_path.DocumentPath(
                instance_or_part["documentId"], self.path.workspace_id, "w"
            ),
            instance_or_part["elementId"],
        )


def assembly(api: api_base.Api, assembly_path: api_path.ElementPath) -> Assembly:
    assembly_data = assemblies.get_assembly(
        api, assembly_path, include_mate_features=True, include_mate_connectors=True
    )
    return Assembly(assembly_data, assembly_path)


def are_keys_equal(first: dict, second: dict, *keys: Iterable[str]) -> bool:
    """Returns true if the value of each key in first and second are equal."""
    return all(first.get(key, None) == second.get(key, None) for key in keys)


class AssemblyFeatures:
    def __init__(self, features: dict) -> None:
        self.features = features

    def get_features(self) -> list[dict]:
        return self.features.get("features", [])

    def get_fastened_mates(self) -> Iterable[dict]:
        for feature in self.get_features():
            if is_fastened_mate(feature):
                yield feature

    def is_mate_connector_used(self, instance: dict, mate_id: str) -> bool:
        """Returns true if the mate connector is already used in a fastened mate feature."""
        # We could remove O(N^2) algo by returning a dict mapping instance ids to used mate connectors
        for feature in self.get_fastened_mates():
            queries = get_parameter(feature, "mateConnectorsQuery")
            if any(
                query["featureId"] == mate_id and query["path"][0] == instance["id"]
                for query in queries
            ):
                return True
        return False


def is_fastened_mate(feature: dict) -> bool:
    """Returns true if feature is a fastened mate."""
    if feature.get("featureType", None) != "mate":
        return False
    mate_type = get_parameter(feature, "mateType")
    return mate_type != None and mate_type.get("value", None) == "FASTENED"


def get_parameter(feature: dict, parameter_id: str) -> Any:
    for parameter in feature.get("parameters", []):
        if parameter.get("parameterId", None) == parameter_id:
            return parameter
    raise ValueError("Failed to find parameter {}".format(parameter_id))


def assembly_features(
    api: api_base.Api, assembly_path: api_path.ElementPath
) -> AssemblyFeatures:
    assembly_data = assemblies.get_assembly_features(api, assembly_path)
    return AssemblyFeatures(assembly_data)
