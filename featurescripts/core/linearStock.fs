FeatureScript 2960;
/**
 * Linear stock, like nut strips and frames: what it is (see `Stock`), how it's built (see `buildStock`), and how it's
 * placed, along an edge or extruded from a point (see `placeStock`).
 *
 * A feature using this module:
 * - Adds `programPredicate` at the top, then its part's parameters, then `stockLocationPredicate` and
 *   `tieHolesPredicate`.
 * - Describes its chosen part as a `Stock`, and calls `placeStock` with it.
 * - Uses `stockManipulatorChange` as its manipulator change function, and calls `stockEditLogic` from its editing
 *   logic.
 *
 * Stock is drawn from its start, which matters since holes are spaced from it: the flip button (or manipulator) chooses
 * which end of the edge (or which way from the point) that is.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "0794d10863d10d98a88c2ab4", version : "7ff3897ddcba9a81bae27310");
import(path : "72b77780ed382be329401627", version : "7de6aa047e083395d076d674");
import(path : "eb11a2948f8123134339137f", version : "2209aff42808fb5a7c367b91");
import(path : "f58a965fe7005e2e3c9a67c7", version : "9c0e8b708d381348c79e36b1");
import(path : "aa47f3d3eb754118903deeec", version : "812299f393e144ff2d6711d6");
// Exported since features' preconditions use their predicates (and enums) through this module's
export import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
export import(path : "58d66340f7b70cfc86606676", version : "c66f2cde90ee0c14ff94cd63");
export import(path : "554542fc345271814c4463b0", version : "9c477217d62dbff99c9b2ad2");
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");
export import(path : "21762d39019c8b2289e2fbb8", version : "8f82cf693e7833130ba80201");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

/**
 * How stock is placed.
 */
export enum StockPlacement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT
}

export predicate isEdgePlacement(definition is map)
{
    definition.placement == StockPlacement.EDGE;
}

/**
 * The default offset of each end of stock on an edge, when it's turned on. Editing logic may set a better one (see
 * `stockEditLogic`).
 */
const OFFSET_BOUNDS = {
            (meter) : [-500, 0.00635, 500],
            (centimeter) : 0.635,
            (millimeter) : 6.35,
            (inch) : 0.25,
            (foot) : 0.25 / 12,
            (yard) : 0.25 / 36
        } as LengthBoundSpec;

/**
 * Where stock goes: the edge to place it along and the offsets of its ends, or the point to extrude it from and the
 * extrude's options (see `isEdgePlacement`), in a Position group. Either way, `flip` draws it from the other end, a
 * button rotates it in 90 degree increments, and a nine point manipulator chooses which point of its profile is on the
 * edge or point (see `orientStock`). From a point, `oppositeDirection` (Flip primary axis) is the extrude's.
 *
 * @param name : What's placed, e.g. `"nut strip"`.
 */
export predicate stockLocationPredicate(definition is map, name is string)
{
    annotation { "Group Name" : "Position", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Selection type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
        definition.placement is StockPlacement;

        if (isEdgePlacement(definition))
        {
            annotation { "Name" : "Edge to place " ~ name, "Filter" : EntityType.EDGE && GeometryType.LINE, "MaxNumberOfPicks" : 1,
                        "UIHint" : ["UNCONFIGURABLE"] }
            definition.edge is Query;
        }
        else
        {
            locationPredicate(definition, name);

            annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.endBound is SMExtrudeBoundingType;

            // The extrude's opposite direction, on the row with the rotate button
            annotation { "Name" : "Flip primary axis", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
            definition.oppositeDirection is boolean;
        }

        // Shared by both placements, since Onshape doesn't allow declaring a parameter twice (even in different branches)
        secondaryAxisPredicate(definition);

        annotation { "Name" : "Flip " ~ name }
        definition.flip is boolean;

        ninePointManipulatorPredicate(definition);

        if (isEdgePlacement(definition))
        {
            stockOffsetsPredicate(definition);
        }
        else
        {
            lengthBoundParametersPredicate(definition);
        }
    }

    // After the group, since they're groups of their own
    if (!isEdgePlacement(definition))
    {
        extrudeDirectionPredicate(definition);
        newExtrudeOptionsPredicate(definition);
    }
}

/**
 * The offsets of the ends of stock on edges.
 */
export predicate stockOffsetsPredicate(definition is map)
{
    annotation { "Name" : "Start offset", "Column Name" : "Has start offset",
                "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
    definition.hasStartOffset is boolean;
    if (definition.hasStartOffset)
    {
        annotation { "Name" : "Start offset", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.edgeStartOffset, OFFSET_BOUNDS);
    }

    annotation { "Name" : "End offset", "Column Name" : "Has end offset",
                "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
    definition.hasEndOffset is boolean;
    if (definition.hasEndOffset)
    {
        annotation { "Name" : "End offset", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.edgeEndOffset, OFFSET_BOUNDS);
    }
}

// What stock is

/**
 * A length of stock: its profile, the rows of holes along it, and how it's sold. Frames come from frameTables.py,
 * which documents the fields in more detail; nut strips are made from nutStripTables.py's entries.
 *
 * @type {{
 *      @field partName {string} : What it's called, after its length, e.g. `"2x1 Tube (WCP, 1/16 in. wall)"`.
 *      @field vendor {string}
 *      @field url {string} : Where to buy it, if no `stock` length fits.
 *      @field appearance {Color}
 *      @field stock {array} : The lengths it's sold in. @seealso [stockFor]
 *      @field width {ValueWithUnits} : The width of its profile, along X.
 *      @field height {ValueWithUnits} : The height of its profile, along Y.
 *      @field profile {array} : @optional A sketch of its profile (a SketchDataArray). Otherwise it's a rectangle:
 *              `solid`, or with walls `wallX` and `wallY` thick, and possibly `open` (a channel), an `angle`, or solid
 *              around a `bore`.
 *      @field holeDiameter {ValueWithUnits} : The diameter of its holes, unless a shape has its own.
 *      @field xRows {array} : Rows of holes through its walls facing X. @seealso [HoleRow]
 *      @field yRows {array} : Rows of holes through its walls facing Y.
 *      @field centerHole {boolean} : @optional Whether a hole `holeDiameter` across runs down its middle (of a `solid`
 *              profile).
 *      @field thread {map} : @optional The thread its holes are tapped with (see `setTappedThroughHoles`), if they are.
 *      @field isFrame {boolean} : @optional Whether it's tagged as a frame, for the std frame features.
 *      @field tieStart {ValueWithUnits} : The position of the first hole which counts for tying holes. @seealso [getTie]
 *      @field tieUnit {ValueWithUnits} : How far apart the holes which count are.
 * }}
 */
export type Stock typecheck canBeStock;

export predicate canBeStock(value)
{
    value is map;
    value.partName is string;
    value.stock is array;
    isLength(value.width);
    isLength(value.height);
    isLength(value.holeDiameter);
    value.xRows is array;
    value.yRows is array;
    isLength(value.tieStart);
    isLength(value.tieUnit);
}

/**
 * A row of holes along stock: a group of `shapes` every `pitch` along it, the first `start` from its start.
 *
 * @type {{
 *      @field start {ValueWithUnits}
 *      @field pitch {ValueWithUnits}
 *      @field shapes {array} : Each is `along` the stock and `offset` across its face from the row's position on the
 *              face's middle: a hole (`holeDiameter` across, unless it has a `diameter`), a `slot` that long along the
 *              stock, or a MAXSpline cutout (`maxSpline`).
 * }}
 */
export type HoleRow typecheck canBeHoleRow;

export predicate canBeHoleRow(value)
{
    value is map;
    isLength(value.start);
    isLength(value.pitch);
    value.shapes is array;
}

/**
 * A row of single holes, as nut strips have.
 */
export function holeRow(start is ValueWithUnits, pitch is ValueWithUnits) returns HoleRow
{
    return { "start" : start, "pitch" : pitch, "shapes" : [{ "along" : 0 * meter, "offset" : 0 * meter }] } as HoleRow;
}

// Tying holes to the end

/**
 * Which holes are tied to the end of stock (see `getTie`).
 */
export enum TieHolesBy
{
    annotation { "Name" : "Half" }
    HALF,
    annotation { "Name" : "Length" }
    LENGTH,
    annotation { "Name" : "Count" }
    COUNT
}

const TIED_HOLE_COUNT_BOUNDS = { (unitless) : [1, 3, 1e3] } as IntegerBoundSpec;

const TIE_LENGTH_BOUNDS = { (meter) : [0, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;

/** The color of tied holes (and the end) when they're colored (see `colorTiedHoles`). */
const TIED_HOLE_COLOR = color(0.25, 0.5, 1);

/**
 * Whether to tie holes to the end, and which (see `getTie`), which go at the end of the feature: the half of them
 * nearest the end, those within a length of it, or a number of them. Tied holes can be shown, highlighted while the
 * feature is edited or colored.
 */
export predicate tieHolesPredicate(definition is map)
{
    annotation { "Name" : "Tie holes to end", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.tieHoles is boolean;

    if (definition.tieHoles)
    {
        annotation { "Group Name" : "Tie holes to end", "Collapsed By Default" : false, "Driving Parameter" : "tieHoles" }
        {
            annotation { "Name" : "Tie hole method", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.tieHolesBy is TieHolesBy;

            if (definition.tieHolesBy == TieHolesBy.LENGTH)
            {
                annotation { "Name" : "Length from end", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.tieLength, TIE_LENGTH_BOUNDS);
            }
            else if (definition.tieHolesBy == TieHolesBy.COUNT)
            {
                annotation { "Name" : "Tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.tiedHoleCount, TIED_HOLE_COUNT_BOUNDS);
            }

            annotation { "Name" : "Show tied holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.showTiedHoles is boolean;

            if (definition.showTiedHoles)
            {
                annotation { "Name" : "Color tied holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.colorTiedHoles is boolean;
            }
        }
    }
}

/**
 * Which holes along stock are tied to its end, from a feature's tie holes parameters (see `tieHolesPredicate`).
 *
 * Holes are spaced from the start of stock, every `unit` from `start`, so a part a whole number of units long has the
 * same margin at each end (see `isRegularLength`). Holes keep their identities as the stock's length changes, so things
 * attached to them stay attached; a hole's identity is usually its count from the start, so those near the end move
 * with it. Tying the holes near the end (and those around them) to it gives them identities counted from the end
 * instead (see `buildStock`).
 *
 * @param start : The position of the first hole which counts.
 * @param unit : How far apart the holes which count are. Rows may have holes between them.
 * @returns {{
 *      @field by {TieHolesBy} : Which holes are tied, or `undefined` if none are.
 *      @field count {number} : How many holes are tied, by `COUNT`.
 *      @field length {ValueWithUnits} : How far from the end tied holes are, by `LENGTH`.
 *      @field start {ValueWithUnits}
 *      @field unit {ValueWithUnits}
 * }}
 */
export function getTie(definition is map, start is ValueWithUnits, unit is ValueWithUnits) returns map
{
    return {
            "by" : definition.tieHoles ? definition.tieHolesBy : undefined,
            "count" : definition.tiedHoleCount,
            "length" : definition.tieLength,
            "start" : start,
            "unit" : unit
        };
}

/**
 * The first of the `spacings + 1` holes which count on stock `length` long (see `regularSpacings`) which is tied to the
 * end, by its number from the start; `spacings + 1` if none are.
 */
function firstTiedHole(length is ValueWithUnits, spacings is number, tie is map) returns number
{
    var first = spacings + 1;
    if (tie.by == TieHolesBy.HALF)
    {
        // The middle hole of an odd number stays with the start
        first = spacings + 1 - floor((spacings + 1) / 2);
    }
    else if (tie.by == TieHolesBy.COUNT && tie.count is number)
    {
        first = spacings + 1 - tie.count;
    }
    else if (tie.by == TieHolesBy.LENGTH && isLength(tie.length))
    {
        // Hole k is length - tie.start - k * tie.unit from the end
        const fromStart = (length - tie.start - tie.length) / tie.unit;
        first = ceil(fromStart);
        if (tolerantEquals(fromStart, first - 1))
        {
            first -= 1;
        }
    }
    return min(max(first, 0), spacings + 1);
}

/**
 * How many units fit between the first and last holes which count on stock `length` long, leaving a margin of
 * `tie.start` at each end. Its regular part is `2 * tie.start + spacings * tie.unit` long, and the rest is at its end.
 */
function regularSpacings(length is ValueWithUnits, tie is map) returns number
{
    const spacings = floor((length - 2 * tie.start) / tie.unit);
    // Not one fewer for a length just short of a whole number of units
    return tolerantEquals(length - 2 * tie.start, (spacings + 1) * tie.unit) ? spacings + 1 : spacings;
}

/**
 * Where a row's holes (or groups of them) go along stock `length` long: maps of `position`, and whether it's `tied` to
 * the end (see `getTie`), in order from the start. Only holes which fit whole in its regular part are kept, so the
 * extra length at the end of a part which isn't a regular length (see `isRegularLength`) has none.
 *
 * @param extent : How far each hole reaches along the stock from its position, e.g. its radius.
 */
export function holePositions(row is HoleRow, extent is ValueWithUnits, length is ValueWithUnits, tie is map) returns array
{
    const spacings = regularSpacings(length, tie);
    const end = 2 * tie.start + spacings * tie.unit;
    // Holes are tied if they're around the holes which count that are
    const firstTied = firstTiedHole(length, spacings, tie);
    var positions = [];
    for (var position = row.start; spacings >= 0 && tolerantLessThanOrEqual(position + extent, end); position += row.pitch)
    {
        const spacing = round((position - tie.start) / tie.unit);
        positions = append(positions, { "position" : position, "tied" : spacing >= firstTied });
    }
    return positions;
}

/**
 * Whether stock `length` long is a whole number of hole spacings long (see `getTie`), so the margin after its last hole
 * which counts is the same as the one before its first. Otherwise, the extra length is at its end.
 */
export function isRegularLength(length is ValueWithUnits, tie is map) returns boolean
{
    return tolerantLessThanOrEqual(length - 2 * tie.start - max(regularSpacings(length, tie), 0) * tie.unit, 0 * meter);
}

// Building

/** The radius of the MAXSpline cutouts of MAXTube with MAX Pattern: its teeth's tips (see splineProfiles.py). */
const MAX_SPLINE_RADIUS = 17.45 * millimeter;

/**
 * Builds stock `length` long from `location` (the middle of its start, with Z along its length and its width along
 * X): its profile is extruded along its length, then its holes are cut.
 *
 * Booleans are slow, so each row's holes are cut by one seed (a tool for its first hole), and the seed's faces are face
 * patterned along the stock. A row's holes tied to the end (see `getTie`) have a seed of their own at the last of them,
 * patterned back toward the start, so their identities are counted from the end. Seeds patterned the same way are cut
 * and patterned together (see `seedGroups`), each group under its own id, so the faces it made are
 * `qCreatedBy(groupId, EntityType.FACE)`.
 *
 * @returns {{
 *      @field body {Query}
 *      @field endFace {Query} : The face at its end.
 *      @field tiedHoles {Query} : The faces of the holes tied to the end.
 *      @field tie {map} : Its holes' tie (see `getTie`), or `undefined` if it has none.
 * }}
 */
export function buildStock(context is Context, id is Id, definition is map, stock is Stock, location is CoordSystem,
    length is ValueWithUnits) returns map
{
    const profileId = id + "profile";
    const sketch = newSketchOnPlane(context, profileId, { "sketchPlane" : plane(location) });
    sketchProfile(sketch, stock);
    skSolve(sketch);
    const stockId = id + "stock";
    opExtrude(context, stockId, {
                "entities" : qSketchRegion(profileId, true),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : length
            });
    opDeleteBodies(context, id + "deleteProfile", { "entities" : qCreatedBy(profileId, EntityType.BODY) });
    const body = qCreatedBy(stockId, EntityType.BODY);
    if (stock.isFrame ?? false)
    {
        // Before the holes are cut, so its faces are still the extrude's
        tagExtrudeAsFrame(context, stockId, stock.partName, stock.vendor);
    }

    const tie = getTie(definition, stock.tieStart, stock.tieUnit);
    var tiedHoles = [];
    var threadedHoles = [];
    for (var i, group in seedGroups(stock, location, length, tie))
    {
        const groupId = id + ("holes" ~ i);
        cutHoles(context, groupId, group, body, stock.holeDiameter);
        if (group.tied)
        {
            tiedHoles = append(tiedHoles, qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body));
        }
        if (stock.thread != undefined)
        {
            threadedHoles = concatenateArrays([threadedHoles, groupHoles(context, groupId, group, body)]);
        }
    }
    if (stock.thread != undefined)
    {
        if (stock.centerHole ?? false)
        {
            threadedHoles = concatenateArrays([threadedHoles, centerHole(context, body, location)]);
        }
        setTappedThroughHoles(context, id, threadedHoles, stock.thread);
    }
    return {
            "body" : body,
            "endFace" : qCapEntity(stockId, CapType.END, EntityType.FACE),
            "tiedHoles" : qUnion(tiedHoles),
            "tie" : stock.xRows != [] || stock.yRows != [] ? tie : undefined
        };
}

/**
 * Sketches stock's profile, centered on the origin with its width along X.
 */
function sketchProfile(sketch is Sketch, stock is Stock)
{
    const halfWidth = stock.width / 2;
    const halfHeight = stock.height / 2;
    const outside = { "firstCorner" : vector(-halfWidth, -halfHeight), "secondCorner" : vector(halfWidth, halfHeight) };
    if (stock.profile != undefined)
    {
        skDataArray(sketch, "profile", { "sketchDataArray" : stock.profile });
    }
    else if (stock.solid ?? false)
    {
        skRectangle(sketch, "outside", outside);
        if (stock.centerHole ?? false)
        {
            skCircle(sketch, "centerHole", { "center" : vector(0, 0) * meter, "radius" : stock.holeDiameter / 2 });
        }
    }
    else if (stock.angle ?? false)
    {
        // Its legs are the walls facing -X and -Y
        skPolyline(sketch, "profile", {
                    "points" : [
                            vector(-halfWidth, -halfHeight), vector(halfWidth, -halfHeight), vector(halfWidth, -halfHeight + stock.wallY),
                            vector(-halfWidth + stock.wallX, -halfHeight + stock.wallY), vector(-halfWidth + stock.wallX, halfHeight),
                            vector(-halfWidth, halfHeight), vector(-halfWidth, -halfHeight)
                        ]
                });
    }
    else if (stock.open ?? false)
    {
        const innerX = halfWidth - stock.wallX;
        const innerY = -halfHeight + stock.wallY;
        skPolyline(sketch, "profile", {
                    "points" : [
                            vector(-halfWidth, halfHeight), vector(-halfWidth, -halfHeight), vector(halfWidth, -halfHeight),
                            vector(halfWidth, halfHeight), vector(innerX, halfHeight), vector(innerX, innerY),
                            vector(-innerX, innerY), vector(-innerX, halfHeight), vector(-halfWidth, halfHeight)
                        ]
                });
    }
    else if (stock.bore != undefined)
    {
        skRectangle(sketch, "outside", outside);
        skCircle(sketch, "bore", { "center" : vector(0, 0) * meter, "radius" : stock.bore / 2 });
    }
    else
    {
        skRectangle(sketch, "outside", outside);
        skRectangle(sketch, "inside", {
                    "firstCorner" : vector(-halfWidth + stock.wallX, -halfHeight + stock.wallY),
                    "secondCorner" : vector(halfWidth - stock.wallX, halfHeight - stock.wallY)
                });
    }
}

/**
 * The faces of stock its rows of holes go in from: the walls facing -X and -Y. Each has its rows, the `plane` its holes
 * are sketched on (with X along the stock, so its Y is the stock's -Y and X respectively, as `sign` says), and how
 * `deep` its holes go to get through the stock.
 */
function stockFaces(stock is Stock, location is CoordSystem) returns array
{
    return [
            {
                "name" : "x",
                "rows" : stock.xRows,
                "plane" : plane(toWorld(location, vector(-stock.width / 2, 0 * meter, 0 * meter)), location.xAxis, location.zAxis),
                "sign" : -1,
                "depth" : stock.width
            },
            {
                "name" : "y",
                "rows" : stock.yRows,
                "plane" : plane(toWorld(location, vector(0 * meter, -stock.height / 2, 0 * meter)), yAxis(location), location.zAxis),
                "sign" : 1,
                "depth" : stock.height
            }
        ];
}

/**
 * The seeds which cut stock's holes (see `buildStock`), in groups patterned the same way (as many times, in the same
 * direction), which are cut and patterned together, in fewer operations. Holes which cross, like those of a solid nut
 * strip's rows which line up, have to be.
 *
 * @returns {array} : Maps of each group's `count` (how many holes its seeds are patterned into, including themselves),
 *          the `step` between them, whether they're `tied`, and its `seeds`: maps of the `face` (see `stockFaces`) and
 *          row `shapes` they cut, and their `position` along the stock.
 */
function seedGroups(stock is Stock, location is CoordSystem, length is ValueWithUnits, tie is map) returns array
{
    var groups = [];
    for (var face in stockFaces(stock, location))
    {
        for (var row in face.rows)
        {
            const positions = holePositions(row, rowExtent(row, stock.holeDiameter), length, tie);
            for (var tied in [false, true])
            {
                const holes = filter(positions, function(hole)
                    {
                        return hole.tied == tied;
                    });
                if (holes == [])
                {
                    continue;
                }
                const seed = {
                        "face" : face,
                        "shapes" : row.shapes,
                        "position" : tied ? holes[size(holes) - 1].position : holes[0].position
                    };
                const count = size(holes);
                const step = location.zAxis * (tied ? -row.pitch : row.pitch);
                var found = false;
                for (var i, group in groups)
                {
                    if (group.count == count && group.tied == tied && tolerantEquals(group.step, step))
                    {
                        groups[i].seeds = append(group.seeds, seed);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    groups = append(groups, { "count" : count, "step" : step, "tied" : tied, "seeds" : [seed] });
                }
            }
        }
    }
    return groups;
}

/**
 * Cuts a group of seeds (see `seedGroups`) from the body with one boolean, then face patterns the holes they made along
 * it, all under `groupId`.
 */
function cutHoles(context is Context, groupId is Id, group is map, body is Query, holeDiameter is ValueWithUnits)
{
    for (var i, seed in group.seeds)
    {
        sketchSeed(context, groupId + ("seed" ~ i), seed.face, seed.shapes, seed.position, holeDiameter);
    }
    opBoolean(context, groupId + "cut", {
                "tools" : qCreatedBy(groupId, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "targets" : body,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    opDeleteBodies(context, groupId + "deleteSketches", {
                "entities" : qCreatedBy(groupId, EntityType.BODY)->qSketchFilter(SketchObject.YES)
            });
    if (group.count > 1)
    {
        const instances = range(1, group.count - 1);
        opPattern(context, groupId + "pattern", {
                    "entities" : qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body),
                    "transforms" : mapArray(instances, function(k)
                        {
                            return transform(group.step * k);
                        }),
                    "instanceNames" : mapArray(instances, holeInstanceName)
                });
    }
}

/**
 * The name of the pattern instance which is a group's `k`th hole after its seeds (see `cutHoles`). A const, so it can be
 * passed to `mapArray`.
 */
const holeInstanceName = function(k is number) returns string
    {
        return "" ~ k;
    };

/**
 * The holes a group of seeds cut (see `cutHoles`), for `setTappedThroughHoles`: maps of each hole's `faces` and
 * `coordSystem`. Each of the group's holes is its seeds' faces (its first) or a pattern instance's (see
 * `qPatternInstances`), split between its seeds by the direction they go through the stock.
 */
function groupHoles(context is Context, groupId is Id, group is map, body is Query) returns array
{
    const pattern = groupId + "pattern";
    var holes = [];
    for (var k = 0; k < group.count; k += 1)
    {
        const faces = k == 0 ?
            qSubtraction(qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body), qCreatedBy(pattern, EntityType.FACE)) :
            qPatternInstances(pattern, holeInstanceName(k), EntityType.FACE);
        for (var seed in group.seeds)
        {
            const plane = seed.face.plane;
            const seedFaces = filter(evaluateQuery(context, faces->qGeometry(GeometryType.CYLINDER)), function(face)
                {
                    return parallelVectors(evSurfaceDefinition(context, { "face" : face }).coordSystem.zAxis, plane.normal);
                });
            if (seedFaces != [])
            {
                const position = seed.position + dot(group.step, plane.x) * k;
                holes = append(holes, {
                            "faces" : qUnion(seedFaces),
                            "coordSystem" : coordSystem(plane.origin + plane.x * position, plane.x, plane.normal)
                        });
            }
        }
    }
    return holes;
}

/**
 * A `solid` profile's center hole (see `Stock`), for `setTappedThroughHoles`: the body's cylindrical faces along it, if
 * it has any.
 */
function centerHole(context is Context, body is Query, location is CoordSystem) returns array
{
    const faces = filter(evaluateQuery(context, qOwnedByBody(body, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)), function(face)
        {
            return parallelVectors(evSurfaceDefinition(context, { "face" : face }).coordSystem.zAxis, location.zAxis);
        });
    return faces == [] ? [] : [{ "faces" : qUnion(faces), "coordSystem" : coordSystem(location.origin, location.xAxis, location.zAxis) }];
}

/**
 * How far a row's shapes reach along the stock from its position.
 */
function rowExtent(row is HoleRow, holeDiameter is ValueWithUnits) returns ValueWithUnits
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
 * Sketches a row's `shapes` at `position` along `face`, as `seedId + "sketch"`, and extrudes them through the stock as
 * a tool, `seedId + "tool"`.
 */
function sketchSeed(context is Context, seedId is Id, face is map, shapes is array, position is ValueWithUnits, holeDiameter is ValueWithUnits)
{
    const sketch = newSketchOnPlane(context, seedId + "sketch", { "sketchPlane" : face.plane });
    for (var i, shape in shapes)
    {
        sketchShape(sketch, "shape" ~ i, vector(position + shape.along, face.sign * shape.offset), shape, holeDiameter);
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
 * Sketches one shape of a row (see `HoleRow`) at `center`.
 */
function sketchShape(sketch is Sketch, name is string, center is Vector, shape is map, holeDiameter is ValueWithUnits)
{
    if (shape.maxSpline ?? false)
    {
        skDataArray(sketch, name, { "sketchDataArray" : MAX_SPLINE_HOLE, "location" : center });
        return;
    }
    const radius = (shape.diameter ?? holeDiameter) / 2;
    const slot = shape.slot ?? 0 * meter;
    if (tolerantEquals(slot, 0 * meter))
    {
        skCircle(sketch, name, { "center" : center, "radius" : radius });
        return;
    }
    // A slot along the stock: two sides and two round ends
    const along = vector(slot / 2, 0 * meter);
    const across = vector(0 * meter, radius);
    skLineSegment(sketch, name ~ "side", { "start" : center - along - across, "end" : center + along - across });
    skArc(sketch, name ~ "end", {
                "start" : center + along - across,
                "mid" : center + along + vector(radius, 0 * meter),
                "end" : center + along + across
            });
    skLineSegment(sketch, name ~ "otherSide", { "start" : center + along + across, "end" : center - along + across });
    skArc(sketch, name ~ "otherEnd", {
                "start" : center - along + across,
                "mid" : center - along - vector(radius, 0 * meter),
                "end" : center - along - across
            });
}

// Placing

const START_OFFSET_MANIPULATOR = "startOffsetManipulator";
const END_OFFSET_MANIPULATOR = "endOffsetManipulator";
const FLIP_MANIPULATOR = "flipManipulator";

/**
 * Builds stock (see `buildStock`) along the selected edge, or extrudes it from the selected point, and names it and
 * sets its properties (see `setStockProperties`).
 *
 * @param name : What's placed, e.g. `"nut strip"`, for messages.
 */
export function placeStock(context is Context, id is Id, definition is map, stock is Stock, name is string)
{
    var length;
    var location;
    if (isEdgePlacement(definition))
    {
        const edge = verifyNonemptyQuery(context, definition, "edge", "Select an edge to place the " ~ name ~ " on.")[0];
        const startOffset = definition.hasStartOffset ? definition.edgeStartOffset : 0 * meter;
        const endOffset = definition.hasEndOffset ? definition.edgeEndOffset : 0 * meter;
        const middle = edgeCoordSystem(context, edge);
        const edgeLength = evLength(context, { "entities" : edge });
        // At the start of the edge, in the direction the stock is drawn
        location = orientStock(definition, middle);
        location.origin -= location.zAxis * edgeLength / 2;
        const edgeStart = location.origin;
        location.origin += location.zAxis * startOffset;

        length = edgeLength - startOffset - endOffset;
        if (tolerantLessThanOrEqual(length, 0 * meter))
        {
            throw regenError("The offsets leave no room.", ["edgeStartOffset", "edgeEndOffset"], edge);
        }
        addOffsetManipulators(context, id, definition, edgeStart, location.zAxis, edgeLength, startOffset, endOffset);
        addManipulators(context, id, {
                    (FLIP_MANIPULATOR) : flipManipulator({
                            "base" : middle.origin,
                            "direction" : middle.zAxis,
                            "flipped" : isFlipped(definition)
                        })
                });
    }
    else
    {
        const extruded = extrudeLength(context, id, definition);
        location = extruded.location;
        length = extruded.length;
    }

    // Centered on the chosen point of its profile
    const offsets = stockPointOffsets(definition, stock);
    var center = location;
    center.origin = toWorld(location, -offsets[getNinePointIndex(definition)]);
    const built = buildStock(context, id, definition, stock, center, length);
    setStockProperties(context, built.body, definition, stock, length);
    // Halfway along it
    addPointManipulator(context, id, definition, mapArray(offsets, function(offset)
            {
                return toWorld(center, offset + vector(0 * meter, 0 * meter, length / 2));
            }), getNinePointIndex(definition));

    if (definition.tieHoles && definition.showTiedHoles)
    {
        const shown = qUnion([built.endFace, built.tiedHoles]);
        if (definition.colorTiedHoles ?? false)
        {
            setProperty(context, { "entities" : shown, "propertyType" : PropertyType.APPEARANCE, "value" : TIED_HOLE_COLOR });
        }
        else
        {
            addDebugEntities(context, shown, DebugColor.BLUE);
        }
    }
    const sold = stock.stock == [] || stockFor(stock, length) != undefined ? undefined :
        "This is only sold up to " ~ lengthString(definition, stock.stock[size(stock.stock) - 1].length) ~ " long.";
    const irregular = built.tie == undefined || isRegularLength(length, built.tie) ? undefined : regularLengthMessage(definition, name, built.tie);
    if (sold != undefined)
    {
        reportFeatureWarning(context, id, sold ~ (irregular == undefined ? "" : " " ~ irregular), [isEdgePlacement(definition) ? "edge" : "depth"]);
    }
    else if (irregular != undefined)
    {
        reportFeatureInfo(context, id, irregular);
    }
}

/**
 * A coordinate system at the middle of a line edge, with Z along the edge and X along its sketch's normal (or, for edges
 * not in a sketch, an arbitrary perpendicular direction).
 */
function edgeCoordSystem(context is Context, edge is Query) returns CoordSystem
{
    const line = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 });
    const xAxis = isQueryEmpty(context, edge->qSketchFilter(SketchObject.YES)) ?
        perpendicularVector(line.direction) :
        evOwnerSketchPlane(context, { "entity" : edge }).normal;
    return coordSystem(line.origin, xAxis, line.direction);
}

/**
 * The coordinate system stock is drawn in from `base` (Z along the edge or extrude), rotated by `secondaryAxisType` and
 * flipped (see `isFlipped`).
 *
 * Flipping turns it about its Y axis rather than mirroring it, so X runs the other way too; `stockPointOffsets` mirrors
 * the nine points to match, so flipped stock stays where it was, and only which end it's drawn from changes.
 */
function orientStock(definition is map, base is CoordSystem) returns CoordSystem
{
    const axisTypes = [MateConnectorAxisType.PLUS_X, MateConnectorAxisType.PLUS_Y, MateConnectorAxisType.MINUS_X, MateConnectorAxisType.MINUS_Y];
    const axes = [base.xAxis, yAxis(base), -base.xAxis, -yAxis(base)];
    var xAxis = base.xAxis;
    for (var i, axisType in axisTypes)
    {
        if (definition.secondaryAxisType == axisType)
        {
            xAxis = axes[i];
        }
    }
    return isFlipped(definition) ?
        coordSystem(base.origin, -xAxis, -base.zAxis) :
        coordSystem(base.origin, xAxis, base.zAxis);
}

/**
 * Whether stock is drawn from the other end of its edge (or of the extruded length), with `flip`.
 */
function isFlipped(definition is map) returns boolean
{
    return definition.flip is boolean && definition.flip;
}

/**
 * The nine points of stock's profile, relative to its center (see `ninePointOffsets`), mirrored across its Y axis when
 * it's flipped (see `orientStock`), so each index stays at the same place.
 */
function stockPointOffsets(definition is map, stock is Stock) returns array
{
    const offsets = ninePointOffsets(stock.width, stock.height);
    if (!isFlipped(definition))
    {
        return offsets;
    }
    return mapArray(offsets, function(offset)
        {
            return vector(-offset[0], offset[1], offset[2]);
        });
}

/**
 * Says what lengths stock with holes `tie` is a regular length at (see `isRegularLength`), e.g. "The nut strip's
 * length should be a multiple of 0.5 in."
 */
function regularLengthMessage(definition is map, name is string, tie is map) returns string
{
    const multiple = "a multiple of " ~ lengthString(definition, tie.unit);
    // Regular lengths are 2 * tie.start more than a multiple of tie.unit
    const extra = (2 * tie.start) % tie.unit;
    if (tolerantEquals(extra, 0 * meter) || tolerantEquals(extra, tie.unit))
    {
        return sentence("The " ~ name ~ "'s length should be " ~ multiple);
    }
    return sentence("The " ~ name ~ "'s length should be " ~ lengthString(definition, extra) ~ " more than " ~ multiple);
}

/**
 * Ends text with a period, unless it already does (as an inch length does, e.g. `0.5 in.`).
 */
function sentence(text is string) returns string
{
    return endsWith(text, ".") ? text : text ~ ".";
}

function addOffsetManipulators(context is Context, id is Id, definition is map, edgeStart is Vector, direction is Vector,
    edgeLength is ValueWithUnits, startOffset is ValueWithUnits, endOffset is ValueWithUnits)
{
    if (definition.hasStartOffset)
    {
        addManipulators(context, id, {
                    (START_OFFSET_MANIPULATOR) : linearManipulator({
                            "base" : edgeStart,
                            "direction" : direction,
                            "offset" : startOffset,
                            "primaryParameterId" : "edgeStartOffset"
                        })
                });
    }
    if (definition.hasEndOffset)
    {
        addManipulators(context, id, {
                    (END_OFFSET_MANIPULATOR) : linearManipulator({
                            "base" : edgeStart + direction * edgeLength,
                            "direction" : -direction,
                            "offset" : endOffset,
                            "primaryParameterId" : "edgeEndOffset"
                        })
                });
    }
}

/**
 * Extrudes a small face from the selected point with the extrude options and measures it, so they (and their
 * manipulators) work as usual. Returns the `location` stock starts at (along the extrude's direction, oriented by
 * `orientStock`) and its `length`.
 */
function extrudeLength(context is Context, id is Id, definition is map) returns map
{
    const facePlane = lengthFacePlane(context, definition);
    // Use the top level id to get extrude's manipulators
    const extrudeDefinition = transformDefintionForNewExtrude(definition, sketchLengthFace(context, id + "lengthFace", facePlane));
    callSubfeatureAndProcessStatus(id, extrude, context, id, extrudeDefinition, {
                "featureParameterMap" : { "entities" : "location" }
            });

    const location = orientStock(definition, coordSystem(facePlane));
    // The extruded face's ends, which are on its axis
    const ends = mapArray([CapType.START, CapType.END], function(capType)
        {
            const cap = evApproximateCentroid(context, { "entities" : qCapEntity(id, capType, EntityType.FACE) });
            return dot(cap - location.origin, location.zAxis);
        });
    opDeleteBodies(context, id + "deleteLength", { "entities" : qCreatedBy(id, EntityType.BODY) });

    var start = location;
    start.origin += location.zAxis * min(ends);
    return { "location" : start, "length" : abs(ends[1] - ends[0]) };
}

/**
 * The plane the extruded face is sketched on: at the selected point, normal to the extrude's direction.
 */
function lengthFacePlane(context is Context, definition is map) returns Plane
{
    return extrudeDirectionPlane(context, definition, getLocationPlane(context, definition));
}

function sketchLengthFace(context is Context, id is Id, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(sketch, "circle", { "center" : vector(0, 0) * meter, "radius" : 1 * millimeter });
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

// Properties

/**
 * The stock a part `length` long is cut from: the shortest length it's sold in that's long enough, or `undefined` if
 * it's longer than all of them.
 *
 * @param part : Has a `stock` array of the lengths it's sold in, shortest first: maps of `length`, `partNumber`, and
 *          `url`.
 */
export function stockFor(part is Stock, length is ValueWithUnits)
{
    for (var stock in part.stock)
    {
        // Lengths are only shown to 3 decimal places, so a part which looks like stock length is
        if (tolerantLessThanOrEqual(length, stock.length + 0.001 * inch))
        {
            return stock;
        }
    }
    return undefined;
}

/**
 * A length in inches for FRC or millimeters for FTC, e.g. `6 in.` or `136 mm`.
 */
export function lengthString(definition is map, length is ValueWithUnits) returns string
{
    return isFrc(definition) ?
        roundToPrecision(length / inch, 3) ~ " in." :
        roundToPrecision(length / millimeter, 1) ~ " mm";
}

/**
 * Names stock (its length, then its `partName`), gives it the part number of the stock it's cut from and a link to buy
 * it, and sets its material and appearance.
 */
export function setStockProperties(context is Context, body is Query, definition is map, part is Stock, length is ValueWithUnits)
{
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.NAME,
                "value" : lengthString(definition, length) ~ " " ~ part.partName
            });
    const stock = stockFor(part, length);
    if (stock != undefined)
    {
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.PART_NUMBER,
                    "value" : stock.partNumber
                });
    }
    const url = stock == undefined ? part.url : stock.url;
    if (url != "")
    {
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.DESCRIPTION,
                    "value" : url
                });
    }
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.MATERIAL,
                "value" : ALUMINUM
            });
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : part.appearance
            });
}

/**
 * Applies the manipulators' changes. Use it as the feature's manipulator change function.
 */
export function stockManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    // The nine points are numbered in the unflipped stock's orientation (see `stockPointOffsets`), so the manipulator's
    // index is the parameter's either way
    definition = pointManipulatorChange(definition, newManipulators);
    if (isEdgePlacement(definition))
    {
        const flip = newManipulators[FLIP_MANIPULATOR];
        if (flip != undefined && flip.flipped is boolean)
        {
            definition.flip = flip.flipped;
        }
        const startOffset = newManipulators[START_OFFSET_MANIPULATOR];
        if (startOffset != undefined && isLength(startOffset.offset))
        {
            definition.edgeStartOffset = startOffset.offset;
        }
        const endOffset = newManipulators[END_OFFSET_MANIPULATOR];
        if (endOffset != undefined && isLength(endOffset.offset))
        {
            definition.edgeEndOffset = endOffset.offset;
        }
        return definition;
    }
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Call from the feature's editing logic function.
 *
 * @param defaultOffset : What each end's offset is set to when it's turned on (or when `partChanged`), unless it's been
 *          set; `undefined` to keep the bounds' default.
 * @param partChanged : Whether the part has changed.
 */
export function stockEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map,
    hiddenBodies is Query, defaultOffset, partChanged is boolean) returns map
{
    // Nothing to do when the feature is created
    if (oldDefinition == {})
    {
        return definition;
    }
    if (isEdgePlacement(definition))
    {
        if (defaultOffset != undefined)
        {
            for (var offset in { "edgeStartOffset" : "hasStartOffset", "edgeEndOffset" : "hasEndOffset" })
            {
                const turnedOn = definition[offset.value] && !(oldDefinition[offset.value] ?? false);
                if ((turnedOn || partChanged) && !(specifiedParameters[offset.key] ?? false))
                {
                    definition[offset.key] = defaultOffset;
                }
            }
        }
        return definition;
    }
    definition.entities = qNothing();
    if (!isQueryEmpty(context, definition.location))
    {
        try silent
        {
            definition.entities = sketchLengthFace(context, id + "lengthFace", lengthFacePlane(context, definition));
        }
    }
    return stdNewExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
