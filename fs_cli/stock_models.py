"""Builds STEP models of COTS frames, for Robot frame (featurescripts/released/frame) to import and cut down to size,
rather than building each frame's tube and holes in Onshape.

Each model is built from the frame as Robot frame describes it: the `Stock` its `getFrame` returns (with the holes
starting as they're sold), its profile as linear stock's `sketchProfile` sketches it, and its holes' shapes as
`sketchShape` sketches them, all run with the evaluator (`fs_eval`). So a model is what Robot frame would build, with
OpenCascade (the `step` dependency group: `uv sync --group step`) doing the modeling.

A model runs along +Z, centered on the origin with its width along X, as linear stock builds it from its start: its
holes start where they're sold, `row.start` from Z = 0, and carry on to the longest length it's sold in. Before Z = 0
it has a `LEAD` with no holes, so the feature can move its holes along it (Hole start distance) and still cut its start
from the model. It reaches a `MARGIN` past both, so the feature always cuts it inside it. Models are written in millimeters, with a fixed header, so the same frame always writes
the same file, and `fs push` only uploads models which changed.
"""

from __future__ import annotations

import dataclasses
import math
import pathlib
import re
from typing import Iterator

from fs_eval import Evaluator, to_python
from fs_eval.sketch import Curve, Loop, recorded_sketch
from fs_eval.values import FSMap, untag

# Where Robot frame's models go, in the code folder, and the module mapping frames to them
MODELS_FOLDER = "released/frame/models"
MODELS_MODULE = "released/frame/frameModels.gen.fs"

# Before the start of a model's holes, so Hole start distance can move them this far along
LEAD = 2 * 0.0254

# Models reach this much further than they say (`LEAD` back, and their length forward), so the feature never cuts them
# where they end
MARGIN = 0.5 * 0.0254

# Frames whose geometry differs by no more than this share a model (see `FrameGeometry.matches`), built from the first of
# them: vendors give the same holes as 0.196 in. and 5 mm (0.1969 in.). Linear stock finds a modeled hole's faces in a
# region a little bigger than the hole the frame says (see its `HOLE_REGION_MARGIN`), which covers this.
TOLERANCE = 0.05e-3

# Tools for holes reach this far past the frame, so they don't end on its faces
_TOOL_MARGIN = 0.001

_FRAME_MODULE = "released/frame/robotFrame.fs"
_STOCK_MODULE = "core/linearStock.fs"
_TABLES = {"FRC": "frcFrameTable", "FTC": "ftcFrameTable"}

# A model's file name: its part name's words, e.g. 2x1-tube-wcp-1-16-in-wall.gen.step
MODEL_SUFFIX = ".gen.step"


class ModelError(Exception):
    pass


@dataclasses.dataclass
class Frame:
    """A COTS frame: its `program`, its `path` through its lookup table, and its `Stock` (as the evaluator returns
    it)."""

    program: str
    path: dict[str, str]
    stock: FSMap

    @property
    def part_name(self) -> str:
        return untag(self.stock).get_str("partName")

    @property
    def file_name(self) -> str:
        return model_file_name(self.part_name)

    @property
    def length(self) -> float:
        """How long its model is past its start: the longest it's sold in."""
        lengths = [entry["length"] for entry in to_python(untag(self.stock).get_str("stock"))]
        return max(lengths)


def model_file_name(part_name: str) -> str:
    words = re.findall(r"[a-z0-9]+", part_name.lower())
    return "-".join(words) + MODEL_SUFFIX


def frames(evaluator: Evaluator) -> list[Frame]:
    """Every COTS frame, FRC's then FTC's, as Robot frame builds it with its holes where they're sold."""
    module = evaluator.module(_FRAME_MODULE)
    result = []
    for program, table_name in _TABLES.items():
        table = evaluator.eval(table_name, module)
        for path in _leaf_paths(table):
            definition = evaluator.eval(
                f'{{ "program" : Program.{program}, "source" : FrameSource.COTS, "{program.lower()}Frame" : path as LookupTablePath }}',
                module,
                path=path,
            )
            entry = evaluator.eval("frameTableEntry(definition)", module, definition=definition)
            definition = evaluator.eval(
                'mergeMaps(definition, { "holeStart" : entry.tieStart, "holeDiameter" : entry.holeDiameter })',
                module,
                definition=definition,
                entry=entry,
            )
            result.append(Frame(program, path, evaluator.eval("getFrame(definition)", module, definition=definition)))
    names = [frame.file_name for frame in result]
    duplicates = sorted({name for name in names if names.count(name) > 1})
    if duplicates:
        raise ModelError(f"Frames' part names make the same file names: {', '.join(duplicates)}")
    return result


def _leaf_paths(table) -> Iterator[dict[str, str]]:
    """Every path through a lookup table to an entry, as a map of each level's name to its choice."""

    def visit(node, path: dict[str, str]) -> Iterator[dict[str, str]]:
        node = untag(node)
        for key, child in untag(node.get_str("entries")).sorted_items():
            child_path = {**path, node.get_str("name"): key}
            child = untag(child)
            if type(child) is FSMap and child.get_str("entries") is not None and child.get_str("name") is not None:
                yield from visit(child, child_path)
            else:
                yield child_path

    yield from visit(table, {})


# The frame's geometry, from the evaluator


@dataclasses.dataclass
class Wall:
    """A face of a frame its holes go in from (see linear stock's `stockFaces`): its plane, in meters, with `x` along
    the frame, and how `deep` its holes go."""

    origin: tuple[float, float, float]
    normal: tuple[float, float, float]
    x: tuple[float, float, float]
    depth: float

    @property
    def y(self) -> tuple[float, float, float]:
        return _cross(self.normal, self.x)

    def at(self, u: float, v: float, w: float = 0.0) -> tuple[float, float, float]:
        """The point `u` along it, `v` across it, and `w` along its normal."""
        y = self.y
        return tuple(self.origin[i] + u * self.x[i] + v * y[i] + w * self.normal[i] for i in range(3))


@dataclasses.dataclass
class HoleRow:
    """A row of holes in a wall: the loops of the holes at its first position, `start` along the frame, which repeat
    every `pitch`, each reaching `extent` along the frame from its position."""

    wall: Wall
    loops: list[Loop]
    start: float
    pitch: float
    extent: float

    def positions(self, length: float) -> list[float]:
        """Where its holes are on a frame `length` long: those which start before its end."""
        positions = []
        position = self.start
        while position - self.extent < length - 1e-9:
            positions.append(position)
            position += self.pitch
        return positions


@dataclasses.dataclass
class FrameGeometry:
    """A frame's profile and rows of holes, in meters."""

    profile: list[Loop]
    rows: list[HoleRow]

    def matches(self, other: FrameGeometry) -> bool:
        """Whether frames with these geometries can share a model: they're the same, but for lengths at most
        `TOLERANCE` apart (like the clear and black anodized of an extrusion, or a 2x1 tube with 0.196 in. holes and one
        with 5 mm holes)."""
        return _matches(self, other)


def _matches(a, b) -> bool:
    if isinstance(a, bool) or isinstance(b, bool):
        return a == b
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return abs(a - b) <= TOLERANCE
    if isinstance(a, (list, tuple)) and isinstance(b, (list, tuple)):
        return len(a) == len(b) and all(_matches(x, y) for x, y in zip(a, b))
    if dataclasses.is_dataclass(a) and dataclasses.is_dataclass(b):
        return type(a) is type(b) and all(
            _matches(getattr(a, field.name), getattr(b, field.name))
            for field in dataclasses.fields(a)
            if field.name != "id"
        )
    return a == b


def geometry(evaluator: Evaluator, frame: Frame) -> FrameGeometry:
    """What a frame's model is built from: its profile, and its rows of holes."""
    module = evaluator.module(_STOCK_MODULE)
    stock = frame.stock
    context = evaluator.eval("newContext()")
    sketch = evaluator.eval('newSketchOnPlane(context, newId() + "profile", { "sketchPlane" : XY_PLANE })', context=context)
    evaluator.eval("sketchProfile(sketch, stock, 1)", module, sketch=sketch, stock=stock)
    evaluator.eval("skSolve(sketch)", sketch=sketch)
    profile = recorded_sketch(context, "profile").loops()
    if not all(loop.closed for loop in profile):
        raise ModelError(f"{frame.part_name}'s profile isn't closed")

    faces = evaluator.eval(
        "stockFaces(stock, coordSystem(vector(0, 0, 0) * meter, vector(1, 0, 0), vector(0, 0, 1)), 1)",
        module,
        stock=stock,
    )
    hole_diameter = untag(stock).get_str("holeDiameter")
    rows = []
    for face in untag(faces).items:
        face = untag(face)
        plane = to_python(face.get_str("plane"))
        wall = Wall(tuple(plane["origin"]), tuple(plane["normal"]), tuple(plane["x"]), to_python(face.get_str("depth")))
        sign = float(to_python(face.get_str("sign")))
        for index, row in enumerate(untag(face.get_str("rows")).items):
            loops = []
            for shape_index, shape in enumerate(untag(untag(row).get_str("shapes")).items):
                shape_context = evaluator.eval("newContext()")
                shape_sketch = evaluator.eval(
                    'newSketchOnPlane(context, newId() + "shape", { "sketchPlane" : XY_PLANE })', context=shape_context
                )
                evaluator.eval(
                    'sketchShape(sketch, "shape", vector(shape.along, sign * shape.offset), shape, holeDiameter)',
                    module,
                    sketch=shape_sketch,
                    shape=shape,
                    sign=sign,
                    holeDiameter=hole_diameter,
                )
                evaluator.eval("skSolve(sketch)", sketch=shape_sketch)
                shape_loops = recorded_sketch(shape_context, "shape").loops()
                if not all(loop.closed for loop in shape_loops):
                    raise ModelError(f"{frame.part_name}'s hole {index}.{shape_index} isn't closed")
                loops.extend(shape_loops)
            values = to_python(row)
            extent = to_python(evaluator.eval("rowExtent(row, holeDiameter)", module, row=row, holeDiameter=hole_diameter))
            rows.append(HoleRow(wall, loops, values["start"], values["pitch"], extent))
    return FrameGeometry(profile, rows)


@dataclasses.dataclass
class Model:
    """A model of the frames with the same geometry (see `FrameGeometry.matches`), built from and named after the first of
    them (see `model_file_name`), as long as the longest of them is sold."""

    file_name: str
    geometry: FrameGeometry
    frames: list[Frame]

    @property
    def length(self) -> float:
        return max(frame.length for frame in self.frames)

    @property
    def namespace(self) -> str:
        """What the generated module imports it as, e.g. `Model2x1TubeWcp116InWall`."""
        words = self.file_name.removesuffix(MODEL_SUFFIX).split("-")
        return "Model" + "".join(word[:1].upper() + word[1:] for word in words)


def models(evaluator: Evaluator) -> list[Model]:
    """The models of every COTS frame, one for each geometry, by file name."""
    result: list[Model] = []
    for frame in sorted(frames(evaluator), key=lambda frame: frame.file_name):
        frame_geometry = geometry(evaluator, frame)
        model = next((model for model in result if model.geometry.matches(frame_geometry)), None)
        if model is not None:
            model.frames.append(frame)
        else:
            result.append(Model(frame.file_name, frame_geometry, [frame]))
    return result


# Modeling, with OpenCascade


def build(model: Model):
    """A model's solid (an OpenCascade TopoDS_Shape), in millimeters: from `LEAD` before its start to its length, and
    `MARGIN` past both."""
    from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut, BRepAlgoAPI_Fuse
    from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
    from OCP.gp import gp_Pnt, gp_Trsf

    # Its profile: loops nested an even number of loops deep are solid, and those an odd number deep are holes
    geometry = model.geometry
    depths = _nesting_depths(geometry.profile)
    body = None
    for depth in sorted(set(depths)):
        for loop, loop_depth in zip(geometry.profile, depths):
            if loop_depth != depth:
                continue
            prism = _prism(loop, _XY_WALL, -LEAD - MARGIN, model.length + LEAD + 2 * MARGIN)
            if body is None:
                body = prism
            elif depth % 2 == 0:
                body = _boolean(BRepAlgoAPI_Fuse, body, [prism])
            else:
                body = _boolean(BRepAlgoAPI_Cut, body, [prism])
    assert body is not None

    tools = []
    for row in geometry.rows:
        for position in row.positions(model.length + MARGIN):
            for loop in row.loops:
                shifted = _shifted(loop, position)
                tools.append(_prism(shifted, row.wall, -_TOOL_MARGIN, row.wall.depth + 2 * _TOOL_MARGIN))
    if tools:
        body = _boolean(BRepAlgoAPI_Cut, body, tools)
    # In millimeters, which OpenCascade's lengths are in
    scale = gp_Trsf()
    scale.SetScale(gp_Pnt(0, 0, 0), 1000)
    return BRepBuilderAPI_Transform(body, scale).Shape()


# The XY plane as a wall, so profiles are built like holes: `u` along X, `v` along Y, extruded along Z
_XY_WALL = Wall((0.0, 0.0, 0.0), (0.0, 0.0, 1.0), (1.0, 0.0, 0.0), 0.0)


def _boolean(operation, body, tools):
    from OCP.collections import List_TopoDS_Shape

    arguments = List_TopoDS_Shape()
    arguments.Append(body)
    tool_list = List_TopoDS_Shape()
    for tool in tools:
        tool_list.Append(tool)
    result = operation()
    result.SetArguments(arguments)
    result.SetTools(tool_list)
    result.SetRunParallel(True)
    result.Build()
    if not result.IsDone():
        raise ModelError(f"{operation.__name__} failed")
    return result.Shape()


def _shifted(loop: Loop, u: float) -> Loop:
    """A loop moved `u` along X."""

    def move(point):
        return (point[0] + u, point[1])

    curves = [
        dataclasses.replace(
            curve,
            start=move(curve.start),
            end=move(curve.end),
            center=move(curve.center) if curve.center else None,
            points=[move(point) for point in curve.points] if curve.points else curve.points,
        )
        for curve in loop.curves
    ]
    return Loop(curves, loop.closed)


def _prism(loop: Loop, wall: Wall, start: float, depth: float):
    """A loop sketched on a wall, `start` along its normal, extruded `depth` along it."""
    from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeFace, BRepBuilderAPI_MakeWire
    from OCP.BRepPrimAPI import BRepPrimAPI_MakePrism
    from OCP.gp import gp_Vec

    wire = BRepBuilderAPI_MakeWire()
    for curve in loop.curves:
        wire.Add(_edge(curve, wall, start))
    if not wire.IsDone():
        raise ModelError("A loop doesn't make a wire")
    face = BRepBuilderAPI_MakeFace(wire.Wire(), True)
    if not face.IsDone():
        raise ModelError("A loop doesn't make a face")
    normal = wall.normal
    return BRepPrimAPI_MakePrism(face.Face(), gp_Vec(*(n * depth for n in normal))).Shape()


def _edge(curve: Curve, wall: Wall, w: float):
    from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeEdge
    from OCP.GC import GC_MakeArcOfCircle, GC_MakeCircle
    from OCP.GeomAPI import GeomAPI_Interpolate
    from OCP.gp import gp_Dir, gp_Pnt
    from OCP.collections import HArray1_gp_Pnt

    def point(p) -> gp_Pnt:
        return gp_Pnt(*wall.at(p[0], p[1], w))

    if curve.kind == "line":
        return BRepBuilderAPI_MakeEdge(point(curve.start), point(curve.end)).Edge()
    if curve.kind == "circle":
        assert curve.center and curve.radius
        circle = GC_MakeCircle(point(curve.center), gp_Dir(*wall.normal), curve.radius).Value()
        return BRepBuilderAPI_MakeEdge(circle).Edge()
    if curve.kind == "arc":
        assert curve.center and curve.radius
        return BRepBuilderAPI_MakeEdge(
            GC_MakeArcOfCircle(point(curve.start), point(_arc_middle(curve)), point(curve.end)).Value()
        ).Edge()
    if curve.kind == "spline":
        assert curve.points
        points = HArray1_gp_Pnt(1, len(curve.points))
        for i, p in enumerate(curve.points, 1):
            points.SetValue(i, point(p))
        interpolate = GeomAPI_Interpolate(points, False, 1e-9)
        interpolate.Perform()
        if not interpolate.IsDone():
            raise ModelError("A fit spline can't be interpolated")
        return BRepBuilderAPI_MakeEdge(interpolate.Curve()).Edge()
    raise ModelError(f"Can't model a {curve.kind}")


def _arc_middle(curve: Curve) -> tuple[float, float]:
    assert curve.center and curve.radius
    cx, cy = curve.center
    start = math.atan2(curve.start[1] - cy, curve.start[0] - cx)
    end = math.atan2(curve.end[1] - cy, curve.end[0] - cx)
    sweep = (end - start) % (2 * math.pi) if curve.counter_clockwise else -((start - end) % (2 * math.pi))
    middle = start + sweep / 2
    return (cx + curve.radius * math.cos(middle), cy + curve.radius * math.sin(middle))


def _nesting_depths(loops: list[Loop]) -> list[int]:
    """How many of the other loops each loop is inside."""
    polygons = [_polygon(loop) for loop in loops]
    depths = []
    for i, polygon in enumerate(polygons):
        inside = polygon[0]
        depths.append(sum(1 for j, other in enumerate(polygons) if j != i and _contains(other, inside)))
    return depths


def _polygon(loop: Loop, segments: int = 16) -> list[tuple[float, float]]:
    """Points along a loop, closely enough to tell which loops are inside which."""
    points = []
    for curve in loop.curves:
        if curve.kind == "spline":
            points.extend(curve.points or [])
        elif curve.kind in ("arc", "circle"):
            assert curve.center and curve.radius
            cx, cy = curve.center
            start = math.atan2(curve.start[1] - cy, curve.start[0] - cx)
            if curve.kind == "circle":
                sweep = 2 * math.pi
            else:
                end = math.atan2(curve.end[1] - cy, curve.end[0] - cx)
                sweep = (end - start) % (2 * math.pi) if curve.counter_clockwise else -((start - end) % (2 * math.pi))
            points.extend(
                (cx + curve.radius * math.cos(start + sweep * k / segments), cy + curve.radius * math.sin(start + sweep * k / segments))
                for k in range(segments)
            )
        else:
            points.append(curve.start)
    return points


def _contains(polygon: list[tuple[float, float]], point: tuple[float, float]) -> bool:
    x, y = point
    inside = False
    for (x1, y1), (x2, y2) in zip(polygon, polygon[1:] + polygon[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            inside = not inside
    return inside


def _cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


# Writing


def step_text(shape, name: str) -> str:
    """A shape as a STEP file (AP214, in millimeters), with a fixed header, so the same shape always writes the same
    file."""
    import tempfile

    from OCP.IFSelect import IFSelect_RetDone
    from OCP.Interface import Interface_Static
    from OCP.STEPControl import STEPControl_AsIs, STEPControl_Writer

    # Made first, so the settings below exist
    writer = STEPControl_Writer()
    Interface_Static.SetCVal_s("write.step.schema", "AP214IS")
    Interface_Static.SetCVal_s("write.step.unit", "MM")
    # Without the curves' parameters on each surface, which Onshape works out itself: a third of the file
    Interface_Static.SetIVal_s("write.surfacecurve.mode", 0)
    Interface_Static.SetCVal_s("write.step.product.name", name)
    if writer.Transfer(shape, STEPControl_AsIs) != IFSelect_RetDone:
        raise ModelError(f"Can't write {name} as STEP")
    with tempfile.TemporaryDirectory() as folder:
        path = pathlib.Path(folder) / "model.step"
        if writer.Write(str(path)) != IFSelect_RetDone:
            raise ModelError(f"Can't write {name} as STEP")
        text = path.read_text()
    # The header's timestamp and the translator's name change between runs and versions; the model doesn't
    text = re.sub(r"FILE_NAME\(.*?\);", f"FILE_NAME('{_step_string(name)}','',(''),(''),'','','');", text, count=1, flags=re.S)
    # Nor does the count OpenCascade adds to the product's name, of the files written so far
    quoted = re.escape(_step_string(name))
    return re.sub(rf"'{quoted} \d+'", f"'{_step_string(name)}'", text)


def _step_string(text: str) -> str:
    return text.replace("'", "''")


def _quiet() -> None:
    """Stops OpenCascade printing statistics as it writes each file."""
    from OCP.Message import Message, Message_PrinterOStream

    Message.DefaultMessenger_s().RemovePrinters(Message_PrinterOStream.get_type_descriptor_s())


def write_models(code_dir: pathlib.Path, models: list[Model], dry_run: bool = False) -> tuple[list[str], list[str]]:
    """Builds each model and writes it to the models folder, where it's changed; and deletes the models no frame has
    any more. Returns the paths written and deleted, relative to the code folder."""
    _quiet()
    folder = code_dir / MODELS_FOLDER
    written = []
    for model in models:
        path = folder / model.file_name
        text = step_text(build(model), model.file_name.removesuffix(MODEL_SUFFIX))
        data = text.encode()
        if not path.is_file() or path.read_bytes() != data:
            if not dry_run:
                folder.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
            written.append(path.relative_to(code_dir).as_posix())
    names = {model.file_name for model in models}
    deleted = []
    for path in sorted(folder.glob("*" + MODEL_SUFFIX)) if folder.is_dir() else []:
        if path.name not in names:
            if not dry_run:
                path.unlink()
            deleted.append(path.relative_to(code_dir).as_posix())
    return written, deleted


# The module mapping frames to their models (see featurescripts/released/frame/frameModels.py)


def generated_contents(start: pathlib.Path) -> list:
    """What `fs gen` writes to frameModels.gen.fs: an import of each model's Part Studio, and `FRAME_MODELS`, each COTS
    frame's model by its part name. Models which haven't been built yet (by `fs models`) are left out, so the frame
    tables can be generated first."""
    from fs_cli.gen import Code, Import
    from fs_cli.tables import string

    evaluator = Evaluator(start)
    folder = evaluator.config.code_dir / MODELS_FOLDER
    frame_models = [model for model in models(evaluator) if (folder / model.file_name).is_file()]
    # The Part Studio Onshape translates each model into (FeatureScript can't import the model's tab itself)
    imports = [
        Import(f"{MODELS_FOLDER}/{model.file_name.removesuffix('.step')}", namespace=model.namespace)
        for model in frame_models
    ]
    entries = []
    for model in frame_models:
        for frame in sorted(model.frames, key=lambda frame: frame.part_name):
            value = (
                f'{{ "build" : {model.namespace}::build, "length" : {_length(model.length)}, '
                f'"lead" : {_length(LEAD)} }}'
            )
            entries.append(f"        {string(frame.part_name)} : {value}")
    code = (
        "/**\n"
        " * The model Robot frame instantiates for each COTS frame, by its part name (see fs_cli/stock_models.py): the\n"
        " * `build` of the Part Studio Onshape translates it into, how far its holes go (`length`, from where they start\n"
        " * as they're sold, as long as it's sold), and how far it reaches back before its holes start (`lead`).\n"
        " */\n"
        "export const FRAME_MODELS = {\n" + ",\n".join(entries) + "\n    };\n"
    )
    return [*imports, Code(code)]


def _length(meters: float) -> str:
    """A length as FeatureScript, in inches if it's a round number of them, and otherwise millimeters."""
    from fs_cli.tables import inch, mm

    inches = meters / 0.0254
    if abs(inches * 8 - round(inches * 8)) < 1e-6:
        return inch(round(inches * 8) / 8)
    return mm(round(meters * 1000, 6))
