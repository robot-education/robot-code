FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");
import(path : "472bc4c291e1d2d6f9b98937", version : "515ac497eaa4f72b8c5f1a57");

export const POINT_MANIPULATOR = "pointManipulator";
export const FLIP_MANIPULATOR = "flipManipulator";
export const PLATE_EXTRUDE_MANIPULATOR = "plateExtrudeManipulator";

/**
 * Returns the endpoints of a line tangental to two circles.
 * @param circle1 {map} : An outer geometry map representing a circle.
 * @param circle2 {map} : An outer geometry map representing a circle.
 * @param chirality {boolean} : The chirality of the plate.
 */
export function circleToCircle(circle1 is map, circle2 is map, chirality is boolean) returns array
precondition
{
    circle1.location is Vector;
    circle1.radius is ValueWithUnits;

    circle2.location is Vector;
    circle2.radius is ValueWithUnits;
}
{
    const radius1 = circle1.radius * (chirality ? -1 : 1);
    const radius2 = circle2.radius * (chirality ? -1 : 1);
    const delta = normalize(circle2.location - circle1.location);
    const cross = vector(-delta[1], delta[0]);
    const alpha = (radius1 - radius2) / norm(circle2.location - circle1.location);
    const beta = sqrt(1 - alpha * alpha);

    const point1 = circle1.location + (alpha * delta + beta * cross) * radius1;
    const point2 = circle2.location + (alpha * delta + beta * cross) * radius2;
    return [point1, point2];
}

export function circleToCircle(circle1 is map, circle2 is map) returns array
{
    return circleToCircle(circle1, circle2, false);
}

/**
 * Returns the endpoints of a line through a point tangental to a circle.
 * @param point {Vector} : A 2D point representing the point to connect to the circle.
 * @param circle {map} : An outer geometry map of a circle.
 * @param chirality {boolean} : The chirality of the plate.
 */
// Function borrowed from here:
// https://stackoverflow.com/questions/49968720/find-tangent-points-in-a-circle-from-a-point/49981991
export function pointToCircle(point is Vector, circle is map, chirality is boolean) returns array
precondition
{
    circle.location is Vector;
    circle.radius is ValueWithUnits;
}
{
    const centerToCenter = norm(point - circle.location);
    const angle = acos(circle.radius / centerToCenter);
    const angleOffset = atan2(point[1] - circle.location[1], point[0] - circle.location[0]);

    if (chirality)
    {
        const angle1 = angleOffset + angle;
        return [circle.location + vector(circle.radius * cos(angle1), circle.radius * sin(angle1)), point];
    }
    else
    {
        const angle2 = angleOffset - angle;
        return [point, circle.location + vector(circle.radius * cos(angle2), circle.radius * sin(angle2))];
    }
}

export function pointToCircle(circle is map, point is Vector, chirality is boolean) returns array
{
    return pointToCircle(point, circle, !chirality);
}

export function pointToCircle(point is Vector, circle is map)
{
    return pointToCircle(point, circle, false);
}

export function pointToCircle(circle is map, point is Vector)
{
    return pointToCircle(point, circle, true);
}

/**
 * Adapted from code from the Onshape Belt FS.
 * @param fourPoints {array} :
 *          An array of four points to connect.
 *          [start of "in" line, end of "in" line, start of "out" line, end of "out" line]
 * @param center {Vector} : The 2D center of the arc.
 * @param radius {ValueWithUnits} : The radius of the arc.
 *
 * @returns {Vector} : The midpoint of the created arc, or `undefined` if it does not exist.
 */
export function addArc(sketch is Sketch, sketchId is string, fourPoints is array, center is Vector, radius is ValueWithUnits, chirality is boolean) returns Vector
{
    if (tolerantEquals(fourPoints[1], fourPoints[2]))
    {
        return fourPoints[1];
    }

    var midVec = ((fourPoints[1] + fourPoints[2]) * 0.5) - center;

    // if the average of the endpoints minus the center is equal to zero (i.e. arc is perfect 180)
    if (tolerantEquals(norm(midVec), 0 * meter))
    {
        const crossVec = chirality ? fourPoints[1] - fourPoints[2] : fourPoints[2] - fourPoints[1];
        // const crossVec = fourPoints[2] - fourPoints[1];
        midVec = vector(-crossVec[1], crossVec[0]);
    }
    else
    {
        // If the in and out line lead to a 'bend' of greater than 180 degrees then we want to flip the midVec
        const inVec = normalize(fourPoints[1] - fourPoints[0]);
        const outVec = normalize(fourPoints[3] - fourPoints[2]);
        const crossProduct = inVec[0] * outVec[1] - inVec[1] * outVec[0];

        if (crossProduct < 0 == chirality)
        {
            midVec *= -1;
        }
    }
    skArc(sketch, sketchId, {
                "start" : fourPoints[1],
                "mid" : normalize(midVec) * radius + center,
                "end" : fourPoints[2]
            });
    return normalize(midVec) * radius + center;
}

export function addArc(sketch is Sketch, sketchId is string, fourPoints is array, center is Vector, radius is ValueWithUnits) returns Vector
{
    return addArc(sketch, sketchId, fourPoints, center, radius, false);
}

export function computeCentroid(locations is array) returns Vector
precondition
{
    is2dPointVector(locations);
}
{
    return accumulate(locations, zeroVector(2) * meter, function(centroid is Vector, location is Vector)
            {
                return centroid + location;
            }) / size(locations);
}

export function isCounterClockwise(locations is array, centroid is Vector) returns boolean
{
    var angleSum = 0 * radian;
    for (var i, curr in locations)
    {
        const next = getNext(locations, i);
        const vector1 = curr - centroid;
        const vector2 = next - centroid;
        var angle = atan2(vector2[1], vector2[0]) - atan2(vector1[1], vector1[0]);
        // clamp angle to 0, 2 PI
        if (angle > PI * radian)
            angle -= 2 * PI * radian;
        else if (angle <= -PI * radian)
            angle += 2 * PI * radian;
        angleSum += angle;
    }
    return (angleSum > -TOLERANCE.zeroAngle * radian);
}

export function sketchConnectingLines(context is Context, id is Id, identities is array, platePlane is Plane, connectingPointsArray is array)
{
    for (var i, curr in identities)
    {
        const next = getNext(identities, i);
        if (curr != undefined && next != undefined)
        {
            setExternalDisambiguation(context, id + unstableIdComponent(i), qUnion(curr, next));
        }
        const autoSketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : platePlane });
        const connectingPoints = connectingPointsArray[i];
        skLineSegment(autoSketch, "autoLine", {
                    "start" : connectingPoints[0],
                    "end" : connectingPoints[1]
                });
        skSolve(autoSketch);
    }
}

/**
 * Shuffles an array backwards one.
 */
export function shuffleBackward(inputArray is array) returns array
{
    // save last value
    const tmp = inputArray[size(inputArray) - 1];
    for (var i = size(inputArray) - 1; i > 0; i -= 1)
    {
        inputArray[i] = inputArray[i - 1];
    }
    inputArray[0] = tmp;
    return inputArray;
}

export function extractFaces(context is Context, id is Id, sketchEntities is Query, platePlane is Plane) returns Query
{
    // sketchFilter removes fitSpline in case where ids align
    sketchEntities = sketchEntities->qSketchFilter(SketchObject.YES)->qBodyType(BodyType.WIRE);
    const maxLength = qBoundingBoxLength(context, sketchEntities) * 3;
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
