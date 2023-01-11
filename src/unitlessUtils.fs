FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

/**
 * A module defining `unitless` versions of many common STD functions which enable more performant useage.
 */

/**
 * A vector with no units.
 */
export type UnitlessVector typecheck canBeUnitlessVector;

export predicate canBeUnitlessVector(value)
{
    value is array;
}

/**
 * A plane with no units.
 */
export type UnitlessPlane typecheck canBeUnitlessPlane;

export predicate canBeUnitlessPlane(value)
{
    value.origin is UnitlessVector;
    is3dDirection(value.normal);
    is3dDirection(value.x);
    abs(dot(value.x, value.normal)) < TOLERANCE.zeroAngle;
}

/**
 * Create a [Plane] which fully specifies its orientation.
 *
 * @param origin : A 3D point in world space.
 * @param normal : A 3D vector in world space. Need not be normalized.
 * @param x      : A 3D vector in world space. Need not be normalized.
 */
export function unitlessPlane(origin is UnitlessVector, normal is UnitlessVector, x is UnitlessVector) returns UnitlessPlane
{
    return { "origin" : origin, "normal" : unitlessNormalize(normal), "x" : unitlessNormalize(x) } as UnitlessPlane;
}

/**
 * Create a `UnitlessPlane` on the XY plane of a specified coordinate system.
 */
export function unitlessPlane(cSys is UnitlessCoordSystem) returns Plane
{
    return unitlessPlane(cSys.origin, cSys.zAxis, cSys.xAxis);
}

export type UnitlessCoordSystem typecheck canBeUnitlessCoordSystem;

export predicate canBeUnitlessCoordSystem(value)
{
    value.origin is UnitlessVector;
    is3dDirection(value.zAxis);
    is3dDirection(value.xAxis);
}

/**
 * Returns the (unitless) result of normalizing vector. Throws if the input is zero-length.
 * @param vector : A Vector with any units.
 */
export function unitlessNormalize(vector is UnitlessVector) returns UnitlessVector
{
    return vector / unitlessNorm(vector);
}

/**
 * Returns the length (norm) of a vector.
 */
export function unitlessNorm(vector is UnitlessVector) returns number
{
    return sqrt(squaredNorm(vector));
}

export function unitlessCross(vector1 is UnitlessVector, vector2 is UnitlessVector) returns UnitlessVector
{
    return cross(vector1, vector2) as UnitlessVector;
}

/**
 * Checks if one value is within `TOLERANCE.zeroLength` of another.
 */
export predicate tolerantEqualsUnitless(value1 is number, value2 is number)
{
    abs(value1 - value2) < TOLERANCE.zeroLength;
}

/**
 *
 */
export function worldToPlaneUnitless(plane is UnitlessPlane, vector is UnitlessVector)
{

}
