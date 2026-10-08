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
    """The text shown in a dialog: its text and its inputs' values, without dropdowns' (closed) options or icons."""
    body = page[page.index("<body>") :]
    body = re.sub(r"<ul class='os-select-choices.*?</ul>", "", body)
    body = re.sub(r"<svg.*?</svg>", "", body, flags=re.S)
    body = re.sub(r"<input[^>]* value='([^']*)'[^>]*>", r"<i>\1</i>", body)
    return [text for text in re.split(r"<[^>]+>", body) if text.strip()]


def parameter(page: str, name: str) -> str:
    """The markup of a parameter in a list."""
    start = page.index(f" data-parameter-id='{name}'")
    start = page.rindex("<div class='os-parameter-list-item", 0, start)
    ends = [page.find(marker, start + 1) for marker in ("<div class='os-parameter-list-item", "</os-parameter-group>")]
    return page[start : min(end for end in ends if end >= 0)]


def test_dialog_follows_the_precondition(repo):
    page, warnings = render(repo)
    assert warnings == []
    assert texts(page) == [
        "Widget 1",
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
        "Second",
        "Second depth",
        "1 in",
    ]
    # Onshape's markup: the chosen tab is active, and clicking a tab sets it
    assert "<div class='option os-active os-param-form-item' data-enum-param-value='EDGE' data-set='placement' data-value='EDGE'>" in page
    # The flip button, with Onshape's icon, in the column beside the depth
    flip = parameter(page, "flip")
    assert "os-param-fits-in-right-column" in flip and "#svg-icon-flip-direction-opposite" in flip
    assert "data-set='flip' data-value='true'" in flip
    assert "os-param-fill-first-column" in parameter(page, "depth")
    # The group's driving parameter is a checkbox in its header
    assert "<div class='os-param-group-driving-parameter os-parameter-list-item' data-parameter-id='hasSecond' data-tip-name='Second'" in page


def test_short_parameters_share_a_row(repo):
    page, _ = render(repo, "hasOffset=true")
    # Onshape lays out short parameters beside each other, and doesn't label short values
    has_offset, offset = parameter(page, "hasOffset"), parameter(page, "offset")
    assert "os-param-display-short" in has_offset and "os-param-display-short" in offset
    assert texts("<body>" + has_offset) == ["Offset"] and " checked" in has_offset
    assert texts("<body>" + offset) == ["1 in"]


def test_settings_change_the_dialog(repo):
    page, _ = render(repo, "placement=POINT", "size=REV > 3/8 in.", "hasSecond=false", "depth=2 in")
    shown = texts(page)
    assert "Edges" not in shown and "Point" in shown
    assert shown[shown.index("Vendor") + 1] == "REV"
    assert "Second depth" not in shown
    assert "2 in" in shown
    # The unchecked group keeps its header, closed
    assert "Second" in shown and "os-param-group-expander node-expander-disabled" in page
    assert "<div class='os-param-group-collapsible-contents ng-hide'>" in page


def test_groups_inside_false_conditions_keep_their_headers(repo):
    (repo / "featurescripts" / "widget.fs").write_text(DRIVEN_GROUP_FEATURE)
    page, _ = render(repo)
    assert texts(page)[1:] == ["Extra"]
    assert "data-driving-parameter-id='hasExtra'" in page and "ng-hide" in page
    page, _ = render(repo, "hasExtra=true")
    assert texts(page)[1:] == ["Extra", "Extra depth", "1 in"]


def test_groups_hidden_with_their_driving_parameters(repo):
    (repo / "featurescripts" / "widget.fs").write_text(HIDDEN_DRIVEN_GROUP_FEATURE)
    page, _ = render(repo, "hasSecond=true")
    assert texts(page)[1:] == ["Symmetric", "Second", "Offset", "1 in"]
    # A button after a checkbox and a short value stays in their row
    row = page[page.index("data-parameter-id='hasOffset'") :]
    row = row[: row.index("</os-parameter-group>")]
    assert "data-parameter-id='offsetOpposite'" in row
    # Hiding the group's driving parameter hides the group, header and all
    page, _ = render(repo, "symmetric=true", "hasSecond=true")
    assert texts(page)[1:] == ["Symmetric"]
    assert "data-group=" not in page


HIDDEN_DRIVEN_GROUP_FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Symmetric" }
        definition.symmetric is boolean;

        if (!definition.symmetric)
        {
            annotation { "Name" : "Second" }
            definition.hasSecond is boolean;

            annotation { "Group Name" : "Group", "Driving Parameter" : "hasSecond", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"], "Default" : true }
                definition.hasOffset is boolean;

                annotation { "Name" : "Offset", "UIHint" : ["DISPLAY_SHORT"] }
                isLength(definition.offset, LENGTH_BOUNDS);

                annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.offsetOpposite is boolean;
            }
        }
    }
    {
    });
"""


DRIVEN_GROUP_FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Extra" }
        definition.hasExtra is boolean;

        if (definition.hasExtra)
        {
            annotation { "Group Name" : "Extra", "Driving Parameter" : "hasExtra", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Extra depth" }
                isLength(definition.extraDepth, LENGTH_BOUNDS);
            }
        }
    }
    {
    });
"""


def test_nested_groups(repo):
    (repo / "featurescripts" / "widget.fs").write_text(NESTED_GROUP_FEATURE)
    page, warnings = render(repo)
    assert warnings == []
    assert texts(page)[1:] == ["Outer", "Outer depth", "1 in", "Inner", "Inner depth", "1 in", "Extra"]
    # Each nested group is a row of the outer group, with its driving parameter in its header
    assert page.count(" data-group=") == 3
    inner = page.index("data-group='Inner'")
    assert page.rindex("<div class='os-param-subgroup-row'>", 0, inner) > page.index("data-group='Outer'")
    assert "data-driving-parameter-id='hasExtra'" in page
    page, _ = render(repo, "hasExtra=true")
    assert texts(page)[1:] == ["Outer", "Outer depth", "1 in", "Inner", "Inner depth", "1 in", "Extra", "Extra depth", "1 in"]


NESTED_GROUP_FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Group Name" : "Outer", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Outer depth" }
            isLength(definition.outerDepth, LENGTH_BOUNDS);

            annotation { "Group Name" : "Inner", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Inner depth" }
                isLength(definition.innerDepth, LENGTH_BOUNDS);
            }

            annotation { "Name" : "Extra" }
            definition.hasExtra is boolean;

            annotation { "Group Name" : "Extra", "Driving Parameter" : "hasExtra", "Collapsed By Default" : false }
            {
                if (definition.hasExtra)
                {
                    annotation { "Name" : "Extra depth" }
                    isLength(definition.extraDepth, LENGTH_BOUNDS);
                }
            }
        }
    }
    {
    });
"""


def test_bad_settings_are_reported(repo):
    with pytest.raises(UiError, match="no value SIDEWAYS"):
        render(repo, "placement=SIDEWAYS")
    with pytest.raises(UiError, match="no 'AndyMark'"):
        render(repo, "size=AndyMark")
    _, warnings = render(repo, "point=x", "nothing=1")
    assert warnings == [
        "nothing isn't a parameter, so --set nothing did nothing.",
        "point isn't shown, so --set point did nothing.",
    ]


def test_conditions_read_parameters_declared_anywhere(repo):
    # Every parameter has a value, even where it isn't shown, and conditions before its declaration read it
    (repo / "featurescripts" / "widget.fs").write_text(FORWARD_FEATURE)
    page, warnings = render(repo)
    assert warnings == []
    assert texts(page)[1:] == ["Shown by a later default", "Flag"]
    page, _ = render(repo, "flag=false")
    assert texts(page)[1:] == ["Flag"]


FORWARD_FEATURE = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        if (definition.flag)
        {
            annotation { "Name" : "Shown by a later default" }
            definition.shown is boolean;
        }

        annotation { "Name" : "Flag", "Default" : true }
        definition.flag is boolean;
    }
    {
    });
"""


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
    assert texts(page)[1:] == ["Holes", "CLEAR", "Add hole", "Total", "1 in"]
    assert "os-param-readonly" in parameter(page, "total")
    page, _ = render(repo, "holes=2", "holes.1.name=Big", "holes.1.depth=2 in")
    assert texts(page)[1:] == [
        "Holes",
        "CLEAR",
        "Hole (1 in)",
        "&times;",
        "Name",
        "Hole",
        "Depth",
        "1 in",
        # Each item's parameters can be set
        "Big (2 in)",
        "&times;",
        "Name",
        "Big",
        "Depth",
        "2 in",
        "Add hole",
        "Total",
        "1 in",
    ]
    assert "data-set='holes.1.depth'" in page
    assert "data-remove='holes' data-index='1'" in page
    assert "data-set='holes' data-value='3'>Add hole" in page
    assert "#svg-icon-hole-tolerance-precision" in page
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
    from fs_cli.ui import BUTTONS, ICON_DIR, ICON_VALUES, ONSHAPE_UI, PARAMETER_ICONS, SPRITE_ICONS

    for name in set(PARAMETER_ICONS.values()) | set(ICON_VALUES.values()):
        assert (ICON_DIR / f"{name}.svg").is_file(), name
    sprite = (ONSHAPE_UI / "icons.svg").read_text()
    for name in {*BUTTONS.values(), *SPRITE_ICONS.values(), "collapsed", "mate-connector-button", "ok-button"}:
        assert f'id="svg-icon-{name}"' in sprite, name


def test_html_for_the_preview(repo, capsys):
    # `-o -` prints the dialog's HTML (for the VS Code extension's preview), without taking a screenshot
    assert cli.main(["ui", "featurescripts/widget.fs", "-o", "-", "--set", "placement=POINT"]) == 0
    page = capsys.readouterr().out
    assert page.startswith("<!doctype html>")
    # Controls say which parameter they set, and to what
    assert "data-set='placement' data-value='EDGE'><span>Edge</span>" in page
    assert not (repo / "widget.png").exists()


def test_tooltips(repo):
    # Each parameter says what the preview's tooltip for it shows: its name, its default, and its UI hints
    page, _ = render(repo, "placement=POINT", "size=REV", "depth=3 in", "hasSecond=false")
    tips = {
        name: dict(re.findall(r" data-tip-(\w+)='([^']*)'", re.search(rf"<div [^>]*data-parameter-id='{name}' data-tip-[^>]*>", page)[0]))
        for name in ("placement", "size", "depth", "flip", "hasSecond")
    }
    assert tips == {
        # The feature's defaults map gives placement's default
        "placement": {"name": "Placement", "default": "Edge", "hints": "HORIZONTAL_ENUM"},
        "size": {"name": "Size", "default": "WCP &gt; 1/2 in."},
        "depth": {"name": "Depth", "default": "1 in"},
        "flip": {"name": "Flip", "default": "false", "hints": "OPPOSITE_DIRECTION"},
        "hasSecond": {"name": "Second", "default": "true"},
    }
