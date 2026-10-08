FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/cosmeticThreadUtils.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

export import(path : "603be214b7d30fd438966545", version : "8cea43d8d56e3cbfb73426f0");
import(path : "8fc3df84a88e74d27ad43d26", version : "a29c4701c1348914e6f1f6c4");


export import(path : "21762d39019c8b2289e2fbb8", version : "8f82cf693e7833130ba80201");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
import(path : "b75434df23d86ba9542f761e", version : "410f29dc5fa8b0fe88f1e9c5");
import(path : "ea127c07807644fb48d3a1ae", version : "3c1ddfaf5ff0b3d5897422d0");
import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "7ff3897ddcba9a81bae27310");
// Also exports the enum used as a parameter type
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");
// Exports Fit, a parameter type
export import(path : "core/fit.fs", version : "");

/**
 * Whether a shaft is one someone sells (see robotShaftTables.py), or custom.
 */
export enum ShaftSource
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Custom" }
    CUSTOM
}

export predicate isCotsShaft(definition is map)
{
    definition.shaftSource == ShaftSource.COTS;
}

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

/**
 * Whether a shaft is a SplineXS, whose ends can only be tapped (see `getEndOperation`): it's solid, and too small for
 * retaining rings or captive ends.
 *
 * The predicates below repeat its condition, as Onshape doesn't allow predicates used in a precondition's if conditions
 * to call other predicates.
 */
predicate isSplineXsShaft(definition is map)
{
    definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS;
}

/**
 * Whether a shaft's ends can be modified: hex shafts, and SplineXS shafts (tapped). Other splines are tubes.
 */
predicate canModifyShaftEnds(definition is map)
{
    definition.shaftType == ShaftType.HEX ||
        (definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS);
}

predicate isTappedFirstEnd(definition is map)
{
    (definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS) ||
        definition.firstEndOperation == EndOperation.TAPPED_HOLE;
}

predicate isTappedSecondEnd(definition is map)
{
    (definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS) ||
        definition.secondEndOperation == SecondEndOperation.TAPPED_HOLE;
}

predicate isClearanceFirstEnd(definition is map)
{
    !(definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS) &&
        definition.firstEndOperation == EndOperation.CLEARANCE_HOLE;
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
        (definition.modifyFirstEnd && (definition.symmetricEnds ||
                (!(definition.shaftType == ShaftType.SPLINE && definition.splineType == SplineType.SPLINE_XS) &&
                    definition.firstEndOperation == EndOperation.CLEARANCE_HOLE)));
}

predicate shaftEndPredicate(definition is map)
{
    annotation { "Group Name" : "Shaft ends", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Modify first end", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.modifyFirstEnd is boolean;

        if (definition.modifyFirstEnd)
        {
            annotation { "Group Name" : "First end hardware", "Collapsed By Default" : false, "Driving Parameter" : "modifyFirstEnd" }
            {
                if (!isSplineXsShaft(definition))
                {
                    annotation { "Name" : "End operation", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.firstEndOperation is EndOperation;
                }

                // annotation { "Name" : "Flip shaft ends", "UIHint" : ["OPPOSITE_DIRECTION"] }
                // definition.flipShaftEnds is boolean;

                if (isTappedFirstEnd(definition) || isClearanceFirstEnd(definition))
                {
                    if (isTappedFirstEnd(definition))
                    {
                        annotation { "Name" : "Hole table", "Lookup Table" : tappedHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.firstEndTappedHolePath is LookupTablePath;
                    }
                    else
                    {
                        annotation { "Name" : "Hole table", "Lookup Table" : clearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.firstEndClearanceHolePath is LookupTablePath;

                        fitPredicate(definition);
                    }

                    if (isTappedFirstEnd(definition))
                    {
                        annotation { "Name" : "Hole diameter", "Icon" : Icon.HOLE_DIAMETER }
                        isLength(definition.firstEndHoleDiameter, HOLE_DIAMETER_BOUNDS);

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
            if (!isClearanceFirstEnd(definition) && !definition.mirrorShaft)
            {
                annotation { "Name" : "Symmetric ends", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.symmetricEnds is boolean;
            }
        }

        if (!cannotSpecifySecondEnd(definition))
        {
            annotation { "Name" : "Modify second end", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.modifySecondEnd is boolean;

            if (definition.modifySecondEnd)
            {
                annotation { "Group Name" : "Second end hardware", "Collapsed By Default" : false, "Driving Parameter" : "modifySecondEnd" }
                {
                    if (!isSplineXsShaft(definition))
                    {
                        annotation { "Name" : "End operation", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.secondEndOperation is SecondEndOperation;
                    }

                    if (isTappedSecondEnd(definition))
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
        "Feature Type Description" : "Create the shafts FRC and FTC teams buy, or custom ones." ~ CREDIT,
        "Manipulator Change Function" : "extrudeManipulatorChange",
        "Editing Logic Function" : "robotShaftEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotShaft = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Name" : "Source", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.shaftSource is ShaftSource;

        locationPredicate(definition, "shaft");

        if (isCotsShaft(definition))
        {
            // Sets the shaft's profile parameters, through editing logic (see withShaft)
            if (isFrc(definition))
            {
                annotation { "Name" : "Shaft", "Lookup Table" : frcShaftTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcShaft is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Shaft", "Lookup Table" : ftcShaftTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcShaft is LookupTablePath;
            }
        }
        else
        {
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

        if (canModifyShaftEnds(definition))
        {
            shaftEndPredicate(definition);
        }
    }
    {
        definition = withShaft(definition);
        const shaftPlane = getLocationPlane(context, definition);
        const shaftProfile = createShaftProfile(context, id + "profile", definition, shaftPlane);
        definition = transformDefintionForNewExtrude(definition, shaftProfile);
        // Use top level id for extrude so manipulators propagate
        // Still distinguish extrudeId from id for semantics though
        const extrudeId = id;
        callSubfeatureAndProcessStatus(id, extrude, context, extrudeId, definition, { "featureParameterMap" : { "entities" : "location" } });
        // Need makeRobustQuery here since otherwise shaft will always include any other solids produced by the feature
        const shaft = makeRobustQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID));
        verifyFlatEnds(context, extrudeId, shaft, "shaft");

        // Capture the mirror plane before the shaft ends are modified
        var mirrorPlane = undefined;
        if (definition.mirrorShaft)
        {
            mirrorPlane = getMirrorPlane(context, definition, extrudeId, shaft);
        }

        if (canModifyShaftEnds(definition))
        {
            const endDefinitions = getShaftEndDefinitions(context, definition, extrudeId, shaft);
            if (definition.shaftType == ShaftType.HEX)
            {
                cutHexShaftFeatures(context, id + "cutFeatures", definition, shaft, shaftPlane, endDefinitions);
            }

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

        const cots = getCotsShaft(definition);
        if (cots != undefined)
        {
            setCotsShaftProperties(context, id, shaft, definition, cots, shaftLength);
        }
        else
        {
            setShaftProperties(context, definition, shaft);
            setShaftName(context, shaft, definition, shaftLength);
        }

        // Cleanup after boolean to avoid deleting profile faces to early
        cleanup(context, id + "deleteProfiles", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    });


/**
 * The selected COTS shaft's entry in its lookup table (see robotShaftTables.py), or `undefined` for a custom shaft.
 */
function getCotsShaft(definition is map)
{
    if (!isCotsShaft(definition))
    {
        return undefined;
    }
    return isFrc(definition) ? getLookupTable(frcShaftTable, definition.frcShaft) : getLookupTable(ftcShaftTable, definition.ftcShaft);
}

/**
 * The definition with the unit system its program uses (inches for FRC, millimeters for FTC), and a COTS shaft's
 * profile (`shaftType`, `hexType`, `hexSize`, and `splineType`), so the rest of the feature can treat it as custom.
 * A COTS spline shaft may also have a `predrilledHoleDiameter`: a hole through a solid spline (see `isTubeSpline`).
 */
function withShaft(definition is map) returns map
{
    definition.unitSystem = isFrc(definition) ? UnitSystem.IMPERIAL : UnitSystem.METRIC;
    const shaft = getCotsShaft(definition);
    if (shaft != undefined)
    {
        for (var key in ["shaftType", "hexType", "hexSize", "splineType"])
        {
            if (shaft[key] != undefined)
            {
                definition[key] = shaft[key];
            }
        }
        definition.predrilledHoleDiameter = shaft.predrilledHoleDiameter;
    }
    return definition;
}

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
        if (isTubeSpline(definition.splineType))
        {
            skSplineProfile(sketch, "inside", {
                        "splineType" : definition.splineType,
                        "profileSide" : ProfileSide.INSIDE
                    });
        }
        else if (definition.predrilledHoleDiameter != undefined)
        {
            skCircle(sketch, "predrilledHole", {
                        "center" : zeroVector(2) * meter,
                        "radius" : predrilledHoleRadius(definition.predrilledHoleDiameter, splineTapDrills(definition))
                    });
        }
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
        var tapDrills = [];
        for (var endDefinition in endDefinitions)
        {
            if (endDefinition.endOperation == EndOperation.TAPPED_HOLE)
            {
                tapDrills = append(tapDrills, endDefinition.holeDiameter);
            }
        }
        skCircle(sketch, "predrilledHoleCircle", {
                    "center" : zeroVector(2) * meter,
                    "radius" : predrilledHoleRadius(getPredrilledHoleDiameter(definition), tapDrills)
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
        const innerRadius = getRoundedDiameter(getHexSize(definition)) / 2;
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

/**
 * The radius to draw a predrilled hole: its diameter's, made slightly smaller if an end is tapped with a hole that size
 * (`tapDrills`), so the hole cut still works. (Making it bigger and cutting the holes first would work too, but then
 * holes which are too small wouldn't fail correctly.)
 */
function predrilledHoleRadius(diameter is ValueWithUnits, tapDrills is array) returns ValueWithUnits
{
    for (var tapDrill in tapDrills)
    {
        if (tolerantEquals(diameter, tapDrill))
        {
            // For some reason we need a bigger tolerance when trying to get a hole to successfully boolean
            return diameter / 2 - TOLERANCE.zeroLength * meter * 1000;
        }
    }
    return diameter / 2;
}

/**
 * The tap drills of the ends of a SplineXS shaft which will be tapped, from its definition (its profile is drawn before
 * its ends are found).
 */
function splineTapDrills(definition is map) returns array
{
    var tapDrills = [];
    if (definition.modifyFirstEnd)
    {
        tapDrills = append(tapDrills, definition.firstEndHoleDiameter);
    }
    if (definition.modifySecondEnd && !cannotSpecifySecondEnd(definition))
    {
        tapDrills = append(tapDrills, definition.secondEndHoleDiameter);
    }
    return tapDrills;
}

predicate hasPredrilledHole(definition is map)
{
    // Ultra hex has a hexagon, not a hole, and REX is solid (tapped at its ends)
    definition.hexType != HexType.STOCK && definition.hexType != HexType.ULTRA_HEX;
    !isMetricHex(getHexSize(definition));
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

/**
 * A length in inches for FRC or millimeters for FTC, e.g. `6 in.` or `136 mm`.
 */
function shaftLengthString(definition is map, length is ValueWithUnits) returns string
{
    return isFrc(definition) ?
        roundToPrecision(length / inch, 3) ~ " in." :
        roundToPrecision(length / millimeter, 1) ~ " mm";
}

// How close a shaft has to be to a length it's sold in to be that length: lengths are shown to 3 decimal places
const SOLD_LENGTH_TOLERANCE = 0.001 * inch;

/**
 * Names a COTS shaft (its length, then its `partName`), gives it the part number of the stock it's cut from (or, for a
 * shaft sold in its length, its own) and a link to buy it, and sets its vendor, material, and appearance. Warns if it's
 * longer than it's sold, or, for a shaft only sold in set lengths (`fixedLengths`), isn't one of them.
 */
function setCotsShaftProperties(context is Context, id is Id, shaft is Query, definition is map, cots is map, length is ValueWithUnits)
{
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.NAME, "value" : shaftLengthString(definition, length) ~ " " ~ cots.partName });
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.MATERIAL, "value" : cots.material });
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.APPEARANCE, "value" : cots.appearance });
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.VENDOR, "value" : cots.vendor });

    var stock = undefined;
    var shorter = undefined;
    for (var candidate in cots.stock)
    {
        if (length <= candidate.length + SOLD_LENGTH_TOLERANCE)
        {
            stock = candidate;
            break;
        }
        shorter = candidate;
    }
    if (stock == undefined)
    {
        reportFeatureWarning(context, id, "This shaft is only sold up to " ~ shaftLengthString(definition, shorter.length) ~ " long.");
        return;
    }
    if ((cots.fixedLengths ?? false) && stock.length - length > SOLD_LENGTH_TOLERANCE)
    {
        const nearest = shorter == undefined ?
            "the shortest is " ~ shaftLengthString(definition, stock.length) :
            "the nearest are " ~ shaftLengthString(definition, shorter.length) ~ " and " ~ shaftLengthString(definition, stock.length);
        reportFeatureWarning(context, id, "This shaft is only sold in set lengths; " ~ nearest ~ ".");
    }
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.PART_NUMBER, "value" : stock.partNumber });
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.DESCRIPTION, "value" : stock.url });
}

/**
 * Sets a custom shaft's material and appearance, in the colors its profile is usually sold in.
 */
function setShaftProperties(context is Context, definition is map, shaft is Query)
{
    const bare = definition.shaftType == ShaftType.HEX ?
        definition.hexType == HexType.ULTRA_HEX :
        definition.splineType != SplineType.SPLINE_XL;
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.APPEARANCE, "value" : bare ? MEDIUM_GRAY : BLACK });
    setProperty(context, { "entities" : shaft, "propertyType" : PropertyType.MATERIAL, "value" : ALUMINUM });
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
        shaftName = splineName(definition.splineType);
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
        if (getEndOperation(definition, ShaftEnd.FIRST) == EndOperation.CLEARANCE_HOLE)
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
            return mergeMaps(base, { "holeDiameter" : definition[endString ~ "HoleDiameter"] }) as EndDefinition;
        }
        const size = getTableAndPath(definition, shaftEnd).path.size;
        return mergeMaps(base, { "holeDiameter" : fastenerHoleDiameter(definition, size) }) as EndDefinition;
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
            // For the hole's callout; its diameter is the fit's
            holePath.fit = definition.fit == Fit.CLOSE ? "Close" : "Free";

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
    if (isMetricHex(hexSize))
    {
        throw regenError("Retaining ring grooves are only defined for 1/2 in. and 3/8 in. hex.", ["firstEndOperation", "secondEndOperation"]);
    }
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
    // So the precondition shows what suits a COTS shaft (e.g. no shaft ends on splines)
    definition = withShaft(definition);
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
    if (isMetricHex(definition.hexSize))
    {
        definition[endString ~ "Diameter"] = toString(getHexWidth(definition) / millimeter - 1) ~ " mm";
        definition[endString ~ "Length"] = "6 mm";
    }
    else if (definition.hexSize == HexSize._1_2_IN)
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
    if (isSplineXsShaft(definition))
    {
        return EndOperation.TAPPED_HOLE;
    }
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
    // A clearance hole's diameter comes from its fit
    if (getEndOperation(definition, shaftEnd) == EndOperation.TAPPED_HOLE)
    {
        const tableAndPath = getTableAndPath(definition, shaftEnd);
        definition[getShaftEndString(shaftEnd) ~ "HoleDiameter"] = getLookupTable(tableAndPath.table, tableAndPath.path).tapDrillDiameter;
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
