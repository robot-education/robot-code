FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

/**
 * An enum defining valid sketch operations which can be saved as `SketchData`.
 */
export enum SketchOperation
{
    LINE,
    ARC,
    CIRCLE
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
}

/**
 * A constructor for `SketchData`.
 *
 * @param definition {{
 *          @field operation {SketchOperation} :
 *                  @autocomplete `SketchOperation.LINE`
 *          @field start {Vector} : @requiredif `operation == SketchOperation.LINE || operation == SketchOperation.ARROW || operation == SketchOperation.ARC`
 *                  @autocomplete `startPoint`
 *          @field end {Vector} : @requiredif `operation == SketchOperation.LINE || operation == SketchOperation.ARROW || operation == SketchOperation.ARC`
 *                  @autocomplete `endPoint`
 *          @field mid {Vector} : @requiredif `operation == SketchOperation.ARC`
 *          @field center {Vector} : @requiredif `operation == SketchOperation.CIRCLE`
 *          @field radius {ValueWithUnits} : @requiredif `operation == SketchOperation.CIRCLE`
 *          @field firstCorner {Vector} : @requiredif `operation == SketchOperation.TEXT`
 *          @field secondCorner {Vector} : @requiredif `operation == SketchOperation.TEXT`
 *          @field text {String} : @requiredif `operation == SketchOperation.TEXT`
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
 * Converts entities into a `SketchDataArray` which can be consumed by `createSketchDataArray`.
 * @seealso [createSketchDataArray]
 * @param plane : @autocomplete `annotationControl.plane`
 */
export function extractSketchDataArray(context is Context, entities is Query, plane is Plane) returns SketchDataArray
{
    var sketchDataArray = [];
    const projectFunction = function(line is Line) returns Vector
        {
            return worldToPlane(plane, line.origin);
        };

    for (var entity in evaluateQuery(context, entities))
    {
        if (!isQueryEmpty(context, entity->qGeometry(GeometryType.LINE)))
        {
            const points = evEdgeTangentLines(context, {
                            "edge" : entity,
                            "parameters" : [0, 1]
                        })->mapArray(projectFunction);
            sketchDataArray = append(sketchDataArray, { "operation" : SketchOperation.LINE, "start" : points[0], "end" : points[1] });
        }
        else if (!isQueryEmpty(context, entity->qGeometry(GeometryType.CIRCLE)))
        {
            const circle = evCurveDefinition(context, { "edge" : entity });
            const center = worldToPlane(plane, circle.coordSystem.origin);
            sketchDataArray = append(sketchDataArray, { "operation" : SketchOperation.CIRCLE, "center" : center, "radius" : circle.radius });
        }
        else if (!isQueryEmpty(context, entity->qGeometry(GeometryType.ARC)))
        {
            const points = evEdgeTangentLines(context, {
                            "edge" : entity,
                            "parameters" : [0, 0.5, 1]
                        })->mapArray(projectFunction);

            sketchDataArray = append(sketchDataArray, { "operation" : SketchOperation.ARC, "start" : points[0], "mid" : points[1], "end" : points[2] });
        }
    }
    return sketchDataArray as SketchDataArray;
}

/**
 * Creates a sketch, adds a `SketchDataArray` to it, and then solves the sketch.
 * To add a `SketchDataArray` to an existing sketch, use `skSketchDataArray`.
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
    skSketchDataArray(sketch, "sketchData", definition);
    skSolve(sketch);
}

/**
 * Adds a `SketchDataArray` to a given `sketch`.
 * @param value {{
 *      @field sketchDataArray {SketchDataArray} :
 *              A `SketchDataArray` to create.
 *      @field location {Vector} : @optional
 *              A vector representing the center of created entities.
 *              @autocomplete `vector(0, 0) * meter`
 * }}
 */
export function skSketchDataArray(sketch is Sketch, textId is string, value is map)
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
