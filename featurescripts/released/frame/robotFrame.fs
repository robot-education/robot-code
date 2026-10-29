FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotFrameIcon::import(path : "94bcdc0a50ec32f81a3aa449", version : "0ee450364120d3c453712009");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
import(path : "ff444db0395e01aaa8c7e555", version : "87aa3665d5a93d91331dbe19");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "439d5a6da5926ffd182185cd");

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
const ROWS_BOUNDS = { (unitless) : [0, 1, 100] } as IntegerBoundSpec;

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
                annotation { "Name" : "Rectangular frame", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isRectangularFrame" }
                definition.rectangularFrame is boolean;

                annotation { "Name" : "Custom size", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isCustomSizeFrame" }
                definition.customSize is boolean;

                annotation { "Name" : "No holes", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isNoHolesFrame" }
                definition.noHoles is boolean;

                if (definition.customSize)
                {
                    annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.width, WIDTH_BOUNDS);

                    annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.height, HEIGHT_BOUNDS);

                    annotation { "Name" : "Top rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.topRows, ROWS_BOUNDS);

                    annotation { "Name" : "Side rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.sideRows, ROWS_BOUNDS);

                    annotation { "Name" : "Top row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.topRowSpacing, SPACING_BOUNDS);

                    annotation { "Name" : "Side row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.sideRowSpacing, SPACING_BOUNDS);
                }
                else if (!definition.noHoles)
                {
                    if (definition.rectangularFrame)
                    {
                        annotation { "Name" : "2 in. face row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.wideRowSpacing, SPACING_BOUNDS);

                        annotation { "Name" : "1 in. face row distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.narrowRowSpacing, SPACING_BOUNDS);
                    }
                    else
                    {
                        annotation { "Name" : "Distance between rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        isLength(definition.rowSpacing, SPACING_BOUNDS);
                    }
                }

                annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.wallThickness, WALL_BOUNDS);

                if (!definition.noHoles)
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

            if (!(isCustomFrame(definition) && definition.noHoles))
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
        placeStock(context, id, definition, getFrame(definition), "frame");
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
 * table has its size and rows from there and its rows' distances from the dialog; a custom size has them all from the
 * dialog.
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
                "topRows" : definition.topRows,
                "sideRows" : definition.sideRows,
                "topRowSpacing" : definition.topRowSpacing,
                "sideRowSpacing" : definition.sideRowSpacing
            };
    }
    return mergeMaps(entry, {
                "topRowSpacing" : rowSpacing(definition, entry, entry.width),
                "sideRowSpacing" : rowSpacing(definition, entry, entry.height)
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
 * Whether a custom frame's faces differ in width (like 2x1's), so their rows of holes are spaced separately.
 */
function isRectangular(entry is map) returns boolean
{
    return !tolerantEquals(entry.width, entry.height);
}

/**
 * How far apart a custom frame's rows of holes are across a face `face` wide: on a rectangular frame's wider or
 * narrower faces, or on any face of a square one.
 */
function rowSpacing(definition is map, entry is map, face is ValueWithUnits) returns ValueWithUnits
{
    if (!isRectangular(entry))
    {
        return definition.rowSpacing;
    }
    return tolerantEquals(face, max(entry.width, entry.height)) ? definition.wideRowSpacing : definition.narrowRowSpacing;
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
            ["customFrame", "width", "height", "topRows", "sideRows", "rowSpacing", "wideRowSpacing", "narrowRowSpacing",
                    "topRowSpacing", "sideRowSpacing", "holeDiameter"]);
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
 * Rectangular frame: whether a custom frame's faces differ in width (2x1), so their rows are spaced separately (which
 * shows its faces' row distances).
 */
function isRectangularFrame(definition is map) returns boolean
{
    return isCustomFrame(definition) && !isCustomSizeFrame(definition) && isRectangular(frameTableEntry(definition));
}

/**
 * Custom size: whether a custom frame's size is the Custom one (which shows its size, rows, and their distances).
 */
function isCustomSizeFrame(definition is map) returns boolean
{
    return isCustomFrame(definition) && frameTableEntry(definition).customSize;
}

/**
 * No holes: whether a custom frame is a size from the table chosen with no rows of holes (which hides the hole and row
 * parameters).
 */
function isNoHolesFrame(definition is map) returns boolean
{
    if (!isCustomFrame(definition) || isCustomSizeFrame(definition))
    {
        return false;
    }
    const entry = frameTableEntry(definition);
    return entry.topRows == 0 && entry.sideRows == 0;
}

/**
 * @internal
 * The editing logic function for robot frame. Sets Rectangular frame (`isRectangularFrame`), Custom size
 * (`isCustomSizeFrame`), and No holes (`isNoHolesFrame`), and when the frame changes
 * (or the feature is created), its hole diameter to the frame's, unless it's been set larger.
 */
export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.rectangularFrame = isRectangularFrame(definition);
    definition.customSize = isCustomSizeFrame(definition);
    definition.noHoles = isNoHolesFrame(definition);
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
