FeatureScript 2960;
/**
 * Cut stock: linear stock (see `core/linearStock.fs`) built the usual way, by extruding its profile and cutting its
 * holes (see `buildCutStock`), like nut strips, and frames which can't be built from their walls (see
 * `core/wallStock.fs`).
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "72b77780ed382be329401627", version : "a0ad561c9e3a3bdae870026b");
import(path : "f58a965fe7005e2e3c9a67c7", version : "5794d15cbce3be3e963e93d5");
import(path : "9fc889bb93a3c29feb4f9ae5", version : "45d4504d059b748ebf938c51");

/**
 * Builds stock `length` long from `location` (the middle of its start, with Z along its length and its width along
 * X): its profile is extruded along its length, then its holes are cut, then its ends are trimmed. A `placeStock`
 * builder.
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
    const body = qCreatedBy(stockId, EntityType.BODY);
    if (stock.isFrame ?? false)
    {
        // Before the holes are cut, so its faces are still the extrude's
        tagExtrudeAsFrame(context, stockId, stock.partName, stock.vendor);
    }

    var tiedHoles = [];
    const groups = seedGroups(stock, location, length, tie, across);
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
    }
    // The stock's end, or the faces the cuts at its end leave (those still on it)
    const endFace = qUnion(endFaces)->qOwnedByBody(body);

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
};

/**
 * The seeds which cut stock's holes (see `buildCutStock`), in groups patterned the same way (as many times, in the same
 * direction), which are cut and patterned together, in fewer operations. Holes which cross, like those of a solid nut
 * strip's rows which line up, have to be.
 *
 * @returns {array} : Maps of each group's `count` (how many holes its seeds are patterned into, including themselves),
 *          the `step` between them, whether they're `tied`, and its `seeds`: maps of the `face` (see `stockFaces`) and
 *          row `shapes` they cut, and their `position` along the stock.
 */
function seedGroups(stock is Stock, location is CoordSystem, length is ValueWithUnits, tie is map, across is number) returns array
{
    var groups = [];
    for (var face in stockFaces(stock, location, across))
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
                    "instanceNames" : mapArray(instances, function(k)
                        {
                            return toString(k);
                        })
                });
    }
}

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
            qPatternInstances(pattern, toString(k), EntityType.FACE);
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
