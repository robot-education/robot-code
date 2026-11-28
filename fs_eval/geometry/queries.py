"""Evaluating queries (the maps std's `q...` functions build) against a `Model`: to the transient ids of what they
match, in a stable order (bodies, then each body's faces, edges, and vertices, as they were made)."""

from __future__ import annotations

from OCP.BRepAdaptor import BRepAdaptor_Curve, BRepAdaptor_Surface
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeVertex
from OCP.BRepExtrema import BRepExtrema_DistShapeShape
from OCP.GeomAbs import (
    GeomAbs_BezierSurface,
    GeomAbs_BSplineSurface,
    GeomAbs_Circle,
    GeomAbs_Cone,
    GeomAbs_Cylinder,
    GeomAbs_Line,
    GeomAbs_Plane,
    GeomAbs_SurfaceOfExtrusion,
    GeomAbs_SurfaceOfRevolution,
    GeomAbs_Sphere,
    GeomAbs_Torus,
)
from OCP.gp import gp_Pnt
from OCP.TopoDS import TopoDS

from fs_eval.geometry import fsvalues as fv
from fs_eval.geometry.model import ENTITY_TYPES, Model
from fs_eval.values import FSArray, FSMap, untag

# How near a point must be to an entity to be on it (Onshape's zero length tolerance)
POINT_TOLERANCE = 1e-8


class QueryError(ValueError):
    pass


def evaluate(model: Model, query) -> list[str]:
    query = untag(query)
    if type(query) is FSArray:
        # An array of queries, as some of std's functions take
        return _unique(tid for item in query.items for tid in evaluate(model, item))
    if type(query) is not FSMap:
        raise QueryError(f"expected a query, got {query!r}")
    kind = fv.enum_name(query.get_str("queryType"))
    handler = _HANDLERS.get(kind)
    if handler is None:
        raise QueryError(f"Queries of type {kind} aren't supported by the evaluator's geometry")
    return [tid for tid in handler(model, query) if model.alive(tid)]


def _unique(tids) -> list[str]:
    seen = set()
    found = []
    for tid in tids:
        if tid not in seen:
            seen.add(tid)
            found.append(tid)
    return found


def _kinds(query: FSMap, key: str = "entityType"):
    name = fv.enum_name(query.get_str(key))
    return ENTITY_TYPES if name is None else (name,)


def _sub(model: Model, query: FSMap, key: str = "subquery") -> list[str]:
    return evaluate(model, query.get_str(key))


def _starts_with(creator: tuple[str, ...], prefix: tuple[str, ...]) -> bool:
    return creator[: len(prefix)] == prefix


# Handlers


def _transient(model, query):
    tid = untag(query.get_str("transientId"))
    return [tid] if tid in model.bodies or tid in model.entities else []


def _nothing(model, query):
    return []


def _everything(model, query):
    found = model.everything(_kinds(query))
    body_types = query.get_str("bodyType")
    if body_types is not None:
        allowed = set(fv.enum_names(body_types))
        found = [tid for tid in found if model.owner(tid).body_type in allowed]
    return found


def _union(model, query):
    return _unique(tid for sub in untag(query.get_str("subqueries")).items for tid in evaluate(model, sub))


def _intersection(model, query):
    subs = [evaluate(model, sub) for sub in untag(query.get_str("subqueries")).items]
    if not subs:
        return []
    rest = [set(s) for s in subs[1:]]
    return [tid for tid in subs[0] if all(tid in s for s in rest)]


def _subtraction(model, query):
    removed = set(evaluate(model, query.get_str("query2")))
    return [tid for tid in evaluate(model, query.get_str("query1")) if tid not in removed]


def _created_by(model, query):
    prefix = fv.id_parts(query.get_str("featureId"))
    kinds = _kinds(query)
    return [tid for tid in model.everything(kinds) if _starts_with(model.creator(tid), prefix)]


def _sketch_region(model, query):
    sketch = fv.id_parts(query.get_str("featureId"))
    found = model.sketches.get(sketch)
    if found is None:
        return []
    return list(found["inner" if fv.boolean(query.get_str("filterInnerLoops")) else "regions"])


def _cap_entity(model, query):
    prefix = fv.id_parts(query.get_str("featureId"))
    cap = fv.enum_name(query.get_str("capType")) or "EITHER"
    roles = {"START": ("start",), "END": ("end",), "EITHER": ("start", "end")}[cap]
    kinds = _kinds(query)
    if kinds == ENTITY_TYPES:
        kinds = ("FACE",)
    return [tid for tid in model.everything(kinds)
            if tid in model.entities and model.entities[tid].role in roles and _starts_with(model.creator(tid), prefix)]


def _body_type(model, query):
    allowed = set(fv.enum_names(query.get_str("bodyType")))
    return [tid for tid in _sub(model, query) if model.owner(tid).body_type in allowed]


def _entity_filter(model, query):
    kinds = _kinds(query)
    return [tid for tid in _sub(model, query) if model.kind(tid) in kinds]


def _owned_by_part(model, query):
    owners = []
    for tid in evaluate(model, query.get_str("part")):
        owner = model.owner(tid)
        if owner is not None and owner.tid not in owners:
            owners.append(owner.tid)
    kinds = _kinds(query)
    found = []
    for owner in owners:
        body = model.bodies[owner]
        for kind in ("FACE", "EDGE", "VERTEX"):
            if kind in kinds:
                found.extend(body.entities(kind))
        if "BODY" in kinds and query.get_str("entityType") is not None:
            found.append(owner)
    if query.get_str("subquery") is not None:
        allowed = set(found)
        return [tid for tid in _sub(model, query) if tid in allowed]
    return found


def _owner_part(model, query):
    return _unique(model.owner(tid).tid for tid in evaluate(model, query.get_str("query")))


def _sketch_object(model, query):
    want = fv.enum_name(query.get_str("sketchObjectFilter")) == "YES"
    return [tid for tid in _sub(model, query) if (model.owner(tid).sketch is not None) == want]


def _construction(model, query):
    want = fv.enum_name(query.get_str("constructionFilter")) == "YES"
    return [tid for tid in _sub(model, query) if model.owner(tid).construction == want]


def _nth(model, query):
    found = _sub(model, query)
    n = int(fv.number(query.get_str("n")))
    if n < 0:
        n += len(found)
    return [found[n]] if 0 <= n < len(found) else []


def _geometry(model, query):
    wanted = set(fv.enum_names(query.get_str("geometryType")))
    return [tid for tid in _sub(model, query) if geometry_type(model, tid) in wanted]


def _contains_point(model, query):
    point = fv.vec3(query.get_str("point"))
    vertex = BRepBuilderAPI_MakeVertex(gp_Pnt(*point)).Vertex()
    found = []
    for tid in _sub(model, query):
        shape = model.shape(tid)
        if shape is None:
            body = model.bodies.get(tid)
            if body is not None and body.coord_system is not None and fv.norm(fv.sub(body.coord_system[0], point)) < POINT_TOLERANCE:
                found.append(tid)
            continue
        distance = BRepExtrema_DistShapeShape(shape, vertex)
        if distance.IsDone() and distance.Value() < POINT_TOLERANCE:
            found.append(tid)
    return found


def geometry_type(model: Model, tid: str) -> str | None:
    """A face's or edge's `GeometryType`."""
    if tid not in model.entities:
        return None
    entity = model.entities[tid]
    if entity.kind == "FACE":
        kind = BRepAdaptor_Surface(TopoDS.Face(entity.shape)).GetType()
        return {
            GeomAbs_Plane: "PLANE",
            GeomAbs_Cylinder: "CYLINDER",
            GeomAbs_Cone: "CONE",
            GeomAbs_Sphere: "SPHERE",
            GeomAbs_Torus: "TORUS",
            GeomAbs_SurfaceOfRevolution: "REVOLVED",
            GeomAbs_SurfaceOfExtrusion: "EXTRUDED",
            GeomAbs_BSplineSurface: "OTHER_SURFACE",
            GeomAbs_BezierSurface: "OTHER_SURFACE",
        }.get(kind, "OTHER_SURFACE")
    if entity.kind == "EDGE":
        curve = BRepAdaptor_Curve(TopoDS.Edge(entity.shape))
        kind = curve.GetType()
        if kind == GeomAbs_Line:
            return "LINE"
        if kind == GeomAbs_Circle:
            return "CIRCLE" if curve.IsClosed() else "ARC"
        return "OTHER_CURVE"
    return None


_HANDLERS = {
    "TRANSIENT": _transient,
    "NOTHING": _nothing,
    "EVERYTHING": _everything,
    "UNION": _union,
    "INTERSECTION": _intersection,
    "SUBTRACTION": _subtraction,
    "CREATED_BY": _created_by,
    "SKETCH_REGION": _sketch_region,
    "CAP_ENTITY": _cap_entity,
    "BODY_TYPE": _body_type,
    "ENTITY_FILTER": _entity_filter,
    "OWNED_BY_PART": _owned_by_part,
    "OWNER_PART": _owner_part,
    "SKETCH_OBJECT_FILTER": _sketch_object,
    "CONSTRUCTION_FILTER": _construction,
    "NTH_ELEMENT": _nth,
    "GEOMETRY": _geometry,
    "CONTAINS_POINT": _contains_point,
}
