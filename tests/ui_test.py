import pathlib
import re

import pytest

from fs_cli import cli
from fs_cli.ui import UiError, find_chromium

FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");
import(path : "core/shared.fs", version : "");

export enum Placement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT,
    annotation { "Name" : "Old", "Hidden" : true }
    OLD
}

export predicate isPoint(definition is map)
{
    definition.placement == Placement.POINT;
}

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Placement", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.placement is Placement;

        annotation { "Name" : "Size", "Lookup Table" : sizeTable }
        definition.size is LookupTablePath;

        if (!isPoint(definition))
        {
            annotation { "Name" : "Edges", "Filter" : EntityType.EDGE }
            definition.edges is Query;

            annotation { "Name" : "Hidden", "UIHint" : UIHint.ALWAYS_HIDDEN }
            isAnything(definition.hidden);
        }
        else
        {
            annotation { "Name" : "Point" }
            definition.point is Query;
        }

        sharedPredicate(definition, "Depth");

        annotation { "Name" : "Second", "Default" : true }
        definition.hasSecond is boolean;

        annotation { "Group Name" : "Second", "Driving Parameter" : "hasSecond", "Collapsed By Default" : false }
        {
            if (definition.hasSecond)
            {
                annotation { "Name" : "Second depth" }
                isLength(definition.secondDepth, LENGTH_BOUNDS);
            }
        }
    }
    {
    }, { "placement" : Placement.EDGE });

export const sizeTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "WCP" : { "name" : "size", "displayName" : "Size", "default" : "1/2 in.", "entries" : { "3/8 in." : {}, "1/2 in." : {} } },
            "REV" : { "name" : "size", "displayName" : "Size", "entries" : { "3/8 in." : {} } }
        }
    };
"""

SHARED = """FeatureScript 1;
export predicate sharedPredicate(definition is map, name is string)
{
    annotation { "Name" : name }
    isLength(definition.depth, LENGTH_BOUNDS);

    annotation { "Name" : "Flip", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.flip is boolean;

    annotation { "Name" : "Offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
    definition.hasOffset is boolean;
    if (definition.hasOffset)
    {
        annotation { "Name" : "Offset", "UIHint" : ["DISPLAY_SHORT"] }
        isLength(definition.offset, LENGTH_BOUNDS);
    }
}
"""

STD_BOUNDS = """FeatureScript 1;
export const LENGTH_BOUNDS =
{
    (meter)      : [-500, 0.025, 500],
    (inch)       : 1.0
} as LengthBoundSpec;
"""


@pytest.fixture
def repo(tmp_path, monkeypatch):
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    code_dir = tmp_path / "featurescripts"
    (code_dir / "core").mkdir(parents=True)
    (code_dir / "widget.fs").write_text(FEATURE)
    (code_dir / "core" / "shared.fs").write_text(SHARED)
    (tmp_path / "std").mkdir()
    (tmp_path / "std" / "common.fs").write_text(
        'FeatureScript 1;\nexport import(path : "onshape/std/valueBounds.fs", version : "1.0");\n'
    )
    (tmp_path / "std" / "valueBounds.fs").write_text(STD_BOUNDS)
    monkeypatch.chdir(tmp_path)
    return tmp_path


def render(repo, *settings):
    from fs_cli.config import load_config
    from fs_cli.cli import _project
    from fs_cli.ui import render_feature

    config = load_config()
    overrides = dict(setting.split("=", 1) for setting in settings)
    return render_feature(
        _project(config), config.std_dir, repo / "featurescripts" / "widget.fs", None, overrides
    )


def texts(page: str) -> list[str]:
    body = page[page.index("<body>") :]
    return [text for text in re.split(r"<[^>]+>", body) if text.strip()]


def test_dialog_follows_the_precondition(repo):
    page, warnings = render(repo)
    assert warnings == []
    assert texts(page) == [
        "Widget 1",
        "&#x2714;",
        "&#x2716;",
        # A horizontal enum, without the hidden value
        "Edge",
        "Point",
        # Each level of the lookup table, at its default
        "Vendor",
        "WCP",
        "Size",
        "1/2 in.",
        # The branch for edges, without the hidden parameter
        "Edges",
        # From the predicate in another file, with its argument; the flip button joins its row
        "Depth",
        "1 in",
        "Offset",
        # The group's driving parameter is its header
        "&#x2714;",
        "Second",
        "Second depth",
        "1 in",
    ]
    assert "class='tab selected'>Edge<" in page
    # The flip button, with Onshape's icon
    assert "<span class='button' title='Flip'><span class='icon invert'><svg" in page


def test_short_parameters_share_a_row(repo):
    page, _ = render(repo, "hasOffset=true")
    rows = [row.split("</div>")[0] for row in page.split("<div class='row'>")]
    [row] = [row for row in rows if "Offset" in row]
    assert texts("<body>" + row) == ["&#x2714;", "Offset", "1 in"]


def test_settings_change_the_dialog(repo):
    page, _ = render(repo, "placement=POINT", "size=REV > 3/8 in.", "hasSecond=false", "depth=2 in")
    shown = texts(page)
    assert "Edges" not in shown and "Point" in shown
    assert shown[shown.index("Vendor") + 1] == "REV"
    assert "Second depth" not in shown
    assert "2 in" in shown


def test_bad_settings_are_reported(repo):
    with pytest.raises(UiError, match="no value SIDEWAYS"):
        render(repo, "placement=SIDEWAYS")
    with pytest.raises(UiError, match="no 'AndyMark'"):
        render(repo, "size=AndyMark")
    _, warnings = render(repo, "point=x")
    assert warnings == ["point isn't shown, so --set point did nothing."]


ARRAY_FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Holes", "Item name" : "hole", "Item label template" : "#name (#depth)" }
        definition.holes is array;
        for (var hole in definition.holes)
        {
            annotation { "Name" : "Name", "Default" : "Hole" }
            hole.name is string;

            annotation { "Name" : "Depth", "UIHint" : ["CAN_BE_TOLERANT"] }
            isLength(hole.depth, LENGTH_BOUNDS);
        }

        annotation { "Name" : "Total", "UIHint" : ["READ_ONLY"] }
        isLength(definition.total, LENGTH_BOUNDS);
    }
    {
    });
"""


def test_arrays(repo):
    (repo / "featurescripts" / "widget.fs").write_text(ARRAY_FEATURE)
    page, warnings = render(repo)
    assert warnings == []
    # New features start with no items
    assert texts(page)[3:] == ["Holes", "Add hole", "Total", "1 in"]
    assert "class='input read-only'" in page
    page, _ = render(repo, "holes=2")
    assert texts(page)[3:] == [
        "Holes",
        "Hole (1 in)",
        "&#x2716;",
        "Name",
        "Hole",
        "Depth",
        "1 in",
        "Hole (1 in)",
        "&#x2716;",
        "Name",
        "Hole",
        "Depth",
        "1 in",
        "Add hole",
        "Total",
        "1 in",
    ]
    assert "title='Add tolerance'" in page
    with pytest.raises(UiError, match="how many items"):
        render(repo, "holes=many")


def test_screenshot(repo, capsys):
    try:
        find_chromium()
    except UiError:
        pytest.skip("Chromium isn't installed")
    assert cli.main(["ui", "featurescripts/widget.fs", "-o", "widget.png"]) == 0
    assert (repo / "widget.png").read_bytes()[:8] == b"\x89PNG\r\n\x1a\n"


def test_icons_exist():
    from fs_cli.ui import BUTTONS, ICON_DIR, ICON_VALUES, MATE_CONNECTOR_ICON, PARAMETER_ICONS, TOLERANCE_ICON

    names = {name for name, _ in BUTTONS.values()} | set(PARAMETER_ICONS.values()) | set(ICON_VALUES.values())
    for name in names | {MATE_CONNECTOR_ICON, TOLERANCE_ICON}:
        assert (ICON_DIR / f"{name}.svg").is_file(), name
