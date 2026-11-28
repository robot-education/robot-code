FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
export import(path : "core/bearing.fs", version : "");
import(path : "released/bearing/robotBearingTables.gen.fs", version : "");
// FRCDesign's bushings part set: its bushings' sizes, and the bearings' bore radius
import(path : "bushings/bushings.fs", version : "");
import(path : "bearings/bearings.fs", version : "");

/**
 * Cuts bores for bushings, and declares each for Auto Assembly, with an Auto Assembly body: Robot bearing's placement.
 */
annotation { "Feature Type Name" : "Robot bushing",
        "Feature Type Description" : "Cut bores for bushings." ~ CREDIT,
        "Editing Logic Function" : "robotBushingEditLogic",
        "Auto Assembly" : true,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBushing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        bearingLocationPredicate(definition, "bushings");

        annotation { "Name" : "Bushing", "Lookup Table" : bushingTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bushing is LookupTablePath;

        bearingDirectionPredicate(definition);

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            autoAssemblyBodyPredicate(definition);
        }
    }
    {
        const part = libraryBushing(definition);
        const planes = bearingPlanes(context, definition);
        // A COTS bushing is pressed in: its bore is its outside diameter
        placeBearings(context, id + "place", definition, planes, part, {
                    "diameter" : part.outerDiameter,
                    "autoAssemblyBody" : definition.autoAssemblyBody
                }, AutoAssemblyKind.BUSHING);
    });

/**
 * The bushing chosen, as FRCDesign's bushings part set makes it: its sizes and flange from the set's `BUSHINGS`,
 * and its body's hole at its bore's corners (`boreRadius`).
 */
function libraryBushing(definition is map) returns BearingPart
{
    const entry = getLookupTable(bushingTable, definition.bushing);
    if (entry.product == undefined)
    {
        throw regenError("Choose a different bushing.", ["bushing"]);
    }
    const made = BUSHINGS[entry.vendor][entry.bore][entry.size][entry.material][entry.dimensions];
    var part = {
        "outerDiameter" : made.outerDiameter,
        "width" : made.width,
        "innerDiameter" : 2 * boreRadius(BUSHING_BORES[entry.bore][entry.size]),
        "flangeDiameter" : made.flangeDiameter,
        "flangeThickness" : made.flangeThickness,
        "product" : entry.product
    };
    return part as BearingPart;
}

export function robotBushingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    return bearingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
