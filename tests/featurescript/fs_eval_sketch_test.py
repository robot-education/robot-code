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


def gear_profile(teeth: int, module_mm: float = 1.27, pressure_degrees: float = 20, fillet: str = "1",
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
    # A 1/16 in. bit fits a 20 DP gear's gaps as fillets
    sketch, form = gear_profile(24, fillet="1 / 32 * inch")
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    radii = {round(curve.radius, 9) for curve in loops[0].curves if curve.kind == "arc" and "_4" not in curve.id}
    assert round(0.0254 / 32, 9) in radii


def gear_slot(teeth: int, bit_radius: str) -> dict:
    form = f"getGearForm(1.27 * millimeter, {teeth}, 20 * degree, {bit_radius}, 0 * meter)"
    return to_python(shared_evaluator().eval(f"gearRoot({form}).slot", "gear/gearCommon.fs"))


def test_router_bit_too_wide_for_the_root_cuts_a_slot():
    # A 5/64 in. bit can't reach a 24T 20 DP gear's root between its flanks: a slot of its width down the gap's middle,
    # square to the pitch circle, to the root circle
    sketch, form = gear_profile(24, fillet="5 / 128 * inch")
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    curves = loops[0].curves
    assert min(curve.nearest() for curve in curves) == pytest.approx(form["rootRadius"], abs=1e-9)
    radius = 0.0254 * 5 / 128
    gap = -math.pi / 24
    # Its round bottom: the bit's radius, centered on the gap's middle, a radius above the root circle
    (bottom,) = [curve for curve in curves if curve.id == "tooth0_0"]
    assert bottom.radius == pytest.approx(radius, abs=1e-12)
    assert math.atan2(bottom.center[1], bottom.center[0]) == pytest.approx(gap, abs=1e-9)
    assert math.hypot(*bottom.center) == pytest.approx(form["rootRadius"] + radius, abs=1e-12)
    # Its side: parallel to the gap's middle, the radius from it
    (side,) = [curve for curve in curves if curve.id == "tooth0_1"]
    direction = (math.cos(gap), math.sin(gap))
    for x, y in (side.start, side.end):
        assert abs(x * direction[1] - y * direction[0]) == pytest.approx(radius, abs=1e-9)
    assert not gear_slot(24, "5 / 128 * inch")["tooLarge"]


def test_router_bit_too_wide_for_the_gap_at_the_pitch_circle():
    # A 1/8 in. bit's slots cut a 20 DP gear's flanks above its pitch circle (its gaps are about 0.08 in. wide there)
    slot = gear_slot(24, "1 / 16 * inch")
    assert slot["tooLarge"] and slot["exitRadius"] > 24 * 1.27e-3 / 2
    # Its profile still closes, for showing what the slots cut
    sketch, _ = gear_profile(14, fillet="1 / 16 * inch")
    assert len(sketch.loops()) == 1 and sketch.loops()[0].closed


ROOT_FILLETS = ["1", "2 / 3", "1 / 2", "0"]


@pytest.mark.parametrize("teeth", [10, 24, 60])
@pytest.mark.parametrize("pressure_degrees", [14.5, 20])
@pytest.mark.parametrize("fillet", ROOT_FILLETS[1:])
def test_root_fillets_are_closed_and_meet_tangent(teeth, pressure_degrees, fillet):
    # 1/3, 1/4, and None, as Onshape's Spur gear's Root fillet (test_gear_profile checks Full): none of them dig below
    # the root
    sketch, form = gear_profile(teeth, pressure_degrees=pressure_degrees, fillet=fillet)
    loops = sketch.loops()
    assert len(loops) == 1 and loops[0].closed
    assert min(curve.nearest() for curve in loops[0].curves) == pytest.approx(form["rootRadius"], abs=1e-9)
    if fillet == "0":
        return
    for before, after in loops[0].joints():
        if "spline" not in (before.kind, after.kind):
            out, into = before.tangent(True), after.tangent(False)
            assert abs(out[0] * into[1] - out[1] * into[0]) < 1e-6, f"{before.id} and {after.id} aren't tangent"


def root_arcs(teeth: int, fillet: str):
    """The first tooth's arcs on its clockwise side, below its flank: its root's and fillet's, or its full round."""
    sketch, form = gear_profile(teeth, fillet=fillet)
    arcs = [curve for curve in sketch.loops()[0].curves if curve.kind == "arc" and curve.id in ("tooth0_0", "tooth0_1")]
    return arcs, form


def test_full_round_is_tangent_to_the_root_circle():
    (round_arc,), form = root_arcs(24, "1")
    # Centered in the gap, a radius above the root circle
    assert math.hypot(*round_arc.center) - round_arc.radius == pytest.approx(form["rootRadius"], abs=1e-9)
    tooth_angle = 2 * math.pi / 24
    assert math.atan2(round_arc.center[1], round_arc.center[0]) == pytest.approx(-tooth_angle / 2, abs=1e-9)


@pytest.mark.parametrize("fillet, fraction", [("2 / 3", 2 / 3), ("1 / 2", 1 / 2)])
def test_partial_root_fillets_are_a_fraction_of_the_full_round(fillet, fraction):
    (full,), _ = root_arcs(24, "1")
    root, round_arc = root_arcs(24, fillet)[0]
    assert round_arc.radius == pytest.approx(full.radius * fraction, abs=1e-12)


def test_no_root_fillet_leaves_a_sharp_corner():
    sketch, form = gear_profile(24, fillet="0")
    arcs = [curve for curve in sketch.loops()[0].curves if curve.kind == "arc"]
    # Only the root's and tips' arcs
    assert {round(arc.radius, 9) for arc in arcs} == {round(form["rootRadius"], 9), round(form["tipRadius"], 9)}


RACK_FORM = "getGearForm(1 * millimeter, undefined, 20 * degree, {fillet}, {offset} * millimeter)"


def rack_centers(length_mm: float, gap_mm: float, offset_mm: float = 0.0) -> list[float]:
    form = RACK_FORM.format(fillet=0, offset=offset_mm)
    return to_python(shared_evaluator().eval(
        f"rackToothCenters({form}, {length_mm} * millimeter, {gap_mm} * millimeter)", "gear/gearCommon.fs"))


def rack_cut(face_height: str, length_mm: float, offset_mm: float = 0.0, fillet: str = "0"):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    form = RACK_FORM.format(fillet=fillet, offset=offset_mm)
    centers = f"rackToothCenters({form}, {length_mm} * millimeter, 0 * meter)"
    regions = f"rackCutRegions({form}, {face_height}, {length_mm} * millimeter, {centers}, undefined)"
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


def rack_regions(length_mm: float, gap_mm: float, face_height: str = "1 * millimeter", offset_mm: float = 0.0, stop: str = "undefined"):
    """A rack's cut regions (without root fillets), as lists of (x, y) in mm, and its teeth's centers."""
    form = RACK_FORM.format(fillet=0, offset=offset_mm)
    centers = rack_centers(length_mm, gap_mm, offset_mm)
    centers_fs = "[" + ", ".join(f"{c} * meter" for c in centers) + "]"
    regions = to_python(shared_evaluator().eval(
        f"rackCutRegions({form}, {face_height}, {length_mm} * millimeter, {centers_fs}, {stop})", "gear/gearCommon.fs"))
    return [[(x * 1000, y * 1000) for x, y in region] for region in regions], [c * 1000 for c in centers]


def inside(polygon, point) -> bool:
    x, y = point
    result = False
    for (x1, y1), (x2, y2) in zip(polygon, polygon[1:] + polygon[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            result = not result
    return result


@pytest.mark.parametrize("length_mm", [10 * math.pi, 7.3, 12.9, 20.05, 31.4])
@pytest.mark.parametrize("gap_mm", [0, 0.4, 1.1, 2.9])
@pytest.mark.parametrize("face_height,offset_mm", [("1 * millimeter", 0), ("0 * meter", 0), ("1 * millimeter", -0.1)])
def test_rack_leaves_no_partial_teeth(length_mm, gap_mm, face_height, offset_mm):
    # Whatever its length and wherever it's lined up, the teeth are whole and a pitch apart, and past the first and
    # last, it's cut to the root: just above the root, everything's cut but the teeth's roots
    regions, centers = rack_regions(length_mm, gap_mm, face_height, offset_mm)
    pitch = math.pi
    half_root = pitch / 4 + offset_mm / math.cos(math.radians(20)) + (1.25 - offset_mm) * math.tan(math.radians(20))
    assert centers and all(c - half_root >= -1e-9 and c + half_root <= length_mm + 1e-9 for c in centers)
    assert all(b - a == pytest.approx(pitch) for a, b in zip(centers, centers[1:]))
    face = 1 if face_height.startswith("1") else 0
    y = -1.25 + offset_mm - face + 1e-6
    for i in range(1, 400):
        x = length_mm * i / 400
        in_tooth = any(abs(x - c) < half_root - 1e-6 for c in centers)
        near_corner = any(abs(abs(x - c) - half_root) < 1e-5 for c in centers)
        if not near_corner:
            assert any(inside(region, (x, y)) for region in regions) != in_tooth, f"at {x} mm"


def test_partial_teeth_stop_at_a_count_or_a_point():
    pitch = math.pi * 0.001
    centers = "[" + ", ".join(f"{(k + 0.5) * pitch} * meter" for k in range(10)) + "]"

    def partial(count: str, stop: str) -> dict:
        return to_python(shared_evaluator().eval(
            f"partialTeeth({centers}, {pitch} * meter, 1 * millimeter, {count}, {stop})", "gear/gearCommon.fs"))

    # The first 3, and the 4th as the stop
    three = partial("3", "undefined")
    assert three["centers"] == pytest.approx([(k + 0.5) * pitch for k in range(3)])
    assert three["stopCenter"] == pytest.approx(3.5 * pitch)
    # As many as fit before the stop: the next tooth's root (1 mm from its center) at it or before it
    stop = 4.5 * pitch - 0.001
    assert len(partial("undefined", f"{stop} * meter")["centers"]) == 4
    assert len(partial("undefined", f"{stop - 1e-6} * meter")["centers"]) == 3
    # Whichever's fewer; and all of them, with no stop, if neither stops them
    assert len(partial("2", f"{stop} * meter")["centers"]) == 2
    everything = partial("20", "undefined")
    assert len(everything["centers"]) == 10 and "stopCenter" not in everything


@pytest.mark.parametrize("face_height,offset_mm", [("1 * millimeter", 0), ("1 * millimeter", -0.1)])
def test_partial_rack_leaves_the_face_past_its_stop(face_height, offset_mm):
    # Ten pitches long, stopping after four teeth: the fifth's near flank, and past it, nothing's cut
    length = 10 * math.pi
    centers = rack_centers(length, 0, offset_mm)
    stop_center = centers[4]
    kept = "[" + ", ".join(f"{c} * meter" for c in centers[:4]) + "]"
    form = RACK_FORM.format(fillet=0, offset=offset_mm)
    regions = to_python(shared_evaluator().eval(
        f'rackCutRegions({form}, {face_height}, {length} * millimeter, {kept}, {{ "center" : {stop_center} * meter, "after" : true }})',
        "gear/gearCommon.fs"))
    points = [(x * 1000, y * 1000) for region in regions for x, y in region]
    alpha = math.radians(20)
    tip = 1 + offset_mm
    near_tip = stop_center * 1000 - (math.pi / 4 + offset_mm / math.cos(alpha) - tip * math.tan(alpha))
    assert max(x for x, _ in points) == pytest.approx(near_tip, abs=1e-9)
    # A gap before each of the four teeth and after the last; or teeth stopping short of the face, one region
    assert len(regions) == (5 if offset_mm == 0 else 1)
    # The other way, from the end: before the stop, nothing's cut
    kept = "[" + ", ".join(f"{c} * meter" for c in centers[6:]) + "]"
    regions = to_python(shared_evaluator().eval(
        f'rackCutRegions({form}, {face_height}, {length} * millimeter, {kept}, {{ "center" : {centers[5]} * meter, "after" : false }})',
        "gear/gearCommon.fs"))
    points = [(x * 1000, y * 1000) for region in regions for x, y in region]
    assert min(x for x, _ in points) == pytest.approx(centers[5] * 1000 + (near_tip - centers[4] * 1000) * -1, abs=1e-9)


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


def test_rack_roots_are_rounded_for_a_router_bit():
    # 1 mm module: a gap's floor is about 0.66 mm, room for a 0.2 mm radius bit's round at each corner
    sketch = rack_cut("0 * meter", 10 * math.pi, fillet="0.2 * millimeter")
    loops = sketch.loops()
    assert len(loops) == 11 and all(loop.closed for loop in loops)
    arcs = [curve for loop in loops for curve in loop.curves if curve.kind == "arc"]
    # Two corners in each gap between teeth, and one at each end's
    assert len(arcs) == 9 * 2 + 2
    assert all(arc.radius == pytest.approx(0.0002, abs=1e-12) for arc in arcs)
    # The floor's still at the root
    points = [point for loop in loops for curve in loop.curves for point in (curve.start, curve.end)]
    assert min(y for _, y in points) == pytest.approx(-0.00125, abs=1e-12)


def rack_bit_too_large(radius_mm: float) -> bool:
    form = RACK_FORM.format(fillet=f"{radius_mm} * millimeter", offset=0)
    return to_python(shared_evaluator().eval(f"rackBitTooLarge({form})", "gear/gearCommon.fs"))


def test_rack_roots_are_a_slot_when_the_floor_is_too_narrow():
    # A 0.5 mm radius bit doesn't fit both corners: a slot of its width down each gap's middle, to the root
    sketch = rack_cut("0 * meter", 10 * math.pi, fillet="0.5 * millimeter")
    loops = sketch.loops()
    assert all(loop.closed for loop in loops)
    gap = loops[1]
    arcs = [curve for curve in gap.curves if curve.kind == "arc"]
    assert len(arcs) == 1 and arcs[0].radius == pytest.approx(0.0005, abs=1e-12)
    assert arcs[0].center[1] - arcs[0].radius == pytest.approx(-0.00125, abs=1e-12)
    # Its sides square to the floor, the radius from its middle
    sides = [curve for curve in gap.curves if curve.kind == "line" and abs(curve.start[0] - curve.end[0]) < 1e-12]
    assert sorted(abs(side.start[0] - arcs[0].center[0]) for side in sides) == pytest.approx([0.0005, 0.0005], abs=1e-12)
    assert not rack_bit_too_large(0.5)
    # Fillets fit, so no slots
    assert not rack_bit_too_large(0.2)


def test_rack_full_round_fillets_meet_in_the_floors_middle():
    sketch = rack_cut("0 * meter", 10 * math.pi, fillet="1")
    loops = sketch.loops()
    assert all(loop.closed for loop in loops)
    alpha = math.radians(20)
    floor = math.pi * 0.001 / 2 - 2.5 * 0.001 * math.tan(alpha)
    expected = floor / 2 * math.tan(math.pi / 4 + alpha / 2)
    gap = loops[1]
    arcs = [curve for curve in gap.curves if curve.kind == "arc"]
    assert len(arcs) == 2 and all(arc.radius == pytest.approx(expected, abs=1e-12) for arc in arcs)
    # Meeting at the root, tangent to it, with no floor between them
    assert arcs[0].end == pytest.approx(arcs[1].start, abs=1e-12) or arcs[1].end == pytest.approx(arcs[0].start, abs=1e-12)
    points = [point for curve in gap.curves for point in (curve.start, curve.end)]
    assert min(arc.center[1] - arc.radius for arc in arcs) == pytest.approx(-0.00125, abs=1e-12)
    assert min(y for _, y in points) >= -0.00125 - 1e-12


@pytest.mark.parametrize("fillet", ["2 / 3", "1 / 2"])
def test_rack_partial_fillets_leave_a_floor(fillet):
    sketch = rack_cut("0 * meter", 10 * math.pi, fillet=fillet)
    gap = sketch.loops()[1]
    lines = [curve for curve in gap.curves if curve.kind == "line" and abs(curve.start[1] + 0.00125) < 1e-12 and abs(curve.end[1] + 0.00125) < 1e-12]
    assert len(lines) == 1


def test_rack_slot_too_big_for_its_gaps():
    # Its slots would cut the flanks above the pitch line, where the gaps are about 0.79 mm across; they're still cut,
    # for showing what they cut
    assert rack_bit_too_large(0.8)
    sketch = rack_cut("0 * meter", 10 * math.pi, fillet="0.8 * millimeter")
    assert all(loop.closed for loop in sketch.loops())


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


def test_custom_pitch():
    diametral = '{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.DIAMETRAL_PITCH, "diametralPitch" : 24, "pressureAngle" : 25 * degree }'
    module = '{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.MODULE, "module" : 1.5 * millimeter }'
    circular = '{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.CIRCULAR_PITCH, "circularPitch" : PI * 2 * millimeter }'
    assert gear_eval(f"gearModule({diametral}) / inch") == pytest.approx(1 / 24)
    assert gear_eval(f"pressureAngle({diametral}) / degree") == pytest.approx(25)
    assert gear_eval(f"gearModule({module}) / millimeter") == pytest.approx(1.5)
    assert gear_eval(f"gearModule({circular}) / millimeter") == pytest.approx(2)


@pytest.mark.parametrize("definition,units", [
    ('{ "pitch" : GearPitch.DP_20 }', "IMPERIAL"),
    ('{ "pitch" : GearPitch.MODULE_0_8 }', "METRIC"),
    ('{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.DIAMETRAL_PITCH }', "IMPERIAL"),
    ('{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.MODULE }', "METRIC"),
    # A circular pitch typed in inches, or in millimeters
    ('{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.CIRCULAR_PITCH, "circularPitch" : 0.25 * inch }', "IMPERIAL"),
    ('{ "pitch" : GearPitch.CUSTOM, "customPitch" : CustomPitch.CIRCULAR_PITCH, "circularPitch" : 5 * millimeter }', "METRIC"),
])
def test_values_are_shown_in_the_pitchs_units(definition, units):
    assert gear_eval(f"pitchUnits({definition}) == UnitSystem.{units}") is True


def test_nearest_gear_teeth():
    # A 3 in. pitch circle is a 60T 20 DP gear; a little bigger rounds to it, and half a tooth more to the next
    assert gear_eval("nearestGearTeeth(1.5 * inch, inch / 20)") == 60
    assert gear_eval("nearestGearTeeth(1.51 * inch, inch / 20)") == 60
    assert gear_eval("nearestGearTeeth(1.5 * inch + inch / 40 * 0.6, inch / 20)") == 61


def bevel_cones(teeth: int, mate_teeth: int, shaft_degrees: float = 90, module_mm: float = 1) -> dict:
    return to_python(shared_evaluator().eval(
        f"bevelCones({module_mm} * millimeter, {teeth}, {mate_teeth}, {shaft_degrees} * degree)", "gear/gearCommon.fs"))


def test_bevel_cones():
    # A miter pair: 45 degree pitch cones, and a back cone sqrt(2) times the pitch radius
    cones = bevel_cones(20, 20)
    assert cones["pitchAngle"] == pytest.approx(math.pi / 4)
    assert cones["pitchRadius"] == pytest.approx(0.010)
    assert cones["coneDistance"] == pytest.approx(0.010 * math.sqrt(2))
    assert cones["virtualTeeth"] == pytest.approx(20 * math.sqrt(2))
    # At right angles, the pinion's and gear's pitch angles add up to the shaft angle, and their cone distances match
    pinion, gear = bevel_cones(15, 45), bevel_cones(45, 15)
    assert pinion["pitchAngle"] + gear["pitchAngle"] == pytest.approx(math.pi / 2)
    assert pinion["pitchAngle"] == pytest.approx(math.atan(15 / 45))
    assert pinion["coneDistance"] == pytest.approx(gear["coneDistance"])
    # And at other shaft angles too
    pinion, gear = bevel_cones(18, 30, 60), bevel_cones(30, 18, 60)
    assert pinion["pitchAngle"] + gear["pitchAngle"] == pytest.approx(math.pi / 3)
    assert pinion["coneDistance"] == pytest.approx(gear["coneDistance"])


@pytest.mark.parametrize("virtual_teeth", [12.5, 28.28, 60])
@pytest.mark.parametrize("fillet", ROOT_FILLETS)
def test_tooth_gap_is_one_closed_loop(virtual_teeth, fillet):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    form = f"getGearForm(1 * millimeter, {virtual_teeth}, 20 * degree, {fillet}, 0 * meter)"
    evaluator.eval(f'sketchToothGap(context, newId() + "gap", XY_PLANE, {form}, 1)', "gear/gearCommon.fs", context=context)
    loops = recorded_sketch(context, "gap").loops()
    assert len(loops) == 1 and loops[0].closed
    info = to_python(evaluator.eval(form, "gear/gearCommon.fs"))
    curves = loops[0].curves
    # From the root circle to a module past the tip
    assert min(curve.nearest() for curve in curves) == pytest.approx(info["rootRadius"], abs=1e-9)
    assert max(curve.farthest() for curve in curves) == pytest.approx(info["tipRadius"] + 0.001, abs=1e-9)
