FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "804646d519131ab5d52e2baf");

import(path : "9cb13882ac97598c2be31cc1", version : "2650327f9f114cdb36c8acd2");

export enum BoundaryType
{
    POINT,
    CIRCLE
}

export type BoundaryElement typecheck canBeBoundaryElement;

export predicate canBeBoundaryElement(value)
{
    value is map;
    value["type"] is BoundaryType;
    value.location is Vector;
    value.valid is boolean;

    if (isElementCircle(value))
    {
        value.radius is ValueWithUnits;
    }
}

export predicate isElementPoint(element is map)
{
    element["type"] == BoundaryType.POINT;
}

export predicate isElementCircle(element is map)
{
    element["type"] == BoundaryType.CIRCLE;
}

export type PlateAttribute typecheck canBePlateAttribute;

export predicate canBePlateAttribute(value)
{
    value is map;
    value.query is Query;
    value.platePlane is Plane;
    value.depth is ValueWithUnits;

    value.boundary is array;
    for (var element in value.boundary)
    {
        element is BoundaryElement;
    }
}

// Abstraction: PLATE_ATTRIBUTE is hidden
const PLATE_ATTRIBUTE = "plateAttribute";

export function plateAttribute(plate is Query, platePlane is Plane, depth is ValueWithUnits, boundary is array)
{
    return {
                "query" : plate,
                "boundary" : boundary,
                "platePlane" : platePlane,
                "depth" : depth
            } as PlateAttribute;
}

export function setPlateAttribute(context is Context, plateAttribute is PlateAttribute)
{
    setAttribute(context, {
                "entities" : plateAttribute.query,
                "name" : PLATE_ATTRIBUTE,
                "attribute" : plateAttribute
            });
}

/**
 * Returns `true` if `entity` is a valid plate.
 */
export function isPlate(context is Context, entity is Query) returns boolean
{
    return !isQueryEmpty(context, qHasAttribute(entity, PLATE_ATTRIBUTE));
}

/**
 * Returns a `Query` containing all valid plates in `entities`.
 */
export function qPlateFilter(context is Context, entities is Query) returns Query
{
    return qHasAttribute(entities, PLATE_ATTRIBUTE);
}

export function getPlateAttribute(context is Context, plate is Query)
{
    return getAttribute(context, {
                "entity" : plate,
                "name" : PLATE_ATTRIBUTE
            });
}

/**
 * Expands a given plate by adding one or more `points` to it.
 * @param id : @autocomplete `id + "expandPlate"`.
 * @param definition {{
 *      @field plate {Query} : A query for the plate being expanded.
 *      @field points {array} : An array of 3D `points` to add to the plate.
 *      @field identities {array} : An array of `Query`s, one for each point in `points`.
 *      @field radius {ValueWithUnits} : The radius each point should be expanded to.
 * }}
 *
 * @returns {{
 *      @field invalidMatches {array} :
 *              An array of indicies into `points` corresponding to
 *              `points` which did not map to a valid `point` in `plate`.
 * }}
 */
export const opExpandPlate = function(context is Context, id is Id, definition is map)
    {
        if (!isPlate(context, definition.plate))
        {
            throw regenError("Expected valid plate.", ["plate"], definition.plate);
        }
        var plateAttribute = getPlateAttribute(context, definition.plate);
        const boundary = plateAttribute.boundary;

        const boundaryPointsResult = getBoundaryPoints(definition, boundary, plateAttribute.platePlane);
        const newBoundary = boundaryPointsResult.newBoundary;
        const indexGroups = boundaryPointsResult.indexGroups;
        const invalidMatches = boundaryPointsResult.invalidMatches;

        const tempId = id + "temp";
        sketchPlateExpansion(context, tempId, definition, indexGroups, boundary, newBoundary, plateAttribute.platePlane);
        const faces = extractFaces(context, tempId + "extract", qCreatedBy(tempId, EntityType.EDGE), plateAttribute.platePlane);

        const extrudeId = id + "extrude";
        opExtrude(context, extrudeId, {
                    "entities" : faces,
                    "direction" : plateAttribute.platePlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : plateAttribute.depth
                });
        cleanup(context, id + "delete", qCreatedBy(tempId, EntityType.BODY));

        opBoolean(context, id + "plate", {
                    "tools" : qUnion([definition.plate, qCreatedBy(extrudeId, EntityType.BODY)]),
                    "operationType" : BooleanOperationType.UNION
                });

        setPlateAttribute(context, mergeMaps(plateAttribute, { "boundary" : newBoundary }));
        return { "invalidMatches" : invalidMatches };
    };

/**
 * @returns {{
 *      @field invalidMatches {array} :
 *      @field indexGroups {map} : A map mapping plateIndicies to pointIndicies.
 *      @field newBoundary {array} : A boundary updated to match values.
 * }}
 */
function getBoundaryPoints(definition is map, boundary is array, platePlane is Plane) returns map
{
    // maps boundary elements to indicies
    const validBoundary = getValidBoundary(boundary);
    var indexGroups = {};
    var invalidMatches = [];
    for (var pointIndex, point in definition.points)
    {
        var matched = false;
        for (var plateIndex, element in validBoundary)
        {
            if (tolerantEquals(worldToPlane(platePlane, point), element.location))
            {
                indexGroups[plateIndex] = pointIndex;
                boundary[plateIndex] = mergeMaps(element, { "type" : BoundaryType.CIRCLE, "radius" : definition.radius });
                matched = true;
                break;
            }
        }
        if (!matched)
        {
            invalidMatches = append(invalidMatches, pointIndex);
        }
    }
    return {
            "invalidMatches" : invalidMatches,
            "indexGroups" : indexGroups,
            "newBoundary" : boundary
        };
}

/**
 * @return {map} : A map mapping valid indicies in boundary to the corresponding element.
 *          This format serves as a drop-in replacement for a regular array, with some elements possibly removed,
 *          while maintaining valid indexing into the non-modified boundary array.
 */
function getValidBoundary(boundary is array) returns map
{
    var validBoundary = {};
    for (var i, element in boundary)
    {
        if (isElementPoint(element) && element.valid)
        {
            validBoundary[i] = element;
        }
    }
    return validBoundary;
}

function sketchPlateExpansion(context is Context, id is Id, definition is map, indexGroups is map, boundary is array, newBoundary is array, platePlane is Plane)
{
    for (var plateIndex, pointIndex in indexGroups)
    {
        const currId = id + unstableIdComponent(pointIndex);
        setExternalDisambiguation(context, currId, definition.identities[pointIndex]);
        const currSketch = newSketchOnPlane(context, currId, { "sketchPlane" : platePlane });

        const prevIndex = getPrevious(size(boundary), plateIndex);
        const prev = newBoundary[prevIndex];

        const nextIndex = getNext(size(boundary), plateIndex);
        var next = newBoundary[nextIndex];

        const element = boundary[plateIndex];
        const newElement = newBoundary[plateIndex];

        const newPrevPoints = connectElements(prev, newElement);
        // connections from curr to next are preferred over prev to curr
        if (indexGroups[prevIndex] == undefined)
        {
            drawLine(currSketch, "newStartLine", newPrevPoints);

            const prevPoints = connectElements(prev, element);
            drawLine(currSketch, "startLine", prevPoints);
            if (isElementCircle(prev))
            {
                addArc(currSketch, "startArc", concatenateArrays([reverse(prevPoints), newPrevPoints]), prev.location, prev.radius);
            }
        }

        const newNextPoints = connectElements(newElement, next);
        if (indexGroups[nextIndex] == undefined)
        {
            drawLine(currSketch, "newEndLine", newNextPoints);
            const nextPoints = connectElements(element, next);
            drawLine(currSketch, "endLine", nextPoints);

            if (isElementCircle(next))
            {
                addArc(currSketch, "endArc", concatenateArrays([newNextPoints, reverse(nextPoints)]), next.location, next.radius);
            }
        }

        addArc(currSketch, "capArc", concatenateArrays([newPrevPoints, newNextPoints]), element.location, definition.radius);
        skSolve(currSketch);
    }

    for (var plateIndex, pointIndex in indexGroups)
    {
        const nextIndex = getNext(size(newBoundary), plateIndex);
        if (indexGroups[nextIndex] == undefined)
        {
            continue;
        }
        const next = newBoundary[nextIndex];
        const element = boundary[plateIndex];
        const newElement = newBoundary[plateIndex];

        const nextId = id + "next" + unstableIdComponent(pointIndex);
        setExternalDisambiguation(context, nextId,
            qUnion([definition.identities[pointIndex], definition.identities[indexGroups[nextIndex]]]));

        const nextSketch = newSketchOnPlane(context, nextId, { "sketchPlane" : platePlane });

        const newNextPoints = connectElements(newElement, next);
        drawLine(nextSketch, "newEndLine", newNextPoints);
        const nextPoints = connectElements(element, boundary[nextIndex]);
        drawLine(nextSketch, "endLine", nextPoints);

        skSolve(nextSketch);
    }
}

function drawLine(sketch is Sketch, sketchId is string, points is array)
{
    skLineSegment(sketch, sketchId, {
                "start" : points[0],
                "end" : points[1]
            });
}

function connectElements(start is BoundaryElement, end is BoundaryElement) returns array
{
    if (isElementCircle(start) && isElementCircle(end))
    {
        return circleToCircle(start, end);
    }
    else if (isElementCircle(start))
    {
        return pointToCircle(start, end.location);
    }
    else if (isElementCircle(end))
    {
        return pointToCircle(start.location, end);
    }
    return [start.location, end.location];
}

