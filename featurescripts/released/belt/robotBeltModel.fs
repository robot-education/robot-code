FeatureScript 2960;
/**
 * Modeling a belt's body.
 */
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

import(path : "00b10ef1fb1a7418097fc0af", version : "3ba879cf97235b1a292f0dbc");
import(path : "4d2d3f0157d54e1b6a06420a", version : "b17a9f4837591274d709d92b");

/**
 * Sketches and extrudes the belt. Returns a query for the created belt as well as the belt start face.
 *
 * @returns {{
 *      @field startFace : A query for one of the sides of the belt.
 *      @field belt : A query for the created belt.
 * }}
 */
export function extrudeBelt(context is Context, id is Id, beltModel is BeltModel, beltPlane is Plane, beltLoop is Query, counterClockwise is boolean) returns map
{
    const beltInfo = getBeltModelInfo(beltModel.beltType);

    try
    {
        // create the outer belt profile by thickening the surface of the belt profile
        const insideBeltThickness = getBeltModelInsideThickness(beltInfo, beltModel.modelBeltTeeth);
        const outsideBeltThickness = beltModel.isDoubleSidedBelt ? insideBeltThickness : beltInfo.outsideThickness;
        opOffsetWire(context, id + "beltSurface", {
                    "edges" : beltLoop,
                    "normal" : beltPlane.normal,
                    "offset1" : outsideBeltThickness,
                    "offset2" : insideBeltThickness,
                    "makeRegions" : true
                });
    }
    catch
    {
        throw regenError("Failed to extrude belt. Check input.", beltLoop);
    }

    var beltFaces = qCreatedBy(id + "beltSurface", EntityType.FACE);
    if (beltModel.modelBeltTeeth)
    {
        const toothFaces = sketchBeltTeeth(context, id + "beltTeeth", beltModel, beltPlane, beltLoop);
        beltFaces = qUnion(beltFaces, toothFaces);
    }

    try
    {
        opExtrude(context, id + "belt", {
                    "entities" : beltFaces,
                    "direction" : beltPlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : beltModel.beltWidth / 2,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : beltModel.beltWidth / 2,
                });
    }
    catch
    {
        throw regenError("Failed to extrude belt. Check input.", beltFaces);
    }

    const belt = qCreatedBy(id + "belt", EntityType.BODY);
    if (beltModel.modelBeltTeeth)
    {
        opBoolean(context, id + "booleanBelt", {
                    "tools" : qCreatedBy(id + "belt", EntityType.BODY),
                    "operationType" : BooleanOperationType.UNION
                });

        try
        {
            const toothEdges = qParallelEdges(qOwnedByBody(belt, EntityType.EDGE), beltPlane.normal);
            opFillet(context, id + "toothFillet", {
                        "entities" : toothEdges,
                        "radius" : beltInfo.toothFilletRadius
                    });
        }
        catch
        {
            throw regenError("Failed to fillet belt teeth. Check input.", belt);
        }
    }

    cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSubtraction(belt));
    return {
            "startFace" : qCapEntity(id + "belt", CapType.START, EntityType.FACE),
            "belt" : belt
        };
}

/**
 * Sketches the teeth belonging to a belt.
 * @return {Query} : A query for the faces of the sketched teeth.
 */
function sketchBeltTeeth(context is Context, id is Id, beltModel is BeltModel, beltPlane is Plane, beltLoop is Query) returns Query
{
    const beltInfo = getBeltModelInfo(beltModel.beltType);

    const path = constructPath(context, beltLoop);
    // Arbitrary start point
    const startLine = evPathTangentLines(context, path, [0]).tangentLines[0];
    // const startLine = evEdgeTangentLine(context, {
    //             "edge" : beltLoop,
    //             "parameter" : 0
    //         });
    const insideDirection = -cross(startLine.direction, beltPlane.normal);

    const toothSketch = newSketchOnPlane(context, id + "toothSketch", { "sketchPlane" : beltPlane });
    const startPoint = startLine.origin + insideDirection * beltInfo.toothOffset;
    skCircle(toothSketch, "tooth", {
                "center" : worldToPlane(beltPlane, startPoint),
                "radius" : beltInfo.toothRadius
            });

    if (beltModel.isDoubleSidedBelt)
    {
        const startPoint = startLine.origin - insideDirection * beltInfo.toothOffset;
        skCircle(toothSketch, "doubleSidedTooth", {
                    "center" : worldToPlane(beltPlane, startPoint),
                    "radius" : beltInfo.toothRadius
                });
    }
    skSolve(toothSketch);
    const seed = qSketchRegion(id + "toothSketch");
    const patternDefinition = getClosedPathPatternDefinition(context, path, seed, beltModel.beltTeeth);
    opPattern(context, id + "toothPattern", patternDefinition);
    return qUnion(seed, qCreatedBy(id + "toothPattern", EntityType.FACE));
}

// Adapted from Onshape STD
function getClosedPathPatternDefinition(context is Context, path is Path, entities is Query, instanceCount is number) returns map
{
    var transforms = [];
    var names = [];

    if (instanceCount > 1)
    {
        // If the path is open, the parameters are {0.0, 1 / (instanceCount - 1), ..., (instanceCount - 2) / (instanceCount - 1), 1.0}
        // If the path is closed, the parameters are {0.0, 1 / (instanceCount), ..., (instanceCount - 2) / (instanceCount), (instanceCount - 1) / (instanceCount)}
        var parameters = [];
        for (var i = 0; i < instanceCount; i += 1)
        {
            parameters = append(parameters, i / instanceCount);
        }

        // Get tangent planes or lines from computePatternTangents
        // var tangents = computePatternTangents(context, path, parameters, referenceEntities);
        var tangents = evPathTangentLines(context, path, parameters, entities).tangentLines;

        // transform(..., ...) works with planes or lines
        // transform 0 is left out since original tool is kept
        // was transform(tangents[1] - tangents[0])
        transforms = [transform(tangents[0], tangents[1])];
        names = ["1"];
        for (var i = 2; i < instanceCount; i += 1)
        {
            // transforms = append(transforms, transform(tangents[i - 1] - tangents[i]) * transforms[i - 2]);
            transforms = append(transforms, transform(tangents[i - 1], tangents[i]) * transforms[i - 2]);
            names = append(names, "" ~ i);
        }
    }
    return { "transforms" : transforms, "instanceNames" : names, "entities" : entities };
}
