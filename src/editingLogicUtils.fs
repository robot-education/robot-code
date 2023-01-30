FeatureScript 1948;
import(path : "onshape/std/containers.fs", version : "1948.0");
import(path : "onshape/std/context.fs", version : "1948.0");
import(path : "onshape/std/feature.fs", version : "1948.0");
import(path : "onshape/std/evaluate.fs", version : "1948.0");
import(path : "onshape/std/vector.fs", version : "1948.0");
import(path : "onshape/std/units.fs", version : "1948.0");
import(path : "onshape/std/math.fs", version : "1948.0");
import(path : "onshape/std/surfaceGeometry.fs", version : "1948.0");

/**
 * Returns `true` when a new `Query` selection is added to a query parameter (but not when a `Query` is removed).
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param queryParameter {string} : @autocomplete `"myQuery"`
 */
export function isQueryAdded(context is Context, oldDefinition is map, definition is map, queryParameter is string)
precondition
{
    definition[queryParameter] is Query;
}
{
    return oldDefinition != {} &&
        oldDefinition[queryParameter] != definition[queryParameter] &&
        // only returns true when a query is explicitly added
        !isQueryEmpty(context, definition[queryParameter]->qSubtraction(oldDefinition[queryParameter]));
}

/**
 * Returns `true` when a `Query` selection is removed from a query parameter (but not when a `Query` is added).
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param queryParameter {string} : @autocomplete `"myQuery"`
 */
export function isQueryRemoved(context is Context, oldDefinition is map, definition is map, queryParameter is string)
precondition
{
    definition[queryParameter] is Query;
}
{
    return oldDefinition != {} &&
        oldDefinition[queryParameter] != definition[queryParameter] &&
        // only returns true when a query is explicitly added
        !isQueryEmpty(context, oldDefinition[queryParameter]->qSubtraction(definition[queryParameter]));
}

/**
 * Returns `true` when a new `Query` selection is added to a query parameter (but not when a `Query` is removed).
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param queryParameter {string} : @autocomplete `"myQuery"`
 */
export function isQueryChanged(oldDefinition is map, definition is map, queryParameter is string)
precondition
{
    definition[queryParameter] is Query;
}
{
    return oldDefinition != {} && oldDefinition[queryParameter] != definition[queryParameter];
}

/**
 * Returns `true` when a `boolean` parameter is toggled from `false` to `true`.
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param booleanParameter {string} : @autocomplete `"myBoolean"`
 */
export function isBooleanChangedToTrue(oldDefinition is map, definition is map, booleanParameter is string)
precondition
{
    definition[booleanParameter] is boolean;
}
{
    return oldDefinition != {} && !oldDefinition[booleanParameter] && definition[booleanParameter];
}

/**
 * Returns `true` when a `boolean` parameter is toggled from `true` to `false`.
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param booleanParameter {string} : @autocomplete `"myBoolean"`
 */
export function isBooleanChangedToFalse(oldDefinition is map, definition is map, booleanParameter is string)
precondition
{
    definition[booleanParameter] is boolean;
}
{
    return oldDefinition != {} && oldDefinition[booleanParameter] && !definition[booleanParameter];
}

/**
 * Returns `true` when a `boolean` parameter is toggled.
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param booleanParameter {string} : @autocomplete `"myBoolean"`
 */
export function isBooleanChanged(oldDefinition is map, definition is map, booleanParameter is string)
precondition
{
    definition[booleanParameter] is boolean;
}
{
    return oldDefinition != {} && oldDefinition[booleanParameter] != definition[booleanParameter];
}

/**
 * Returns `true` when one or more parameters inside an array parameter is changed (and the number of items in the array is not).
 * @param arrayParameter {string} : @autocomplete `"myWidgets"`
 *
 */
export function isArrayParameterChanged(oldDefinition is map, definition is map, arrayParameter is string) returns boolean
precondition
{
    definition[arrayParameter] is array;
}
{
    return oldDefinition != {} &&
        oldDefinition[arrayParameter] != definition[arrayParameter] &&
        definition[arrayParameter] != [] &&
        size(definition[arrayParameter]) == size(oldDefinition[arrayParameter]);
}

/**
 * Returns `true` whenever one or more items are added to an array parameter.
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param arrayParameter {string} : @autocomplete `"myWidgets"`
 */
export function isArrayParameterItemAdded(oldDefinition is map, definition is map, arrayParameter is string) returns boolean
precondition
{
    definition[arrayParameter] is array;
}
{
    return oldDefinition != {} && definition[arrayParameter]->size() > oldDefinition[arrayParameter]->size();
}

/**
 * Returns `true` whenever one or more items are removed from an array parameter.
 * If the feature has just been created (`oldDefinition == {}`), returns `false`.
 * @param arrayParameter {string} : @autocomplete `"myWidgets"`
 */
export function isArrayParameterItemRemoved(oldDefinition is map, definition is map, arrayParameter is string) returns boolean
precondition
{
    definition[arrayParameter] is array;
}
{
    return oldDefinition != {} &&
        definition[arrayParameter]->size() < oldDefinition[arrayParameter]->size();
}

/**
 * A query for a solitary part in the part studio (either a solitary part, or a solitary unhidden part).
 */
export function qSinglePart(context is Context, definition is map, hiddenBodies is Query) returns Query
{
    if (size(evaluateQuery(context, qAllModifiableSolidBodiesNoMesh())) == 1)
    {
        return qAllModifiableSolidBodiesNoMesh();
    }
    else if (size(evaluateQuery(context, qAllModifiableSolidBodiesNoMesh()->qSubtraction(hiddenBodies))) == 1)
    {
        return qAllModifiableSolidBodiesNoMesh()->qSubtraction(hiddenBodies);
    }
    return qNothing();
}
