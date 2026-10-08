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
export import(path : "21762d39019c8b2289e2fbb8", version : "c49d2d2fae0776d783a28e44");
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
 * extrude's options (see `isEdgePlacement`), in a Position group; then, on an edge, a Trim ends group of faces to trim
 * its ends to (like miters). Either way, `flip` (Flip hole pattern, and a flip manipulator) draws it from the other end, a button rotates it in 90 degree increments, and a nine point manipulator
 * chooses which point of its profile is on the edge or point (see `orientStock`). From a point, `oppositeDirection`
 * (Flip primary axis) is the extrude's.
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
            definition.endBound is StockBoundingType;

            // The extrude's opposite direction, on the row with the rotate button
            annotation { "Name" : "Flip primary axis", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
            definition.oppositeDirection is boolean;
        }

        // Shared by both placements, since Onshape doesn't allow declaring a parameter twice (even in different branches)
        secondaryAxisPredicate(definition);

        annotation { "Name" : "Flip hole pattern" }
        definition.flip is boolean;

        ninePointManipulatorPredicate(definition);

        if (isEdgePlacement(definition))
        {
            stockOffsetsPredicate(definition);
        }
        else
        {
            stockBoundsPredicate(definition);
            extrudeDirectionPredicate(definition);
            newExtrudeOptionsPredicate(definition);
        }
    }

    if (isEdgePlacement(definition))
    {
        annotation { "Name" : "Trim " ~ name ~ " ends" }
        definition.trimEnds is boolean;

        annotation { "Group Name" : "Trim " ~ name ~ " ends", "Driving Parameter" : "trimEnds", "Collapsed By Default" : false }
        {
            if (definition.trimEnds)
            {
                annotation { "Name" : "Faces to trim to", "Filter" : (EntityType.FACE && GeometryType.PLANE) || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 2 }
                definition.trimFaces is Query;
            }
        }
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
            annotation { "Name" : "Tie hole method", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
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
    else if (tie.by == TieHolesBy.COUNT)
    {
        first = spacings + 1 - tie.count;
    }
    else if (tie.by == TieHolesBy.LENGTH)
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
 * The shortest regular length (see `isRegularLength`) at least `length` long: what stock is built as before it's trimmed
 * to `length` (see `buildStock`), so a part which isn't a regular length has the holes it would have if it were cut from
 * longer stock, the last of them cut through by its end.
 */
export function regularLength(length is ValueWithUnits, tie is map) returns ValueWithUnits
{
    var spacings = ceil((length - 2 * tie.start) / tie.unit);
    // Not one more for a length just over a whole number of units
    if (tolerantEquals(length - 2 * tie.start, (spacings - 1) * tie.unit))
    {
        spacings -= 1;
    }
    return max(length, 2 * tie.start + max(spacings, 0) * tie.unit);
}

/**
 * Where a row's holes (or groups of them) go along stock `length` long: maps of `position`, and whether it's `tied` to
 * the end (see `getTie`), in order from the start. Holes are laid out on the regular length at least `length` long (see
 * `regularLength`), and those which start before its end are kept, so the last of them may be cut through by the end.
 *
 * @param extent : How far each hole reaches along the stock from its position, e.g. its radius.
 */
export function holePositions(row is HoleRow, extent is ValueWithUnits, length is ValueWithUnits, tie is map) returns array
{
    const built = regularLength(length, tie);
    const spacings = regularSpacings(built, tie);
    // Holes are tied if they're around the holes which count that are
    const firstTied = firstTiedHole(length, spacings, tie);
    var positions = [];
    for (var position = row.start; spacings >= 0 && tolerantLessThanOrEqual(position + extent, built); position += row.pitch)
    {
        if (tolerantLessThanOrEqual(length, position - extent))
        {
            // Wholly past the end
            break;
        }
        const spacing = round((position - tie.start) / tie.unit);
        positions = append(positions, { "position" : position, "tied" : spacing >= firstTied });
    }
    return positions;
}

/**
 * Whether stock `length` long is a whole number of hole spacings long (see `getTie`), so the margin after its last hole
 * which counts is the same as the one before its first. Otherwise, the extra length is at its end, where the pattern
 * of holes carries on, cut through by the end (see `regularLength`).
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
 * X): its profile is extruded along its length, then its holes are cut, then its ends are trimmed.
 *
 * Stock with holes is built as the regular length at least `length` long (see `regularLength`), so its holes are all
 * whole while they're cut, and is then cut to `length`, cutting through any holes the end crosses, as cutting it from
 * longer stock would. Ends can be slanted (like miters): `ends` has the `startPlane` and `endPlane` they're on, which
 * are inside its length, and other `cuts` to make (see `trimCuts`); it's cut to each (see `trimBeyond`), and the face
 * each cut leaves is a frame's cap.
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
    length is ValueWithUnits, ends is map) returns map
{
    const tie = getTie(definition, stock.tieStart, stock.tieUnit);
    const hasHoles = stock.xRows != [] || stock.yRows != [];
    const built = hasHoles ? regularLength(length, tie) : length;
    const profileId = id + "profile";
    const sketch = newSketchOnPlane(context, profileId, { "sketchPlane" : plane(location) });
    sketchProfile(sketch, stock);
    skSolve(sketch);
    const stockId = id + "stock";
    opExtrude(context, stockId, {
                "entities" : qSketchRegion(profileId, true),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : built
            });
    opDeleteBodies(context, id + "deleteProfile", { "entities" : qCreatedBy(profileId, EntityType.BODY) });
    const body = qCreatedBy(stockId, EntityType.BODY);
    if (stock.isFrame ?? false)
    {
        // Before the holes are cut, so its faces are still the extrude's
        tagExtrudeAsFrame(context, stockId, stock.partName, stock.vendor);
    }

    var tiedHoles = [];
    const groups = seedGroups(stock, location, length, tie);
    for (var i, group in groups)
    {
        const groupId = id + ("holes" ~ i);
        cutHoles(context, groupId, group, body, stock.holeDiameter);
        if (group.tied)
        {
            tiedHoles = append(tiedHoles, qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body));
        }
    }

    // Cut to its ends and trims (after its holes are cut, so they're cut through like the stock it's cut from)
    var cuts = ends.cuts;
    if (ends.endPlane != undefined || !tolerantEquals(built, length))
    {
        const endPlane = ends.endPlane ?? plane(location.origin + location.zAxis * length, location.zAxis);
        cuts = append(cuts, { "plane" : endPlane, "away" : location.zAxis });
    }
    if (ends.startPlane != undefined)
    {
        cuts = append(cuts, { "plane" : ends.startPlane, "away" : -location.zAxis });
    }
    var endFace = qCapEntity(stockId, CapType.END, EntityType.FACE);
    const reach = built + stock.width + stock.height;
    for (var i, cut in cuts)
    {
        const isEnd = dot(cut.away, location.zAxis) > 0;
        const face = trimBeyond(context, id + ("cut" ~ i), body, cut.plane, cut.away, reach);
        if (stock.isFrame ?? false)
        {
            tagFrameCaps(context, face, !isEnd);
        }
        if (isEnd)
        {
            endFace = qUnion([endFace->qOwnedByBody(body), face]);
        }
    }

    // After trimming, so holes the trims removed aren't threaded
    if (stock.thread != undefined)
    {
        var threadedHoles = [];
        for (var i, group in groups)
        {
            threadedHoles = concatenateArrays([threadedHoles, groupHoles(context, id + ("holes" ~ i), group, body)]);
        }
        if (stock.centerHole ?? false)
        {
            threadedHoles = concatenateArrays([threadedHoles, centerHole(context, body, location)]);
        }
        setTappedThroughHoles(context, id, threadedHoles, stock.thread);
    }
    return {
            "body" : body,
            "endFace" : endFace,
            "tiedHoles" : qUnion(tiedHoles),
            "tie" : hasHoles ? tie : undefined
        };
}

/**
 * Cuts away the part of `body` beyond `cut`, in `direction` (which way is out of the body, for either way the plane
 * faces), with a block `reach` across. Returns the face the cut leaves.
 */
function trimBeyond(context is Context, id is Id, body is Query, cut is Plane, direction is Vector, reach is ValueWithUnits) returns Query
{
    const outward = dot(cut.normal, direction) > 0 ? cut.normal : -cut.normal;
    const sketchPlane = plane(cut.origin, outward);
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : sketchPlane });
    skRectangle(sketch, "block", { "firstCorner" : vector(-reach, -reach), "secondCorner" : vector(reach, reach) });
    skSolve(sketch);
    opExtrude(context, id + "tool", {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : outward,
                "endBound" : BoundingType.BLIND,
                "endDepth" : reach
            });
    opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(id + "sketch", EntityType.BODY) });
    opBoolean(context, id + "cut", {
                "tools" : qCreatedBy(id + "tool", EntityType.BODY),
                "targets" : body,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    return qCreatedBy(id + "tool", EntityType.FACE)->qOwnedByBody(body);
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
    if (tolerantEqualsZero(slot))
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
// Not "flipManipulator", which is std's extrude's (see `addExtrudeManipulator`)
const FLIP_MANIPULATOR = "stockFlipManipulator";

/**
 * Builds stock (see `buildStock`) along the selected edge, or extrudes it from the selected point, and names it and
 * sets its properties (see `setStockProperties`).
 *
 * @param name : What's placed, e.g. `"nut strip"`, for messages.
 */
export function placeStock(context is Context, id is Id, definition is map, stock is Stock, name is string)
{
    const placement = isEdgePlacement(definition) ?
        edgePlacement(context, id, definition, name) :
        extrudePlacement(context, id, definition, name);

    // Centered on the chosen point of its profile
    const offsets = stockPointOffsets(definition, stock);
    var center = placement.location;
    center.origin = toWorld(placement.location, -offsets[getNinePointIndex(definition)]);
    // From the farthest back point of a slanted start to the farthest point of a slanted end, as an extrude up to them
    // would reach
    const span = stockSpan(center, stock, placement.startPlane, placement.endPlane, placement.length);
    center.origin += center.zAxis * span.start;
    var length = span.end - span.start;
    if (tolerantLessThanOrEqual(length, 0 * meter))
    {
        throw regenError("The " ~ name ~ " has no length.", placement.errorParameters);
    }
    const cuts = definition.trimEnds && isEdgePlacement(definition) ? trimCuts(context, definition, stock, center, length, name) : [];
    const built = buildStock(context, id, definition, stock, center, length,
        { "startPlane" : placement.startPlane, "endPlane" : placement.endPlane, "cuts" : cuts });
    if (cuts != [])
    {
        // Trimming shortens it
        length = stockExtent(context, built.body, center);
    }
    setStockProperties(context, built.body, definition, stock, length);
    // Halfway along it
    addPointManipulator(context, id, definition, mapArray(offsets, function(offset)
            {
                return toWorld(center, offset + vector(0 * meter, 0 * meter, length / 2));
            }), getNinePointIndex(definition));
    addManipulators(context, id, {
                (FLIP_MANIPULATOR) : flipManipulator({
                        "base" : center.origin + center.zAxis * length / 2,
                        // The way it's drawn unflipped
                        "direction" : isFlipped(definition) ? -center.zAxis : center.zAxis,
                        "flipped" : isFlipped(definition)
                    })
            });

    if (definition.tieHoles && definition.showTiedHoles)
    {
        const shown = qUnion([built.endFace, built.tiedHoles]);
        if (definition.colorTiedHoles)
        {
            setProperty(context, { "entities" : shown, "propertyType" : PropertyType.APPEARANCE, "value" : TIED_HOLE_COLOR });
        }
        else
        {
            addDebugEntities(context, shown, DebugColor.BLUE);
        }
        showTieMark(context, id + "tieMark", definition, stock, center, length);
    }
    // Being too long to buy matters more than not being a regular length
    if (stock.stock != [] && stockFor(stock, length) == undefined)
    {
        const longest = stock.stock[size(stock.stock) - 1].length;
        reportFeatureWarning(context, id, "The " ~ name ~ " exceeds the max length sold by the vendor (" ~ lengthString(definition, longest) ~ ").",
            [isEdgePlacement(definition) ? "edge" : "depth"]);
    }
    else if (built.tie != undefined && !isRegularLength(length, built.tie))
    {
        reportFeatureInfo(context, id, regularLengthMessage(definition, name, built.tie));
    }
}

/**
 * Where stock goes along the selected edge, between its offsets: from the edge's start to its end (as
 * `evEdgeTangentLine` runs), unless it's flipped. Returns the `location` its start is at (oriented by `orientStock`)
 * and its `length`.
 */
function edgePlacement(context is Context, id is Id, definition is map, name is string) returns map
{
    const edge = verifyNonemptyQuery(context, definition, "edge", "Select an edge to use.")[0];
    const startOffset = definition.hasStartOffset ? definition.edgeStartOffset : 0 * meter;
    const endOffset = definition.hasEndOffset ? definition.edgeEndOffset : 0 * meter;
    const middle = edgeCoordSystem(context, edge);
    const edgeLength = evLength(context, { "entities" : edge });
    // At the start of the edge, in the direction the stock is drawn
    var location = orientStock(definition, middle);
    location.origin -= location.zAxis * edgeLength / 2;
    const edgeStart = location.origin;
    location.origin += location.zAxis * startOffset;
    addOffsetManipulators(context, id, definition, edgeStart, location.zAxis, edgeLength, startOffset, endOffset);

    const length = edgeLength - startOffset - endOffset;
    if (tolerantLessThanOrEqual(length, 0 * meter))
    {
        throw regenError("Specified offsets are too long.", ["edgeStartOffset", "edgeEndOffset"], edge);
    }
    return { "location" : location, "length" : length, "errorParameters" : ["trimFaces"] };
}

/**
 * The cuts trimming stock to the selected faces (or mate connectors): each cuts it with its plane, keeping the side it
 * faces (out of the part it's a face of, or along a mate connector's Z), so trimming only ever shortens stock. Throws if
 * a plane misses the stock, or would leave none of it.
 *
 * @returns {array} : Maps of each cut's `plane`, and the direction `away` it removes stock in.
 */
function trimCuts(context is Context, definition is map, stock is Stock, center is CoordSystem, length is ValueWithUnits, name is string) returns array
{
    var corners = [];
    for (var z in [0 * meter, length])
    {
        for (var x in [-1, 1])
        {
            for (var y in [-1, 1])
            {
                corners = append(corners, toWorld(center, vector(x * stock.width / 2, y * stock.height / 2, z)));
            }
        }
    }
    var cuts = [];
    for (var face in evaluateQuery(context, definition.trimFaces))
    {
        const cut = boundPlane(context, face);
        var kept = false;
        var removed = false;
        for (var corner in corners)
        {
            const distance = dot(corner - cut.origin, cut.normal);
            kept = kept || tolerantGreaterThan(distance, 0 * meter);
            removed = removed || tolerantLessThan(distance, 0 * meter);
        }
        if (!kept || !removed)
        {
            throw regenError("The selected face does not intersect the " ~ name ~ ".", ["trimFaces"], face);
        }
        cuts = append(cuts, { "plane" : cut, "away" : -cut.normal });
    }
    return cuts;
}

/**
 * How long stock is along `center`'s Z axis, from its body.
 */
function stockExtent(context is Context, body is Query, center is CoordSystem) returns ValueWithUnits
{
    const bounds = evBox3d(context, { "topology" : body, "cSys" : center, "tight" : true });
    return bounds.maxCorner[2] - bounds.minCorner[2];
}

/**
 * The plane of a planar face, or a mate connector's XY plane.
 */
function boundPlane(context is Context, entity is Query) returns Plane
{
    if (!isQueryEmpty(context, entity->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        return plane(evMateConnector(context, { "mateConnector" : entity }));
    }
    return evPlane(context, { "face" : entity });
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
    return definition.flip;
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
    if (tolerantEqualsZero(extra) || tolerantEquals(extra, tie.unit))
    {
        return sentence("The " ~ name ~ "'s length should be " ~ multiple);
    }
    return sentence("The " ~ name ~ "'s length should be " ~ lengthString(definition, extra) ~ " more than " ~ multiple);
}

/**
 * Shows where holes start being tied to the end, when they're tied by length (see `getTie`): a rectangle across the
 * stock, a little bigger than its profile, there.
 */
function showTieMark(context is Context, id is Id, definition is map, stock is Stock, center is CoordSystem, length is ValueWithUnits)
{
    if (definition.tieHolesBy != TieHolesBy.LENGTH)
    {
        return;
    }
    const mark = length - definition.tieLength;
    if (tolerantLessThanOrEqual(mark, 0 * meter) || tolerantLessThanOrEqual(length, mark))
    {
        return;
    }
    const corner = vector(stock.width, stock.height) * 0.75;
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane(center.origin + center.zAxis * mark, center.zAxis, center.xAxis) });
    skRectangle(sketch, "mark", { "firstCorner" : -corner, "secondCorner" : corner });
    skSolve(sketch);
    addDebugEntities(context, qCreatedBy(id, EntityType.EDGE), DebugColor.BLUE);
    opDeleteBodies(context, id + "delete", { "entities" : qCreatedBy(id, EntityType.BODY) });
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
 * How far along `center`'s Z axis stock reaches: from where its start plane is farthest back across its profile (or 0,
 * for a square start) to where its end plane is farthest forward (or `length`, for a square end), as an extrude up to a
 * slanted face reaches it. Stock is built that long, then cut to the planes (see `buildStock`).
 */
function stockSpan(center is CoordSystem, stock is Stock, startPlane, endPlane, length is ValueWithUnits) returns map
{
    var corners = [];
    for (var x in [-1, 1])
    {
        for (var y in [-1, 1])
        {
            corners = append(corners, toWorld(center, vector(x * stock.width / 2, y * stock.height / 2, 0 * meter)));
        }
    }
    const along = function(cut)
        {
            return mapArray(corners, function(corner)
                {
                    return alongTo(corner, center.zAxis, cut);
                });
        };
    return {
            "start" : startPlane == undefined ? 0 * meter : min(along(startPlane)),
            "end" : endPlane == undefined ? length : max(along(endPlane))
        };
}

/**
 * How far from `point` along `direction` `cut` is.
 */
function alongTo(point is Vector, direction is Vector, cut is Plane) returns ValueWithUnits
{
    return dot(cut.origin - point, cut.normal) / dot(direction, cut.normal);
}

/**
 * Where stock extruded from the selected point goes, worked out from the extrude's options as std's extrude would
 * extrude it, without extruding. Its manipulators are std's extrude's (see `addExtrudeManipulator`).
 *
 * The stock runs along the extrude's direction (which Opposite direction flips), from its start (the profile, half a
 * symmetric length back, or the second end) to its end, unless it's flipped. Returns the `location` its start is at
 * (oriented by `orientStock`), its `length`, and the `startPlane` and `endPlane` its ends are on, if they're slanted
 * (up to faces at an angle, like miters).
 */
function extrudePlacement(context is Context, id is Id, definition is map, name is string) returns map
{
    const profilePlane = getLocationPlane(context, definition);
    const axisPlane = extrudeDirectionPlane(context, definition, profilePlane);
    const extrudeAxis = line(axisPlane.origin, axisPlane.normal);

    // The profile moved by the starting offset, along the extrude's axis, as std's extrude moves it
    var shift = 0 * meter;
    if (definition.startOffset)
    {
        shift = definition.startOffsetBound == StartOffsetType.BLIND ?
            (definition.startOffsetOppositeDirection ? -1 : 1) * definition.startOffsetDistance :
            alongTo(extrudeAxis.origin, extrudeAxis.direction, plane(evDistance(context, {
                                    "side0" : extrudeAxis.origin,
                                    "side1" : definition.startOffsetEntity
                                }).sides[1].point, profilePlane.normal));
    }
    const origin = extrudeAxis.origin + extrudeAxis.direction * shift;

    var manipulatorDefinition = transformDefintionForNewExtrude(definition, qNothing());
    manipulatorDefinition.distanceForManipulator = shift;
    addExtrudeManipulator(context, id, manipulatorDefinition, qNothing(), extrudeAxis, false);

    const direction = definition.oppositeDirection ? -extrudeAxis.direction : extrudeAxis.direction;
    var start = { "along" : 0 * meter };
    var end;
    if (definition.endBound == StockBoundingType.BLIND && definition.symmetric)
    {
        start = { "along" : -definition.depth / 2 };
        end = { "along" : definition.depth / 2 };
    }
    else
    {
        end = boundAlong(context, {
                        "boundType" : definition.endBound,
                        "depth" : definition.depth,
                        "face" : definition.endBoundEntityFace,
                        "vertex" : definition.endBoundEntityVertex,
                        "hasOffset" : definition.hasOffset,
                        "offsetDistance" : definition.offsetDistance,
                        "offsetOppositeDirection" : definition.offsetOppositeDirection
                    }, origin, direction, name, ["endBound", "endBoundEntityFace", "endBoundEntityVertex"]);
        if (definition.hasSecondDirection)
        {
            // Back from the profile, unless its direction is flipped to match the first's (as std's extrude decides it)
            const sign = definition.secondDirectionOppositeDirection != definition.oppositeDirection ? -1 : 1;
            const second = boundAlong(context, {
                            "boundType" : definition.secondDirectionBound,
                            "depth" : definition.secondDirectionDepth,
                            "face" : definition.secondDirectionBoundEntityFace,
                            "vertex" : definition.secondDirectionBoundEntityVertex,
                            "hasOffset" : definition.hasSecondDirectionOffset,
                            "offsetDistance" : definition.secondDirectionOffsetDistance,
                            "offsetOppositeDirection" : definition.secondDirectionOffsetOppositeDirection
                        }, origin, sign * direction, name,
                ["secondDirectionBound", "secondDirectionBoundEntityFace", "secondDirectionBoundEntityVertex"]);
            start = mergeMaps(second, { "along" : sign * second.along });
        }
    }

    // Drawn from its start, or from its end if it's flipped (see `orientStock`)
    var location = orientStock(definition, coordSystem(origin, axisPlane.x, direction));
    const flipped = isFlipped(definition);
    location.origin = origin + direction * (flipped ? end.along : start.along);
    return {
            "location" : location,
            "length" : end.along - start.along,
            "startPlane" : slantedEnd((flipped ? end : start).plane, location.zAxis),
            "endPlane" : slantedEnd((flipped ? start : end).plane, location.zAxis),
            "errorParameters" : ["depth", "endBound", "secondDirectionBound"]
        };
}

/**
 * Where one bound of an extrude from `origin` along `direction` is: how far `along` it, and the `plane` it ends on if
 * it's up to a face. Its offset pulls it back toward `origin`, unless it's flipped. Up to next is up to the nearest
 * face in front of `origin` (as an extrude of a point would be), which must be flat.
 *
 * @param bound {{
 *      @field boundType {StockBoundingType}
 *      @field depth {ValueWithUnits} : For `BLIND`.
 *      @field face {Query} : For `UP_TO_SURFACE`: a planar face or mate connector.
 *      @field vertex {Query} : For `UP_TO_VERTEX`: a vertex or mate connector.
 *      @field hasOffset {boolean}
 *      @field offsetDistance {ValueWithUnits}
 *      @field offsetOppositeDirection {boolean}
 * }}
 * @param parameters : The bound's parameters, which errors highlight.
 */
function boundAlong(context is Context, bound is map, origin is Vector, direction is Vector, name is string, parameters is array) returns map
{
    if (bound.boundType == StockBoundingType.BLIND)
    {
        return { "along" : bound.depth };
    }
    const offset = bound.hasOffset ? (bound.offsetOppositeDirection ? 1 : -1) * bound.offsetDistance : 0 * meter;
    if (bound.boundType == StockBoundingType.UP_TO_VERTEX)
    {
        if (isQueryEmpty(context, bound.vertex))
        {
            throw regenError(ErrorStringEnum.EXTRUDE_SELECT_TERMINATING_VERTEX, parameters);
        }
        // Square to the extrude, as std's extrude ends up to a vertex
        return { "along" : dot(evVertexPoint(context, { "vertex" : bound.vertex }) - origin, direction) + offset };
    }
    var face;
    if (bound.boundType == StockBoundingType.UP_TO_SURFACE)
    {
        if (isQueryEmpty(context, bound.face))
        {
            throw regenError(ErrorStringEnum.EXTRUDE_SELECT_TERMINATING_SURFACE, parameters);
        }
        face = bound.face;
    }
    else
    {
        // Up to next
        var nearest;
        for (var hit in evRaycast(context, { "entities" : qAllModifiableSolidBodiesNoMesh(), "ray" : line(origin, direction), "closest" : false }))
        {
            if (!tolerantEqualsZero(hit.distance) && (nearest == undefined || hit.distance < nearest.distance))
            {
                nearest = hit;
            }
        }
        if (nearest == undefined)
        {
            throw regenError(ErrorStringEnum.EXTRUDE_FAILED, parameters);
        }
        face = nearest.entity;
        if (isQueryEmpty(context, face->qGeometry(GeometryType.PLANE)))
        {
            throw regenError(flatEndsMessage(name), parameters, face);
        }
    }
    var cut = boundPlane(context, face);
    if (tolerantEqualsZero(dot(cut.normal, direction)))
    {
        // Parallel to the extrude, so it never reaches it
        throw regenError(ErrorStringEnum.EXTRUDE_FAILED, parameters, face);
    }
    cut.origin += direction * offset;
    return { "along" : alongTo(origin, direction, cut), "plane" : cut };
}

/**
 * `cut`, if it's slanted (not square to `direction`, like a miter), which stock's end is cut to; otherwise
 * `undefined`, and the end is square.
 */
function slantedEnd(cut, direction is Vector)
{
    return cut == undefined || parallelVectors(cut.normal, direction) ? undefined : cut;
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
    const flip = newManipulators[FLIP_MANIPULATOR];
    if (flip != undefined)
    {
        definition.flip = flip.flipped;
    }
    if (isEdgePlacement(definition))
    {
        const startOffset = newManipulators[START_OFFSET_MANIPULATOR];
        if (startOffset != undefined)
        {
            definition.edgeStartOffset = startOffset.offset;
        }
        const endOffset = newManipulators[END_OFFSET_MANIPULATOR];
        if (endOffset != undefined)
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
    // Along the extrude's direction from the selected point. A guard, not a fallback: editing logic mustn't throw while
    // the dialog is being filled in (no point, or no direction, selected yet), so then there's no axis, and no flips
    var extrudeAxis;
    try silent
    {
        const axisPlane = extrudeDirectionPlane(context, definition, getLocationPlane(context, definition));
        extrudeAxis = line(axisPlane.origin, axisPlane.normal);
    }
    return newExtrudeEditLogicAlong(context, definition, specifiedParameters, extrudeAxis);
}
