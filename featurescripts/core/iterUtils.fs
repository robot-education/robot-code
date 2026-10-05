FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");


/**
 * Returns the next element of an array.
 * @param index : @autocomplete `i`
 */
export function getNext(inputArray is array, index is number)
{
    return inputArray[(index + 1) % size(inputArray)]; // if index = size(geometry), returns geometry[0]
}

/**
 * Returns the index of the next element of an array.
 * @param arraySize : @autocomplete `size(inputArray)`
 * @param index : @autocomplete `i`
 */
export function getNext(arraySize is number, index is number) returns number
{
    return (index + 1) % arraySize;
}

/**
 * Returns the previous element of an array.
 * @param index : @autocomplete `i`
 */
export function getPrevious(inputArray is array, index is number)
{
    return inputArray[(index + size(inputArray) - 1) % size(inputArray)]; // if index = 0, returns geometry[size(geometry)]
}

/**
 * Returns the index of the previous element of an array.
 * @param arraySize : @autocomplete `size(inputArray)`
 * @param index : @autocomplete `i`
 */
export function getPrevious(arraySize is number, index is number) returns number
{
    return (index + arraySize - 1) % arraySize;
}
