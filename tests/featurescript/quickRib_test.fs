FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "rib/quickRib.fs", version : "");
import(path : "testing.fs", version : "");

const A = vector(0, 0, 0) * inch;
const B = vector(1, 0, 0) * inch;
const C = vector(1, 1, 0) * inch;
const D = vector(0, 1, 0) * inch;

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

export function testUniqueSegments()
{
    // Repeats either way round, and lines with no length, are dropped
    expectEqual(uniqueSegments([[A, B], [B, A], [C, C], [A, C], [A, B]]), [[A, B], [A, C]]);
}

export function testEdgeRibManipulatorIndex()
{
    expectEqual(edgeRibManipulatorIndex("edgeRib.3"), 3);
    expectEqual(edgeRibManipulatorIndex("other"), undefined);
}

export function testRibKeysAreTheSameEitherWayRound()
{
    expectEqual(lineKey([A, B]), lineKey([B, A]));
    expectTrue(lineKey([A, B]) != lineKey([A, C]), "different lines");
    // Ends within a micrometer are the same rib
    expectEqual(lineKey([A, B + vector(1e-8, 0, 0) * meter]), lineKey([A, B]));
    expectTrue(circleKey(A, B) != circleKey(A, C), "different radii");
}

export function testRibTogglePoints()
{
    // A line's middle, and a circle's point across from its first spoke
    expectNear(norm(ribTogglePoint(XY_PLANE, { "line" : [A, C] }) - vector(0.5, 0.5, 0) * inch), 0 * inch, 1e-9 * inch);
    expectNear(norm(ribTogglePoint(XY_PLANE, { "circle" : { "center" : A, "through" : B } }) - vector(-1, 0, 0) * inch), 0 * inch, 1e-9 * inch);
}

/**
 * Quick rib's definition, without ribs, deleting `deleted` and choosing `toDelete`.
 */
function deleting(deletingRibs is boolean, deleted is array, toDelete is array) returns map
{
    return { "face" : qNothing(), "chains" : [], "hubs" : [], "edgeRibs" : [], "deletingRibs" : deletingRibs, "deletedRibs" : deleted,
            "ribsToDelete" : toDelete };
}

export function testStartDeletingStartsFromTheDeletedRibs()
{
    const definition = quickRibEditLogic(newContext(), newId(), {}, deleting(false, ["a"], []), false, { "face" : true }, qNothing(), "startDeleting");
    expectTrue(definition.deletingRibs, "deleting");
    // So deleted ones can be put back
    expectEqual(definition.ribsToDelete, ["a"]);
}

export function testFinishDeletingDeletesTheChosenRibs()
{
    const definition = quickRibEditLogic(newContext(), newId(), {}, deleting(true, ["a"], ["b", "c"]), false, { "face" : true }, qNothing(), "finishDeleting");
    expectTrue(!definition.deletingRibs, "done");
    expectEqual(definition.deletedRibs, ["b", "c"]);
    expectEqual(definition.ribsToDelete, []);
}

/**
 * Quick rib's definition, without ribs, adding `type` with Closed and Add circle on.
 */
function quickAdding(type is QuickRibType) returns map
{
    return { "chains" : [], "hubs" : [], "edgeRibs" : [], "quickRibType" : type, "quickSelections" : qEverything(), "quickClosed" : true,
            "quickAddCircle" : true };
}

// Stand-ins for selected points (queries' results aren't available in the evaluator)
const P = qCreatedBy(makeId("p"), EntityType.VERTEX);
const Q = qCreatedBy(makeId("q"), EntityType.VERTEX);
const R = qCreatedBy(makeId("r"), EntityType.VERTEX);

export function testQuickAddAddsAChain()
{
    const definition = addQuickRibs(quickAdding(QuickRibType.CHAIN), [P, Q, R]);
    expectEqual(definition.chains, [{ "chainPoints" : qUnion([P, Q, R]), "closed" : true }]);
    expectEqual(definition.hubs, []);
    // Cleared, for the next
    expectEqual(definition.quickSelections, qNothing());
}

export function testQuickAddAddsAHubAndSpokes()
{
    // The first selection is the hub
    const definition = addQuickRibs(quickAdding(QuickRibType.HUB), [P, Q, R]);
    expectEqual(definition.hubs, [{ "hub" : P, "spokes" : qUnion([Q, R]), "addCircle" : true }]);
    expectEqual(definition.chains, []);
}

export function testQuickAddLeavesOneSelection()
{
    // One point makes no rib: it's left to add to
    expectEqual(addQuickRibs(quickAdding(QuickRibType.CHAIN), [P]), quickAdding(QuickRibType.CHAIN));
}

export function testQuickAddShowsWhatItWouldAdd()
{
    // A closed chain through three points, or a hub with two spokes and its circle through the first
    const chain = quickAddRibsOf(quickAdding(QuickRibType.CHAIN), [A, B, C]);
    expectEqual(mapArray(chain, rib => rib.line), [[A, B], [B, C], [C, A]]);
    const hub = quickAddRibsOf(quickAdding(QuickRibType.HUB), [A, B, C]);
    expectEqual(mapArray(hub, rib => rib.line), [[A, B], [A, C], undefined]);
    expectEqual(hub[2].circle, { "center" : A, "through" : B });
    expectEqual(quickAddRibsOf(quickAdding(QuickRibType.CHAIN), [A]), []);
}

export function testToggledRibs()
{
    const ribs = [{ "key" : "a" }, { "key" : "b" }, { "key" : "c" }];
    expectEqual(toggledRibs(ribs, ["c", "a", "gone"]), [0, 2]);
    expectEqual(toggledRibs(ribs, []), []);
}
