"""Checks of quickFastenerTables.py's manifest for FRCDesign, and the configurations it lays out."""

import json
import pathlib
import runpy

import pytest

FOLDER = pathlib.Path(__file__).parents[2] / "featurescripts" / "quickFastener"
DEFINITION = runpy.run_path(str(FOLDER / "quickFastenerTables.py"))
MANIFEST = json.loads((FOLDER / "quickFastener.gen.json").read_text())


def test_hole_scan_is_the_function_body_as_a_lambda():
    scan = MANIFEST["holeScan"]
    assert scan.startswith("function(context is Context, queries is map)\n{\n")
    assert scan.endswith("\n}")
    assert "return { \"parts\" : parts, \"circles\" : circles };" in scan


def test_hole_scan_needs_its_signature():
    with pytest.raises(ValueError, match="quickFastenerHoleScan"):
        DEFINITION["hole_scan_lambda"]("export function other() {}\n")


@pytest.mark.parametrize("program", ["frc", "ftc"])
def test_every_length_of_a_size_shows_with_it(program):
    [entry] = [entry for entry in MANIFEST["programs"] if entry["id"] == program]
    inputs = {input["id"]: input for input in entry["studios"]["screw"]["inputs"]}
    shown = {
        size: {
            option["id"]
            for option in inputs["length"]["options"]
            if "shownWith" not in option or size in option["shownWith"]
        }
        for size in entry["sizes"]
    }
    for size in MANIFEST["sizes"]:
        if size["program"] == program:
            assert shown[size["id"]] == {length["id"] for length in size["lengths"]}


def test_length_names_and_ids():
    size = DEFINITION["SIZES_BY_ID"]["NO_10_32"]
    assert DEFINITION["length_id"](size, 0.375) == "IN_0_375"
    assert DEFINITION["length_name"](size, 0.375) == "3/8 in."
    assert DEFINITION["length_name"](size, 1.25) == "1-1/4 in."
    assert DEFINITION["length_name"](size, 2) == "2 in."
    m3 = DEFINITION["SIZES_BY_ID"]["M3"]
    assert (DEFINITION["length_id"](m3, 10), DEFINITION["length_name"](m3, 10)) == ("MM_10", "10 mm")


def test_washers_and_nuts_fit_their_screws():
    by_id = {size["id"]: size for size in MANIFEST["sizes"]}
    for washer in MANIFEST["washers"]:
        for size, dimensions in washer["sizes"].items():
            assert dimensions["innerDiameter"] > by_id[size]["majorDiameter"]
            assert dimensions["outerDiameter"] > dimensions["innerDiameter"]
    for nut in MANIFEST["nuts"]:
        for size, dimensions in nut["sizes"].items():
            assert dimensions["acrossFlats"] > by_id[size]["majorDiameter"]
    for size in MANIFEST["sizes"]:
        assert size["tapDrillDiameter"] < size["majorDiameter"] < size["closeClearanceDiameter"]
        assert size["closeClearanceDiameter"] < size["freeClearanceDiameter"]
