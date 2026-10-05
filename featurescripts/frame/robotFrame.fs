FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/frameTags.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "frame/frameTables.gen.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "core/linearStock.fs", version : "");

/**
 * Whether a frame is a tube or channel someone sells (see frameTables.py), or custom.
 */
export enum FrameSource
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Custom" }
    CUSTOM
}

export predicate isCustomFrame(definition is map)
{
    definition.source == FrameSource.CUSTOM;
}

/**
 * The holes of a custom frame, or of a COTS frame with a custom hole pattern.
 */
export enum CustomHolePattern
{
    annotation { "Name" : "Grid" }
    GRID,
    annotation { "Name" : "Narrow faces only" }
    NARROW_FACES,
    annotation { "Name" : "None" }
    NONE
}

const WIDTH_BOUNDS = { (meter) : [1e-5, 0.0508, 500], (inch) : 2, (millimeter) : 48 } as LengthBoundSpec;
const HEIGHT_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 24 } as LengthBoundSpec;
const WALL_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 2.5 } as LengthBoundSpec;
const HOLE_SPACING_BOUNDS = { (meter) : [1e-5, 0.0127, 500], (inch) : 0.5, (millimeter) : 8 } as LengthBoundSpec;
const HOLE_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.0049784, 500], (inch) : 0.196, (millimeter) : 4 } as LengthBoundSpec;

/** The default number of holes tied to the end of a frame. */
const TIED_HOLE_COUNT_BOUNDS = { (unitless) : [1, 3, 1e3] } as IntegerBoundSpec;

/**
 * The hole pattern of a custom frame: holes `holeSpacing` apart, in centered rows `holeSpacing` apart across each
 * face, starting `holeSpacing` from the end.
 */
export predicate customHolePatternPredicate(definition is map)
{
    annotation { "Name" : "Hole pattern", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.holePattern is CustomHolePattern;

    if (definition.holePattern != CustomHolePattern.NONE)
    {
        annotation { "Name" : "Hole spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.holeSpacing, HOLE_SPACING_BOUNDS);

        annotation { "Name" : "Hole diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
    }
}

/**
 * Places tube and channel along edges, or extrudes it from a point, tagged as frames so the std frame features work
 * with it.
 */
annotation { "Feature Type Name" : "Robot frame",
        "Feature Type Description" : "Add tube and channel along edges, or extrude it from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotFrameManipulatorChange",
        "Editing Logic Function" : "robotFrameEditLogic"
    }
export const robotFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        stockPlacementPredicate(definition);

        annotation { "Name" : "Source", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.source is FrameSource;

        if (isCustomFrame(definition))
        {
            annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.width, WIDTH_BOUNDS);

            annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.height, HEIGHT_BOUNDS);

            annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.wallThickness, WALL_BOUNDS);

            customHolePatternPredicate(definition);
        }
        else
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "Tube", "Lookup Table" : frcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcFrame is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Channel", "Lookup Table" : ftcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcFrame is LookupTablePath;
            }

            annotation { "Name" : "Custom hole pattern", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.customHolePattern is boolean;

            if (definition.customHolePattern)
            {
                customHolePatternPredicate(definition);
            }
        }

        if (isEdgePlacement(definition))
        {
            stockEdgePredicate(definition);
        }
        else
        {
            stockPointPredicate(definition, "frame");
        }

        // Forked from robotNutStrip, with its own defaults
        annotation { "Name" : "Tie holes to end", "Column Name" : "Has tied holes", "Default" : true,
                    "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
        definition.tieHoles is boolean;
        if (definition.tieHoles)
        {
            annotation { "Name" : "Tied holes", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
            isInteger(definition.tiedHoleCount, TIED_HOLE_COUNT_BOUNDS);

            annotation { "Name" : "Show tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.showTiedHoles is boolean;
        }
    }
    {
        const frame = getFrame(definition);
        placeStock(context, id, definition, frame, function(context is Context, id is Id, location is CoordSystem, length is ValueWithUnits)
            {
                return buildFrame(context, id, definition, frame, location, length);
            });
    }, mergeMaps(STOCK_DEFAULTS, { "source" : FrameSource.COTS, "customHolePattern" : false, "tiedHoleCount" : 3 }));

/**
 * The selected frame: its entry in its lookup table (see frameTables.py), or a custom one, with a custom hole pattern
 * if it has one.
 */
function getFrame(definition is map) returns map
{
    var frame;
    if (isCustomFrame(definition))
    {
        frame = {
                "vendor" : "Custom",
                "url" : "",
                "appearance" : WHITE,
                "stock" : [],
                "partName" : roundToPrecision(definition.width / (isFrc(definition) ? inch : millimeter), 3) ~ "x" ~
                    roundToPrecision(definition.height / (isFrc(definition) ? inch : millimeter), 3) ~ " Tube (custom, " ~
                    lengthString(definition, definition.wallThickness) ~ " wall)",
                "width" : definition.width,
                "height" : definition.height,
                "wallX" : definition.wallThickness,
                "wallY" : definition.wallThickness,
                "open" : false
            };
    }
    else
    {
        frame = isFrc(definition) ?
            getLookupTable(frcFrameTable, definition.frcFrame) :
            getLookupTable(ftcFrameTable, definition.ftcFrame);
    }
    if (isCustomFrame(definition) || definition.customHolePattern)
    {
        frame = withCustomHolePattern(definition, frame);
    }
    return frame;
}

/**
 * Replaces a frame's holes with the custom hole pattern.
 */
function withCustomHolePattern(definition is map, frame is map) returns map
{
    frame.xRows = [];
    frame.yRows = [];
    frame.tieStart = 0.5 * inch;
    frame.tieUnit = 0.5 * inch;
    if (definition.holePattern == CustomHolePattern.NONE)
    {
        return frame;
    }
    const spacing = definition.holeSpacing;
    const tolerance = TOLERANCE.zeroLength * meter;
    // The walls facing X are as wide as the frame is high, and those facing Y as wide as it is wide
    if (definition.holePattern == CustomHolePattern.GRID || frame.height <= frame.width + tolerance)
    {
        frame.xRows = gridRows(frame.height, spacing, definition.holeDiameter);
    }
    if (definition.holePattern == CustomHolePattern.GRID || frame.width <= frame.height + tolerance)
    {
        frame.yRows = gridRows(frame.width, spacing, definition.holeDiameter);
    }
    frame.tieStart = spacing;
    frame.tieUnit = spacing;
    return frame;
}

/**
 * Rows of holes `spacing` apart, centered across a face `width` wide, starting `spacing` from the end.
 */
function gridRows(width is ValueWithUnits, spacing is ValueWithUnits, diameter is ValueWithUnits) returns array
{
    const count = floor((width / 2 - spacing / 2) / spacing + 1e-9);
    if (count < 0)
    {
        return [];
    }
    return mapArray(range(-count, count), function(i)
        {
            return { "offset" : i * spacing, "start" : spacing, "pitch" : spacing, "diameter" : diameter };
        });
}

/**
 * Builds a frame `length` long, from `location` along its Z axis, with its width along X: its profile is extruded,
 * and each axis's holes (tied to the end, and not) are cut with one extrude each.
 */
function buildFrame(context is Context, id is Id, definition is map, frame is map, location is CoordSystem, length is ValueWithUnits) returns map
{
    const halfWidth = frame.width / 2;
    const halfHeight = frame.height / 2;

    const profileId = id + "profile";
    const sketch = newSketchOnPlane(context, profileId, { "sketchPlane" : plane(location.origin, location.zAxis, location.xAxis) });
    if (frame.open)
    {
        const innerX = halfWidth - frame.wallX;
        const innerY = -halfHeight + frame.wallY;
        skPolyline(sketch, "profile", {
                    "points" : [
                            vector(-halfWidth, halfHeight), vector(-halfWidth, -halfHeight), vector(halfWidth, -halfHeight),
                            vector(halfWidth, halfHeight), vector(innerX, halfHeight), vector(innerX, innerY),
                            vector(-innerX, innerY), vector(-innerX, halfHeight), vector(-halfWidth, halfHeight)
                        ]
                });
    }
    else
    {
        skRectangle(sketch, "outside", { "firstCorner" : vector(-halfWidth, -halfHeight), "secondCorner" : vector(halfWidth, halfHeight) });
        skRectangle(sketch, "inside", {
                    "firstCorner" : vector(-halfWidth + frame.wallX, -halfHeight + frame.wallY),
                    "secondCorner" : vector(halfWidth - frame.wallX, halfHeight - frame.wallY)
                });
    }
    skSolve(sketch);
    const tubeId = id + "tube";
    opExtrude(context, tubeId, {
                "entities" : qSketchRegion(profileId, true),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : length
            });
    const body = qCreatedBy(tubeId, EntityType.BODY);
    // Before the holes are cut, so its faces are still the extrude's
    tagExtrudeAsFrame(context, tubeId, frame.partName, frame.vendor);

    const tie = getTie(definition, frame.tieStart, frame.tieUnit);
    // Sketched on the walls facing -X and -Y, with X along the frame; the sketch's Y is the frame's -Y and X respectively
    const xPlane = plane(toWorld(location, vector(-halfWidth, 0 * meter, 0 * meter)), location.xAxis, location.zAxis);
    const yPlane = plane(toWorld(location, vector(0 * meter, -halfHeight, 0 * meter)), yAxis(location), location.zAxis);
    const xHoles = holes(frame.xRows, length, tie, -1);
    const yHoles = holes(frame.yRows, length, tie, 1);
    const cuts = [
            cutHoles(context, id + "xHoles", xPlane, xHoles, false, frame.width),
            cutHoles(context, id + "xTiedHoles", xPlane, xHoles, true, frame.width),
            cutHoles(context, id + "yHoles", yPlane, yHoles, false, frame.height),
            cutHoles(context, id + "yTiedHoles", yPlane, yHoles, true, frame.height)
        ];
    const tools = qUnion(mapArray(cuts, function(cut)
            {
                return cut.tools;
            }));
    if (!isQueryEmpty(context, tools))
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : tools,
                    "targets" : body,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    opDeleteBodies(context, id + "deleteSketches", {
                "entities" : qUnion(append(mapArray(cuts, function(cut)
                            {
                                return cut.sketch;
                            }), qCreatedBy(profileId, EntityType.BODY)))
            });

    setStockProperties(context, body, definition, frame, frame.partName, length);
    return {
            "endFace" : qCapEntity(tubeId, CapType.END, EntityType.FACE),
            "tiedHoles" : qUnion([qCreatedBy(id + "xTiedHoles", EntityType.FACE), qCreatedBy(id + "yTiedHoles", EntityType.FACE)])
        };
}

/**
 * Each hole of `rows` along a frame `length` long: maps of its `position` along the frame, `offset` across it (times
 * `sign`, for the sketch it's drawn in), `radius`, `slot` length, and whether it's `tied` to the end.
 */
function holes(rows is array, length is ValueWithUnits, tie, sign is number) returns array
{
    var result = [];
    for (var row in rows)
    {
        const radius = row.diameter / 2;
        const slot = row.slot ?? 0 * meter;
        for (var hole in holePositions(row.start, row.pitch, radius + slot / 2, length, tie))
        {
            result = append(result, {
                        "position" : hole.position,
                        "offset" : sign * row.offset,
                        "radius" : radius,
                        "slot" : slot,
                        "tied" : hole.tied
                    });
        }
    }
    return result;
}

/**
 * Extrudes tools for the `holes` which are (or aren't) `tied`, from a sketch on `holePlane`, `depth` deep. Returns
 * queries for the `tools` and the `sketch`.
 */
function cutHoles(context is Context, id is Id, holePlane is Plane, holes is array, tied is boolean, depth is ValueWithUnits) returns map
{
    const result = { "tools" : qCreatedBy(id, EntityType.BODY), "sketch" : qCreatedBy(id + "sketch", EntityType.BODY) };
    var count = 0;
    var sketch;
    for (var hole in holes)
    {
        if (hole.tied != tied)
        {
            continue;
        }
        if (sketch == undefined)
        {
            sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : holePlane });
        }
        const center = vector(hole.position, hole.offset);
        if (hole.slot < TOLERANCE.zeroLength * meter)
        {
            skCircle(sketch, "hole" ~ count, { "center" : center, "radius" : hole.radius });
        }
        else
        {
            // A slot along the frame: two sides and two round ends
            const along = vector(hole.slot / 2, 0 * meter);
            const across = vector(0 * meter, hole.radius);
            skLineSegment(sketch, "side" ~ count, { "start" : center - along - across, "end" : center + along - across });
            skArc(sketch, "end" ~ count, {
                        "start" : center + along - across,
                        "mid" : center + along + vector(hole.radius, 0 * meter),
                        "end" : center + along + across
                    });
            skLineSegment(sketch, "otherSide" ~ count, { "start" : center + along + across, "end" : center - along + across });
            skArc(sketch, "otherEnd" ~ count, {
                        "start" : center - along + across,
                        "mid" : center - along - vector(hole.radius, 0 * meter),
                        "end" : center - along - across
                    });
        }
        count += 1;
    }
    if (sketch == undefined)
    {
        return { "tools" : qNothing(), "sketch" : qNothing() };
    }
    skSolve(sketch);
    opExtrude(context, id, {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : holePlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });
    return result;
}

/**
 * @internal
 * The manipulator change function for robot frame.
 */
export function robotFrameManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return stockManipulatorChange(context, definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for robot frame.
 */
export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}
