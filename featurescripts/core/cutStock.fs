FeatureScript 2960;
/**
 * Cut stock: linear stock (see `core/linearStock.fs`), like nut strips and frames, built by extruding its profile and
 * cutting its holes (see `buildCutStock`).
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "72b77780ed382be329401627", version : "505d7b698065300de55ae4da");
import(path : "f58a965fe7005e2e3c9a67c7", version : "5794d15cbce3be3e963e93d5");
import(path : "9fc889bb93a3c29feb4f9ae5", version : "3eb159cc8a1f8aa01f9bed32");

/**
 * Builds stock `length` long from `location` (the middle of its start, with Z along its length and its width along
 * X): its profile is extruded along its length, then its holes are cut, then its ends are trimmed. A `placeStock`
 * builder.
 *
 * Stock with holes is built as the regular length at least `length` long (see `regularLength`), so its holes are all
 * whole while they're cut, and is then cut to `length`, cutting through any holes the end crosses, as cutting it from
 * longer stock would. Ends can be slanted (like miters): `ends` has the `startPlane` and `endPlane` they're on, which
 * are inside its length, and other `cuts` to make (see `trimCuts`); it's cut to each (see `trimBeyond`), and the face
 * each cut leaves is a frame's cap. Then it's trimmed to `ends.parts` (see `partTrims`): each part is subtracted from
 * it, and of the pieces left, the one with its other end is kept, as std's Frame trims to parts.
 *
 * Booleans are slow, so each row's holes are cut by one seed (a tool for its first hole, or its last when they're all
 * tied to the end), and the seed's faces are face patterned along the stock. Each pattern instance is named by its
 * hole's count from the start, or for holes tied to the end (see `getTie`), from the end (see `holeInstances`), so
 * tied holes keep their identities as the length changes. Seeds patterned the same way (with the same steps and
 * names) are cut and patterned together (see `seedGroups`), each group under its own id, so the faces it made are
 * `qCreatedBy(groupId, EntityType.FACE)`.
 *
 * @returns {{
 *      @field body {Query}
 *      @field endFace {Query} : The face at its end.
 *      @field tiedHoles {Query} : The faces of the holes tied to the end.
 *      @field tie {map} : Its holes' tie (see `getTie`), or `undefined` if it has none.
 * }}
 */
export const buildCutStock = function(context is Context, id is Id, definition is map, stock is Stock, location is CoordSystem,
    length is ValueWithUnits, ends is map) returns map
{
    const tie = getTie(definition, stock.tieStart, stock.tieUnit);
    const hasHoles = stock.xRows != [] || stock.yRows != [];
    const across = acrossSign(definition);
    const built = hasHoles ? regularLength(length, tie) : length;
    const profileId = id + "profile";
    const sketch = newSketchOnPlane(context, profileId, { "sketchPlane" : plane(location) });
    sketchProfile(sketch, stock, across);
    skSolve(sketch);
    const stockId = id + "stock";
    opExtrude(context, stockId, {
                "entities" : qSketchRegion(profileId, true),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : built
            });
    opDeleteBodies(context, id + "deleteProfile", { "entities" : qCreatedBy(profileId, EntityType.BODY) });
    var body = qCreatedBy(stockId, EntityType.BODY);
    if (stock.isFrame ?? false)
    {
        // Before the holes are cut, so its faces are still the extrude's
        tagExtrudeAsFrame(context, stockId);
    }

    const groups = seedGroups(stock, location, length, tie, across);
    for (var i, group in groups)
    {
        cutHoles(context, id + ("holes" ~ i), group, body, stock.holeDiameter);
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
    // The faces at each end: its caps, and the faces the cuts and trims there leave
    var startFaces = [qCapEntity(stockId, CapType.START, EntityType.FACE)];
    var endFaces = [qCapEntity(stockId, CapType.END, EntityType.FACE)];
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
            endFaces = append(endFaces, face);
        }
        else
        {
            startFaces = append(startFaces, face);
        }
    }

    // Trimmed to parts: each is subtracted, and the piece with the other end is kept (the rest, cut off past the part,
    // are deleted), as std's Frame trims to parts
    for (var i, trim in ends.parts)
    {
        const trimId = id + ("trimPart" ~ i);
        try
        {
            opBoolean(context, trimId + "cut", {
                        "tools" : trim.part,
                        "targets" : body,
                        "operationType" : BooleanOperationType.SUBTRACTION,
                        "keepTools" : true
                    });
        }
        catch
        {
            // A failed boolean changes nothing, so the part is still there to show
            throw regenError("Failed to trim to the selected part.", ["trimBodies"], trim.part);
        }
        const kept = qOwnerBody(qUnion(trim.isStart ? endFaces : startFaces));
        const trimmed = qCreatedBy(trimId + "cut", EntityType.FACE)->qOwnedByBody(kept);
        if (isQueryEmpty(context, trimmed))
        {
            // It missed the stock, or cut away its other end
            throw regenError("Failed to trim to the selected part.", ["trimBodies"], trim.part);
        }
        const pieces = qSubtraction(qUnion([body, qCreatedBy(trimId + "cut", EntityType.BODY)]), kept);
        if (!isQueryEmpty(context, pieces))
        {
            opDeleteBodies(context, trimId + "delete", { "entities" : pieces });
        }
        body = kept;
        if (stock.isFrame ?? false)
        {
            tagFrameCaps(context, trimmed, trim.isStart);
        }
        if (trim.isStart)
        {
            startFaces = append(startFaces, trimmed);
        }
        else
        {
            endFaces = append(endFaces, trimmed);
        }
    }
    // The stock's end, or the faces the cuts and trims at its end leave (those still on it)
    const endFace = qUnion(endFaces)->qOwnedByBody(body);
    if (stock.isFrame ?? false)
    {
        // On the body it ended up as, which a trim to parts may have split
        tagFrameProfile(context, body, stock.partName, stock.vendor);
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
    var tiedHoles = [];
    for (var i, group in groups)
    {
        const groupId = id + ("holes" ~ i);
        if (group.seedTied)
        {
            tiedHoles = append(tiedHoles, qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body));
        }
        else if (group.tiedNames != [])
        {
            tiedHoles = append(tiedHoles, qPatternInstances(groupId + "pattern", group.tiedNames, EntityType.FACE)->qOwnedByBody(body));
        }
    }
    return {
            "body" : body,
            "endFace" : endFace,
            "tiedHoles" : qUnion(tiedHoles),
            "tie" : hasHoles ? tie : undefined
        };
};

/**
 * How a row's holes (see `holePositions`, in order from the start) are patterned from one seed: by naming each pattern
 * instance by its hole's count from the start, or for a hole tied to the end, from the end (`"e0"` is the last), tied
 * holes keep their identities as the stock's length changes, with no seed or pattern of their own. The seed is the
 * first hole, patterned toward the end, or when every hole is tied, the last, patterned back toward the start (so
 * every hole, the seed too, is counted from the end).
 *
 * @returns {{
 *      @field seed {number} : The seed's hole, by its index in `positions`.
 *      @field sign {number} : 1 if the pattern runs toward the end, -1 if back toward the start.
 *      @field names {array} : The names of the instances after the seed, the hole a step further each.
 *      @field seedTied {boolean} : Whether the seed is tied (and so every hole is).
 *      @field tiedNames {array} : The names of the tied instances.
 * }}
 */
export function holeInstances(positions is array) returns map
{
    const count = size(positions);
    const fromEnd = positions[0].tied;
    var names = [];
    var tiedNames = [];
    for (var k = 1; k < count; k += 1)
    {
        const hole = fromEnd ? count - 1 - k : k;
        const tied = positions[hole].tied;
        const name = tied ? "e" ~ (count - 1 - hole) : toString(hole);
        names = append(names, name);
        if (tied)
        {
            tiedNames = append(tiedNames, name);
        }
    }
    return {
            "seed" : fromEnd ? count - 1 : 0,
            "sign" : fromEnd ? -1 : 1,
            "names" : names,
            "seedTied" : fromEnd,
            "tiedNames" : tiedNames
        };
}

/**
 * The seeds which cut stock's holes (see `buildCutStock`), in groups patterned the same way (the same steps, with the
 * same instance names; see `holeInstances`), which are cut and patterned together, in fewer operations. Holes which
 * cross, like those of a solid nut strip's rows which line up, have to be.
 *
 * @returns {array} : Maps of each group's `step` between holes, its instances' `names`, whether its seeds are tied
 *          (`seedTied`), the names of its tied instances (`tiedNames`), and its `seeds`: maps of the `face` (see
 *          `stockFaces`) and row `shapes` they cut, and their `position` along the stock.
 */
export function seedGroups(stock is Stock, location is CoordSystem, length is ValueWithUnits, tie is map, across is number) returns array
{
    var groups = [];
    for (var face in stockFaces(stock, location, across))
    {
        for (var row in face.rows)
        {
            const positions = holePositions(row, rowExtent(row, stock.holeDiameter), length, tie);
            if (positions == [])
            {
                continue;
            }
            const instances = holeInstances(positions);
            const seed = { "face" : face, "shapes" : row.shapes, "position" : positions[instances.seed].position };
            const step = location.zAxis * instances.sign * row.pitch;
            var found = false;
            for (var i, group in groups)
            {
                if (group.names == instances.names && group.seedTied == instances.seedTied && tolerantEquals(group.step, step))
                {
                    groups[i].seeds = append(group.seeds, seed);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                groups = append(groups, {
                            "step" : step,
                            "names" : instances.names,
                            "seedTied" : instances.seedTied,
                            "tiedNames" : instances.tiedNames,
                            "seeds" : [seed]
                        });
            }
        }
    }
    return groups;
}

/**
 * Cuts a group of seeds (see `seedGroups`) from the body with one boolean, then face patterns the holes they made along
 * it, the instances named as `holeInstances` names them, all under `groupId`.
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
    if (group.names != [])
    {
        opPattern(context, groupId + "pattern", {
                    "entities" : qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body),
                    "transforms" : mapArray(range(1, size(group.names)), function(k)
                        {
                            return transform(group.step * k);
                        }),
                    "instanceNames" : group.names
                });
    }
}

/**
 * The holes a group of seeds cut (see `cutHoles`), for `setTappedThroughHoles`: maps of each hole's `faces` and
 * `coordSystem`. Each of the group's holes is its seeds' faces (the seed's hole) or a pattern instance's (see
 * `qPatternInstances`), split between its seeds by the direction they go through the stock.
 */
function groupHoles(context is Context, groupId is Id, group is map, body is Query) returns array
{
    const pattern = groupId + "pattern";
    var holes = [];
    for (var k = 0; k <= size(group.names); k += 1)
    {
        const faces = k == 0 ?
            qSubtraction(qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body), qCreatedBy(pattern, EntityType.FACE)) :
            qPatternInstances(pattern, group.names[k - 1], EntityType.FACE);
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
