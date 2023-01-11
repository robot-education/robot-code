FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

/**
 * @param definition {{
 *      @field start {Vector} :
 *      @field mid {Vector} :
 *      @field end {Vector} :
 * }}
 *
 * @returns {{
 *      @field center {Vector} :
 *      @field radius {ValueWithUnits} :
 * }}
 */
export function characterizeArc(definition is map) returns map
{
    var x12 = (definition.start[0] - definition.mid[0]);
    var x13 = (definition.start[0] - definition.end[0]);

    var y12 = (definition.start[1] - definition.mid[1]);
    var y13 = (definition.start[1] - definition.end[1]);

    var y31 = (definition.end[1] - definition.start[1]);
    var y21 = (definition.mid[1] - definition.start[1]);

    var x31 = (definition.end[0] - definition.start[0]);
    var x21 = (definition.mid[0] - definition.start[0]);

    var sx13 = definition.start[0] ^ 2 - definition.end[0] ^ 2;

    var sy13 = definition.start[1] ^ 2 - definition.end[1] ^ 2;

    var sx21 = definition.mid[0] ^ 2 - definition.start[0] ^ 2;
    var sy21 = definition.mid[1] ^ 2 - definition.start[1] ^ 2;

    var f = ((sx13) * (x12)
            + (sy13) * (x12)
            + (sx21) * (x13)
            + (sy21) * (x13))
    / (2 * ((y31) * (x12) - (y21) * (x13)));
    var g = ((sx13) * (y12)
            + (sy13) * (y12)
            + (sx21) * (y13)
            + (sy21) * (y13))
    / (2 * ((x31) * (y12) - (x21) * (y13)));

    var c = -(definition.start[0] ^ 2) -
    definition.start[1] ^ 2 - 2 * g * definition.start[0] - 2 * f * definition.start[1];

    // eqn of circle be
    // x^2 + y^2 + 2*g*x + 2*f*y + c = 0
    // where centre is (h = -g, k = -f) and radius r
    // as r^2 = h^2 + k^2 - c
    var h = -g;
    var k = -f;
    var sqr_of_r = h * h + k * k - c;

    // r is the radius
    var r = sqrt(sqr_of_r);

    return { "center" : vector(h, k), "radius" : r };
}

/**
 * Returns the counter-clockwise angle between a 2D `Vector` and the X-Axis. The result angle is between 0 and 2 * PI radians.
 */
export function angleFromX(point is Vector, center is Vector) returns ValueWithUnits
precondition
{
    is2dPoint(point);
    is2dPoint(center);
    !tolerantEquals(point, center);
}
{
    return angleFromX(point - center);
}

export function angleFromX(point is Vector) returns ValueWithUnits
precondition
{
    is2dPoint(point);
    !tolerantEquals(point, zeroVector(2) * meter);
}
{
    return (point[1] < 0 * meter ? 2 * PI * radian : 0 * radian) + atan2(point[1], point[0]);
}

/**
 * Given a distance along an arc, converts the distance to a specified angle.
 */
export function getAngleAlongArc(radius is ValueWithUnits, chordLength is ValueWithUnits) returns ValueWithUnits
{
    return asin(clamp(chordLength / (2 * radius), 0, 1)) * 2;
    //return acos(clamp((2 * radius ^ 2 - distance ^ 2) / (2 * radius ^ 2), 0, 1));
}

export function getArcLength(radius is ValueWithUnits, chordLength is ValueWithUnits) returns ValueWithUnits
{
    return getAngleAlongArc(radius, chordLength) / radian * radius;
}

/**
 * @param radius {ValueWithUnits} : @autocomplete `attribute.radius`
 */
export function applyAngle(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
{
    return vector(radius * cos(angle), radius * sin(angle));
}

/**
 * Returns the midpoint of an arc centered on the specified `center`, drawn counterclockwise from startPoint to endPoint.
 * @param radius {ValueWithUnits} : @autocomplete `attribute.radius`
 */
export function arcMidPoint(startPoint is Vector, endPoint is Vector, center is Vector) returns Vector
precondition
{
    is2dPoint(startPoint);
    is2dPoint(endPoint);
    is2dPoint(center);
    tolerantEquals(squaredNorm(startPoint - center), squaredNorm(endPoint - center));
}
{
    const vector1 = append(startPoint - center, 0 * meter) as Vector;
    const vector2 = append(endPoint - center, 0 * meter) as Vector;
    const midVector = (normalize(startPoint - center) + normalize(endPoint - center))->normalize() * norm(startPoint - center);
    return center + (cross(vector1, vector2)[2] < 0 ? -midVector : midVector);
}

/**
 * Returns `true` if a given point lies on a specified arc.
 */
export function isOnArc(startPoint is Vector, midPoint is Vector, endPoint is Vector, center is Vector, point is Vector) returns boolean
{
    const angle1 = angleFromX(startPoint, center); // no guarantee that angle1 is greater than or less than angle2
    const angle2 = angleFromX(endPoint, center);
    const middleAngle = angleFromX(midPoint, center);
    const minAngle = min(angle1, angle2);
    const maxAngle = max(angle1, angle2);

    const pointAngle = angleFromX(point, center);
    // if middle is betweeen min and max
    if (middleAngle > minAngle && middleAngle < maxAngle)
    {
        return pointAngle > minAngle && pointAngle < maxAngle; // false if pointAngle is outside min or max
    }
    else // middle is outside min and max
    {
        return pointAngle < minAngle || pointAngle > maxAngle; // false if pointAngle is inside min and max
    }
}