"""Building frames from their walls (see linear stock's `stockWalls` and `buildFromWalls`): which frames are, that a
sketched profile's walls are as thick as its sketch's where its holes go, plain inside, with nothing behind them, and
custom tubes with no holes."""

import math

import pytest

from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator
from fs_eval.sketch import recorded_sketch
from fs_eval.values import untag

FRAME = "released/frame/robotFrame.fs"
STOCK = "core/linearStock.fs"
INCH = 0.0254
LOCATION = "coordSystem(vector(0, 0, 0) * meter, vector(1, 0, 0), vector(0, 0, 1))"
MAX_SPLINE_RADIUS = 17.45e-3

# Frames with holes built by cutting them, as they have no walls: solid around a bore
CUT = {"Square Beam (goBILDA 1106 Series)"}
PROFILE_WALL_MARGIN = 0.25e-3


def evaluate(expression: str, module: str, **variables):
    return shared_evaluator().eval(expression, module, **variables)


def leaf_paths(node, path=None):
    node = untag(node)
    for key, child in untag(node.get_str("entries")).sorted_items():
        child_path = {**(path or {}), node.get_str("name"): key}
        child = untag(child)
        if type(child).__name__ == "FSMap" and child.get_str("entries") is not None and child.get_str("name") is not None:
            yield from leaf_paths(child, child_path)
        else:
            yield child_path


def cots_frames():
    """Every COTS frame with holes, as Robot frame builds it (its holes where they're sold), unflipped."""
    frames = []
    for program, table in (("FRC", "frcFrameTable"), ("FTC", "ftcFrameTable")):
        for path in leaf_paths(evaluate(table, FRAME)):
            definition = evaluate(
                f'{{ "program" : Program.{program}, "source" : FrameSource.COTS, "{program.lower()}Frame" : path as LookupTablePath, "flip" : false }}',
                FRAME,
                path=path,
            )
            entry = evaluate("frameTableEntry(definition)", FRAME, definition=definition)
            definition = evaluate(
                'mergeMaps(definition, { "holeStart" : entry.tieStart, "holeDiameter" : entry.holeDiameter })',
                FRAME,
                definition=definition,
                entry=entry,
            )
            stock = evaluate("getFrame(definition)", FRAME, definition=definition)
            if to_python(untag(stock))["xRows"] or to_python(untag(stock))["yRows"]:
                frames.append(stock)
    return frames


def built_from_walls(stock) -> bool:
    walls = evaluate(f"stockWalls(stock, {LOCATION}, 1)", STOCK, stock=stock)
    return to_python(walls) != [] and evaluate("holesFitWalls(stock, walls)", STOCK, stock=stock, walls=walls) is True


def test_frames_with_holes_are_built_from_their_walls():
    cut = {to_python(untag(stock))["partName"] for stock in cots_frames() if not built_from_walls(stock)}
    assert cut == CUT


def polygon(loop) -> list[tuple[float, float]]:
    """A loop of a sketch as a polygon, its arcs in short lines."""
    points = []
    for curve in loop.curves:
        if curve.kind != "arc":
            points.extend([curve.start, *(curve.points or [])])
            continue
        start = math.atan2(curve.start[1] - curve.center[1], curve.start[0] - curve.center[0])
        end = math.atan2(curve.end[1] - curve.center[1], curve.end[0] - curve.center[0])
        if curve.start == curve.end:
            end = start + 2 * math.pi
        elif curve.counter_clockwise and end <= start:
            end += 2 * math.pi
        elif not curve.counter_clockwise and end >= start:
            end -= 2 * math.pi
        for i in range(64):
            angle = start + (end - start) * i / 64
            points.append((curve.center[0] + curve.radius * math.cos(angle), curve.center[1] + curve.radius * math.sin(angle)))
    return points


def inside(point, polygon) -> bool:
    x, y = point
    crossed = False
    for (x1, y1), (x2, y2) in zip(polygon, polygon[1:] + polygon[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            crossed = not crossed
    return crossed


def profile_walls(stock) -> list[tuple[float, float, float, bool]]:
    """A sketched profile's walls with holes (facing X, then Y): how thick it is through them (its first void, walking in
    from its side) at least and at most, across its holes (as `holeSpan` reaches), the thickness of its wall, and
    whether it's clear of material behind its walls there (sampled), as the holes go through it."""
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    sketch = evaluator.eval('newSketchOnPlane(context, newId() + "profile", { "sketchPlane" : XY_PLANE })', context=context)
    evaluate("sketchProfile(sketch, stock, 1)", STOCK, sketch=sketch, stock=stock)
    evaluator.eval("skSolve(sketch)", sketch=sketch)
    polygons = [polygon(loop) for loop in recorded_sketch(context, "profile").loops()]
    values = to_python(untag(stock))

    def material(point) -> bool:
        return sum(inside(point, p) for p in polygons) % 2 == 1

    walls = []
    for rows, faces_x in ((values["xRows"], True), (values["yRows"], False)):
        if not rows:
            continue
        half = values["width" if faces_x else "height"] / 2
        wall = values["wallX" if faces_x else "wallY"]
        spans = []
        for row in rows:
            for shape in row["shapes"]:
                radius = MAX_SPLINE_RADIUS if shape.get("maxSpline") else (shape.get("diameter") or values["holeDiameter"]) / 2
                spans.append((shape["offset"] - radius, shape["offset"] + radius))
        low = min(a for a, _ in spans) - PROFILE_WALL_MARGIN
        high = max(b for _, b in spans) + PROFILE_WALL_MARGIN
        depths = []
        clear = True
        for i in range(41):
            across = low + (high - low) * i / 40
            at = (lambda depth: (-half + depth, across)) if faces_x else (lambda depth: (across, -half + depth))
            # Its first void: stepping in, then halving the step
            step = half / 100
            depth = step
            while material(at(depth)) and depth < half:
                depth += step
            inner, outer = depth - step, depth
            for _ in range(30):
                middle = (inner + outer) / 2
                inner, outer = (middle, outer) if material(at(middle)) else (inner, middle)
            depths.append(outer)
            # Through it, between its walls
            clear = clear and not any(material(at(wall + (2 * half - 2 * wall) * j / 60)) for j in range(1, 60))
        walls.append((min(depths), max(depths), wall, clear))
    return walls


@pytest.mark.parametrize(
    "stock", [s for s in cots_frames() if to_python(untag(s)).get("profile") is not None], ids=lambda s: to_python(untag(s))["partName"]
)
def test_sketched_profiles_walls_are_as_thick_as_their_sketches_where_their_holes_go(stock):
    for shallowest, deepest, wall, clear in profile_walls(stock):
        # Plain, without notches inside (see frameTables.py's `without_notches`)
        assert (shallowest, deepest) == (pytest.approx(wall, abs=1e-6), pytest.approx(wall, abs=1e-6))
        assert clear


def rectangle(**fields) -> str:
    entries = {"width": "2 * inch", "height": "1 * inch", "wallX": "0.0625 * inch", "wallY": "0.0625 * inch", **fields}
    return (
        '{ "partName" : "Tube", "stock" : [], "holeDiameter" : 0.196 * inch, "xRows" : [], "yRows" : [], '
        '"tieStart" : 0.5 * inch, "tieUnit" : 0.5 * inch, '
        + ", ".join(f'"{key}" : {value}' for key, value in entries.items())
        + " } as Stock"
    )


@pytest.mark.parametrize(
    "fields,across,walls",
    [
        # A tube's walls facing Y run its width, and those facing X between them
        ({}, 1, [("x0", -1, -0.4375, 0.4375), ("x1", 0.9375, -0.4375, 0.4375), ("y0", -0.5, -1, 1), ("y1", 0.4375, -1, 1)]),
        # A channel is open facing +Y
        ({"open": "true"}, 1, [("x0", -1, -0.5, 0.4375), ("x1", 0.9375, -0.5, 0.4375), ("y0", -0.5, -1, 1)]),
        # An angle's legs face -X and -Y, mirrored with it
        ({"angle": "true"}, 1, [("x0", -1, -0.5, 0.4375), ("y0", -0.5, -1, 1)]),
        ({"angle": "true"}, -1, [("x0", 0.9375, -0.5, 0.4375), ("y0", -0.5, -1, 1)]),
    ],
)
def test_walls(fields, across, walls):
    found = to_python(evaluate(f"stockWalls({rectangle(**fields)}, {LOCATION}, {across})", STOCK))
    starts = {}
    for wall, (name, at, low, high) in zip(found, walls):
        assert wall["name"] == name
        # Where it starts across the stock, along the way it faces
        assert sum(o * n for o, n in zip(wall["plane"]["origin"], wall["plane"]["normal"])) == pytest.approx(at * INCH)
        assert (wall["low"], wall["high"]) == (pytest.approx(low * INCH), pytest.approx(high * INCH))
        # The second facing each way copies the first, as far along as it is from it
        first = name[0] + "0"
        assert wall.get("copies") == (None if name == first else first)
        assert wall["along"] == pytest.approx((at - starts.setdefault(first, at)) * INCH)
    assert len(found) == len(walls)


def test_solid_stock_has_no_walls():
    assert to_python(evaluate(f'stockWalls({rectangle(solid="true")}, {LOCATION}, 1)', STOCK)) == []


@pytest.mark.parametrize(
    "spans,length,position",
    [
        # The widest gap between holes
        ([(0.4, 0.6), (0.9, 1.1)], 1.5, 0.2),
        ([(0.1, 0.3), (0.9, 1.1)], 1.2, 0.6),
        # Holes crossing each other
        ([(0.1, 0.5), (0.4, 0.8)], 1.0, 0.9),
        # None
        ([], 2.0, 1.0),
    ],
)
def test_clear_position(spans, length, position):
    spans_code = "[" + ", ".join(f"[{a} * inch, {b} * inch]" for a, b in spans) + "]"
    assert to_python(evaluate(f"clearPosition({spans_code}, {length} * inch)", STOCK)) == pytest.approx(position * INCH)


@pytest.mark.parametrize("size", ["1x1", "2x2"])
def test_custom_tubes_can_have_no_holes(size):
    definition = evaluate(
        f'{{ "program" : Program.FRC, "source" : FrameSource.CUSTOM, "customFrame" : {{ "profile" : "Tube", "size" : "{size}", "rows" : "No holes" }} as LookupTablePath, '
        '"wallThickness" : 0.0625 * inch, "holeSpacing" : 0.5 * inch, "holeStart" : 0.5 * inch, "holeDiameter" : 0.196 * inch, '
        '"rowSpacing" : 0.5 * inch }',
        FRAME,
    )
    assert evaluate("isNoHolesFrame(definition)", FRAME, definition=definition) is True
    stock = to_python(untag(evaluate("getFrame(definition)", FRAME, definition=definition)))
    assert (stock["xRows"], stock["yRows"]) == ([], [])


def test_custom_2x1_tubes_can_have_no_holes():
    path = '{ "profile" : "Tube", "size" : "2x1", "wideRows" : "No holes" } as LookupTablePath'
    definition = evaluate(f'{{ "program" : Program.FRC, "source" : FrameSource.CUSTOM, "customFrame" : {path} }}', FRAME)
    assert evaluate("isNoHolesFrame(definition)", FRAME, definition=definition) is True
    with_holes = evaluate(
        '{ "program" : Program.FRC, "source" : FrameSource.CUSTOM, "customFrame" : { "profile" : "Tube", "size" : "2x1", "wideRows" : "1", "narrowRows" : "1" } as LookupTablePath }',
        FRAME,
    )
    assert evaluate("isNoHolesFrame(definition)", FRAME, definition=with_holes) is False


def test_sketched_profiles_walls_reach_just_past_their_holes():
    frames = {to_python(untag(stock))["partName"]: stock for stock in cots_frames()}
    # 5 mm holes
    reach = 2.5e-3 + PROFILE_WALL_MARGIN
    walls = to_python(evaluate(f"stockWalls(stock, {LOCATION}, 1)", STOCK, stock=frames["1x1 Tube (REV, standard)"]))
    assert [wall["name"] for wall in walls] == ["x0", "x1", "y0", "y1"]
    for wall in walls:
        assert (wall["low"], wall["high"]) == (pytest.approx(-reach, abs=1e-6), pytest.approx(reach, abs=1e-6))
    # Only walls with holes
    walls = to_python(evaluate(f"stockWalls(stock, {LOCATION}, 1)", STOCK, stock=frames["2x1 Tube (REV, standard, 1 in. sides only)"]))
    assert [wall["name"] for wall in walls] == ["x0", "x1"]


def test_notches_are_left_out():
    import importlib.util
    import pathlib

    from fs_cli.sketches import Line, Point

    spec = importlib.util.spec_from_file_location(
        "frameTables", pathlib.Path(__file__).parents[2] / "featurescripts/released/frame/frameTables.py"
    )
    tables = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(tables)
    # A wall's inside, with a notch 0.005 in. deep, then a corner
    wall = [
        Line(Point(-1, 0.46), Point(-0.005, 0.46)),
        Line(Point(-0.005, 0.46), Point(0, 0.465)),
        Line(Point(0, 0.465), Point(0.005, 0.46)),
        Line(Point(0.005, 0.46), Point(1, 0.46)),
        Line(Point(1, 0.46), Point(1, 0)),
    ]
    assert tables.without_notches(wall) == [Line(Point(-1, 0.46), Point(1, 0.46)), Line(Point(1, 0.46), Point(1, 0))]
