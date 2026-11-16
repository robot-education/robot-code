"""Checks of bearingTables.py: what it reads from FRCDesign's snapshot, the configuration it lays out, and the manifest
it writes for FRCDesign."""

import json
import pathlib
import runpy

from fs_cli.configured_studios import input_shows, shows

FOLDER = pathlib.Path(__file__).parents[2] / "featurescripts" / "bearings"
DEFINITION = runpy.run_path(str(FOLDER / "bearingTables.py"))
MANIFEST = json.loads((FOLDER / "bearings.gen.json").read_text())
STUDIO = DEFINITION["STUDIOS"]["bearing"]
BEARINGS = DEFINITION["BEARINGS"]


def _shown(input_id: str, selection: dict[str, str]) -> list[str]:
    input = next(input for input in STUDIO.inputs if input.id == input_id)
    if input.fixed or not input_shows(input, selection):
        return []
    return [option.id for option in input.options if shows(option, selection)]


def test_the_configuration_asks_vendor_then_type_bore_size_dimensions_and_seal():
    assert [input.id for input in STUDIO.inputs] == ["vendor", "type", "bore", "size", "dimensions", "seal"]
    assert STUDIO.problems() == []
    # 1/2 in and 3/8 in come first, wherever they're made
    generic_round = {"vendor": "GENERIC", "type": "FLANGED", "bore": "ROUND"}
    assert _shown("size", generic_round)[:2] == ["IN_1_2", "IN_3_8"]
    vex = {"vendor": "VEX", "type": "FLANGED"}
    assert _shown("bore", vex) == ["HEX", "ROUNDED_HEX", "ROUND", "THUNDERSQUARE"]


def test_the_default_is_the_most_inserted_bearing():
    assert STUDIO.defaults() == {
        "vendor": "GENERIC",
        "type": "FLANGED",
        "bore": "ROUNDED_HEX",
        "size": "IN_1_2",
        "dimensions": "OD_1_125_X_0_313",
        "seal": "SHIELDED",
    }


def test_dimensions_and_seal_show_only_where_theres_a_choice():
    rounded_hex = {"vendor": "WCP", "type": "FLANGED", "bore": "ROUNDED_HEX"}
    assert _shown("dimensions", {**rounded_hex, "size": "IN_1_2"}) == ["OD_0_875_X_0_281", "OD_1_X_0_281", "OD_1_125_X_0_313"]
    assert _shown("dimensions", {**rounded_hex, "size": "IN_3_8"}) == []
    andymark = {"vendor": "ANDYMARK", "type": "FLANGED", "bore": "ROUND", "size": "IN_3_8", "dimensions": "OD_0_875_X_0_281"}
    assert _shown("seal", andymark) == ["SHIELDED", "SEALED"]
    # An open FR6 is its own width
    assert _shown("seal", {**andymark, "dimensions": "OD_0_875_X_0_219"}) == []


def test_bearings_come_from_frcdesigns_dimensions():
    wcp = BEARINGS[("WCP", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_X_0_281", "SHIELDED")]
    assert wcp[1].part_number == "WCP-2058"
    assert BEARINGS[("ANDYMARK", "RADIAL", "ROUND", "IN_3_8", "OD_0_875_X_0_219", "OPEN")][1].part_number == "am-0936"
    assert ("ANDYMARK", "RADIAL", "ROUND", "NO_10", "OD_0_375_X_0_156_EXTENDED", "SHIELDED") in BEARINGS
    # FRCDesign has F126ZZ 8 mm wide; it's 4 mm
    assert ("ANDYMARK", "FLANGED", "ROUND", "MM_6", "MM_12_X_4", "SHIELDED") in BEARINGS


def test_generic_makes_every_bearing_by_its_designation():
    made = {key[1:] for key in BEARINGS}
    assert made == {key[1:] for key in BEARINGS if key[0] == "GENERIC"}
    generic = {key[1:]: stock for key, (_, stock) in BEARINGS.items() if key[0] == "GENERIC"}
    assert generic[("FLANGED", "HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")].part_number == "FR8ZZ-HexHD"
    assert generic[("FLANGED", "ROUNDED_HEX", "IN_3_8", "OD_0_875_X_0_281", "SHIELDED")].part_number is None
    assert all(stock.url is None for stock in generic.values())


def test_names():
    name = DEFINITION["bearing_name"]
    bearing = BEARINGS[("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")][0]
    assert name(bearing) == "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)"
    sealed = BEARINGS[("GENERIC", "RADIAL", "ROUND", "IN_3_8", "OD_0_875_X_0_281", "SEALED")][0]
    assert name(sealed) == "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)"
    metric = BEARINGS[("GENERIC", "FLANGED", "ROUND", "MM_8", "MM_16_X_5", "SHIELDED")][0]
    assert name(metric) == "8 mm Round Flanged Bearing (16 mm OD x 5 mm)"


def test_the_manifest_has_a_record_per_configuration():
    studio = MANIFEST["studios"]["bearing"]
    assert studio["feature"] == "libraryBearing" and studio["mateConnector"] == "mateConnector"
    values = [record["values"] for record in studio["records"]]
    assert len(values) == len({json.dumps(value, sort_keys=True) for value in values})
    assert sorted(json.dumps(value, sort_keys=True) for value in values) == sorted(
        json.dumps(combination, sort_keys=True) for combination in STUDIO.combinations()
    )
    default = next(record for record in studio["records"] if record["values"] == {"vendor": "GENERIC", "type": "FLANGED", "bore": "ROUNDED_HEX", "size": "IN_1_2", "dimensions": "OD_1_125_X_0_313"})
    assert default["name"] == "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)"
