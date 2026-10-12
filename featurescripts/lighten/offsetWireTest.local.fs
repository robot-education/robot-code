FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * An experiment, not a feature: whether std's internal `opOffsetWire` (what thin extrudes use) could do Robot
 * lighten's work in 2D. Paste it into a Feature Studio, and try it on awkward faces: a narrow neck which a wall pinches
 * off, a neck exactly twice the wall wide (a tangency), holes, and arcs meeting lines.
 *
 * Its stages, each a body left in the Part Studio (Stop after picks the last):
 *
 * 1. Bands: the face's edges, each loop offset both ways by Wall (`makeRegions`), as flat sheets. Look at the joins at
 *    convex and concave corners (arcs, or sharp?), and where a band crosses itself or another.
 * 2. Inset: the face's extrude (a thin slab), less the bands (extruded into solids): what the walls leave. Look at
 *    whether a pinched neck splits the slab cleanly, and what a tangency does.
 * 3. Grown: the inset, with bands of Radius around its own loops added back (a union): a router's pocket, if the
 *    outer joins are arcs.
 *
 * Edges mode offsets just the selected edges, with Offset 1 and Offset 2, for poking at `opOffsetWire` itself.
 *
 * Found with it: for a closed chain, `offset1` is outward of the region it encloses, and `offset2` inward. Bands both
 * ways around a hole fail where they're wider than its radius, so Robot lighten offsets each loop one way: the outer
 * loop in, and holes out of them.
 */
export enum OffsetWireTestMode
{
    annotation { "Name" : "Face" }
    FACE,
    annotation { "Name" : "Edges" }
    EDGES
}

export enum OffsetWireTestStage
{
    annotation { "Name" : "Bands" }
    BANDS,
    annotation { "Name" : "Inset" }
    INSET,
    annotation { "Name" : "Grown" }
    GROWN
}

annotation { "Feature Type Name" : "Offset wire test" }
export const offsetWireTest = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Mode", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.mode is OffsetWireTestMode;

        if (definition.mode == OffsetWireTestMode.FACE)
        {
            annotation { "Name" : "Face", "Filter" : EntityType.FACE && GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
            definition.face is Query;

            annotation { "Name" : "Wall" }
            isLength(definition.wall, BLEND_BOUNDS);

            annotation { "Name" : "Radius" }
            isLength(definition.radius, BLEND_BOUNDS);

            annotation { "Name" : "Stop after" }
            definition.stage is OffsetWireTestStage;

            annotation { "Name" : "Each loop separately", "Default" : true }
            definition.eachLoop is boolean;
        }
        else
        {
            annotation { "Name" : "Edges", "Filter" : EntityType.EDGE }
            definition.edges is Query;

            annotation { "Name" : "Offset 1" }
            isLength(definition.offset1, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Offset 2" }
            isLength(definition.offset2, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Make regions", "Default" : true }
            definition.makeRegions is boolean;

            annotation { "Name" : "Flip" }
            definition.flip is boolean;
        }
    }
    {
        if (definition.mode == OffsetWireTestMode.EDGES)
        {
            const edges = verifyNonemptyQuery(context, definition, "edges", "Select edges.");
            // A sketch's plane, or else a flat face the first edge is on
            var normal = try silent(evOwnerSketchPlane(context, { "entity" : edges[0] }).normal);
            if (normal == undefined)
            {
                normal = evPlane(context, { "face" : qAdjacent(edges[0], AdjacencyType.EDGE, EntityType.FACE)->qGeometry(GeometryType.PLANE)->qNthElement(0) }).normal;
            }
            opOffsetWire(context, id + "offset", {
                        "edges" : definition.edges,
                        "normal" : normal,
                        "offset1" : definition.offset1,
                        "offset2" : definition.offset2,
                        "flip" : definition.flip,
                        "makeRegions" : definition.makeRegions
                    });
            reportFeatureInfo(context, id, size(evaluateQuery(context, qCreatedBy(id + "offset", EntityType.BODY))) ~ " bodies, " ~
                    size(evaluateQuery(context, qCreatedBy(id + "offset", EntityType.FACE))) ~ " faces.");
            return;
        }

        const face = verifyNonemptyQuery(context, definition, "face", "Select a face.")[0];
        const plane = evPlane(context, { "face" : face });
        // A slab of the face, a wall thick, so the stages are solids which booleans can cut
        const depth = definition.wall;

        // 1. Bands around the face's loops
        const bands = offsetBands(context, id + "bands", qLoopEdges(face), plane.normal, definition.wall, definition.eachLoop);
        if (definition.stage == OffsetWireTestStage.BANDS)
        {
            reportFeatureInfo(context, id, size(evaluateQuery(context, bands)) ~ " band faces.");
            return;
        }

        // 2. The slab, less the bands
        opExtrude(context, id + "slab", {
                    "entities" : face,
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : depth
                });
        const cutters = extrudeThrough(context, id + "bandSolids", bands, plane, depth);
        opBoolean(context, id + "inset", {
                    "targets" : qCreatedBy(id + "slab", EntityType.BODY),
                    "tools" : cutters,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        const inset = qUnion([qCreatedBy(id + "slab", EntityType.BODY), qCreatedBy(id + "inset", EntityType.BODY)]);
        opDeleteBodies(context, id + "deleteBands", { "entities" : qCreatedBy(id + "bands", EntityType.BODY) });
        if (definition.stage == OffsetWireTestStage.INSET)
        {
            reportFeatureInfo(context, id, size(evaluateQuery(context, inset)) ~ " inset pieces.");
            return;
        }

        // 3. Grown back: bands of Radius around the inset's own loops (on the face's plane), added
        const insetLoops = qLoopEdges(qCoincidesWithPlane(qOwnedByBody(inset, EntityType.FACE), plane));
        const growBands = offsetBands(context, id + "growBands", insetLoops, plane.normal, definition.radius, definition.eachLoop);
        // Exactly the slab's depth, so the union's flush with it
        opExtrude(context, id + "growSolids", {
                    "entities" : growBands,
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : depth
                });
        const growers = qCreatedBy(id + "growSolids", EntityType.BODY);
        opBoolean(context, id + "grow", {
                    "tools" : qUnion([inset, growers]),
                    "operationType" : BooleanOperationType.UNION
                });
        opDeleteBodies(context, id + "deleteGrowBands", { "entities" : qCreatedBy(id + "growBands", EntityType.BODY) });
        reportFeatureInfo(context, id, size(evaluateQuery(context, qUnion([inset, growers]))) ~ " grown pieces.");
    });

/**
 * Bands `distance` either side of `edges` (`opOffsetWire` with `makeRegions`), under `id`: in one call, or a call per
 * connected loop (`eachLoop`), since a call's input edges mustn't intersect, and bands of different loops can cross.
 */
function offsetBands(context is Context, id is Id, edges is Query, normal is Vector, distance is ValueWithUnits, eachLoop is boolean) returns Query
{
    const chains = eachLoop ? connectedChains(context, edges) : [edges];
    for (var i, chain in chains)
    {
        opOffsetWire(context, id + unstableIdComponent(i), {
                    "edges" : chain,
                    "normal" : normal,
                    "offset1" : distance,
                    "offset2" : distance,
                    "makeRegions" : true
                });
    }
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * `edges`, split into chains of edges which share vertices.
 */
function connectedChains(context is Context, edges is Query) returns array
{
    var left = evaluateQuery(context, edges);
    var chains = [];
    while (left != [])
    {
        var chain = [left[0]];
        left = subArray(left, 1);
        while (true)
        {
            const next = evaluateQuery(context, qIntersection([qAdjacent(qUnion(chain), AdjacencyType.VERTEX, EntityType.EDGE), qUnion(left)]));
            if (next == [])
            {
                break;
            }
            chain = concatenateArrays([chain, next]);
            left = filter(left, edge => !isIn(edge, next));
        }
        chains = append(chains, qUnion(chain));
    }
    return chains;
}

/**
 * `faces` (flat sheets on `plane`) extruded into solids past both sides of the slab, `depth` under it, so they cut
 * through it.
 */
function extrudeThrough(context is Context, id is Id, faces is Query, plane is Plane, depth is ValueWithUnits) returns Query
{
    opExtrude(context, id, {
                "entities" : faces,
                "direction" : -plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth * 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : depth
            });
    return qCreatedBy(id, EntityType.BODY);
}
