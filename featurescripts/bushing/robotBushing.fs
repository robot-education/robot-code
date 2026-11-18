FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
export import(path : "core/bearing.fs", version : "");
import(path : "released/bearing/bearingTables.gen.fs", version : "");

/**
 * Cuts a bore for a bushing, and declares it for Auto Assembly, with a stand-in: Robot bearing's placement, without its
 * retention.
 */
annotation { "Feature Type Name" : "Robot bushing",
        "Feature Type Description" : "Cut a bore for a bushing." ~ CREDIT,
        "Manipulator Change Function" : "robotBushingManipulatorChange",
        "Editing Logic Function" : "robotBushingEditLogic",
        "Auto Assembly" : true,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBushing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Bushing", "Lookup Table" : bushingTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bushing is LookupTablePath;

        annotation { "Group Name" : "Position", "Collapsed By Default" : false }
        {
            bearingPositionPredicate(definition, "bushing");
        }

        annotation { "Group Name" : "Bore", "Collapsed By Default" : false }
        {
            bearingBorePredicate(definition);
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            standInPredicate(definition);
        }
    }
    {
        const part = getLookupTable(bushingTable, definition.bushing);
        if (!canBeBearingPart(part))
        {
            throw regenError("Choose a different bushing.", ["bushing"]);
        }
        const plane = bearingPlane(context, id, definition, part as BearingPart);
        placeBearing(context, id + "place", definition, plane, part as BearingPart, AutoAssemblyKind.BUSHING);
    });

export function robotBushingManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return bearingManipulatorChange(definition, newManipulators);
}

export function robotBushingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    return mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
