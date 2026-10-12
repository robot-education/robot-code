"""Tests of sketches made by FeatureScript, recorded by the evaluator (see fs_eval.sketch): their geometry's
properties, rather than pictures of them."""

import math

import pytest

from fs_eval import FSError, to_python
from fs_eval.pytest_plugin import shared_evaluator
from fs_eval.sketch import recorded_sketch

BELT_TYPES = ["_2_MM_GT2", "_3_MM_GT2", "_3_MM_HTD", "_5_MM_HTD", "RT25"]


def pulley_profile(belt_type: str, teeth: int, offset_mm: float = 0.0):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    evaluator.eval(
        f'sketchPulleyProfile(context, newId() + "profile", XY_PLANE, BeltType.{belt_type}, {teeth}, {offset_mm} * millimeter)',
        "released/belt/robotPulleyCommon.fs",
        context=context,
    )
    return recorded_sketch(context, "profile")


def belt_model(belt_type: str) -> dict:
    info = shared_evaluator().eval(f"getBeltModelInfo(BeltType.{belt_type})", "released/belt/robotBeltCommon.fs")
    return to_python(info)


def pitch_radius(belt_type: str, teeth: int) -> float:
    return to_python(shared_evaluator().eval(
        f"getPulleyRadius(getBeltPitch(BeltType.{belt_type}), {teeth})", "released/belt/robotPulleyCommon.fs"))


@pytest.mark.parametrize("belt_type", BELT_TYPES)
@pytest.mark.parametrize("teeth", [12, 24, 60])
def test_pulley_profile_is_one_smooth_closed_loop(belt_type, teeth):
    sketch = pulley_profile(belt_type, teeth)
    loops = sketch.loops()
    assert len(loops) == 1
    loop = loops[0]
    assert loop.closed
    # Per tooth: two fillets, a groove, and a land
    assert len(loop.curves) == 4 * teeth
    for before, after in loop.joints():
        out, into = before.tangent(True), after.tangent(False)
        assert abs(out[0] * into[1] - out[1] * into[0]) < 1e-6, f"{before.id} and {after.id} aren't tangent"
        assert out[0] * into[0] + out[1] * into[1] > 0


@pytest.mark.parametrize("belt_type", BELT_TYPES)
def test_pulley_profile_stays_between_groove_and_land(belt_type):
    teeth = 24
    sketch = pulley_profile(belt_type, teeth)
    model = belt_model(belt_type)
    pitch = pitch_radius(belt_type, teeth)
    land = pitch - model["insideThickness"]
    groove_bottom = pitch - model["toothOffset"] - model["toothRadius"]
    curves = sketch.loops()[0].curves
    assert max(curve.farthest() for curve in curves) == pytest.approx(land, abs=1e-9)
    assert min(curve.nearest() for curve in curves) == pytest.approx(groove_bottom, abs=1e-9)


def test_profile_offset_grows_the_pulley():
    plain = pulley_profile("_5_MM_HTD", 24)
    offset = pulley_profile("_5_MM_HTD", 24, 0.1)
    farthest = max(curve.farthest() for curve in plain.loops()[0].curves)
    assert max(curve.farthest() for curve in offset.loops()[0].curves) == pytest.approx(farthest + 0.0001, abs=1e-9)


def test_too_few_teeth():
    with pytest.raises(FSError, match="needs more teeth"):
        pulley_profile("_3_MM_GT2", 10)


CHAIN_TYPES = ["ANSI_25", "ANSI_35", "ISO_05B"]


def sprocket_profile(chain_type: str, teeth: int):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    evaluator.eval(
        f'sketchSprocketProfile(context, newId() + "profile", XY_PLANE, ChainType.{chain_type}, {teeth}, 0 * meter)',
        "chain/chainCommon.fs",
        context=context,
    )
    form = to_python(evaluator.eval(f"getSprocketToothForm(ChainType.{chain_type}, {teeth}, 0 * meter)", "chain/chainCommon.fs"))
    return recorded_sketch(context, "profile"), form


@pytest.mark.parametrize("chain_type", CHAIN_TYPES)
@pytest.mark.parametrize("teeth", [6, 9, 16, 30, 100])
def test_sprocket_profile(chain_type, teeth):
    sketch, form = sprocket_profile(chain_type, teeth)
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    curves = loops[0].curves
    # Per tooth: a seating curve, two flanks, and a tip
    assert len(curves) == 4 * teeth
    # Seating curves meet their flanks tangent (flanks meet tips at a corner)
    for before, after in loops[0].joints():
        if "seating" in before.id or "seating" in after.id:
            out, into = before.tangent(True), after.tangent(False)
            assert abs(out[0] * into[1] - out[1] * into[0]) < 1e-6
    assert min(curve.nearest() for curve in curves) == pytest.approx(form["rootRadius"], abs=1e-9)
    assert max(curve.farthest() for curve in curves) <= form["tipRadius"] + 1e-9
    # The rollers sit on the pitch circle, in the seating curves
    seating = [curve for curve in curves if curve.id.startswith("seating")]
    for curve in seating:
        assert math.dist(curve.center, (0, 0)) == pytest.approx(form["pitchRadius"], abs=1e-9)


def test_gobilda_sprocket_pitch_diameter():
    # goBILDA's 14 tooth 8mm sprocket: 35.95 mm pitch diameter
    _, form = sprocket_profile("ISO_05B", 14)
    assert 2 * form["pitchRadius"] * 1000 == pytest.approx(35.95, abs=0.01)


def test_open_belt_profile_is_closed_and_smooth():
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    # From a start, over a pulley, under an idler, and on to an end: the belt turns both ways
    evaluator.eval(
        """sketchOpenPathProfile(context, newId() + "profile", XY_PLANE,
            openPath(vector(0, 0) * inch, [
                    { "location" : vector(3, 2) * inch, "radius" : 1 * inch, "flipped" : true } as BoundaryCircle,
                    { "location" : vector(7, -1) * inch, "radius" : 0.5 * inch, "flipped" : false } as BoundaryCircle
                ], vector(10, 2) * inch),
            0.03 * inch, 0.05 * inch)""",
        "core/loop.fs",
        context=context,
    )
    loops = recorded_sketch(context, "profile").loops()
    assert len(loops) == 1 and loops[0].closed
    for before, after in loops[0].joints():
        if "Cap" in before.id or "Cap" in after.id:
            continue
        out, into = before.tangent(True), after.tangent(False)
        assert abs(out[0] * into[1] - out[1] * into[0]) < 1e-6, f"{before.id} and {after.id} aren't tangent"


def gear_profile(teeth: int, module_mm: float = 1.27, pressure_degrees: float = 20, fillet: str = "standardFilletRadius(m)",
                 sector: str = "undefined"):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    form = f"getGearForm(m, {teeth}, {pressure_degrees} * degree, {fillet}, 0 * meter)"
    m = evaluator.eval(f"{module_mm} * millimeter")
    evaluator.eval(f'sketchGearProfile(context, newId() + "profile", XY_PLANE, {form}, {sector})', "gear/gearCommon.fs", context=context, m=m)
    return recorded_sketch(context, "profile"), to_python(evaluator.eval(form, "gear/gearCommon.fs", m=m))


def involute(angle: float) -> float:
    return math.tan(angle) - angle


@pytest.mark.parametrize("teeth", [8, 12, 24, 60])
@pytest.mark.parametrize("pressure_degrees", [14.5, 20])
def test_gear_profile(teeth, pressure_degrees):
    sketch, form = gear_profile(teeth, pressure_degrees=pressure_degrees)
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    curves = loops[0].curves
    # Arcs and lines meet tangent (splines are checked against the involute below)
    for before, after in loops[0].joints():
        if "spline" not in (before.kind, after.kind):
            out, into = before.tangent(True), after.tangent(False)
            assert abs(out[0] * into[1] - out[1] * into[0]) < 1e-6, f"{before.id} and {after.id} aren't tangent"
    assert max(curve.farthest() for curve in curves) == pytest.approx(form["tipRadius"], abs=1e-9)
    assert min(curve.nearest() for curve in curves) == pytest.approx(form["rootRadius"], abs=1e-9)
    # Every flank point is on its tooth's involute: half the tooth's width at the pitch circle (a quarter of the
    # circular pitch), less how far the involute's turned from there
    alpha = math.radians(pressure_degrees)
    tooth_angle = 2 * math.pi / teeth
    for curve in curves:
        if curve.kind != "spline":
            continue
        for x, y in curve.points:
            radius = math.hypot(x, y)
            angle = math.atan2(y, x)
            # The nearest tooth's center, and how far from it the point is
            offset = abs((angle + tooth_angle / 2) % tooth_angle - tooth_angle / 2)
            expected = math.pi / (2 * teeth) + involute(alpha) - involute(math.acos(min(form["baseRadius"] / radius, 1)))
            assert offset == pytest.approx(expected, abs=1e-9)


def test_sector_gear_closes_through_its_center():
    sketch, form = gear_profile(40, sector="5")
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    assert min(curve.nearest() for curve in loops[0].curves) == pytest.approx(0, abs=1e-12)


def test_router_relief_rounds_roots():
    # A 1/16 in. bit fits a 20 DP gear's gaps as fillets; a 1/8 in. one doesn't fit at all
    sketch, form = gear_profile(24, fillet="1 / 32 * inch")
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    radii = {round(curve.radius, 9) for curve in loops[0].curves if curve.kind == "arc" and "_4" not in curve.id}
    assert round(0.0254 / 32, 9) in radii
    with pytest.raises(FSError, match="doesn't fit between the teeth"):
        gear_profile(14, fillet="1 / 16 * inch")


RACK_FORM = "getGearForm(1 * millimeter, undefined, 20 * degree, 0 * meter, {offset} * millimeter)"


def rack_centers(length_mm: float, gap_mm: float, offset_mm: float = 0.0) -> list[float]:
    form = RACK_FORM.format(offset=offset_mm)
    return to_python(shared_evaluator().eval(
        f"rackToothCenters({form}, {length_mm} * millimeter, {gap_mm} * millimeter)", "gear/gearCommon.fs"))


def rack_cut(face_height: str, length_mm: float, offset_mm: float = 0.0):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    form = RACK_FORM.format(offset=offset_mm)
    centers = f"rackToothCenters({form}, {length_mm} * millimeter, 0 * meter)"
    regions = f"rackCutRegions({form}, {face_height}, {length_mm} * millimeter, {centers})"
    evaluator.eval(f'sketchRackCut(context, newId() + "profile", XY_PLANE, {regions})', "gear/gearCommon.fs", context=context)
    return recorded_sketch(context, "profile")


def test_rack_teeth_fill_the_face_from_a_gap():
    pitch = math.pi * 0.001
    # Ten pitches long, from a gap at the start: ten whole teeth, half a pitch from it
    centers = rack_centers(10 * math.pi, 0)
    assert centers == pytest.approx([(k + 0.5) * pitch for k in range(10)])
    # From the far end, the same teeth
    assert rack_centers(10 * math.pi, 10 * math.pi) == pytest.approx(centers)
    # A gap anywhere along it (or past it) lines them up the same way, a pitch apart
    shifted = rack_centers(10 * math.pi, 0.3 * math.pi + 20 * math.pi)
    assert all(c % pitch == pytest.approx(0.8 * pitch % pitch, abs=1e-12) for c in shifted)
    # Too short for a whole tooth, root and all
    assert rack_centers(2, 0) == []


@pytest.mark.parametrize("face_height", ["1 * millimeter", "0 * meter"])
def test_rack_cut_is_a_region_per_gap_when_teeth_reach_the_face(face_height):
    # At the tips (the outside diameter), or at the pitch line (tips cut off at the face)
    sketch = rack_cut(face_height, 10 * math.pi)
    loops = sketch.loops()
    assert len(loops) == 11 and all(loop.closed for loop in loops)
    points = [point for loop in loops for curve in loop.curves for point in (curve.start, curve.end)]
    height = 0.001 if face_height.startswith("1") else 0
    assert max(y for _, y in points) == pytest.approx(0, abs=1e-12)
    # The root's 1.25 m below the pitch line
    assert min(y for _, y in points) == pytest.approx(-0.00125 - height, abs=1e-12)
    assert min(x for x, _ in points) == pytest.approx(0, abs=1e-12)
    assert max(x for x, _ in points) == pytest.approx(10 * math.pi * 0.001, abs=1e-12)


def test_rack_cut_is_one_region_when_teeth_stop_short_of_the_face():
    # With clearance, the tips are below a face at the outside diameter: it's cut over them too
    sketch = rack_cut("1 * millimeter", 10 * math.pi, offset_mm=-0.1)
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    # A tooth's flanks cross the pitch line half a circular pitch apart, less the clearance's share (the ends'
    # walls, straight up, aren't flanks)
    lines = [line for line in loops[0].curves if abs(line.end[0] - line.start[0]) > 1e-12]
    pitch_y = -0.001
    crossings = sorted(
        line.start[0] + (line.end[0] - line.start[0]) * (pitch_y - line.start[1]) / (line.end[1] - line.start[1])
        for line in lines
        if min(line.start[1], line.end[1]) < pitch_y < max(line.start[1], line.end[1])
    )
    width = math.pi * 0.001 / 2 - 2 * 0.0001 / math.cos(math.radians(20))
    assert crossings[1] - crossings[0] == pytest.approx(width, abs=1e-12)


def test_rack_teeth_too_thin():
    with pytest.raises(FSError, match="come to points"):
        rack_cut("1 * millimeter", 10 * math.pi, offset_mm=-1)


def gear_eval(expression: str):
    return to_python(shared_evaluator().eval(expression, "gear/robotGear.fs"))


@pytest.mark.parametrize("pitch,module_mm,pressure_degrees", [
    ("DP_10", 25.4 / 10, 20),
    ("DP_20", 25.4 / 20, 14.5),
    ("DP_32", 25.4 / 32, 20),
    ("MODULE_0_8", 0.8, 20),
])
def test_standard_pitches(pitch, module_mm, pressure_degrees):
    definition = f'{{ "pitch" : GearPitch.{pitch} }}'
    assert gear_eval(f"gearModule({definition}) / millimeter") == pytest.approx(module_mm)
    assert gear_eval(f"pressureAngle({definition}) / degree") == pytest.approx(pressure_degrees)


def test_custom_pitch_by_unit_system():
    imperial = '{ "pitch" : GearPitch.CUSTOM, "unitSystem" : UnitSystem.IMPERIAL, "diametralPitch" : 24, "pressureAngle" : 25 * degree }'
    metric = '{ "pitch" : GearPitch.CUSTOM, "unitSystem" : UnitSystem.METRIC, "module" : 1.5 * millimeter }'
    assert gear_eval(f"gearModule({imperial}) / inch") == pytest.approx(1 / 24)
    assert gear_eval(f"pressureAngle({imperial}) / degree") == pytest.approx(25)
    assert gear_eval(f"gearModule({metric}) / millimeter") == pytest.approx(1.5)


def test_nearest_gear_teeth():
    # A 3 in. pitch circle is a 60T 20 DP gear; a little bigger rounds to it, and half a tooth more to the next
    assert gear_eval("nearestGearTeeth(1.5 * inch, inch / 20)") == 60
    assert gear_eval("nearestGearTeeth(1.51 * inch, inch / 20)") == 60
    assert gear_eval("nearestGearTeeth(1.5 * inch + inch / 40 * 0.6, inch / 20)") == 61
