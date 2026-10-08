FeatureScript 2960;
/**
 * A collection of utilities which can be used to compute and draw boundaries consisting of points and circles.
 *
 * Boundaries can be made in a clockwise or counter-clockwise fasion.
 * Arcs along the boundary may or may not be flipped.
 * This disambiguates the case of, e.g., a large circle whoose center is the midpoint between two other points.
 */
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
export import(path : "e14d0b81a4d6b12b9dda1cb5", version : "29cf048977f101dc3c7ec59f");

export type BoundaryCircle typecheck canBeBoundaryCircle;

export predicate canBeBoundaryCircle(value)
{
    value is map;
    value.identity is Query || value.identity is undefined;
    is2dPoint(value.location);
    value.radius is ValueWithUnits;
    value.radius > 0 * meter;
    value.flipped is boolean;
}


/**
 * Returns the `radius` of circle, inverted if `circle.flipped` is `true`.
 */
function flippedRadius(circle is BoundaryCircle) returns ValueWithUnits
{
    return circle.radius * (circle.flipped ? -1 : 1);
}

/**
 * Returns the endpoints of a line tangent between two circles.
 * @param circle1 {map} : A map representing a circle.
 * @param circle2 {map} : A map representing a circle.
 */
export function circleToCircle(circle1 is BoundaryCircle, circle2 is BoundaryCircle, counterClockwise is boolean) returns array
{
    const radius1 = flippedRadius(circle1) * (counterClockwise ? -1 : 1);
    const radius2 = flippedRadius(circle2) * (counterClockwise ? -1 : 1);
    const delta = normalize(circle2.location - circle1.location);
    const cross = vector(-delta[1], delta[0]);
    const alpha = (radius1 - radius2) / norm(circle2.location - circle1.location);
    const beta = sqrt(1 - alpha * alpha);

    const point1 = circle1.location + (alpha * delta + beta * cross) * radius1;
    const point2 = circle2.location + (alpha * delta + beta * cross) * radius2;
    return [point1, point2];
}

/**
 * Returns the endpoints of a line through a point tangental to a circle.
 * @param point {Vector} : A 2D point representing the point to connect to the circle.
 * @param circle {map} : An outer geometry map of a circle.
 * @param counterClockwise {boolean} : Whether the selections are counterClockwise.
 */
// Code borrowed from here:
// https://stackoverflow.com/questions/49968720/find-tangent-points-in-a-circle-from-a-point/49981991
export function pointToCircle(point is Vector, circle is map, counterClockwise is boolean) returns array
{
    const centerToCenter = norm(point - circle.location);
    const radius = flippedRadius(circle);
    const angle = acos(radius / centerToCenter);
    const angleOffset = atan2(point[1] - circle.location[1], point[0] - circle.location[0]);

    if (counterClockwise)
    {
        const angle1 = angleOffset + angle;
        return [circle.location + vector(radius * cos(angle1), radius * sin(angle1)), point];

    }
    else
    {
        const angle2 = angleOffset - angle;
        return [point, circle.location + vector(radius * cos(angle2), radius * sin(angle2))];
    }
}

export function pointToCircle(circle is map, point is Vector, counterClockwise is boolean) returns array
{
    return pointToCircle(point, circle, !counterClockwise);
}

/**
 * Returns `true` if the in and out lines result in a bend greater than 180 degrees.
 */
export function isConcave(fourPoints is array, counterClockwise is boolean) returns boolean
{
    const inVec = normalize(fourPoints[1] - fourPoints[0]);
    const outVec = normalize(fourPoints[3] - fourPoints[2]);
    const crossProduct = inVec[0] * outVec[1] - inVec[1] * outVec[0];
    return (crossProduct < 0) == counterClockwise;
}

/**
 * Returns the length of an arc.
 */
export function arcLength(startPoint is Vector, endPoint is Vector, circle is BoundaryCircle, counterClockwise is boolean) returns ValueWithUnits
{
    const startVector = startPoint - circle.location;
    const endVector = endPoint - circle.location;
    // (-pi, pi]
    var angle = counterClockwiseAngle(startVector, endVector);

    // Normalize angle to [0, 2pi)
    if (angle < 0 * PI * radian)
    {
        // -pi -> pi
        angle += 2 * PI * radian;
    }

    if (!counterClockwise)
    {
        // 0.25pi -> 1.75pi
        angle = flipAngle(angle);
    }

    if (circle.flipped)
    {
        angle = flipAngle(angle);
    }

    return angle * norm(startVector) / radian;
}

/**
 * Adapted from code from the Onshape Belt FS.
 * @param fourPoints {array} :
 *          An array of four points to connect.
 *          [start of "in" line, end of "in" line, start of "out" line, end of "out" line]
 * @param circle {BoundaryCircle} : A boundary circle to use.
 *
 * @returns {Vector} : The midpoint of the created arc, or `undefined` if it does not exist.
 */
export function addArc(sketch is Sketch, sketchId is string, fourPoints is array, circle is BoundaryCircle, counterClockwise is boolean) returns Vector
{
    // No arc needed; skip
    if (tolerantEquals(fourPoints[1], fourPoints[2]))
    {
        return fourPoints[1];
    }
    const midDirection = getMidDirection(fourPoints, circle.location, counterClockwise);
    const radius = flippedRadius(circle);
    skArc(sketch, sketchId, {
                "start" : fourPoints[1],
                "mid" : midDirection * radius + circle.location,
                "end" : fourPoints[2]
            });
    return midDirection * radius + circle.location;
}

/**
 * Computes a vector pointing from the center to the midpoint of the arc.
 */
function getMidDirection(fourPoints is array, center is Vector, counterClockwise is boolean) returns Vector
{
    var midVec = ((fourPoints[1] + fourPoints[2]) * 0.5) - center;

    // if the arc is a perfect 180 (i.e. the two points cancel out, so we end up back in the center)
    if (tolerantEqualsZero(norm(midVec)))
    {
        const crossVec = counterClockwise ? (fourPoints[1] - fourPoints[2]) : (fourPoints[2] - fourPoints[1]);
        midVec = vector(-crossVec[1], crossVec[0]);
    }
    else if (isConcave(fourPoints, counterClockwise))
    {
        midVec *= -1;
    }
    return normalize(midVec);
}

export function sketchConnectingLines(context is Context, id is Id, circles is array, plane is Plane, connectingPointsArray is array)
{
    for (var i, curr in circles)
    {
        const next = getNext(circles, i);
        if (curr.identity != undefined && next.identity != undefined)
        {
            setExternalDisambiguation(context, id + unstableIdComponent(i), qUnion(curr.identity, next.identity));
        }
        const autoSketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : plane });
        const connectingPoints = connectingPointsArray[i];
        skLineSegment(autoSketch, "autoLine", {
                    "start" : connectingPoints[0],
                    "end" : connectingPoints[1]
                });
        skSolve(autoSketch);
    }
}

/**
 * @return {array} : An array of queries, one for each arc.
 */
export function sketchConnectingArcs(context is Context, id is Id, circles is array, plane is Plane, connectingPointsArray is array, counterClockwise is boolean) returns array
{
    var arcQueries = [];
    for (var i, curr in circles)
    {
        const nextIndex = getNext(size(circles), i);
        const next = circles[nextIndex];

        if (curr.identity != undefined)
        {
            setExternalDisambiguation(context, id + unstableIdComponent(i), curr.identity);
        }
        const autoSketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : plane });

        const fourPoints = concatenateArrays([connectingPointsArray[i], connectingPointsArray[nextIndex]]);
        addArc(autoSketch, "arc", fourPoints, curr, counterClockwise);
        skSolve(autoSketch);
        arcQueries = append(arcQueries, sketchEntityQuery(id + unstableIdComponent(i), EntityType.EDGE, "arc"));
    }
    return arcQueries;
}

export function computeCentroid(locations is array) returns Vector
precondition
{
    is2dPointVector(locations);
}
{
    return sum(locations) / size(locations);
}

/**
 * Returns `true` if `locations` are selected in a counter-clockwise fashion relative to their centroid.
 * @param centroid {Vector} : @optional
 */
export function isCounterClockwise(centroid is Vector, locations is array) returns boolean
precondition
{
    is2dPointVector(locations);
}
{
    var angleSum = 0 * radian;
    for (var i, curr in locations)
    {
        const next = getNext(locations, i);
        const vector1 = curr - centroid;
        const vector2 = next - centroid;
        angleSum += counterClockwiseAngle(vector1, vector2);
    }
    // If the angleSum is positive, locations are generally counterClockwise
    return (angleSum > -TOLERANCE.zeroAngle * radian);
}

export function isCounterClockwise(locations is array)
precondition
{
    is2dPointVector(locations);
}
{
    return isCounterClockwise(computeCentroid(locations), locations);
}

/**
 * Computes the counter clockwise angle between two vectors.
 * The result is in the range (-pi, pi].
 */
export function counterClockwiseAngle(vector1 is Vector, vector2 is Vector) returns ValueWithUnits
{
    // angle from start to end
    var angle = atan2(vector2[1], vector2[0]) - atan2(vector1[1], vector1[0]);
    // range of angle is (-2pi, 2pi)
    // normalize to (-pi, pi]
    if (angle > PI * radian)
        angle -= 2 * PI * radian;
    else if (angle <= -PI * radian)
        angle += 2 * PI * radian;
    return angle;
}

/**
 * Given an angle in the range [0, 2PI), returns the flipped angle corresponding to the other side of the circle.
 */
export function flipAngle(angle is ValueWithUnits) returns ValueWithUnits
{
    return 2 * PI * radian - angle;
}

export function extractFaces(context is Context, id is Id, sketchEntities is Query, platePlane is Plane) returns Query
{
    // sketchFilter removes fitSpline in case where ids align
    sketchEntities = sketchEntities->qSketchFilter(SketchObject.YES)->qBodyType(BodyType.WIRE);
    const maxLength = boundingBoxLength(context, sketchEntities) * 3;
    opFitSpline(context, id + "fitSpline", { "points" : [platePlane.origin + platePlane.x * maxLength, platePlane.origin - platePlane.x * maxLength] });

    opExtrude(context, id + "extrude", {
                "entities" : qCreatedBy(id + "fitSpline", EntityType.EDGE),
                "direction" : platePlane->yAxis(),
                "endBound" : BoundingType.BLIND,
                "endDepth" : maxLength,
                "startBound" : BoundingType.BLIND,
                "startDepth" : maxLength
            });
    const extrudedFace = qCreatedBy(id + "extrude", EntityType.FACE);

    opSplitFace(context, id + "splitFace", {
                "faceTargets" : extrudedFace,
                "edgeTools" : sketchEntities,
                "direction" : platePlane.normal
            });
    return extrudedFace->qSubtraction(extrudedFace->qLargest());
}


// /**
//  * Returns `true` if the 2d line segments defined by `startPoints` and `endPoints` intersect each other.
//  *
//  * Adapted from: www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
//  */
// export function lineSegmentsIntersect(startPoints is array, endPoints is array) returns boolean
// precondition
// {
//     is2dPointVector(startPoints);
//     is2dPointVector(endPoints);
// }
// {
//     // Find the four orientations needed for general and
//     // special cases
//     const o1 = orientation(startPoints[0], endPoints[0], startPoints[1]);
//     const o2 = orientation(startPoints[0], endPoints[0], endPoints[1]);
//     const o3 = orientation(startPoints[1], endPoints[1], startPoints[0]);
//     const o4 = orientation(startPoints[1], endPoints[1], endPoints[0]);

//     // General case
//     if (o1 != o2 && o3 != o4)
//         return true;

//     // Special Cases
//     // startPoints[0], endPoints[0] and startPoints[1] are collinear and startPoints[1] lies on segment startPoints[0]endPoints[0]
//     if (o1 == 0 && onSegment(startPoints[0], startPoints[1], endPoints[0]))
//         return true;

//     // startPoints[0], endPoints[0] and endPoints[1] are collinear and endPoints[1] lies on segment startPoints[0]endPoints[0]
//     if (o2 == 0 && onSegment(startPoints[0], endPoints[1], endPoints[0]))
//         return true;

//     // startPoints[1], endPoints[1] and startPoints[0] are collinear and startPoints[0] lies on segment startPoints[1]endPoints[1]
//     if (o3 == 0 && onSegment(startPoints[1], startPoints[0], endPoints[1]))
//         return true;

//     // startPoints[1], endPoints[1] and endPoints[0] are collinear and endPoints[0] lies on segment startPoints[1]endPoints[1]
//     if (o4 == 0 && onSegment(startPoints[1], endPoints[0], endPoints[1]))
//         return true;

//     return false; // Doesn't fall in any of the above cases
// }

// function onSegment(p, q, r)
// {
//     return q[0] <= max(p[0], r[0]) && q[0] >= min(p[0], r[0]) &&
//         q[1] <= max(p[1], r[1]) && q[1] >= min(p[1], r[1]);
// }

// function orientation(p, q, r)
// {
//     const val = (q[1] - p[1]) * (r[0] - q[0]) - (q[0] - p[0]) * (r[1] - q[1]);
//     if (tolerantEqualsZero(val))
//         return 0; // collinear
//     return (val > 0) ? 1 : 2; // clock or counterclock wise
// }
