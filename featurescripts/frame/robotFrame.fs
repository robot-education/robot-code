FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotFrameIcon::import(path : "94bcdc0a50ec32f81a3aa449", version : "580b51d3f344bc5f5987df45");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "7ff3897ddcba9a81bae27310");
import(path : "ff444db0395e01aaa8c7e555", version : "e4db7164a5fde703c448de5f");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "3e1a1a3e6b103a1480625571");

/**
 * Whether a frame is one someone sells (see frameTables.py), or custom.
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

/**
 * A custom frame: a common size of tube or angle with #10 clearance holes on a 1/2 in. grid, or any tube.
 */
export enum CustomProfile
{
    annotation { "Name" : "2x2" }
    TWO_BY_TWO,
    annotation { "Name" : "2x1" }
    TWO_BY_ONE,
    annotation { "Name" : "1x1" }
    ONE_BY_ONE,
    annotation { "Name" : "1x1 angle" }
    ONE_BY_ONE_ANGLE,
    annotation { "Name" : "Custom tube" }
    CUSTOM
}

// Repeats isCustomFrame's condition, as Onshape doesn't allow predicates used in a precondition's if conditions to call
// other predicates
export predicate isCustomTube(definition is map)
{
    definition.program == Program.FRC;
    definition.source == FrameSource.CUSTOM;
    definition.customProfile == CustomProfile.CUSTOM;
}

/**
 * The common sizes of custom frame: tube (or `angle`) `width` by `height`, with `sideRows` rows of holes on the sides
 * facing X and `topRows` on those facing Y, `holeSpacing` apart.
 */
const CUSTOM_PROFILES = {
        (CustomProfile.TWO_BY_TWO) : { "width" : 2 * inch, "height" : 2 * inch, "sideRows" : 3, "topRows" : 3 },
        (CustomProfile.TWO_BY_ONE) : { "width" : 2 * inch, "height" : 1 * inch, "sideRows" : 1, "topRows" : 3 },
        (CustomProfile.ONE_BY_ONE) : { "width" : 1 * inch, "height" : 1 * inch, "sideRows" : 1, "topRows" : 1 },
        (CustomProfile.ONE_BY_ONE_ANGLE) : { "width" : 1 * inch, "height" : 1 * inch, "sideRows" : 1, "topRows" : 1, "angle" : true }
    };

const CUSTOM_HOLE_SPACING = 0.5 * inch;
const CUSTOM_HOLE_DIAMETER = 0.196 * inch;

const WIDTH_BOUNDS = { (meter) : [1e-5, 0.0508, 500], (inch) : 2, (millimeter) : 48 } as LengthBoundSpec;
const HEIGHT_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 24 } as LengthBoundSpec;
const WALL_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 2.5 } as LengthBoundSpec;
const HOLE_SPACING_BOUNDS = { (meter) : [1e-5, 0.0127, 500], (inch) : 0.5, (millimeter) : 8 } as LengthBoundSpec;
const HOLE_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.0049784, 500], (inch) : 0.196, (millimeter) : 4 } as LengthBoundSpec;
const SIDE_HOLE_ROWS_BOUNDS = { (unitless) : [0, 1, 1e3] } as IntegerBoundSpec;
const TOP_HOLE_ROWS_BOUNDS = { (unitless) : [0, 3, 1e3] } as IntegerBoundSpec;

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
                annotation { "Name" : "Profile", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
                definition.customProfile is CustomProfile;

                if (isCustomTube(definition))
                {
                    annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.width, WIDTH_BOUNDS);

                    annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.height, HEIGHT_BOUNDS);
                }

                annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.wallThickness, WALL_BOUNDS);

                if (isCustomTube(definition))
                {
                    annotation { "Name" : "Side hole rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.sideHoleRows, SIDE_HOLE_ROWS_BOUNDS);

                    annotation { "Name" : "Top hole rows", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.topHoleRows, TOP_HOLE_ROWS_BOUNDS);

                    annotation { "Name" : "Hole spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.holeSpacing, HOLE_SPACING_BOUNDS);
                }
            }
            else
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
            }

            // Set to the frame's by editing logic
            annotation { "Name" : "Hole diameter" }
            isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
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
 * as `holeDiameter`.
 */
function getFrame(definition is map) returns Stock
{
    if (!isCustomFrame(definition))
    {
        const entry = frameTableEntry(definition);
        return mergeMaps(entry, {
                    "holeDiameter" : definition.holeDiameter,
                    "xRows" : holeRows(entry.xRows),
                    "yRows" : holeRows(entry.yRows),
                    "isFrame" : true
                }) as Stock;
    }
    const custom = getCustomProfile(definition);
    const unit = isFrc(definition) ? inch : millimeter;
    return {
            // e.g. 2x1 Tube (Custom, 0.0625 in. wall)
            "partName" : roundToPrecision(custom.width / unit, 3) ~ "x" ~ roundToPrecision(custom.height / unit, 3) ~
                ((custom.angle ?? false) ? " Angle" : " Tube") ~ " (Custom, " ~ lengthString(definition, definition.wallThickness) ~ " wall)",
            "vendor" : "Custom",
            "url" : "",
            "appearance" : WHITE,
            "stock" : [],
            "width" : custom.width,
            "height" : custom.height,
            "wallX" : definition.wallThickness,
            "wallY" : definition.wallThickness,
            "angle" : custom.angle ?? false,
            "holeDiameter" : definition.holeDiameter,
            "xRows" : gridRows(custom.sideRows, custom.holeSpacing),
            "yRows" : gridRows(custom.topRows, custom.holeSpacing),
            "isFrame" : true,
            "tieStart" : custom.holeSpacing,
            "tieUnit" : custom.holeSpacing
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
 * The selected COTS frame's entry in its lookup table.
 */
function frameTableEntry(definition is map) returns map
{
    return isFrc(definition) ?
        getLookupTable(frcFrameTable, definition.frcFrame) :
        getLookupTable(ftcFrameTable, definition.ftcFrame);
}

/**
 * The size and holes of a custom frame's profile: one of `CUSTOM_PROFILES`, or as set for a custom tube.
 */
function getCustomProfile(definition is map) returns map
{
    if (isCustomTube(definition))
    {
        return {
                "width" : definition.width,
                "height" : definition.height,
                "sideRows" : definition.sideHoleRows,
                "topRows" : definition.topHoleRows,
                "holeSpacing" : definition.holeSpacing,
                "holeDiameter" : definition.holeDiameter
            };
    }
    return mergeMaps(CUSTOM_PROFILES[definition.customProfile], {
                "holeSpacing" : CUSTOM_HOLE_SPACING,
                "holeDiameter" : CUSTOM_HOLE_DIAMETER
            });
}

/**
 * The diameter of the selected frame's holes: as sold for a COTS frame, or a custom frame's.
 */
function frameHoleDiameter(definition is map) returns ValueWithUnits
{
    return isCustomFrame(definition) ? getCustomProfile(definition).holeDiameter : frameTableEntry(definition).holeDiameter;
}

/**
 * A row of holes `spacing` apart, `count` across centered on a face, starting `spacing` from the end, or none.
 */
function gridRows(count is number, spacing is ValueWithUnits) returns array
{
    if (count == 0)
    {
        return [];
    }
    return [{
                "start" : spacing,
                "pitch" : spacing,
                "shapes" : mapArray(range(0, count - 1), function(i)
                    {
                        return { "along" : 0 * meter, "offset" : (i - (count - 1) / 2) * spacing };
                    })
            } as HoleRow];
}

/**
 * Throws if a COTS frame's holes are set smaller than they come, or a custom frame's rows of holes don't fit across its
 * faces.
 */
function verifyHoles(definition is map)
{
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
    const custom = getCustomProfile(definition);
    const inside = 2 * definition.wallThickness;
    if (!rowsFit(custom.sideRows, custom.holeSpacing, definition.holeDiameter, custom.height - inside) ||
        !rowsFit(custom.topRows, custom.holeSpacing, definition.holeDiameter, custom.width - inside))
    {
        throw regenError("The holes don't fit across the frame's faces.", ["sideHoleRows", "topHoleRows", "holeSpacing", "holeDiameter"]);
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
 * set to the frame's, unless it's been set larger.
 */
export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    var changed = false;
    for (var parameter in ["program", "source", "customProfile", "frcFrame", "ftcFrame"])
    {
        changed = changed || oldDefinition[parameter] != definition[parameter];
    }
    if (changed && !isCustomTube(definition))
    {
        const holeDiameter = frameHoleDiameter(definition);
        if (!(specifiedParameters.holeDiameter ?? false) || tolerantLessThan(definition.holeDiameter, holeDiameter))
        {
            definition.holeDiameter = holeDiameter;
        }
    }
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}
