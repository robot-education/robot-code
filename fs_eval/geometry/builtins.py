"""The built-ins which model geometry (`@opExtrude`, `@evaluateQuery`, `@startFeature`, ...), on a context's `Model`:
`GEOMETRY_BUILTINS`, which `fs_eval.Evaluator(geometry=True)` adds to the evaluator's own.

Each reads its definition (FeatureScript values) into plain Python, runs the operation (`fs_eval.geometry.operations`),
and returns what std expects back, made with std's own constructors. Errors are thrown as Onshape's built-ins throw
them (a regen error std's features catch and report), and remembered on the model (`Model.errors`), so a feature's
failure can be explained.
"""

from __future__ import annotations

from fs_eval.geometry import fsvalues as fv
from fs_eval.geometry import operations as ops
from fs_eval.geometry import queries
from fs_eval.geometry.model import Model
from fs_eval.values import Context, FSArray, FSMap, untag


def model_of(context) -> Model:
    context = untag(context)
    if type(context) is not Context:
        raise ValueError("expected a context")
    if context.model is None:
        context.model = Model()
    return context.model


def _maker(interp) -> fv.Maker:
    maker = getattr(interp, "_geometry_maker", None)
    if maker is None:
        maker = fv.Maker(interp)
        interp._geometry_maker = maker
    return maker


def _definition(args, index: int = 2) -> FSMap:
    value = untag(args[index]) if len(args) > index else None
    return value if type(value) is FSMap else FSMap()


def _ids(model: Model, query) -> list[str]:
    return [] if query is None else queries.evaluate(model, query)


def _operation(function):
    """A built-in which runs an operation: what goes wrong is remembered, and thrown as a regen error."""

    def builtin(interp, args, at):
        from fs_eval.interpreter import FSError

        model = model_of(args[0])
        try:
            return function(interp, model, args)
        except FSError:
            raise
        except (ops.OperationError, queries.QueryError, ValueError, KeyError, TypeError, AttributeError, IndexError) as e:
            message = str(e) or type(e).__name__
            model.errors.append(message)
            raise FSError(FSMap.of([("message", "REGEN_ERROR"), ("customMessage", message)]), [f"{at[0]}:{at[1]}"] if at else [])
        except Exception as e:  # OpenCascade's own failures (Standard_Failure and friends)
            message = f"{type(e).__name__}: {e}"
            model.errors.append(message)
            raise FSError(FSMap.of([("message", "REGEN_ERROR"), ("customMessage", message)]), [f"{at[0]}:{at[1]}"] if at else [])

    return builtin


# Features


def b_startFeature(interp, args, at):
    model_of(args[0]).snapshot(fv.id_parts(args[1]))
    return FSMap()


def b_endFeature(interp, args, at):
    model_of(args[0]).end(fv.id_parts(args[1]))
    return None


def b_abortFeature(interp, args, at):
    model_of(args[0]).abort(fv.id_parts(args[1]))
    return None


def b_functionGetFeatureStatus(interp, args, at):
    status = model_of(args[0]).statuses.get(fv.id_parts(args[1]))
    if status is None:
        return FSMap.of([("statusType", "OK"), ("statusEnum", "NO_ERROR")])
    return FSMap.of(list(status.items()))


def b_functionReportFeatureStatus(interp, args, at):
    status = untag(args[2])
    plain = {}
    for key, value in status.sorted_items():
        value = untag(value)
        plain[key] = fv.enum_name(value) if hasattr(value, "enum") else value
    model_of(args[0]).statuses[fv.id_parts(args[1])] = plain
    return None


def b_noop(interp, args, at):
    return None


# Sketches


def _sketch_solve(interp, model, args):
    from fs_eval.builtins import b_skSolve

    b_skSolve(interp, args, None)
    sketch = untag(args[0])
    ops.solve_sketch(model, fv.id_parts(sketch.id), sketch)
    return None


def b_skSolve(interp, args, at):
    sketch = untag(args[0])
    # Run as an operation on the sketch's context's model
    return _operation(lambda i, m, a: _sketch_solve(i, m, args))(interp, [sketch.context], at)


# Operations


def _op_extrude(interp, model, args):
    d = _definition(args)
    end_bound = fv.enum_name(d.get_str("endBound")) or "BLIND"
    end = _depth(end_bound, d.get_str("endDepth"))
    start_bound = fv.enum_name(d.get_str("startBound"))
    start = _depth(start_bound, d.get_str("startDepth")) if start_bound is not None else 0.0
    ops.extrude(model, fv.id_parts(args[1]), _ids(model, d.get_str("entities")), fv.vec3(d.get_str("direction")), end, start)
    return None


def _depth(bound: str, depth) -> float:
    if bound == "BLIND":
        return fv.number(depth)
    if bound == "THROUGH_ALL":
        return ops.THROUGH_ALL
    raise ops.OperationError(f"An extrude's {bound} bound isn't supported")


def _op_revolve(interp, model, args):
    d = _definition(args)
    forward = fv.optional_number(d.get_str("angleForward"), 0.0)
    back = fv.optional_number(d.get_str("angleBack"), 0.0)
    ops.revolve(model, fv.id_parts(args[1]), _ids(model, d.get_str("entities")), fv.line(d.get_str("axis")), forward, back)
    return None


def _op_boolean(interp, model, args):
    d = _definition(args)
    operation = fv.enum_name(d.get_str("operationType"))
    ops.boolean(model, fv.id_parts(args[1]), operation, _ids(model, d.get_str("tools")), _ids(model, d.get_str("targets")),
                fv.boolean(d.get_str("keepTools")))
    return None


def _op_mate_connector(interp, model, args):
    d = _definition(args)
    owners = _ids(model, d.get_str("owner"))
    owner = model.owner(owners[0]).tid if owners else None
    ops.mate_connector(model, fv.id_parts(args[1]), fv.coord_system(d.get_str("coordSystem")), owner)
    return None


def _op_delete_bodies(interp, model, args):
    ops.delete_bodies(model, _ids(model, _definition(args).get_str("entities")))
    return None


def _op_point(interp, model, args):
    ops.construction_point(model, fv.id_parts(args[1]), fv.vec3(_definition(args).get_str("point")))
    return None


def _op_plane(interp, model, args):
    d = _definition(args)
    origin, normal, x = fv.plane(d.get_str("plane"))
    ops.construction_plane(model, fv.id_parts(args[1]), origin, normal, x)
    return None


# Queries


def _evaluate_query(interp, model, args):
    maker = _maker(interp)
    tids = queries.evaluate(model, _definition(args, 1).get_str("query"))
    return FSArray(tuple(maker.transient(tid) for tid in tids))


def _is_query_empty(interp, model, args):
    return not queries.evaluate(model, _definition(args, 1).get_str("query"))


def _evaluate_query_count(interp, model, args):
    return float(len(queries.evaluate(model, _definition(args, 1).get_str("query"))))


# Evaluations


def _single(model: Model, query, what: str) -> str:
    found = _ids(model, query)
    if not found:
        raise ops.OperationError(f"Select a {what}")
    return found[0]


def _ev_surface_definition(interp, model, args):
    from OCP.BRepAdaptor import BRepAdaptor_Surface
    from OCP.TopoDS import TopoDS

    maker = _maker(interp)
    tid = _single(model, _definition(args, 1).get_str("face"), "face")
    entity = model.entities[tid]
    if entity.kind != "FACE":
        raise ops.OperationError("Select a face")
    face = TopoDS.Face(entity.shape)
    surface = BRepAdaptor_Surface(face)
    kind = queries.geometry_type(model, tid)
    reversed_ = face.Orientation() == 1  # TopAbs_REVERSED

    def frame(position):
        loc, x, z = position.Location(), position.XDirection(), position.Direction()
        return (loc.X(), loc.Y(), loc.Z()), (x.X(), x.Y(), x.Z()), (z.X(), z.Y(), z.Z())

    if kind == "PLANE":
        origin, x, normal = frame(surface.Plane().Position())
        if reversed_:
            normal = fv.scale(normal, -1)
        return maker.plane(origin, normal, x)
    if kind == "CYLINDER":
        cylinder = surface.Cylinder()
        origin, x, z = frame(cylinder.Position())
        return maker.call("surfaceGeometry.fs", "cylinder", maker.coord_system(origin, x, z), maker.length(cylinder.Radius()))
    if kind == "CONE":
        cone = surface.Cone()
        origin, x, z = frame(cone.Position())
        # std's cone has its apex at the origin
        apex = cone.Apex()
        return maker.call("surfaceGeometry.fs", "cone", maker.coord_system((apex.X(), apex.Y(), apex.Z()), x, z),
                          maker.angle(abs(cone.SemiAngle())))
    if kind == "SPHERE":
        sphere = surface.Sphere()
        origin, x, z = frame(sphere.Position())
        return maker.call("surfaceGeometry.fs", "sphere", maker.coord_system(origin, x, z), maker.length(sphere.Radius()))
    if kind == "TORUS":
        torus = surface.Torus()
        origin, x, z = frame(torus.Position())
        return maker.call("surfaceGeometry.fs", "torus", maker.coord_system(origin, x, z), maker.length(torus.MinorRadius()),
                          maker.length(torus.MajorRadius()))
    raise ops.OperationError(f"Evaluating a {kind} surface isn't supported")


# Properties and attributes


def _set_property(interp, model, args):
    d = _definition(args, 1)
    property_type = fv.enum_name(d.get_str("propertyType"))
    key = property_type if property_type != "CUSTOM" else f"CUSTOM:{untag(d.get_str('customPropertyId'))}"
    value = d.get_str("value")
    for tid in _ids(model, d.get_str("entities")):
        body = model.bodies.get(tid)
        if body is not None:
            model.bodies[tid] = _with_property(body, key, value)
        else:
            model.face_properties.setdefault(tid, {})[key] = value
    return None


def _with_property(body, key, value):
    import dataclasses

    properties = dict(body.properties)
    properties[key] = value
    return dataclasses.replace(body, properties=properties)


def _get_property(interp, model, args):
    d = _definition(args, 1)
    property_type = fv.enum_name(d.get_str("propertyType"))
    key = property_type if property_type != "CUSTOM" else f"CUSTOM:{untag(d.get_str('customPropertyId'))}"
    tid = _single(model, d.get_str("entity"), "body")
    body = model.bodies.get(tid)
    if body is not None:
        return body.properties.get(key)
    return model.face_properties.get(tid, {}).get(key)


def _set_attribute(interp, model, args):
    d = _definition(args, 1)
    name = untag(d.get_str("name"))
    attribute = d.get_str("attribute")
    for tid in _ids(model, d.get_str("entities")):
        model.attributes[:] = [(t, n, v) for t, n, v in model.attributes if not (t == tid and name is not None and n == name)]
        if attribute is not None:
            model.attributes.append((tid, name, attribute))
    return None


def _matches(value, pattern) -> bool:
    from fs_eval.values import equal, tag_of

    if pattern is None:
        return True
    value, pattern_value = untag(value), untag(pattern)
    if tag_of(pattern) is not None and tag_of(pattern) is not tag_of(value):
        return False
    if type(pattern_value) is FSMap:
        if type(value) is not FSMap:
            return False
        return all(equal(value.get(k), v) for k, v in pattern_value.sorted_items())
    return True


def _get_attributes(interp, model, args):
    d = _definition(args, 1)
    tids = set(_ids(model, d.get_str("entities")))
    name = untag(d.get_str("name"))
    pattern = d.get_str("attributePattern")
    found = []
    from fs_eval.values import key_of

    seen = set()
    for tid, attribute_name, value in model.attributes:
        if tid in tids and (name is None or attribute_name == name) and _matches(value, pattern):
            key = key_of(value)
            if key not in seen:
                seen.add(key)
                found.append(value)
    return FSArray(tuple(found))


def _get_attribute(interp, model, args):
    d = _definition(args, 1)
    found = _ids(model, d.get_str("entity"))
    name = untag(d.get_str("name"))
    if not found:
        return None
    for tid, attribute_name, value in model.attributes:
        if tid == found[0] and attribute_name == name:
            return value
    return None


def _wrap(function):
    return _operation(function)


GEOMETRY_BUILTINS = {
    # Features
    "startFeature": b_startFeature,
    "endFeature": b_endFeature,
    "abortFeature": b_abortFeature,
    "functionGetFeatureStatus": b_functionGetFeatureStatus,
    "functionReportFeatureStatus": b_functionReportFeatureStatus,
    "setFeatureComputedParameter": b_noop,
    "setErrorEntities": b_noop,
    "setHighlightedEntities": b_noop,
    "setDimensionedEntities": b_noop,
    "recordQuery": b_noop,
    "setFeatureHiddenParameters": b_noop,
    "skipOrderDisambiguation": b_noop,
    # Sketches
    "skSolve": b_skSolve,
    # Operations
    "opExtrude": _wrap(_op_extrude),
    "opRevolve": _wrap(_op_revolve),
    "opBoolean": _wrap(_op_boolean),
    "opMateConnector": _wrap(_op_mate_connector),
    "opDeleteBodies": _wrap(_op_delete_bodies),
    "opPoint": _wrap(_op_point),
    "opPlane": _wrap(_op_plane),
    # Queries
    "evaluateQuery": _wrap(_evaluate_query),
    "isQueryEmpty": _wrap(_is_query_empty),
    "evaluateQueryCount": _wrap(_evaluate_query_count),
    # Evaluations
    "evSurfaceDefinition": _wrap(_ev_surface_definition),
    # Properties and attributes
    "setProperty": _wrap(_set_property),
    "getProperty": _wrap(_get_property),
    "setAttribute": _wrap(_set_attribute),
    "getAttributes": _wrap(_get_attributes),
    "getAttribute": _wrap(_get_attribute),
}
