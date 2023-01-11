FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");
import(path : "onshape/std/chamfertype.gen.fs", version : "1930.0");

import(path : "472bc4c291e1d2d6f9b98937", version : "515ac497eaa4f72b8c5f1a57");

// custom icon import
// The icon was done by Eliza Barnett of Team 1745
icon::import(path : "f7208b20c64f74041af4afa2", version : "3326cee850e70c8a170b912d");

// pulley imports
Pulley::import(path : "1e62f40c1b0e67e2a4698b0d", version : "56bd23fa1f8c30c25c57d7f9");
Bore::import(path : "502b1384525cb629db56f1ca", version : "d04897f4140d563eac348f56");

export import(path : "00b10ef1fb1a7418097fc0af", version : "81a5f0dafb1063cd569c35e6");
export import(path : "e269bd2b7266145c47eaf374", version : "b54f77829d3c3e4e5f1b9278");
export import(path : "4d2d3f0157d54e1b6a06420a", version : "11f3d539786a3f39fd300bde");

/**
 * Todo:
 * Additional supplier belts
 * Supplier enums
 * Offset
 * Offset points
 * Combine engrave tooth count
 * Combine chamfer bore edges
 * Fix pulley size adjustment and rename to offset
 *
 * Editing logic fix
 * Update pulley flange styles?
 */

// export enum BeltCreationType
// {
//     annotation { "Name" : "Simple" }
//     SIMPLE,
//     annotation { "Name" : "Complex" }
//     COMPLEX
// }

// export enum PulleySelectionType
// {
//     annotation { "Name" : "Pulley" }
//     PULLEY,
//     annotation { "Name" : "Idler" }
//     IDLER
// }

// predicate simpleBelt(definition is map)
// {
//     definition.beltCreationType == BeltCreationType.SIMPLE;
// }

// predicate complexBelt(definition is map)
// {
//     definition.beltCreationType == BeltCreationType.COMPLEX;
// }

predicate selectionPredicate(definition is map)
{
    // annotation { "Name" : "Belt creation type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
    // definition.beltCreationType is BeltCreationType;

    annotation { "Name" : "Selections", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.hasSelections is boolean;

    if (definition.hasSelections)
    {
        annotation { "Group Name" : "Selections", "Collapsed By Default" : false, "Driving Parameter" : "hasSelections" }
        {
            annotation { "Name" : "Pulley one position", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.firstPoint is Query;

            annotation { "Name" : "Pulley two position", "Filter" : QueryFilterCompound.ALLOWS_AXIS || (EntityType.VERTEX && SketchObject.YES), "MaxNumberOfPicks" : 1,
                        "Description" : "Select a vertex, axis, or mate connector for pulley two to orient towards." }
            definition.secondPoint is Query;

            annotation { "Name" : "Offset", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.offset, OFFSET_BOUNDS);

            annotation { "Name" : "Flip direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.flipOffset is boolean;
        }
    }
    //     annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
    //     {
    //         annotation { "Name" : "Pulleys", "Item name" : "Pulley", "Item label template" : "#myParameter", "Driven query" : "pulleyLocation" }
    //         definition.pulleys is array;
    //         for (var i, pulley in definition.pulleys)
    //         {
    //             annotation { "Name" : "Pulley location", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
    //             pulley.pulleyLocation is Query;

    //             annotation { "Name" : "Pulley teeth" }
    //             isInteger(pulley.pulleyTeeth, PULLEY_TEETH_BOUNDS);
    //         }
    //     }
}

predicate autoBelt(definition is map)
{
    definition.hasSelections && definition.autoBelt;
}

predicate gt2Belt(definition is map)
{
    definition.beltType == BeltType.GT2;
}

predicate htdBelt(definition is map)
{
    definition.beltType == BeltType.HTD;
}

predicate htd9mmBelt(definition is map)
{
    // predicates can't be nested in preconditions
    definition.beltType == BeltType.HTD && definition.beltWidth == BeltWidth._9MM;
}

predicate htd15mmBelt(definition is map)
{
    definition.beltType == BeltType.HTD && definition.beltWidth == BeltWidth._15MM;
}

predicate beltPredicate(definition is map)
{
    annotation { "Group Name" : "General", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Belt type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
        definition.beltType is BeltType;

        if (definition.beltType == BeltType.HTD)
        {
            annotation { "Name" : "Belt width", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.beltWidth is BeltWidth;
        }

        if (gt2Belt(definition))
        {
            annotation { "Name" : "Belt supplier", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Default" : "CUSTOM" }
            definition.gt2BeltSupplier is Gt2BeltSupplier;
        }
        else if (htd9mmBelt(definition))
        {
            annotation { "Name" : "Belt supplier", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Default" : "CUSTOM" }
            definition.htd9mmBeltSupplier is Htd9mmBeltSupplier;
        }
        else if (htd15mmBelt(definition))
        {
            annotation { "Name" : "Belt supplier", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Default" : "CUSTOM" }
            definition.htd15mmBeltSupplier is Htd15mmBeltSupplier;
        }

        if (definition.hasSelections)
        {
            annotation { "Name" : "Use closest belt", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.autoBelt is boolean;
        }

        if (!autoBelt(definition))
        {
            if (gt2Belt(definition))
            {
                if (definition.gt2BeltSupplier == Gt2BeltSupplier.VEXPRO)
                {
                    annotation { "Name" : "VEXpro belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.vexproGt2Belt is VexproGt2Belt;
                }
                else if (definition.gt2BeltSupplier == Gt2BeltSupplier.REV)
                {
                    annotation { "Name" : "REV belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.revGt2Belt is RevGt2Belt;
                }
                else if (definition.gt2BeltSupplier == Htd9mmBeltSupplier.CUSTOM)
                {
                    annotation { "Name" : "Belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.gt2BeltTeeth, BELT_TEETH_BOUNDS);
                }
            }
            else if (htd9mmBelt(definition))
            {
                if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.VEXPRO)
                {
                    annotation { "Name" : "VEXpro belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.vexproHtd9mmBelt is VexproHtdBelt;
                }
                else if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.ANDYMARK)
                {
                    annotation { "Name" : "Andymark belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.andymarkHtd9mmBelt is AndymarkHtd9mmBelt;
                }
                else if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.CUSTOM)
                {
                    annotation { "Name" : "Belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.htd9mmBeltTeeth, BELT_TEETH_BOUNDS);
                }
            }
            else if (htd15mmBelt(definition))
            {
                if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.VEXPRO)
                {
                    annotation { "Name" : "VEXpro belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.vexproHtd15mmBelt is VexproHtdBelt;
                }
                else if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.ANDYMARK)
                {
                    annotation { "Name" : "Andymark belt", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.andymarkHtd15mmBelt is AndymarkHtd15mmBelt;
                }
                else if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.CUSTOM)
                {
                    annotation { "Name" : "Belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.htd15mmBeltTeeth, BELT_TEETH_BOUNDS);
                }
            }
        }
    }
}

function beltTable(definition is map)
{
    if (gt2Belt(definition))
    {
        if (definition.gt2BeltSupplier == Gt2BeltSupplier.VEXPRO)
        {
            return VexproGt2Belt;
        }
        else if (definition.gt2BeltSupplier == Gt2BeltSupplier.REV)
        {
            return RevGt2Belt;
        }
    }
    else if (htdBelt(definition))
    {
        if (definition.beltWidth == BeltWidth._9MM)
        {
            if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.VEXPRO)
            {
                return VexproHtdBelt;
            }
            else if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.ANDYMARK)
            {
                return AndymarkHtd15mmBelt;
            }
        }
        else if (definition.beltWidth == BeltWidth._15MM)
        {
            if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.VEXPRO)
            {
                return VexproHtdBelt;
            }
            else if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.ANDYMARK)
            {
                return AndymarkHtd15mmBelt;
            }
        }
    }
    return undefined;
}

function beltTeeth(definition is map) returns number
{
    var key;
    if (gt2Belt(definition))
    {
        if (definition.gt2BeltSupplier == Gt2BeltSupplier.VEXPRO)
        {
            key = "vexproGt2Belt";
        }
        else if (definition.gt2BeltSupplier == Gt2BeltSupplier.REV)
        {
            key = "revGt2Belt";
        }
        else if (definition.gt2BeltSupplier == Gt2BeltSupplier.CUSTOM)
        {
            return definition.gt2BeltTeeth;
        }
    }
    else if (htd9mmBelt(definition))
    {
        if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.VEXPRO)
        {
            key = "vexproHtd9mmBelt";
        }
        else if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.ANDYMARK)
        {
            key = "andymarkHtd9mmBelt";
        }
        else if (definition.htd9mmBeltSupplier == Htd9mmBeltSupplier.CUSTOM)
        {
            return definition.htd9mmBeltTeeth;
        }
    }
    else if (htd15mmBelt(definition))
    {
        if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.VEXPRO)
        {
            key = "vexproHtd15mmBelt";
        }
        else if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.ANDYMARK)
        {
            key = "andymarkHtd15mmBelt";
        }
        else if (definition.htd15mmBeltSupplier == Htd15mmBeltSupplier.CUSTOM)
        {
            return definition.htd15mmBeltTeeth;
        }
    }
    return extractNumber(definition[key]);
}

function beltPitchAndWidth(definition is map) returns map
{
    if (gt2Belt(definition))
    {
        return { "pitch" : 3 * millimeter, "width" : 9 * millimeter };
    }
    else if (htdBelt(definition))
    {
        return { "pitch" : 5 * millimeter, "width" : extractNumber(definition.beltWidth) * millimeter };
    }
}

predicate pulleyPredicate(definition is map)
{
    annotation { "Name" : "Pulley one teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    isInteger(definition.pulleyOneTeeth, PULLEY_TEETH_BOUNDS);

    annotation { "Name" : "Pulley two teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    isInteger(definition.pulleyTwoTeeth, PULLEY_TEETH_BOUNDS);

    annotation { "Name" : "Pulley one", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.hasPulleyOne is boolean;

    if (definition.hasPulleyOne)
    {
        annotation { "Group Name" : "Pulley one", "Collapsed By Default" : false, "Driving Parameter" : "hasPulleyOne" }
        {
            annotation { "Name" : "Pulley width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.pulleyOneWidth, PULLEY_WIDTH_BOUNDS);

            annotation { "Name" : "Bore type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyOneBoreType is BoreType;

            if (definition.pulleyOneBoreType == BoreType.HEX || definition.pulleyOneBoreType == BoreType.CIRCULAR)
            {
                annotation { "Name" : "Bore diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.pulleyOneBoreDiameter, PULLEY_BORE_BOUNDS);
            }
            else if (definition.pulleyOneBoreType == BoreType.GEAR)
            {
                annotation { "Name" : "Gear teeth" }
                isInteger(definition.pulleyOneGearTeeth, PULLEY_GEAR_TEETH_BOUNDS);
            }

            if (definition.pulleyOneBoreType != BoreType.NONE)
            {
                annotation { "Name" : "Chamfer bore edges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.pulleyOneChamferEdges is boolean;
            }

            annotation { "Name" : "Has flanges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyOneHasFlanges is boolean;

            if (definition.pulleyOneHasFlanges)
            {
                // annotation { "Name" : "Flange size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                // definition.pulleyOneFlangeSize is FlangeSize;
            }

            annotation { "Name" : "Engrave tooth count", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyOneEngraveToothCount is boolean;
        }
    }

    annotation { "Name" : "Pulley two", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.hasPulleyTwo is boolean;

    if (definition.hasPulleyTwo)
    {
        annotation { "Group Name" : "Pulley two", "Collapsed By Default" : false, "Driving Parameter" : "hasPulleyTwo" }
        {
            annotation { "Name" : "Pulley width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.pulleyTwoWidth, PULLEY_WIDTH_BOUNDS);

            annotation { "Name" : "Bore type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyTwoBoreType is BoreType;

            if (definition.pulleyTwoBoreType == BoreType.HEX || definition.pulleyTwoBoreType == BoreType.CIRCULAR)
            {
                annotation { "Name" : "Bore diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.pulleyTwoBoreDiameter, PULLEY_BORE_BOUNDS);
            }
            else if (definition.pulleyTwoBoreType == BoreType.GEAR)
            {
                annotation { "Name" : "Gear teeth" }
                isInteger(definition.pulleyTwoGearTeeth, PULLEY_GEAR_TEETH_BOUNDS);
            }

            if (definition.pulleyTwoBoreType != BoreType.NONE)
            {
                annotation { "Name" : "Chamfer bore edges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.pulleyTwoChamferEdges is boolean;
            }

            annotation { "Name" : "Has flanges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyTwoHasFlanges is boolean;

            if (definition.pulleyTwoHasFlanges)
            {
                // annotation { "Name" : "Flange size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                // definition.pulleyTwoFlangeSize is FlangeSize;
            }

            annotation { "Name" : "Engrave tooth count", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.pulleyTwoEngraveToothCount is boolean;
        }
    }
}

predicate optionsPredicate(definition is map)
{
    annotation { "Group Name" : "Options", "Collapsed By Default" : true }
    {
        if (definition.hasPulleyOne || definition.hasPulleyTwo)
        {
            annotation { "Name" : "Pulley teeth size adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Slightly adjusts the size of the teeth profile of the pulleys."
                    }
            isLength(definition.fitAdjustment, FIT_ADJUSTMENT_BOUNDS);

            annotation { "Name" : "Create composite part", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Creates a composite part containing the belt and pulleys." }
            definition.createComposite is boolean;
        }

        annotation { "Name" : "Create belt teeth", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.createBeltTeeth is boolean;

        annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.addMateConnectors is boolean;

        annotation { "Name" : "Center to center adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Adjusts the checked center to center distance. Useful for making slight adjustments to the tension of belts."
                }
        isLength(definition.centerToCenterAdjustment, CENTER_TO_CENTER_ADJUST_BOUNDS);
    }
}

annotation { "Feature Type Name" : "Robot belt",
        "Editing Logic Function" : "robotBeltEditLogic",
        "Icon" : icon::BLOB_DATA,
        "Feature Type Description" : "Create GT2 and Htd belts, position and size them automatically based on model geometry, and insert and modify custom 3D printable pulleys.<br>" ~
        "FeatureScript created by Alex Kempen."
    }
export const beltGenerator = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        selectionPredicate(definition);
        beltPredicate(definition);
        pulleyPredicate(definition);
        optionsPredicate(definition);
    }
    {
        doRobotBelt(context, id, definition);
    });

/**
 * Extracts the first number from `value` and returns it.
 */
function extractNumber(value is string) returns number
{
    // [^\\d] accounts for regex greedy match behavior
    const parsed = match(value, "[^\\d]*(\\d+).*");
    verify(parsed.hasMatch, ErrorStringEnum.INVALID_RESULT);
    return stringToNumber(parsed.captures[1]);
}

function doRobotBelt(context is Context, id is Id, definition is map)
{
    const beltDefinition = getBeltDefinition(context, id, definition);

    // center to center too small error
    // if ((pulleyOneRadius + pulleyTwoRadius) > centerToCenter)
    // {
    //     reportFeatureWarning(context, id, "The center to center distance of the belt (" ~ (roundToPrecision(centerToCenter / inch, 3)) ~ " inches) is too small for the selected pulleys.");
    // }

    const beltId = id + "belt";
    opBelt(context, beltId, {
                "beltDefinition" : beltDefinition,
                "createBeltTeeth" : definition.createBeltTeeth
            });
    const beltQuery = qCreatedBy(beltId, EntityType.BODY);

    return;

    const pulleyId = id + "pulleys";
    createPulleys(context, pulleyId, definition, beltDefinition.pulleyLocations);

    opDeleteBodies(context, id + "deleteSketches", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });

    // ---mate connectors---
    // create mate connectors on the belt if no pulley is being created
    if (definition.addMateConnectors)
    {
        for (var i, pulley in values(Pulley))
        {
            if (!hasPulley(definition, pulley))
            {
                opMateConnector(context, id + ("mateConnector" ~ i), {
                            "coordSystem" : beltDefinition.pulleyLocations[i],
                            "owner" : beltQuery
                        });
            }
        }
    }

    // create composite part
    if ((definition.hasPulleyOne || definition.hasPulleyTwo) && definition.createComposite)
    {
        var pulleyQuery = qCreatedBy(pulleyId, EntityType.BODY)->qBodyType(BodyType.SOLID);

        const compositeId = id + "composite";
        opCreateCompositePart(context, compositeId, { "bodies" : qUnion([beltQuery, pulleyQuery]) });

        setProperty(context, {
                    "entities" : qCreatedBy(compositeId, EntityType.BODY),
                    "propertyType" : PropertyType.NAME,
                    "value" : beltDefinition.pulleyTeeth[0] ~ ":" ~ beltDefinition.pulleyTeeth[1] ~ " " ~ definition.beltType ~ " Belt Run" //composite part name
                });
    }

    const remainingTransform = getRemainderPatternTransform(context, { "references" : qUnion([definition.firstPoint, definition.secondPoint]) });
    transformResultIfNecessary(context, id, remainingTransform);
}

function getBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    if (definition.hasSelections)
    {
        if (isQueryEmpty(context, qUnion(definition.firstPoint, definition.secondPoint)))
        {
            throw regenError("Select pulley positions.", ["firstPoint", "secondPoint"]);
        }
        verifyNonemptyQuery(context, definition, "firstPoint", "Select a position for pulley one.");
        verifyNonemptyQuery(context, definition, "secondPoint", "Select a position for pulley two.");
    }

    const beltPlane = beltPlane(context, definition);
    const secondPoint = secondPoint(context, definition, beltPlane);
    if (definition.hasSelections && tolerantEquals(beltPlane.origin, secondPoint))
    {
        throw regenError("The selected pulley locations cannot be coincident.",
            ["firstPoint", "secondPoint"], qUnion([definition.firstPoint, definition.secondPoint]));
    }

    const pulleyTeeth = getPulleyTeeth(definition);

    const pitchAndWidth = beltPitchAndWidth(definition);
    const pitch = pitchAndWidth.pitch;
    const width = pitchAndWidth.width;

    var teeth;
    if (autoBelt(definition))
    {
        const targetCenterToCenter = evDistance(context, {
                        "side0" : definition.firstPoint,
                        "side1" : definition.secondPoint
                    }).distance;
        teeth = computeBeltTeeth(definition, pitch, pulleyTeeth, targetCenterToCenter);
    }
    else
    {
        teeth = beltTeeth(definition);
    }

    var centerToCenter = beltCenterToCenter(pitch, teeth, pulleyTeeth) +
    definition.centerToCenterAdjustment;
    if (definition.hasSelections)
    {
        const selectionDistance = evDistance(context, {
                        "side0" : definition.firstPoint,
                        "side1" : definition.secondPoint
                    }).distance;

        if (centerToCenter - selectionDistance > 0.0005 * inch || centerToCenter - selectionDistance < -0.0005 * inch)
        {
            if (definition.autoBelt)
            {
                // No belt matches the distance between pulleys. The closest belt is 100T and has a (4.5 in) center to center distance.
                reportFeatureWarning(context, id, "No belt matches the distance between selections. The closest belt is " ~ teeth ~ "T and has a (" ~
                        roundToPrecision(centerToCenter / inch, 3) ~ " in) center to center distance.");
            }
            else
            {
                // const targetCenterToCenter = evDistance(context, {
                //                 "side0" : definition.firstPoint,
                //                 "side1" : definition.secondPoint
                //             }).distance;
                // const closestTeeth = computeBeltTeeth(definition, pitch, pulleyTeeth, targetCenterToCenter);
                // const closestCenterToCenter = beltCenterToCenter(pitch, teeth, pulleyTeeth) +
                //     definition.centerToCenterAdjustment;
                reportFeatureWarning(context, id, "The selected " ~ teeth ~ "T belt has a (" ~ roundToPrecision(centerToCenter / inch, 3) ~ " in) center to center distance, which does not match the distance between selections.");
            }
        }
        else
        {
            // The selected 100T belt matches the distance between selections (3.45 in)."
            reportFeatureInfo(context, id, "The selected " ~ teeth ~ "T belt matches the distance between selections (" ~ roundToPrecision(centerToCenter / inch, 3) ~ " in).");
        }
    }
    else
    {
        // The selected 100T belt has a (3.45 in) center to center distance.
        reportFeatureInfo(context, id, "The selected " ~ teeth ~ "T belt has a (" ~ roundToPrecision(centerToCenter / inch, 3) ~ " in) center to center distance.");
    }

    // round centerToCenter to 0.001 when drawing belt to match requested selectionDistance
    centerToCenter = roundToPrecision(centerToCenter / inch, 3) * inch;

    const pulleyDefinitions = [
            {
                    "location" : zeroVector(2) * meter,
                    "identity" : (definition.hasSelections ? definition.firstPoint : undefined),
                    "radius" : pulleyRadius(pitch, pulleyTeeth[0]),
                    "flipped" : false
                } as PulleyDefinition,
            {
                    "location" : worldToPlane(beltPlane, secondPoint)->normalize() * centerToCenter,
                    "identity" : (definition.hasSelections ? definition.secondPoint : undefined),
                    "radius" : pulleyRadius(pitch, pulleyTeeth[1]),
                    "flipped" : false
                } as PulleyDefinition
        ];

    return {
                "beltPlane" : beltPlane,
                "pitch" : pitch,
                "width" : width,
                "teeth" : teeth,
                "pulleyDefinitions" : pulleyDefinitions
            } as BeltDefinition;
}

function beltPlane(context is Context, definition is map) returns Plane
{
    return (definition.hasSelections) ? evVertexCoordSystem(context, { "vertex" : definition.firstPoint })->plane() : XY_PLANE;
}

function secondPoint(context is Context, definition is map, beltPlane is Plane)
{
    if (definition.hasSelections)
    {
        if (!isQueryEmpty(context, qEntityFilter(definition.secondPoint, EntityType.VERTEX)))
        {
            return project(beltPlane, evVertexCoordSystem(context, { "vertex" : definition.secondPoint }).origin);
        }
        else
        {
            const axis = evAxis(context, { "axis" : definition.secondPoint });
            const axisIntersection = intersection(beltPlane, axis);
            if (axisIntersection.dim == 1)
            {
                throw regenError("The selected axis is collinear with the belt's plane.",
                    ["secondPoint"], definition.secondPoint);
            }
            else if (axisIntersection.dim == -1)
            {
                throw regenError("The selected axis does not intersect the belt's plane.",
                    ["secondPoint"], definition.secondPoint);
            }
            return axisIntersection.intersection;
        }
    }
    return beltPlane.origin + beltPlane.x * meter;
}

function computeBeltTeeth(definition is map, pitch is ValueWithUnits, pulleyTeeth is array, targetCenterToCenter is ValueWithUnits) returns number
{
    const calculatedTeeth = beltTeeth(pitch, pulleyTeeth, targetCenterToCenter);

    const table = beltTable(definition);
    if (table == undefined)
    {
        return round(calculatedTeeth);
    }
    const beltArray = mapArray(values(table), function(value is string)
            {
                return extractNumber(value);
            })->sort(function(a, b)
        {
            return a - b;
        });

    var bestBelt;
    for (var belt in beltArray)
    {
        if (belt <= calculatedTeeth)
        {
            bestBelt = belt;
        }
        else // belt is larger; terminate search
        {
            // smaller belt, larger belt
            if (abs(bestBelt - calculatedTeeth) >= abs(belt - calculatedTeeth))
            {
                bestBelt = belt;
            }
            break;
        }
    }
    return bestBelt;
}

function getPulleyTeeth(definition is map) returns array
{
    return mapArray(values(Pulley), function(pulley is Pulley)
        {
            return definition[pulleyString(pulley) ~ "Teeth"];
        });
}

enum Pulley
{
    ONE,
    TWO
}

predicate hasPulley(definition is map, pulley is Pulley)
{
    definition["hasPulley" ~ (pulley == Pulley.ONE ? "One" : "Two")];
}

/**
 * @param pulley : @autocomplete `pulley`
 */
function pulleyString(pulley is Pulley) returns string
{
    return "pulley" ~ (pulley == Pulley.ONE ? "One" : "Two");
}

function pulleyRadius(pitch is ValueWithUnits, pulleyTeeth is number)
{
    return pulleyTeeth * pitch / PI / 2;
}

function pulleyDiameter(pitch is ValueWithUnits, pulleyTeeth is number)
{
    return pulleyRadius(pitch, pulleyTeeth) * 2;
}

function beltCenterToCenter(pitch is ValueWithUnits, teeth is number, pulleyTeeth is array) returns ValueWithUnits
precondition
{
    size(pulleyTeeth) == 2;
}
{
    const pulleyDiameters = pulleyDiameters(pitch, pulleyTeeth);
    const largePulleyDiameter = max(pulleyDiameters);
    const smallPulleyDiameter = min(pulleyDiameters);

    const term = (teeth * pitch - (PI / 2 * (largePulleyDiameter + smallPulleyDiameter))) / 4;
    return term + sqrt(term ^ 2 - ((largePulleyDiameter - smallPulleyDiameter) ^ 2) / 8);
}

function beltTeeth(pitch is ValueWithUnits, pulleyTeeth is array, targetCenterToCenter is ValueWithUnits) returns number
precondition
{
    size(pulleyTeeth) == 2;
}
{
    const pulleyDiameters = pulleyDiameters(pitch, pulleyTeeth);
    const largePulleyDiameter = max(pulleyDiameters);
    const smallPulleyDiameter = min(pulleyDiameters);

    return (largePulleyDiameter ^ 2 - 2 * largePulleyDiameter * smallPulleyDiameter + smallPulleyDiameter ^ 2 +
                8 * targetCenterToCenter ^ 2 + 2 * largePulleyDiameter * targetCenterToCenter * PI + 2 * smallPulleyDiameter * targetCenterToCenter * PI) /
        (4 * targetCenterToCenter * pitch);
}

function getPulleyConfigurations(definition is map, pulleyString is string) returns map
{
    var pulleyConfiguration = {
        "beltType" : definition.beltType,
        "Size_Mod" : definition.fitAdjustment,
        "teeth" : definition[pulleyString ~ "Teeth"],
        "width" : definition[pulleyString ~ "Width"],
        "hasFlanges" : definition[pulleyString ~ "HasFlanges"]
    };

    if (pulleyConfiguration.hasFlanges)
    {
        // pulleyConfiguration = mergeMaps(FLANGE_SIZE_MAP[definition[pulleyString ~ "FlangeSize"]], pulleyConfiguration);
    }

    const hasBore = definition[pulleyString ~ "BoreType"] != BoreType.NONE;
    var boreConfiguration;
    if (hasBore)
    {
        const pulleyWidth = pulleyConfiguration.width; // + (pulleyConfiguration.hasFlanges ? pulleyConfiguration.flangeWidth * 2 : 0 * meter);
        boreConfiguration = {
                "boreType" : definition[pulleyString ~ "BoreType"],
                "width" : pulleyWidth,
                "depth" : pulleyWidth,
                "diameter" : definition[pulleyString ~ "BoreDiameter"],
                "gearTeeth" : definition[pulleyString ~ "GearTeeth"],
                "chamferEdges" : definition[pulleyString ~ "ChamferEdges"]
            };
    }

    return {
            "pulley" : pulleyConfiguration,
            "hasBore" : hasBore,
            "bore" : boreConfiguration,
        };
}

function createPulleys(context is Context, id is Id, definition is map, pulleyLocations is array)
{
    // iterate over the Pulley enum
    for (var i, pulley in values(Pulley))
    {
        if (hasPulley(definition, pulley))
        {
            const pulleyString = pulleyString(pulley);
            const pulleyId = id + pulleyString;
            const addPulleyFunction = getAddPulleyFunction(context, definition, pulleyString, pulleyLocations[i]);

            const result = addPulleyFunction(pulleyId);

            if (result.configurations.hasBore)
            {
                opBoolean(context, pulleyId + "boolean", {
                            "tools" : result.boreQuery,
                            "targets" : result.pulleyQuery,
                            "operationType" : BooleanOperationType.SUBTRACTION
                        });

                if (result.configurations.bore.chamferEdges)
                {
                    try
                    {
                        const edges = qCreatedBy(pulleyId + "boolean", EntityType.EDGE);
                        const chamferEdges = edges->qSubtraction(edges->qParallelEdges(pulleyLocations[i].zAxis));
                        opChamfer(context, pulleyId + "chamfer", {
                                    "entities" : chamferEdges,
                                    "chamferType" : ChamferType.EQUAL_OFFSETS,
                                    "width" : 0.6 * millimeter
                                });
                    }
                }

                if (result.configurations.bore.boreType == BoreType.FALCON_SPLINE)
                {
                    try
                    {
                        const edges = qCreatedBy(pulleyId + "boolean", EntityType.EDGE);
                        const filletEdges = edges->qParallelEdges(pulleyLocations[i].zAxis);
                        opFillet(context, pulleyId + "fillet", {
                                    "entities" : filletEdges,
                                    "radius" : 0.1 * millimeter
                                });
                    }
                }
            }

            if (size(evaluateQuery(context, result.pulleyQuery)) != 1)
            {
                const errorId = id + "errorEntities";
                const errorResult = addPulleyFunction(errorId);
                if (errorResult.configurations.hasBore)
                {
                    addDebugEntities(context, errorResult.boreQuery, DebugColor.BLUE);
                }
                throw regenError("Failed to create pulleys. Check input.", errorResult.pulleyQuery);
            }
            setPulleyProperties(context, result.configurations, result.pulleyQuery);
        }
    }
}

/**
 * Generates a function to add the pulley.
 * Used to enable reconstrucing the pulley in case of error.
 * @returns {function} : A function which takes in an `id` and generates a given pulley.
 *      The result of the function is a map which has the fields `configurations`, `pulleyQuery`, and `boreQuery`.
 */
function getAddPulleyFunction(context is Context, definition is map, pulleyString is string, location is CoordSystem) returns function
{
    return function(id is Id)
        {
            const instantiator = newInstantiator(id + pulleyString, {
                        "partQuery" : qEverything(EntityType.BODY)->qBodyType(BodyType.SOLID)
                    });

            const configurations = getPulleyConfigurations(definition, pulleyString);
            const pulleyQuery = addInstance(instantiator, Pulley::build, {
                        "configuration" : configurations.pulley,
                        "transform" : toWorld(location),
                        "name" : pulleyString,
                    // "identity" : pulleyDefinitions[i].identity
                    });

            var boreQuery;
            if (configurations.hasBore)
            {
                boreQuery = addInstance(instantiator, Bore::build, {
                            "configuration" : configurations.bore,
                            "transform" : toWorld(location),
                            "name" : pulleyString ~ "Bore",
                        // "identity" : pulleyDefinitions[i].identity
                        });
            }
            instantiate(context, instantiator);

            return {
                    "configurations" : configurations,
                    "pulleyQuery" : pulleyQuery,
                    "boreQuery" : boreQuery
                };
        };
}

function setPulleyProperties(context is Context, configurations is map, pulleyQuery is Query)
{
    setProperty(context, {
                "entities" : pulleyQuery,
                "propertyType" : PropertyType.NAME,
                "value" : configurations.pulley.teeth ~ "T Custom Pulley"
            });

    setProperty(context, {
                "entities" : pulleyQuery,
                "propertyType" : PropertyType.MATERIAL,
                "value" : material("Onyx", 1.18 * gram / centimeter ^ 3) // custom pulley material and density
            });

    setProperty(context, {
                "entities" : pulleyQuery,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : color(0.3, 0.3, 0.3) // custom pulley color (RGB value, x/255)
            });
}

export function robotBeltEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    // update the widths of the pulleys when the beltType is changed
    // if (oldDefinition == {} || oldDefinition.beltType != definition.beltType)
    // {
    //     if (definition.beltType == BeltType.GT2)
    //     {
    //         definition.pulleyOneWidth = defaultGT2PulleyWidth;
    //         definition.pulleyTwoWidth = defaultGT2PulleyWidth;
    //     }
    //     else if (definition.beltType == BeltType.HTD)
    //     {
    //         if (definition.beltWidth == BeltWidth._9MM)
    //         {
    //             definition.pulleyOneWidth = default9mmHtdPulleyWidth;
    //             definition.pulleyTwoWidth = default9mmHtdPulleyWidth;
    //         }
    //         else if (definition.beltWidth == BeltWidth._15MM)
    //         {
    //             definition.pulleyOneWidth = default15mmHtdPulleyWidth;
    //             definition.pulleyTwoWidth = default15mmHtdPulleyWidth;
    //         }
    //     }
    // }
    return definition;
}
