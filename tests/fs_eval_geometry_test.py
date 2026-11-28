"""Tests of the evaluator's geometry (fs_eval.geometry): operations, queries, and feature rollback, on FeatureScript
written here (so they don't depend on what's in the features). Skipped without OpenCascade (`uv sync --group render`)."""

import math
import pathlib

import pytest

pytest.importorskip("OCP", reason="building parts needs OpenCascade: uv sync --group render")

from fs_eval import Evaluator, FSError  # noqa: E402
from fs_eval.geometry import BuildError, build_feature  # noqa: E402

STD = pathlib.Path(__file__).parents[1] / "std"
CONFIG = """[tool.fs]
path = "code"
std = "{std}"
documents.backend = {{ role = "backend", url = "https://cad.onshape.com/documents/d/w/w" }}
"""

FEATURES = """FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

annotation { "Feature Type Name" : "Ring" }
export const ring = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Outer" }
        isLength(definition.outer, LENGTH_BOUNDS);
        annotation { "Name" : "Inner" }
        isLength(definition.inner, LENGTH_BOUNDS);
        annotation { "Name" : "Inner loops" }
        definition.filter is boolean;
    }
    {
        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : XY_PLANE });
        skCircle(sketch, "outer", { "center" : vector(0, 0) * meter, "radius" : definition.outer });
        skCircle(sketch, "inner", { "center" : vector(0, 0) * meter, "radius" : definition.inner });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(id + "sketch", definition.filter),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 1 * millimeter
                });
        opDeleteBodies(context, id + "delete", { "entities" : qCreatedBy(id + "sketch", EntityType.BODY) });
        setProperty(context, { "entities" : qCreatedBy(id + "extrude", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Ring" });
    });

annotation { "Feature Type Name" : "Drilled block" }
export const drilledBlock = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : XY_PLANE });
        skRectangle(sketch, "block", { "firstCorner" : vector(-10, -10) * millimeter, "secondCorner" : vector(10, 10) * millimeter });
        skSolve(sketch);
        opExtrude(context, id + "block", {
                    "entities" : qSketchRegion(id + "sketch"),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 5 * millimeter
                });
        // A pin revolved through it, from a profile in the XZ plane, then cut out
        const profile = newSketchOnPlane(context, id + "profile", { "sketchPlane" : plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION) });
        skPolyline(profile, "pin", { "points" : [vector(0, -1) * millimeter, vector(3, -1) * millimeter, vector(3, 6) * millimeter,
                    vector(0, 6) * millimeter, vector(0, -1) * millimeter] });
        skSolve(profile);
        opRevolve(context, id + "pin", {
                    "entities" : qSketchRegion(id + "profile"),
                    "axis" : line(WORLD_ORIGIN, Z_DIRECTION),
                    "angleForward" : 2 * PI * radian
                });
        opBoolean(context, id + "drill", {
                    "tools" : qCreatedBy(id + "pin", EntityType.BODY),
                    "targets" : qCreatedBy(id + "block", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        const block = qCreatedBy(id + "block", EntityType.BODY);
        const hole = qOwnedByBody(block, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)->qContainsPoint(vector(3, 0, 2.5) * millimeter);
        if (size(evaluateQuery(context, hole)) != 1)
            throw regenError("Expected the hole's face");
        const cylinder = evSurfaceDefinition(context, { "face" : hole });
        if (abs(cylinder.radius - 3 * millimeter) > 1e-9 * meter)
            throw regenError("Expected a 3 mm radius");
        setAttribute(context, { "entities" : hole, "name" : "hole", "attribute" : { "diameter" : 6 * millimeter } });
        opMateConnector(context, id + "mate", { "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION), "owner" : block });
        opDeleteBodies(context, id + "delete", { "entities" : qSketchFilter(qCreatedBy(id, EntityType.BODY), SketchObject.YES) });
    });

annotation { "Feature Type Name" : "Broken" }
export const broken = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : XY_PLANE });
        skCircle(sketch, "c", { "center" : vector(0, 0) * meter, "radius" : 1 * millimeter });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(id + "sketch"),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 1 * millimeter
                });
        throw regenError("On purpose");
    });
"""


@pytest.fixture(scope="module")
def evaluator(tmp_path_factory):
    root = tmp_path_factory.mktemp("repo")
    (root / "pyproject.toml").write_text(CONFIG.format(std=STD.as_posix()))
    (root / "code").mkdir()
    (root / "code" / "parts.fs").write_text(FEATURES)
    return Evaluator(root, geometry=True)


def volume(body) -> float:
    from OCP.BRepGProp import BRepGProp
    from OCP.GProp import GProp_GProps

    props = GProp_GProps()
    BRepGProp.VolumeProperties_s(body.shape, props)
    return props.Mass()


@pytest.mark.parametrize("filter_inner,expected", [(True, 10**2 - 4**2), (False, 10**2)])
def test_sketch_regions_with_and_without_inner_loops(evaluator, filter_inner, expected):
    # The ring alone without the regions inside its hole; with them, the ring and its middle, fused into a disk
    part = build_feature(evaluator, "parts.fs", "ring", {"outer": "10 * millimeter", "inner": "4 * millimeter",
                                                         "filter": str(filter_inner).lower()})
    [body] = part.parts
    assert volume(body) == pytest.approx(math.pi * expected * 1e-6 * 1e-3, rel=1e-6)
    assert [b.body_type for b in part.model.bodies.values() if b.sketch is not None] == []
    assert body.properties["NAME"] == "Ring"


def test_boolean_queries_evaluations_and_attributes(evaluator):
    part = build_feature(evaluator, "parts.fs", "drilledBlock", {})
    [body] = part.parts
    assert volume(body) == pytest.approx((20 * 20 * 5 - math.pi * 9 * 5) * 1e-9, rel=1e-6)
    # The cut kept the block's identity: created by its extrude
    assert body.created_by == ("drilledBlock", "block")
    [mate] = part.mate_connectors
    assert mate.owner == body.tid
    [attribute] = [value for tid, name, value in part.model.attributes if name == "hole"]
    assert attribute is not None
    # The sketches were deleted
    assert all(b.sketch is None for b in part.model.bodies.values())


def test_a_failing_feature_is_rolled_back_and_reported(evaluator):
    with pytest.raises(BuildError, match="On purpose"):
        build_feature(evaluator, "parts.fs", "broken", {})


def test_operations_without_geometry_are_still_errors(tmp_path):
    (tmp_path / "pyproject.toml").write_text(CONFIG.format(std=STD.as_posix()))
    (tmp_path / "code").mkdir()
    plain = Evaluator(tmp_path)
    with pytest.raises(FSError, match="isn't available in the evaluator"):
        plain.eval('opExtrude(newContext(), newId() + "x", {})')
