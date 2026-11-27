FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
export import(path : "core/bearing.fs", version : "");
export import(path : "released/bearing/robotBearingTables.gen.fs", version : "");
// FRCDesign's bearings part set: its bearings' sizes and bores
import(path : "bearings/bearings.fs", version : "");

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
        annotation { "Group Name" : "Bearing", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Bearing", "Lookup Table" : robotBearingTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.bearing is LookupTablePath;

            boreFitPredicate(definition);
        }

        annotation { "Group Name" : "Position", "Collapsed By Default" : false }
        {
            bearingPositionPredicate(definition, "bearings");
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            autoAssemblyBodyPredicate(definition);
        }
    }
    {
        const part = libraryBearing(definition);
        const planes = bearingPlanes(context, id, definition, part);
        placeBearings(context, id + "place", definition, planes, part, boreFitClearance(definition, part.outerDiameter), AutoAssemblyKind.BEARING);
    });

/**
 * The bearing chosen, as FRCDesign's bearings part set makes it: its sizes and flange from the set's `BEARINGS`, and its
 * body's hole at its bore's corners (`boreRadius`).
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

export function robotBearingManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return bearingManipulatorChange(definition, newManipulators);
}

export function robotBearingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    return bearingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
