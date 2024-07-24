from __future__ import annotations
from abc import ABC, abstractmethod
from concurrent import futures
from typing import Iterable
import flask
import onshape_api
from onshape_api import endpoints

from backend.common import assembly_data, connect, database, evaluate
from onshape_api.endpoints import assemblies

router = flask.Blueprint("assembly-mirror", __name__)


@router.post("/assembly-mirror" + connect.element_route())
def assembly_mirror(**kwargs):
    assembly_path = connect.get_element_path()
    db = database.Database()
    api = connect.get_api(db)
    AssemblyMirror(api, assembly_path).execute()
    return {"message": "Success"}


class AssemblyMirrorCandidate:
    """Represents a candidate for assembly mirror.

    Attributes:
        mate_connectors: A dict mapping mate_ids to a boolean which is True if the mate connector is used and False otherwise.
        fully_used: True if all mate connectors are used.
    """

    def __init__(
        self,
        instance: dict,
        assembly: assembly_data.Assembly,
        assembly_features: assembly_data.AssemblyFeatures,
    ) -> None:
        self.instance = instance
        self.part = assembly.get_part_from_instance(instance)
        self.part_path = assembly.resolve_part_path(instance)
        self.element_path = onshape_api.ElementPath.to_path(self.part_path)
        self.mate_connectors = self._init_mate_connectors(assembly_features)
        self.all_used = all(self.mate_connectors.values())

    def _init_mate_connectors(
        self, assembly_features: assembly_data.AssemblyFeatures
    ) -> dict[str, bool]:
        return dict(
            (
                mate_connector,
                assembly_features.is_mate_connector_used(self.instance, mate_connector),
            )
            for mate_connector in self.part.get("mateConnectors", [])
        )


class AssemblyMirrorPart(ABC):
    """Represents a part which assembly mirror is being applied to."""

    def __init__(self, part_path: onshape_api.PartPath) -> None:
        self.part_path = part_path

    def add_to_assembly(
        self, api: onshape_api.Api, assembly_path: onshape_api.ElementPath
    ) -> None:
        assemblies.add_parts(api, assembly_path, self.part_path)

    def find_match(self, instance_dict: dict[onshape_api.PartPath, dict]) -> dict:
        """Returns the an assembled instance which matches this part."""
        # TODO: Atomicity error handling
        return instance_dict[self.part_path]

    @abstractmethod
    def do_fasten(self) -> None: ...


class OriginMirrorPart(AssemblyMirrorPart):
    """An assembly mirror part which is being fastened to the origin."""

    def __init__(self) -> None:
        pass


class CenterMirrorPart(AssemblyMirrorPart):
    """An assembly mirror part which is being fastened to a copy."""

    def __init__(self) -> None:
        pass


class AssemblyMirror:
    """Represents the execution of a single assembly mirror operation."""

    def __init__(
        self, api: onshape_api.Api, assembly_path: onshape_api.ElementPath
    ) -> None:
        self.api = api
        self.path = assembly_path

    def execute(self) -> None:
        self._init_assemblies()
        candidates = self._get_candidates()
        part_studios = self._collect_part_studios(candidates)
        evaluate_result = evaluate.evaluate_assembly_mirror_parts(
            self.api, part_studios
        )
        self._create_assembly_mirror_parts(candidates, *evaluate_result)

    def _init_assemblies(self) -> None:
        with futures.ThreadPoolExecutor(2) as executor:
            assembly_future = executor.submit(
                assembly_data.assembly, self.api, self.path
            )
            assembly_features_future = executor.submit(
                assembly_data.assembly_features, self.api, self.path
            )
        self.assembly: assembly_data.Assembly = assembly_future.result()
        self.assembly_features: assembly_data.AssemblyFeatures = (
            assembly_features_future.result()
        )

    def _make_candidate(self, instance: dict) -> AssemblyMirrorCandidate:
        return AssemblyMirrorCandidate(instance, self.assembly, self.assembly_features)
        # part = self.assembly.get_part_from_instance(instance)
        # part_path = self.assembly.resolve_part_path(instance)
        # return AssemblyMirrorCandidate(
        #     instance, part, part_path, self._make_mate_connectors(instance, part)
        # )

    def _get_candidates(self) -> list[AssemblyMirrorCandidate]:
        """Collects a list of candidates which can potentially be mirrored."""
        return [
            self._make_candidate(instance)
            for instance in self.assembly.get_instances()
            if instance.get("type") == "Part"
        ]

    def _collect_part_studios(
        self, candidates: Iterable[AssemblyMirrorCandidate]
    ) -> Iterable[onshape_api.ElementPath]:
        """Maps candidates into a set of unique part studios.

        Candidates without any unused mate connectors are first filtered out.
        """
        return set(
            candidate.element_path for candidate in candidates if not candidate.all_used
        )

    def _has_used_origin_mate(
        self, candidate: AssemblyMirrorCandidate, origin_base_mates: set[str]
    ) -> bool:
        """Returns True if the candidate has a used origin base mate."""
        intersections = origin_base_mates.intersection(candidate.mate_connectors)
        return any(
            candidate.mate_connectors[intersection] for intersection in intersections
        )

    def _collect_used_origin_paths(
        self,
        candidates: Iterable[AssemblyMirrorCandidate],
        origin_base_mates: set[str],
    ) -> set[onshape_api.PartPath]:
        """Returns a set of part_paths representing parts which are ineligible for origin mirroring."""
        return set(
            candidate.part_path
            for candidate in candidates
            if self._has_used_origin_mate(candidate, origin_base_mates)
        )

    def _is_eligible_for_origin_mirror(
        self,
        candidate: AssemblyMirrorCandidate,
        used_origin_paths: set[onshape_api.PartPath],
        origin_base_mates: set[str],
    ) -> bool:
        if candidate.part_path in used_origin_paths:
            return False
        origin_mate_intersection = origin_base_mates.intersection(
            candidate.mate_connectors.keys()
        )
        return len(origin_mate_intersection) >= 1

    def _is_elibible_for_center_mirror(
        self, candidate: AssemblyMirrorCandidate, base_to_target_mates: dict[str, str]
    ) -> bool:
        """Returns True if the candidate is eligible for assembly mirror.

        Formally, this means the candidate has two unused mates matching a key-value pair in base_to_target_mates.
        """
        return any(
            not candidate.mate_connectors.get(base_mate_id, True)
            and not candidate.mate_connectors.get(target_mate_id, True)
            for base_mate_id, target_mate_id in base_to_target_mates
        )

    def _create_assembly_mirror_parts(
        self,
        candidates: list[AssemblyMirrorCandidate],
        base_to_target_mates: dict[str, str],
        origin_base_mates: set[str],
    ) -> list[dict]:
        """Forms the list of assembly mirror candidates into a list of AssemblyMirrorParts to be assembled.

        Notably, candidates are first trimmed according to the following logic:
        1. For an origin candidate, the instance is trimmed if there already exists some other instance of the part in the assembly
        which is fastened to the origin.
        2. For a regular candidate, the instance is trimmed if either mate is already used (which handles both base and mirrored instances).
        """
        used_origin_paths = self._collect_used_origin_paths(
            candidates, origin_base_mates
        )

        for candidate in candidates:
            # Problem: these predicates don't say what mate_ids to use
            if self._is_eligible_for_origin_mirror(
                candidate, used_origin_paths, origin_base_mates
            ):
                # Handle origin mirror - duplicate part?
                # Generate a matcher function and store the mate_ids, I guess
                continue
            elif self._is_elibible_for_center_mirror(candidate, base_to_target_mates):
                # Handle standard mirror
                continue

        return []
