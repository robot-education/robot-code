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


def test_store_screws_replace_the_snapshots():
    screws = DEFINITION["SCREWS"]
    wcp_button = screws[("WCP", "NO_10_32", "BUTTON")]
    # The group's 3/4 in. is WCP-0254, which WCP no longer lists
    assert wcp_button[Fraction(3, 4)].part_number == "WCP-0135"
    assert wcp_button[Fraction(3, 4)].url == "https://wcproducts.com/products/wcp-0135"
    # Sizes the group has none of from WCP, and none of its aluminum or ultra low profile screws
    assert screws[("WCP", "NO_8_32", "BUTTON")][Fraction(1, 2)].part_number == "WCP-0250"
    assert ("WCP", "NO_10_32", "FLAT") not in screws
    assert screws[("ANDYMARK", "QUARTER_20", "BUTTON")][Fraction(1)].part_number == "am-1525"


def test_a_store_screw_without_a_thread_lock_patch_is_preferred():
    # AndyMark sells #10-32 x 1/2 in. socket heads with (am-1744) and without (am-1002) one
    assert DEFINITION["SCREWS"][("ANDYMARK", "NO_10_32", "SOCKET")][Fraction(1, 2)].part_number == "am-1002"


def test_store_titles_parse():
    wcp, andymark = DEFINITION["_STORE_SCREWS"]["WCP"], DEFINITION["_STORE_SCREWS"]["ANDYMARK"]
    assert wcp.match('#10-32 x 4.750 L BHCS (Steel, Black Oxide) (25-Pack)').groups() == ("#10-32", "4.750", "BHCS")
    assert wcp.match('#4-40 x 1/4" BHCS (IP8, Kraken CAN Terminal) (25-Pack)').groups() == ("#4-40", "1/4", "BHCS")
    assert wcp.match('#10-32 x .250" L BHCS (Aluminum, Anodized) (25-Pack)') is None
    assert wcp.match('#10-32 x .375" L SHCS (Steel, Ultra Low Profile) (2-Pack)') is None
    patched = andymark.match("10-32 x 3/8 in. Button Head Cap Screw with Nylon Patch")
    assert patched.groups() == ("10-32", "3/8", "Button Head Cap Screw", " with Nylon Patch")
    assert andymark.match("1/4-20 x 1 in. Pan Head Phillips Screw") is None


def test_screws_come_from_the_snapshot():
    screws = DEFINITION["SCREWS"]
    rev_button = screws[("REV", "NO_10_32", "BUTTON")]
    assert rev_button[Fraction(3, 8)].part_number == "REV-29-2941-PK50"
    # REV only makes #10-32
    assert {size for vendor, size, _ in screws if vendor == "REV"} == {"NO_10_32"}
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
