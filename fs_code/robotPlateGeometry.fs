FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "c470cc508bab81129ff0d110");
import(path : "a31342637c8f0fafa3d91dec", version : "a0e9b2cd23196f5e48dc48b7");

import(path : "2a1fbdd680ed055fe57e372f", version : "b09d630978b71b04194fc4a8");
import(path : "9cb13882ac97598c2be31cc1", version : "68db899741d9fca7b9ee84df");

/**
 * Extracts the plate face defined by the plate geometry query.
 * @param geometryDefinition {{
 *          @field flipMap {map} :
 *          @field selections {array} :
 *          @field toDelete {box} :
 * }}
 *
 * @returns {{
 *      @field manipulators {map} :
 *      @field centerid {Vector} :
 *      @field plateFaces {Query} :
 *      @field boundary {array} : A boundary defined in proper clockwise order.
 * }}
 */
export function getPlateGeometryFaces(context is Context, id is Id, platePlane is Plane, geometryDefinition is map) returns map
{
    var plateGeometryArray = getPlateGeometryArray(context, id, platePlane, geometryDefinition.selections, geometryDefinition.flipMap);
    validatePlateGeometryArray(plateGeometryArray);

    const locations = extractFromArrayOfMaps(plateGeometryArray, "location");
    const centroid = computeCentroid(locations);
    const counterClockwise = isCounterClockwise(locations, centroid);

    var manipulators = plateArcFlipManipulators(platePlane, plateGeometryArray);

    const sketchId = id + "sketch";
    sketchSelections(context, sketchId + "user", plateGeometryArray, platePlane);

    const connectingPointsArray = getConnectingPointsArray(plateGeometryArray, counterClockwise);
    manipulators = mergeMaps(platePointManipulator(platePlane, connectingPointsArray), manipulators);
    addPlateDebugEdges(context, platePlane, connectingPointsArray);

    sketchConnectingLines(context, sketchId + "auto",
        extractFromArrayOfMaps(plateGeometryArray, "identity"), platePlane, connectingPointsArray);

    geometryDefinition.toDelete[] = concatenateArrays([geometryDefinition.toDelete[], [
                    qCreatedBy(sketchId, EntityType.BODY),
                    qCreatedBy(id + "extractPlateFace", EntityType.BODY)
                ]]);

    const sketchEdges = qCreatedBy(sketchId, EntityType.EDGE);
    const plateFaces = extractFaces(context, id + "extractPlateFace", sketchEdges, platePlane);
    if (isQueryEmpty(context, plateFaces))
    {
        throw regenError(customError(CustomError.PLATE_GEOMETRY_FAILED), ["selections"], sketchEdges);
    }

    var boundary = getBoundary(plateGeometryArray);
    if (counterClockwise)
    {
        boundary = reverse(boundary);
    }
    return {
            "manipulators" : manipulators,
            "centroid" : centroid,
            "plateFaces" : plateFaces,
            "boundary" : boundary
        };
}

enum PlateSelectionType
{
    POINT,
    CIRCLE,
    ARC
}

type PlateGeometry typecheck canBePlateGeometry;

export predicate canBePlateGeometry(value)
{
    value is map;
    value.identity is Query;
    value["type"] is PlateSelectionType;

    is2dPoint(value.location);

    if (isPlateArcOrCircle(value))
    {
        value.radius is ValueWithUnits;
    }

    if (isPlateArc(value))
    {
        value.flipped is boolean;
        is3dDirection(value.direction);

        value.vertices is array;
        size(value.vertices) == 3;
    }
}

predicate isPlatePoint(plateGeometry is map)
{
    plateGeometry["type"] == PlateSelectionType.POINT;
}

predicate isPlateArc(plateGeometry is map)
{
    plateGeometry["type"] == PlateSelectionType.ARC;
}

predicate isPlateCircle(plateGeometry is map)
{
    plateGeometry["type"] == PlateSelectionType.CIRCLE;
}

predicate isPlateArcOrCircle(plateGeometry is map)
{
    isPlateArc(plateGeometry) || isPlateCircle(plateGeometry);
}

/**
 * @return an array of `PlateGeometry`, one for each selection in `selections`.
 */
function getPlateGeometryArray(context is Context, id is Id, platePlane is Plane, selections is array, flipMap is map) returns array
{
    return mapArray(selections, function(selection is Query) returns PlateGeometry
        {
            var plateGeometry = {
                "identity" : selection,
                "type" : classifyPlateSelection(context, selection)
            };

            if (isPlatePoint(plateGeometry))
            {
                plateGeometry.location = worldToPlane(platePlane, evVertexPoint(context, { "vertex" : selection }));
            }
            else
            {
                const curveDefinition = evCurveDefinition(context, { "edge" : selection });
                plateGeometry.radius = curveDefinition.radius;

                if (isPlateCircle(plateGeometry))
                {
                    plateGeometry.location = worldToPlane(platePlane, curveDefinition.coordSystem.origin);
                }
                else if (isPlateArc(plateGeometry))
                {
                    // == true to handle undefined
                    plateGeometry.flipped = (flipMap[selection] == true);

                    const edges = evEdgeTangentLines(context, {
                                "edge" : selection,
                                "parameters" : [0, 0.5, 1]
                            });
                    plateGeometry.direction = edges[1].direction;

                    const vertices = mapArray(edges, function(line is Line)
                        {
                            return worldToPlane(platePlane, line.origin);
                        });

                    plateGeometry.location = vertices[1];
                    plateGeometry.vertices = [vertices[0], vertices[2]];
                    if (plateGeometry.flipped)
                    {
                        plateGeometry.vertices = reverse(plateGeometry.vertices);
                    }
                }
            }
            return plateGeometry as PlateGeometry;
        });
}

function classifyPlateSelection(context is Context, selection is Query) returns PlateSelectionType
{
    if (!isQueryEmpty(context, selection->qEntityFilter(EntityType.VERTEX)))
    {
        return PlateSelectionType.POINT;
    }
    else if (!isQueryEmpty(context, selection->qGeometry(GeometryType.ARC)))
    {
        return PlateSelectionType.ARC;
    }
    else if (!isQueryEmpty(context, selection->qGeometry(GeometryType.CIRCLE)))
    {
        return PlateSelectionType.CIRCLE;
    }
    throw regenError(customError(CustomError.INVALID_GEOMETRY_SELECTION), ["selections"], selection);
}

function plateArcFlipManipulators(platePlane is Plane, plateGeometryArray is array) returns map
{
    var flipManipulators = {};
    var i = 0;
    for (var plateGeometry in plateGeometryArray)
    {
        if (!isPlateArc(plateGeometry))
        {
            continue;
        }
        flipManipulators[(FLIP_MANIPULATOR ~ i)] = flipManipulator({
                    "base" : planeToWorld(platePlane, plateGeometry.location),
                    "direction" : plateGeometry.direction,
                    "flipped" : plateGeometry.flipped,
                });
        i += 1;
    }
    return flipManipulators;
}

function validatePlateGeometryArray(plateGeometryArray is array)
{
    for (var i, curr in plateGeometryArray)
    {
        const next = getNext(plateGeometryArray, i);
        if (isConnected(curr, next))
        {
            throw regenError(customError(CustomError.PLATE_GEOMETRY_TOUCHING), ["selections"], qUnion([curr.identity, next.identity]));
        }
    }
}

/**
 * Returns true if two plateGeometries are physically touching.
 * @param start : @autocomplete `curr`
 * @param end : @autocomplete `next`
 */
function isConnected(start is PlateGeometry, end is PlateGeometry) returns boolean
{
    var startPoint;
    if (!isPlateCircle(start))
    {
        startPoint = isPlatePoint(start) ? start.location : start.vertices[1];
    }

    var endPoint;
    if (!isPlateCircle(end))
    {
        endPoint = isPlatePoint(end) ? end.location : end.vertices[0];
    }

    if (isPlateCircle(start) && isPlateCircle(end))
    {
        return tolerantEquals(start.location, end.location);
    }
    else if (isPlateCircle(start) && !isPlateCircle(end))
    {
        return norm(start.location - endPoint) <= start.radius + TOLERANCE.zeroLength * meter;
    }
    else if (isPlateCircle(end) && !isPlateCircle(start))
    {
        return norm(end.location - startPoint) <= end.radius + TOLERANCE.zeroLength * meter;
    }
    return tolerantEquals(startPoint, endPoint);
}


function sketchSelections(context is Context, id is Id, plateGeometryArray is array, platePlane is Plane)
{
    for (var i, plateGeometry in plateGeometryArray)
    {
        if (!isPlateArc(plateGeometry) && !isPlateCircle(plateGeometry))
        {
            continue;
        }

        setExternalDisambiguation(context, id + unstableIdComponent(i), plateGeometry.identity);
        const userSketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : platePlane });

        if (isPlateArc(plateGeometry))
        {
            skArc(userSketch, "user", {
                        "start" : plateGeometry.vertices[0],
                        "mid" : plateGeometry.location,
                        "end" : plateGeometry.vertices[1]
                    });
        }
        else if (isPlateCircle(plateGeometry))
        {
            skCircle(userSketch, "user", {
                        "center" : plateGeometry.location,
                        "radius" : plateGeometry.radius
                    });
        }

        skSolve(userSketch);
    }
}

/**
 * Returns pairs representing connecting lines drawn between selections.
 */
function getConnectingPointsArray(plateGeometryArray is array, counterClockwise is boolean) returns array
{
    var connectingPoints = [];
    for (var i, curr in plateGeometryArray)
    {
        const next = getNext(plateGeometryArray, i);
        connectingPoints = append(connectingPoints, getConnectingPoints(curr, next, counterClockwise));
    }
    return connectingPoints;
}

function getConnectingPoints(start is PlateGeometry, end is PlateGeometry, counterClockwise is boolean) returns array
{
    var startPoint;
    if (isPlatePoint(start))
    {
        startPoint = start.location;
    }
    else if (isPlateArc(start))
    {
        startPoint = start.vertices[1];
    }

    var endPoint;
    if (isPlatePoint(end))
    {
        endPoint = end.location;
    }
    else if (isPlateArc(end))
    {
        endPoint = end.vertices[0];
    }

    if (isPlateCircle(start))
    {
        if (isPlateCircle(end))
        {
            return circleToCircle(start, end, counterClockwise);
        }
        return pointToCircle(start, endPoint, counterClockwise);
    }
    else if (isPlateCircle(end))
    {
        return pointToCircle(startPoint, end, counterClockwise);
    }
    return [startPoint, endPoint];
}

function platePointManipulator(platePlane is Plane, connectingPointsArray is array) returns map
{
    const pointArray = mapArray(connectingPointsArray, function(connectingPoints is array)
        {
            return planeToWorld(platePlane, (connectingPoints[0] + connectingPoints[1]) / 2);
        });

    return { (POINT_MANIPULATOR) :
            pointsManipulator({
                    "points" : pointArray,
                    "index" : size(pointArray) - 1
                })
        };
}

function addPlateDebugEdges(context is Context, platePlane is Plane, connectingPointsArray is array)
{
    for (var i, connectingPoints in connectingPointsArray)
    {
        if (!tolerantEquals(connectingPoints[0], connectingPoints[1]))
        {
            const debugColor = (i == size(connectingPointsArray) - 1) ? DebugColor.GREEN : DebugColor.BLUE;
            addDebugLine(context, planeToWorld(platePlane, connectingPoints[0]), planeToWorld(platePlane, connectingPoints[1]), debugColor);
        }
    }
}

function getBoundary(plateGeometryArray is array) returns array
{
    var boundary = [];
    for (var plateGeometry in plateGeometryArray)
    {
        if (isPlatePoint(plateGeometry) || isPlateCircle(plateGeometry))
        {
            boundary = append(boundary, {
                            "type" : plateGeometry["type"] as BoundaryType,
                            "location" : plateGeometry.location,
                            "radius" : plateGeometry.radius,
                            "valid" : true
                        } as BoundaryElement);
        }
        else if (isPlateArc(plateGeometry))
        {
            boundary = concatenateArrays([
                        boundary,
                        mapArray(plateGeometry.vertices, function(location is Vector)
                        {
                            return {
                                        "type" : BoundaryType.POINT,
                                        "location" : location,
                                        "valid" : false
                                    } as BoundaryElement;
                        })
                    ]);
        }
    }
    return boundary;
}
