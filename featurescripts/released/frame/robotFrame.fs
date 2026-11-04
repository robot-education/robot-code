FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotFrameIcon::import(path : "94bcdc0a50ec32f81a3aa449", version : "0ee450364120d3c453712009");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "2e446fc39b56fd5bff8ff054", version : "67a499eda38c0e000182de1b");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
import(path : "ff444db0395e01aaa8c7e555", version : "3f036ef981a0c3e4c7745dde");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "c7c8c6ed9437957b04b6e3da");

/**
 * Whether a frame is one someone sells, or custom: a common size of tube or angle, with holes as the feature says
 * (see frameTables.py).
 */
export enum FrameSource
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Custom" }
    CUSTOM
}

/**
 * Whether the frame is custom, which only FRC frames can be for now.
 */
export predicate isCustomFrame(definition is map)
{
    definition.program == Program.FRC;
    definition.source == FrameSource.CUSTOM;
}

const CUSTOM_HOLE_DIAMETER = 0.196 * inch;

const WALL_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 2.5 } as LengthBoundSpec;
const SPACING_BOUNDS = { (meter) : [1e-5, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;
const START_BOUNDS = { (meter) : [0, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;
const HOLE_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.0049784, 500], (inch) : 0.196, (millimeter) : 4 } as LengthBoundSpec;
const WIDTH_BOUNDS = { (meter) : [1e-5, 0.0508, 500], (inch) : 2, (millimeter) : 50 } as LengthBoundSpec;
const HEIGHT_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;
const ROWS_BOUNDS = { (unitless) : [1, 1, 100] } as IntegerBoundSpec;

/**
 * Places tube, channel, angle, or extrusion along an edge, or extrudes it from a point, tagged as a frame so the std
 * frame features work with it.
 */
annotation { "Feature Type Name" : "Robot frame",
        "Feature Type Description" : "Add tube, channel, angle, or extrusion along an edge, or extrude it from a point." ~ CREDIT,
        "Manipulator Change Function" : "stockManipulatorChange",
        "Editing Logic Function" : "robotFrameEditLogic",
        "Icon" : RobotFrameIcon::BLOB_DATA
    }
export const robotFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        if (isFrc(definition))
        {
            annotation { "Name" : "Source", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
            definition.source is FrameSource;
        }

        annotation { "Group Name" : "Frame", "Collapsed By Default" : false }
        {
            if (isCustomFrame(definition))
            {
                annotation { "Name" : "Frame", "Lookup Table" : customFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.customFrame is LookupTablePath;

                // Set by editing logic, as Onshape can't show parameters by a lookup table's value
                annotation { "Name" : "Custom size", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isCustomSizeFrame" }
                definition.customSize is boolean;

                annotation { "Name" : "No top holes", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isNoTopHolesFrame" }
                definition.noTopHoles is boolean;

                annotation { "Name" : "No side holes", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isNoSideHolesFrame" }
                definition.noSideHoles is boolean;

                if (definition.customSize)
                {
                    annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.width, WIDTH_BOUNDS);

                    annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.height, HEIGHT_BOUNDS);

                    annotation { "Name" : "Add top holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.topHoles is boolean;

                    if (definition.topHoles)
                    {
                        annotation { "Group Name" : "Add top holes", "Collapsed By Default" : false, "Driving Parameter" : "topHoles" }
                        {
                            annotation { "Name" : "Top rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                            isInteger(definition.topRows, ROWS_BOUNDS);

                            annotation { "Name" : "Top row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                            isLength(definition.topRowSpacing, SPACING_BOUNDS);
                        }
                    }

                    annotation { "Name" : "Add side holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.sideHoles is boolean;

                    if (definition.sideHoles)
                    {
                        annotation { "Group Name" : "Add side holes", "Collapsed By Default" : false, "Driving Parameter" : "sideHoles" }
                        {
                            annotation { "Name" : "Side rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                            isInteger(definition.sideRows, ROWS_BOUNDS);

                            annotation { "Name" : "Side row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                            isLength(definition.sideRowSpacing, SPACING_BOUNDS);
                        }
                    }
                }
                else
                {
                    // A size from the table: its rows are the table's
                    if (!definition.noTopHoles)
                    {
                        annotation { "Name" : "Top row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.tableTopRowSpacing, SPACING_BOUNDS);
                    }

                    if (!definition.noSideHoles)
                    {
                        annotation { "Name" : "Side row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.tableSideRowSpacing, SPACING_BOUNDS);
                    }
                }

                annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.wallThickness, WALL_BOUNDS);

                if (!(definition.noTopHoles && definition.noSideHoles))
                {
                    annotation { "Name" : "Distance between holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.holeSpacing, SPACING_BOUNDS);
                }
            }
            else if (isFrc(definition))
            {
                annotation { "Name" : "Frame", "Lookup Table" : frcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcFrame is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Frame", "Lookup Table" : ftcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcFrame is LookupTablePath;
            }

            if (!(isCustomFrame(definition) && definition.noTopHoles && definition.noSideHoles))
            {
                // Set to the frame's by editing logic. FTC frames' holes are as they're sold
                if (isFrc(definition))
                {
                    annotation { "Name" : "Hole diameter" }
                    isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
                }

                // Set to a COTS frame's (where its holes start as it's sold) by editing logic
                annotation { "Name" : "Hole start distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The distance from the start of the frame to its first hole." }
                isLength(definition.holeStart, START_BOUNDS);
            }
        }

        stockLocationPredicate(definition, "frame");

        tieHolesPredicate(definition);
    }
    {
        verifyFrame(definition);
        placeStock(context, id, definition, getFrame(definition), "frame", buildCutStock);
    });

/**
 * The selected frame as stock: its entry in its lookup table (see frameTables.py), or a custom one, with holes as big
 * as `holeDiameter` (an FTC frame's are as it's sold).
 */
function getFrame(definition is map) returns Stock
{
    if (!isCustomFrame(definition))
    {
        const entry = frameTableEntry(definition);
        // Every row (and the holes counted for tying them to the end) moves along it, so the first hole which counts is
        // Hole start distance from the start
        const shift = definition.holeStart - entry.tieStart;
        return mergeMaps(entry, {
                    "holeDiameter" : isFrc(definition) ? definition.holeDiameter : entry.holeDiameter,
                    "xRows" : holeRows(entry.xRows, shift),
                    "yRows" : holeRows(entry.yRows, shift),
                    "isFrame" : true,
                    "tieStart" : entry.tieStart + shift
                }) as Stock;
    }
    const size = customFrameSize(definition);
    return {
            // e.g. 2x1 Tube (Custom, 0.0625 in. wall)
            "partName" : roundToPrecision(size.width / inch, 3) ~ "x" ~ roundToPrecision(size.height / inch, 3) ~
                (size.angle ? " Angle" : " Tube") ~ " (Custom, " ~ lengthString(definition, definition.wallThickness) ~ " wall)",
            "vendor" : "Custom",
            "url" : "",
            "appearance" : MEDIUM_GRAY,
            "stock" : [],
            "width" : size.width,
            "height" : size.height,
            "wallX" : definition.wallThickness,
            "wallY" : definition.wallThickness,
            "angle" : size.angle,
            "holeDiameter" : definition.holeDiameter,
            "xRows" : gridRows(size.sideRows, definition.holeStart, definition.holeSpacing, size.sideRowSpacing),
            "yRows" : gridRows(size.topRows, definition.holeStart, definition.holeSpacing, size.topRowSpacing),
            "isFrame" : true,
            "tieStart" : definition.holeStart,
            "tieUnit" : definition.holeSpacing
        } as Stock;
}

/**
 * A lookup table entry's rows of holes, as `HoleRow`s, their first holes `shift` further along.
 */
export function holeRows(rows is array, shift is ValueWithUnits) returns array
{
    return mapArray(rows, function(row)
        {
            row.start += shift;
            return row as HoleRow;
        });
}

/**
 * The selected frame's entry in its lookup table: a COTS frame, or a custom frame's size and rows of holes.
 */
function frameTableEntry(definition is map) returns map
{
    if (isCustomFrame(definition))
    {
        return getLookupTable(customFrameTable, definition.customFrame);
    }
    return isFrc(definition) ?
        getLookupTable(frcFrameTable, definition.frcFrame) :
        getLookupTable(ftcFrameTable, definition.ftcFrame);
}

/**
 * A custom frame's size and rows of holes: `width`, `height`, `angle`, and how many rows go on its faces facing Y
 * (`topRows`, `width` wide) and X (`sideRows`), and how far apart (`topRowSpacing`, `sideRowSpacing`). A size from the
 * table has its size and rows from there, and their distances from the dialog; the custom size has them all from the
 * dialog, with no rows on faces it doesn't add holes to.
 */
export function customFrameSize(definition is map) returns map
{
    const entry = frameTableEntry(definition);
    if (entry.customSize)
    {
        return {
                "width" : definition.width,
                "height" : definition.height,
                "angle" : entry.angle,
                "topRows" : definition.topHoles ? definition.topRows : 0,
                "sideRows" : definition.sideHoles ? definition.sideRows : 0,
                "topRowSpacing" : definition.topRowSpacing,
                "sideRowSpacing" : definition.sideRowSpacing
            };
    }
    return mergeMaps(entry, {
                "topRowSpacing" : definition.tableTopRowSpacing,
                "sideRowSpacing" : definition.tableSideRowSpacing
            });
}

/**
 * The diameter of the selected frame's holes: as sold for a COTS frame, or a custom frame's.
 */
function frameHoleDiameter(definition is map) returns ValueWithUnits
{
    return isCustomFrame(definition) ? CUSTOM_HOLE_DIAMETER : frameTableEntry(definition).holeDiameter;
}

/**
 * A row of holes `pitch` apart, the first `start` from the start, with `count` holes across it `spacing` apart, centered
 * on a face; or none.
 */
function gridRows(count is number, start is ValueWithUnits, pitch is ValueWithUnits, spacing is ValueWithUnits) returns array
{
    if (count == 0)
    {
        return [];
    }
    return [{
                "start" : start,
                "pitch" : pitch,
                "shapes" : mapArray(range(0, count - 1), function(i)
                    {
                        return { "along" : 0 * meter, "offset" : (i - (count - 1) / 2) * spacing };
                    })
            } as HoleRow];
}

/**
 * Throws if an FRC COTS frame's holes are set smaller than they come, or a custom frame's walls don't fit inside it, or
 * its rows of holes don't fit across its faces.
 */
function verifyFrame(definition is map)
{
    if (!isFrc(definition))
    {
        return;
    }
    if (!isCustomFrame(definition))
    {
        const holeDiameter = frameHoleDiameter(definition);
        if (tolerantLessThan(definition.holeDiameter, holeDiameter))
        {
            throw regenError("This frame's holes are " ~ lengthString(definition, holeDiameter) ~ ", so holes can't be smaller.", ["holeDiameter"]);
        }
        return;
    }
    // The sides facing X are as wide as the frame is high, inside the walls facing Y, and the other way around
    const size = customFrameSize(definition);
    const inside = 2 * definition.wallThickness;
    if (!tolerantLessThan(inside, min(size.width, size.height)))
    {
        throw regenError("The walls are too thick for the frame.", ["customFrame", "width", "height", "wallThickness"]);
    }
    if (!rowsFit(size.sideRows, size.sideRowSpacing, definition.holeDiameter, size.height - inside) ||
        !rowsFit(size.topRows, size.topRowSpacing, definition.holeDiameter, size.width - inside))
    {
        throw regenError("The rows of holes don't fit across the frame's faces.",
            ["customFrame", "width", "height", "topRows", "sideRows", "topRowSpacing", "sideRowSpacing", "tableTopRowSpacing",
                    "tableSideRowSpacing", "holeDiameter"]);
    }
}

/**
 * Whether `count` rows of holes `spacing` apart (see `gridRows`) fit across a face `width` wide.
 */
function rowsFit(count is number, spacing is ValueWithUnits, holeDiameter is ValueWithUnits, width is ValueWithUnits) returns boolean
{
    return count == 0 || tolerantLessThanOrEqual((count - 1) * spacing + holeDiameter, width);
}

/**
 * Custom size: whether a custom frame's size is the Custom one (which shows its size, rows, and their distances).
 */
function isCustomSizeFrame(definition is map) returns boolean
{
    return isCustomFrame(definition) && frameTableEntry(definition).customSize;
}

/**
 * No top holes: whether a custom frame has no holes on its top: none from the table, or the custom size without Add top
 * holes (which hides Top row distance, and with No side holes, the hole parameters).
 */
function isNoTopHolesFrame(definition is map) returns boolean
{
    return isCustomFrame(definition) && noRows(definition, "topRows", "topHoles");
}

/**
 * No side holes: whether a custom frame has no holes on its sides (see `isNoTopHolesFrame`).
 */
function isNoSideHolesFrame(definition is map) returns boolean
{
    return isCustomFrame(definition) && noRows(definition, "sideRows", "sideHoles");
}

/**
 * Whether a custom frame has no rows of holes on the faces whose rows its entry has under `rowsKey`: a size from the
 * table with none, or the custom size without the dialog's `addKey` (like Add top holes).
 */
function noRows(definition is map, rowsKey is string, addKey is string) returns boolean
{
    const entry = frameTableEntry(definition);
    return entry.customSize ? !definition[addKey] : entry[rowsKey] == 0;
}

/**
 * @internal
 * The editing logic function for robot frame. Sets Custom size (`isCustomSizeFrame`), No top holes
 * (`isNoTopHolesFrame`), and No side holes (`isNoSideHolesFrame`), and when the frame changes
 * (or the feature is created), its hole diameter to the frame's, unless it's been set larger.
 */
export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.customSize = isCustomSizeFrame(definition);
    definition.noTopHoles = isNoTopHolesFrame(definition);
    definition.noSideHoles = isNoSideHolesFrame(definition);
    definition = frameChangeEditLogic(oldDefinition, definition, specifiedParameters);
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}

/**
 * When the frame changes: a COTS frame's Hole start distance (where its holes
 * start as it's sold, unless it's been set), and Hole diameter (the frame's, unless it's been set bigger).
 */
export function frameChangeEditLogic(oldDefinition is map, definition is map, specifiedParameters is map) returns map
{
    var changed = false;
    for (var parameter in ["program", "source", "customFrame", "frcFrame", "ftcFrame"])
    {
        changed = changed || oldDefinition[parameter] != definition[parameter];
    }
    if (!changed)
    {
        return definition;
    }
    if (!isCustomFrame(definition) && !(specifiedParameters.holeStart ?? false))
    {
        definition.holeStart = frameTableEntry(definition).tieStart;
    }
    const holeDiameter = frameHoleDiameter(definition);
    if (!(specifiedParameters.holeDiameter ?? false) || tolerantLessThan(definition.holeDiameter, holeDiameter))
    {
        definition.holeDiameter = holeDiameter;
    }
    return definition;
}
