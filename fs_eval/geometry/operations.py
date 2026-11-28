"""The operations FeatureScript-backed parts are built with (`@opExtrude`, `@opRevolve`, `@opBoolean`, ...), on a
`Model`, with what they take as plain Python (see `fs_eval.geometry.builtins`, which reads it from their definitions).

Each names what it makes as Onshape does, so queries find it: new bodies are created by the operation's id, a
boolean's result keeps the identity of the body it changed (a union's first tool, a subtraction's targets), and faces
that survive a boolean keep theirs, those split or trimmed by it their creator's.
"""

from __future__ import annotations

import math

from OCP.BRepAlgoAPI import BRepAlgoAPI_Common, BRepAlgoAPI_Cut, BRepAlgoAPI_Fuse
from OCP.BRepPrimAPI import BRepPrimAPI_MakePrism, BRepPrimAPI_MakeRevol
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeFace, BRepBuilderAPI_MakeVertex, BRepBuilderAPI_Transform
from OCP.gp import gp_Ax1, gp_Ax3, gp_Dir, gp_Pln, gp_Pnt, gp_Trsf, gp_Vec
from OCP.TopAbs import TopAbs_EDGE, TopAbs_FACE, TopAbs_VERTEX
from OCP.collections import List_TopoDS_Shape as TopTools_ListOfShape

from fs_eval.geometry import fsvalues as fv
from fs_eval.geometry.model import Body, Model, solids, subshapes
from fs_eval.geometry.sketches import SketchGeometry, filter_inner_loops

FeatureId = tuple[str, ...]
# How far "through all" reaches
THROUGH_ALL = 10.0


class OperationError(ValueError):
    pass


def solve_sketch(model: Model, sketch_id: FeatureId, sketch) -> None:
    geometry = SketchGeometry(sketch)
    regions = geometry.regions()
    wire = geometry.wire_body()
    if wire is not None:
        model.add_body("WIRE", wire, sketch_id, sketch=sketch_id)
    region_ids: list[str] = []
    if regions:
        from fs_eval.geometry.sketches import _compound

        body = model.add_body("SHEET", _compound(regions), sketch_id, sketch=sketch_id, roles=lambda s: "region")
        region_ids = [model.id_of(face) for face in regions]
        inner = [model.id_of(face) for face in filter_inner_loops(regions)]
    else:
        inner = []
    model.sketches[sketch_id] = {"regions": region_ids, "inner": inner, "plane": (geometry.origin, geometry.normal, geometry.x)}


def _faces(model: Model, tids: list[str]) -> list:
    faces = [model.entities[tid].shape for tid in tids if tid in model.entities and model.entities[tid].kind == "FACE"]
    if not faces:
        raise OperationError("Select faces to extrude or revolve")
    return faces


def _new_solids(model: Model, op: FeatureId, made: list[tuple], merge: bool = True) -> list[Body]:
    """Adds what an extrude or revolve made, one body per solid (each face's, fused where they touch), with their
    start and end caps marked."""
    caps: dict[str, list] = {"start": [], "end": []}
    shapes = []
    for shape, start, end in made:
        shapes.append(shape)
        caps["start"].extend(start)
        caps["end"].extend(end)
    if merge and len(shapes) > 1:
        fused = _fuse(shapes)
        shapes = solids(fused)

    def role(sub):
        for name, faces in caps.items():
            if any(face.IsSame(sub) for face in faces):
                return name
        return None

    return [model.add_body("SOLID", shape, op, roles=role) for shape in shapes]


def extrude(model: Model, op: FeatureId, face_ids: list[str], direction: fv.Vec, end_depth: float, start_depth: float = 0.0) -> list[Body]:
    """Extrudes faces `end_depth` along `direction`, and `start_depth` back from them."""
    direction = fv.unit(direction)
    total = end_depth + start_depth
    if abs(total) < 1e-12:
        raise OperationError("An extrude's depth is zero")
    made = []
    for face in _faces(model, face_ids):
        if start_depth:
            face = _moved(face, fv.scale(direction, -start_depth))
        prism = BRepPrimAPI_MakePrism(face, gp_Vec(*fv.scale(direction, total)))
        prism.Build()
        if not prism.IsDone():
            raise OperationError("The extrude failed")
        start, end = prism.FirstShape(), prism.LastShape()
        made.append((prism.Shape(), [start], [end]))
    return _new_solids(model, op, made)


def revolve(model: Model, op: FeatureId, face_ids: list[str], axis: tuple[fv.Vec, fv.Vec], forward: float, back: float = 0.0) -> list[Body]:
    origin, direction = axis
    ax = gp_Ax1(gp_Pnt(*origin), gp_Dir(*direction))
    total = forward + back
    if total <= 0:
        raise OperationError("A revolve's angle is zero")
    made = []
    for face in _faces(model, face_ids):
        if back:
            trsf = gp_Trsf()
            trsf.SetRotation(ax, -back)
            face = BRepBuilderAPI_Transform(face, trsf, True).Shape()
        full = total >= 2 * math.pi - 1e-12
        revol = BRepPrimAPI_MakeRevol(face, ax, 2 * math.pi if full else total, True)
        revol.Build()
        if not revol.IsDone():
            raise OperationError("The revolve failed")
        caps = ([], []) if full else ([revol.FirstShape()], [revol.LastShape()])
        made.append((revol.Shape(), *caps))
    return _new_solids(model, op, made)


def _moved(shape, offset: fv.Vec):
    trsf = gp_Trsf()
    trsf.SetTranslation(gp_Vec(*offset))
    return BRepBuilderAPI_Transform(shape, trsf, True).Shape()


def _list(shapes) -> TopTools_ListOfShape:
    result = TopTools_ListOfShape()
    for shape in shapes:
        result.Append(shape)
    return result


def _fuse(shapes):
    fuse = BRepAlgoAPI_Fuse()
    fuse.SetArguments(_list(shapes[:1]))
    fuse.SetTools(_list(shapes[1:]))
    fuse.Build()
    if not fuse.IsDone():
        raise OperationError("The union failed")
    return fuse.Shape()


def _history_creators(model: Model, algorithm, inputs: list, op: FeatureId):
    """For the faces, edges, and vertices of a boolean's result: who made the one each came from (the creator of the
    input face it is, or is a piece of), or else the boolean, for what it made itself."""
    origins: dict[int, list] = {}
    for shape in inputs:
        for abs_type in (TopAbs_FACE, TopAbs_EDGE, TopAbs_VERTEX):
            for sub in subshapes(shape, abs_type):
                tid = model.id_of(sub)
                if tid is None or tid not in model.entities:
                    continue
                creator = model.entities[tid].created_by
                for modified in algorithm.Modified(sub):
                    origins.setdefault(hash(modified), []).append((modified, creator))

    def creator(sub):
        for shape, made_by in origins.get(hash(sub), ()):
            if shape.IsSame(sub):
                return made_by
        return op

    return creator


def boolean(model: Model, op: FeatureId, operation: str, tools: list[str], targets: list[str], keep_tools: bool = False) -> None:
    tool_bodies = [model.bodies[t] for t in tools if t in model.bodies and model.bodies[t].body_type == "SOLID"]
    target_bodies = [model.bodies[t] for t in targets if t in model.bodies and model.bodies[t].body_type == "SOLID"]
    if not tool_bodies:
        raise OperationError("Select solid bodies for the boolean's tools")
    if operation == "UNION":
        group = tool_bodies + [b for b in target_bodies if b not in tool_bodies]
        if len(group) < 2:
            return
        fuse = BRepAlgoAPI_Fuse()
        fuse.SetArguments(_list([group[0].shape]))
        fuse.SetTools(_list([b.shape for b in group[1:]]))
        fuse.Build()
        if not fuse.IsDone():
            raise OperationError("The union failed")
        _replace(model, op, fuse, [b.shape for b in group], group, keep=group[0])
    elif operation == "SUBTRACTION":
        if not target_bodies:
            raise OperationError("Select solid bodies for the subtraction's targets")
        for target in target_bodies:
            cut = BRepAlgoAPI_Cut()
            cut.SetArguments(_list([target.shape]))
            cut.SetTools(_list([b.shape for b in tool_bodies]))
            cut.Build()
            if not cut.IsDone():
                raise OperationError("The subtraction failed")
            _replace(model, op, cut, [target.shape] + [b.shape for b in tool_bodies], [target], keep=target)
        if not keep_tools:
            for tool in tool_bodies:
                if tool not in target_bodies:
                    model.remove_body(tool.tid)
    elif operation == "INTERSECTION":
        if len(tool_bodies) < 2:
            raise OperationError("Select at least two bodies to intersect")
        shape = tool_bodies[0].shape
        algorithms = []
        for other in tool_bodies[1:]:
            common = BRepAlgoAPI_Common(shape, other.shape)
            common.Build()
            if not common.IsDone():
                raise OperationError("The intersection failed")
            algorithms.append(common)
            shape = common.Shape()
        creator = _chained_creators(model, algorithms, [b.shape for b in tool_bodies], op)
        for body in tool_bodies:
            model.remove_body(body.tid)
        for solid in solids(shape):
            model.add_body("SOLID", solid, op, creators=creator)
    else:
        raise OperationError(f"Boolean {operation} isn't supported")


def _chained_creators(model, algorithms, inputs, op):
    # Each step's creators, from the faces they came from: run through the steps in turn
    creator = _history_creators(model, algorithms[0], inputs[:2], op)
    for algorithm, extra in zip(algorithms[1:], inputs[2:]):
        previous = creator
        step = _history_creators(model, algorithm, [extra], op)

        def creator(sub, previous=previous, step=step):
            made = step(sub)
            return made if made != op else previous(sub)

    return creator


def _replace(model: Model, op: FeatureId, algorithm, inputs: list, consumed: list[Body], keep: Body) -> None:
    creator = _history_creators(model, algorithm, inputs, op)
    pieces = solids(algorithm.Shape())
    for body in consumed:
        model.remove_body(body.tid)
    mates = [b for b in model.bodies.values() if b.owner == keep.tid]
    for i, piece in enumerate(pieces):
        model.add_body("SOLID", piece, keep.created_by if i == 0 else op, tid=keep.tid if i == 0 else None,
                       creators=creator, properties=dict(keep.properties) if i == 0 else {})
    for mate in mates:
        model.bodies[mate.tid] = mate


def mate_connector(model: Model, op: FeatureId, coord_system: tuple, owner: str | None) -> Body:
    origin = coord_system[0]
    vertex = BRepBuilderAPI_MakeVertex(gp_Pnt(*origin)).Vertex()
    body = model.add_body("MATE_CONNECTOR", None, op, coord_system=coord_system, owner=owner)
    body.shape = vertex
    return body


def delete_bodies(model: Model, tids: list[str]) -> None:
    for tid in tids:
        body = model.owner(tid)
        if body is not None and body.tid == tid:
            model.remove_body(tid)


def construction_plane(model: Model, op: FeatureId, origin: fv.Vec, normal: fv.Vec, x: fv.Vec, size: float = 0.15) -> Body:
    plane = gp_Pln(gp_Ax3(gp_Pnt(*origin), gp_Dir(*normal), gp_Dir(*x)))
    face = BRepBuilderAPI_MakeFace(plane, -size / 2, size / 2, -size / 2, size / 2).Face()
    return model.add_body("SHEET", face, op, construction=True)


def construction_point(model: Model, op: FeatureId, point: fv.Vec) -> Body:
    return model.add_body("POINT", BRepBuilderAPI_MakeVertex(gp_Pnt(*point)).Vertex(), op, construction=True)
