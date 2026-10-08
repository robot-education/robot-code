FeatureScript 2960;
/**
 * A library of core utils which are frequently useful.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Returns `true` if the provided query is for a vertex.
 */
export function isVertex(context is Context, query is Query) returns boolean
{
    return !isQueryEmpty(context, query->qNthElement(0)->qEntityFilter(EntityType.VERTEX));
}

/**
 * Returns `true` if the provided query is for a mate connector.
 */
export function isMateConnector(context is Context, query is Query) returns boolean
{
    return !isQueryEmpty(context, query->qNthElement(0)->qBodyType(BodyType.MATE_CONNECTOR));
}

/**
 * Returns `true` if the provided query is for a circle.
 */
export function isCircle(context is Context, query is Query) returns boolean
{
    return !isQueryEmpty(context, query->qNthElement(0)->qGeometry(GeometryType.CIRCLE));
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
export function boundingBoxLength(context is Context, entities is Query) returns ValueWithUnits
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
 * Extracts the key-value pairs from `input` corresponding to `keys`.
 *
 * In other words, returns a copy of `input` with all keys which aren't in `keys` removed.
 *
 * @example `extractFromMap({ "a" : 1, "b" : 2 }, ["a"])` returns `{ "a" : 1 }`
 */
export function extractFromMap(input is map, keys is array) returns map
precondition
{
    for (var key in keys)
    {
        key is string;
    }
}
{
    var result = {};
    for (var key in keys)
    {
        result[key] = input[key];
    }
    return result;
}

// /**
//  * Robustly pattern-copies `entities` to each plane in `planes`, stabilized using `identities`. Returns an array of queries for the patterned bodies.
//  */
// export function patternToPlanes(context is Context, id is Id, planes is array, identities is array, entities is Query) returns array
// precondition
// {
//     for (var plane in planes)
//     {
//         plane is Plane;
//     }
//     for (var identity in identities)
//     {
//         identity is Query;
//     }
// }
// {
//     var queries = [];
//     for (var i, plane in planes)
//     {
//         const patternId = id + unstableIdComponent(i);
//         // setExternalDisambiguation(context, patternId, identities[i]);
//         opPattern(context, patternId, {
//                     "entities" : entities,
//                     "transforms" : [toWorld(plane->coordSystem())],
//                     "instanceNames" : [toString(i)]
//                 });
//         queries = append(queries, qCreatedBy(patternId, EntityType.BODY));
//     }
//     cleanup(context, id + "delete", entities);
//     return queries;
// }

/**
 * Throws a [regenError] and marks the specified [Query] parameter as faulty if the specified `Query` parameter is not
 * a `Query` which resolves to at least one entity.
 *
 * Is overloaded to work with array parameters as well, e.g. `parameterName = "myArrayParameter[" ~ i ~ "].myParameter"`
 *
 * @param parameterName {string} : @autocomplete `arrayParameterId("myArray", i, "myParameter")`
 *
 * @returns : An array representing the result of evaluating the `Query` parameter with [evaluateQuery]
 */
export function verifyNonemptyArrayQuery(context is Context, definition is map, parameterName is string, errorToReport is string) returns array
{
    const result = evaluateQuery(context, getParameter(definition, parameterName));
    if (result == [])
    {
        throw regenError(errorToReport, [parameterName]);
    }
    return result;
}

/**
 * Accesses a value in `definition` according to a given `parameterName`. Supports array parameters.
 */
export function getParameter(definition is map, parameterName is string)
{
    const parsed = match(parameterName, "([\\w\\d]+)\\[(\\d+)\\]\\.([\\w\\d]+)");
    if (parsed.hasMatch)
    {
        return definition[parsed.captures[1]][parsed.captures[2]->stringToNumber()][parsed.captures[3]];
    }
    return definition[parameterName];
}
