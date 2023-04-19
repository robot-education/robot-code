FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

/**
 * Retrieve a variable attached to the context by name.
 * If the variable does not exist, returns `defaultValue` (instead of throwing an exception).
 */
export function getVariableWithDefault(context is Context, name is string, defaultValue)
{
    const value = try silent(getVariable(context, name));
    return (value == undefined ? defaultValue : value);
}

/**
 * Checks if two 2D lines intersect each other.
 * @returns {Vector} : A 2D `Vector` representing the intersection point, or `undefined` if no intersection exists.
 */
export function intersection2d(point1 is Vector, direction1 is Vector, point2 is Vector, direction2 is Vector)
precondition
{
    is2dPoint(point1);
    size(direction1) == 2;
    is2dPoint(point2);
    size(direction2) == 2;
}
{
    direction1 = normalize(direction1);
    direction2 = normalize(direction2);
    if (cross2d(direction1, direction2) == 0)
    {
        return undefined;
    }
    return point1 + direction1 * (cross2d((point2 - point1), direction2) / cross2d(direction1, direction2));
}

/**
 * Returns a scalar representing the cross product of two 2d vectors.
 * The result is either a ValueWIthUnits (with units equal to length & 2) or a number based on the units of the input vectors.
 */
export function cross2d(point1 is Vector, point2 is Vector)
{

    return (point1[0] * point2[1] - point1[1] * point2[0]);
}

/**
 * Throws a [regenError] and marks the specified [Query] parameter as faulty if the specified `Query` parameter is not
 * a `Query` which resolves to at least one entity.
 *
 * Is overloaded to work with array parameters as well, e.g. `parameterName = "myArrayParameter[" ~ i ~ "].myParameter"`
 *
 * @param parameterName {string} : @autocomplete `"myArray[" ~ i ~ "].myParameter"`
 *
 * @returns : An array representing the result of evaluating the `Query` parameter with [evaluateQuery]
 */
export function verifyNonemptyArrayQuery(context is Context, definition is map, parameterName is string, errorToReport is string) returns array
{
    const result = evaluateQuery(context, parseParameter(definition, parameterName));
    if (result == [])
    {
        throw regenError(errorToReport, [parameterName]);
    }
    return result;
}

/**
 * Accesses a value in map according to a given parameterName. Is overloaded for array parameters, so syntax like
 * `"myArrayParameter[i].myArrayParam"` behaves as expected.
 */
function parseParameter(definition is map, parameterName is string)
{
    const parsed = match(parameterName, "([\\w\\d]+)\\[(\\d+)\\]\\.([\\w\\d]+)");
    if (parsed.hasMatch)
    {
        return definition[parsed.captures[1]][parsed.captures[2]->stringToNumber()][parsed.captures[3]];
    }
    return definition[parameterName];
}

export enum Type
{
    PLANE,
    LINE,
    VECTOR,
    TRANSFORM,
    MATRIX,
    VECTOR_ARRAY,
    VALUE_WITH_UNITS
}

/**
 * Casts a value as a given `Type`.
 */
export function typecast(value, derivedType is Type)
{
    if (derivedType == Type.VECTOR)
    {
        return value as Vector;
    }
    else if (derivedType == Type.MATRIX)
    {
        return value as Matrix;
    }
    else if (derivedType == Type.PLANE)
    {
        value.origin = typecast(value.origin, Type.VECTOR);
        value.normal = typecast(value.normal, Type.VECTOR);
        value.x = typecast(value.x, Type.VECTOR);
        return value as Plane;
    }
    else if (derivedType == Type.LINE)
    {
        value.origin = typecast(value.origin, Type.VECTOR);
        value.direction = typecast(value.direction, Type.VECTOR);
        return value as Line;
    }
    else if (derivedType == Type.TRANSFORM)
    {
        value.translation = typecast(value.translation, Type.VECTOR);
        value.linear = typecast(value.linear, Type.MATRIX);
        return value as Transform;
    }
    else if (derivedType == Type.VECTOR_ARRAY) // allows types to be nested infintiely deep
    {
        return mapArray(value, function(item)
            {
                return typecast(item, Type.VECTOR);
            });
    }
    else if (derivedType == Type.VALUE_WITH_UNITS)
    {
        return value as ValueWithUnits;
    }
}

/**
 * Typecasts key-value pairs in `inputMap` according to the keys and types specified by a given `typeMap`.
 * @param inputMap : @autocomplete `attribute`
 * @param typeMap {{
 *      @field key1 {Type} : @autocomplete `Type.VECTOR`,
 *      @field key2 {Type} : @autocomplete `Type.VALUE_WITH_UNITS`
 * }}
 */
export function typecast(inputMap is map, typeMap is map) returns map
{
    for (var key, derivedType in typeMap)
    {
        if (inputMap[key] != undefined)
        {
            inputMap[key] = typecast(inputMap[key], derivedType);
        }
    }
    return inputMap;
}