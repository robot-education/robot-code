"""Checks of fastenerTables.py: what it reads from FRCDesign's snapshot, the configurations it lays out, and the
manifest it writes for FRCDesign."""

import json
import pathlib
import runpy
from fractions import Fraction

import pytest

from fs_cli.configured_studios import input_shows, shows

FOLDER = pathlib.Path(__file__).parents[2] / "featurescripts" / "fasteners"
DEFINITION = runpy.run_path(str(FOLDER / "fastenerTables.py"))
MANIFEST = json.loads((FOLDER / "fasteners.gen.json").read_text())


def test_hole_scan_is_the_function_body_as_a_lambda():
    scan = MANIFEST["holeScan"]
    assert scan.startswith("function(context is Context, queries is map)\n{\n")
    assert scan.endswith("\n}")
    assert 'return { "parts" : parts, "circles" : circles };' in scan


def test_hole_scan_needs_its_signature():
    with pytest.raises(ValueError, match="fastenerHoleScan"):
        DEFINITION["hole_scan_lambda"]("export function other() {}\n")


STANDARD = DEFINITION["STANDARD"]
Variant = DEFINITION["Variant"]


def test_store_screws_replace_the_snapshots():
    screws = DEFINITION["SCREWS"]
    wcp_button = screws[("WCP", "NO_10_32", "BUTTON")]
    # The group's 3/4 in. is WCP-0254, which WCP no longer lists
    assert wcp_button[Fraction(3, 4)][STANDARD].part_number == "WCP-0135"
    assert wcp_button[Fraction(3, 4)][STANDARD].url == "https://wcproducts.com/products/wcp-0135"
    # Sizes the group has none of from WCP, and its aluminum screws, but none of its ultra low profile ones
    assert screws[("WCP", "NO_8_32", "BUTTON")][Fraction(1, 2)][STANDARD].part_number == "WCP-0250"
    aluminum = wcp_button[Fraction(1, 4)][Variant("ALUMINUM")]
    assert (aluminum.part_number, aluminum.material.name) == ("WCP-2110", "Aluminum - 6061")
    assert ("WCP", "NO_10_32", "FLAT") not in screws
    assert screws[("ANDYMARK", "QUARTER_20", "BUTTON")][Fraction(1)][STANDARD].part_number == "am-1525"


def test_a_store_screw_without_a_thread_lock_patch_is_preferred():
    # AndyMark sells #10-32 x 1/2 in. socket heads with (am-1744) and without (am-1002) one
    socket = DEFINITION["SCREWS"][("ANDYMARK", "NO_10_32", "SOCKET")]
    assert socket[Fraction(1, 2)][STANDARD].part_number == "am-1002"


def test_store_titles_parse():
    wcp, andymark = DEFINITION["_STORE_SCREWS"]["WCP"], DEFINITION["_STORE_SCREWS"]["ANDYMARK"]
    steel = wcp.match('#10-32 x 4.750 L BHCS (Steel, Black Oxide) (25-Pack)')
    assert steel.groupdict() == {"size": "#10-32", "length": "4.750", "head": "BHCS", "finish": "Steel, Black Oxide"}
    kraken = wcp.match('#4-40 x 1/4" BHCS (IP8, Kraken CAN Terminal) (25-Pack)')
    assert kraken.groupdict() == {"size": "#4-40", "length": "1/4", "head": "BHCS", "finish": None}
    assert wcp.match('#10-32 x .250" L BHCS (Aluminum, Anodized) (25-Pack)')["finish"] == "Aluminum, Anodized"
    assert wcp.match('#10-32 x .375" L SHCS (Steel, Ultra Low Profile) (2-Pack)') is None
    patched = andymark.match("10-32 x 3/8 in. Button Head Cap Screw with Nylon Patch")
    assert patched.groupdict() == {
        "size": "10-32",
        "length": "3/8",
        "head": "Button Head Cap Screw",
        "patch": " with Nylon Patch",
    }
    assert andymark.match("1/4-20 x 1 in. Pan Head Phillips Screw") is None


def test_screws_come_from_the_snapshot():
    screws = DEFINITION["SCREWS"]
    rev_button = screws[("REV", "NO_10_32", "BUTTON")]
    assert rev_button[Fraction(3, 8)][STANDARD].part_number == "REV-29-2941-PK50"
    assert rev_button[Fraction(3, 8)][Variant("ZINC_PLATED")].part_number == "REV-29-1221-PK100"
    # REV only makes #10-32
    assert {size for vendor, size, _ in screws if vendor == "REV"} == {"NO_10_32"}
    # Generic makes every length anyone does, without part numbers, and only in the standard variant
    generic = screws[("GENERIC", "NO_10_32", "BUTTON")]
    assert set(rev_button) <= set(generic)
    assert all(list(variants) == [STANDARD] for variants in generic.values())
    assert all(variants[STANDARD].part_number is None for variants in generic.values())


def test_mcmaster_carrs_long_socket_heads_come_fully_threaded_too():
    socket = DEFINITION["SCREWS"][("MCMASTER", "NO_8_32", "SOCKET")]
    assert socket[Fraction(2)][STANDARD].part_number == "91251A205"
    assert socket[Fraction(2)][Variant(full_thread=True)].part_number == "90044A113"
    assert list(socket[Fraction(1, 2)]) == [STANDARD, Variant("ZINC_PLATED")]


def test_variant_shows_only_where_more_than_the_standard_is_made():
    screw = DEFINITION["STUDIOS"]["screw"]
    variant = next(input for input in screw.inputs if input.id == "variant")
    length_id = DEFINITION["length_id"]
    for (vendor, size, head), stocked in DEFINITION["SCREWS"].items():
        for length, variants in stocked.items():
            selection = {"vendor": vendor, "size": size, "head": head, "length": length_id(length)}
            if list(variants) == [STANDARD]:
                assert not input_shows(variant, selection)
            else:
                assert {option.id for option in variant.options if shows(option, selection)} == {
                    each.id for each in variants
                }


def test_washer_finishes():
    washers = DEFINITION["WASHERS"]
    assert washers[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "BLACK")].stock.part_number == "96765A125"
    plain = washers[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "STAINLESS")]
    assert (plain.stock.part_number, plain.stock.material.name) == ("92141A011", "Stainless Steel")
    # WCP's black zinc washers are its standard black
    assert ("WCP", "NO_10_32", "OD_3_8_X_0_031", "BLACK") in washers
    records = {tuple(record["values"].values()): record for record in MANIFEST["studios"]["washer"]["records"]}
    assert records[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "STAINLESS")]["name"] == (
        "#10 Washer (7/16 in. OD x 0.03 in., Stainless steel)"
    )
    # Hidden where only black is made
    assert ("GENERIC", "NO_10_32", "OD_1_2_X_0_049") in records


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


@pytest.mark.parametrize("kind", ["screw", "nut", "washer"])
def test_each_configuration_has_one_record(kind):
    """FRCDesignApp indexes these instead of asking Onshape, so they must be exactly what the configuration offers."""
    records = [record["values"] for record in MANIFEST["studios"][kind]["records"]]
    assert sorted(map(str, records)) == sorted(map(str, DEFINITION["STUDIOS"][kind].combinations()))


def test_records_name_and_sell_each_part():
    records = {tuple(record["values"].values()): record for record in MANIFEST["studios"]["screw"]["records"]}
    mcmaster = records[("MCMASTER", "NO_10_32", "BUTTON", "IN_0_5", "STANDARD")]
    assert mcmaster["name"] == "#10-32 x 1/2 in. Button Head Screw"
    assert mcmaster["vendor"] == "McMaster-Carr"
    assert mcmaster["partNumber"] == "91255A265"
    assert mcmaster["material"] == "Steel"
    zinc = records[("MCMASTER", "NO_10_32", "BUTTON", "IN_0_5", "ZINC_PLATED")]
    assert zinc["name"] == "#10-32 x 1/2 in. Button Head Screw (Zinc plated)"
    # Generic parts have no vendor or part number, nor a variant
    generic = records[("GENERIC", "NO_10_32", "BUTTON", "IN_0_5")]
    assert "vendor" not in generic and "partNumber" not in generic
