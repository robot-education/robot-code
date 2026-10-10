"""Tests of Robot lighten's walls' sketch (`sketchWalls`), recorded by the evaluator: the capsules around a face's
edges, whose regions closer to the edges than the wall are cut from the pocket."""

import math

import pytest

from fs_eval.pytest_plugin import shared_evaluator
from fs_eval.sketch import recorded_sketch

WIDTH = 0.003


def walls(curves: str, square_ends: str = "[]"):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    evaluator.eval(
        "(function() { const sketch = newSketchOnPlane(context, newId() + \"walls\", { \"sketchPlane\" : XY_PLANE });"
        f" sketchWalls(sketch, {curves}, {WIDTH} * meter, {square_ends}); skSolve(sketch); }})()",
        "lighten/robotLighten.fs",
        context=context,
    )
    return recorded_sketch(context, "walls")


def mm(x: float, y: float) -> str:
    return f"vector({x}, {y}) * millimeter"


def test_line_capsule():
    sketch = walls(f'[{{ "kind" : "line", "points" : [{mm(0, 0)}, {mm(20, 0)}] }}]')
    construction = [curve for curve in sketch.curves if curve.construction]
    outline = [curve for curve in sketch.curves if not curve.construction]
    # The edge itself, to tell the walls' regions by
    assert [(curve.kind, curve.start, curve.end) for curve in construction] == [("line", (0.0, 0.0), (0.02, 0.0))]
    # Its sides, a wall's width to each side, and a round end at each end
    sides = sorted(curve.start[1] for curve in outline if curve.kind == "line")
    assert sides == pytest.approx([-WIDTH, WIDTH])
    circles = [curve for curve in outline if curve.kind == "circle"]
    assert sorted(circle.center for circle in circles) == [(0.0, 0.0), (0.02, 0.0)]
    assert all(circle.radius == pytest.approx(WIDTH) for circle in circles)


def test_ends_where_edges_meet_are_rounded_once_and_square_ends_are_straight():
    curves = (
        f'[{{ "kind" : "line", "points" : [{mm(0, 0)}, {mm(20, 0)}] }},'
        f' {{ "kind" : "line", "points" : [{mm(20, 0)}, {mm(20, 20)}] }}]'
    )
    sketch = walls(curves, f"[{mm(20, 20)}]")
    circles = sorted(curve.center for curve in sketch.curves if curve.kind == "circle")
    # The corner's circle is drawn once; the end at a face to ignore gets a straight end instead
    assert circles == [(0.0, 0.0), (0.02, 0.0)]
    square = [curve for curve in sketch.curves if curve.kind == "line" and "square" in curve.id]
    assert len(square) == 1
    assert [square[0].start[1], square[0].end[1]] == pytest.approx([0.02, 0.02])
    assert sorted((square[0].start[0], square[0].end[0])) == pytest.approx([0.02 - WIDTH, 0.02 + WIDTH])


def test_arcs_and_circles():
    # A hole's circle: its outer circle, and inner one only when it's bigger than the wall
    big = walls(f'[{{ "kind" : "circle", "center" : {mm(0, 0)}, "radius" : 10 * millimeter }}]')
    assert sorted(curve.radius for curve in big.curves if not curve.construction) == pytest.approx([0.007, 0.013])
    small = walls(f'[{{ "kind" : "circle", "center" : {mm(0, 0)}, "radius" : 2 * millimeter }}]')
    assert [curve.radius for curve in small.curves if not curve.construction] == [pytest.approx(0.005)]
    # A small arc is a wedge to its center: no inner arc, and sides from the center
    corner = f'{{ "kind" : "arc", "center" : {mm(0, 0)}, "radius" : 2 * millimeter, "points" : [{mm(2, 0)}, {mm(math.sqrt(2), math.sqrt(2))}, {mm(0, 2)}] }}'
    wedge = walls(f"[{corner}]")
    arcs = [curve for curve in wedge.curves if curve.kind == "arc" and not curve.construction]
    assert [arc.radius for arc in arcs] == [pytest.approx(0.005)]
    sides = [curve for curve in wedge.curves if curve.kind == "line"]
    assert len(sides) == 2 and all(side.start == pytest.approx((0.0, 0.0)) for side in sides)


def test_polyline_bends_are_rounded():
    sketch = walls(f'[{{ "kind" : "polyline", "points" : [{mm(0, 0)}, {mm(10, 0)}, {mm(20, 5)}] }}]')
    centers = sorted(curve.center for curve in sketch.curves if curve.kind == "circle")
    assert centers == pytest.approx([(0.0, 0.0), (0.01, 0.0), (0.02, 0.005)])
    assert len([curve for curve in sketch.curves if curve.kind == "line" and curve.construction]) == 2
