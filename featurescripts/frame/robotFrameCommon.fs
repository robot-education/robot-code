FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

export import(path : "onshape/std/frameAttributes.fs", version : "2960.0");
export import(path : "onshape/std/frameUtils.fs", version : "2960.0");

import(path : "a816414b5bd99693e25e303c", version : "021db05cf4ac12aff11e01b6");
import(path : "eb11a2948f8123134339137f", version : "aa3b93f58a282fb8286a97ca");
import(path : "aa47f3d3eb754118903deeec", version : "0cf078513442fad9ec35fd7b");

export const ROBOT_FRAME_ATTRIBUTE = "robotFrame";
const ROBOT_FRAME_FACE_ATTRIBUTE = "robotFrameFace";

export function setRobotFrameAttribute(context is Context, frame is Query, definition is map, tubeDefinition is map, finished is boolean)
{
    const frameLine = getFrameLine(context, frame);
    const faces = getFrameFaces(frame, getFramePlane(frameLine, definition), tubeDefinition.flipStart);

    setAttribute(context, {
                "entities" : faces.firstFace,
                "name" : ROBOT_FRAME_FACE_ATTRIBUTE,
                "attribute" : { "tubeFace" : TubeFace.FIRST }
            });
    setAttribute(context, {
                "entities" : faces.secondFace,
                "name" : ROBOT_FRAME_FACE_ATTRIBUTE,
                "attribute" : { "tubeFace" : TubeFace.SECOND }
            });
}

export function getRobotFrameAttribute(context is Context, frame is Query) returns map
{
    return {}; //getRobotAttribute(context, frame, ROBOT_FRAME_ATTRIBUTE);
}

export function qRobotFrames(queryToFilter is Query)
{
    return qHasAttribute(queryToFilter, ROBOT_FRAME_ATTRIBUTE);
}


function qRobotFrameFace(frame is Query, tubeFace is TubeFace) returns Query
{
    return qHasAttributeWithValueMatching(qFrameSweptFace(frame), ROBOT_FRAME_FACE_ATTRIBUTE, { "tubeFace" : tubeFace });
}

export function qFirstFace(frame is Query) returns Query
{
    return qRobotFrameFace(frame, TubeFace.FIRST);
}

export function qSecondFace(frame is Query) returns Query
{
    return qRobotFrameFace(frame, TubeFace.SECOND);
}

export const opRobotFrameHoles = function(context is Context, id is Id, definition is map)
    precondition
    {
        definition.body is Query;
    }
    {
        const frame = definition.body;

        const tubeDefinition = getRobotFrameAttribute(context, frame).tubeDefinition;
        const frameDefinition = getRobotFrameDefinition(context, frame, tubeDefinition.flipStart);

        for (var j, tubeFace in values(TubeFace))
        {
            const tubeFaceDefinition = tubeDefinition[tubeFace];
            if (tubeFaceDefinition.patternCount <= 0)
            {
                continue;
            }

            const faceId = id + toString(j);
            const seedId = faceId + "seed";
            const baseCoordSystem = makeHoleSeed(context, seedId, tubeFaceDefinition, tubeFace, frameDefinition.endPlane);
            const seed = qCreatedBy(seedId, EntityType.BODY);

            const transformMaps = computeHoleTransforms(frame, baseCoordSystem, tubeFaceDefinition, frameDefinition.frameLength);

            // create all the holes in a given face at once
            try silent
            {
                const patternTools = applyHoleTransforms(context, faceId + "pattern", seed, transformMaps);
                for (var i, pattern in patternTools)
                {
                    opBoolean(context, faceId + ("boolean" ~ i), {
                                "tools" : pattern,
                                "targets" : frame,
                                "operationType" : BooleanOperationType.SUBTRACTION,
                                "targetsAndToolsNeedGrouping" : true
                            });

                    if (featureHasNonTrivialStatus(context, faceId + ("boolean" ~ i)))
                    {
                        throw regenError(ErrorStringEnum.NO_ERROR);
                    }
                }
            }
            catch
            {
                // need to reconstruct holes for error display since boolean likely consumed or partially consumed some holes in face pattern
                // we also try to highlight frame, but we can't reconstruct it since reconstruct info is lost during Robot finish
                const patternTools = applyHoleTransforms(context, id + "error", seed, transformMaps);

                if (!isQueryEmpty(context, frame))
                {
                    addDebugEntities(context, frame, DebugColor.BLUE);
                }
                throw regenError("Some hole patterns do not intersect the tube.", qUnion(patternTools));
            }

            cleanup(context, faceId + "deleteSeed", seed);
        }

        if (tubeDefinition.isMaxTube && tubeDefinition.maxTubePatternType == MaxTubePatternType.MAX)
        {
            makeMaxPattern(context, id + "maxPattern", frame, frameDefinition);
        }
    };

function computeHoleTransforms(frame is Query, baseCoordSystem is CoordSystem, tubeFaceDefinition is map, frameLength is ValueWithUnits) returns array
{
    const patternCount = tubeFaceDefinition.patternCount;
    const patternSpacing = tubeFaceDefinition.patternSpacing;
    const holeDepth = tubeFaceDefinition.holeDepth;

    var edgeDistanceBox = new box(0 * meter);
    const holeCount = getHoleCount(frameLength, tubeFaceDefinition);
    if (holeCount <= 0)
    {
        // highlight most likely suspects for errors
        throw regenError("Selections failed to produce any holes.", ["startDistance", "endDistance", "distance", "selections"], frame);
    }

    return mapArrayIndices(makeArray(patternCount), function(i)
        {
            const holeTransforms = getHoleTransforms(baseCoordSystem, holeCount, frameLength, edgeDistanceBox[], tubeFaceDefinition.startOffset, tubeFaceDefinition.distance);
            const holeNames = mapArrayIndices(holeTransforms, function(j)
                {
                    return i ~ "_" ~ j;
                });
            edgeDistanceBox[] += tubeFaceDefinition.patternSpacing;
            return {
                    "transforms" : holeTransforms,
                    "instanceNames" : holeNames
                };
        });
}

function makeHoleSeed(context is Context, id is Id, tubeFaceDefinition is map, tubeFace is TubeFace, endPlane is Plane) returns CoordSystem
{
    const holeDirection = (tubeFace == TubeFace.FIRST ? endPlane.x : yAxis(endPlane));
    const oppositeDirection = (tubeFace != TubeFace.FIRST ? endPlane.x : yAxis(endPlane));

    const totalWidth = tubeFaceDefinition.patternSpacing * (tubeFaceDefinition.patternCount - 1);
    const edgeDistance = (tubeFaceDefinition.width - totalWidth) / 2;
    const seedOrigin = endPlane.origin + oppositeDirection * edgeDistance;
    fCylinder(context, id, {
                "topCenter" : seedOrigin,
                "bottomCenter" : seedOrigin + holeDirection * tubeFaceDefinition.holeDepth,
                "radius" : tubeFaceDefinition.holeDiameter / 2
            });
    return coordSystem(zeroVector(3) * meter, endPlane.normal, oppositeDirection);
}

function getHoleCount(frameLength is ValueWithUnits, tubeFaceDefinition is map) returns number
{
    // the available length is frameLength - start and end offsets
    return floor((frameLength - tubeFaceDefinition.startOffset - tubeFaceDefinition.endOffset) / tubeFaceDefinition.distance);
}

function getHoleTransforms(
    baseCoordSystem is CoordSystem,
    holeCount is number,
    frameLength is ValueWithUnits,
    edgeDistance is ValueWithUnits,
    startOffset is ValueWithUnits,
    distance is ValueWithUnits) returns array
{
    // round up to do partial holes
    return mapArray(range(0, holeCount), function(i)
        {
            return transform(baseCoordSystem.xAxis * (startOffset + (i * distance)) + baseCoordSystem.zAxis * edgeDistance);
        });
}

function applyHoleTransforms(context is Context, id is Id, seed is Query, transformMaps is array) returns array
{
    return mapArrayIndices(transformMaps, function(i)
        {
            const patternId = id + toString(i);
            opPattern(context, patternId, mergeMaps({
                            "entities" : seed->qBodyType(BodyType.SOLID),
                            "copyPropertiesAndAttributes" : false
                        }, transformMaps[i]));
            return qCreatedBy(patternId, EntityType.BODY);
        });
}

function makeMaxPattern(context is Context, id is Id, frame is Query, frameDefinition is map)
{
    const tubeFaceDefinition = MAX_PATTERN;
    const baseCoordSystem = makeMaxSeed(context, id + "seed", tubeFaceDefinition, frameDefinition.endPlane);
    const seed = qCreatedBy(id + "seed", EntityType.BODY);
    const transformMaps = computeHoleTransforms(frame, baseCoordSystem, tubeFaceDefinition, frameDefinition.frameLength);
    applyHoleTransforms(context, id + "transform", seed, transformMaps);
    opBoolean(context, id + "boolean", {
                "tools" : qCreatedBy(id + "transform", EntityType.BODY),
                "targets" : frame,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    cleanup(context, id + "delete", seed);
}

function makeMaxSeed(context is Context, id is Id, tubeFaceDefinition is map, endPlane is Plane) returns CoordSystem
{
    const holeDirection = endPlane.x;
    const oppositeDirection = yAxis(endPlane);

    const totalWidth = tubeFaceDefinition.patternSpacing * (tubeFaceDefinition.patternCount - 1);
    const edgeDistance = (tubeFaceDefinition.width - totalWidth) / 2;

    const seedOrigin = endPlane.origin + oppositeDirection * edgeDistance;
    createSketchDataArray(context, id + "sketch", {
                "plane" : plane(seedOrigin, holeDirection, oppositeDirection),
                "sketchDataArray" : MAX_SPLINE_HOLE
            });
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : holeDirection,
                "endBound" : BoundingType.BLIND,
                "endDepth" : tubeFaceDefinition.holeDepth
            });
    return coordSystem(zeroVector(3) * meter, endPlane.normal, oppositeDirection);
}

export function getRobotFrameDefinition(context is Context, frame is Query, flipStart is boolean) returns map
{
    const frameDistance = measureFrameDistance(context, frame);
    const firstFacePlane = evPlane(context, { "face" : qFirstFace(frame) });
    const secondFacePlane = evPlane(context, { "face" : qSecondFace(frame) });
    return {
            "frameLength" : frameDistance.customOffset,
            "endPlane" : getEndPlane(firstFacePlane, secondFacePlane, frameDistance, flipStart)
        };
}

function getEndPlane(firstFacePlane is Plane, secondFacePlane is Plane, frameDistance is map, flipStart is boolean) returns Plane
{
    var intersectionLine = intersection(firstFacePlane, secondFacePlane);
    intersectionLine.direction *= flipStart ? -1 : 1;
    const startPoint = project(intersectionLine, flipStart ? frameDistance.secondPoint : frameDistance.firstPoint);
    return plane(startPoint, intersectionLine.direction * (flipStart ? -1 : 1), -firstFacePlane.normal);
}

function measureFrameDistance(context is Context, frame is Query) returns map
{
    return measureDistance(context, {
                "entities" : qUnion(qFrameStartFace(frame)->qNthElement(0), qFrameEndFace(frame)->qNthElement(0)),
                "customDirection" : qFrameSweptEdge(frame)->qNthElement(0),
                "maximum" : true
            });
}

/**
 * Returns the two faces on the outside of the frame which are at right angles to each other.
 * Specifically, the faces are those which are the most neagtive x and y-ward.
 * The first face is the most negative face parallel to the Y-axis.
 * The second face is the most negative face which is parallel to the X-axis.
 */
function getFrameFaces(frame is Query, framePlane is Plane, flipStart is boolean) returns map
{
    const firstFacePlane = plane(framePlane.origin, framePlane.x, yAxis(framePlane));
    const secondFacePlane = plane(framePlane.origin, yAxis(framePlane), -framePlane.x);
    const firstFace = qParallelPlanes(qOwnedByBody(frame, EntityType.FACE), firstFacePlane)->qFarthestAlong(-firstFacePlane.normal * (flipStart ? -1 : 1));
    const secondFace = qParallelPlanes(qOwnedByBody(frame, EntityType.FACE), secondFacePlane)->qFarthestAlong(-secondFacePlane.normal);
    return {
            "firstFace" : firstFace,
            "secondFace" : secondFace
        };
}

export function getFramePlane(frameLine is Line, definition is map) returns Plane
{
    var framePlane = getPlaneAtLineStart(frameLine);
    const baseAngle = definition.angle * (definition.mirrorProfile ? -1 : 1);
    return rotationAround(frameLine, baseAngle) * framePlane;
}

/**
 * Returns a line pointing in the direction of the frame towards the frame end face.
 */
export function getFrameLine(context is Context, frame is Query) returns Line
{
    const edgeLine = getEdgeLine(context, frame);
    return flipFrameDirection(context, frame, edgeLine);
}

function flipFrameDirection(context is Context, frame is Query, line is Line) returns Line
{
    line.origin += line.direction * (TOLERANCE.zeroLength * 100 * meter);
    const result = evRaycast(context, {
                "entities" : qFrameEndFace(frame),
                "ray" : line,
                "closest" : true
            });
    line.direction *= (size(result) == 0 ? -1 : 1);
    return line;
}

function getEdgeLine(context is Context, body is Query) returns Line
{
    const sweptEdges = qFrameSweptEdge(body);
    if (!isQueryEmpty(context, sweptEdges))
    {
        const lineEdges = qGeometry(sweptEdges, GeometryType.LINE);
        if (!isQueryEmpty(context, lineEdges))
        {
            return evLine(context, {
                        "edge" : qNthElement(lineEdges, 0)
                    });
        }
    }

    try silent
    {
        const sweptFace = qFrameSweptFace(body);
        const cylinderFaces = qGeometry(qNthElement(sweptFace, 0), GeometryType.CYLINDER);
        return evAxis(context, {
                    "axis" : qNthElement(cylinderFaces, 0)
                });
    }

    throw regenError(ErrorStringEnum.CAP_FRAME_AXIS_ERROR);
}

function getPlaneAtLineStart(edgeLine is Line)
{
    const xDir = getXDirFromHeuristic(edgeLine);
    return plane(edgeLine.origin, edgeLine.direction, cross(xDir, edgeLine.direction));
}

function getXDirFromHeuristic(edgeLine is Line) returns Vector
{
    return (abs(edgeLine.direction[2]) > TOLERANCE.computational) ? Y_DIRECTION : Z_DIRECTION;
}
