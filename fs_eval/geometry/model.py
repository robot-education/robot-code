"""A Part Studio's geometry, as OpenCascade shapes: its bodies, and every face, edge, and vertex in them, each named
by a transient id (as Onshape's `qTransient` names them) and knowing which operation created it, which is what
`qCreatedBy` and the other queries find them by.

Bodies are replaced, never changed: an operation which changes a body (a boolean) makes a new `Body` with the same
transient id, so a feature's rollback (`abortFeature`) restores the bodies as they were at its start.
"""

from __future__ import annotations

import dataclasses
import itertools

from OCP.TopAbs import TopAbs_EDGE, TopAbs_FACE, TopAbs_SOLID, TopAbs_VERTEX
from OCP.TopExp import TopExp_Explorer
from OCP.TopoDS import TopoDS_Shape

ENTITY_TYPES = ("BODY", "FACE", "EDGE", "VERTEX")
_SUB_TYPES = {"FACE": TopAbs_FACE, "EDGE": TopAbs_EDGE, "VERTEX": TopAbs_VERTEX}


@dataclasses.dataclass
class Entity:
    """A face, edge, or vertex: its shape, the body it's in, and who made it."""

    tid: str
    kind: str  # FACE, EDGE, or VERTEX
    shape: TopoDS_Shape
    body: str
    created_by: tuple[str, ...]
    # Its role in the operation that made it: "start" or "end" for an extrude's caps, "region" for a sketch's regions
    role: str | None = None


@dataclasses.dataclass
class Body:
    tid: str
    body_type: str  # SOLID, SHEET, WIRE, POINT, or MATE_CONNECTOR
    shape: TopoDS_Shape | None
    created_by: tuple[str, ...]
    sketch: tuple[str, ...] | None = None  # The sketch it's part of
    construction: bool = False
    properties: dict = dataclasses.field(default_factory=dict)
    # A mate connector's coordinate system (origin, x, z), and the body it's owned by
    coord_system: tuple | None = None
    owner: str | None = None
    # Its faces', edges', and vertices' transient ids, in order
    faces: tuple[str, ...] = ()
    edges: tuple[str, ...] = ()
    vertices: tuple[str, ...] = ()

    def entities(self, kind: str) -> tuple[str, ...]:
        return {"FACE": self.faces, "EDGE": self.edges, "VERTEX": self.vertices}[kind]


class Model:
    def __init__(self):
        self.bodies: dict[str, Body] = {}
        self.entities: dict[str, Entity] = {}
        # Shapes' transient ids, by hash (then IsSame), so a shape that survives an operation keeps its id
        self._by_hash: dict[int, list[tuple[TopoDS_Shape, str]]] = {}
        self._ids = itertools.count(1)
        self.statuses: dict[tuple, dict] = {}
        self._snapshots: dict[tuple, tuple] = {}
        # (entity tid, attribute value), in the order they were set
        self.attributes: list[tuple[str, object]] = []
        # Faces' properties (a nylock's insert's appearance), by face tid
        self.face_properties: dict[str, dict] = {}
        self.sketches: dict[tuple[str, ...], dict] = {}
        # The last error an operation raised, for reporting why a feature failed
        self.errors: list[str] = []

    # Ids

    def new_id(self, prefix: str) -> str:
        return f"{prefix}{next(self._ids)}"

    def id_of(self, shape: TopoDS_Shape) -> str | None:
        for other, tid in self._by_hash.get(hash(shape), ()):
            if other.IsSame(shape):
                return tid
        return None

    def _remember(self, shape: TopoDS_Shape, tid: str) -> None:
        self._by_hash.setdefault(hash(shape), []).append((shape, tid))

    # Bodies

    def add_body(self, body_type: str, shape: TopoDS_Shape | None, created_by: tuple[str, ...], *, tid: str | None = None,
                 creators: dict | None = None, roles: dict | None = None, **fields) -> Body:
        """Adds a body (or replaces the one with `tid`), naming its faces, edges, and vertices: those it shares with
        shapes already named keep their ids and creators; the rest are new, created by `creators`' entry for them (a
        function of the shape) or else by `created_by`."""
        tid = tid or self.new_id("B")
        lists = {}
        if shape is not None:
            for kind, abs_type in _SUB_TYPES.items():
                names = []
                for sub in subshapes(shape, abs_type):
                    sub_id = self.id_of(sub)
                    if sub_id is None or sub_id not in self.entities:
                        sub_id = sub_id or self.new_id(kind[0])
                        creator = creators(sub) if creators else None
                        role = roles(sub) if roles else None
                        self.entities[sub_id] = Entity(sub_id, kind, sub, tid, creator or created_by, role)
                        self._remember(sub, sub_id)
                    else:
                        entity = self.entities[sub_id]
                        if entity.body != tid:
                            self.entities[sub_id] = dataclasses.replace(entity, body=tid, shape=sub)
                    names.append(sub_id)
                lists[kind] = tuple(names)
        body = Body(tid, body_type, shape, created_by, faces=lists.get("FACE", ()), edges=lists.get("EDGE", ()),
                    vertices=lists.get("VERTEX", ()), **fields)
        self.bodies[tid] = body
        return body

    def remove_body(self, tid: str) -> None:
        self.bodies.pop(tid, None)
        for mate in [b.tid for b in self.bodies.values() if b.owner == tid]:
            self.bodies.pop(mate, None)

    def owner(self, tid: str) -> Body | None:
        """The body an entity (or body) is."""
        body = self.bodies.get(tid)
        if body is not None:
            return body
        entity = self.entities.get(tid)
        if entity is None:
            return None
        body = self.bodies.get(entity.body)
        # An entity of a body since replaced, but not in it any more, isn't there
        if body is None or tid not in body.entities(entity.kind):
            return None
        return body

    def kind(self, tid: str) -> str:
        return "BODY" if tid in self.bodies else self.entities[tid].kind

    def creator(self, tid: str) -> tuple[str, ...]:
        body = self.bodies.get(tid)
        return body.created_by if body is not None else self.entities[tid].created_by

    def shape(self, tid: str) -> TopoDS_Shape | None:
        body = self.bodies.get(tid)
        return body.shape if body is not None else self.entities[tid].shape

    def alive(self, tid: str) -> bool:
        return self.owner(tid) is not None

    def everything(self, kinds=ENTITY_TYPES) -> list[str]:
        """Every live entity of `kinds`, bodies first, each body's faces, edges, and vertices in order."""
        found = []
        bodies = list(self.bodies.values())
        if "BODY" in kinds:
            found.extend(b.tid for b in bodies)
        for kind in ("FACE", "EDGE", "VERTEX"):
            if kind in kinds:
                for body in bodies:
                    found.extend(body.entities(kind))
        return found

    # Features

    def snapshot(self, feature: tuple) -> None:
        self._snapshots[feature] = (dict(self.bodies), dict(self.face_properties), len(self.attributes))

    def end(self, feature: tuple) -> None:
        self._snapshots.pop(feature, None)

    def abort(self, feature: tuple) -> None:
        saved = self._snapshots.pop(feature, None)
        if saved is not None:
            self.bodies, self.face_properties = dict(saved[0]), dict(saved[1])
            del self.attributes[saved[2]:]


def subshapes(shape: TopoDS_Shape, abs_type) -> list[TopoDS_Shape]:
    """A shape's faces, edges, or vertices, each once (an explorer finds a shared one once per use), in order."""
    found = []
    seen: dict[int, list] = {}
    explorer = TopExp_Explorer(shape, abs_type)
    while explorer.More():
        sub = explorer.Current()
        same = seen.setdefault(hash(sub), [])
        if not any(other.IsSame(sub) for other in same):
            same.append(sub)
            found.append(sub)
        explorer.Next()
    return found


def solids(shape: TopoDS_Shape) -> list[TopoDS_Shape]:
    """The solids in a shape (a compound's, or itself)."""
    found = []
    explorer = TopExp_Explorer(shape, TopAbs_SOLID)
    while explorer.More():
        found.append(explorer.Current())
        explorer.Next()
    return found
