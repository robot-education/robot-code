FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

/**
 * How a group of ribs connects what's chosen.
 */
export enum RibPattern
{
    annotation { "Name" : "Hub and spoke" }
    HUB_AND_SPOKE,
    annotation { "Name" : "Chain" }
    CHAIN,
    annotation { "Name" : "Point to point" }
    POINT_TO_POINT
}

/**
 * Sketches ribs (lines) between circles' centers, points, and mate connectors, quickly: for Robot lighten's Ribs to
 * use. Each group of Ribs is a hub and its spokes, a chain (in the order chosen, closed or not), or a line from one to
 * another.
 */
annotation { "Feature Type Name" : "Quick rib",
        "Feature Type Description" : "Sketch ribs between circles and points: hub and spoke, chain, or point to point." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const quickRib = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Sketch plane", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1,
                    "Description" : "Where the ribs are sketched. Without one, the first circle's plane." }
        definition.sketchPlane is Query;

        annotation { "Name" : "Ribs", "Item name" : "ribs", "Item label template" : "#pattern" }
        definition.ribs is array;

        for (var rib in definition.ribs)
        {
            annotation { "Name" : "Pattern", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            rib.pattern is RibPattern;

            if (rib.pattern == RibPattern.HUB_AND_SPOKE)
            {
                annotation { "Name" : "Hub", "MaxNumberOfPicks" : 1,
                            "Filter" : (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
                rib.hub is Query;

                annotation { "Name" : "Spokes",
                            "Filter" : (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
                rib.spokes is Query;
            }
            else if (rib.pattern == RibPattern.CHAIN)
            {
                annotation { "Name" : "Chain",
                            "Filter" : (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
                rib.chain is Query;

                annotation { "Name" : "Closed" }
                rib.closed is boolean;
            }
            else
            {
                annotation { "Name" : "From", "MaxNumberOfPicks" : 1,
                            "Filter" : (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
                rib.startPoint is Query;

                annotation { "Name" : "To", "MaxNumberOfPicks" : 1,
                            "Filter" : (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
                rib.endPoint is Query;
            }
        }
    }
    {
        if (definition.ribs == [])
        {
            throw regenError("Add ribs to sketch.", ["ribs"]);
        }
        var segments = [];
        for (var i, rib in definition.ribs)
        {
            segments = concatenateArrays([segments, ribSegments(rib.pattern, ribPoints(context, rib, i))]);
        }
        segments = uniqueSegments(segments);
        if (segments == [])
        {
            throw regenError("The ribs chosen have no length.", ["ribs"]);
        }
        const plane = getSketchPlane(context, definition);
        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
        for (var i, segment in segments)
        {
            skLineSegment(sketch, "rib" ~ i, {
                        "start" : worldToPlane(plane, segment[0]),
                        "end" : worldToPlane(plane, segment[1])
                    });
        }
        skSolve(sketch);
    });

/**
 * A group of ribs' points, by what its pattern uses: `hub` and `spokes`, `chain` (in the order chosen) and `closed`,
 * or `start` and `end`. Throws if any are missing.
 */
function ribPoints(context is Context, rib is map, index is number) returns map
{
    if (rib.pattern == RibPattern.HUB_AND_SPOKE)
    {
        return { "hub" : onePoint(context, rib.hub, index, "hub", "Select a hub."),
                "spokes" : somePoints(context, rib.spokes, index, "spokes", "Select spokes.") };
    }
    if (rib.pattern == RibPattern.CHAIN)
    {
        return { "chain" : somePoints(context, rib.chain, index, "chain", "Select circles or points to chain."), "closed" : rib.closed };
    }
    return { "start" : onePoint(context, rib.startPoint, index, "startPoint", "Select where the rib starts."),
            "end" : onePoint(context, rib.endPoint, index, "endPoint", "Select where the rib ends.") };
}

function onePoint(context is Context, query is Query, index is number, parameter is string, message is string) returns Vector
{
    return somePoints(context, query, index, parameter, message)[0];
}

function somePoints(context is Context, query is Query, index is number, parameter is string, message is string) returns array
{
    const points = mapArray(evaluateQuery(context, query), entity => centerOf(context, entity));
    if (points == [])
    {
        throw regenError(message, [faultyArrayParameterId("ribs", index, parameter)]);
    }
    return points;
}

/**
 * Where a rib meets an entity: a circle's or arc's center, a point, or a mate connector's origin.
 */
function centerOf(context is Context, entity is Query) returns Vector
{
    if (!isQueryEmpty(context, entity->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        return evMateConnector(context, { "mateConnector" : entity }).origin;
    }
    if (!isQueryEmpty(context, entity->qEntityFilter(EntityType.VERTEX)))
    {
        return evVertexPoint(context, { "vertex" : entity });
    }
    return evCurveDefinition(context, { "edge" : entity }).coordSystem.origin;
}

/**
 * The lines (each `[from, to]`) a pattern draws between its points (see `ribPoints`): from the hub to each spoke, along
 * the chain (back to its start if it's closed), or from start to end.
 */
export function ribSegments(pattern is RibPattern, points is map) returns array
{
    if (pattern == RibPattern.HUB_AND_SPOKE)
    {
        return mapArray(points.spokes, spoke => [points.hub, spoke]);
    }
    if (pattern == RibPattern.CHAIN)
    {
        var segments = [];
        const count = size(points.chain);
        for (var i = 0; i < count - 1; i += 1)
        {
            segments = append(segments, [points.chain[i], points.chain[i + 1]]);
        }
        if (points.closed && count > 2)
        {
            segments = append(segments, [points.chain[count - 1], points.chain[0]]);
        }
        return segments;
    }
    return [[points.start, points.end]];
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
 * Sketch plane, or without one, the plane of the first circle or arc chosen.
 */
function getSketchPlane(context is Context, definition is map) returns Plane
{
    if (!isQueryEmpty(context, definition.sketchPlane))
    {
        return evPlane(context, { "face" : definition.sketchPlane });
    }
    for (var rib in definition.ribs)
    {
        for (var query in [rib.hub, rib.spokes, rib.chain, rib.startPoint, rib.endPoint])
        {
            if (query == undefined)
            {
                continue;
            }
            const circles = qUnion([query->qGeometry(GeometryType.CIRCLE), query->qGeometry(GeometryType.ARC)]);
            if (!isQueryEmpty(context, circles))
            {
                const coordSystem = evCurveDefinition(context, { "edge" : qNthElement(circles, 0) }).coordSystem;
                return plane(coordSystem.origin, coordSystem.zAxis);
            }
        }
    }
    throw regenError("Select a sketch plane: none of the ribs' circles give one.", ["sketchPlane"]);
}
