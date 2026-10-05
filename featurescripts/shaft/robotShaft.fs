FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/cosmeticThreadUtils.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

export import(path : "603be214b7d30fd438966545", version : "bcecf1b877c65f7c39d56bc4");
import(path : "8fc3df84a88e74d27ad43d26", version : "b6f17a04daefbce8624703b9");


export import(path : "21762d39019c8b2289e2fbb8", version : "06bafd6cfddc3fbe92c47892");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");
import(path : "b75434df23d86ba9542f761e", version : "ba222d9a7c55b13cef9c1c62");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");
import(path : "0195d390c3944cd4fab21ce0", version : "eb719b1576c924e1ac6e1ffa");
import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");
import(path : "0794d10863d10d98a88c2ab4", version : "90bbee184f6552271649afea");

export enum EndOperation
{
    annotation { "Name" : "Tapped hole" }
    TAPPED_HOLE,
    annotation { "Name" : "Clearance hole" }
    CLEARANCE_HOLE,
    annotation { "Name" : "Retaining ring" }
    RETAINING_RING,
    annotation { "Name" : "Captive shaft" }
    CAPTIVE_SHAFT
}

export enum SecondEndOperation
{
    annotation { "Name" : "Tapped hole" }
    TAPPED_HOLE,
    annotation { "Name" : "Retaining ring" }
    RETAINING_RING,
    annotation { "Name" : "Captive shaft" }
    CAPTIVE_SHAFT
}

predicate isHoleOperation(endOperation is EndOperation)
{
    endOperation == EndOperation.TAPPED_HOLE || endOperation == EndOperation.CLEARANCE_HOLE;
}

predicate hexShaftPredicate(definition is map)
{
    annotation { "Name" : "Hex type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
    definition.hexType is HexType;

    if (definition.hexType != HexType.ULTRA_HEX)
    {
        annotation { "Name" : "Shaft size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
        definition.hexSize is HexSize;
    }
}

// predicate tappedHolePredicate(definition is map, endString is string)
// {
//     annotation { "Name" : "Hole table", "Lookup Table" : shaftHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
//     definition[endString ~ "HolePath"] is LookupTablePath;

//     annotation { "Name" : "Hole diameter", "Icon" : Icon.HOLE_DIAMETER }
//     isLength(definition[endString ~ "HoleDiameter"], HOLE_DIAMETER_BOUNDS);

//     annotation { "Name" : "Hole depth", "Icon" : Icon.HOLE_DEPTH }
//     isLength(definition[endString ~ "HoleDepth"], HOLE_DEPTH_BOUNDS);

//     annotation { "Name" : "Tapped depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE, "Icon" : Icon.HOLE_TAPPED_DEPTH }
//     isLength(definition[endString ~ "TappedDepth"], HOLE_DEPTH_BOUNDS);

//     if (!isThroughAllShaft(definition))
//     {
//         annotation { "Name" : "Tap clearance (number of thread pitch lengths)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE, "Icon" : Icon.HOLE_TAP_CLEARANCE }
//         isReal(definition[endString ~ "TapClearance"], HOLE_CLEARANCE_BOUNDS);
//     }
// }

/**
 * `true` if the user cannot specify the second end of the shaft (because the end is inherited from the first,
 * or because the second end is consumed by the mirror).
 */
predicate cannotSpecifySecondEnd(definition is map)
{
    definition.mirrorShaft ||
        (definition.modifyFirstEnd && (definition.firstEndOperation == EndOperation.CLEARANCE_HOLE || definition.symmetricEnds));
}

predicate shaftEndPredicate(definition is map)
{
    annotation { "Group Name" : "Shaft ends", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Modify first end" }
        definition.modifyFirstEnd is boolean;

        if (definition.modifyFirstEnd)
        {
            annotation { "Group Name" : "First end hardware", "Collapsed By Default" : false, "Driving Parameter" : "modifyFirstEnd" }
            {
                annotation { "Name" : "End operation", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.firstEndOperation is EndOperation;

                // annotation { "Name" : "Flip shaft ends", "UIHint" : ["OPPOSITE_DIRECTION"] }
                // definition.flipShaftEnds is boolean;

                if (definition.firstEndOperation == EndOperation.CLEARANCE_HOLE || definition.firstEndOperation == EndOperation.TAPPED_HOLE)
                {
                    if (definition.firstEndOperation == EndOperation.TAPPED_HOLE)
                    {
                        annotation { "Name" : "Hole table", "Lookup Table" : tappedHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.firstEndTappedHolePath is LookupTablePath;
                    }
                    else
                    {
                        annotation { "Name" : "Hole table", "Lookup Table" : clearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.firstEndClearanceHolePath is LookupTablePath;
                    }

                    annotation { "Name" : "Hole diameter", "Icon" : Icon.HOLE_DIAMETER }
                    isLength(definition.firstEndHoleDiameter, HOLE_DIAMETER_BOUNDS);

                    if (definition.firstEndOperation == EndOperation.TAPPED_HOLE)
                    {
                        annotation { "Name" : "Hole depth", "Icon" : Icon.HOLE_DEPTH }
                        isLength(definition.firstEndHoleDepth, HOLE_DEPTH_BOUNDS);
                    }
                }
                else if (definition.firstEndOperation == EndOperation.RETAINING_RING)
                {
                    annotation { "Name" : "Side mount ring", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to use a side-mountable retaining ring." }
                    definition.firstEndSideMount is boolean;
                }
                else if (definition.firstEndOperation == EndOperation.CAPTIVE_SHAFT)
                {
                    // REMEMBER_PREVIOUS_VALUE doesn't work due to the defaulting edit logic
                    annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.firstEndDiameter, NONNEGATIVE_LENGTH_BOUNDS);

                    annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.firstEndLength, NONNEGATIVE_LENGTH_BOUNDS);

                    annotation { "Name" : "Extend shaft", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to extend the shaft to accommodate hardware." }
                    definition.extendFirstEnd is boolean;
                }
            }

            // Mirroring makes the ends symmetric implicitly
            if (definition.firstEndOperation != EndOperation.CLEARANCE_HOLE && !definition.mirrorShaft)
            {
                annotation { "Name" : "Symmetric ends", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.symmetricEnds is boolean;
            }
        }

        if (!cannotSpecifySecondEnd(definition))
        {
            annotation { "Name" : "Modify second end" }
            definition.modifySecondEnd is boolean;

            if (definition.modifySecondEnd)
            {
                annotation { "Group Name" : "Second end hardware", "Collapsed By Default" : false, "Driving Parameter" : "modifySecondEnd" }
                {
                    annotation { "Name" : "End operation", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.secondEndOperation is SecondEndOperation;

                    if (definition.secondEndOperation == SecondEndOperation.TAPPED_HOLE)
                    {
                        annotation { "Name" : "Hole table", "Lookup Table" : tappedHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.secondEndTappedHolePath is LookupTablePath;

                        annotation { "Name" : "Hole diameter", "Icon" : Icon.HOLE_DIAMETER }
                        isLength(definition.secondEndHoleDiameter, HOLE_DIAMETER_BOUNDS);

                        annotation { "Name" : "Hole depth", "Icon" : Icon.HOLE_DEPTH }
                        isLength(definition.secondEndHoleDepth, HOLE_DEPTH_BOUNDS);
                    }
                    else if (definition.secondEndOperation == SecondEndOperation.RETAINING_RING)
                    {
                        annotation { "Name" : "Side mount ring", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to use a side-mountable retaining ring." }
                        definition.secondEndSideMount is boolean;
                    }
                    else if (definition.secondEndOperation == SecondEndOperation.CAPTIVE_SHAFT)
                    {
                        // REMEMBER_PREVIOUS_VALUE doesn't work due to the defaulting edit logic
                        annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.secondEndDiameter, NONNEGATIVE_LENGTH_BOUNDS);

                        annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.secondEndLength, NONNEGATIVE_LENGTH_BOUNDS);

                        annotation { "Name" : "Extend shaft", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to extend the shaft to accommodate hardware." }
                        definition.extendSecondEnd is boolean;
                    }
                }
            }
        }
    }
}


annotation { "Feature Type Name" : "Robot shaft",
        "Feature Type Description" : "Create common robot shafts." ~ CREDIT,
        "Manipulator Change Function" : "extrudeManipulatorChange",
        "Editing Logic Function" : "robotShaftEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotShaft = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        locationPredicate(definition, "shaft");

        annotation { "Group Name" : "Shaft", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Shaft type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.shaftType is ShaftType;

            if (definition.shaftType == ShaftType.HEX)
            {
                hexShaftPredicate(definition);
            }
            else
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.splineType is SplineType;
            }
        }

        annotation { "Group Name" : "Extrude", "Collapsed By Default" : false }
        {
            newExtrudePredicate(definition);

            annotation { "Name" : "Mirror shaft", "Description" : "Mirror the shaft across one of its ends. Useful when the shaft is modeled up to a plane of symmetry." }
            definition.mirrorShaft is boolean;

            if (definition.mirrorShaft)
            {
                annotation { "Name" : "Flip mirror end", "UIHint" : ["OPPOSITE_DIRECTION"] }
                definition.flipMirrorEnd is boolean;
            }
        }

        if (definition.shaftType == ShaftType.HEX)
        {
            shaftEndPredicate(definition);
        }
    }
    {
        const shaftPlane = getLocationPlane(context, definition);
        const shaftProfile = createShaftProfile(context, id + "profile", definition, shaftPlane);
        definition = transformDefintionForNewExtrude(definition, shaftProfile);
        // Use top level id for extrude so manipulators propagate
        // Still distinguish extrudeId from id for semantics though
        const extrudeId = id;
        callSubfeatureAndProcessStatus(id, extrude, context, extrudeId, definition, { "featureParameterMap" : { "entities" : "location" } });
        // Need makeRobustQuery here since otherwise shaft will always include any other solids produced by the feature
        const shaft = makeRobustQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID));

        // Capture the mirror plane before the shaft ends are modified
        var mirrorPlane = undefined;
        if (definition.mirrorShaft)
        {
            mirrorPlane = getMirrorPlane(context, definition, extrudeId, shaft);
        }

        if (definition.shaftType == ShaftType.HEX)
        {
            const endDefinitions = getShaftEndDefinitions(context, definition, extrudeId, shaft);
            cutHexShaftFeatures(context, id + "cutFeatures", definition, shaft, shaftPlane, endDefinitions);

            const reconstructOp = function()
                {
                    // Show shaft in blue on error
                    addDebugEntities(context, shaft, DebugColor.BLUE);
                };

            for (var i, endDefinition in endDefinitions)
            {
                verifyPerpendicularShaftEnd(context, endDefinition.endFace, shaftPlane.normal);
                const endId = id + "modifyShaftEnd" + unstableIdComponent(i);
                setExternalDisambiguation(context, endId, endDefinition.endFace);

                modifyShaftEnd(context, id, endId, definition, shaft, endDefinition, reconstructOp);
            }

            setAttribute(context, {
                        "entities" : shaft,
                        "name" : SHAFT_ATTRIBUTE,
                        "attribute" : extractFromMap(definition, ["shaftType", "hexType", "hexSize", "splineType"])
                    });

            // if (bothEndsTapPredrilledHole)
            // {
            //     const holeFace = qOpHoleFace(id);
            //     const endPlane = evPlane(context, { "face" : cosmeticEndDefinition.endFace });

            //     const holeEdges = qAdjacent(holeFace, AdjacencyType.EDGE, EntityType.EDGE);
            //     const distance = measureDistance(context, { "entities" : holeEdges }).distance;
            //     var middlePlane = endPlane;
            //     middlePlane.origin -= endPlane.normal * distance / 2;
            //     const toolId = id + "tool";
            //     const spline = opFitSpline(context, toolId + "line", {
            //                 "points" : [
            //                     middlePlane.origin + middlePlane.x * meter,
            //                     middlePlane.origin - middlePlane.x * meter,
            //                 ]
            //             });
            //     opExtrude(context, toolId + "extrude", {
            //                 "entities" : qCreatedBy(toolId + "line", EntityType.EDGE),
            //                 "direction" : yAxis(middlePlane),
            //                 "endBound" : BoundingType.BLIND,
            //                 "endDepth" : 1 * meter,
            //                 "startBound" : BoundingType.BLIND,
            //                 "startDepth" : 1 * meter
            //             });
            //     opSplitFace(context, id + "splitFace", {
            //                 "faceTargets" : holeFace,
            //                 "planeTools" : qCreatedBy(toolId + "extrude", EntityType.FACE)
            //             });
            //     cleanup(context, id + "delete", qCreatedBy(toolId, EntityType.BODY));

            //     const path = getTableAndPath(definition, cosmeticEndDefinition.shaftEnd).path;
            //     const pitch = computePitchValue(context, path.pitch);
            //     const cosmeticThreadData = createCosmeticThreadDataFromEntity(endPlane->flip()->coordSystem(), cosmeticEndDefinition.holeDepth.value, pitch.value);
            //     const endFace = qClosestTo(holeFace, endPlane.origin);
            //     addCosmeticThreadAttribute(context, endFace, cosmeticThreadData);
            // }
        }

        // Measure before mirroring, since the union consumes the cap face at the mirror plane
        var shaftLength = measureShaftLength(context, extrudeId);
        if (definition.mirrorShaft)
        {
            shaftLength = shaftLength * 2;
            mirrorShaftAcrossEnd(context, id + "mirrorShaft", shaft, mirrorPlane);
        }

        setShaftProperties(context, definition, shaft);
        setShaftName(context, shaft, definition, shaftLength);

        // Cleanup after boolean to avoid deleting profile faces to early
        cleanup(context, id + "deleteProfiles", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    });


function createShaftProfile(context is Context, id is Id, definition is map, shaftPlane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : shaftPlane });

    if (definition.shaftType == ShaftType.HEX)
    {
        const hexWidth = getHexWidth(definition)->getHexCircleRadius();
        skRegularPolygon(sketch, "hex", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(hexWidth, 0 * meter),
                    "sides" : 6
                });
    }
    else
    {
        skSplineProfile(sketch, "outside", {
                    "splineType" : definition.splineType,
                    "profileSide" : ProfileSide.OUTSIDE
                });
        skSplineProfile(sketch, "inside", {
                    "splineType" : definition.splineType,
                    "profileSide" : ProfileSide.INSIDE
                });
    }
    skSolve(sketch);

    return qSketchRegion(id, true);
}

/**
 * Returns the plane of the shaft end the shaft is mirrored across.
 */
function getMirrorPlane(context is Context, definition is map, extrudeId is Id, shaft is Query) returns Plane
{
    const capType = definition.flipMirrorEnd ? CapType.START : CapType.END;
    const mirrorFace = qCapEntity(extrudeId, capType, EntityType.FACE)->qOwnedByBody(shaft);
    const mirrorPlane = try silent(evPlane(context, { "face" : mirrorFace }));
    if (mirrorPlane == undefined)
    {
        throw regenError("The shaft end to mirror across must be planar.", ["flipMirrorEnd"], mirrorFace);
    }
    return mirrorPlane;
}

/**
 * Mirrors the shaft across `mirrorPlane` and unions the result back into the shaft.
 */
function mirrorShaftAcrossEnd(context is Context, id is Id, shaft is Query, mirrorPlane is Plane)
{
    opPattern(context, id + "pattern", {
                "entities" : shaft,
                "transforms" : [mirrorAcross(mirrorPlane)],
                "instanceNames" : ["mirror"]
            });
    opBoolean(context, id + "boolean", {
                "tools" : qUnion([shaft, qCreatedBy(id + "pattern", EntityType.BODY)]),
                "operationType" : BooleanOperationType.UNION
            });
}

/**
 * Cuts non-standard hex shaft features like rounded edges and churro profiles.
 */
function cutHexShaftFeatures(context is Context, id is Id, definition is map, shaft is Query, shaftPlane is Plane, endDefinitions is array)
{
    if (definition.hexType == HexType.STOCK)
    {
        return;
    }

    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : shaftPlane });

    if (hasPredrilledHole(definition))
    {
        var radius = getPredrilledHoleDiameter(definition) / 2;
        for (var endDefinition in endDefinitions)
        {
            if (endDefinition.endOperation != EndOperation.TAPPED_HOLE)
            {
                continue;
            }

            if (tolerantEquals(radius * 2, endDefinition.holeDiameter))
            {
                // Make it slightly smaller so the hole cut still works
                // We could make it bigger and cut the holes first instead, but then holes which are too small won't fail correctly
                // For some reason we need a bigger tolerance when trying to get a hole to successfully boolean
                radius -= TOLERANCE.zeroLength * meter * 1000;
            }
        }
        skCircle(sketch, "predrilledHoleCircle", {
                    "center" : zeroVector(2) * meter,
                    "radius" : radius
                });
    }

    if (definition.hexType == HexType.HEX_LITE || definition.hexType == HexType.CHURRO)
    {
        const basePoint = vector(0 * meter, getHexWidth(definition) / 2);
        const radius = getHexSize(definition) == HexSize._1_2_IN ? 1 / 16 * inch : 0.042 * inch;
        for (var i in range(0, 6))
        {
            const angle = i * 60 * degree;
            // Apply rotation in 2d coords
            const rotatedPoint = vector(basePoint[0] * cos(angle) - basePoint[1] * sin(angle), basePoint[0] * sin(angle) + basePoint[1] * cos(angle));
            skCircle(sketch, "churroCut" ~ i, {
                        "center" : rotatedPoint,
                        "radius" : radius
                    });
        }
    }
    else if (definition.hexType == HexType.ULTRA_HEX)
    {
        const hexWidth = getHexCircleRadius(5 * millimeter);
        skRegularPolygon(sketch, "insideHex", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(hexWidth, 0 * meter),
                    "sides" : 6
                });
    }
    skSolve(sketch);
    var sketchRegions = qSketchRegion(id + "sketch");

    if (definition.hexType == HexType.ROUNDED_HEX)
    {
        const innerRadius = (getHexSize(definition) == HexSize._1_2_IN ? 13.75 * millimeter : 10.25 * millimeter) / 2;
        const outerRadius = getHexWidth(definition)->getHexCircleRadius();
        // Sketch annulus seperately since filter inner loops will otherwise fail
        sketchRegions = qUnion(sketchRegions, sketchAnnulus(context, id + "annulus", shaftPlane, innerRadius, outerRadius));
    }
    const length = boundingBoxLength(context, qUnion(shaft, sketchRegions));
    opExtrude(context, id + "extrude", {
                "entities" : sketchRegions,
                "direction" : shaftPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : length,
                "startBound" : BoundingType.BLIND,
                "startDepth" : length
            });
    opBoolean(context, id + "boolean", {
                "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                "targets" : shaft,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
}

predicate hasPredrilledHole(definition is map)
{
    // Ultra hex has a hexagon, not a hole
    definition.hexType != HexType.STOCK && definition.hexType != HexType.ULTRA_HEX;
}

function getPredrilledHoleDiameter(definition is map) returns ValueWithUnits
{
    if (getHexSize(definition) == HexSize._1_2_IN)
    {
        return switch (definition.hexType) {
                    HexType.ROUNDED_HEX : 0.159 * inch,
                    HexType.CHURRO : 0.231 * inch,
                    HexType.HEX_LITE : 0.159 * inch
                };
    }
    else
    {
        return switch (definition.hexType) {
                    HexType.ROUNDED_HEX : 0.159 * inch,
                    HexType.CHURRO : 0.170 * inch,
                    HexType.HEX_LITE : 0.159 * inch
                };
    }
}

function setShaftProperties(context is Context, definition is map, shaft is Query)
{
    var color;
    var vendor;
    var partNumber;
    if (definition.shaftType == ShaftType.HEX)
    {
        color = definition.hexType == HexType.ULTRA_HEX ? WHITE : BLACK;

        if (definition.hexType == HexType.STOCK)
        {
            // Too bad, REV (and ThriftyBot I guess)
            vendor = "West Coast Products";
            partNumber = definition.hexSize == HexSize._1_2_IN ? "WCP-0915" : "WCP-0912";
        }
        if (definition.hexType == HexType.ROUNDED_HEX)
        {
            vendor = "West Coast Products";
            partNumber = definition.hexSize == HexSize._1_2_IN ? "WCP-0914" : "WCP-0911";
        }
        else if (definition.hexType == HexType.ULTRA_HEX)
        {
            vendor = "REV Robotics";
            partNumber = "REV-41-3205";
        }
        else if (definition.hexType == HexType.CHURRO)
        {
            vendor = "AndyMark";
            partNumber = "am-3101";
        }
        else if (definition.hexType == HexType.HEX_LITE)
        {
            vendor = "West Coast Products";
            partNumber = definition.hexSize == HexSize._1_2_IN ? "WCP-0917" : "WCP-1418";
        }
    }
    else
    {
        color = definition.splineType == SplineType.MAX_SPLINE ? WHITE : BLACK;
        vendor = definition.splineType == SplineType.MAX_SPLINE ? "REV Robotics" : "West Coast Products";
        partNumber = definition.splineType == SplineType.MAX_SPLINE ? "REV-21-2520" : "WCP-0918";
    }
    setProperty(context, {
                "entities" : shaft,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : color
            });

    setProperty(context, {
                "entities" : shaft,
                "propertyType" : PropertyType.MATERIAL,
                "value" : ALUMINUM
            });

    if (definition.vendor != undefined)
    {
        setProperty(context, {
                    "entities" : shaft,
                    "propertyType" : PropertyType.VENDOR,
                    "value" : vendor
                });

    }

    if (definition.partNumber != undefined)
    {
        setProperty(context, {
                    "entities" : shaft,
                    "propertyType" : PropertyType.PART_NUMBER,
                    "value" : partNumber
                });
    }
}

/**
 * Measures the length of the shaft.
 * Must be called before the shaft is mirrored, since mirroring consumes one of the extrude's cap faces.
 */
function measureShaftLength(context is Context, extrudeId is Id) returns ValueWithUnits
{
    try
    {
        const shaftFaces = qCapEntity(extrudeId, CapType.EITHER, EntityType.FACE);
        // This can fail if, e.g., one of the shaft cap faces is removed entirely by an overzealous hole
        // In a perfect world we'd measure maximum distance but measureDistance is weird and buggy
        return measureDistance(context, { "entities" : shaftFaces }).distance;
    }
    catch
    {
        // Fallback to measuring the edge length directly
        // Not ideal since, e.g., the faces don't have to be parallel (and retaining grooves split edges), but fine most of the time
        const shaftEdge = qNonCapEntity(extrudeId, EntityType.EDGE)->qLargest();
        return evLength(context, { "entities" : shaftEdge });
    }
}

function setShaftName(context is Context, shaft is Query, definition is map, length is ValueWithUnits)
{
    var shaftName;
    if (definition.shaftType == ShaftType.HEX)
    {
        shaftName = switch (definition.hexType)
            {
                    HexType.STOCK : "Hex",
                    HexType.ROUNDED_HEX : "Rounded Hex",
                    HexType.CHURRO : "Churro",
                    HexType.ULTRA_HEX : "UltraHex",
                    HexType.HEX_LITE : "Hex Lite"
                };
    }
    else
    {
        shaftName = definition.splineType == SplineType.MAX_SPLINE ? "MAXSpline" : "SplineXL";
    }

    const valueString = makeValueString(definition.unitSystem, length);
    setProperty(context, {
                "entities" : shaft,
                "propertyType" : PropertyType.NAME,
                /* 3 in. Hex Shaft */
                "value" : valueString ~ ". " ~ shaftName ~ " Shaft"
            });
}

/**
 * Defines a shaft end.
 * @type {{
 *      @field shaftEnd {ShaftEnd} : The shaftEnd to access parameters from.
 *              Note this should not be used to access the face of the shaft, as that is not always aligned with this value.
 * }}
 */
type EndDefinition typecheck canBeEndDefinition;

export predicate canBeEndDefinition(value)
{
    value is map;
    value.endFace is Query;
    value.endOperation is EndOperation;
    value.shaftEnd is ShaftEnd;
    if (isHoleOperation(value.endOperation))
    {
        value.holeDiameter is ValueWithUnits;

        if (value.endOperation == EndOperation.TAPPED_HOLE)
        {
            value.holeDepth is ValueWithUnits;
        }
    }
    else
    {
        if (value.endOperation == EndOperation.RETAINING_RING)
        {
            value.sideMount is boolean;
        }
        else
        {
            value.diameter is ValueWithUnits;
            value.length is ValueWithUnits;
            value.extendEnd is boolean;
        }
    }
}


/**
 * Returns an array of shaft [EndDefinition]s to modify.
 */
function getShaftEndDefinitions(context is Context, definition is map, extrudeId is Id, shaft is Query) returns array
{
    const firstEndFace = qCapEntity(extrudeId, CapType.START, EntityType.FACE)->qOwnedByBody(shaft);
    const secondEndFace = qCapEntity(extrudeId, CapType.END, EntityType.FACE)->qOwnedByBody(shaft);

    if (definition.mirrorShaft)
    {
        if (!definition.modifyFirstEnd)
        {
            return [];
        }
        // The mirrored end is consumed by the mirror, so the first end parameters drive whichever end isn't mirrored
        return [getEndDefinition(definition, ShaftEnd.FIRST, definition.flipMirrorEnd ? secondEndFace : firstEndFace)];
    }

    if (definition.modifyFirstEnd)
    {
        if (definition.firstEndOperation == EndOperation.CLEARANCE_HOLE)
        {
            return [getEndDefinition(definition, ShaftEnd.FIRST, firstEndFace)];
        }
        else if (definition.symmetricEnds)
        {
            return [getEndDefinition(definition, ShaftEnd.FIRST, firstEndFace), getEndDefinition(definition, ShaftEnd.FIRST, secondEndFace)];
        }
        else if (definition.modifySecondEnd)
        {
            return [getEndDefinition(definition, ShaftEnd.FIRST, firstEndFace), getEndDefinition(definition, ShaftEnd.SECOND, secondEndFace)];
        }
        return [getEndDefinition(definition, ShaftEnd.FIRST, firstEndFace)];
    }
    else if (definition.modifySecondEnd)
    {
        return [getEndDefinition(definition, ShaftEnd.SECOND, secondEndFace)];
    }
    return [];
}

function getEndDefinition(definition is map, shaftEnd is ShaftEnd, endFace is Query) returns EndDefinition
{
    const endString = getShaftEndString(shaftEnd);
    // Cast SecondEndOperation to EndOperation if neccessary
    const endOperation = getEndOperation(definition, shaftEnd);
    var base = {
        "shaftEnd" : shaftEnd,
        "endFace" : endFace,
        "endOperation" : endOperation,
    };

    if (isHoleOperation(endOperation))
    {
        if (endOperation == EndOperation.TAPPED_HOLE)
        {
            base.holeDepth = definition[endString ~ "HoleDepth"];
        }
        return mergeMaps(base, { "holeDiameter" : definition[endString ~ "HoleDiameter"] }) as EndDefinition;
    }
    else if (endOperation == EndOperation.RETAINING_RING)
    {
        return mergeMaps(base, {
                        "sideMount" : definition[endString ~ "SideMount"],
                        "extendEnd" : true
                        // Always extend retaining ring shaft end

                    }) as EndDefinition;
    }
    return mergeMaps(base, {
                    "diameter" : definition[endString ~ "Diameter"],
                    "length" : definition[endString ~ "Length"],
                    "extendEnd" : shaftEnd == ShaftEnd.FIRST ? definition.extendFirstEnd : definition.extendSecondEnd
                }) as EndDefinition;
}

/**
 * Verifies the end of a shaft is perpendicular to the direction of the shaft.
 */
function verifyPerpendicularShaftEnd(context is Context, endFace is Query, shaftDirection is Vector)
{
    if (isQueryEmpty(context, qParallelPlanes(endFace, shaftDirection, true)))
    {
        throw regenError("Modified shaft ends must be perpendicular to the direction of the shaft.", endFace);
    }
}

function modifyShaftEnd(context is Context, topLevelId is Id, id is Id, definition is map, shaft is Query, endDefinition is EndDefinition, reconstructOp is function)
precondition
{
    isTopLevelId(topLevelId);
}
{
    if (isHoleOperation(endDefinition.endOperation))
    {
        const shaftEndPlane = evPlane(context, { "face" : endDefinition.endFace });
        const locations = sketchHoleLocation(context, id + "locations", shaftEndPlane);
        const tableAndPath = getTableAndPath(definition, endDefinition.shaftEnd);

        var holeDefinition;
        if (endDefinition.endOperation == EndOperation.TAPPED_HOLE)
        {
            var holePath = tableAndPath.path;
            holePath.holeType = "Tapped";
            holePath.fit = "None";
            holePath["type"] = "Straight tap";

            // definition is source of truth for hole diameter, but major diameter is used for annotation
            const value = getLookupTable(tableAndPath.table, tableAndPath.path);
            holeDefinition = {
                    "isV2" : true,
                    "unitsSystem" : UnitsSystem.INCH,
                    "endStyleV2" : HoleEndStyleV2.BLIND,
                    "locations" : locations,
                    "ansiHoleTableEx" : holePath,
                    "holeDiameterV2" : endDefinition.holeDiameter,
                    "majorDiameter" : value.majorDiameter,
                    "holeDepth" : endDefinition.holeDepth,
                    "tappedDepth" : endDefinition.holeDepth,
                    "holeDiameterV2Precision" : PrecisionType.DEFAULT,
                    "holeDiameterV2ToleranceType" : ToleranceTypeExtended.NONE,
                    "scope" : shaft,
                // "showTappedDepth" : true,
                // "tapClearance" : 0,
                };
        }
        else
        {
            var holePath = tableAndPath.path;
            holePath.holeType = "Clearance";

            holeDefinition = {
                    "isV2" : true,
                    "unitsSystem" : UnitsSystem.INCH,
                    "endStyleV2" : HoleEndStyleV2.THROUGH,
                    "locations" : locations,
                    "ansiHoleTableEx" : holePath,
                    "holeDiameterV2" : endDefinition.holeDiameter,
                    // "majorDiameter" : value.majorDiameter,
                    // "holeDepth" : endDefinition.holeDepth,
                    // "tappedDepth" : endDefinition.holeDepth,
                    "holeDiameterV2Precision" : PrecisionType.DEFAULT,
                    "holeDiameterV2ToleranceType" : ToleranceTypeExtended.NONE,
                    "scope" : shaft,
                // "showTappedDepth" : true,
                // "tapClearance" : 0,
                };
        }

        try
        {
            callSubfeatureAndProcessStatus(topLevelId, hole, context, id + "hole", holeDefinition, { "propagateErrorDisplay" : true });
        }
        catch (error)
        {
            reconstructOp();
            throw error;
        }
    }
    else
    {
        extendShaftEnd(context, id + "extend", definition, endDefinition, reconstructOp);

        const toolId = id + "shaftTool";
        if (endDefinition.endOperation == EndOperation.CAPTIVE_SHAFT)
        {
            extrudeCaptiveShaftTool(context, toolId, definition, shaft, endDefinition, reconstructOp);
        }
        else if (endDefinition.endOperation == EndOperation.RETAINING_RING)
        {
            extrudeShaftGrooveTool(context, toolId, definition, shaft, endDefinition);
        }
        cutShaft(context, id + "cutShaft", shaft, qCreatedBy(toolId, EntityType.BODY)->qBodyType(BodyType.SOLID), reconstructOp);

    }
}

function extendShaftEnd(context is Context, id is Id, definition is map, endDefinition is EndDefinition, reconstructOp is function)
{
    if (!endDefinition.extendEnd)
    {
        return;
    }

    var endWidth;
    if (endDefinition.endOperation == EndOperation.RETAINING_RING)
    {
        endWidth = getGrooveDefinition(endDefinition.sideMount, getHexSize(definition), definition.unitSystem).endWidth;
    }
    else
    {
        endWidth = endDefinition.length;
    }

    try
    {
        opOffsetFace(context, id + "extendFace", {
                    "moveFaces" : endDefinition.endFace,
                    "offsetDistance" : endWidth
                });
    }
    catch
    {
        reconstructOp();
        throw regenError("Failed to extend shaft end.", ["shaftEnds"], endDefinition.endFace);
    }
}

function extrudeCaptiveShaftTool(context is Context, id is Id, definition is map, shaft is Query, endDefinition is EndDefinition, reconstructOp is function)
{
    const facePlane = evPlane(context, { "face" : endDefinition.endFace });

    const outerRadius = getHexWidth(definition)->getHexCircleRadius();
    const innerRadius = endDefinition.diameter / 2;
    if (tolerantGreaterThan(innerRadius, outerRadius))
    {
        reconstructOp();

        const errorId = id + "error";
        try
        {
            const sketch = newSketchOnPlane(context, errorId + "sketch", { "sketchPlane" : facePlane });
            skCircle(sketch, "circle", {
                        "center" : zeroVector(2) * meter,
                        "radius" : innerRadius
                    });
            skSolve(sketch);

            opExtrude(context, errorId + "extrude", {
                        "entities" : qSketchRegion(errorId + "sketch"),
                        "direction" : -facePlane.normal,
                        "endBound" : BoundingType.BLIND,
                        "endDepth" : endDefinition.length
                    });
        }

        throw regenError("The captive shaft diameter is greater than the diameter of the shaft.", [getShaftEndString(endDefinition.shaftEnd) ~ "Diameter"], qCreatedBy(errorId + "extrude", EntityType.BODY));
    }

    const outside = sketchAnnulus(context, id + "outside", facePlane, outerRadius, innerRadius);
    opExtrude(context, id + "extrudeOutside", {
                "entities" : outside,
                "direction" : -facePlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : endDefinition.length
            });
}


function extrudeShaftGrooveTool(context is Context, id is Id, definition is map, shaft is Query, endDefinition is EndDefinition)
{
    const facePlane = evPlane(context, { "face" : endDefinition.endFace });

    const grooveDefinition = getGrooveDefinition(endDefinition.sideMount, getHexSize(definition), definition.unitSystem);
    const edgeMargin = grooveDefinition.endWidth - grooveDefinition.grooveWidth;

    const shaftRadius = getHexWidth(definition)->getHexCircleRadius();
    const groove = sketchAnnulus(context, id + "groove", facePlane, grooveDefinition.grooveDiameter / 2, shaftRadius);

    // Annulus is on the end of the shaft, so extrude the groove by extruding from the edgeMargin up to the endWidth
    opExtrude(context, id + "extrudeGroove", {
                "entities" : groove,
                "direction" : -facePlane.normal,
                "startBound" : BoundingType.BLIND,
                "isStartBoundOpposite" : false,
                "startDepth" : edgeMargin,
                "endBound" : BoundingType.BLIND,
                "endDepth" : grooveDefinition.endWidth
            });

    // Face off end of shaft if ring isn't side mount
    if (!endDefinition.sideMount)
    {
        const outerDiameter = getHexWidth(definition);
        const outside = sketchAnnulus(context, id + "outside", facePlane, outerDiameter / 2, shaftRadius);

        opExtrude(context, id + "extrudeOutside", {
                    "entities" : outside,
                    "direction" : -facePlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : edgeMargin
                });
    }
}

function cutShaft(context is Context, id is Id, shaft is Query, tool is Query, reconstructOp is function)
{
    try
    {
        opBoolean(context, id + "cutShaft", {
                    "tools" : tool,
                    "targets" : shaft,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        reconstructOp();
        throw regenError("Failed to modify shaft end. Check input.", tool);
    }
}


function sketchAnnulus(context is Context, id is Id, sketchPlane is Plane, innerRadius is ValueWithUnits, outerRadius is ValueWithUnits) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : sketchPlane });
    skCircle(sketch, "innerCircle", {
                "center" : zeroVector(2) * meter,
                "radius" : innerRadius
            });

    skCircle(sketch, "outerCircle", {
                "center" : zeroVector(2) * meter,
                "radius" : outerRadius
            });
    skSolve(sketch);
    return qSketchRegion(id, true);
}

function getGrooveDefinition(sideMount is boolean, hexSize is HexSize, unitSystem is UnitSystem) returns map
{
    if (sideMount)
    {
        return switch (hexSize) {
                    HexSize._1_2_IN : {
                        "grooveDiameter" : 0.396 * inch,
                        "grooveWidth" : 0.046 * inch,
                        "endWidth" : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter)
                    },
                    HexSize._3_8_IN : {
                        "grooveDiameter" : 0.303 * inch,
                        "grooveWidth" : 0.039 * inch,
                        "endWidth" : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter)
                    }
                };
    }

    return switch (hexSize) {
                HexSize._1_2_IN : {
                    "grooveDiameter" : 0.39 * inch,
                    "grooveWidth" : 0.0468 * inch,
                    "endWidth" : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter)
                },
                HexSize._3_8_IN : {
                    "grooveDiameter" : 0.352 * inch,
                    "grooveWidth" : 0.029 * inch,
                    "endWidth" : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter)
                }
            };
}

export function robotShaftEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    // Extrude edit logic only runs when we're creating
    if (isCreating)
    {
        // Create entities to trigger merge scope logic
        if (!isQueryEmpty(context, definition.location))
        {
            try
            {
                const shaftPlane = getLocationPlane(context, definition);
                definition.entities = createShaftProfile(context, id, definition, shaftPlane);
            }
        }
        definition = stdNewExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
    }

    for (var shaftEnd in values(ShaftEnd))
    {
        if (activePathChanged(oldDefinition, definition, shaftEnd))
        {
            definition = applyTableDefinition(definition, shaftEnd);
        }

        if (shaftChanged(oldDefinition, definition))
        {
            definition = updateCaptiveShaftParameters(definition, shaftEnd);
        }
    }

    return definition;
}

function shaftChanged(oldDefinition is map, definition is map) returns boolean
{
    if (oldDefinition == {})
    {
        // This breaks REMEMBER_PREVIOUS_VALUE but is neccessary to keep consistent when feature is created for the first time
        return true;
    }
    return oldDefinition.shaftType != definition.shaftType || oldDefinition.hexSize != definition.hexSize;
}

function updateCaptiveShaftParameters(definition is map, shaftEnd is ShaftEnd) returns map
{
    if (definition.shaftType != ShaftType.HEX)
    {
        return definition;
    }

    const endString = getShaftEndString(shaftEnd);
    if (definition.hexSize == HexSize._1_2_IN)
    {
        definition[endString ~ "Diameter"] = "0.5 in";
        definition[endString ~ "Length"] = "0.3125 in";
    }
    else
    {
        definition[endString ~ "Diameter"] = "0.375 in";
        definition[endString ~ "Length"] = "0.280 in";
    }
    return definition;
}

enum ShaftEnd
{
    FIRST,
    SECOND
}

function getShaftEndString(shaftEnd is ShaftEnd) returns string
{
    return switch (shaftEnd) {
                ShaftEnd.FIRST : "firstEnd",
                ShaftEnd.SECOND : "secondEnd"
            };
}

function getEndOperation(definition is map, shaftEnd is ShaftEnd) returns EndOperation
{
    return getShaftEndParameter(definition, shaftEnd, "Operation") as EndOperation;
}

function getShaftEndParameter(definition is map, shaftEnd is ShaftEnd, parameter is string)
{
    return definition[getShaftEndString(shaftEnd) ~ parameter];
}

function getTableAndPath(definition is map, shaftEnd is ShaftEnd) returns map
{
    const endOperation = getEndOperation(definition, shaftEnd);
    if (endOperation == EndOperation.CLEARANCE_HOLE)
    {
        return { "table" : clearanceHoleTable, "path" : getShaftEndParameter(definition, shaftEnd, "ClearanceHolePath") };
    }
    return { "table" : tappedHoleTable, "path" : getShaftEndParameter(definition, shaftEnd, "TappedHolePath") };
}

function activePathChanged(oldDefinition is map, definition is map, shaftEnd is ShaftEnd) returns boolean
{
    if (oldDefinition == {})
    {
        return true;
    }
    // Either change in table or path requires update
    return getTableAndPath(oldDefinition, shaftEnd) != getTableAndPath(definition, shaftEnd);
}

function applyTableDefinition(definition is map, shaftEnd is ShaftEnd) returns map
{
    const tableAndPath = getTableAndPath(definition, shaftEnd);
    const endString = getShaftEndString(shaftEnd);
    if (getEndOperation(definition, shaftEnd) == EndOperation.TAPPED_HOLE)
    {
        definition[endString ~ "HoleDiameter"] = getLookupTable(tableAndPath.table, tableAndPath.path).tapDrillDiameter;
    }
    else
    {
        definition[endString ~ "HoleDiameter"] = getLookupTable(tableAndPath.table, tableAndPath.path).holeDiameter;
    }
    return definition;
}



// function getPitch(definition is map, shaftEnd is ShaftEnd) returns ValueWithUnits
// {
//     // 32 Threads per inch
//     return inch / 32;
// }

// function updateShaftTapParameters(oldDefinition is map, definition is map, specifiedParameters is map, shaftEnd is ShaftEnd) returns map
// {
//     const pitch = getPitch(definition, shaftEnd);
//     const endString = getShaftEndString(shaftEnd);
//     const fieldInfos = [
//             {
//                 "fieldName" : endString ~ "HoleDepth",
//                 "precalculatedValue" : getShaftEndParameter(definition, shaftEnd, "TappedDepth") + getShaftEndParameter(definition, shaftEnd, "TapClearance") * pitch,
//                 "allowedToBeZero" : false
//             }, {
//                 "fieldName" : endString ~ "TappedDepth",
//                 "precalculatedValue" : getShaftEndParameter(definition, shaftEnd, "HoleDepth") - getShaftEndParameter(definition, shaftEnd, "TapClearance") * pitch,
//                 "allowedToBeZero" : false
//             }, {
//                 "fieldName" : endString ~ "TapClearance",
//                 // Pitch should be non-zero if `computePitch` returns successfully above, so dividing by it should be ok
//                 "precalculatedValue" : (getShaftEndParameter(definition, shaftEnd, "HoleDepth") - getShaftEndParameter(definition, shaftEnd, "TappedDepth")) / pitch,
//                 "allowedToBeZero" : true
//             }
//         ];

//     var candidates = [];
//     for (var fieldInfo in fieldInfos)
//     {
//         const valueIsBeingEdited = definition[fieldInfo.fieldName] != oldDefinition[fieldInfo.fieldName];
//         // When this is the "initial edit" made by the system when opening the dialog, it is ok to edit even though the value is undergoing a change.
//         const valueIsBeingEditedByUser = valueIsBeingEdited && oldDefinition[fieldInfo.fieldName] != undefined;

//         var valueIsPositive = fieldInfo.precalculatedValue > 0;
//         if (fieldInfo.allowedToBeZero)
//         {
//             valueIsPositive = valueIsPositive || fieldInfo.precalculatedValue == 0;
//         }

//         // Field is a candidate if it is not currently being edited, and is not going to be set to a negative number
//         if (!valueIsBeingEditedByUser && valueIsPositive)
//         {
//             candidates = candidates->append(fieldInfo);
//         }
//     }

//     if (candidates == [])
//     {
//         // No fields available to change. Let the feature issue a HOLE_INCONSISTENT_TAP_INFO warning
//         return definition;
//     }

//     // Try to pick a parameter that has not been edited yet
//     var fieldToChange = undefined;
//     for (var i = size(candidates) - 1; i >= 0; i -= 1) // Go backwards to prioritize fields at the end
//     {
//         if (specifiedParameters[candidates[i].fieldName] == false)
//         {
//             fieldToChange = candidates[i];
//         }
//     }
//     // Fall back on the last available field, if all the fields have been edited
//     fieldToChange = last(candidates);

//     definition[fieldToChange.fieldName] = fieldToChange.precalculatedValue;
//     return definition;
// }
