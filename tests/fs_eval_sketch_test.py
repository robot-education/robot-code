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
