"""Tests for `fs table`: lookup tables as flat rows."""

from fs_cli import cli
from fs_cli.lookup_tables import find_tables, flatten, lookup_parameters, options, to_html, to_markdown, to_text, with_values
from fs_eval.pytest_plugin import shared_evaluator

SOURCE = """FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export const sizeTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#10" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32",
                "entries" : {
                    "24" : { "diameter" : 0.19 * inch, "angle" : 30 * degree },
                    "32" : { "diameter" : 0.19 * inch, "color" : color(1, 0, 0.5) },
                },
            },
            "M5" : { "diameter" : 5 * millimeter },
            "none" : {},
        },
    };

export const notATable = { "name" : "size" };
"""


def table():
    evaluator = shared_evaluator()
    module = evaluator.source(SOURCE, "tableTest.fs")
    return flatten("sizeTable", "tableTest.fs", evaluator.eval("sizeTable", module))


def test_flatten():
    flat = table()
    assert flat.levels == ["Size", "Threads/inch"]
    assert flat.fields == ["angle", "diameter", "color"]
    assert [row.path for row in flat.rows] == [["#10", "24"], ["#10", "32"], ["M5"], ["none"]]
    assert [row.defaults for row in flat.rows] == [[True, False], [True, True], [False], [False]]
    # Lengths in inches when they're round in them, else millimeters; angles in degrees; colors in hex
    assert flat.rows[0].values == {"angle": "30 deg", "diameter": "0.19 in"}
    assert flat.rows[1].values == {"diameter": "0.19 in", "color": "#ff0080"}
    assert flat.rows[2].values == {"diameter": "5 mm"}
    assert flat.rows[3].values == {}


def test_outputs():
    flat = table()
    text = to_text([flat])
    assert "Size   Threads/inch  angle   diameter  color" in text
    assert "#10 *  32 *          " in text
    assert "| M5 |  |  | 5 mm |  |" in to_markdown([flat])
    page = to_html([flat])
    assert 'class="level default"' in page and "no values" in page and 'style="background:#ff0080"' in page


def test_find_tables_skips_other_constants(tmp_path):
    evaluator = shared_evaluator()
    code_dir = evaluator.config.code_dir
    tables = find_tables(evaluator, code_dir / "released/shaft/robotShaftTables.gen.fs", code_dir)
    assert [found.name for found in tables] == ["tappedHoleTable", "clearanceHoleTable", "frcShaftTable", "ftcShaftTable"]


def test_command(capsys):
    assert cli.main(["table", "featurescripts/released/shaft", "-n", "tappedHole"]) == 0
    out = capsys.readouterr().out
    assert out.startswith("tappedHoleTable  (released/shaft/robotShaftTables.gen.fs, 10 rows)")
    assert "Used by: released/shaft/robotShaft.fs:" in out
    assert "#10 *  32 tpi (UNF) *  0.19 in" in out


def test_lookup_parameters():
    source = """annotation { "Name" : "Hole table", "Lookup Table" : holeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.hole is LookupTablePath;
    annotation { "Name" : "Size" }
    isLength(definition.size, LENGTH_BOUNDS);"""
    assert lookup_parameters(source) == [("holeTable", "Hole table")]


def test_options_are_shown_as_the_dialog_shows_them():
    # In the order they're written, with a level's first option its default when it names none
    written = {
        "name": "size",
        "displayName": "Size",
        "entries": {
            "#8": {"name": "pitch", "displayName": "Threads/inch", "entries": {"32": {}, "36": {}}},
            "#10": {"name": "pitch", "displayName": "Threads/inch", "default": "32", "entries": {"24": {}, "32": {}}},
        },
    }
    shown = options("sizes", "sizes.fs", written)
    assert [row.path for row in shown.rows] == [["#8", "32"], ["#8", "36"], ["#10", "24"], ["#10", "32"]]
    assert [row.defaults for row in shown.rows] == [[True, True], [True, False], [False, False], [False, True]]
    assert [row.is_default for row in shown.rows] == [True, False, False, False]
    # Values are matched to them by path
    merged = with_values(shown, table())
    assert merged.rows[3].values == {"diameter": "0.19 in", "color": "#ff0080"}
    assert merged.rows[0].values == {}
    assert merged.fields == ["angle", "diameter", "color"]
