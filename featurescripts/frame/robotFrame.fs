FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotFrameIcon::import(path : "94bcdc0a50ec32f81a3aa449", version : "0ee450364120d3c453712009");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "7ff3897ddcba9a81bae27310");
import(path : "ff444db0395e01aaa8c7e555", version : "e4db7164a5fde703c448de5f");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "3e1a1a3e6b103a1480625571");

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

                // Whether the frame's faces differ in width (2x1), so their rows are spaced separately. Set by editing
                // logic, as Onshape can't show parameters by a lookup table's value
                annotation { "Name" : "Rectangular frame", "UIHint" : ["ALWAYS_HIDDEN"] }
                definition.rectangularFrame is boolean;

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

                annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.wallThickness, WALL_BOUNDS);

                annotation { "Name" : "Distance between holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.holeSpacing, SPACING_BOUNDS);

                annotation { "Name" : "Start distance", "Description" : "The distance from the start of the frame to its first hole.",
                            "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.holeStart, START_BOUNDS);
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

            // Set to the frame's by editing logic. FTC frames' holes are as they're sold
            if (isFrc(definition))
            {
                annotation { "Name" : "Hole diameter" }
                isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
            }
        }

        stockLocationPredicate(definition, "frame");

        tieHolesPredicate(definition);
    }
    {
        verifyHoles(definition);
        placeStock(context, id, definition, getFrame(definition), "frame");
    });

/**
 * The selected frame as stock: its entry in its lookup table (see frameTables.py), or a custom one, with holes as big
 * as `holeDiameter` (an FTC frame's are as it's sold).
 */
function getFrame(definition is map) returns Stock
{
    const entry = frameTableEntry(definition);
    if (!isCustomFrame(definition))
    {
        return mergeMaps(entry, {
                    "holeDiameter" : isFrc(definition) ? definition.holeDiameter : entry.holeDiameter,
                    "xRows" : holeRows(entry.xRows),
                    "yRows" : holeRows(entry.yRows),
                    "isFrame" : true
                }) as Stock;
    }
    return {
            // e.g. 2x1 Tube (Custom, 0.0625 in. wall)
            "partName" : roundToPrecision(entry.width / inch, 3) ~ "x" ~ roundToPrecision(entry.height / inch, 3) ~
                (entry.angle ? " Angle" : " Tube") ~ " (Custom, " ~ lengthString(definition, definition.wallThickness) ~ " wall)",
            "vendor" : "Custom",
            "url" : "",
            "appearance" : MEDIUM_GRAY,
            "stock" : [],
            "width" : entry.width,
            "height" : entry.height,
            "wallX" : definition.wallThickness,
            "wallY" : definition.wallThickness,
            "angle" : entry.angle,
            "holeDiameter" : definition.holeDiameter,
            "xRows" : gridRows(entry.sideRows, definition.holeStart, definition.holeSpacing, rowSpacing(definition, entry, entry.height)),
            "yRows" : gridRows(entry.topRows, definition.holeStart, definition.holeSpacing, rowSpacing(definition, entry, entry.width)),
            "isFrame" : true,
            "tieStart" : definition.holeStart,
            "tieUnit" : definition.holeSpacing
        } as Stock;
}

/**
 * A lookup table entry's rows of holes, as `HoleRow`s.
 */
function holeRows(rows is array) returns array
{
    return mapArray(rows, function(row)
        {
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
 * Throws if an FRC COTS frame's holes are set smaller than they come, or a custom frame's rows of holes don't fit across
 * its faces.
 */
function verifyHoles(definition is map)
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
    const entry = frameTableEntry(definition);
    const inside = 2 * definition.wallThickness;
    if (!rowsFit(entry.sideRows, rowSpacing(definition, entry, entry.height), definition.holeDiameter, entry.height - inside) ||
        !rowsFit(entry.topRows, rowSpacing(definition, entry, entry.width), definition.holeDiameter, entry.width - inside))
    {
        throw regenError("The rows of holes don't fit across the frame's faces.",
            ["customFrame", "rowSpacing", "wideRowSpacing", "narrowRowSpacing", "holeDiameter"]);
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
 * @internal
 * The editing logic function for robot frame. When the frame changes (or the feature is created), its hole diameter is
 * set to the frame's, unless it's been set larger, and whether a custom frame is rectangular is set (which shows its
 * faces' row distances).
 */
export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    var changed = false;
    for (var parameter in ["program", "source", "customFrame", "frcFrame", "ftcFrame"])
    {
        changed = changed || oldDefinition[parameter] != definition[parameter];
    }
    if (changed)
    {
        if (isCustomFrame(definition))
        {
            definition.rectangularFrame = isRectangular(frameTableEntry(definition));
        }
        const holeDiameter = frameHoleDiameter(definition);
        if (!(specifiedParameters.holeDiameter ?? false) || tolerantLessThan(definition.holeDiameter, holeDiameter))
        {
            definition.holeDiameter = holeDiameter;
        }
    }
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}
