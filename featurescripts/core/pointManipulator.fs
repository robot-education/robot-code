FeatureScript 2960;
/**
 * Defines utilities for adding a point manipulator to a feature.
 *
 * To use, add `pointManipulatorPredicate` to the feature predicate
 * and `pointManipulatorChange` to the manipulator change function,
 * then call `addPointManipulator` and, optionally, `getPointIndex`.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export const INDEX_BOUNDS = { (unitless) : [0, 0, 1e50] } as IntegerBoundSpec;

/**
 * Creates a hidden `index` parameter used to track the index of the point manipulator.
 */
export predicate pointManipulatorPredicate(definition is map)
{
    annotation { "Name" : "Index", "UIHint" : ["ALWAYS_HIDDEN"] }
    isInteger(definition.index, INDEX_BOUNDS);
}

const POINT_MANIPULATOR = "pointManipulator";

/**
 * Returns the current index.
 *
 * @param numPoints {number} : The current number of points. Used to properly clamp the bounds.
 */
export function getPointIndex(definition is map, numPoints is number) returns number
{
    const index = definition.index;
    if (index < 0 || index >= numPoints)
    {
        return 0;
    }
    return index;
}

export function getPointDistance(definition is map, pointDistances is array) returns ValueWithUnits
precondition
{
    isLengthVector(pointDistances);
}
{ 
    const pointIndex = getPointIndex(definition, size(pointDistances));
    return pointDistances[pointIndex];
}

/**
 * Applies the point manipulator to the given plane.
 */
export function applyPointManipulator(definition is map, plane is Plane, pointDistances is array) returns Plane
precondition
{
    isLengthVector(pointDistances);
}
{
    plane.origin -= plane.normal * getPointDistance(definition, pointDistances);
    return plane;
}

export function addPointManipulator(context is Context, id is Id, definition is map, points is array)
{
    addManipulators(context, id, {
                (POINT_MANIPULATOR) : pointsManipulator({
                        "points" : points,
                        "index" : getPointIndex(definition, size(points))
                    })
            });
}

export function pointManipulatorChange(definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[POINT_MANIPULATOR];
    if (manipulator == undefined)
    {
        return definition;
    }
    definition.index = manipulator.index;
    return definition;
}
