FeatureScript 2960;
/**
 * This module provides utilities for quickly inserting static sketches captured using FeatureScript.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * An enum defining valid sketch operations which can be saved as `SketchData`.
 */
export enum SketchOperation
{
    LINE,
    ARC,
    CIRCLE,
    /** A smooth spline through `points`. */
    SPLINE
}

export type SketchDataArray typecheck canBeSketchDataArray;

export predicate canBeSketchDataArray(value)
{
    value is array;
    for (var data in value)
    {
        canBeSketchData(data);
    }
}

/**
 * A type representing the data of a single sketch entity.
 * @type {{
 *      @field operation {SketchOperation} :
 *          The `SketchOperation` defining the symbol.
 * }}
 */
export type SketchData typecheck canBeSketchData;

export predicate canBeSketchData(value)
{
    value is map;
    value.operation is SketchOperation;
    if (value.operation == SketchOperation.LINE)
    {
        is2dPoint(value.start);
        is2dPoint(value.end);
    }
    else if (value.operation == SketchOperation.ARC)
    {
        is2dPoint(value.start);
        is2dPoint(value.mid);
        is2dPoint(value.end);
    }
    else if (value.operation == SketchOperation.CIRCLE)
    {
        is2dPoint(value.center);
        isLength(value.radius);
    }
    else if (value.operation == SketchOperation.SPLINE)
    {
        value.points is array;
        for (var point in value.points)
        {
            is2dPoint(point);
        }
    }
}

/**
 * A constructor for `SketchData`.
 *
 * @param definition {{
 *          @field operation {SketchOperation} :
 *                  @autocomplete `SketchOperation.LINE`
 *          @field start {Vector} : @requiredif `operation == SketchOperation.LINE || operation == SketchOperation.ARC`
 *                  @autocomplete `startPoint`
 *          @field end {Vector} : @requiredif `operation == SketchOperation.LINE || operation == SketchOperation.ARC`
 *                  @autocomplete `endPoint`
 *          @field mid {Vector} : @requiredif `operation == SketchOperation.ARC`
 *          @field center {Vector} : @requiredif `operation == SketchOperation.CIRCLE`
 *          @field radius {ValueWithUnits} : @requiredif `operation == SketchOperation.CIRCLE`
 *          @field points {array} : @requiredif `operation == SketchOperation.SPLINE`
 *                  The points the spline passes through, in order.
 * }}
 */
export function sketchData(definition is map) returns SketchData
precondition
{
    canBeSketchData(definition);
}
{
    return definition as SketchData;
}

/**
 * Creates a sketch, adds a `SketchDataArray` to it, and then solves the sketch.
 * To add a `SketchDataArray` to an existing sketch, use `skDataArray`.
 *
 * @param id : @autocomplete `id + "sketchData"`
 * @param definition {{
 *      @field plane {Plane} :
 *              The plane to place the sketch data on. The plane's origin is used as the origin of the sketch data.
 *      @field sketchDataArray {SketchDataArray} :
 *              A `SketchDataArray` to use.
 * }}
 */
export function createSketchDataArray(context is Context, id is Id, definition is map)
precondition
{
    definition.sketchDataArray is SketchDataArray;
    definition.plane is Plane;
}
{
    var sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : definition.plane });
    skDataArray(sketch, "sketchData", definition);
    skSolve(sketch);
}

/**
 * Adds a `SketchDataArray` to a given `sketch`.
 * @param texId {string} : @autocomplete `"dataArray"`
 * @param value {{
 *      @field sketchDataArray {SketchDataArray} :
 *              A `SketchDataArray` to create.
 *      @field location {Vector} : @optional
 *              A vector representing the center of created entities.
 *              Defaults to `zeroVector(2) * meter`.
 *              @autocomplete `vector(0, 0) * meter`
 * }}
 */
export function skDataArray(sketch is Sketch, textId is string, value is map)
precondition
{
    value.sketchDataArray is SketchDataArray;
    value.location is undefined || value.location is Vector;
}
{
    value = mergeMaps({ "location" : zeroVector(2) * meter }, value);
    const needOffset = !tolerantEquals(value.location, zeroVector(2) * meter);
    for (var i, sketchData in value.sketchDataArray)
    {
        if (sketchData.operation == SketchOperation.LINE)
        {
            sketchData = addLocation(sketchData, value.location, ["start", "end"], needOffset);
            skLineSegment(sketch, textId ~ "line" ~ i, sketchData);
        }
        else if (sketchData.operation == SketchOperation.CIRCLE)
        {
            sketchData = addLocation(sketchData, value.location, ["center"], needOffset);
            skCircle(sketch, textId ~ "circle" ~ i, sketchData);
        }
        else if (sketchData.operation == SketchOperation.ARC)
        {
            sketchData = addLocation(sketchData, value.location, ["start", "mid", "end"], needOffset);
            skArc(sketch, textId ~ "arc" ~ i, sketchData);
        }
        else if (sketchData.operation == SketchOperation.SPLINE)
        {
            if (needOffset)
            {
                sketchData.points = mapArray(sketchData.points, function(point)
                    {
                        return point + value.location;
                    });
            }
            skFitSpline(sketch, textId ~ "spline" ~ i, { "points" : sketchData.points });
        }
    }
}

function addLocation(sketchData is map, location is Vector, keys is array, needOffset is boolean)
{
    if (needOffset)
    {
        for (var key in keys)
        {
            sketchData[key] += location;
        }
    }
    return sketchData;
}
