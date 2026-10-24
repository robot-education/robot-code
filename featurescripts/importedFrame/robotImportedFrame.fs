FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotFrameIcon::import(path : "94bcdc0a50ec32f81a3aa449", version : "0ee450364120d3c453712009");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "ff444db0395e01aaa8c7e555", version : "87aa3665d5a93d91331dbe19");
import(path : "released/frame/robotFrame.fs", version : "");
import(path : "importedFrame/frameModels.gen.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "439d5a6da5926ffd182185cd");

const IMPORTED_START_BOUNDS = { (meter) : [0, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;

/**
 * A variant of Robot frame which imports a model of the frame (a STEP tab, see frameModels.gen.fs) and cuts it to length,
 * rather than building its tube and cutting its holes. Only COTS frames have models, with holes as they're sold.
 */
annotation { "Feature Type Name" : "Robot imported frame",
        "Feature Type Description" : "Add COTS tube, channel, angle, or extrusion along an edge, or extrude it from a point, from a model of it." ~ CREDIT,
        "Manipulator Change Function" : "stockManipulatorChange",
        "Editing Logic Function" : "robotImportedFrameEditLogic",
        "Icon" : RobotFrameIcon::BLOB_DATA
    }
export const robotImportedFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Group Name" : "Frame", "Collapsed By Default" : false }
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "Frame", "Lookup Table" : frcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcFrame is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Frame", "Lookup Table" : ftcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcFrame is LookupTablePath;
            }

            // Set to where the frame's holes start as it's sold by editing logic
            annotation { "Name" : "Hole start distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The distance from the start of the frame to its first hole." }
            isLength(definition.holeStart, IMPORTED_START_BOUNDS);
        }

        stockLocationPredicate(definition, "frame");

        tieHolesPredicate(definition);
    }
    {
        placeStock(context, id, definition, getImportedFrame(definition), "frame");
    });

/**
 * The selected frame's entry in its lookup table.
 */
function importedFrameEntry(definition is map) returns map
{
    return isFrc(definition) ?
        getLookupTable(frcFrameTable, definition.frcFrame) :
        getLookupTable(ftcFrameTable, definition.ftcFrame);
}

/**
 * The selected frame as stock, built from its model (see `FRAME_MODELS`): its entry, with its rows of holes, and its
 * model's, moved along so the first hole which counts is Hole start distance from the start.
 */
export function getImportedFrame(definition is map) returns Stock
{
    const entry = importedFrameEntry(definition);
    const shift = definition.holeStart - entry.tieStart;
    return mergeMaps(entry, {
                "xRows" : holeRows(entry.xRows, shift),
                "yRows" : holeRows(entry.yRows, shift),
                "isFrame" : true,
                "tieStart" : entry.tieStart + shift,
                "model" : mergeMaps(FRAME_MODELS[entry.partName], { "shift" : shift })
            }) as Stock;
}

/**
 * @internal
 * The editing logic function for Robot imported frame. When the frame changes (or the feature is created), sets Hole
 * start distance to where the frame's holes start as it's sold, unless it's been set.
 */
export function robotImportedFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    var changed = false;
    for (var parameter in ["program", "frcFrame", "ftcFrame"])
    {
        changed = changed || oldDefinition[parameter] != definition[parameter];
    }
    if (changed && !(specifiedParameters.holeStart ?? false))
    {
        definition.holeStart = importedFrameEntry(definition).tieStart;
    }
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}
