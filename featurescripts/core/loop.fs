FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/persistentCoordSystem.fs", version : "2960.0");

export import(path : "core/boundary.fs", version : "");

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
