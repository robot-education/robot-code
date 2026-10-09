/**
 * Lightening Generator FeatureScript
 * Created By Evan Fish, Designer, FRC Team 2471
 * Find Me On ChiefDelphi @Mr_Fishy
 */

FeatureScript 1560;
import(path : "onshape/std/geometry.fs", version : "1560.0");

IconNameSpace::import(path : "cc6d1e51ee383de83e588f69", version : "3573118415c80e65a3da4fef");

const WALL_BOUNDS = { (inch) : [0.001, 0.25, 300] } as LengthBoundSpec;
const TOOL_BOUNDS = { (inch) : [0.001, 0.125, 300] } as LengthBoundSpec;
const RIB_BOUNDS = { (inch) : [0.001, 0.2, 300] } as LengthBoundSpec;
const FILLET_BOUNDS = { (inch) : [0.001, 0.15, 300] } as LengthBoundSpec;
const DEPTH_BOUNDS = { (inch) : [0.001, 0.15, 300] } as LengthBoundSpec;

export enum LightenBoundingType
{
    annotation { "Name" : "Blind" }
    BLIND,
    annotation { "Name" : "Through" }
    THROUGH,
    annotation { "Name" : "Up to face" }
    UP_TO_SURFACE,
    annotation { "Name" : "Up to vertex" }
    UP_TO_VERTEX
}

export enum lightenType
{
    // annotation { "Name" : "Automatic" }
    // AUTO,
    annotation { "Name" : "Manual" }
    MANUAL,
}

export predicate isManual(definition is map)
{
    definition.lighten == lightenType.MANUAL;
}

annotation {
        "Feature Type Name" : "Part Lighten",
        "Feature Name Template" : "Part Lightening - #ribs Ribs",
        "Icon" : IconNameSpace::BLOB_DATA,
        "Feature Type Description" : "Lightens part given selections of lines to create ribs along, automatically checks most cases to solve and adjust for a machineable part.",
    // "Description Image" : DescriptionNameSpace::BLOB_DATA
    }
export const lighteningGen = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Lightening type", "UIHint" : UIHint.HORIZONTAL_ENUM, "Default" : lightenType.MANUAL }
        definition.lighten is lightenType;

        annotation { "Name" : "Face to lighten", "Filter" : EntityType.FACE && GeometryType.PLANE && SketchObject.NO, "MaxNumberOfPicks" : 1 }
        definition.face is Query;

        annotation { "Name" : "Exclude face regions" }
        definition.exclude is boolean;

        if (definition.exclude)
        {
            annotation { "Name" : "Exlude regions", "Filter" : EntityType.FACE && GeometryType.PLANE }
            definition.excludeRegions is Query;
        }

        if (isManual(definition))
        {
            annotation { "Name" : "Rib lines", "Filter" : EntityType.EDGE && SketchObject.YES }
            definition.locations is Query;
        }

        annotation { "Name" : "Thickness of walls", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isLength(definition.thickness, WALL_BOUNDS);

        annotation { "Name" : "Thickness of ribs", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isLength(definition.ribThickness, WALL_BOUNDS);

        annotation { "Name" : "Overide fillet radius", "Default" : false }
        definition.overide is boolean;

        if (definition.overide)
        {
            annotation { "Name" : "Fillet radius" }
            isLength(definition.radius, FILLET_BOUNDS);

        }
        else
        {
            annotation { "Name" : "Tool diameter", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.diameter, TOOL_BOUNDS);
        }

        annotation { "Name" : "Lighten multiple bodies" }
        definition.multiple is boolean;

        if (definition.multiple)
        {
            annotation { "Name" : "Parts to lighten", "Filter" : BodyType.SOLID && EntityType.BODY }
            definition.entities is Query;
        }

        annotation { "Name" : "End type", "UIHint" : "REMEMBER_PREVIOUS_VALUE" }
        definition.endBound is LightenBoundingType;

        if (definition.endBound == LightenBoundingType.BLIND)
        {
            annotation { "Name" : "Cut depth", "UIHint" : "REMEMBER_PREVIOUS_VALUE" }
            isLength(definition.depth, NONNEGATIVE_LENGTH_BOUNDS);
        }
        else if (definition.endBound == LightenBoundingType.UP_TO_SURFACE)
        {
            annotation { "Name" : "Up to face", "Filter" : EntityType.FACE && GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityFace is Query;
        }
        else if (definition.endBound == LightenBoundingType.UP_TO_VERTEX)
        {
            annotation { "Name" : "Up to vertex", "Filter" : EntityType.VERTEX, "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityVertex is Query;
        }
    }
    {
        definition.entity = qOwnerBody(definition.face);
        if (definition.multiple)
        {
            definition.entity = definition.entities;
        }
        var startVolume = evVolume(context, { "entities" : definition.entity });
        // ----- Rounds Up Tool Radius For Better Machining ----- //
        var toolRad = definition.radius;
        if (!definition.overide)
        {
            toolRad = round((definition.diameter / 2) + (0.01 * inch), 0.001 * inch);
        }
        // ----- Gets Plane Of Selected Face ----- //
        const sPlane = evPlane(context, { "face" : definition.face });
        // ----- Check if Up_To_Surface Plane is Parallel for Distance Measurement ----- //
        if (isQueryEmpty(context, qParallelPlanes(definition.endBoundEntityFace, sPlane)) && definition.endBound == LightenBoundingType.UP_TO_SURFACE)
        {
            throw regenError("Face is not parallel to part.", ["endBoundEntityFace"], definition.endBoundEntityFace);
        }
        // ----- Distance Calculations for Depth Work ----- //
        var distance = evDistance(context, { "side0" : definition.face, "extendSide0" : true, "side1" : qParallelPlanes(qOwnedByBody(qEverything(EntityType.FACE), definition.entity), sPlane)->qSubtraction(definition.face)->qLargest(), "extendSide1" : true }).distance;
        if (definition.endBound == LightenBoundingType.BLIND)
        {
            distance = definition.depth;
        }
        else if (definition.endBound == LightenBoundingType.THROUGH)
        {
            const farthest = qFarthestAlong(qOwnedByBody(definition.entity, EntityType.EDGE), -sPlane.normal);
            
            distance = evDistance(context, { "side0" : definition.face, "side1" : farthest, "maximum" : true }).distance;
        }
        else if (definition.endBound == LightenBoundingType.UP_TO_SURFACE || definition.endBound == LightenBoundingType.UP_TO_VERTEX)
        {
            var endBound = definition.endBoundEntityFace;
            
            if (definition.endBound == LightenBoundingType.UP_TO_VERTEX)
            {
                endBound = definition.endBoundEntityVertex;
            }
            
            distance = evDistance(context, { "side0" : definition.face, "extendSide0" : true, "side1" : endBound, "extendSide1" : true }).distance;
        }
        var part = baseOperations(context, id, definition.entity, sPlane, toolRad, definition, distance);
        if (isManual(definition))
        {
            setFeatureComputedParameter(context, id, {
                        "name" : "ribs",
                        "value" : size(evaluateQuery(context, definition.locations))
                    });
        }
        try silent
        {
            if (size(evaluateQuery(context, part)) != 1 && !definition.multiple)
            {
                reportFeatureWarning(context, id, "Lightening results in more than one part, try adding more ribs or cut a partial depth.");
            }
            var endVolume = evVolume(context, { "entities" : part });
            if (!featureHasNonTrivialStatus(context, id))
            {
                reportFeatureInfo(context, id, "Weight of parts reduced by " ~ roundToPrecision((1 - (endVolume / startVolume)) * 100, 1) ~ "%.");
            }
        }
    });

/**
 * Takes the part, selection face, toolRad, definition, and distance.
 *
 * Returns resulting part.
 */
function baseOperations(context is Context, id is Id, part, face, toolRad, definition, distance)
{
    var toDelete = new box([]);

    const partOffset = definition.thickness + toolRad;

    try
    {
        opExtrude(context, id + "secondExtrude", {
                    "entities" : definition.face,
                    "direction" : -face.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : distance + partOffset,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : partOffset
                });
        if (definition.exclude)
        {
            opExtrude(context, id + "excludeExtrude", {
                        "entities" : definition.excludeRegions,
                        "direction" : -face.normal,
                        "endBound" : BoundingType.BLIND,
                        "endDepth" : distance + partOffset,
                        "startBound" : BoundingType.BLIND,
                        "startDepth" : partOffset
                    });

            opBoolean(context, id + "excludeBoolean", {
                        "tools" : qCreatedBy(id + "excludeExtrude", EntityType.BODY),
                        "targets" : qCreatedBy(id + "secondExtrude", EntityType.BODY),
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
    }
    catch
    {
        throw regenError("Failed to extrude and offset pre-shelled bodies.");
    }

    var capEntities = qUnion([qCapEntity(id + "secondExtrude", CapType.EITHER), startTracking(context, qCapEntity(id + "secondExtrude", CapType.EITHER))]);

    capEntities = qUnion([capEntities, startTracking(context, qCapEntity(id + "secondExtrude", CapType.EITHER, EntityType.FACE))]);

    try
    {
        shellAndEncloseWithConcaveFillet(context, id + "secondShell", {
                    "entities" : qCreatedBy(id + "secondExtrude", EntityType.BODY),
                    "offset" : partOffset
                }, toDelete);
    }
    catch
    {
        throw regenError("Failed to shell and offset bodies.");
    }

    try silent
    {
        var ribs;

        // if (definition.lighten == lightenType.MANUAL)
        // {
        ribs = manualRibs(context, id, face, definition, distance, definition.ribThickness + (toolRad * 2), partOffset);
        // }
        // else
        // {
        //     ribs = autoRibs(context, id, face, definition, distance);
        // }

        opBoolean(context, id + "ribBoolean", {
                    "tools" : ribs,
                    "targets" : qCreatedBy(id + "secondShell", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }

    try
    {
        // Expand the body by toolRadius
        expandWithFillet(context, id + "expandCombined", {
                    "entities" : qCreatedBy(id + "secondShell", EntityType.BODY),
                    "noEditFaces" : qUnion([capEntities, startTracking(context, capEntities)]),
                    "offset" : toolRad
                });

        // For sheet metal capability, use booleanBodies
        booleanBodies(context, id + "booleanFromPart", {
                    "tools" : qCreatedBy(id + "secondShell", EntityType.BODY),
                    "targets" : part,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        // Checks for bridges between bodies that do not follow rib requirements
    }
    catch
    {
        throw regenError("Failed to finalize and boolean resulting bodies.");
    }

    try silent
    {
        // Detects for partial connections between walls
        const toFixEdge = qAdjacent(qUnion(sharpEdgeCheck(context, qCreatedBy(id + "booleanFromPart", EntityType.EDGE)->qParallelEdges(face.normal))), AdjacencyType.EDGE, EntityType.FACE)->qSubtraction(qParallelPlanes(qCreatedBy(id + "booleanFromPart", EntityType.FACE), face));

        if (!isQueryEmpty(context, toFixEdge))
        {
            // debug(context, toFix, DebugColor.RED);
            try silent
            {
                opDeleteFace(context, id + "fixFaces", {
                            "deleteFaces" : toFixEdge,
                            "includeFillet" : false,
                            "capVoid" : false,
                            "leaveOpen" : false
                        });
            }
        }
    }

    opDeleteBodies(context, id + "delete", { "entities" : qUnion(toDelete[]) });

    return part;
}

/**
 * Takes parts created by baseOffset and automatically generates ribs between parts.
 */
// function autoRibs(context is Context, id is Id, plane, definition, distance)
// {
//
// }

/**
 * Takes parts created by baseOffset and manually generates ribs between parts based on user selections.
 */
function manualRibs(context is Context, id is Id, plane, definition, distance, thickness, extDist)
{
    const lines = evaluateQuery(context, definition.locations);
    const length = thickness / 2;
    var toExtrude is array = [];
    var toDelete = new box([]);

    for (var i, rib in lines)
    {
        var sketch = newSketchOnPlane(context, id + (i ~ "ribSketch"), { "sketchPlane" : plane });
        if (isQueryEmpty(context, qGeometry(rib, GeometryType.ARC)))
        {
            const tangentLines = evEdgeTangentLines(context, { "edge" : rib, "parameters" : [0, 1] });
            const startPoint2D = worldToPlane(plane, tangentLines[0].origin);
            const endPoint2D = worldToPlane(plane, tangentLines[1].origin);
            const longDir = normalize(endPoint2D - startPoint2D);
            const thickDir = vector(-longDir[1], longDir[0]);
            var corners = [startPoint2D, startPoint2D, endPoint2D, endPoint2D];
            corners[0] += thickDir * length;
            corners[1] += -thickDir * length;
            corners[2] += -thickDir * length;
            corners[3] += thickDir * length;
            corners = append(corners, corners[0]); // Close the polyline
            skPolyline(sketch, i ~ "rectangle", { "points" : corners });
        }
        else if (!isQueryEmpty(context, qGeometry(rib, GeometryType.ARC)))
        {
            const tangentArc = evEdgeTangentLines(context, { "edge" : rib, "parameters" : [0, 0.5, 1] });
            const arcOrigin = worldToPlane(plane, evCurveDefinition(context, { "edge" : rib }).coordSystem.origin);
            const startPoint2D = worldToPlane(plane, tangentArc[0].origin);
            const startVec = vector((arcOrigin[0] - startPoint2D[0]) / sqrt((arcOrigin[0] - startPoint2D[0]) ^ 2 + (arcOrigin[1] - startPoint2D[1]) ^ 2), (arcOrigin[1] - startPoint2D[1]) / sqrt((arcOrigin[0] - startPoint2D[0]) ^ 2 + (arcOrigin[1] - startPoint2D[1]) ^ 2));
            const midPoint2D = worldToPlane(plane, tangentArc[1].origin);
            const midVec = vector((arcOrigin[0] - midPoint2D[0]) / sqrt((arcOrigin[0] - midPoint2D[0]) ^ 2 + (arcOrigin[1] - midPoint2D[1]) ^ 2), (arcOrigin[1] - midPoint2D[1]) / sqrt((arcOrigin[0] - midPoint2D[0]) ^ 2 + (arcOrigin[1] - midPoint2D[1]) ^ 2));
            const endPoint2D = worldToPlane(plane, tangentArc[2].origin);
            const endVec = vector((arcOrigin[0] - endPoint2D[0]) / sqrt((arcOrigin[0] - endPoint2D[0]) ^ 2 + (arcOrigin[1] - endPoint2D[1]) ^ 2), (arcOrigin[1] - endPoint2D[1]) / sqrt((arcOrigin[0] - endPoint2D[0]) ^ 2 + (arcOrigin[1] - endPoint2D[1]) ^ 2));
            const startDir = normalize(endPoint2D - startPoint2D);
            var thickDir = startVec;
            var points = [startPoint2D, startPoint2D, endPoint2D, endPoint2D, midPoint2D, midPoint2D];
            points[0] += thickDir * length;
            points[1] += -thickDir * length;
            thickDir = endVec;
            points[2] += -thickDir * length;
            points[3] += thickDir * length;
            thickDir = midVec;
            points[4] += -thickDir * length;
            points[5] += thickDir * length;
            skLineSegment(sketch, "line1", { "start" : points[0], "end" : points[1] });
            skLineSegment(sketch, "line2", { "start" : points[2], "end" : points[3] });
            skArc(sketch, "arc1", { "start" : points[0], "mid" : points[5], "end" : points[3] });
            skArc(sketch, "arc2", { "start" : points[1], "mid" : points[4], "end" : points[2] });
        }
        skSolve(sketch);
        toExtrude = append(toExtrude, qSketchRegion(id + (i ~ "ribSketch")));
        toDelete[] = append(toDelete[], qCreatedBy(id + (i ~ "ribSketch")));
    }

    opExtrude(context, id + "ribExtrude", {
                "entities" : qUnion(toExtrude),
                "direction" : -plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : distance + extDist,
                "startBound" : BoundingType.BLIND,
                "startDepth" : extDist
            });

    opDeleteBodies(context, id + "deleteRibSketchs", { "entities" : qUnion(toDelete[]) });

    return qCreatedBy(id + "ribExtrude", EntityType.BODY);
}

/**
 * Sourced from the Featurescript Lighten by Ilya Baran and Morgan Bartlett
 */
function shellAndEncloseWithConcaveFillet(context is Context, id is Id, definition is map, toDelete is box)
{
    // Fillet concave edges in the parts
    var edgesToFillet = [];
    var edges = evaluateQuery(context, qOwnedByBody(definition.entities, EntityType.EDGE));
    for (var edge in edges)
    {
        if (evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE)
            edgesToFillet = append(edgesToFillet, edge);
    }
    if (edgesToFillet != [])
    {
        opFillet(context, id + "fillet", {
                    "entities" : qUnion(edgesToFillet),
                    "radius" : TOLERANCE.booleanDefaultTolerance * meter
                });
    }
    var filletsToEdit = startTracking(context, qCreatedBy(id + "fillet", EntityType.FACE));
    toDelete[] = append(toDelete[], definition.entities);
    // Shell the parts
    opShell(context, id + "shell", {
                "entities" : definition.entities,
                "thickness" : -definition.offset
            });
    // Enclose the parts, one at a time to avoid a boolean
    for (var i, toEnclose in evaluateQuery(context, definition.entities))
    {
        opEnclose(context, id + "enclose" + unstableIdComponent(i), { "entities" : toEnclose });
    }
    // Modify the fillet to the correct radius
    if (edgesToFillet != [])
    {
        try
        {
            opModifyFillet(context, id + "modifyFillet", {
                        "faces" : qIntersection([filletsToEdit, qCreatedBy(id + "enclose")]),
                        "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                        "radius" : definition.offset
                    });
        }
    }
}

/**
 * Sourced from the Featurescript Lighten by Ilya Baran and Morgan Bartlett
 */
function expandWithFillet(context is Context, id is Id, definition is map)
{
    // Fillet convex edges in the parts
    var edgesToFillet = sharpEdgeCheck(context, qSubtraction(qOwnedByBody(definition.entities, EntityType.EDGE), qAdjacent(definition.noEditFaces, AdjacencyType.EDGE, EntityType.EDGE)));
    if (edgesToFillet != [])
    {
        opFillet(context, id + "fillet", {
                    "entities" : qUnion(edgesToFillet),
                    "radius" : TOLERANCE.booleanDefaultTolerance * meter
                });
    }
    var filletsToEdit = qCreatedBy(id + "fillet", EntityType.FACE);
    // Move the part faces outwards by offset
    opOffsetFace(context, id + "offsetFace", {
                "moveFaces" : qSubtraction(qOwnedByBody(definition.entities, EntityType.FACE), definition.noEditFaces),
                "offsetDistance" : definition.offset
            });
    // Modify the fillet to the correct radius
    if (edgesToFillet != [])
    {
        opModifyFillet(context, id + "modifyFillet", {
                    "faces" : filletsToEdit,
                    "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                    "radius" : definition.offset
                });
    }
}

/**
 * Takes edges and checks for sharp edges.
 *
 * Returns array of sharp edges.
 */
export function sharpEdgeCheck(context is Context, edges is Query) returns array
{
    var arr = [];
    for (var edge in evaluateQuery(context, edges))
    {
        const convexity = evEdgeConvexity(context, { "edge" : edge });
        if (convexity != EdgeConvexityType.SMOOTH)
        {
            arr = append(arr, edge);
        }
    }
    return arr;
}

export function projectToPlane(point is Vector, plane is Plane) returns Vector
{
    return worldToPlane(plane, project(plane, point));
}

export function projectToPlane(plane is Plane, point is Vector) returns Vector
{
    return projectToPlane(point, plane);
}

export function checkGeomType(context is Context, face is Query) returns boolean
{
    if (!isQueryEmpty(context, qGeometry(face, GeometryType.PLANE)))
    {
        return true;
    }
    else if (!isQueryEmpty(context, qGeometry(face, GeometryType.CYLINDER)))
    {
        return false;
    }
}
