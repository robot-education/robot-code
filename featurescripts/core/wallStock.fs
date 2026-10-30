FeatureScript 2960;
/**
 * Wall stock: linear stock (see `core/linearStock.fs`) built from its walls, each sketched with its holes and extruded,
 * which needs no boolean per hole (see `buildWallStock`): tubes, channels, and angles, like frames.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "72b77780ed382be329401627", version : "a0ad561c9e3a3bdae870026b");
import(path : "2e446fc39b56fd5bff8ff054", version : "67a499eda38c0e000182de1b");
import(path : "9fc889bb93a3c29feb4f9ae5", version : "45d4504d059b748ebf938c51");

/**
 * Builds stock `length` long from `location` (the middle of its start, with Z along its length and its width along
 * X) from its walls (see `buildFromWalls`) if it can be: a tube, channel, or angle with holes, each of which fits in
 * one of its walls (see `stockWalls` and `holesFitWalls`). Otherwise, it's cut (see `buildCutStock`). A `placeStock`
 * builder.
 */
export const buildWallStock = function(context is Context, id is Id, definition is map, stock is Stock, location is CoordSystem,
    length is ValueWithUnits, ends is map) returns map
{
    const across = acrossSign(definition);
    const walls = stock.xRows != [] || stock.yRows != [] ? stockWalls(stock, location, across) : [];
    if (walls == [] || !holesFitWalls(stock, walls))
    {
        return buildCutStock(context, id, definition, stock, location, length, ends);
    }
    return buildFromWalls(context, id, stock, location, length, ends, getTie(definition, stock.tieStart, stock.tieUnit), walls, across);
};

/**
 * How far a wall's holes must stay from its sides, along which it meets the next wall or ends (see `holesFitWalls`).
 */
const WALL_EDGE_MARGIN = 1e-5 * meter;

/**
 * Marks the faces of holes tied to the end (see `getTie`), tagged as the walls are made, which their copies and the join
 * carry (see `buildFromWalls`).
 */
const TIED_HOLE_ATTRIBUTE = "robotStockTiedHoleAttribute";

/** How far past each end a profile's `grooves` are extruded, so they cut cleanly through its ends. */
const GROOVE_OVERRUN = 1 * millimeter;

/** How far a sketched profile's walls reach past its holes (see `stockWalls`). */
const PROFILE_WALL_MARGIN = 0.25 * millimeter;

/**
 * The walls of stock (see `buildFromWalls`): a tube's four, a channel's three (it's open facing +Y), or an angle's
 * two (its legs face -X and -Y, mirrored with it; see `acrossSign`), or none for other stock (solid, around a bore, or
 * tapped). The walls facing Y run its whole width, and those facing X between them. A sketched `profile`'s walls are
 * those with holes, reaching across just past its holes (`PROFILE_WALL_MARGIN`), as thick as its walls are there
 * (`wallX` and `wallY`), and the rest of it is its core (see `buildCore`).
 *
 * @returns {array} : Maps of each wall's `name`, its `face` (see `stockFaces`: its rows, and which way its holes'
 *          offsets go), the `plane` it's sketched on (its outside, or its inside for the walls facing +X and +Y, with X
 *          along the stock and the face's Y), how `deep` it is (its thickness, from the plane toward +X or +Y), and
 *          how far across it reaches each way (`low` and `high`, in its plane). The wall opposite another, with the
 *          same holes, is a copy of it: it has the name of the wall it `copies`, and how far `along` its plane's
 *          normal it is from it.
 */
function stockWalls(stock is Stock, location is CoordSystem, across is number) returns array
{
    if ((stock.solid ?? false) || stock.bore != undefined || stock.thread != undefined || (stock.centerHole ?? false) ||
        tolerantEqualsZero(stock.wallX) || tolerantEqualsZero(stock.wallY))
    {
        return [];
    }
    const faces = stockFaces(stock, location, across);
    const halfWidth = stock.width / 2;
    const halfHeight = stock.height / 2;
    const isAngle = stock.profile == undefined && (stock.angle ?? false);
    const isOpen = stock.profile == undefined && (stock.open ?? false);
    // Where the walls facing X go across the stock (from their -X sides), and how far they reach along its Y
    const xWalls = isAngle ? [across == 1 ? -halfWidth : halfWidth - stock.wallX] : [-halfWidth, halfWidth - stock.wallX];
    const top = isAngle || isOpen ? halfHeight : halfHeight - stock.wallY;
    const yWalls = isAngle || isOpen ? [-halfHeight] : [-halfHeight, halfHeight - stock.wallY];
    const isProfile = stock.profile != undefined;
    var walls = [];
    for (var i, x in (isProfile && faces[0].rows == [] ? [] : xWalls))
    {
        // The face's plane is at -X, and its Y is the stock's -Y
        const span = isProfile ? holeSpan(stock, faces[0]) : [-top, halfHeight - stock.wallY];
        walls = append(walls, {
                    "name" : "x" ~ i,
                    "face" : faces[0],
                    "plane" : movedPlane(faces[0].plane, x + halfWidth),
                    "depth" : stock.wallX,
                    "low" : span[0],
                    "high" : span[1],
                    "copies" : i == 0 ? undefined : "x0",
                    "along" : x - xWalls[0]
                });
    }
    for (var i, y in (isProfile && faces[1].rows == [] ? [] : yWalls))
    {
        // The face's plane is at -Y, and its Y is the stock's X
        const span = isProfile ? holeSpan(stock, faces[1]) : [-halfWidth, halfWidth];
        walls = append(walls, {
                    "name" : "y" ~ i,
                    "face" : faces[1],
                    "plane" : movedPlane(faces[1].plane, y + halfHeight),
                    "depth" : stock.wallY,
                    "low" : span[0],
                    "high" : span[1],
                    "copies" : i == 0 ? undefined : "y0",
                    "along" : y - yWalls[0]
                });
    }
    return walls;
}

/**
 * How far a sketched profile's wall reaches across it (in its `face`'s plane): just past its holes.
 */
function holeSpan(stock is Stock, face is map) returns array
{
    var low = undefined;
    var high = undefined;
    for (var row in face.rows)
    {
        for (var shape in row.shapes)
        {
            const center = face.sign * shape.offset;
            const radius = shapeRadius(shape, stock.holeDiameter);
            low = low == undefined ? center - radius : min(low, center - radius);
            high = high == undefined ? center + radius : max(high, center + radius);
        }
    }
    return [low - PROFILE_WALL_MARGIN, high + PROFILE_WALL_MARGIN];
}

/**
 * A plane moved `distance` along its normal.
 */
function movedPlane(original is Plane, distance is ValueWithUnits) returns Plane
{
    return plane(original.origin + original.normal * distance, original.normal, original.x);
}

/**
 * Whether each of stock's holes fits in the walls it goes through (see `stockWalls`), clear of their sides, so it can be
 * built from them.
 */
function holesFitWalls(stock is Stock, walls is array) returns boolean
{
    for (var wall in walls)
    {
        for (var row in wall.face.rows)
        {
            for (var shape in row.shapes)
            {
                const center = wall.face.sign * shape.offset;
                const radius = shapeRadius(shape, stock.holeDiameter);
                if (!tolerantLessThan(wall.low + WALL_EDGE_MARGIN, center - radius) ||
                    !tolerantLessThan(center + radius, wall.high - WALL_EDGE_MARGIN))
                {
                    return false;
                }
            }
        }
    }
    return true;
}

/**
 * Builds stock from its walls (see `stockWalls`), as `buildCutStock` builds it from its profile, but faster: each wall is
 * sketched with its holes and extruded as thick as it is, so its holes need no booleans, then they're joined, with a
 * sketched profile's core (see `extrudeCore` and `cutCoreBlocks`), its `grooves` are cut along it, and its ends are
 * trimmed. A frame's swept faces and edges, and its caps, are tagged by history as its walls, core, and grooves are
 * made, before they're copied and joined, which carry their attributes.
 *
 * Each wall is `length` long, with the holes `buildCutStock` would cut. A hole is named by its count from the start, or
 * from the end if it's tied to it (see `getTie`), so its faces keep their identities as the stock's length changes.
 * Holes reaching an end (crossing it, or tangent to it) are left out, so the wall is whole there: a hole touching its
 * sketch's outside isn't one of its inner loops, so it would be extruded anyway (with what of it is past the end).
 */
function buildFromWalls(context is Context, id is Id, stock is Stock, location is CoordSystem, length is ValueWithUnits,
    ends is map, tie is map, walls is array, across is number) returns map
{
    const wallsId = id + "walls";
    const isFrame = stock.isFrame ?? false;
    // Its faces and edges as a frame (see `core/frameTags.fs`), found by history as each part is made and tagged
    // before they're copied or joined, which carry their attributes: not by where they are
    var swept = [];
    var starts = [];
    // What the walls' (and core's) ends become, tracked from the lines (and caps) they're made from
    var endTracks = [];
    // What the walls' holes tied to the end (see `getTie`) become, tracked from their sketch entities
    var tiedTracks = [];
    const groovesId = id + "grooves";
    if (stock.grooves != undefined)
    {
        // Cut once it's joined, as its walls' sketches can't run along it: a little longer, so they cut through its ends.
        // Made before the walls, as each id's operations must be contiguous
        const sketch = newSketchOnPlane(context, groovesId + "sketch", { "sketchPlane" : plane(location) });
        sketchGrooves(sketch, stock, across);
        skSolve(sketch);
        opExtrude(context, groovesId + "extrude", {
                    "entities" : qSketchRegion(groovesId + "sketch"),
                    "direction" : location.zAxis,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : GROOVE_OVERRUN,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length + GROOVE_OVERRUN
                });
        swept = append(swept, qNonCapEntity(groovesId + "extrude", EntityType.FACE));
        swept = append(swept, qNonCapEntity(groovesId + "extrude", EntityType.EDGE));
    }
    for (var wall in walls)
    {
        if (wall.copies != undefined)
        {
            continue;
        }
        const wallId = wallsId + wall.name;
        const sketchId = wallId + "sketch";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : wall.plane });
        sketchRectangle(sketch, "wall", vector(0 * meter, wall.low), vector(length, wall.high));
        var tied = [];
        for (var r, row in wall.face.rows)
        {
            const extent = rowExtent(row, stock.holeDiameter);
            const positions = holePositions(row, extent, length, tie);
            for (var k, hole in positions)
            {
                if (!tolerantLessThan(extent, hole.position) || !tolerantLessThan(hole.position + extent, length))
                {
                    continue;
                }
                const name = "r" ~ r ~ (hole.tied ? ".t" ~ (size(positions) - 1 - k) : ".h" ~ k);
                for (var s, shape in row.shapes)
                {
                    const entities = sketchShape(sketch, name ~ ".s" ~ s, vector(hole.position + shape.along, wall.face.sign * shape.offset),
                        shape, stock.holeDiameter);
                    if (hole.tied)
                    {
                        tied = concatenateArrays([tied, entities]);
                    }
                }
            }
        }
        skSolve(sketch);
        if (tied != [])
        {
            tiedTracks = append(tiedTracks, trackSketchEntities(context, sketchId, tied));
        }
        const sides = trackSketchEntities(context, sketchId, ["wall.low", "wall.high"]);
        starts = append(starts, trackSketchEntities(context, sketchId, ["wall.start"]));
        endTracks = append(endTracks, trackSketchEntities(context, sketchId, ["wall.end"]));
        // The wall itself, without the regions inside its holes
        const extrudeId = wallId + "extrude";
        opExtrude(context, extrudeId, {
                    "entities" : qSketchRegion(sketchId, true),
                    "direction" : wall.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : wall.depth
                });
        // Its faces on the wall's two sides, and those its own sides (and their edges) became
        swept = concatenateArrays([swept, [qCapEntity(extrudeId, CapType.EITHER, EntityType.FACE), sides]]);
    }
    var coreEnds = [];
    if (stock.profile != undefined)
    {
        const coreId = wallsId + "core";
        extrudeCore(context, coreId, stock, location, length, across);
        swept = append(swept, qNonCapEntity(coreId + "extrude", EntityType.FACE));
        swept = append(swept, qNonCapEntity(coreId + "extrude", EntityType.EDGE));
        starts = append(starts, qCapEntity(coreId + "extrude", CapType.START, EntityType.FACE));
        coreEnds = append(coreEnds, qCapEntity(coreId + "extrude", CapType.END, EntityType.FACE));
    }
    if (tiedTracks != [])
    {
        setAttribute(context, {
                    "entities" : qEntityFilter(qUnion(tiedTracks), EntityType.FACE),
                    "name" : TIED_HOLE_ATTRIBUTE,
                    "attribute" : {}
                });
    }
    if (isFrame)
    {
        const sweptEntities = qUnion(swept);
        tagFrameSwept(context, qEntityFilter(sweptEntities, EntityType.FACE), qEntityFilter(sweptEntities, EntityType.EDGE));
        tagFrameCaps(context, qEntityFilter(qUnion(starts), EntityType.FACE), true);
        tagFrameCaps(context, qEntityFilter(qUnion(concatenateArrays([endTracks, coreEnds])), EntityType.FACE), false);
    }

    if (stock.profile != undefined)
    {
        // Straight after its extrude, as each id's operations must be contiguous
        const coreId = wallsId + "core";
        cutCoreBlocks(context, coreId, length, walls);
        endTracks = append(endTracks, startTracking(context, {
                        "subquery" : qCapEntity(coreId + "extrude", CapType.END, EntityType.FACE),
                        "trackPartialDependency" : true
                    }));
    }
    for (var wall in walls)
    {
        if (wall.copies != undefined)
        {
            // Opposite the wall it copies, with the same holes
            opPattern(context, wallsId + wall.name + "pattern", {
                        "entities" : qCreatedBy(wallsId + wall.copies + "extrude", EntityType.BODY),
                        "transforms" : [transform(wall.plane.normal * wall.along)],
                        "instanceNames" : [wall.name]
                    });
        }
    }
    opDeleteBodies(context, id + "deleteSketches", {
                "entities" : qUnion(qCreatedBy(wallsId, EntityType.BODY), qCreatedBy(groovesId, EntityType.BODY))->qSketchFilter(SketchObject.YES)
            });
    const body = qCreatedBy(wallsId, EntityType.BODY)->qBodyType(BodyType.SOLID);
    opBoolean(context, id + "join", { "tools" : body, "operationType" : BooleanOperationType.UNION });
    if (stock.grooves != undefined)
    {
        opBoolean(context, id + "cutGrooves", {
                    "tools" : qCreatedBy(groovesId + "extrude", EntityType.BODY),
                    "targets" : body,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    if (isFrame)
    {
        tagFrameProfile(context, body, stock.partName, stock.vendor);
    }
    const endFaces = qEntityFilter(qUnion(endTracks), EntityType.FACE)->qOwnedByBody(body);

    // Cut to its trims and slanted ends
    var cuts = ends.cuts;
    if (ends.endPlane != undefined)
    {
        cuts = append(cuts, { "plane" : ends.endPlane, "away" : location.zAxis });
    }
    if (ends.startPlane != undefined)
    {
        cuts = append(cuts, { "plane" : ends.startPlane, "away" : -location.zAxis });
    }
    var ended = [endFaces];
    const reach = length + stock.width + stock.height;
    for (var i, cut in cuts)
    {
        const isEnd = dot(cut.away, location.zAxis) > 0;
        const face = trimBeyond(context, id + ("cut" ~ i), body, cut.plane, cut.away, reach);
        if (isFrame)
        {
            tagFrameCaps(context, face, !isEnd);
        }
        if (isEnd)
        {
            ended = append(ended, face);
        }
    }

    return {
            "body" : body,
            "endFace" : qUnion(ended)->qOwnedByBody(body),
            "tiedHoles" : qHasAttribute(qOwnedByBody(body, EntityType.FACE), TIED_HOLE_ATTRIBUTE),
            "tie" : tie
        };
}

/**
 * Extrudes a sketched profile's core (see `stockWalls`), `length` long, under `id`: its profile, which `cutCoreBlocks`
 * then cuts where its walls go.
 */
function extrudeCore(context is Context, id is Id, stock is Stock, location is CoordSystem, length is ValueWithUnits, across is number)
{
    const profile = newSketchOnPlane(context, id + "profile", { "sketchPlane" : plane(location) });
    sketchProfile(profile, stock, across);
    skSolve(profile);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(id + "profile", true),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : length
            });
}

/**
 * Cuts a block `length` long from the core `extrudeCore` made under `id` where each of its `walls` goes (a copy of the opposite
 * wall's block, for a wall which copies it).
 */
function cutCoreBlocks(context is Context, id is Id, length is ValueWithUnits, walls is array)
{
    for (var wall in walls)
    {
        const blockId = id + "block" + wall.name;
        if (wall.copies != undefined)
        {
            opPattern(context, blockId + "pattern", {
                        "entities" : qCreatedBy(id + "block" + wall.copies + "extrude", EntityType.BODY),
                        "transforms" : [transform(wall.plane.normal * wall.along)],
                        "instanceNames" : [wall.name]
                    });
            continue;
        }
        const sketch = newSketchOnPlane(context, blockId + "sketch", { "sketchPlane" : wall.plane });
        sketchRectangle(sketch, "block", vector(0 * meter, wall.low), vector(length, wall.high));
        skSolve(sketch);
        opExtrude(context, blockId + "extrude", {
                    "entities" : qSketchRegion(blockId + "sketch"),
                    "direction" : wall.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : wall.depth
                });
    }
    opBoolean(context, id + "subtract", {
                "tools" : qCreatedBy(id + "block", EntityType.BODY)->qBodyType(BodyType.SOLID),
                "targets" : qCreatedBy(id + "extrude", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION
            });
}

/**
 * A query for what operations from here on make from a solved sketch's entities (by their ids), as `startTracking`
 * makes it, but also what they make from them and other entities (like a face one's merged into).
 */
function trackSketchEntities(context is Context, sketchId is Id, sketchEntityIds is array) returns Query
{
    var entities = [];
    for (var sketchEntityId in sketchEntityIds)
    {
        const entity = sketchEntityQuery(sketchId, undefined, sketchEntityId);
        entities = concatenateArrays([entities, [entity, makeQuery(sketchId, "IMPRINT", undefined, { "derivedFrom" : entity })]]);
    }
    return startTracking(context, { "subquery" : qUnion(entities), "trackPartialDependency" : true });
}
