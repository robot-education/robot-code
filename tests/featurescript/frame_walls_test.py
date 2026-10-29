"""Building frames from their walls (see linear stock's `stockWalls` and `buildFromWalls`): which frames are, that a
sketched profile's holes go only through its walls when it says so, and custom tubes with no holes."""

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

# Frames with holes built by cutting them, as they can't be built from their walls: solid around a bore, or with hollow
# corners behind their holes
CUT = {
    "Square Beam (goBILDA 1106 Series)",
    "2x1 Tube (REV, light)",
    "2x1 Tube (REV, standard, MAX Pattern)",
    "2x2 Tube (REV, grid)",
    "2x2 Tube (REV, MAX Pattern)",
    "2x2 Tube (REV, MAX Pattern and grid)",
}


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


def profile_walls(stock):
    """A sketched profile's walls: how close its inner loops come to its outside, facing X and Y, and whether the rest
    of it is clear of its holes, which go through it (sampled)."""
    evaluator = shared_evaluator()
    context = evaluator.eval("newContext()")
    sketch = evaluator.eval('newSketchOnPlane(context, newId() + "profile", { "sketchPlane" : XY_PLANE })', context=context)
    evaluate("sketchProfile(sketch, stock, 1)", STOCK, sketch=sketch, stock=stock)
    evaluator.eval("skSolve(sketch)", sketch=sketch)
    polygons = [polygon(loop) for loop in recorded_sketch(context, "profile").loops()]
    values = to_python(untag(stock))
    width, height = values["width"], values["height"]
    outside = max(polygons, key=lambda p: max(abs(x) for x, _ in p) + max(abs(y) for _, y in p))
    inner = [p for p in polygons if p is not outside]
    wall_x = min(width / 2 - abs(x) for p in inner for x, _ in p)
    wall_y = min(height / 2 - abs(y) for p in inner for _, y in p)
    clear = True
    for rows, faces_x in ((values["xRows"], True), (values["yRows"], False)):
        for row in rows:
            for shape in row["shapes"]:
                radius = MAX_SPLINE_RADIUS if shape.get("maxSpline") else (shape.get("diameter") or values["holeDiameter"]) / 2
                # Through the stock, inside the walls it goes through
                reach = width / 2 - wall_x if faces_x else height / 2 - wall_y
                for i in range(21):
                    across = shape["offset"] - radius + 2 * radius * i / 20
                    for j in range(1, 200):
                        along = -reach + 2 * reach * j / 200
                        point = (along, across) if faces_x else (across, along)
                        if sum(inside(point, p) for p in polygons) % 2:
                            clear = False
    return wall_x, wall_y, clear


@pytest.mark.parametrize(
    "stock", [s for s in cots_frames() if to_python(untag(s)).get("profile") is not None], ids=lambda s: to_python(untag(s))["partName"]
)
def test_sketched_profiles_holes_go_only_through_their_walls_if_they_say_so(stock):
    values = to_python(untag(stock))
    wall_x, wall_y, clear = profile_walls(stock)
    assert values.get("holesInWalls", False) == clear
    if clear:
        # Its walls are as thick as its sketch's
        assert values["wallX"] == pytest.approx(wall_x, abs=1e-6)
        assert values["wallY"] == pytest.approx(wall_y, abs=1e-6)


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
    for wall, (name, at, low, high) in zip(found, walls):
        assert wall["name"] == name
        # Where it starts across the stock, along the way it faces
        assert sum(o * n for o, n in zip(wall["plane"]["origin"], wall["plane"]["normal"])) == pytest.approx(at * INCH)
        assert (wall["low"], wall["high"]) == (pytest.approx(low * INCH), pytest.approx(high * INCH))
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
