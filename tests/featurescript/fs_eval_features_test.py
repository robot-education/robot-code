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


LIGHTEN = "released/lighten/robotLighten.fs"
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
    assert evaluate(f"{groups}[0].edges == qSubtraction({sketch}, qUnion([{few}]))", LIGHTEN) is True
    assert evaluate(f"{groups}[1].edges == qSubtraction({few}, qUnion([]))", LIGHTEN) is True
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


def test_later_wall_overrides_take_precedence():
    faces, few = "qEverything(EntityType.FACE)", "qNthElement(qEverything(EntityType.FACE), 0)"
    definition = (
        '{ "wallOverrides" : ['
        f'{{ "overrideFaces" : {faces}, "overrideWall" : 1 * inch }}, {{ "overrideFaces" : {few}, "overrideWall" : 2 * inch }}] }}'
    )
    groups = f"wallOverrideGroups({definition})"
    every, first = f"qEntityFilter({faces}, EntityType.FACE)", f"qEntityFilter({few}, EntityType.FACE)"
    assert evaluate(f"{groups}[0].faces == qSubtraction({every}, qUnion([{first}]))", LIGHTEN) is True
    assert evaluate(f"{groups}[1].faces == qSubtraction({first}, qUnion([]))", LIGHTEN) is True
    assert to_python(evaluate(f"{groups}[1].thickness / inch", LIGHTEN)) == pytest.approx(2)
    assert to_python(evaluate(f"{groups}[0].parameters", LIGHTEN)) == [
        "wallOverrides[0].overrideFaces",
        "wallOverrides[0].overrideWall",
    ]


SHAFT = "released/shaft/robotShaft.fs"


def shaft_definition(**overrides: str) -> str:
    """A shaft's definition (a FeatureScript map): every parameter the end predicates read, set as `overrides` say."""
    values = {
        "program": "Program.FRC",
        "shaftSource": "ShaftSource.CUSTOM",
        "presetShaft": "false",
        "shaftType": "ShaftType.HEX",
        "hexType": "HexType.STOCK",
        "hexSize": "HexSize._1_2_IN",
        "splineType": "SplineType.MAX_SPLINE",
        "ftcCustomShaft": "FtcCustomShaft.REX_8_MM",
        "firstEndOperation": "EndOperation.TAPPED_HOLE",
        "secondEndOperation": "SecondEndOperation.TAPPED_HOLE",
        "firstEndMetricOperation": "MetricEndOperation.TAPPED_HOLE",
        "secondEndMetricOperation": "MetricSecondEndOperation.TAPPED_HOLE",
        "mirrorShaft": "false",
        "modifyFirstEnd": "true",
        "symmetricEnds": "false",
        **overrides,
    }
    return "{ " + ", ".join(f'"{key}" : {value}' for key, value in values.items()) + " }"


def shaft_is(predicate: str, **overrides: str) -> bool:
    return evaluate(f"{predicate}({shaft_definition(**overrides)})", SHAFT)


def test_ftc_cots_shafts_are_preset_lengths_without_editing_logic():
    # Every FTC COTS shaft is sold in set lengths; an FRC one is only when editing logic says so (a SplineXS stub)
    assert shaft_is("isPresetLength", program="Program.FTC", shaftSource="ShaftSource.COTS") is True
    assert shaft_is("isPresetLength", shaftSource="ShaftSource.COTS") is False
    assert shaft_is("isPresetLength", shaftSource="ShaftSource.COTS", presetShaft="true") is True
    assert shaft_is("isPresetLength", program="Program.FTC", presetShaft="true") is False


@pytest.mark.parametrize(
    "overrides,metric",
    [
        ({"program": "Program.FTC"}, True),
        ({"program": "Program.FTC", "ftcCustomShaft": "FtcCustomShaft.REX_12_MM"}, True),
        ({"program": "Program.FTC", "ftcCustomShaft": "FtcCustomShaft.ROBITS_HEX"}, False),
        ({"hexSize": "HexSize._7_MM"}, True),
        ({"hexSize": "HexSize._11_MM", "hexType": "HexType.ROUNDED_HEX"}, True),
        ({"hexSize": "HexSize._3_8_IN"}, False),
        # UltraHex is 1/2 in., whatever its hidden size
        ({"hexSize": "HexSize._7_MM", "hexType": "HexType.ULTRA_HEX"}, False),
        ({"hexSize": "HexSize._7_MM", "shaftSource": "ShaftSource.COTS"}, False),
        ({"hexSize": "HexSize._7_MM", "shaftType": "ShaftType.SPLINE", "splineType": "SplineType.SPLINE_XS"}, False),
    ],
)
def test_metric_hex_shafts(overrides, metric):
    assert shaft_is("isMetricHexShaft", **overrides) is metric


# Each end predicate against the operation getEndOperation reads, for every end operation on imperial, metric, and
# SplineXS shafts: the predicates repeat isMetricHexShaft's condition, as Onshape needs
SHAFTS = [
    {},
    {"program": "Program.FTC"},
    {"hexSize": "HexSize._11_MM"},
    {"shaftType": "ShaftType.SPLINE", "splineType": "SplineType.SPLINE_XS"},
]
FIRST_OPERATIONS = ["TAPPED_HOLE", "CLEARANCE_HOLE", "RETAINING_RING", "CAPTIVE_SHAFT"]
METRIC_FIRST_OPERATIONS = ["TAPPED_HOLE", "CLEARANCE_HOLE", "E_CLIP", "CAPTIVE_SHAFT"]


@pytest.mark.parametrize("shaft", SHAFTS)
@pytest.mark.parametrize("index", range(4))
def test_first_end_predicates_match_its_operation(shaft, index):
    overrides = {
        **shaft,
        "firstEndOperation": f"EndOperation.{FIRST_OPERATIONS[index]}",
        "firstEndMetricOperation": f"MetricEndOperation.{METRIC_FIRST_OPERATIONS[index]}",
    }
    operation = str(evaluate(f"getEndOperation({shaft_definition(**overrides)}, ShaftEnd.FIRST)", SHAFT)).split(".")[-1]
    assert shaft_is("isTappedFirstEnd", **overrides) is (operation == "TAPPED_HOLE")
    assert shaft_is("isClearanceFirstEnd", **overrides) is (operation == "CLEARANCE_HOLE")
    assert shaft_is("isGrooveFirstEnd", **overrides) is (operation == "RETAINING_RING")
    # A clearance hole goes through, so there's no second end to choose
    assert shaft_is("cannotSpecifySecondEnd", **overrides) is (operation == "CLEARANCE_HOLE")


@pytest.mark.parametrize("shaft", SHAFTS)
@pytest.mark.parametrize("index", range(3))
def test_second_end_predicates_match_its_operation(shaft, index):
    overrides = {
        **shaft,
        "secondEndOperation": f"SecondEndOperation.{['TAPPED_HOLE', 'RETAINING_RING', 'CAPTIVE_SHAFT'][index]}",
        "secondEndMetricOperation": f"MetricSecondEndOperation.{['TAPPED_HOLE', 'E_CLIP', 'CAPTIVE_SHAFT'][index]}",
    }
    operation = str(evaluate(f"getEndOperation({shaft_definition(**overrides)}, ShaftEnd.SECOND)", SHAFT)).split(".")[-1]
    assert shaft_is("isTappedSecondEnd", **overrides) is (operation == "TAPPED_HOLE")
    assert shaft_is("isGrooveSecondEnd", **overrides) is (operation == "RETAINING_RING")


def test_metric_hex_e_clip_grooves():
    # goBILDA's REX with an e-clip: 8mm REX's groove 6 mm across, 3.2 to 4 mm from its end; 12mm's 9 mm, 3 to 4 mm
    for hex_size, diameter, width in [("_7_MM", 6, 0.8), ("_11_MM", 9, 1)]:
        groove = to_python(evaluate(f"getGrooveDefinition(false, HexSize.{hex_size}, UnitSystem.METRIC)", SHAFT))
        assert groove["grooveDiameter"] == pytest.approx(diameter / 1000)
        assert groove["grooveWidth"] == pytest.approx(width / 1000)
        assert groove["endWidth"] == pytest.approx(0.004)


def precondition_parameters(module: str) -> set[str]:
    """The parameters a file's features' preconditions declare (directly or through predicates, in any branch)."""
    from fs_cli.config import load_config
    from fs_lsp.project import Project
    from fs_lsp.project_completion import _definition_parameters

    config = load_config()
    project = Project(config.root, config.code_dir, config.studios_path, config.std_dir)
    return set(_definition_parameters(project, project.module(config.code_dir / module)))


@pytest.mark.parametrize(
    "overrides",
    [
        {"shaftSource": "ShaftSource.COTS", "frcShaft": '{ "vendor" : "WCP", "type" : "Rounded hex", "size" : "1/2 in." } as LookupTablePath'},
        # A spline with a hole through it, and a preset length
        {"shaftSource": "ShaftSource.COTS", "frcShaft": '{ "vendor" : "WCP", "type" : "SplineXS", "material" : "Aluminum" } as LookupTablePath'},
        {"shaftSource": "ShaftSource.COTS", "frcShaft": '{ "vendor" : "ThriftyBot", "type" : "SplineXS stub", "length" : "2 in." } as LookupTablePath'},
        {"program": "Program.FTC", "shaftSource": "ShaftSource.COTS",
         "ftcShaft": '{ "vendor" : "goBILDA", "type" : "8 mm REX (e-clip)", "length" : "48 mm" } as LookupTablePath'},
        {"program": "Program.FTC", "firstEndMetricOperation": "MetricEndOperation.E_CLIP", "firstEndOverrideGroove": "true"},
        {"firstEndOperation": "EndOperation.RETAINING_RING", "firstEndOverrideGroove": "true"},
    ],
)
def test_shaft_editing_logic_returns_only_parameters(overrides):
    # Onshape rejects an editing logic result with any other key ("Editing logic result: Unknown parameter
    # unitSystem"), and applies none of it
    parameters = precondition_parameters(SHAFT)
    overrides = {"firstEndOverrideGroove": "false", "secondEndOverrideGroove": "false", "firstEndSideMount": "false",
                 "secondEndSideMount": "false", **overrides}
    definition = shaft_definition(**overrides)
    assert set(to_python(evaluate(f"keys({definition})", SHAFT))) <= parameters
    old = shaft_definition(**{**overrides, "firstEndOverrideGroove": "false"})
    result = evaluate(f"robotShaftEditLogic(newContext(), newId(), {old}, {definition}, false, {{}}, qNothing())", SHAFT)
    assert set(to_python(evaluate("keys(result)", SHAFT, result=result))) - parameters == set()
