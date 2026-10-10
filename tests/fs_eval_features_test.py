"""Tests of features' internal functions, evaluated in their modules' scopes (where what they don't export is
visible too). Exported functions are tested in FeatureScript, in tests/featurescript."""

import pytest

from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator

BELT = "released/belt/robotBelt.fs"
CHAIN = "chain/robotChain.fs"


def evaluate(expression: str, module: str, **variables):
    return shared_evaluator().eval(expression, module, **variables)


@pytest.mark.parametrize("teeth,pulleys", [(100, (18, 36)), (150, (12, 60)), (70, (12, 36)), (200, (24, 24))])
def test_simple_belt_center_distance_is_exact(teeth, pulleys):
    distance = evaluate(f"computeBeltCenterToCenter(BeltType._5_MM_HTD, {teeth}, [{pulleys[0]}, {pulleys[1]}], 0 * meter)", BELT)
    radii = [f"getPulleyRadius(5 * millimeter, {n})" for n in pulleys]
    length = evaluate(f"tryTwoPulleyLength([{radii[0]}, {radii[1]}], distance)", BELT, distance=distance)
    # To the solver's precision, std's zero length tolerance
    assert to_python(length) == pytest.approx(teeth * 0.005, abs=1e-8)


def test_pitch_circle_teeth():
    # A 24T 5mm HTD pulley's pitch circle: 120 mm around
    radius = 0.12 / (2 * 3.141592653589793)
    assert evaluate(f"computeTargetPulleyTeeth(5 * millimeter, {radius} * meter)", BELT) == pytest.approx(24)


@pytest.mark.parametrize("length,links", [(25.0, 100), (25.1, 100), (25.3, 102), (0.1, 2)])
def test_closest_links_are_even(length, links):
    assert evaluate(f"closestLinks(getChainInfo(ChainType.ANSI_25), {length} * inch)", CHAIN) == links


LIGHTEN = "lighten/robotLighten.fs"
SKETCH, FEW = "qEverything(EntityType.EDGE)", "qNthElement(qEverything(EntityType.EDGE), 0)"


def lighten_definition(override: bool, excluded: bool = False) -> str:
    return (
        f'{{ "excludeConstruction" : {str(excluded).lower()}, "ribEdges" : {SKETCH}, "ribThickness" : 1 * inch, '
        f'"overrideRibThickness" : {str(override).lower()}, '
        f'"ribOverrides" : [{{ "overrideEdges" : {FEW}, "overrideThickness" : 2 * inch }}] }}'
    )


def test_rib_overrides_take_precedence():
    # A whole sketch at 1 in., then a few of its ribs at 2 in.: the sketch's group loses the few
    groups = f"ribGroups({lighten_definition(True)})"
    sketch, few = f"qEntityFilter({SKETCH}, EntityType.EDGE)", f"qEntityFilter({FEW}, EntityType.EDGE)"
    assert evaluate(f"size({groups})", LIGHTEN) == 2
    assert evaluate(f"{groups}[0].edges == qSubtraction({sketch}, qUnion([qNothing(), {few}]))", LIGHTEN) is True
    assert evaluate(f"{groups}[1].edges == qSubtraction({few}, qNothing())", LIGHTEN) is True
    assert to_python(evaluate(f"{groups}[1].thickness / inch", LIGHTEN)) == pytest.approx(2)
    assert to_python(evaluate(f"{groups}[0].parameters", LIGHTEN)) == ["ribEdges", "ribThickness"]
    assert to_python(evaluate(f"{groups}[1].parameters", LIGHTEN)) == [
        "ribOverrides[0].overrideEdges",
        "ribOverrides[0].overrideThickness",
    ]


def test_ribs_without_overrides_are_one_group_less_construction():
    groups = f"ribGroups({lighten_definition(False, excluded=True)})"
    assert evaluate(f"size({groups})", LIGHTEN) == 1
    assert evaluate(
        f"{groups}[0].selected == qConstructionFilter(qEntityFilter({SKETCH}, EntityType.EDGE), ConstructionObject.NO)", LIGHTEN
    ) is True
