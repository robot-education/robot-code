FeatureScript 2960;
/**
 * Wall stock: linear stock (see `core/linearStock.fs`) built from its walls, each sketched with its holes and extruded,
 * which needs no boolean per hole (see `buildWallStock`): tubes, channels, and angles, like frames.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "72b77780ed382be329401627", version : "505d7b698065300de55ae4da");
import(path : "core/cutStock.fs", version : "");
import(path : "core/linearStock.fs", version : "");

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
 * sketched profile's core (see `buildCore`), its `grooves` are cut along it, and its ends are trimmed.
 *
 * Each wall is `length` long, with the holes `buildCutStock` would cut. A hole is named by its count from the start, or
 * from the end if it's tied to it (see `getTie`), so its faces keep their identities as the stock's length changes.
 * Holes crossing an end leave a notch, as they're cut through.
 */
function buildFromWalls(context is Context, id is Id, stock is Stock, location is CoordSystem, length is ValueWithUnits,
    ends is map, tie is map, walls is array, across is number) returns map
{
    const wallsId = id + "walls";
    var holeSpans = [];
    var tiedFrom = length;
    // What the walls' (and core's) ends become, tracked from the lines (and caps) they're made from
    var startTracks = [];
    var endTracks = [];
    for (var wall in walls)
    {
        const wallId = wallsId + wall.name;
        if (wall.copies != undefined)
        {
            // Opposite the wall it copies, with the same holes
            opPattern(context, wallId + "pattern", {
                        "entities" : qCreatedBy(wallsId + wall.copies + "extrude", EntityType.BODY),
                        "transforms" : [transform(wall.plane.normal * wall.along)],
                        "instanceNames" : [wall.name]
                    });
            continue;
        }
        const sketch = newSketchOnPlane(context, wallId + "sketch", { "sketchPlane" : wall.plane });
        sketchRectangle(sketch, "wall", vector(0 * meter, wall.low), vector(length, wall.high));
        for (var r, row in wall.face.rows)
        {
            const extent = rowExtent(row, stock.holeDiameter);
            const positions = holePositions(row, extent, length, tie);
            for (var k, hole in positions)
            {
                const name = "r" ~ r ~ (hole.tied ? ".t" ~ (size(positions) - 1 - k) : ".h" ~ k);
                for (var s, shape in row.shapes)
                {
                    sketchShape(sketch, name ~ ".s" ~ s, vector(hole.position + shape.along, wall.face.sign * shape.offset), shape,
                        stock.holeDiameter);
                }
                holeSpans = append(holeSpans, [hole.position - extent, hole.position + extent]);
                if (hole.tied)
                {
                    tiedFrom = min(tiedFrom, hole.position - extent);
                }
            }
        }
        skSolve(sketch);
        startTracks = append(startTracks, trackSketchEntity(context, wallId + "sketch", "wall.start"));
        endTracks = append(endTracks, trackSketchEntity(context, wallId + "sketch", "wall.end"));
        // The wall itself, without its holes, or where holes cross its ends: the region along its side, which its holes
        // stay clear of (see `holesFitWalls`)
        opExtrude(context, wallId + "extrude", {
                    "entities" : qContainsPoint(qSketchRegion(wallId + "sketch"),
                        planeToWorld(wall.plane, vector(length / 2, wall.low + WALL_EDGE_MARGIN / 2))),
                    "direction" : wall.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : wall.depth
                });
    }
    if (stock.profile != undefined)
    {
        const coreId = wallsId + "core";
        buildCore(context, coreId, stock, location, length, across, walls);
        startTracks = append(startTracks, startTracking(context, {
                        "subquery" : qCapEntity(coreId + "extrude", CapType.START, EntityType.FACE),
                        "trackPartialDependency" : true
                    }));
        endTracks = append(endTracks, startTracking(context, {
                        "subquery" : qCapEntity(coreId + "extrude", CapType.END, EntityType.FACE),
                        "trackPartialDependency" : true
                    }));
    }
    const groovesId = id + "grooves";
    if (stock.grooves != undefined)
    {
        // Cut once it's joined, as its walls' sketches can't run along it: a little longer, so they cut through its ends
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
    }
    opDeleteBodies(context, id + "deleteSketches", {
                "entities" : qUnion(qCreatedBy(wallsId, EntityType.BODY), qCreatedBy(groovesId, EntityType.BODY))->qSketchFilter(SketchObject.YES)
            });
    const body = qCreatedBy(wallsId, EntityType.BODY)->qBodyType(BodyType.SOLID);
    opBoolean(context, id + "join", { "tools" : body, "operationType" : BooleanOperationType.UNION });
    if (stock.grooves != undefined)
    {
        opBoolean(context, groovesId + "cut", {
                    "tools" : qCreatedBy(groovesId + "extrude", EntityType.BODY),
                    "targets" : body,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }

    const startFaces = qEntityFilter(qUnion(startTracks), EntityType.FACE)->qOwnedByBody(body);
    const endFaces = qEntityFilter(qUnion(endTracks), EntityType.FACE)->qOwnedByBody(body);
    if (stock.isFrame ?? false)
    {
        // Its profile's faces and edges are those which cross it where it has no holes
        const section = plane(location.origin + location.zAxis * clearPosition(holeSpans, length), location.zAxis);
        tagSweptAsFrame(context, body, qIntersectsPlane(qOwnedByBody(body, EntityType.FACE), section),
            qIntersectsPlane(qOwnedByBody(body, EntityType.EDGE), section), stock.partName, stock.vendor);
        tagFrameCaps(context, startFaces, true);
        tagFrameCaps(context, endFaces, false);
    }

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
        if (stock.isFrame ?? false)
        {
            tagFrameCaps(context, face, !isEnd);
        }
        if (isEnd)
        {
            ended = append(ended, face);
        }
    }

    // The faces of the holes past where the first tied hole starts: the walls' faces wholly between there and the end,
    // which leaves out the faces along the whole wall (and the end)
    var tiedHoles = qNothing();
    if (tiedFrom < length)
    {
        const from = max(tiedFrom - WALL_EDGE_MARGIN, WALL_EDGE_MARGIN);
        tiedHoles = qCreatedBy(wallsId, EntityType.FACE)
            ->qOwnedByBody(body)
            ->qInFrontOfPlane(plane(location.origin + location.zAxis * from, location.zAxis))
            ->qInFrontOfPlane(plane(location.origin + location.zAxis * (length - WALL_EDGE_MARGIN), -location.zAxis));
    }
    return {
            "body" : body,
            "endFace" : qUnion(ended)->qOwnedByBody(body),
            "tiedHoles" : tiedHoles,
            "tie" : tie
        };
}

/**
 * Builds a sketched profile's core (see `stockWalls`), `length` long: its profile extruded, less a block where each of
 * its `walls` goes (a copy of the opposite wall's block, for a wall which copies it), all under `id`.
 */
function buildCore(context is Context, id is Id, stock is Stock, location is CoordSystem, length is ValueWithUnits, across is number,
    walls is array)
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
 * A query for what operations from here on make from a solved sketch's entity, as `startTracking` makes it, but also
 * what they make from it and other entities (like a face it's merged into).
 */
function trackSketchEntity(context is Context, sketchId is Id, sketchEntityId is string) returns Query
{
    const entity = sketchEntityQuery(sketchId, undefined, sketchEntityId);
    return startTracking(context, {
                "subquery" : qUnion([entity, makeQuery(sketchId, "IMPRINT", undefined, { "derivedFrom" : entity })]),
                "trackPartialDependency" : true
            });
}

/**
 * Where along stock `length` long it can be cut across without crossing a hole, given where its holes reach
 * (`spans`, pairs of positions): the middle of the widest gap between them, or its middle if they leave none.
 */
function clearPosition(spans is array, length is ValueWithUnits) returns ValueWithUnits
{
    const sorted = sort(spans, function(a, b)
        {
            return a[0] - b[0];
        });
    var best = undefined;
    var reached = 0 * meter;
    for (var span in append(sorted, [length, length]))
    {
        if (best == undefined || span[0] - reached > best[1] - best[0])
        {
            best = [reached, span[0]];
        }
        reached = max(reached, span[1]);
    }
    return tolerantLessThan(best[0], best[1]) ? (best[0] + best[1]) / 2 : length / 2;
}
