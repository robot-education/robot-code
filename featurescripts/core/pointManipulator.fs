FeatureScript 2960;
/**
 * Defines utilities for adding a point manipulator to a feature.
 *
 * To use, add `pointManipulatorPredicate` (or `ninePointManipulatorPredicate`) to the feature predicate
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

/**
 * The index of the center point of a nine point manipulator. @seealso [ninePointOffsets]
 */
export const NINE_POINT_CENTER_INDEX = 4;

export const NINE_POINT_INDEX_BOUNDS = { (unitless) : [0, NINE_POINT_CENTER_INDEX, 8] } as IntegerBoundSpec;

/**
 * Creates a hidden `index` parameter for a nine point manipulator, which defaults to the center point.
 * Use it in place of `pointManipulatorPredicate`.
 */
export predicate ninePointManipulatorPredicate(definition is map)
{
    annotation { "Name" : "Index", "UIHint" : ["ALWAYS_HIDDEN"] }
    isInteger(definition.index, NINE_POINT_INDEX_BOUNDS);
}

/**
 * The points of a nine point manipulator relative to the center of a `width` by `height` rectangle in the XY plane,
 * as in the std Frame feature: its corners, edge midpoints, and center. They're ordered:
 * ```
 * 8 7 6
 * 5 4 3
 * 2 1 0
 * ```
 */
export function ninePointOffsets(width is ValueWithUnits, height is ValueWithUnits) returns array
{
    return mapArray(range(0, 8), function(i)
        {
            return vector((1 - i % 3) * width / 2, (floor(i / 3) - 1) * height / 2, 0 * meter);
        });
}

const POINT_MANIPULATOR = "pointManipulator";

/**
 * Returns the current index, or 0 if it isn't a valid one.
 *
 * @param numPoints {number} : The current number of points. Used to properly clamp the bounds.
 */
export function getPointIndex(definition is map, numPoints is number) returns number
{
    return getPointIndex(definition, numPoints, 0);
}

/**
 * Returns the current index, or `defaultIndex` if it isn't a valid one.
 */
export function getPointIndex(definition is map, numPoints is number, defaultIndex is number) returns number
{
    const index = definition.index;
    return index is number && index >= 0 && index < numPoints ? index : defaultIndex;
}

/**
 * Returns the current index of a nine point manipulator, or its center if it isn't a valid one.
 */
export function getNinePointIndex(definition is map) returns number
{
    return getPointIndex(definition, 9, NINE_POINT_CENTER_INDEX);
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
    addPointManipulator(context, id, definition, points, getPointIndex(definition, size(points)));
}

/**
 * Adds the point manipulator with `index` chosen, e.g. from `getNinePointIndex`.
 */
export function addPointManipulator(context is Context, id is Id, definition is map, points is array, index is number)
{
    addManipulators(context, id, {
                (POINT_MANIPULATOR) : pointsManipulator({
                        "points" : points,
                        "index" : index
                    })
            });
}

export function pointManipulatorChange(definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[POINT_MANIPULATOR];
    if (manipulator == undefined || !(manipulator.index is number))
    {
        return definition;
    }
    definition.index = manipulator.index;
    return definition;
}
