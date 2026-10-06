FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "72b77780ed382be329401627", version : "c007335a53e017e87fb72abc");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "599a218f6ba935dcd664345c");
import(path : "eb11a2948f8123134339137f", version : "2209aff42808fb5a7c367b91");
import(path : "ff444db0395e01aaa8c7e555", version : "2035cadd8237ea6f5a32ef6a");
import(path : "aa47f3d3eb754118903deeec", version : "cdbb3e4ffa802ae7b7ce0f89");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "96c729d8e6fb2f0289f4a22d");

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

export predicate isCustomFrame(definition is map)
{
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

/** The default number of holes tied to the end of a frame. */
const TIED_HOLE_COUNT_BOUNDS = { (unitless) : [1, 3, 1e3] } as IntegerBoundSpec;

/**
 * Places tube, channel, angle, and extrusion along edges, or extrudes it from a point, tagged as frames so the std
 * frame features work with it.
 */
annotation { "Feature Type Name" : "Robot frame",
        "Feature Type Description" : "Add tube, channel, angle, and extrusion along edges, or extrude it from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotFrameManipulatorChange",
        "Editing Logic Function" : "robotFrameEditLogic"
    }
export const robotFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Name" : "Source", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.source is FrameSource;

        placementPredicate(definition);

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
                annotation { "Name" : "Tube", "Lookup Table" : frcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcFrame is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Channel", "Lookup Table" : ftcFrameTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcFrame is LookupTablePath;
            }
        }

        // Set to the frame's by editing logic
        annotation { "Name" : "Hole diameter" }
        isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);

        if (isEdgePlacement(definition))
        {
            stockEdgePredicate(definition);
        }
        else
        {
            stockPointPredicate(definition, "frame");
        }

        // Forked from robotNutStrip, with its own defaults
        annotation { "Name" : "Tie holes to end", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.tieHoles is boolean;

        if (definition.tieHoles)
        {
            annotation { "Group Name" : "Tie holes to end", "Collapsed By Default" : false, "Driving Parameter" : "tieHoles" }
            {
                annotation { "Name" : "Tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.tiedHoleCount, TIED_HOLE_COUNT_BOUNDS);

                annotation { "Name" : "Show tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.showTiedHoles is boolean;
            }
        }
    }
    {
        const frame = getFrame(definition);
        verifyHoles(definition, frame);
        placeStock(context, id, definition, frame, function(context is Context, id is Id, location is CoordSystem, length is ValueWithUnits)
            {
                return buildFrame(context, id, definition, frame, location, length);
            });
    }, mergeMaps(STOCK_DEFAULTS, {
            "source" : FrameSource.COTS,
            "customProfile" : CustomProfile.TWO_BY_ONE,
            "holeDiameter" : 0.196 * inch,
            "tiedHoleCount" : 3
        }));

/**
 * The selected frame: its entry in its lookup table (see frameTables.py), or a custom one in the same form.
 */
function getFrame(definition is map) returns map
{
    if (!isCustomFrame(definition))
    {
        return isFrc(definition) ?
            getLookupTable(frcFrameTable, definition.frcFrame) :
            getLookupTable(ftcFrameTable, definition.ftcFrame);
    }
    const tube = getCustomTube(definition);
    const unit = isFrc(definition) ? inch : millimeter;
    return {
            "vendor" : "Custom",
            "url" : "",
            "appearance" : WHITE,
            "stock" : [],
            // e.g. 2x1 Tube (custom, 0.0625 in. wall)
            "partName" : roundToPrecision(tube.width / unit, 3) ~ "x" ~ roundToPrecision(tube.height / unit, 3) ~
                ((tube.angle ?? false) ? " Angle" : " Tube") ~ " (custom, " ~ lengthString(definition, definition.wallThickness) ~ " wall)",
            "width" : tube.width,
            "height" : tube.height,
            "wallX" : definition.wallThickness,
            "wallY" : definition.wallThickness,
            "angle" : tube.angle ?? false,
            "holeDiameter" : tube.holeDiameter,
            "xRows" : gridRows(tube.sideRows, tube.holeSpacing),
            "yRows" : gridRows(tube.topRows, tube.holeSpacing),
            "tieStart" : tube.holeSpacing,
            "tieUnit" : tube.holeSpacing
        };
}

/**
 * The size and holes of a custom frame: one of `CUSTOM_PROFILES`, or as set.
 */
function getCustomTube(definition is map) returns map
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
 * A row of holes `spacing` apart, `count` across centered on a face, starting `spacing` from the end.
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
            }];
}

/**
 * Throws if the holes are smaller than a COTS frame's, which come with them, or a custom frame's don't fit across its
 * faces.
 */
function verifyHoles(definition is map, frame is map)
{
    const tolerance = TOLERANCE.zeroLength * meter;
    if (!isCustomFrame(definition))
    {
        if (definition.holeDiameter < frame.holeDiameter - tolerance)
        {
            throw regenError("This frame's holes are " ~ lengthString(definition, frame.holeDiameter) ~ ", so holes can't be smaller.",
                ["holeDiameter"]);
        }
        return;
    }
    // The sides facing X are as wide as the frame is high, inside the walls facing Y, and the other way around
    const faces = [
            { "rows" : frame.xRows, "width" : frame.height - 2 * frame.wallY, "parameter" : "sideHoleRows" },
            { "rows" : frame.yRows, "width" : frame.width - 2 * frame.wallX, "parameter" : "topHoleRows" }
        ];
    for (var face in faces)
    {
        for (var row in face.rows)
        {
            for (var shape in row.shapes)
            {
                if (2 * abs(shape.offset) + definition.holeDiameter > face.width + tolerance)
                {
                    throw regenError("The holes don't fit across the frame's faces.", [face.parameter, "holeSpacing", "holeDiameter"]);
                }
            }
        }
    }
}

/**
 * Builds a frame `length` long, from `location` along its Z axis, with its width along X: its profile is extruded,
 * then its holes are cut.
 *
 * Each row's holes are cut by one seed, a tool for its first hole (or group of holes), which is then face patterned
 * along the frame; the holes tied to the end have their own seed at the last of them, patterned back toward the start.
 * The tools are cut with one boolean, and the holes have no hole attributes, for speed.
 */
function buildFrame(context is Context, id is Id, definition is map, frame is map, location is CoordSystem, length is ValueWithUnits) returns map
{
    const halfWidth = frame.width / 2;
    const halfHeight = frame.height / 2;

    const profileId = id + "profile";
    const sketch = newSketchOnPlane(context, profileId, { "sketchPlane" : plane(location.origin, location.zAxis, location.xAxis) });
    if (frame.profile != undefined)
    {
        skDataArray(sketch, "profile", { "sketchDataArray" : frame.profile });
    }
    else if (frame.angle ?? false)
    {
        // Its legs are the walls facing -X and -Y
        skPolyline(sketch, "profile", {
                    "points" : [
                            vector(-halfWidth, -halfHeight), vector(halfWidth, -halfHeight), vector(halfWidth, -halfHeight + frame.wallY),
                            vector(-halfWidth + frame.wallX, -halfHeight + frame.wallY), vector(-halfWidth + frame.wallX, halfHeight),
                            vector(-halfWidth, halfHeight), vector(-halfWidth, -halfHeight)
                        ]
                });
    }
    else if (frame.open ?? false)
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
    else if (frame.bore != undefined)
    {
        skRectangle(sketch, "outside", { "firstCorner" : vector(-halfWidth, -halfHeight), "secondCorner" : vector(halfWidth, halfHeight) });
        skCircle(sketch, "bore", { "center" : vector(0, 0) * meter, "radius" : frame.bore / 2 });
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
    const faces = [
            {
                "name" : "x",
                "rows" : frame.xRows,
                "plane" : plane(toWorld(location, vector(-halfWidth, 0 * meter, 0 * meter)), location.xAxis, location.zAxis),
                "sign" : -1,
                "depth" : frame.width
            },
            {
                "name" : "y",
                "rows" : frame.yRows,
                "plane" : plane(toWorld(location, vector(0 * meter, -halfHeight, 0 * meter)), yAxis(location), location.zAxis),
                "sign" : 1,
                "depth" : frame.height
            }
        ];
    var seeds = [];
    for (var face in faces)
    {
        for (var i, row in face.rows)
        {
            const positions = holePositions(row.start, row.pitch, rowExtent(row, definition.holeDiameter), length, tie);
            for (var tied in [false, true])
            {
                const group = filter(positions, function(hole)
                    {
                        return hole.tied == tied;
                    });
                if (group == [])
                {
                    continue;
                }
                const seedId = id + (face.name ~ "Row" ~ i ~ (tied ? "Tied" : ""));
                const seedPosition = tied ? group[size(group) - 1].position : group[0].position;
                sketchSeed(context, seedId, face, row.shapes, seedPosition, definition.holeDiameter);
                seeds = append(seeds, {
                            "id" : seedId,
                            "count" : size(group),
                            "step" : location.zAxis * (tied ? -row.pitch : row.pitch),
                            "tied" : tied
                        });
            }
        }
    }

    var tiedHoles = [];
    if (seeds != [])
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : qUnion(mapArray(seeds, function(seed)
                            {
                                return qCreatedBy(seed.id + "tool", EntityType.BODY);
                            })),
                    "targets" : body,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        for (var i, pattern in patterns(seeds))
        {
            if (pattern.tied)
            {
                tiedHoles = append(tiedHoles, qCreatedBy(id + ("pattern" ~ i), EntityType.FACE));
            }
            const instances = range(1, pattern.count - 1);
            opPattern(context, id + ("pattern" ~ i), {
                        "entities" : qUnion(mapArray(pattern.seeds, function(seed)
                                {
                                    return qCreatedBy(seed.id + "tool", EntityType.FACE);
                                })),
                        "transforms" : mapArray(instances, function(k)
                            {
                                return transform(pattern.step * k);
                            }),
                        "instanceNames" : mapArray(instances, function(k)
                            {
                                return "" ~ k;
                            })
                    });
        }
    }
    opDeleteBodies(context, id + "deleteSketches", {
                "entities" : qUnion(append(mapArray(seeds, function(seed)
                            {
                                return qCreatedBy(seed.id + "sketch", EntityType.BODY);
                            }), qCreatedBy(profileId, EntityType.BODY)))
            });

    setStockProperties(context, body, definition, frame, frame.partName, length);
    for (var seed in seeds)
    {
        if (seed.tied)
        {
            tiedHoles = append(tiedHoles, qCreatedBy(seed.id + "tool", EntityType.FACE));
        }
    }
    return {
            "endFace" : qCapEntity(tubeId, CapType.END, EntityType.FACE),
            "tiedHoles" : qUnion(tiedHoles),
            "irregular" : (frame.xRows != [] || frame.yRows != []) && !isRegularLength(length, tie)
        };
}

/**
 * Groups the seeds which are patterned the same way (the same number of times, as far, in the same direction), so
 * they're patterned together, with fewer operations. Holes which cross, like those through each side of a square
 * beam, have to be.
 */
function patterns(seeds is array) returns array
{
    var result = [];
    for (var seed in seeds)
    {
        if (seed.count < 2)
        {
            continue;
        }
        var found = false;
        for (var i, pattern in result)
        {
            if (pattern.count == seed.count && pattern.tied == seed.tied && tolerantEquals(pattern.step, seed.step))
            {
                result[i].seeds = append(pattern.seeds, seed);
                found = true;
                break;
            }
        }
        if (!found)
        {
            result = append(result, { "count" : seed.count, "step" : seed.step, "tied" : seed.tied, "seeds" : [seed] });
        }
    }
    return result;
}

/** The radius of the MAXSpline cutouts of MAXTube with MAX Pattern: its teeth's tips (see splineProfiles.py). */
const MAX_SPLINE_RADIUS = 17.45 * millimeter;

/**
 * How far a row's shapes reach along the frame from its position.
 */
function rowExtent(row is map, holeDiameter is ValueWithUnits) returns ValueWithUnits
{
    var extent = 0 * meter;
    for (var shape in row.shapes)
    {
        const radius = (shape.maxSpline ?? false) ? MAX_SPLINE_RADIUS : (shape.diameter ?? holeDiameter) / 2;
        extent = max(extent, abs(shape.along) + (shape.slot ?? 0 * meter) / 2 + radius);
    }
    return extent;
}

/**
 * Sketches a row's `shapes` at `position` along `face`, and extrudes them through the frame as a tool, `seedId +
 * "tool"`.
 */
function sketchSeed(context is Context, seedId is Id, face is map, shapes is array, position is ValueWithUnits, holeDiameter is ValueWithUnits)
{
    const sketch = newSketchOnPlane(context, seedId + "sketch", { "sketchPlane" : face.plane });
    for (var i, shape in shapes)
    {
        const center = vector(position + shape.along, face.sign * shape.offset);
        if (shape.maxSpline ?? false)
        {
            skDataArray(sketch, "maxSpline" ~ i, { "sketchDataArray" : MAX_SPLINE_HOLE, "location" : center });
            continue;
        }
        const radius = (shape.diameter ?? holeDiameter) / 2;
        const slot = shape.slot ?? 0 * meter;
        if (slot < TOLERANCE.zeroLength * meter)
        {
            skCircle(sketch, "hole" ~ i, { "center" : center, "radius" : radius });
            continue;
        }
        // A slot along the frame: two sides and two round ends
        const along = vector(slot / 2, 0 * meter);
        const across = vector(0 * meter, radius);
        skLineSegment(sketch, "side" ~ i, { "start" : center - along - across, "end" : center + along - across });
        skArc(sketch, "end" ~ i, {
                    "start" : center + along - across,
                    "mid" : center + along + vector(radius, 0 * meter),
                    "end" : center + along + across
                });
        skLineSegment(sketch, "otherSide" ~ i, { "start" : center + along + across, "end" : center - along + across });
        skArc(sketch, "otherEnd" ~ i, {
                    "start" : center - along + across,
                    "mid" : center - along - vector(radius, 0 * meter),
                    "end" : center - along - across
                });
    }
    skSolve(sketch);
    opExtrude(context, seedId + "tool", {
                "entities" : qSketchRegion(seedId + "sketch"),
                "direction" : face.plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : face.depth
            });
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
 * The editing logic function for robot frame. When the frame changes, its hole diameter is set to the frame's, unless
 * it's been set larger.
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
        const holeDiameter = getFrame(definition).holeDiameter;
        if (!(specifiedParameters.holeDiameter ?? false) || definition.holeDiameter < holeDiameter)
        {
            definition.holeDiameter = holeDiameter;
        }
    }
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, undefined, false);
}
