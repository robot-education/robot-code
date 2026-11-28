"""The part sets' parts, built offline with the evaluator's geometry (fs_eval.geometry, as `fs render` builds them):
every configuration builds one part, and the parts measure what their tables say, and what vendors' STEP files do.
Skipped without OpenCascade (`uv sync --group render`)."""

import pathlib

import pytest

pytest.importorskip("OCP", reason="building parts needs OpenCascade: uv sync --group render")

from fs_cli.config import load_config  # noqa: E402
from fs_cli.render import RenderError, build, configuration, configuration_name, feature_definition, part_set_studios  # noqa: E402
from fs_eval import to_python  # noqa: E402
from fs_eval.pytest_plugin import geometry_evaluator  # noqa: E402

ROOT = pathlib.Path(__file__).parents[2]
CONFIG = load_config(ROOT)
STUDIOS = {ref.label: ref for ref in part_set_studios(CONFIG)}
INCH = 0.0254

# Configurations the configuration offers but the feature can't build, as Onshape couldn't either, by studio
KNOWN_UNBUILDABLE: dict[str, int] = {}


def bounds(part):
    from OCP.Bnd import Bnd_Box
    from OCP.BRepBndLib import BRepBndLib

    box = Bnd_Box()
    for body in part.parts:
        BRepBndLib.AddOptimal_s(body.shape, box, False, False)
    return (box.GetXMin(), box.GetYMin(), box.GetZMin()), (box.GetXMax(), box.GetYMax(), box.GetZMax())


def made(ref, combination, table: str, parameters: list[str]) -> dict:
    """What the feature's table makes for a configuration (its `resolveChoices(...).made`), in meters."""
    definition = feature_definition(ref.studio, combination)
    entries = ", ".join(f'"{k}" : {v}' for k, v in definition.items())
    keys = ", ".join(f'"{p}"' for p in parameters)
    value = geometry_evaluator().eval(f"resolveChoices({table}, {{ {entries} }}, [{keys}]).made", ref.module)
    return to_python(value)


@pytest.mark.parametrize("label", sorted(STUDIOS))
def test_every_configuration_builds_one_part(label):
    ref = STUDIOS[label]
    failures = []
    for combination in ref.studio.combinations():
        try:
            part = build(geometry_evaluator(), ref, combination)
        except RenderError as error:
            failures.append(f"{configuration_name(ref.studio, combination)}: {error}")
            continue
        if len(part.parts) != 1:
            failures.append(f"{configuration_name(ref.studio, combination)}: {len(part.parts)} parts")
        if not part.mate_connectors:
            failures.append(f"{configuration_name(ref.studio, combination)}: no mate connector")
    assert len(failures) == KNOWN_UNBUILDABLE.get(label, 0), "\n".join(failures)


@pytest.mark.parametrize("label", ["fasteners/washer", "ftcFasteners/washer"])
def test_washers_measure_their_tables(label):
    ref = STUDIOS[label]
    for combination in ref.studio.combinations():
        washer = made(ref, combination, "WASHERS", ["vendor", "size", "washerType", "finish"])
        lo, hi = bounds(build(geometry_evaluator(), ref, combination))
        assert hi[0] - lo[0] == pytest.approx(washer["outerDiameter"], abs=1e-7)
        assert (lo[2], hi[2]) == pytest.approx((0, washer["thickness"]), abs=1e-7)


@pytest.mark.parametrize("label", ["fasteners/nut", "ftcFasteners/nut"])
def test_nuts_measure_their_tables(label):
    ref = STUDIOS[label]
    for combination in ref.studio.combinations():
        nut = made(ref, combination, "NUTS", ["vendor", "size", "nutType", "finish"])
        lo, hi = bounds(build(geometry_evaluator(), ref, combination))
        # Across its flats along Y (a hex's first corner is on X), from its bearing face at the origin up
        assert hi[1] - lo[1] == pytest.approx(nut["acrossFlats"], abs=1e-6)
        assert (lo[2], hi[2]) == pytest.approx((0, nut["height"]), abs=1e-7)


@pytest.mark.parametrize("label", ["fasteners/screw", "ftcFasteners/screw"])
def test_screws_reach_their_length_below_the_seat(label):
    ref = STUDIOS[label]
    seen = set()
    for combination in ref.studio.combinations():
        key = (combination.get("size"), combination.get("head"))
        if key in seen:
            continue
        seen.add(key)
        definition = feature_definition(ref.studio, combination)
        length = to_python(geometry_evaluator().eval(f"SCREW_LENGTHS[{definition['length']}].length", ref.module))
        lo, hi = bounds(build(geometry_evaluator(), ref, combination))
        # A flat head's length is from its top, flush with the part; the rest from under the head
        assert lo[2] == pytest.approx(-length, abs=1e-7)
        if combination.get("head") == "FLAT":
            assert hi[2] == pytest.approx(0, abs=1e-7)
        else:
            assert hi[2] > 0


def _step_bounds(path: pathlib.Path):
    from OCP.Bnd import Bnd_Box
    from OCP.BRepBndLib import BRepBndLib
    from OCP.STEPControl import STEPControl_Reader

    reader = STEPControl_Reader()
    reader.ReadFile(str(path))
    reader.TransferRoots()
    box = Bnd_Box()
    BRepBndLib.AddOptimal_s(reader.OneShape(), box, False, False)
    sizes = sorted([box.GetXMax() - box.GetXMin(), box.GetYMax() - box.GetYMin(), box.GetZMax() - box.GetZMin()])
    # Its diameter (the largest, twice) and its width, in meters (STEP files here are in millimeters)
    return sizes[2] / 1000, sizes[0] / 1000


@pytest.mark.parametrize("label,choices,step", [
    ("bearings/bearing", {"vendor": "REV", "type": "FLANGED", "bore": "ROUNDED_HEX", "size": "IN_1_2"}, "REV-21-1916.STEP"),
    ("bearings/bearing", {"vendor": "WCP", "type": "FLANGED", "bore": "ROUND", "size": "MM_35"}, "WCP-1471.STEP"),
    ("bearings/specialtyBearing", {"type": "FLANGED", "size": "MM_30"}, "WCP-0299.STEP"),
])
def test_bearings_measure_their_vendors_models(label, choices, step):
    ref = STUDIOS[label]
    try:
        combination = configuration(ref.studio, choices)
    except RenderError as error:
        pytest.fail(f"The configuration this checks isn't offered any more: {error}")
    lo, hi = bounds(build(geometry_evaluator(), ref, combination))
    diameter, width = _step_bounds(CONFIG.code_dir / "bearings" / "vendor" / step)
    assert hi[0] - lo[0] == pytest.approx(diameter, abs=0.05e-3)
    assert hi[2] - lo[2] == pytest.approx(width, abs=0.05e-3)
