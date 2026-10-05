"""Tests for generating Feature Studios (lookup tables and sketch profiles) from Python definitions."""

import math
import pathlib

import pytest

from fs_cli import cli
from fs_cli.gen import GenerateError, Import, generate, render
from fs_cli.sketches import (
    Arc,
    Circle,
    FitSpline,
    Line,
    Point,
    Profile,
    Sketch,
    SketchMap,
    polar,
)
from fs_cli.tables import Enum, Node, Table, Value, inch, number, string

REPO = pathlib.Path(__file__).parent.parent


def test_render():
    color = Enum("Color", ["RED", "BLUE"])
    size = Node("size", [Value("Small", {"width": inch(1)}), Value("Large", {"width": inch(2.5)})])
    node = Node(
        "color",
        [
            Value("Red", {"color": color["RED"]}, size),
            Value("Blue", {"color": color["BLUE"], "label": string('say "hi"')}),
        ],
        display_name="Paint",
        default="Blue",
    )
    assert render([color, Table("paint", node)], "2615", "paint.py") == (
        "FeatureScript 2615;\n"
        'import(path : "onshape/std/common.fs", version : "2615.0");\n'
        "\n"
        "/* Generated from paint.py by `fs gen` -- DO NOT EDIT */\n"
        "\n"
        "export enum Color\n"
        "{\n"
        "    RED,\n"
        "    BLUE,\n"
        "}\n"
        "\n"
        "export const paint = {\n"
        '        "name" : "color",\n'
        '        "displayName" : "Paint",\n'
        '        "default" : "Blue",\n'
        '        "entries" : {\n'
        '            "Red" : {\n'
        '                "name" : "size",\n'
        '                "displayName" : "Size",\n'
        '                "entries" : {\n'
        '                    "Small" : { "color" : Color.RED, "width" : 1 * inch },\n'
        '                    "Large" : { "color" : Color.RED, "width" : 2.5 * inch },\n'
        "                },\n"
        "            },\n"
        '            "Blue" : { "color" : Color.BLUE, "label" : "say \\"hi\\"" },\n'
        "        },\n"
        "    };\n"
    )


def test_values_closer_to_the_root_win():
    leaf = Node("b", [Value("x", {"key": "2"})])
    node = Node("a", [Value("y", {"key": "1"}, leaf)])
    assert '"x" : { "key" : 1 }' in render([Table("t", node)], "1", "t.py")


def test_number():
    assert number(2) == "2"
    assert number(2.0) == "2"
    assert number(5 / 16) == "0.3125"


def test_with_next():
    node = Node("a", [Value("x"), Value("y")])
    leaf = Node("b", [Value("z")])
    assert [entry.next for entry in node.with_next(leaf).entries] == [leaf, leaf]
    assert [entry.next for entry in node.with_next({"y": leaf}).entries] == [None, leaf]
    with pytest.raises(ValueError):
        node.with_next({"w": leaf})


def test_invalid_nodes():
    with pytest.raises(ValueError):
        Node("a", [Value("x")], default="y")
    with pytest.raises(ValueError):
        Node("a", [Value("x"), Value("x")])


def test_generate_keeps_the_existing_version(tmp_path):
    (tmp_path / "tables.py").write_text(
        "from fs_cli.tables import Enum\nCONTENTS = [Enum('E', ['A'])]\n"
    )
    [result] = generate(tmp_path, "2960")
    assert result.output == tmp_path / "tables.gen.fs"
    assert result.code.startswith("FeatureScript 2960;")
    assert result.changed

    result.output.write_text(result.code.replace("2960", "2615"))
    [result] = generate(tmp_path, "2960")
    assert result.code.startswith("FeatureScript 2615;")
    assert not result.changed


def test_gen_tables_command(tmp_path, monkeypatch, capsys):
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    code_dir = tmp_path / "featurescripts" / "belt"
    code_dir.mkdir(parents=True)
    (code_dir / "beltTables.py").write_text(
        "from fs_cli.tables import Enum\nCONTENTS = [Enum('E', ['A'])]\n"
    )
    monkeypatch.chdir(tmp_path)
    monkeypatch.delenv("API_ACCESS_KEY", raising=False)

    # No std version to use yet
    assert cli.main(["gen"]) == 2

    (tmp_path / "std").mkdir()
    (tmp_path / "std" / "std.json").write_text('{"version": "2960.0"}')
    assert cli.main(["gen", "--dry-run"]) == 0
    assert not (code_dir / "beltTables.gen.fs").exists()
    assert cli.main(["gen"]) == 0
    assert (code_dir / "beltTables.gen.fs").read_text().startswith("FeatureScript 2960;")
    capsys.readouterr()
    assert cli.main(["gen"]) == 0
    assert "up to date" in capsys.readouterr().out


def test_checked_in_files_are_up_to_date():
    """Also checks generated imports resolve without the sync state, from their annotations."""
    for result in generate(REPO / "featurescripts", None):
        assert not result.changed, f"{result.output} is stale; run `fs gen`"


# Imports

ID_A, ID_B, VERSION_1, VERSION_2 = "a" * 24, "b" * 24, "1" * 24, "2" * 24


def test_imports(tmp_path):
    (tmp_path / "profiles.py").write_text(
        "from fs_cli.gen import Import\nCONTENTS = [Import('core/sketchData.fs'), Import('core/a.fs', export=True)]\n"
    )
    synced = {"core/sketchData.fs": (ID_A, VERSION_1), "core/a.fs": (ID_B, VERSION_1)}
    [result] = generate(tmp_path, "2960", synced)
    assert result.code.splitlines()[2:4] == [
        f'import(path : "{ID_A}", version : "{VERSION_1}"); // core/sketchData.fs',
        f'export import(path : "{ID_B}", version : "{VERSION_1}"); // core/a.fs',
    ]
    # Onshape manages import versions, so a newer one in the file is kept...
    result.output.write_text(result.code.replace(VERSION_1, VERSION_2))
    [result] = generate(tmp_path, "2960", synced)
    assert not result.changed
    # ...and the annotations resolve imports without the sync state
    [result] = generate(tmp_path, "2960", {})
    assert not result.changed

    # Studios which aren't synced are imported by path, which `fs push` resolves
    result.output.unlink()
    (tmp_path / "core").mkdir()
    (tmp_path / "core" / "sketchData.fs").write_text("")
    (tmp_path / "core" / "a.fs").write_text("")
    [result] = generate(tmp_path, "2960", {})
    assert result.code.splitlines()[2] == 'import(path : "core/sketchData.fs", version : ""); // core/sketchData.fs'
    (tmp_path / "core" / "a.fs").unlink()
    with pytest.raises(GenerateError, match="imports core/a.fs, which doesn't exist"):
        generate(tmp_path, "2960", {})


# Sketches


def close(a: Point, b: Point) -> bool:
    return math.dist((a.x, a.y), (b.x, b.y)) < 1e-12


def test_arcs():
    arc = Arc.around(Point(0, 0), 1, 350, 10)
    assert close(arc.start, polar(1, -10)) and close(arc.mid, Point(1, 0)) and close(arc.end, polar(1, 10))

    # between keeps the given start and end, going the short way unless major
    start, end = polar(2, 90, Point(1, 1)), polar(2, 0, Point(1, 1))
    minor = Arc.between(Point(1, 1), start, end)
    assert close(minor.start, start) and close(minor.end, end)
    assert close(minor.mid, polar(2, 45, Point(1, 1)))
    major = Arc.between(Point(1, 1), start, end, major=True)
    assert close(major.start, start) and close(major.mid, polar(2, 225, Point(1, 1)))


def test_profile_order():
    entities = [Line(Point(0, 0), Point(1, 0)), Circle(Point(0, 0), 1)]
    assert Profile(entities, [1, 0]).ordered() == entities[::-1]
    with pytest.raises(ValueError):
        Profile(entities, [0, 0])


def test_render_fit_splines():
    spline = FitSpline((Point(0, 0), Point(1, 0.5), Point(2, 0)))
    assert spline.render() == (
        '{ "operation" : SketchOperation.SPLINE, "points" : '
        "[vector(0, 0) * inch, vector(1, 0.5) * inch, vector(2, 0) * inch] }"
    )
    assert spline.rotated(90).end == Point(2, 0).rotated(90)


def test_render_sketches():
    profile = Profile([Line(Point(0, 0), Point(0.5, 1 / 3)), Circle(Point(1, 0), 2)])
    assert Sketch("HOLE", profile).render() == (
        "export const HOLE = [\n"
        '            { "operation" : SketchOperation.LINE, "start" : vector(0, 0) * inch, '
        '"end" : vector(0.5, 0.333333333333) * inch },\n'
        '            { "operation" : SketchOperation.CIRCLE, "center" : vector(1, 0) * inch, "radius" : 2 * inch },\n'
        "        ] as SketchDataArray;\n"
    )
    rendered = SketchMap("BY_SIDE", {"ProfileSide.INSIDE": profile}).render()
    assert rendered.startswith("export const BY_SIDE = {\n        ProfileSide.INSIDE : [\n")
    assert rendered.endswith("            ] as SketchDataArray,\n    };\n")


def test_checked_in_profiles_are_closed():
    """Every endpoint of a profile meets exactly one other (apart from MAXSpline's legacy stub line)."""
    import collections
    import runpy

    for source in sorted((REPO / "featurescripts").rglob("*.py")):
        for item in runpy.run_path(str(source))["CONTENTS"]:
            profiles = (
                item.profiles.values() if isinstance(item, SketchMap)
                else [item.profile] if isinstance(item, Sketch) else []
            )
            for profile in profiles:
                ends = collections.Counter()
                for entity in profile.entities:
                    if isinstance(entity, (Line, Arc, FitSpline)):
                        for point in (entity.start, entity.end):
                            ends[(round(point.x, 9), round(point.y, 9))] += 1
                def key(point):
                    return (round(point.x, 9), round(point.y, 9))

                dangling = [
                    e for e in profile.entities
                    if isinstance(e, Line) and ends[key(e.start)] == 1 and ends[key(e.end)] == 1
                ]
                loose = {key(e.start) for e in dangling} | {key(e.end) for e in dangling}
                unmatched = [p for p, count in ends.items() if count != 2 and p not in loose]
                assert not unmatched, (source.name, item.name, unmatched)
                assert not dangling or item.name == "MAX_SPLINE", (source.name, item.name)
