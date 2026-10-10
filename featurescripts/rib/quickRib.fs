FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
export import(path : "onshape/std/manipulator.fs", version : "2960.0");

/**
 * The attribute on Quick rib's sketches, which Robot lighten finds them by.
 */
export const QUICK_RIB_ATTRIBUTE = "robotQuickRib";
/**
 * The attribute Robot lighten puts on the Quick rib sketches it uses, so a new one leaves them out.
 */
export const QUICK_RIB_USED_ATTRIBUTE = "robotQuickRibUsed";

const CIRCLE_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;
const LOCATION_BOUNDS = { (unitless) : [0, 0.5, 1] } as RealBoundSpec;
const EDGE_POINT_MANIPULATOR = "edgePoint";

/**
 * Sketches ribs (lines, and circles) between circles' centers, points, mate connectors, and points on edges, quickly:
 * for Robot lighten, which finds them itself. Chains run through their points in order (closed or not; two points make
 * one rib); hubs have spokes to their hub, and a circle around it if asked.
 */
annotation { "Feature Type Name" : "Quick rib",
        "Feature Type Description" : "Sketch ribs between circles and points, as chains or hubs and spokes, for Robot lighten." ~ CREDIT,
        "Editing Logic Function" : "quickRibEditLogic",
        "Manipulator Change Function" : "quickRibManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const quickRib = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face to use", "MaxNumberOfPicks" : 1, "Filter" : EntityType.FACE && GeometryType.PLANE && SketchObject.NO,
                    "Description" : "Where the ribs are sketched (points are projected onto it). Without one, the first circle's plane." }
        definition.face is Query;

        annotation { "Name" : "Chains", "Item name" : "chain", "Item label template" : "#chainPoints", "Driven query" : "chainPoints",
                    "UIHint" : ["FOCUS_INNER_QUERY"] }
        definition.chains is array;

        for (var chain in definition.chains)
        {
            annotation { "Name" : "Points", "Filter" : EntityType.VERTEX || EntityType.EDGE || BodyType.MATE_CONNECTOR }
            chain.chainPoints is Query;

            annotation { "Name" : "Closed" }
            chain.closed is boolean;
        }

        annotation { "Name" : "Hubs", "Item name" : "hub", "Item label template" : "#hub", "Driven query" : "hub",
                    "UIHint" : ["FOCUS_INNER_QUERY"] }
        definition.hubs is array;

        for (var hub in definition.hubs)
        {
            annotation { "Name" : "Hub", "MaxNumberOfPicks" : 1, "Filter" : EntityType.VERTEX || EntityType.EDGE || BodyType.MATE_CONNECTOR }
            hub.hub is Query;

            annotation { "Name" : "Spokes", "Filter" : EntityType.VERTEX || EntityType.EDGE || BodyType.MATE_CONNECTOR }
            hub.spokes is Query;

            annotation { "Name" : "Add circle" }
            hub.addCircle is boolean;

            if (hub.addCircle)
            {
                annotation { "Name" : "Circle diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(hub.circleDiameter, CIRCLE_BOUNDS);
            }
        }

        annotation { "Name" : "Points on edges", "Item name" : "point", "Item label template" : "#edge", "Driven query" : "edge",
                    "UIHint" : ["PREVENT_ARRAY_REORDER"] }
        definition.edgePoints is array;

        for (var edgePoint in definition.edgePoints)
        {
            annotation { "Name" : "Edge", "MaxNumberOfPicks" : 1, "Filter" : EntityType.EDGE, "UIHint" : ["ALWAYS_HIDDEN"] }
            edgePoint.edge is Query;

            annotation { "Name" : "Location", "Description" : "How far along the edge the rib meets it, from its start (0) to its end (1)." }
            isReal(edgePoint.edgeLocation, LOCATION_BOUNDS);
        }

        annotation { "Name" : "Clear invalid selections",
                    "Description" : "Drops selections which no longer exist, chains of fewer than two points, hubs without a hub, and points on edges nothing uses." }
        isButton(definition.clearInvalid);
    }
    {
        var segments = [];
        var circles = [];
        var skipped = 0;
        for (var chain in definition.chains)
        {
            const points = pointsOf(context, definition, chain.chainPoints);
            if (size(points) < 2)
            {
                skipped += 1;
                continue;
            }
            segments = concatenateArrays([segments, chainSegments(points, chain.closed)]);
        }
        for (var hub in definition.hubs)
        {
            const center = pointsOf(context, definition, hub.hub);
            const spokes = pointsOf(context, definition, hub.spokes);
            if (center == [] || (spokes == [] && !hub.addCircle))
            {
                skipped += 1;
                continue;
            }
            segments = concatenateArrays([segments, hubSegments(center[0], spokes)]);
            if (hub.addCircle)
            {
                circles = append(circles, { "center" : center[0], "radius" : hub.circleDiameter / 2 });
            }
        }
        segments = uniqueSegments(segments);
        if (segments == [] && circles == [])
        {
            throw regenError("Add chains or hubs to sketch ribs.", ["chains", "hubs"]);
        }
        if (skipped > 0)
        {
            reportFeatureWarning(context, id, skipped == 1 ? "A chain or hub isn't complete, so it's skipped." :
                    skipped ~ " chains and hubs aren't complete, so they're skipped.");
        }

        const plane = getSketchPlane(context, definition);
        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
        for (var i, segment in segments)
        {
            skLineSegment(sketch, "rib" ~ i, { "start" : worldToPlane(plane, segment[0]), "end" : worldToPlane(plane, segment[1]) });
        }
        for (var i, circle in circles)
        {
            skCircle(sketch, "circle" ~ i, { "center" : worldToPlane(plane, circle.center), "radius" : circle.radius });
        }
        skSolve(sketch);
        setAttribute(context, { "entities" : qCreatedBy(id + "sketch", EntityType.BODY), "name" : QUICK_RIB_ATTRIBUTE, "attribute" : true });

        addEdgePointManipulators(context, id, definition);
    });

/**
 * The points of a selection, in order: a full circle's center, a vertex's point, a mate connector's origin, or a point
 * along another edge (an arc, line, ...), at its Points on edges location (halfway, if it has none). What isn't there
 * any more is left out.
 */
function pointsOf(context is Context, definition is map, selection is Query) returns array
{
    var points = [];
    for (var entity in evaluateQuery(context, selection))
    {
        try silent
        {
            points = append(points, pointOf(context, definition, entity));
        }
    }
    return points;
}

function pointOf(context is Context, definition is map, entity is Query) returns Vector
{
    if (!isQueryEmpty(context, entity->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        return evMateConnector(context, { "mateConnector" : entity }).origin;
    }
    if (!isQueryEmpty(context, entity->qEntityFilter(EntityType.VERTEX)))
    {
        return evVertexPoint(context, { "vertex" : entity });
    }
    if (!isQueryEmpty(context, entity->qGeometry(GeometryType.CIRCLE)))
    {
        return evCurveDefinition(context, { "edge" : entity }).coordSystem.origin;
    }
    return evEdgeTangentLine(context, { "edge" : entity, "parameter" : edgeLocation(context, definition, entity), "arcLengthParameterization" : true }).origin;
}

/**
 * Where along an edge its point is: its Points on edges item's location, or halfway.
 */
function edgeLocation(context is Context, definition is map, edge is Query) returns number
{
    const index = edgePointIndex(context, definition, edge);
    return index == undefined ? 0.5 : definition.edgePoints[index].edgeLocation;
}

/**
 * The index of an edge's Points on edges item, matched by the edge itself (not its place in a chain), or undefined.
 */
function edgePointIndex(context is Context, definition is map, edge is Query)
{
    const evaluated = evaluateQuery(context, edge);
    for (var i, edgePoint in definition.edgePoints)
    {
        if (evaluated != [] && evaluateQuery(context, edgePoint.edge) == evaluated)
        {
            return i;
        }
    }
    return undefined;
}

/**
 * The lines (each `[from, to]`) along a chain's points, back to the start if it's closed (and has more than two).
 */
export function chainSegments(points is array, closed is boolean) returns array
{
    var segments = [];
    const count = size(points);
    for (var i = 0; i < count - 1; i += 1)
    {
        segments = append(segments, [points[i], points[i + 1]]);
    }
    if (closed && count > 2)
    {
        segments = append(segments, [points[count - 1], points[0]]);
    }
    return segments;
}

/**
 * The lines (each `[from, to]`) from a hub to each of its spokes.
 */
export function hubSegments(hub is Vector, spokes is array) returns array
{
    return mapArray(spokes, spoke => [hub, spoke]);
}

/**
 * Segments without repeats (either way round) or zero length ones.
 */
export function uniqueSegments(segments is array) returns array
{
    var unique = [];
    for (var segment in segments)
    {
        if (tolerantEquals(segment[0], segment[1]))
        {
            continue;
        }
        if (!any(unique, other => (tolerantEquals(other[0], segment[0]) && tolerantEquals(other[1], segment[1])) ||
                        (tolerantEquals(other[0], segment[1]) && tolerantEquals(other[1], segment[0]))))
        {
            unique = append(unique, segment);
        }
    }
    return unique;
}

/**
 * Face to use's plane, or without one, the plane of the first full circle chosen.
 */
function getSketchPlane(context is Context, definition is map) returns Plane
{
    if (!isQueryEmpty(context, definition.face))
    {
        return evPlane(context, { "face" : definition.face });
    }
    const selections = concatenateArrays([mapArray(definition.chains, chain => chain.chainPoints),
                mapArray(definition.hubs, hub => hub.hub), mapArray(definition.hubs, hub => hub.spokes)]);
    const circles = qUnion(selections)->qGeometry(GeometryType.CIRCLE);
    if (isQueryEmpty(context, circles))
    {
        throw regenError("Select a face to use: none of the ribs' circles give a plane.", ["face"]);
    }
    const coordSystem = evCurveDefinition(context, { "edge" : qNthElement(circles, 0) }).coordSystem;
    return plane(coordSystem.origin, coordSystem.zAxis);
}

/**
 * A manipulator on each point on an edge, to drag it along the edge (as std's fillet does its points on edges).
 */
function addEdgePointManipulators(context is Context, id is Id, definition is map)
{
    for (var i, edgePoint in definition.edgePoints)
    {
        try silent
        {
            const lines = evEdgeTangentLines(context, { "edge" : edgePoint.edge, "parameters" : [0, edgePoint.edgeLocation, 1],
                        "arcLengthParameterization" : true });
            addManipulators(context, id, { (EDGE_POINT_MANIPULATOR ~ "." ~ i) : linearManipulator({
                                "base" : lines[1].origin,
                                "direction" : lines[1].direction,
                                "offset" : 0 * meter,
                                "minValue" : -norm(lines[0].origin - lines[1].origin),
                                "maxValue" : norm(lines[2].origin - lines[1].origin),
                                "style" : ManipulatorStyleEnum.TANGENTIAL
                            }) });
        }
    }
}

/**
 * Dragging a point along its edge sets its location: the edge's nearest point to where it's dragged.
 */
export function quickRibManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        const index = edgePointManipulatorIndex(key);
        if (index == undefined || index >= size(definition.edgePoints))
        {
            continue;
        }
        const position = manipulator.base + manipulator.direction * manipulator.offset;
        const nearest = evDistance(context, { "side0" : position, "side1" : definition.edgePoints[index].edge, "arcLengthParameterization" : true });
        definition.edgePoints[index].edgeLocation = clamp(nearest.sides[1].parameter, 0, 1);
    }
    return definition;
}

/**
 * The Points on edges item a manipulator's key is for (`edgePoint.<index>`), or undefined.
 */
export function edgePointManipulatorIndex(key is string)
{
    const found = match(key, EDGE_POINT_MANIPULATOR ~ "\\.(\\d+)");
    return found.hasMatch ? stringToNumber(found.captures[1]) : undefined;
}

/**
 * Adds a Points on edges item for each edge (but full circles, whose centers are used) the chains and hubs use which
 * hasn't one, halfway along it; and when Clear invalid selections is clicked, drops what's invalid (see its
 * description).
 */
export function quickRibEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    if (clickedButton == "clearInvalid")
    {
        definition = clearInvalidSelections(context, definition);
    }
    for (var edge in evaluateQuery(context, usedEdges(definition)))
    {
        if (edgePointIndex(context, definition, edge) == undefined)
        {
            definition.edgePoints = append(definition.edgePoints, { "edge" : edge, "edgeLocation" : 0.5 });
        }
    }
    return definition;
}

/**
 * The edges (but full circles) the chains and hubs choose.
 */
function usedEdges(definition is map) returns Query
{
    const selections = concatenateArrays([mapArray(definition.chains, chain => chain.chainPoints),
                mapArray(definition.hubs, hub => hub.hub), mapArray(definition.hubs, hub => hub.spokes)]);
    return qSubtraction(qUnion(selections)->qEntityFilter(EntityType.EDGE), qUnion(selections)->qGeometry(GeometryType.CIRCLE));
}

/**
 * Clear invalid selections: what's chosen, but what isn't there any more; chains of fewer than two points; hubs
 * without a hub; and points on edges nothing uses.
 */
function clearInvalidSelections(context is Context, definition is map) returns map
{
    // Evaluated, to keep only what's there
    var chains = [];
    for (var chain in definition.chains)
    {
        chain.chainPoints = qUnion(evaluateQuery(context, chain.chainPoints));
        if (evaluateQueryCount(context, chain.chainPoints) >= 2)
        {
            chains = append(chains, chain);
        }
    }
    var hubs = [];
    for (var hub in definition.hubs)
    {
        hub.hub = qUnion(evaluateQuery(context, hub.hub));
        hub.spokes = qUnion(evaluateQuery(context, hub.spokes));
        if (!isQueryEmpty(context, hub.hub))
        {
            hubs = append(hubs, hub);
        }
    }
    definition.chains = chains;
    definition.hubs = hubs;
    const used = usedEdges(definition);
    definition.edgePoints = filter(definition.edgePoints, edgePoint => !isQueryEmpty(context, qIntersection([edgePoint.edge, used])));
    return definition;
}
