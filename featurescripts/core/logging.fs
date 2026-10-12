FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Logging for features, for finding out why one fails on a part: on while the Part Studio has a variable `#logging`
 * of `true` (a Variable feature of type Any, before the feature). Logging prints lines to the FeatureScript console
 * (`println`), and shows entities as debug entities, which stay even when the feature fails. Off, it does nothing, so
 * features can log as they go.
 */
export function isLogging(context is Context) returns boolean
{
    const logging = try silent(getVariable(context, "logging"));
    return logging is boolean && logging;
}

/**
 * Prints `message`, if logging.
 */
export function logLine(context is Context, message is string)
{
    if (isLogging(context))
    {
        println(message);
    }
}

/**
 * Prints `label` and how many of `entities` there are, and shows them in `color`, if logging.
 */
export function logEntities(context is Context, label is string, entities is Query, color is DebugColor)
{
    if (isLogging(context))
    {
        println(label ~ ": " ~ size(evaluateQuery(context, entities)));
        addDebugEntities(context, entities, color);
    }
}

/**
 * Prints `label`, then each of `edges` (on `face`), and shows them in `color`, if logging. Each edge is its type and
 * size, and its ends in the face's plane, in inches: a line's length, or an arc's radius, angle, and center, and
 * whether it's convex (bulging out of the face, as a part's rounded corner) or concave (as a hole).
 */
export function logEdges(context is Context, label is string, edges is Query, face is Query, color is DebugColor)
{
    if (!isLogging(context))
    {
        return;
    }
    const evaluated = evaluateQuery(context, edges);
    println(label ~ ": " ~ size(evaluated) ~ " edges");
    const plane = evPlane(context, { "face" : face });
    for (var i, edge in evaluated)
    {
        println("  " ~ i ~ ": " ~ edgeDescription(context, edge, face, plane));
    }
    addDebugEntities(context, edges, color);
}

/**
 * An edge of `face` (in `plane`), described for `logEdges`.
 */
function edgeDescription(context is Context, edge is Query, face is Query, plane is Plane) returns string
{
    const curve = evCurveDefinition(context, { "edge" : edge });
    const length = evLength(context, { "entities" : edge });
    // Walking along it, the face is to the left
    const ends = evEdgeTangentLines(context, { "edge" : edge, "parameters" : [0, 1], "face" : face });
    const span = " from " ~ pointText(worldToPlane(plane, ends[0].origin)) ~ " to " ~ pointText(worldToPlane(plane, ends[1].origin));
    if (curve is Line)
    {
        return "line " ~ lengthText(length) ~ span;
    }
    if (curve is Circle)
    {
        const left = cross(plane.normal, ends[0].direction);
        const convex = dot(curve.coordSystem.origin - ends[0].origin, left) > 0 * meter;
        return (convex ? "convex" : "concave") ~ " arc, radius " ~ lengthText(curve.radius) ~ ", " ~
            roundToPrecision(length / curve.radius * 180 / PI, 2) ~ " deg, center " ~ pointText(worldToPlane(plane, curve.coordSystem.origin)) ~
            span;
    }
    return "curve " ~ lengthText(length) ~ span;
}

/**
 * `length` in inches, to 4 places, for logging.
 */
export function lengthText(length is ValueWithUnits) returns string
{
    return roundToPrecision(length / inch, 4) ~ " in";
}

/**
 * A 2D point in inches, to 4 places, for logging.
 */
export function pointText(point is Vector) returns string
{
    return "(" ~ roundToPrecision(point[0] / inch, 4) ~ ", " ~ roundToPrecision(point[1] / inch, 4) ~ ")";
}
