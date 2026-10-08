FeatureScript 2960;
/**
 * Defines functions and behaviors shared by plate features.
 */

import(path : "onshape/std/common.fs", version : "2960.0");


export const POINT_MANIPULATOR = "pointManipulator";
export const FLIP_MANIPULATOR = "flipManipulator";
export const PLATE_EXTRUDE_MANIPULATOR = "plateExtrudeManipulator";

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
