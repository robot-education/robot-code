FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
export import(path : "core/bearing.fs", version : "");
export import(path : "released/bearing/robotBearingTables.gen.fs", version : "");
// FRCDesign's bearings part set: its bearings' sizes and bores
import(path : "bearings/bearings.fs", version : "");

/** As std's hole's Style: a flanged bearing's flange sits on the face, or in a counterbore. */
export enum BoreStyle
{
    annotation { "Name" : "Simple" }
    SIMPLE,
    annotation { "Name" : "Counterbore" }
    COUNTERBORE
}

/**
 * The dialog follows std's hole's order: Style, the sketch points and merge scope, what goes in the holes, where they
 * start and which way they go, then the counterbore.
 */
annotation { "Feature Type Name" : "Robot bearing",
        "Feature Type Description" : "Cut bores for bearings." ~ CREDIT,
        "Manipulator Change Function" : "robotBearingManipulatorChange",
        "Editing Logic Function" : "robotBearingEditLogic",
        "Auto Assembly" : true,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBearing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Set by editing logic, as conditions can't read lookup tables
        annotation { "Name" : "Flanged", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isFlangedBearing" }
        definition.flanged is boolean;

        if (definition.flanged)
        {
            annotation { "Name" : "Style", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
            definition.boreStyle is BoreStyle;
        }

        bearingLocationPredicate(definition, "bearings");

        annotation { "Name" : "Bearing", "Lookup Table" : robotBearingTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bearing is LookupTablePath;

        boreFitPredicate(definition);

        bearingOrientationPredicate(definition);

        if (definition.flanged && definition.boreStyle == BoreStyle.COUNTERBORE)
        {
            annotation { "Name" : "Counterbore diameter", "UIHint" : ["SHOW_EXPRESSION"] }
            isLength(definition.counterboreDiameter, HOLE_DIAMETER_BOUNDS);

            annotation { "Name" : "Counterbore depth", "UIHint" : ["SHOW_EXPRESSION"] }
            isLength(definition.counterboreDepth, HOLE_DEPTH_BOUNDS);
        }
    }
    {
        const part = libraryBearing(definition);
        const planes = bearingPlanes(context, id, definition, part);
        var bore = {
            "diameter" : part.outerDiameter + boreFitClearance(definition, part.outerDiameter),
            "autoAssemblyBody" : false
        };
        if (part.flangeDiameter != undefined && definition.boreStyle == BoreStyle.COUNTERBORE)
        {
            if (!tolerantGreaterThan(definition.counterboreDiameter, bore.diameter))
            {
                throw regenError("The counterbore must be wider than the bore.", ["counterboreDiameter"]);
            }
            bore.counterboreDiameter = definition.counterboreDiameter;
            bore.counterboreDepth = definition.counterboreDepth;
        }
        placeBearings(context, id + "place", definition, planes, part, bore, AutoAssemblyKind.BEARING);
    });

/**
 * The bearing chosen, as FRCDesign's bearings part set makes it: its sizes and flange from the set's `BEARINGS`, and its
 * hole at its bore's corners (`boreRadius`).
 */
function libraryBearing(definition is map) returns BearingPart
{
    const entry = getLookupTable(robotBearingTable, definition.bearing);
    if (entry.product == undefined)
    {
        throw regenError("Choose a different bearing.", ["bearing"]);
    }
    const made = BEARINGS[entry.vendor][entry.bearingType][entry.bore][entry.size][entry.dimensions][entry.seal];
    var part = {
        "outerDiameter" : made.outerDiameter,
        "width" : made.width,
        "innerDiameter" : 2 * boreRadius(BEARING_BORES[entry.bore][entry.size]),
        "product" : entry.product
    };
    if (entry.bearingType != BearingType.RADIAL)
    {
        part.flangeDiameter = made.flangeDiameter;
        part.flangeThickness = made.flangeThickness;
    }
    return part as BearingPart;
}

/**
 * Flanged: whether the bearing chosen has a flange to counterbore, which shows Style.
 */
export function isFlangedBearing(definition is map) returns boolean
{
    const entry = getLookupTable(robotBearingTable, definition.bearing);
    return entry.bearingType != undefined && entry.bearingType != BearingType.RADIAL;
}

/**
 * A counterbore as wide as the flange, with Bore fit's clearance, and as deep as it's thick, so it sits flush.
 */
export function flangeCounterbore(definition is map) returns map
{
    const entry = getLookupTable(robotBearingTable, definition.bearing);
    const made = BEARINGS[entry.vendor][entry.bearingType][entry.bore][entry.size][entry.dimensions][entry.seal];
    return {
            "diameter" : made.flangeDiameter + boreFitClearance(definition, made.flangeDiameter),
            "depth" : made.flangeThickness
        };
}

export function robotBearingManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return bearingManipulatorChange(definition, newManipulators);
}

/**
 * Sets Flanged; and when the bearing, its fit or Style changes, the counterbore to the flange's (`flangeCounterbore`), as
 * std's hole sets its from its standard.
 */
export function robotBearingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.flanged = isFlangedBearing(definition);
    const changed = oldDefinition == {} || oldDefinition.bearing != definition.bearing || oldDefinition.boreFit != definition.boreFit ||
        oldDefinition.boreFitClearance != definition.boreFitClearance || oldDefinition.boreStyle != definition.boreStyle;
    if (definition.flanged && changed)
    {
        const counterbore = flangeCounterbore(definition);
        definition.counterboreDiameter = counterbore.diameter;
        definition.counterboreDepth = counterbore.depth;
    }
    return bearingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
