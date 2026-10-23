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
    # A Kraken's spares, whose drive isn't a hex socket
    assert wcp.match('#4-40 x 1/4" BHCS (IP8, Kraken CAN Terminal) (25-Pack)') is None
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


def test_only_a_stores_catalog_screws_are_kept():
    store_screw = DEFINITION["_store_screw"]

    def product(title: str, sku: str = "am-1", url: str = "https://andymark.com/products/x") -> dict:
        return {"store": "andymark.com", "title": title, "sku": sku, "url": url}

    assert store_screw(product("10-32 x 0.5 in. Socket Head Cap Screw")) is not None
    # Listed again to ship from abroad, metric, or not a cap screw
    assert store_screw(product("Ships from Sydney - 10-32 x 0.5 in. Socket Head Cap Screw", "am-1AUS")) is None
    assert store_screw(product("M3-0.5 x 10 mm Socket Head Cap Screw")) is None
    assert store_screw(product("10-12 x 1.25 in. Self Piercing Hex Head Cap Screw")) is None
    # A zinc plated one, named so only in its url
    zinc = store_screw(product("1/4-20 x 0.75 in. Button Head Cap Screw", url="https://andymark.com/products/screw-bhcs-1-4-20-x-750-zinc"))
    assert zinc.variant == Variant("ZINC_PLATED")


def test_the_stores_snapshot_holds_only_catalog_screws():
    snapshot = json.loads(DEFINITION["STORES_SNAPSHOT"].read_text())
    assert all(DEFINITION["_store_screw"](product) is not None for product in snapshot["products"])


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


def test_fully_threaded_shows_only_where_a_length_is_sold_both_ways():
    screw = DEFINITION["STUDIOS"]["screw"]
    fully_threaded = next(input for input in screw.inputs if input.id == "fullyThreaded")
    assert screw.inputs[-1] is fully_threaded and fully_threaded.kind == "switch"
    length_id = DEFINITION["length_id"]
    for (vendor, size, head), stocked in DEFINITION["SCREWS"].items():
        for length, variants in stocked.items():
            for variant in variants:
                selection = {
                    "vendor": vendor,
                    "size": size,
                    "head": head,
                    "finish": variant.finish,
                    "length": length_id(length),
                }
                both = Variant(variant.finish) in variants and Variant(variant.finish, True) in variants
                shown = [option.id for option in fully_threaded.options if shows(option, selection)]
                assert shown == (["false", "true"] if both else []), selection


def test_finish_comes_between_head_and_length_and_shows_where_theres_a_choice():
    screw = DEFINITION["STUDIOS"]["screw"]
    assert [input.id for input in screw.inputs] == ["vendor", "size", "head", "finish", "length", "fullyThreaded"]
    finish = screw.inputs[3]
    # Generic's are black alone
    generic = {"vendor": "GENERIC", "size": "NO_10_32", "head": "BUTTON"}
    assert [option.id for option in finish.options if shows(option, generic)] == []
    mcmaster = {"vendor": "MCMASTER", "size": "NO_10_32", "head": "BUTTON"}
    assert [option.id for option in finish.options if shows(option, mcmaster)] == ["BLACK", "ZINC_PLATED"]


def test_washer_finishes():
    washers = DEFINITION["WASHERS"]
    assert washers[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "BLACK")].stock.part_number == "96765A125"
    plain = washers[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "STAINLESS")]
    assert (plain.stock.part_number, plain.stock.material.name) == ("92141A011", "Stainless Steel")
    # WCP's black zinc washers are its standard black, and AndyMark's zink finish is zinc plating
    assert ("WCP", "NO_10_32", "OD_3_8_X_0_031", "BLACK") in washers
    assert washers[("ANDYMARK", "NO_10_32", "OD_3_4_X_0_062", "ZINC_PLATED")].stock.part_number == "am-1523"
    records = {tuple(record["values"].values()): record for record in MANIFEST["studios"]["washer"]["records"]}
    assert records[("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "STAINLESS")]["name"] == (
        "#10 Washer (7/16 in OD x 0.03 in, Stainless steel)"
    )
    # Generic makes one washer in each size, so its type and finish are hidden
    assert records[("GENERIC", "NO_10_32")]["name"] == "#10 Washer (1/2 in OD x 0.049 in)"


def test_andymarks_washers_take_their_stated_bores():
    washers = DEFINITION["WASHERS"]
    quarter = washers[("ANDYMARK", "QUARTER_20", "OD_5_8_X_0_063", "ZINC_PLATED")]
    # FRCDesign's record names it 0.625", its outer diameter
    assert (quarter.stock.part_number, quarter.inner_diameter) == ("am-1027", 0.281)
    assert washers[("ANDYMARK", "QUARTER_20", "OD_3_X_0_06", "ZINC_PLATED")].inner_diameter == 0.252
    # Bores no screw here fits are left out
    assert {washer.stock.part_number for key, washer in washers.items() if key[0] == "ANDYMARK"} == {
        "am-1658",
        "am-1523",
        "am-1575",
        "am-1628",
        "am-1027",
        "am-1421",
    }


def test_lengths_show_only_where_theyre_made():
    screw = DEFINITION["STUDIOS"]["screw"]
    length = next(input for input in screw.inputs if input.id == "length")
    for (vendor, size, head), stocked in DEFINITION["SCREWS"].items():
        for finish in {variant.finish for variants in stocked.values() for variant in variants}:
            selection = {"vendor": vendor, "size": size, "head": head, "finish": finish}
            shown = {option.id for option in length.options if shows(option, selection)}
            made = {DEFINITION["length_id"](each) for each, variants in stocked.items() if Variant(finish) in variants}
            # Hidden where there's one, like REV's zinc plated #10-32 flat heads
            assert shown == (made if len(made) > 1 else set()), selection


def test_length_names_and_ids():
    assert DEFINITION["length_id"](Fraction(3, 8)) == "IN_0_375"
    assert DEFINITION["length_name"](Fraction(3, 8)) == "3/8 in"
    assert DEFINITION["length_name"](Fraction(5, 4)) == "1-1/4 in"
    assert DEFINITION["length_name"](Fraction(2)) == "2 in"
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
    mcmaster = records[("MCMASTER", "NO_10_32", "BUTTON", "BLACK", "IN_0_5")]
    assert mcmaster["name"] == "#10-32 x 1/2 in Button Head Screw"
    assert mcmaster["vendor"] == "McMaster-Carr"
    assert mcmaster["partNumber"] == "91255A265"
    assert mcmaster["material"] == "Steel"
    zinc = records[("MCMASTER", "NO_10_32", "BUTTON", "ZINC_PLATED", "IN_0_5")]
    assert zinc["name"] == "#10-32 x 1/2 in Button Head Screw (Zinc plated)"
    full = records[("MCMASTER", "NO_8_32", "SOCKET", "BLACK", "IN_2", "true")]
    assert (full["name"], full["partNumber"]) == ("#8-32 x 2 in Socket Head Screw (Fully threaded)", "90044A113")
    assert records[("MCMASTER", "NO_8_32", "SOCKET", "BLACK", "IN_2", "false")]["partNumber"] == "91251A205"
    # Generic parts have no vendor or part number, and only one finish, which is hidden
    generic = records[("GENERIC", "NO_10_32", "BUTTON", "IN_0_5")]
    assert "vendor" not in generic and "partNumber" not in generic


# The configuration rules (see docs/fs-cli.md's "Configured Part Studios"), checked on each studio as generated

KINDS = ["screw", "nut", "washer"]
TABLES = {"screw": DEFINITION["SCREW_TABLE"], "nut": DEFINITION["NUT_TABLE"], "washer": DEFINITION["WASHER_TABLE"]}
MADE = {
    "screw": [record[:5] for record in DEFINITION["SCREW_RECORDS"]],
    "nut": list(DEFINITION["NUTS"]),
    "washer": list(DEFINITION["WASHERS"]),
}


@pytest.mark.parametrize("kind", KINDS)
def test_each_studio_keeps_to_the_configuration_rules(kind):
    """What `fs parts` checks before it sends anything to Onshape."""
    assert DEFINITION["STUDIOS"][kind].problems() == []


@pytest.mark.parametrize("kind", KINDS)
def test_an_input_shows_only_with_a_choice_of_options(kind):
    studio = DEFINITION["STUDIOS"][kind]
    for combination in studio.combinations():
        for input in studio.configured_inputs():
            shown = [option.id for option in input.options if input_shows(input, combination) and shows(option, combination)]
            assert len(shown) != 1, (input.id, combination)
            assert (input.id in combination) == bool(shown), (input.id, combination)


@pytest.mark.parametrize("kind", KINDS)
def test_a_hidden_input_has_one_option_made_where_its_hidden(kind):
    """So the feature resolves it to that one, whatever value Onshape's configuration holds."""
    table, made = TABLES[kind], MADE[kind]
    for key in made:
        shown = table.shown(key)
        for index, id in enumerate(table.ids):
            if id not in shown:
                assert {other[index] for other in made if other[:index] == key[:index]} == {key[index]}, (id, key)


@pytest.mark.parametrize("kind", KINDS)
def test_every_option_is_controlled_by_a_rule(kind):
    """Onshape hides every option no rule controls, once an input has any rule."""
    for parameter in DEFINITION["STUDIOS"][kind].configuration_body()["configurationParameters"]:
        rules = parameter.get("enumOptionVisibilityConditions", {}).get("visibilityConditions", [])
        if rules:
            controlled = {option for rule in rules for option in rule["controlledOptions"]}
            assert controlled == {option["option"] for option in parameter["options"]}, parameter["parameterId"]


@pytest.mark.parametrize("kind", KINDS)
def test_each_default_shows_with_the_others(kind):
    """Onshape refuses a configuration whose default hides an input's default while it shows others."""
    studio = DEFINITION["STUDIOS"][kind]
    selection: dict[str, str] = {}
    for input in studio.configured_inputs():
        shown = [option.id for option in input.options if input_shows(input, selection) and shows(option, selection)]
        if shown:
            assert input.default_option in shown, input.id
            selection[input.id] = input.default_option
    assert selection in studio.combinations()


def test_defaults_are_whats_inserted_most():
    assert DEFINITION["STUDIOS"]["screw"].defaults() == {
        "vendor": "GENERIC",
        "size": "NO_10_32",
        "head": "BUTTON",
        "finish": "BLACK",
        "length": "IN_0_375",
        "fullyThreaded": "false",
    }
    assert DEFINITION["STUDIOS"]["nut"].defaults() == {"size": "NO_10_32", "type": "NYLOCK"}
    assert DEFINITION["STUDIOS"]["washer"].defaults() == {
        "vendor": "MCMASTER",
        "size": "NO_10_32",
        "type": "OD_7_16_X_0_03",
        "finish": "STAINLESS",
    }


def test_one_nut_vendor_is_fixed():
    vendor = DEFINITION["STUDIOS"]["nut"].inputs[0]
    assert vendor.fixed and vendor.options[0].id == "MCMASTER"
    assert all("vendor" not in record["values"] for record in MANIFEST["studios"]["nut"]["records"])
