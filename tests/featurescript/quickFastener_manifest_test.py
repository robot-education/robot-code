"""Checks of quickFastenerTables.py: what it reads from FRCDesign's snapshot, the configurations it lays out, and the
manifest it writes for FRCDesign."""

import json
import pathlib
import runpy
from fractions import Fraction

import pytest

from fs_cli.configured_studios import shows

FOLDER = pathlib.Path(__file__).parents[2] / "featurescripts" / "quickFastener"
DEFINITION = runpy.run_path(str(FOLDER / "quickFastenerTables.py"))
MANIFEST = json.loads((FOLDER / "quickFastener.gen.json").read_text())


def test_hole_scan_is_the_function_body_as_a_lambda():
    scan = MANIFEST["holeScan"]
    assert scan.startswith("function(context is Context, queries is map)\n{\n")
    assert scan.endswith("\n}")
    assert 'return { "parts" : parts, "circles" : circles };' in scan


def test_hole_scan_needs_its_signature():
    with pytest.raises(ValueError, match="quickFastenerHoleScan"):
        DEFINITION["hole_scan_lambda"]("export function other() {}\n")


def test_screws_come_from_the_snapshot():
    screws = DEFINITION["SCREWS"]
    rev_button = screws[("REV", "NO_10_32", "BUTTON")]
    assert rev_button[Fraction(3, 8)].part_number == "REV-29-2941-PK50"
    # WCP makes no flat heads, and REV and WCP only #10-32
    assert ("WCP", "NO_10_32", "FLAT") not in screws
    assert {size for vendor, size, _ in screws if vendor in ("REV", "WCP")} == {"NO_10_32"}
    # Generic makes every length anyone does, without part numbers
    generic = screws[("GENERIC", "NO_10_32", "BUTTON")]
    assert set(rev_button) <= set(generic)
    assert all(stock.part_number is None for stock in generic.values())


def test_lengths_show_only_where_theyre_made():
    screw = DEFINITION["STUDIOS"]["screw"]
    length = next(input for input in screw.inputs if input.id == "length")
    for (vendor, size, head), stocked in DEFINITION["SCREWS"].items():
        selection = {"vendor": vendor, "size": size, "head": head}
        shown = {option.id for option in length.options if shows(option, selection)}
        assert shown == {DEFINITION["length_id"](each) for each in stocked}, selection


def test_length_names_and_ids():
    assert DEFINITION["length_id"](Fraction(3, 8)) == "IN_0_375"
    assert DEFINITION["length_name"](Fraction(3, 8)) == "3/8 in."
    assert DEFINITION["length_name"](Fraction(5, 4)) == "1-1/4 in."
    assert DEFINITION["length_name"](Fraction(2)) == "2 in."
    assert DEFINITION["parse_inches"]("1-1/4") == Fraction(5, 4)
    assert DEFINITION["parse_inches"]("0.031") == Fraction("0.031")


def test_washers_and_nuts_fit_their_screws():
    by_id = {size["id"]: size for size in MANIFEST["sizes"]}
    for washer in MANIFEST["washers"]:
        assert washer["innerDiameter"] > by_id[washer["size"]]["majorDiameter"]
        assert washer["outerDiameter"] > washer["innerDiameter"]
    for nut in MANIFEST["nuts"]:
        assert nut["acrossFlats"] > by_id[nut["size"]]["majorDiameter"]
    for size in MANIFEST["sizes"]:
        assert size["tapDrillDiameter"] < size["majorDiameter"] < size["closeClearanceDiameter"]
        assert size["closeClearanceDiameter"] < size["freeClearanceDiameter"]
