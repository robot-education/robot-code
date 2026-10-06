FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotNutStripIcon::import(path : "70fa8905356b9bb43809037c", version : "723096218434914d6c71e0ec");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "e316d3a31f8726cbc70fe081", version : "f8720e7e64d2b639cd3f03d8");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "3e1a1a3e6b103a1480625571");

/**
 * Places a nut strip along an edge, or extrudes one from a point.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add a nut strip along an edge, such as an inside edge of tube, or extrude one from a point." ~ CREDIT,
        "Manipulator Change Function" : "stockManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic",
        "Icon" : RobotNutStripIcon::BLOB_DATA
    }
export const robotNutStrip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Group Name" : "Nut strip", "Collapsed By Default" : false }
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "Nut strip", "Lookup Table" : frcNutStripTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcNutStrip is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Nut strip", "Lookup Table" : ftcNutStripTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcNutStrip is LookupTablePath;
            }
        }

        stockLocationPredicate(definition, "nut strip");

        tieHolesPredicate(definition);
    }
    {
        placeStock(context, id, definition, getNutStrip(definition), "nut strip");
    });

/**
 * The selected nut strip (see nutStripTables.py), as stock: a solid bar with its X and Y rows of tapped holes, and its
 * center hole if it has one.
 */
function getNutStrip(definition is map) returns Stock
{
    const nutStrip = isFrc(definition) ?
        getLookupTable(frcNutStripTable, definition.frcNutStrip) :
        getLookupTable(ftcNutStripTable, definition.ftcNutStrip);
    return {
            // e.g. Nut Strip (WCP 1/2 in., #10-32)
            "partName" : "Nut Strip (" ~ nutStrip.vendor ~ " " ~ nutStrip.sizeName ~ ", " ~ nutStrip.threadName ~ ")",
            "vendor" : nutStrip.vendor,
            "url" : nutStrip.url,
            "appearance" : nutStrip.appearance,
            "stock" : nutStrip.stock,
            "width" : nutStrip.width,
            "height" : nutStrip.height,
            "solid" : true,
            "holeDiameter" : nutStrip.tapDrillDiameter,
            "xRows" : [holeRow(nutStrip.xHoleStart, nutStrip.spacing)],
            "yRows" : [holeRow(nutStrip.yHoleStart, nutStrip.spacing)],
            "centerHole" : nutStrip.centerHole ?? false,
            "thread" : nutStrip,
            // Each hole counts, in either row
            "tieStart" : min(nutStrip.xHoleStart, nutStrip.yHoleStart),
            "tieUnit" : tolerantEquals(nutStrip.xHoleStart, nutStrip.yHoleStart) ? nutStrip.spacing : abs(nutStrip.xHoleStart - nutStrip.yHoleStart)
        } as Stock;
}

/**
 * @internal
 * The editing logic function for robot nut strip. Each end's offset defaults to its distance to the closest hole.
 */
export function robotNutStripEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (oldDefinition == {})
    {
        return definition;
    }
    const changed = oldDefinition.program != definition.program ||
        oldDefinition.frcNutStrip != definition.frcNutStrip ||
        oldDefinition.ftcNutStrip != definition.ftcNutStrip;
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies,
        getNutStrip(definition).tieStart, changed);
}
