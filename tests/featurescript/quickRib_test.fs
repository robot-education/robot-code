FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/lighten/quickRib.fs", version : "");
import(path : "testing.fs", version : "");

const A = vector(0, 0, 0) * inch;
const B = vector(1, 0, 0) * inch;
const C = vector(1, 1, 0) * inch;
const D = vector(0, 1, 0) * inch;

// Stand-ins for selected points (queries' results aren't available in the evaluator)
const P = qCreatedBy(makeId("p"), EntityType.VERTEX);
const Q = qCreatedBy(makeId("q"), EntityType.VERTEX);
const R = qCreatedBy(makeId("r"), EntityType.VERTEX);

export function testHubSegments()
{
    expectEqual(hubSegments(A, [B, C, D]), [[A, B], [A, C], [A, D]]);
    expectEqual(hubSegments(A, []), []);
}

export function testHubCircleRadius()
{
    // Through the first spoke, measured in the sketch's plane: a spoke above it is projected onto it
    expectNear(hubCircleRadius(XY_PLANE, A, C), sqrt(2) * inch, 1e-9 * inch);
    expectNear(hubCircleRadius(XY_PLANE, A, B + vector(0, 0, 3) * inch), 1 * inch, 1e-9 * inch);
}

export function testChainSegments()
{
    // In the order chosen, and closed back to the start; two points are one rib
    expectEqual(chainSegments([A, B, C], false), [[A, B], [B, C]]);
    expectEqual(chainSegments([A, B, C], true), [[A, B], [B, C], [C, A]]);
    expectEqual(chainSegments([A, B], true), [[A, B]]);
    expectEqual(chainSegments([A], false), []);
}

export function testRibsOf()
{
    // Lines first; repeats (lines either way round, circles of the same size) are one rib, with each's source; lines
    // with no length are dropped
    const ribs = ribsOf([{ "circle" : { "center" : A, "through" : B }, "source" : "circle" }, { "line" : [A, B], "source" : "first" },
                { "line" : [B, A], "source" : "again" }, { "line" : [C, C], "source" : "none" }, { "line" : [A, C], "source" : "second" },
                { "circle" : { "center" : A, "through" : D }, "source" : "same circle" }]);
    expectEqual(mapArray(ribs, rib => rib.line), [[A, B], [A, C], undefined]);
    expectEqual(mapArray(ribs, rib => rib.sources), [["first", "again"], ["second"], ["circle", "same circle"]]);
}

export function testRibSources()
{
    // A chain's segments in order, the closing one last; a hub's spokes, then its circle
    expectEqual(mapArray(chainParts([A, B, C], true, { "chain" : 2 }), part => part.source),
        [{ "chain" : 2, "segment" : 0 }, { "chain" : 2, "segment" : 1 }, { "chain" : 2, "segment" : 2 }]);
    expectEqual(mapArray(hubParts(A, [B, C], true, { "hub" : 1 }), part => part.source),
        [{ "hub" : 1, "spoke" : 0 }, { "hub" : 1, "spoke" : 1 }, { "hub" : 1, "circle" : true }]);
}

export function testEdgeRibManipulatorIndex()
{
    expectEqual(edgeRibManipulatorIndex("edgeRib.3"), 3);
    expectEqual(edgeRibManipulatorIndex("other"), undefined);
}

export function testRibTogglePoints()
{
    // A line's middle, and a circle's point across from its first spoke
    expectNear(norm(ribTogglePoint(XY_PLANE, { "line" : [A, C] }) - vector(0.5, 0.5, 0) * inch), 0 * inch, 1e-9 * inch);
    expectNear(norm(ribTogglePoint(XY_PLANE, { "circle" : { "center" : A, "through" : B } }) - vector(-1, 0, 0) * inch), 0 * inch, 1e-9 * inch);
}

export function testStartAndFinishDeleting()
{
    const definition = { "face" : qNothing(), "chains" : [], "hubs" : [], "edgeRibs" : [], "deletingRibs" : false, "ribsToDelete" : [1] };
    const started = quickRibEditLogic(newContext(), newId(), {}, definition, false, { "face" : true }, qNothing(), "startDeleting");
    expectTrue(started.deletingRibs, "deleting");
    expectEqual(started.ribsToDelete, []);
    // There's no rib 0: nothing's deleted
    const finished = quickRibEditLogic(newContext(), newId(), {}, mergeMaps(started, { "ribsToDelete" : [0] }), false, { "face" : true },
        qNothing(), "finishDeleting");
    expectTrue(!finished.deletingRibs, "done");
    expectEqual(finished.ribsToDelete, []);
}

export function testChainPieces()
{
    // An open chain splits where a segment's deleted
    expectEqual(chainPieces(4, false, [1]), [{ "indices" : [0, 1], "closed" : false }, { "indices" : [2, 3], "closed" : false }]);
    // Runs of one point are dropped
    expectEqual(chainPieces(3, false, [0]), [{ "indices" : [1, 2], "closed" : false }]);
    expectEqual(chainPieces(2, false, [0]), []);
    // A closed chain opens, running on around from the deleted segment
    expectEqual(chainPieces(4, true, [1]), [{ "indices" : [2, 3, 0, 1], "closed" : false }]);
    expectEqual(chainPieces(4, true, [3]), [{ "indices" : [0, 1, 2, 3], "closed" : false }]);
    expectEqual(chainPieces(4, true, [0, 2]), [{ "indices" : [3, 0], "closed" : false }, { "indices" : [1, 2], "closed" : false }]);
    // Two points have no closing segment
    expectEqual(chainPieces(2, true, [0]), []);
}

export function testDeleteRibSources()
{
    const S = qCreatedBy(makeId("s"), EntityType.VERTEX);
    const definition = {
            "chains" : [{ "chainPoints" : qUnion([P, Q, R, S]), "closed" : false }, { "chainPoints" : qUnion([P, Q]), "closed" : false }],
            "hubs" : [{ "hub" : P, "spokes" : qUnion([Q, R]), "addCircle" : true }, { "hub" : S, "spokes" : Q, "addCircle" : false }],
            "edgeRibs" : [{ "ribPoints" : qUnion([P, Q]) }, { "ribPoints" : R }]
        };
    const entities = { "chains" : [[P, Q, R, S], [P, Q]], "spokes" : [[Q, R], [Q]], "edgeRibs" : [[P, Q], [R]] };
    const deleted = deleteRibSources(definition, entities, [{ "chain" : 0, "segment" : 1 }, { "chain" : 1, "segment" : 0 },
                { "hub" : 0, "spoke" : 0 }, { "hub" : 0, "circle" : true }, { "hub" : 1, "spoke" : 0 }, { "edgeRib" : 0, "point" : 1 },
                { "edgeRib" : 1, "point" : 0 }]);
    // The first chain splits in two; the second, its only rib gone, is removed
    expectEqual(deleted.chains, [{ "chainPoints" : qUnion([P, Q]), "closed" : false }, { "chainPoints" : qUnion([R, S]), "closed" : false }]);
    // The first hub loses a spoke and its circle; the second, its only spoke gone, is removed
    expectEqual(deleted.hubs, [{ "hub" : P, "spokes" : qUnion([R]), "addCircle" : false }]);
    expectEqual(deleted.edgeRibs, [{ "ribPoints" : qUnion([P]) }]);
    // Nothing to delete leaves them as they are
    expectEqual(deleteRibSources(definition, entities, []), definition);
}

export function testRibIndicesAreNoneUntilSet()
{
    // An isAnything parameter's unset value is a number
    expectEqual(ribIndices(0), []);
    expectEqual(ribIndices([2]), [2]);
}

/**
 * Quick rib's definition, without ribs, adding `type`: a chain through P, Q, and R, or a hub P with spokes Q and R, with
 * Closed and Add circle on.
 */
function quickAdding(type is QuickRibType) returns map
{
    return { "chains" : [], "hubs" : [], "edgeRibs" : [], "quickRibType" : type, "quickPoints" : qUnion([P, Q, R]), "quickClosed" : true,
            "quickHub" : P, "quickSpokes" : qUnion([Q, R]), "quickAddCircle" : true };
}

export function testQuickAddAddsAChain()
{
    const definition = addQuickRibs(quickAdding(QuickRibType.CHAIN));
    expectEqual(definition.chains, [{ "chainPoints" : qUnion([P, Q, R]), "closed" : true }]);
    expectEqual(definition.hubs, []);
    // Cleared, for the next
    expectEqual(definition.quickPoints, qNothing());
}

export function testQuickAddAddsAHubAndSpokes()
{
    const definition = addQuickRibs(quickAdding(QuickRibType.HUB));
    expectEqual(definition.hubs, [{ "hub" : P, "spokes" : qUnion([Q, R]), "addCircle" : true }]);
    expectEqual(definition.chains, []);
    expectEqual(definition.quickHub, qNothing());
    expectEqual(definition.quickSpokes, qNothing());
    // The chain's points are left
    expectEqual(definition.quickPoints, qUnion([P, Q, R]));
}

export function testQuickAddShowsWhatItWouldAdd()
{
    // A closed chain through three points, or a hub with two spokes and its circle through the first
    expectEqual(mapArray(chainRibs([A, B, C], true), rib => rib.line), [[A, B], [B, C], [C, A]]);
    const hub = hubRibs([A], [B, C], true);
    expectEqual(mapArray(hub, rib => rib.line), [[A, B], [A, C], undefined]);
    expectEqual(hub[2].circle, { "center" : A, "through" : B });
    // Nothing from one point, or a hub without a hub or spokes
    expectEqual(chainRibs([A], false), []);
    expectEqual(hubRibs([], [B, C], true), []);
    expectEqual(hubRibs([A], [], true), []);
}
