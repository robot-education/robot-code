"""Tests for `fs_cli.configurations`: Part Studio configurations, read from Onshape's JSON, shown as Onshape's panel
would (in Python for VS Code's preview, and by `RUNTIME` on `fs audit`'s page), for a part set's preview. They build
their own studios, so that changing a part set doesn't change what these see."""

import json
import pathlib
import shutil
import subprocess
import types

import pytest

from fs_cli.configurations import (
    ConfigurationError,
    configuration_page,
    configuration_script,
    inputs_html,
    parse_configuration,
    part_set_views,
)
from fs_cli.configured_studios import ConfiguredStudio, Input, LookupTable, Option

# What's made: vendor, size, and finish
MADE = [("A", "S1", "BLACK"), ("A", "S2", "BLACK"), ("A", "S2", "ZINC"), ("B", "S1", "ZINC")]


def studio() -> ConfiguredStudio:
    table = LookupTable(("vendor", "size", "finish"), MADE)

    def spec(id: str, values: list[str]) -> Input:
        return table.input(Input(id, id.capitalize(), id, [Option(value, value.title(), value) for value in values], enum_name="E"))

    return ConfiguredStudio(
        "part",
        "feature",
        [
            spec("vendor", ["A", "B"]),
            spec("size", ["S1", "S2"]),
            spec("finish", ["BLACK", "ZINC"]),
            Input("washer", "Washer", "washer", kind="boolean", shown_when=[{"vendor": "A"}]),
        ],
    )


def shown(configuration, settings) -> dict[str, str]:
    return {item.input.id: item.value for item in configuration.state(settings)}


def test_state_shows_what_the_configuration_offers():
    configuration = parse_configuration(studio().configuration_body())
    # Each configuration the studio offers shows its own choices, and nothing more (its checkbox aside)
    for combination in studio().combinations():
        state = shown(configuration, combination)
        assert {key: value for key, value in state.items() if key != "washer"} == {
            key: value for key, value in combination.items() if key != "washer"
        }
    # B makes one size and one finish, so neither shows; A's S2 comes in two finishes
    assert shown(configuration, {"vendor": "B"}) == {"vendor": "B"}
    assert shown(configuration, {"vendor": "A", "size": "S2"}) == {"vendor": "A", "size": "S2", "finish": "BLACK", "washer": "false"}
    finish = next(item for item in configuration.state({"size": "S2"}) if item.input.id == "finish")
    assert finish.options == (("BLACK", "Black"), ("ZINC", "Zinc"))


def test_a_hidden_choice_shows_the_first_shown_option():
    configuration = parse_configuration(studio().configuration_body())
    # Zinc was chosen, then a size made only in black
    assert shown(configuration, {"size": "S1", "finish": "ZINC"}) == {"vendor": "A", "size": "S1", "washer": "false"}
    assert configuration.values({"vendor": "A", "size": "S2", "finish": "GOLD"})["finish"] == "BLACK"


def test_conditions_and_kinds():
    body = {
        "configurationParameters": [
            {"btType": "BTMConfigurationParameterBoolean-2550", "parameterId": "on", "parameterName": "On", "defaultValue": True},
            {
                "btType": "BTMConfigurationParameterEnum-105",
                "parameterId": "pick",
                "parameterName": "Pick",
                "defaultValue": "X",
                "options": [{"option": "X", "optionName": "Ex"}, {"option": "Y", "optionName": "Why"}],
                "visibilityCondition": {
                    "btType": "BTParameterVisibilityLogical-178",
                    "operation": "AND",
                    "children": [{"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "on", "value": True}],
                },
            },
        ]
    }
    configuration = parse_configuration(body)
    assert [item.input.id for item in configuration.state({})] == ["on", "pick"]
    assert [item.input.id for item in configuration.state({"on": "false"})] == ["on"]
    unknown = {"configurationParameters": [{"btType": "BTMConfigurationParameterString-872", "parameterId": "s"}]}
    with pytest.raises(ConfigurationError, match="can't show"):
        parse_configuration(unknown)


def test_page_sets_inputs_when_clicked():
    configuration = parse_configuration(studio().configuration_body())
    page = configuration_page("Part", configuration, {"vendor": "A", "size": "S2"})
    assert "data-set='finish' data-value='ZINC'" in page
    assert "data-set='washer' data-value='true'" in page
    assert "<span class='ns-dialog-title'>Part</span>" in page


@pytest.mark.skipif(shutil.which("node") is None, reason="needs Node.js")
def test_the_pages_script_draws_what_python_does(tmp_path):
    configuration = parse_configuration(studio().configuration_body())
    settings = [{}, {"vendor": "B"}, {"vendor": "A", "size": "S2"}, {"size": "S1", "finish": "ZINC"}, {"washer": "true"}]
    script = tmp_path / "panel.js"
    script.write_text(configuration_script(configuration) + f"\nconsole.log(JSON.stringify({json.dumps(settings)}.map((s) => renderList(s).html)));\n")
    drawn = json.loads(subprocess.run(["node", str(script)], capture_output=True, text=True, check=True).stdout)
    assert drawn == [inputs_html(configuration.state(each)) for each in settings]


def test_part_set_views_are_the_studios_using_a_files_features(tmp_path):
    folder = tmp_path / "parts"
    folder.mkdir()
    (folder / "parts.fs").write_text("export const feature = defineFeature(function(context, id, definition) {});\n")
    (folder / "other.fs").write_text("export const elsewhere = defineFeature(function(context, id, definition) {});\n")
    (folder / "partTables.py").write_text(
        "import runpy\n"
        f"STUDIOS = {{'part': runpy.run_path({str(pathlib.Path(__file__))!r})['studio']()}}\n"
    )
    part_set = types.SimpleNamespace(title="Parts", folder="parts", definition="partTables.py")
    config = types.SimpleNamespace(parts=[part_set], code_dir=tmp_path)
    [view] = part_set_views(config, folder / "parts.fs")
    assert (view.label, view.tab) == ("Parts: Part", "Part")
    assert part_set_views(config, folder / "other.fs") == []


def test_audit_page_shows_each_studios_panel_in_the_dialogs_place():
    from fs_cli.audit import audit_page, split_page

    configuration = parse_configuration(studio().configuration_body())
    shell = split_page(configuration_page("Part", configuration, {}))[0]
    panels = [("Parts: Part", shell, configuration_script(configuration)), ("FTC Parts: Part", shell, configuration_script(configuration))]
    page = audit_page("Feature", "feature.fs", "", "", None, [], "dark", configurations=panels)
    assert page.count('class="dialog-frame"') == 2
    assert "<h2>Configurations</h2>" in page and "<h3>FTC Parts: Part</h3>" in page
    assert "<h2>Dialog</h2>" not in page
