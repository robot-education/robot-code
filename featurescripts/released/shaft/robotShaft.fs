FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
// ISO_HoleTableEx, for a preset shaft's tapped ends
import(path : "onshape/std/holetables.gen.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");

export import(path : "603be214b7d30fd438966545", version : "81c3f377a07c755804b5c678");
import(path : "8fc3df84a88e74d27ad43d26", version : "ea3e16596c4700ea14d4def4");


export import(path : "21762d39019c8b2289e2fbb8", version : "4cdb575f478ffc25d97c7422");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "bb7c494edc43a307e631af8d");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
import(path : "b75434df23d86ba9542f761e", version : "410f29dc5fa8b0fe88f1e9c5");
import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
// Also exports the enum used as a parameter type
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");
// Exports Fit: released shafts have a fit parameter, and Onshape looks up its enum in the feature's file to load them
// fs check: ignore unused-import
export import(path : "926d933eb33b11a3452660fd", version : "734a856ee6a464616f05e7e4");
RobotShaftIcon::import(path : "41c2fc2cc7659b215feb01c8", version : "47af683e8c175dbcacb47bdf");

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

/**
 * The shafts an FTC team cuts to length or modifies (a custom shaft): goBILDA's and AndyMark's, by their profiles.
 */
export enum FtcCustomShaft
{
    annotation { "Name" : "8 mm REX" }
    REX_8_MM,
    annotation { "Name" : "12 mm REX" }
    REX_12_MM,
    annotation { "Name" : "3/8 in. Robits hex" }
    ROBITS_HEX
}

/**
 * Whether a shaft is a COTS shaft sold in set lengths, chosen by its table's Length: it's built as it's sold, its length
 * and ends set, so only where it starts is chosen. A custom shaft is one cut to a length, or with its ends modified.
 * Editing logic sets `presetShaft` from its table entry (`withShaft`).
 */
predicate isPresetLength(definition is map)
{
    definition.shaftSource == ShaftSource.COTS;
    definition.presetShaft;
}

/**
 * Whether a shaft has e-clip grooves at one end, as it's sold (goBILDA's REX with an e-clip), so Flip ends can put them
 * at the other. Editing logic sets `factoryGrooved` from its table entry (`withShaft`).
 */
predicate hasFactoryGrooves(definition is map)
{
    definition.shaftSource == ShaftSource.COTS;
    definition.factoryGrooved;
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
                    }

                    // Editing logic sets it from the hole table; it can be overridden
                    annotation { "Name" : "Hole diameter", "Icon" : Icon.HOLE_DIAMETER }
                    isLength(definition.firstEndHoleDiameter, HOLE_DIAMETER_BOUNDS);

                    if (isTappedFirstEnd(definition))
                    {
                        annotation { "Name" : "Hole depth", "Icon" : Icon.HOLE_DEPTH }
                        isLength(definition.firstEndHoleDepth, HOLE_DEPTH_BOUNDS);
                    }
                }
                else if (definition.firstEndOperation == EndOperation.RETAINING_RING)
                {
                    annotation { "Name" : "Side mount ring", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to use a side-mountable retaining ring." }
                    definition.firstEndSideMount is boolean;

                    annotation { "Name" : "Override groove diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.firstEndOverrideGroove is boolean;

                    if (definition.firstEndOverrideGroove)
                    {
                        // Editing logic sets it to the ring's usual groove when it's turned on
                        annotation { "Name" : "Groove diameter", "Description" : "The diameter of the groove the retaining ring sits in." }
                        isLength(definition.firstEndGrooveDiameter, GROOVE_DIAMETER_BOUNDS);
                    }
                }
                else if (definition.firstEndOperation == EndOperation.CAPTIVE_SHAFT)
                {
                    // Editing logic sets these from the shaft's size when it's created or its size changes
                    annotation { "Name" : "Diameter" }
                    isLength(definition.firstEndDiameter, NONNEGATIVE_LENGTH_BOUNDS);

                    annotation { "Name" : "Length" }
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

                        annotation { "Name" : "Override groove diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.secondEndOverrideGroove is boolean;

                        if (definition.secondEndOverrideGroove)
                        {
                            // Editing logic sets it to the ring's usual groove when it's turned on
                            annotation { "Name" : "Groove diameter", "Description" : "The diameter of the groove the retaining ring sits in." }
                            isLength(definition.secondEndGrooveDiameter, GROOVE_DIAMETER_BOUNDS);
                        }
                    }
                    else if (definition.secondEndOperation == SecondEndOperation.CAPTIVE_SHAFT)
                    {
                        // Editing logic sets these from the shaft's size when it's created or its size changes
                        annotation { "Name" : "Diameter" }
                        isLength(definition.secondEndDiameter, NONNEGATIVE_LENGTH_BOUNDS);

                        annotation { "Name" : "Length" }
                        isLength(definition.secondEndLength, NONNEGATIVE_LENGTH_BOUNDS);

                        annotation { "Name" : "Extend shaft", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Whether to extend the shaft to accommodate hardware." }
                        definition.extendSecondEnd is boolean;
                    }
                }
            }
        }
    }
}


// 1/2 in. hex's retaining ring groove
const GROOVE_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.009906, 500], (inch) : 0.39, (millimeter) : 10 } as LengthBoundSpec;

annotation { "Feature Type Name" : "Robot shaft",
        "Feature Type Description" : "Create the shafts FRC and FTC teams buy, or custom ones." ~ CREDIT,
        "Manipulator Change Function" : "extrudeManipulatorChange",
        "Editing Logic Function" : "robotShaftEditLogic",
        "Icon" : RobotShaftIcon::BLOB_DATA
    }
export const robotShaft = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Name" : "Source", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.shaftSource is ShaftSource;

        // From the COTS shaft's table entry, by editing logic (withShaft): a precondition can't read a lookup table's
        // levels
        annotation { "Name" : "Preset shaft", "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.presetShaft is boolean;

        annotation { "Name" : "Factory grooved", "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.factoryGrooved is boolean;

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
                if (isFrc(definition))
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
                else
                {
                    // Sets the shaft's profile parameters, through editing logic (see withShaft)
                    annotation { "Name" : "Shaft", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.ftcCustomShaft is FtcCustomShaft;
                }
            }
        }

        annotation { "Group Name" : "Extrude", "Collapsed By Default" : false }
        {
            locationPredicate(definition, "shaft");

            // A preset's length is its own: only where it starts, and which way it goes (newExtrudePredicate, but
            // those). Onshape doesn't allow a parameter in two branches of an if
            if (!isPresetLength(definition))
            {
                annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.endBound is StockBoundingType;
            }

            annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.oppositeDirection is boolean;

            if (!isPresetLength(definition))
            {
                stockBoundsPredicate(definition);
            }

            extrudeOffsetPredicate(definition);

            if (hasFactoryGrooves(definition))
            {
                annotation { "Name" : "Flip ends", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.flipEnds is boolean;
            }

            if (!isPresetLength(definition))
            {
                newExtrudeSecondEndPredicate(definition);

                annotation { "Name" : "Mirror shaft", "Description" : "Mirror the shaft across one of its ends. Useful when the shaft is modeled up to a plane of symmetry." }
                definition.mirrorShaft is boolean;

                if (definition.mirrorShaft)
                {
                    annotation { "Name" : "Flip mirror end", "UIHint" : ["OPPOSITE_DIRECTION"] }
                    definition.flipMirrorEnd is boolean;
                }
            }
        }

        // A preset's ends are as it's sold
        if (canModifyShaftEnds(definition) && !isPresetLength(definition))
        {
            shaftEndPredicate(definition);
        }
    }
    {
        // A path from before the table's levels changed (like Length) isn't a whole entry
        const entry = getCotsShaft(definition);
        if (entry != undefined && entry.partName == undefined)
        {
            throw regenError("Choose the shaft again: its table has changed.", [isFrc(definition) ? "frcShaft" : "ftcShaft"]);
        }
        definition = withShaft(definition);
        const preset = isPresetLength(definition);
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

        if (preset)
        {
            cutFactoryFeatures(context, id, id + "factory", definition, extrudeId, shaft);
        }
        else if (canModifyShaftEnds(definition))
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

        if (definition.mirrorShaft)
        {
            mirrorShaftAcrossEnd(context, id + "mirrorShaft", shaft, mirrorPlane);
        }
        const shaftLength = measureShaftLength(context, shaft, shaftPlane);

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
 * profile (`shaftType`, `hexType`, `hexSize`, and `splineType`), or a custom FTC shaft's (from its Shaft), so the rest
 * of the feature can treat it as a custom FRC one. A
 * preset length's shaft is extruded that long (Blind, one way, not mirrored).
 * A COTS spline shaft may also have a `predrilledHoleDiameter`: a hole through a solid spline (see `isTubeSpline`).
 */
function withShaft(definition is map) returns map
{
    definition.unitSystem = isFrc(definition) ? UnitSystem.IMPERIAL : UnitSystem.METRIC;
    if (!isFrc(definition) && !isCotsShaft(definition))
    {
        definition.shaftType = ShaftType.HEX;
        definition.hexType = definition.ftcCustomShaft == FtcCustomShaft.ROBITS_HEX ? HexType.STOCK : HexType.ROUNDED_HEX;
        definition.hexSize = switch (definition.ftcCustomShaft) {
                FtcCustomShaft.REX_8_MM : HexSize._7_MM,
                FtcCustomShaft.REX_12_MM : HexSize._11_MM,
                FtcCustomShaft.ROBITS_HEX : HexSize._3_8_IN
            };
    }
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
        definition.presetShaft = shaft.presetLength != undefined;
        definition.factoryGrooved = shaft.factoryGrooves != undefined && shaft.factoryGrooves != [];
        if (shaft.presetLength != undefined)
        {
            // A preset's length, from where it starts
            definition.endBound = StockBoundingType.BLIND;
            definition.depth = shaft.presetLength;
            definition.symmetric = false;
            definition.hasSecondDirection = false;
            definition.mirrorShaft = false;
        }
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
/**
 * Cuts a preset shaft's features as it's sold (see robotShaftTables.py's `factory`), under `id`: its e-clip grooves,
 * measured from its first end (where it starts), or its second, with Flip ends, then a tapped hole in each end (std's
 * hole, as a subfeature of `topLevelId`). If one fails, the shaft is shown in blue.
 */
function cutFactoryFeatures(context is Context, topLevelId is Id, id is Id, definition is map, extrudeId is Id, shaft is Query)
{
    const cots = getCotsShaft(definition);
    const firstEnd = qCapEntity(extrudeId, CapType.START, EntityType.FACE)->qOwnedByBody(shaft);
    const secondEnd = qCapEntity(extrudeId, CapType.END, EntityType.FACE)->qOwnedByBody(shaft);
    const showShaft = function()
        {
            addDebugEntities(context, shaft, DebugColor.BLUE);
        };

    const groovedPlane = evPlane(context, { "face" : hasFactoryGrooves(definition) && definition.flipEnds ? secondEnd : firstEnd });
    for (var i, groove in cots.factoryGrooves)
    {
        const grooveId = id + "groove" + unstableIdComponent(i);
        const annulus = sketchAnnulus(context, grooveId + "sketch", groovedPlane, groove.diameter / 2, getHexWidth(definition)->getHexCircleRadius());
        opExtrude(context, grooveId + "extrude", {
                    "entities" : annulus,
                    "direction" : -groovedPlane.normal,
                    "startBound" : BoundingType.BLIND,
                    "isStartBoundOpposite" : false,
                    "startDepth" : groove.start,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : groove.end
                });
        cutShaft(context, grooveId, shaft, qCreatedBy(grooveId + "extrude", EntityType.BODY)->qBodyType(BodyType.SOLID),
            { "message" : "Failed to cut the shaft's e-clip groove.", "parameters" : ["ftcShaft"] }, showShaft);
    }

    const factoryHole = cots.factoryHole;
    if (factoryHole == undefined)
    {
        return;
    }
    // In std's hole table, which names a level "type"
    // fs check: ignore keyword-key
    const path = { "holeType" : "Tapped", "type" : "Straight tap", "size" : factoryHole.size, "pitch" : factoryHole.pitch, "fit" : "None" } as LookupTablePath;
    const tapped = getLookupTable(factoryHole.metric ? ISO_HoleTableEx : ANSI_HoleTableEx, path);
    for (var i, end in [firstEnd, secondEnd])
    {
        const holeId = id + "hole" + unstableIdComponent(i);
        const locations = sketchHoleLocation(context, holeId + "location", evPlane(context, { "face" : end }));
        var holeDefinition = {
                "isV2" : true,
                "unitsSystem" : factoryHole.metric ? UnitsSystem.METRIC : UnitsSystem.INCH,
                "endStyleV2" : HoleEndStyleV2.BLIND,
                "locations" : locations,
                "holeDiameterV2" : lookupTableEvaluate(tapped.tapDrillDiameter),
                "majorDiameter" : lookupTableEvaluate(tapped.majorDiameter),
                "holeDepth" : factoryHole.depth,
                "tappedDepth" : factoryHole.depth,
                "holeDiameterV2Precision" : PrecisionType.DEFAULT,
                "holeDiameterV2ToleranceType" : ToleranceTypeExtended.NONE,
                "scope" : shaft
            };
        holeDefinition[factoryHole.metric ? "isoHoleTableEx" : "ansiHoleTableEx"] = path;
        try
        {
            callSubfeatureAndProcessStatus(topLevelId, hole, context, holeId + "hole", holeDefinition, { "propagateErrorDisplay" : true });
        }
        catch (error)
        {
            showShaft();
            throw error;
        }
    }
}

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
 * longer than it's sold.
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
 * The shaft's length: how far it goes along its axis (`shaftPlane`'s normal, which it's extruded along, with flat ends
 * across it), whatever its ends' holes, grooves, and extensions, and mirrored or not.
 */
function measureShaftLength(context is Context, shaft is Query, shaftPlane is Plane) returns ValueWithUnits
{
    const bounds = evBox3d(context, { "topology" : shaft, "cSys" : coordSystem(shaftPlane), "tight" : true });
    return bounds.maxCorner[2] - bounds.minCorner[2];
}

function setShaftName(context is Context, shaft is Query, definition is map, length is ValueWithUnits)
{
    var shaftName;
    if (definition.shaftType == ShaftType.HEX)
    {
        shaftName = switch (definition.hexType)
            {
                    HexType.STOCK : "Hex",
                    HexType.ROUNDED_HEX : isMetricHex(definition.hexSize) ? "REX" : "Rounded Hex",
                    HexType.CHURRO : "Churro",
                    HexType.ULTRA_HEX : "UltraHex",
                    HexType.HEX_LITE : "Hex Lite"
                };
    }
    else
    {
        shaftName = splineName(definition.splineType);
    }

    setProperty(context, {
                "entities" : shaft,
                "propertyType" : PropertyType.NAME,
                // 3 in. Hex Shaft, or 136 mm Hex Shaft: its length as a COTS shaft's is
                "value" : shaftLengthString(definition, length) ~ " " ~ shaftName ~ " Shaft"
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
            // Undefined for the ring's usual groove
            value.grooveDiameter == undefined || isLength(value.grooveDiameter);
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
        }
        return mergeMaps(base, { "holeDiameter" : definition[endString ~ "HoleDiameter"] }) as EndDefinition;
    }
    else if (endOperation == EndOperation.RETAINING_RING)
    {
        return mergeMaps(base, {
                        "sideMount" : definition[endString ~ "SideMount"],
                        "grooveDiameter" : definition[endString ~ "OverrideGroove"] ? definition[endString ~ "GrooveDiameter"] : undefined,
                        // Always extend retaining ring shaft end
                        "extendEnd" : true
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
            extrudeShaftGrooveTool(context, toolId, definition, shaft, endDefinition, reconstructOp);
        }
        const endString = getShaftEndString(endDefinition.shaftEnd);
        const failure = endDefinition.endOperation == EndOperation.CAPTIVE_SHAFT ?
            { "message" : "Failed to cut the captive end.", "parameters" : [endString ~ "Diameter", endString ~ "Length"] } :
            { "message" : "Failed to cut the retaining ring's groove.", "parameters" : endDefinition.grooveDiameter == undefined ? [endString ~ "SideMount"] :
                    [endString ~ "SideMount", endString ~ "GrooveDiameter"] };
        cutShaft(context, id + "cutShaft", shaft, qCreatedBy(toolId, EntityType.BODY)->qBodyType(BodyType.SOLID), failure, reconstructOp);

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
        const endString = getShaftEndString(endDefinition.shaftEnd);
        throw regenError("Failed to extend the shaft's end.",
            endDefinition.endOperation == EndOperation.RETAINING_RING ? [endString ~ "Operation"] : [endString ~ "Length", endDefinition.shaftEnd == ShaftEnd.FIRST ? "extendFirstEnd" : "extendSecondEnd"],
            endDefinition.endFace);
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


function extrudeShaftGrooveTool(context is Context, id is Id, definition is map, shaft is Query, endDefinition is EndDefinition, reconstructOp is function)
{
    const facePlane = evPlane(context, { "face" : endDefinition.endFace });

    var grooveDefinition = getGrooveDefinition(endDefinition.sideMount, getHexSize(definition), definition.unitSystem);
    const edgeMargin = grooveDefinition.endWidth - grooveDefinition.grooveWidth;

    const shaftRadius = getHexWidth(definition)->getHexCircleRadius();
    if (endDefinition.grooveDiameter != undefined)
    {
        if (endDefinition.grooveDiameter >= shaftRadius * 2)
        {
            // The shaft in blue, and the groove as entered in red
            reconstructOp();
            const errorId = id + "error";
            try silent
            {
                const sketch = newSketchOnPlane(context, errorId + "sketch", { "sketchPlane" : facePlane });
                skCircle(sketch, "circle", {
                            "center" : zeroVector(2) * meter,
                            "radius" : endDefinition.grooveDiameter / 2
                        });
                skSolve(sketch);
                opExtrude(context, errorId + "extrude", {
                            "entities" : qSketchRegion(errorId + "sketch"),
                            "direction" : -facePlane.normal,
                            "startBound" : BoundingType.BLIND,
                            "isStartBoundOpposite" : false,
                            "startDepth" : edgeMargin,
                            "endBound" : BoundingType.BLIND,
                            "endDepth" : grooveDefinition.endWidth
                        });
            }
            throw regenError("The groove diameter is larger than the shaft.", [getShaftEndString(endDefinition.shaftEnd) ~ "GrooveDiameter"],
                qCreatedBy(errorId + "extrude", EntityType.BODY));
        }
        grooveDefinition.grooveDiameter = endDefinition.grooveDiameter;
    }
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

/**
 * Cuts `tool` from the shaft; if it can't be, throws `failure.message`, highlighting `failure.parameters` and the tool.
 */
function cutShaft(context is Context, id is Id, shaft is Query, tool is Query, failure is map, reconstructOp is function)
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
        throw regenError(failure.message, failure.parameters, tool);
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

        if (grooveOverrideStarted(oldDefinition, definition, shaftEnd))
        {
            definition = updateGrooveDiameter(definition, shaftEnd);
        }
    }

    return definition;
}

function shaftChanged(oldDefinition is map, definition is map) returns boolean
{
    if (oldDefinition == {})
    {
        // A new shaft's captive end fits its size (so its Diameter and Length don't remember their previous values)
        return true;
    }
    return oldDefinition.shaftType != definition.shaftType || oldDefinition.hexSize != definition.hexSize;
}

/**
 * Whether an end's groove diameter is newly overridden: Override groove diameter turned on, or on (remembered) for a
 * new shaft, or the shaft's size or the end's Side mount ring changed, whose usual grooves differ.
 */
function grooveOverrideStarted(oldDefinition is map, definition is map, shaftEnd is ShaftEnd) returns boolean
{
    const endString = getShaftEndString(shaftEnd);
    if (!definition[endString ~ "OverrideGroove"])
    {
        return false;
    }
    return oldDefinition == {} || !oldDefinition[endString ~ "OverrideGroove"] || shaftChanged(oldDefinition, definition) ||
        oldDefinition[endString ~ "SideMount"] != definition[endString ~ "SideMount"];
}

/**
 * Sets an end's Groove diameter to its ring's usual groove, for 1/2 in. and 3/8 in. hex (others have none, and say so
 * when the shaft's built).
 */
function updateGrooveDiameter(definition is map, shaftEnd is ShaftEnd) returns map
{
    if (definition.shaftType != ShaftType.HEX || isMetricHex(definition.hexSize))
    {
        return definition;
    }
    const endString = getShaftEndString(shaftEnd);
    const grooveDiameter = getGrooveDefinition(definition[endString ~ "SideMount"], definition.hexSize, definition.unitSystem).grooveDiameter;
    definition[endString ~ "GrooveDiameter"] = toString(roundToPrecision(grooveDiameter / inch, 4)) ~ " in";
    return definition;
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

/**
 * Sets an end's Hole diameter from its hole table: a tapped hole's tap drill, or a clearance hole's diameter for its
 * fit.
 */
function applyTableDefinition(definition is map, shaftEnd is ShaftEnd) returns map
{
    const tableAndPath = getTableAndPath(definition, shaftEnd);
    const value = getLookupTable(tableAndPath.table, tableAndPath.path);
    definition[getShaftEndString(shaftEnd) ~ "HoleDiameter"] = getEndOperation(definition, shaftEnd) == EndOperation.TAPPED_HOLE ? value.tapDrillDiameter : value.holeDiameter;
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
