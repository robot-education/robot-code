"""Tests of spherical involute bevel gears' teeth (`sphericalBevelForm`, with `bevelDepths`' Gleason proportions): their
flanks, as the form gives them projected onto its plane, are taken back onto the sphere of the cone distance, and a
pair is turned through a tooth to check that they mesh: touching on both sides, without overlapping."""

import bisect
import functools
import math

import pytest

from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator
from fs_eval.sketch import recorded_sketch

FLANK_SAMPLES = 40


@functools.cache
def bevel_form(teeth: int, mate_teeth: int, shaft_degrees: float, pressure_degrees: float = 20, offset_mm: float = 0):
    """The gear's cones, depths, and form, and its flank: its tooth's half angle at radii from the top of its root's
    radial line (`gearRoot`'s `lineTop`, where the involute starts) to its tip."""
    definition = ('{ "gearType" : GearType.BEVEL, "bevelForm" : BevelForm.SPHERICAL_INVOLUTE, "pitch" : GearPitch.CUSTOM, '
                  f'"pressureAngle" : {pressure_degrees} * degree, "mateTeeth" : {mate_teeth}, "shaftAngle" : {shaft_degrees} * degree, '
                  f'"addProfileOffset" : {"true" if offset_mm else "false"}, "profileOffsetDistance" : {abs(offset_mm)} * millimeter, '
                  f'"profileOffsetOppositeDirection" : {"true" if offset_mm < 0 else "false"} }}')
    expression = f"""(function() {{
        const definition = {definition};
        const cones = bevelPairCones(millimeter, {teeth}, {mate_teeth}, {shaft_degrees} * degree);
        const depths = bevelDepths(definition, millimeter, {teeth}, cones);
        const form = sphericalBevelForm(millimeter, {teeth}, cones.gear, cones.mate.pitchAngle, {pressure_degrees} * degree, depths.thickness,
            depths.addendum, depths.dedendum, depths.mateAddendum, getProfileOffset(definition));
        const lineTop = gearRoot(form).lineTop;
        var flank = [];
        for (var i = 0; i <= {FLANK_SAMPLES}; i += 1)
        {{
            const radius = lineTop + (form.tipRadius - lineTop) * i / {FLANK_SAMPLES};
            flank = append(flank, [radius, toothHalfAngle(form, radius)]);
        }}
        return {{ "cones" : cones.gear, "depths" : depths, "teeth" : form.teeth, "rootRadius" : form.rootRadius, "flank" : flank }};
    }})()"""
    return to_python(shared_evaluator().eval(expression, "gear/robotGear.fs"))


class Teeth:
    """A bevel gear's teeth on the unit sphere about its apex, from its form: each tooth's half width around its axis
    (`half`), at an angle from its axis, between its root's and its tip's (at its back)."""

    def __init__(self, teeth: int, form: dict):
        cones = form["cones"]
        self.teeth = teeth
        self.pitch_angle = pitch = cones["pitchAngle"]
        distance = cones["coneDistance"]
        tooth_line = math.pi / form["teeth"]
        # The form's plane: square to the pitch cone through a gap's middle, centered where the axis meets it
        center = (0, 0, distance / math.cos(pitch))
        out = (math.cos(pitch), 0, -math.sin(pitch))
        self.angles, self.halves = [], []
        for radius, half in form["flank"]:
            # The next tooth's flank, around from the gap's middle
            around = tooth_line - half
            point = [center[k] + radius * (math.cos(around) * out[k] + math.sin(around) * (k == 1)) for k in range(3)]
            norm = math.sqrt(sum(c * c for c in point))
            self.angles.append(math.acos(point[2] / norm))
            self.halves.append(math.pi / teeth - math.atan2(point[1], point[0]))
        depths = form["depths"]
        self.tip = pitch + math.atan(depths["addendum"] / distance)
        self.root = pitch - math.atan(depths["dedendum"] / distance)

    def half(self, angle: float) -> float:
        """Half the tooth's width around the axis at `angle` from it: below its involute, its root's radial line (a plane
        through the axis), which mustn't reach where its mate's teeth touch it."""
        if angle <= self.angles[0]:
            return self.halves[0]
        # Through the three samples nearest it
        i = min(max(bisect.bisect_left(self.angles, angle), 1), len(self.angles) - 2)
        result = 0
        for j in (i - 1, i, i + 1):
            weight = 1
            for k in (i - 1, i, i + 1):
                if k != j:
                    weight *= (angle - self.angles[k]) / (self.angles[j] - self.angles[k])
            result += weight * self.halves[j]
        return result

    def flank(self, side: int, center: float, count: int = 400):
        """A tooth's flank (centered `center` around the axis), on its `side` (1, counterclockwise), from its root to its
        tip, as (angle from the axis, around it)."""
        return [(angle, center + side * self.half(angle))
                for angle in (self.root + (self.tip - self.root) * i / count for i in range(count + 1))]

    def margin(self, angle: float, around: float, turn: float) -> float:
        """How far a point is outside the teeth (turned `turn` about the axis), along the sphere: negative inside them."""
        if angle >= self.tip:
            return math.inf
        if angle < self.root:
            return angle - self.root
        pitch = 2 * math.pi / self.teeth
        offset = (around - turn) % pitch
        offset = min(offset, pitch - offset)
        return (offset - self.half(angle)) * math.sin(angle)


def direction(angle, around):
    return (math.sin(angle) * math.cos(around), math.sin(angle) * math.sin(around), math.cos(angle))


def mesh_margins(teeth: int, mate_teeth: int, shaft_degrees: float, turn: float, offset_mm: float = 0) -> dict:
    """With a pair of bevel gears meshing, the gear's first tooth toward its mate turned `turn`: how far its teeth's
    flanks near the mate are outside the mate's teeth, least on each side."""
    gear = Teeth(teeth, bevel_form(teeth, mate_teeth, shaft_degrees, offset_mm=offset_mm))
    mate = Teeth(mate_teeth, bevel_form(mate_teeth, teeth, shaft_degrees, offset_mm=offset_mm))
    shaft = math.radians(shaft_degrees)
    # The mate's axis, the shaft angle from the gear's, and its frame: x along the line their pitch cones touch along
    axis = (math.sin(shaft), 0, math.cos(shaft))
    touch = direction(gear.pitch_angle, 0)
    dot = sum(t * a for t, a in zip(touch, axis))
    x = [t - dot * a for t, a in zip(touch, axis)]
    length = math.sqrt(sum(c * c for c in x))
    x = [c / length for c in x]
    y = [axis[1] * x[2] - axis[2] * x[1], axis[2] * x[0] - axis[0] * x[2], axis[0] * x[1] - axis[1] * x[0]]
    # The mate has a gap where the gear has a tooth, and turns the other way, by the ratio
    mate_turn = math.pi / mate_teeth - turn * teeth / mate_teeth
    least = {}
    for side in (1, -1):
        margins = []
        for k in (-1, 0, 1):
            for angle, around in gear.flank(side, turn + k * 2 * math.pi / teeth):
                u = direction(angle, around)
                local = [sum(u[i] * basis[i] for i in range(3)) for basis in (x, y, axis)]
                margins.append(mate.margin(math.acos(max(-1, min(1, local[2]))), math.atan2(local[1], local[0]), mate_turn))
        least[side] = min(margins)
    return least


@pytest.mark.parametrize("teeth,mate_teeth,shaft_degrees", [(13, 31, 90), (20, 20, 90), (15, 30, 60), (12, 24, 110)])
def test_spherical_bevel_pair_meshes_through_a_tooth(teeth, mate_teeth, shaft_degrees):
    # Touching (within the flanks' sampling) on both sides, without overlapping, wherever they're turned to
    pitch = 2 * math.pi / teeth
    for step in range(10):
        least = mesh_margins(teeth, mate_teeth, shaft_degrees, pitch * (step / 10 - 0.5))
        for side, margin in least.items():
            assert -1e-6 < margin < 1e-5, f"side {side}, turned {step}/10 of a tooth: {margin}"


def test_negative_profile_offset_leaves_backlash():
    least = mesh_margins(13, 31, 90, 0.05, offset_mm=-0.05)
    # Each tooth's flanks are moved in 0.05 mm, on a cone distance of about 16.8 mm: twice that between them
    for margin in least.values():
        assert margin > 0.05 / 16.8


@pytest.mark.parametrize("teeth,mate_teeth", [(13, 31), (31, 13), (20, 20)])
def test_spherical_bevel_tooth_thickness_at_its_pitch_cone(teeth, mate_teeth):
    form = bevel_form(teeth, mate_teeth, 90)
    gear = Teeth(teeth, form)
    # Half its circular thickness, around its pitch circle (`m z / 2` across)
    assert gear.half(gear.pitch_angle) == pytest.approx(form["depths"]["thickness"] / teeth / 0.001, abs=1e-6)


@pytest.mark.parametrize("teeth,mate_teeth,shaft_degrees", [(10, 40, 90), (13, 31, 90), (20, 20, 90), (31, 13, 90), (15, 30, 60)])
def test_spherical_bevel_gap_is_one_closed_loop(teeth, mate_teeth, shaft_degrees):
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    form = f"""(function() {{
        const definition = {{ "bevelForm" : BevelForm.SPHERICAL_INVOLUTE, "pitch" : GearPitch.DP_10, "mateTeeth" : {mate_teeth}, "addProfileOffset" : false }};
        const cones = bevelPairCones(inch / 10, {teeth}, {mate_teeth}, {shaft_degrees} * degree);
        const depths = bevelDepths(definition, inch / 10, {teeth}, cones);
        return sphericalBevelForm(inch / 10, {teeth}, cones.gear, cones.mate.pitchAngle, 20 * degree, depths.thickness, depths.addendum,
            depths.dedendum, depths.mateAddendum, 0 * meter);
    }})()"""
    evaluator.eval(f'sketchToothGap(context, newId() + "gap", XY_PLANE, {form}, 0.5)', "gear/robotGear.fs", context=context)
    loops = recorded_sketch(context, "gap").loops()
    assert len(loops) == 1 and loops[0].closed
    info = to_python(evaluator.eval(f"(function() {{ const form = {form}; return [form.rootRadius, form.tipRadius]; }})()", "gear/robotGear.fs"))
    curves = loops[0].curves
    # Scaled by half: from the root to a module past the tip
    assert min(curve.nearest() for curve in curves) == pytest.approx(info[0] / 2, abs=1e-9)
    assert max(curve.farthest() for curve in curves) == pytest.approx((info[1] + 0.00254) / 2, abs=1e-9)


@pytest.mark.parametrize("bit_mm,expected", [(0.25, "none"), (0.35, "slot"), (0.45, "too large")])
def test_router_bit_slots_where_it_cant_reach_the_root(bit_mm, expected):
    # A 13T pinion's gap, at 0.6 of its back's size: a small bit reaches its fillets, a bigger one cuts a slot down its
    # middle, and one so big its slot meets the flanks above the pitch cone is too large
    expression = f"""(function() {{
        const definition = {{ "bevelForm" : BevelForm.SPHERICAL_INVOLUTE, "pitch" : GearPitch.CUSTOM, "pressureAngle" : 20 * degree,
            "mateTeeth" : 31, "addProfileOffset" : false }};
        const cones = bevelPairCones(millimeter, 13, 31, 90 * degree);
        const depths = bevelDepths(definition, millimeter, 13, cones);
        const form = sphericalBevelForm(millimeter, 13, cones.gear, cones.mate.pitchAngle, 20 * degree, depths.thickness, depths.addendum,
            depths.dedendum, depths.mateAddendum, 0 * meter);
        const root = bevelBitRoot(form, {bit_mm} * millimeter, 0.6);
        return root.slot == undefined ? "none" : root.slot.tooLarge ? "too large" : "slot";
    }})()"""
    assert to_python(shared_evaluator().eval(expression, "gear/robotGear.fs")) == expected
