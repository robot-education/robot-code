"""Building a feature in a new Part Studio, as Onshape would: `build_feature` runs it in a context with the default
geometry (the origin and the Top, Front, and Right planes), and returns what it made, or why it failed."""

from __future__ import annotations

import dataclasses

from fs_eval.geometry.model import Body, Model


class BuildError(Exception):
    pass


@dataclasses.dataclass
class BuiltPart:
    model: Model

    @property
    def parts(self) -> list[Body]:
        """Its parts: the solids which aren't sketches or construction."""
        return [b for b in self.model.bodies.values() if b.body_type == "SOLID" and b.sketch is None and not b.construction]

    @property
    def mate_connectors(self) -> list[Body]:
        return [b for b in self.model.bodies.values() if b.body_type == "MATE_CONNECTOR"]

    def attributes(self, tid: str) -> list:
        return [value for entity, _name, value in self.model.attributes if entity == tid]


def build_feature(evaluator, module: str, feature: str, definition: dict[str, str]) -> BuiltPart:
    """Runs `feature` (exported by `module`, a path in the code folder) with `definition`'s parameters, each a
    FeatureScript expression (like `"FastenerSize.NO_10_32"`, `"true"`, or `"1 * inch"`). Raises `BuildError` with
    the feature's error if it fails."""
    from fs_eval import FSError

    entries = ", ".join(f'"{key}" : {value}' for key, value in definition.items())
    context = evaluator.eval("newContextWithDefaults()")
    try:
        evaluator.eval(f'{feature}(context, makeId("{feature}"), {{ {entries} }})', module, context=context)
    except FSError as e:
        raise BuildError(str(e)) from e
    model: Model = context.model
    status = model.statuses.get((feature,))
    if status is not None and status.get("statusType") == "ERROR":
        message = status.get("statusMsg") or status.get("statusEnum") or "the feature failed"
        if model.errors:
            message = f"{message} ({model.errors[-1]})"
        raise BuildError(str(message))
    return BuiltPart(model)
