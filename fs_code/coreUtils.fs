FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

/**
 * A library of core utils which are frequently useful.
 */

/**
 * Returns a value created by mapping each element in an array through an `accumulator` function.
 *
 * @example `accumulate([1, 2, 3], 0, function(val, item) { return item + val; })` returns `6` (0 + 1 + 2 + 3)
 * @example `accumulate([2, 2, 3], 1, function(val, item) { return item * val; })` returns `12` (1 * 2 * 2 * 3)
 *
 * @param accumulator {function} :
 *          A function which takes in two arguments (the accumulated value and a member of the input function)
 *          and returns an updated `initialValue` (which becomes the first argument to next call of `accumulator`).
 *          @autocomplete ```
 *          function(val, item)
 *          {
 *              return val + item;
 *          }```
 */
export function accumulate(arr is array, initialValue, accumulator is function)
{
    for (var item in arr)
    {
        initialValue = accumulator(initialValue, item);
    }
    return initialValue;
}

/**
 * Returns `true` if the first provided query is for a vertex.
 */
export function isVertex(context is Context, query is Query) returns boolean
{
    return !isQueryEmpty(context, query->qNthElement(0)->qEntityFilter(EntityType.VERTEX));
}

/**
 * Returns `true` if the first provided query is for an edge.
 */
export function isEdge(context is Context, query is Query) returns boolean
{
    return !isQueryEmpty(context, query->qNthElement(0)->qEntityFilter(EntityType.EDGE));
}

/**
 * Returns the coordinate system representing a sketch vertex or mate connector.
 * Can be used to parse input from query parameters with `Filter` set to:
 * `(EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR`
 *
 * @param arg {{
 *      @field vertex {Query} : A [Query] for a sketch vertex or mate connector.
 * }}
 */
export function evVertexCoordSystem(context is Context, arg is map) returns CoordSystem
precondition
{
    arg.vertex is Query;
}
{
    if (!isQueryEmpty(context, arg.vertex->qNthElement(0)->qEntityFilter(EntityType.VERTEX)->qSketchFilter(SketchObject.YES)))
    {
        var plane = evOwnerSketchPlane(context, { "entity" : arg.vertex });
        plane.origin = evVertexPoint(context, { "vertex" : arg.vertex });
        return coordSystem(plane);
    }
    else if (!isQueryEmpty(context, arg.vertex->qNthElement(0)->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        return evMateConnector(context, { "mateConnector" : arg.vertex });
    }
    throw regenError("Expected a sketch vertex or mate connector.");
}

/**
 * Returns the diagonal length of a bounding box containing `entities`.
 * @throws {GBTErrorStringEnum.CANNOT_BE_EMPTY} : `entities` is empty.
 */
export function qBoundingBoxLength(context is Context, entities is Query) returns ValueWithUnits
{
    if (isQueryEmpty(context, entities))
    {
        throw regenError(ErrorStringEnum.CANNOT_BE_EMPTY);
    }
    return evBox3d(context, {
                    "topology" : entities,
                    "tight" : false
                })->box3dDiagonalLength();
}

/**
 * Deletes `entities`. Does nothing if `entities` is empty.
 * @param id : @autocomplete `id + "delete"`
 */
export function cleanup(context is Context, id is Id, entities is Query)
{
    if (!isQueryEmpty(context, entities))
    {
        opDeleteBodies(context, id, { "entities" : entities });
    }
}

/**
 * Fetches the values of all maps in the `input` array corresponding to the given `key`.
 *
 * @example `extractFromArrayOfMaps([{ "myKey" : 2 }, { "myKey" : 3 }], "myKey")` returns `[2, 3]`
 * @example `extractFromArrayOfMaps([{ "myKey" : 2 }, { "otherKey" : 3 }], "myKey")` returns `[2, undefined]`
 *
 * @param inputArray {array} : @autocomplete `definition.myArray`
 *          An array of maps.
 * @param key : @autocomplete `"myParameter"`
 * @returns {array} : An `array` the same size as `input` containing the values from each map in `input` corresponding to `key`.
 */
export function extractFromArrayOfMaps(input is array, key) returns array
precondition
{
    for (var item in input)
    {
        item is map;
    }
}
{
    var result = [];
    for (var item in input)
    {
        result = append(result, item[key]);
    }

    return result;
}

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
