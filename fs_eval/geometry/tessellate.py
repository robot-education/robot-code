"""Meshing shapes into triangles (to draw them, and to find points inside faces), and their edges into polylines."""

from __future__ import annotations

from OCP.BRep import BRep_Tool
from OCP.BRepAdaptor import BRepAdaptor_Curve
from OCP.BRepMesh import BRepMesh_IncrementalMesh
from OCP.GCPnts import GCPnts_TangentialDeflection
from OCP.TopAbs import TopAbs_REVERSED
from OCP.TopLoc import TopLoc_Location
from OCP.TopoDS import TopoDS

Point = tuple[float, float, float]


def mesh(shape, deflection: float, angle: float = 0.35) -> None:
    """Meshes a shape's faces (stored on them), within `deflection` of the surface."""
    BRepMesh_IncrementalMesh(shape, deflection, False, angle, True)


def face_triangles(face, deflection: float | None = None) -> list[tuple[Point, Point, Point]]:
    """A face's triangles, wound so their normals point out of it (meshing it first, if `deflection` is given or it
    isn't meshed)."""
    face = TopoDS.Face(face)
    location = TopLoc_Location()
    triangulation = BRep_Tool.Triangulation_s(face, location)
    if triangulation is None or deflection is not None:
        mesh(face, deflection or 1e-5)
        triangulation = BRep_Tool.Triangulation_s(face, location)
    if triangulation is None:
        return []
    transform = location.Transformation()
    nodes = []
    for i in range(1, triangulation.NbNodes() + 1):
        p = triangulation.Node(i).Transformed(transform)
        nodes.append((p.X(), p.Y(), p.Z()))
    flip = face.Orientation() == TopAbs_REVERSED
    triangles = []
    for i in range(1, triangulation.NbTriangles() + 1):
        a, b, c = triangulation.Triangle(i).Get()
        if flip:
            b, c = c, b
        triangles.append((nodes[a - 1], nodes[b - 1], nodes[c - 1]))
    return triangles


def face_mesh(face) -> tuple[list[Point], list[tuple[int, int, int]], list[Point]]:
    """A (meshed) face's vertices, triangles (indices, wound outward), and vertex normals."""
    from OCP.BRepLib import BRepLib_ToolTriangulatedShape

    face = TopoDS.Face(face)
    location = TopLoc_Location()
    triangulation = BRep_Tool.Triangulation_s(face, location)
    if triangulation is None:
        return [], [], []
    if not triangulation.HasNormals():
        BRepLib_ToolTriangulatedShape.ComputeNormals_s(face, triangulation)
    transform = location.Transformation()
    flip = face.Orientation() == TopAbs_REVERSED
    points, normals = [], []
    for i in range(1, triangulation.NbNodes() + 1):
        p = triangulation.Node(i).Transformed(transform)
        points.append((p.X(), p.Y(), p.Z()))
        n = triangulation.Normal(i).Transformed(transform)
        sign = -1.0 if flip else 1.0
        normals.append((n.X() * sign, n.Y() * sign, n.Z() * sign))
    triangles = []
    for i in range(1, triangulation.NbTriangles() + 1):
        a, b, c = triangulation.Triangle(i).Get()
        triangles.append((a - 1, c - 1, b - 1) if flip else (a - 1, b - 1, c - 1))
    return points, triangles, normals


def edge_polyline(edge, deflection: float, angle: float = 0.2) -> list[Point]:
    """Points along an edge, within `deflection` of it."""
    edge = TopoDS.Edge(edge)
    if BRep_Tool.Degenerated_s(edge):
        return []
    curve = BRepAdaptor_Curve(edge)
    sampler = GCPnts_TangentialDeflection(curve, angle, deflection)
    points = []
    for i in range(1, sampler.NbPoints() + 1):
        p = sampler.Value(i)
        points.append((p.X(), p.Y(), p.Z()))
    return points
