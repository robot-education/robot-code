"""A solved sketch as geometry: its entities as edges on its plane, and the regions they bound as faces, as Onshape
makes them (a wire body of its edges, and a sheet body of its regions, both sketch bodies).

Sketches aren't solved: each entity is built as it's given (see `fs_eval.sketch`), so only sketches whose entities
are fully given come out right, which every FeatureScript-backed part's are.
"""

from __future__ import annotations

from OCP.BRep import BRep_Builder
from OCP.BRepAlgoAPI import BRepAlgoAPI_Splitter
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeEdge, BRepBuilderAPI_MakeFace, BRepBuilderAPI_MakeVertex, BRepBuilderAPI_MakePolygon
from OCP.BRepClass import BRepClass_FaceClassifier
from OCP.BRepTools import BRepTools
from OCP.GC import GC_MakeArcOfCircle, GC_MakeSegment
from OCP.GeomAPI import GeomAPI_Interpolate
from OCP.gp import gp_Ax2, gp_Circ, gp_Dir, gp_Pln, gp_Pnt, gp_Pnt2d, gp_Ax3
from OCP.collections import HArray1_gp_Pnt as TColgp_HArray1OfPnt
from OCP.TopAbs import TopAbs_EDGE, TopAbs_FACE, TopAbs_IN, TopAbs_VERTEX
from OCP.TopExp import TopExp_Explorer
from OCP.TopoDS import TopoDS, TopoDS_Compound, TopoDS_Shape
from OCP.collections import List_TopoDS_Shape as TopTools_ListOfShape
from OCP.BRep import BRep_Tool

from fs_eval.geometry import fsvalues as fv
from fs_eval.sketch import RecordedSketch

BOUNDS_MARGIN = 10.0


class SketchGeometry:
    def __init__(self, sketch):
        self.origin, self.normal, self.x = fv.plane(sketch.plane)
        self.y = fv.cross(self.normal, self.x)
        recorded = RecordedSketch(sketch)
        self.edges: list[TopoDS_Shape] = []
        self.construction: list[TopoDS_Shape] = []
        for curve in recorded.curves:
            edge = self._edge(curve)
            (self.construction if curve.construction else self.edges).append(edge)
        self.points = [BRepBuilderAPI_MakeVertex(self._point(p)).Vertex() for p in recorded.points]

    def to_world(self, p) -> fv.Vec:
        return fv.add(self.origin, fv.add(fv.scale(self.x, p[0]), fv.scale(self.y, p[1])))

    def _point(self, p) -> gp_Pnt:
        return gp_Pnt(*self.to_world(p))

    def _edge(self, curve) -> TopoDS_Shape:
        if curve.kind == "line":
            return BRepBuilderAPI_MakeEdge(GC_MakeSegment(self._point(curve.start), self._point(curve.end)).Value()).Edge()
        if curve.kind == "circle":
            axis = gp_Ax2(self._point(curve.center), gp_Dir(*self.normal), gp_Dir(*self.x))
            return BRepBuilderAPI_MakeEdge(gp_Circ(axis, curve.radius)).Edge()
        if curve.kind == "arc":
            # Through its start, a point midway along it, and its end
            mid = _arc_middle(curve)
            arc = GC_MakeArcOfCircle(self._point(curve.start), self._point(mid), self._point(curve.end)).Value()
            return BRepBuilderAPI_MakeEdge(arc).Edge()
        if curve.kind == "spline":
            points = TColgp_HArray1OfPnt(1, len(curve.points))
            for i, p in enumerate(curve.points):
                points.SetValue(i + 1, self._point(p))
            interpolate = GeomAPI_Interpolate(points, False, 1e-9)
            interpolate.Perform()
            return BRepBuilderAPI_MakeEdge(interpolate.Curve()).Edge()
        raise ValueError(f"a sketch's {curve.kind} isn't supported")

    def wire_body(self) -> TopoDS_Shape | None:
        shapes = self.edges + self.construction + self.points
        return _compound(shapes) if shapes else None

    def regions(self) -> list[TopoDS_Shape]:
        """The faces the sketch's (non-construction) edges bound: a face big enough to hold them all, split by them,
        but for the pieces touching its border."""
        if not self.edges:
            return []
        reach = max(_reach(edge, self.origin) for edge in self.edges) * 2 + BOUNDS_MARGIN
        plane = gp_Pln(gp_Ax3(gp_Pnt(*self.origin), gp_Dir(*self.normal), gp_Dir(*self.x)))
        big = BRepBuilderAPI_MakeFace(plane, -reach, reach, -reach, reach).Face()
        splitter = BRepAlgoAPI_Splitter()
        arguments, tools = TopTools_ListOfShape(), TopTools_ListOfShape()
        arguments.Append(big)
        for edge in self.edges:
            tools.Append(edge)
        splitter.SetArguments(arguments)
        splitter.SetTools(tools)
        splitter.Build()
        if not splitter.IsDone():
            raise ValueError("the sketch's regions couldn't be found")
        found = []
        explorer = TopExp_Explorer(splitter.Shape(), TopAbs_FACE)
        while explorer.More():
            face = explorer.Current()
            if not _touches_border(face, reach, self):
                found.append(face)
            explorer.Next()
        return found


def filter_inner_loops(faces: list[TopoDS_Shape]) -> list[TopoDS_Shape]:
    """Leaves out regions inside another's hole: within its outer boundary, without touching it."""
    outers = [_outer_face(face) for face in faces]
    kept = []
    for i, face in enumerate(faces):
        inside = _inner_point(face)
        contained = False
        for j, other in enumerate(faces):
            if i == j or inside is None:
                continue
            classifier = BRepClass_FaceClassifier(outers[j], inside, 1e-9)
            if classifier.State() == TopAbs_IN and not _shares_vertex(face, BRepTools.OuterWire_s(TopoDS.Face(other))):
                contained = True
                break
        if not contained:
            kept.append(face)
    return kept


def _arc_middle(curve):
    import math

    cx, cy = curve.center
    a0 = math.atan2(curve.start[1] - cy, curve.start[0] - cx)
    a1 = math.atan2(curve.end[1] - cy, curve.end[0] - cx)
    sweep = (a1 - a0) % (2 * math.pi) if curve.counter_clockwise else -((a0 - a1) % (2 * math.pi))
    mid = a0 + sweep / 2
    return (cx + curve.radius * math.cos(mid), cy + curve.radius * math.sin(mid))


def _compound(shapes) -> TopoDS_Compound:
    builder = BRep_Builder()
    compound = TopoDS_Compound()
    builder.MakeCompound(compound)
    for shape in shapes:
        builder.Add(compound, shape)
    return compound


def _vertices(shape):
    explorer = TopExp_Explorer(shape, TopAbs_VERTEX)
    while explorer.More():
        yield BRep_Tool.Pnt_s(TopoDS.Vertex(explorer.Current()))
        explorer.Next()


def _reach(edge, origin) -> float:
    from OCP.Bnd import Bnd_Box
    from OCP.BRepBndLib import BRepBndLib

    box = Bnd_Box()
    BRepBndLib.Add_s(edge, box)
    xmin, ymin, zmin = box.GetXMin(), box.GetYMin(), box.GetZMin()
    xmax, ymax, zmax = box.GetXMax(), box.GetYMax(), box.GetZMax()
    return max(abs(v - o) for v, o in zip((xmin, ymin, zmin, xmax, ymax, zmax), origin * 2))


def _touches_border(face, reach: float, sketch: SketchGeometry) -> bool:
    for p in _vertices(face):
        d = fv.sub((p.X(), p.Y(), p.Z()), sketch.origin)
        if abs(abs(fv.dot(d, sketch.x)) - reach) < 1e-9 * max(1.0, reach) or abs(abs(fv.dot(d, sketch.y)) - reach) < 1e-9 * max(1.0, reach):
            return True
    return False


def _outer_face(face):
    face = TopoDS.Face(face)
    surface = BRep_Tool.Surface_s(face)
    return BRepBuilderAPI_MakeFace(surface, BRepTools.OuterWire_s(face), True).Face()


def _inner_point(face) -> gp_Pnt | None:
    """A point inside a face: the middle of its largest triangle, meshed."""
    from fs_eval.geometry.tessellate import face_triangles

    best, best_area = None, -1.0
    for a, b, c in face_triangles(face, 1e-5):
        ab, ac = fv.sub(b, a), fv.sub(c, a)
        area = fv.norm(fv.cross(ab, ac))
        if area > best_area:
            best_area = area
            best = fv.scale(fv.add(fv.add(a, b), c), 1 / 3)
    return gp_Pnt(*best) if best is not None else None


def _shares_vertex(face, wire) -> bool:
    mine = list(_vertices(face))
    for p in _vertices(wire):
        for q in mine:
            if p.Distance(q) < 1e-9:
                return True
    return False
