FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/persistentCoordSystem.fs", version : "2960.0");

export import(path : "452d43a015d17145ad7775e4", version : "9ecfc0af8f11e2db5c0d0667");

/**
 * Belts and chains: loops of a pitch (a belt's teeth, or a chain's links) along a path around pulleys, sprockets, and
 * idlers, each a `BoundaryCircle` of the path's radius around it (see `core/boundary.fs`), with lines tangent between
 * them. Robot belt and Robot chain set `LOOP_ATTRIBUTE` on what they make, which Robot tensioner reads.
 */

/**
 * Set on a belt or chain, as a `LoopAttribute`.
 */
export const LOOP_ATTRIBUTE = "robotLoop";

/**
 * A belt's or chain's path, and the length it should be.
 *
 * @type {{
 *      @field name {string} : What it is, for messages, like `"100T 5mm HTD belt"`.
 *      @field coordSystem {PersistentCoordSystem} : Its plane, which its circles' locations are in.
 *      @field length {ValueWithUnits} : The length its path should be: its pitch times its teeth or links (plus any
 *              fit adjustment).
 *      @field circles {array} : Its `BoundaryCircle`s, in order. An idler's has its `idlerRadius` too, which Robot
 *              tensioner can resize, and which is less than its path's radius by the belt or chain over it.
 *      @field counterClockwise {boolean} : Whether its circles go counter clockwise around its plane's normal.
 * }}
 */
export type LoopAttribute typecheck canBeLoopAttribute;

export predicate canBeLoopAttribute(value)
{
    value is map;
    value.name is string;
    value.coordSystem is PersistentCoordSystem;
    isLength(value.length);
    value.circles is array;
    for (var circle in value.circles)
    {
        circle is BoundaryCircle;
        circle.idlerRadius == undefined || isLength(circle.idlerRadius);
    }
    value.counterClockwise is boolean;
}

/**
 * A `LoopAttribute`, for a loop on `plane`.
 */
export function loopAttribute(name is string, plane is Plane, length is ValueWithUnits, circles is array, counterClockwise is boolean) returns LoopAttribute
{
    // Not forced right handed: a mirrored loop's circles would be mirrored too, so its coordSystem is better undefined
    return {
                "name" : name,
                "coordSystem" : persistentCoordSystem(coordSystem(plane), "loop", false),
                "length" : length,
                "circles" : circles,
                "counterClockwise" : counterClockwise
            } as LoopAttribute;
}

/**
 * The endpoints of the line into each circle from the one before it.
 */
export function loopConnectingPoints(circles is array, counterClockwise is boolean) returns array
{
    return mapArrayIndices(circles, function(i)
        {
            return circleToCircle(getPrevious(circles, i), circles[i], counterClockwise);
        });
}

/**
 * The length of the path around `circles`.
 */
export function loopLength(circles is array, counterClockwise is boolean) returns ValueWithUnits
{
    const connectingPoints = loopConnectingPoints(circles, counterClockwise);
    var length = 0 * meter;
    for (var i, points in connectingPoints)
    {
        length += norm(points[1] - points[0]);
        length += arcLength(points[1], getNext(connectingPoints, i)[0], circles[i], counterClockwise);
    }
    return length;
}

/**
 * Sketches the path around `circles` on `plane`, each line and arc in a sketch of its own (disambiguated by its
 * circles' identities).
 *
 * @returns {{
 *      @field path {Query} : The path's edges.
 *      @field arcs {array} : A query for the arc around each circle, in order.
 * }}
 */
export function sketchLoop(context is Context, id is Id, plane is Plane, circles is array, counterClockwise is boolean) returns map
{
    const connectingPoints = loopConnectingPoints(circles, counterClockwise);
    const arcs = sketchConnectingArcs(context, id + "arcs", circles, plane, connectingPoints, counterClockwise);
    sketchConnectingLines(context, id + "lines", circles, plane, connectingPoints);
    return {
            "path" : qCreatedBy(id, EntityType.EDGE)->qSketchFilter(SketchObject.YES),
            "arcs" : arcs
        };
}

/**
 * Whether `circles` go counter clockwise around their centroid.
 */
export function loopCounterClockwise(circles is array) returns boolean
{
    return isCounterClockwise(mapArray(circles, function(circle)
                {
                    return circle.location;
                }));
}

/**
 * The `x` nearest 0 at which `f(x)` is 0, or `undefined` if there's none: `f` is walked away from 0 each way, by
 * `step` and then twice as far each time, until its sign changes (or it's undefined, where it stops looking that way),
 * and the change is bisected to within `precision`.
 *
 * @param f {function} : A length (or `undefined`, where it has none), of a length `x`, like how much longer than it
 *          should be a belt is with a pulley moved `x`.
 */
export function nearestRoot(f is function, step is ValueWithUnits, precision is ValueWithUnits)
{
    const start = f(0 * meter);
    if (start == undefined)
    {
        return undefined;
    }
    if (abs(start) <= precision)
    {
        return 0 * meter;
    }
    const startPositive = start > 0 * meter;
    var nearest = undefined;
    for (var direction in [1, -1])
    {
        // The sign is still `start`'s at `inside`, and isn't at `outside`
        var inside = 0 * meter;
        var outside = undefined;
        var distance = step;
        for (var i = 0; i < 40; i += 1)
        {
            const x = direction * distance;
            const value = f(x);
            if (value == undefined)
            {
                break;
            }
            if ((value > 0 * meter) != startPositive)
            {
                outside = x;
                break;
            }
            inside = x;
            distance *= 2;
        }
        if (outside == undefined)
        {
            continue;
        }
        for (var i = 0; i < 100 && abs(outside - inside) > precision; i += 1)
        {
            const middle = (inside + outside) / 2;
            const value = f(middle);
            if (value == undefined)
            {
                break;
            }
            if ((value > 0 * meter) == startPositive)
            {
                inside = middle;
            }
            else
            {
                outside = middle;
            }
        }
        const root = (inside + outside) / 2;
        if (nearest == undefined || abs(root) < abs(nearest))
        {
            nearest = root;
        }
    }
    return nearest;
}

/**
 * `loopLength(circles, counterClockwise)`, or `undefined` if there's no path around them (like when one's inside
 * another).
 */
export function tryLoopLength(circles is array, counterClockwise is boolean)
{
    for (var circle in circles)
    {
        if (circle.radius <= 0 * meter)
        {
            return undefined;
        }
    }
    try silent
    {
        return loopLength(circles, counterClockwise);
    }
    return undefined;
}

/**
 * What a pulley's or sprocket's selected location is, for editing logic to choose how to read it: "part" (a part or
 * mate connector with the attribute `attributeName`, like a Robot pulley's or sprocket's), "pitchCircle" (a circle
 * whose size is a pitch circle's with a whole number of teeth, as `teethFor(radius)` says), or "center" (anything
 * else: a point, or a circle like a bore's). `undefined` if nothing's selected.
 */
export function locationKind(context is Context, selection is Query, attributeName is string, teethFor is function)
{
    if (isQueryEmpty(context, selection))
    {
        return undefined;
    }
    if (getAttribute(context, { "entity" : selection, "name" : attributeName }) != undefined)
    {
        return "part";
    }
    const circle = selection->qEntityFilter(EntityType.EDGE)->qGeometry(GeometryType.CIRCLE);
    if (!isQueryEmpty(context, circle))
    {
        const teeth = teethFor(evCurveDefinition(context, { "edge" : circle }).radius);
        if (teeth != undefined && teeth >= 3 && abs(teeth - round(teeth)) < 0.01)
        {
            return "pitchCircle";
        }
    }
    return "center";
}

/**
 * Whether editing logic should look at an array item's selection again: it's new, or selects something else than it
 * did (`oldItems` are the array's items before).
 */
export function selectionChanged(context is Context, oldItems, index is number, parameter is string, selection is Query) returns boolean
{
    if (oldItems == undefined || index >= size(oldItems))
    {
        return true;
    }
    const old = oldItems[index][parameter];
    return old == undefined || !areQueriesEquivalent(context, old, selection);
}

/**
 * An open path (a belt with ends): from `start` (a 2D point), around `circles` in order, to `end`, with lines tangent
 * between them. It goes counter clockwise around each circle, or clockwise around a flipped one.
 *
 * @returns {{
 *      @field lines {array} : Each line's `[start, end]`: from `start` to the first circle, between circles, and from the
 *              last circle to `end`.
 *      @field arcs {array} : Around each circle: a map of its `center`, `radius`, `counterClockwise`, `startAngle`, and
 *              `sweep` (from the line into it to the line out).
 *      @field length {ValueWithUnits} :
 * }}
 */
export function openPath(start is Vector, circles is array, end is Vector) returns map
{
    // A point's a circle with no radius; a radius's sign says which way the path goes around it, as `circleToCircle`'s
    var centers = [start];
    var signedRadii = [0 * meter];
    for (var circle in circles)
    {
        centers = append(centers, circle.location);
        signedRadii = append(signedRadii, circle.flipped ? circle.radius : -circle.radius);
    }
    centers = append(centers, end);
    signedRadii = append(signedRadii, 0 * meter);

    var lines = [];
    var length = 0 * meter;
    for (var i = 0; i < size(centers) - 1; i += 1)
    {
        const line = tangentLine(centers[i], signedRadii[i], centers[i + 1], signedRadii[i + 1]);
        lines = append(lines, line);
        length += norm(line[1] - line[0]);
    }
    var arcs = [];
    for (var i, circle in circles)
    {
        const arrive = lines[i][1] - circle.location;
        const leave = lines[i + 1][0] - circle.location;
        const startAngle = atan2(arrive[1], arrive[0]);
        const endAngle = atan2(leave[1], leave[0]);
        var sweep = (circle.flipped ? startAngle - endAngle : endAngle - startAngle) % (2 * PI * radian);
        if (sweep < 0 * radian)
        {
            sweep += 2 * PI * radian;
        }
        arcs = append(arcs, {
                        "center" : circle.location,
                        "radius" : circle.radius,
                        "counterClockwise" : !circle.flipped,
                        "startAngle" : startAngle,
                        "sweep" : sweep
                    });
        length += circle.radius * sweep / radian;
    }
    return { "lines" : lines, "arcs" : arcs, "length" : length };
}

/**
 * The line tangent from one circle to another (or a point, with no radius), leaving and reaching them going around
 * them as their radii's signs say (see `openPath`).
 */
function tangentLine(center1 is Vector, radius1 is ValueWithUnits, center2 is Vector, radius2 is ValueWithUnits) returns array
{
    const distance = norm(center2 - center1);
    if (tolerantEqualsZero(distance))
    {
        throw regenError("Two of the belt's points or pulleys are in the same place.");
    }
    const direction = (center2 - center1) / distance;
    const across = vector(-direction[1], direction[0]);
    const alpha = (radius1 - radius2) / distance;
    if (abs(alpha) > 1)
    {
        throw regenError("The belt can't go around its pulleys: one's inside another, or its start or end is inside one.");
    }
    const toTangent = alpha * direction + sqrt(1 - alpha ^ 2) * across;
    return [center1 + toTangent * radius1, center2 + toTangent * radius2];
}

/**
 * Sketches the region of an open path (see `openPath`) thickened `left` and `right` of it (going from its start to its
 * end), with square ends, on `plane`, and returns it: `faces`, and `arcs`, the edges around each circle (a query
 * each).
 */
export function sketchOpenPathProfile(context is Context, id is Id, plane is Plane, path is map, left is ValueWithUnits, right is ValueWithUnits) returns map
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const lineOffset = function(line is array, distance is ValueWithUnits) returns array
        {
            const direction = normalize(line[1] - line[0]);
            const normal = vector(-direction[1], direction[0]) * distance;
            return [line[0] + normal, line[1] + normal];
        };
    for (var side in [["left", left], ["right", right]])
    {
        // Left of the path is toward a counter clockwise circle's center
        const distance = side[0] == "left" ? side[1] : -side[1];
        for (var i, line in path.lines)
        {
            const offset = lineOffset(line, distance);
            if (!tolerantEquals(offset[0], offset[1]))
            {
                skLineSegment(sketch, side[0] ~ "Line" ~ i, { "start" : offset[0], "end" : offset[1] });
            }
        }
        for (var i, arc in path.arcs)
        {
            const radius = arc.radius - (arc.counterClockwise ? distance : -distance);
            if (radius <= 0 * meter)
            {
                throw regenError("A pulley is too small for the belt to wrap around.");
            }
            if (tolerantEqualsZero(arc.sweep / radian))
            {
                continue;
            }
            const direction = arc.counterClockwise ? 1 : -1;
            const at = function(angle is ValueWithUnits) returns Vector
                {
                    return arc.center + vector(cos(angle), sin(angle)) * radius;
                };
            skArc(sketch, side[0] ~ "Arc" ~ i, {
                        "start" : at(arc.startAngle),
                        "mid" : at(arc.startAngle + direction * arc.sweep / 2),
                        "end" : at(arc.startAngle + direction * arc.sweep)
                    });
        }
    }
    // Square ends
    const first = path.lines[0];
    const last = path.lines[size(path.lines) - 1];
    skLineSegment(sketch, "startCap", { "start" : lineOffset(first, left)[0], "end" : lineOffset(first, -right)[0] });
    skLineSegment(sketch, "endCap", { "start" : lineOffset(last, left)[1], "end" : lineOffset(last, -right)[1] });
    skSolve(sketch);
    return {
            "faces" : qCreatedBy(id, EntityType.FACE),
            "arcs" : mapArrayIndices(path.arcs, function(i)
                {
                    return qUnion([sketchEntityQuery(id, EntityType.EDGE, "leftArc" ~ i), sketchEntityQuery(id, EntityType.EDGE, "rightArc" ~ i)]);
                })
        };
}
