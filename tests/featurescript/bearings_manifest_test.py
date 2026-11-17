"""Checks of bearingTables.py: what it reads from FRCDesign's snapshot and goBILDA's catalog, the configurations it lays
out for each part set, and the manifests it writes for FRCDesign (FRC's, and FTC's through ftcBearingTables.py)."""

import json
import pathlib
import runpy

import pytest

from fs_cli.configured_studios import input_shows, shows

FOLDER = pathlib.Path(__file__).parents[2] / "featurescripts" / "bearings"
DEFINITION = runpy.run_path(str(FOLDER / "bearingTables.py"))
MANIFEST = json.loads((FOLDER / "bearings.gen.json").read_text())
FTC_MANIFEST = json.loads((FOLDER / "ftcBearings.gen.json").read_text())
STUDIO = DEFINITION["STUDIOS"]["bearing"]
FTC_STUDIO = DEFINITION["FTC"].studios["bearing"]
BEARINGS = DEFINITION["FRC_BEARINGS"]
FTC_BEARINGS = DEFINITION["FTC_BEARINGS"]


def _shown(input_id: str, selection: dict[str, str], studio=STUDIO) -> list[str]:
    input = next(input for input in studio.inputs if input.id == input_id)
    if input.fixed or not input_shows(input, selection):
        return []
    return [option.id for option in input.options if shows(option, selection)]


def test_the_configuration_asks_vendor_then_type_bore_size_dimensions_and_seal():
    assert [input.id for input in STUDIO.inputs] == ["vendor", "type", "bore", "size", "dimensions", "seal"]
    # 1/2 in and 3/8 in come first, wherever they're made
    generic_round = {"vendor": "GENERIC", "type": "FLANGED", "bore": "ROUND"}
    assert _shown("size", generic_round)[:2] == ["IN_1_2", "IN_3_8"]
    wcp = {"vendor": "WCP", "type": "FLANGED"}
    assert _shown("bore", wcp) == ["HEX", "ROUNDED_HEX", "ROUND"]


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
    assert BEARINGS[("GENERIC", "RADIAL", "ROUND", "IN_3_8", "OD_0_875_X_0_219", "OPEN")][1].part_number == "R6"
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


@pytest.mark.parametrize("manifest, studio", [(MANIFEST, STUDIO), (FTC_MANIFEST, FTC_STUDIO)])
def test_the_manifest_has_a_record_per_configuration(manifest, studio):
    assert studio.problems() == []
    names = manifest["studios"]["bearing"]
    assert names["feature"] == "libraryBearing"
    assert (names["mateConnector"], names["topMateConnector"], names["bottomMateConnector"]) == (
        "mateConnector",
        "topMateConnector",
        "bottomMateConnector",
    )
    values = [record["values"] for record in names["records"]]
    assert len(values) == len({json.dumps(value, sort_keys=True) for value in values})
    assert sorted(json.dumps(value, sort_keys=True) for value in values) == sorted(
        json.dumps(combination, sort_keys=True) for combination in studio.combinations()
    )


def test_ftc_offers_gobilda_rev_and_andymarks_ftc_bearings():
    assert {key[0] for key in FTC_BEARINGS} == {"GOBILDA", "REV", "ANDYMARK", "MCMASTER"}
    assert "GOBILDA" not in {key[0] for key in BEARINGS}
    assert [option.id for option in FTC_STUDIO.inputs[0].options] == ["GOBILDA", "REV", "ANDYMARK", "MCMASTER"]
    assert FTC_STUDIO.defaults() == {
        "vendor": "GOBILDA",
        "type": "FLANGED",
        "bore": "ROUND",
        "size": "MM_8",
        "dimensions": "MM_14_X_5",
        "seal": "SHIELDED",
    }
    assert _shown("bore", {"vendor": "GOBILDA", "type": "FLANGED"}, FTC_STUDIO) == ["ROUND", "REX"]
    # REV's Through Bore Bearings, short and long
    assert _shown("dimensions", {"vendor": "REV", "type": "BUSHING"}, FTC_STUDIO) == ["MM_9_X_5", "MM_9_X_11"]
    rex = FTC_BEARINGS[("GOBILDA", "FLANGED", "REX", "MM_8", "MM_14_X_5", "SHIELDED")][1]
    assert (rex.part_number, rex.url) == (
        "1611-0514-4008",
        "https://www.gobilda.com/1611-series-flanged-ball-bearing-8mm-rex-id-x-14mm-od-5mm-thickness-2-pack/",
    )
    records = {record.get("partNumber"): record for record in FTC_MANIFEST["studios"]["bearing"]["records"]}
    assert records["1611-0514-4008"]["name"] == "8 mm REX Flanged Bearing (14 mm OD x 5 mm)"
    assert records["REV-41-1326"]["name"] == "5 mm Hex Flanged Bushing (9 mm OD x 5 mm)"
    assert records["REV-41-1326"]["material"] == "Acetal" and records["am-3377"]["material"] == "Steel"


def test_flanges_are_each_vendors_where_its_model_differs():
    flange = DEFINITION["flange"]
    assert flange("REV", "IN_1_2", "OD_1_X_0_281") == ("1.0625", "0.062")
    assert flange("ANDYMARK", "IN_1_2", "OD_1_125_X_0_313") == ("1.225", "0.06")
    assert flange("WCP", "IN_1_2", "OD_1_125_X_0_313") == ("1.225", "0.063")


def test_a_radial_bearings_top_connector_is_its_seat():
    records = MANIFEST["studios"]["bearing"]["records"]
    radial = [record for record in records if "Radial" in record["name"]]
    flanged = [record for record in records if "Flanged" in record["name"]]
    assert radial and all(record["topMateConnector"] == "mateConnector" for record in radial)
    assert flanged and all("topMateConnector" not in record for record in flanged)


def test_the_default_record_is_the_most_inserted_bearing():
    records = MANIFEST["studios"]["bearing"]["records"]
    shown = {"vendor": "GENERIC", "type": "FLANGED", "bore": "ROUNDED_HEX", "size": "IN_1_2", "dimensions": "OD_1_125_X_0_313"}
    default = next(record for record in records if record["values"] == shown)
    assert default["name"] == "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)"


def test_discontinued_parts_are_dropped():
    sold = {(key[0], stock.part_number) for key, (_, stock) in BEARINGS.items()}
    assert not sold & DEFINITION["DISCONTINUED"]
    # Generic still makes a standard bearing only a discontinued part was, by its designation: AndyMark's 1621ZZ
    generic = BEARINGS[("GENERIC", "RADIAL", "ROUND", "IN_1_2", "OD_1_375_X_0_438", "SHIELDED")][1]
    assert generic.part_number == "1621ZZ"
    assert ("ANDYMARK", "RADIAL", "ROUND", "IN_1_2", "OD_1_375_X_0_438", "SHIELDED") not in BEARINGS


def test_mcmaster_carrs_rows_are_vendor_and_generic_bearings():
    # Its cheapest of a bearing's rows: a 5 x 16 x 5 mm shielded bearing in 440C stainless, not steel at three times the price
    radial = DEFINITION["MCMASTER_BEARINGS"][("MCMASTER", "RADIAL", "ROUND", "MM_5", "MM_16_X_5", "SHIELDED")][1]
    assert (radial.part_number, radial.material) == ("6153K15", "STAINLESS_STEEL")
    # Nothing over $20 no other vendor sells: its flanged 6805, $48.69, isn't offered, by it or Generic
    assert ("MCMASTER", "FLANGED", "ROUND", "MM_25", "MM_37_X_7", "SHIELDED") not in BEARINGS
    assert ("GENERIC", "FLANGED", "ROUND", "MM_25", "MM_37_X_7", "SHIELDED") not in BEARINGS
    # but its 6807, $24.24, is, as REV and ThriftyBot sell it too
    assert BEARINGS[("MCMASTER", "RADIAL", "ROUND", "MM_35", "MM_47_X_7", "SHIELDED")][1].part_number == "6656K216"
    # Its ABEC 3 608 costs less than its ABEC 1
    assert BEARINGS[("MCMASTER", "RADIAL", "ROUND", "MM_8", "MM_22_X_7", "SHIELDED")][1].part_number == "6661K12"
    # A shielded R8 beside its open one: Seal shows both
    r8 = {"vendor": "MCMASTER", "type": "RADIAL", "bore": "ROUND", "size": "IN_1_2"}
    assert "OD_1_125_X_0_313" in _shown("dimensions", r8)
    record = next(r for r in MANIFEST["studios"]["bearing"]["records"] if r.get("partNumber") == "6153K15")
    assert record["material"] == "Stainless Steel"
    # A 5 mm bore, which clears a #10 screw, shielded or open
    shielded = BEARINGS[("MCMASTER", "FLANGED", "ROUND", "MM_5", "MM_14_X_5", "SHIELDED")][1]
    assert (shielded.part_number, shielded.url) == ("57155K577", "https://www.mcmaster.com/57155K577/")
    assert _shown("seal", {"vendor": "MCMASTER", "type": "FLANGED", "bore": "ROUND", "size": "MM_5", "dimensions": "MM_14_X_5"}) == [
        "SHIELDED",
        "OPEN",
    ]
    assert DEFINITION["flange"]("MCMASTER", "MM_5", "MM_14_X_5") == ("16", "1")
    # Generic offers a size only McMaster-Carr sells, with its flange; FTC's catalog its metric rows alone
    assert ("GENERIC", "FLANGED", "ROUND", "MM_5", "MM_8_X_2", "OPEN") in BEARINGS
    assert DEFINITION["flange"]("GENERIC", "MM_5", "MM_8_X_2") == ("9.2", "0.6")
    assert ("MCMASTER", "RADIAL", "ROUND", "MM_5", "MM_19_X_6", "OPEN") in FTC_BEARINGS
    assert not [key for key in FTC_BEARINGS if key[0] == "MCMASTER" and key[3].startswith("IN_")]


def test_vex_and_vendorless_bearings_are_gone():
    assert "VEX" not in {key[0] for key in BEARINGS}
    # VEX's ThunderSquare and WCP's 3/4 in flanged bearing were sold by no one else, and have no designation
    assert not [key for key in BEARINGS if key[3] == "IN_3_4" and key[1] == "FLANGED"]
    # A vendor's own bearing is still Generic's while someone sells it: WCP's rounded hex
    assert ("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED") in BEARINGS


def test_rev_through_bore_bearings_are_bushings():
    bushings = [key for key in FTC_BEARINGS if key[1] == "BUSHING"]
    assert {FTC_BEARINGS[key][1].part_number for key in bushings} == {"REV-41-1326", "REV-41-1329"}
    assert _shown("type", {"vendor": "REV"}, FTC_STUDIO) == ["FLANGED", "BUSHING"]
