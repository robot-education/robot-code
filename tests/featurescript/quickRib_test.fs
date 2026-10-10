FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "rib/quickRib.fs", version : "");
import(path : "testing.fs", version : "");

const A = vector(0, 0, 0) * inch;
const B = vector(1, 0, 0) * inch;
const C = vector(1, 1, 0) * inch;
const D = vector(0, 1, 0) * inch;

export function testHubAndSpoke()
{
    expectEqual(ribSegments(RibPattern.HUB_AND_SPOKE, { "hub" : A, "spokes" : [B, C, D] }), [[A, B], [A, C], [A, D]]);
}

export function testChain()
{
    // In the order chosen, and closed back to the start
    expectEqual(ribSegments(RibPattern.CHAIN, { "chain" : [A, B, C], "closed" : false }), [[A, B], [B, C]]);
    expectEqual(ribSegments(RibPattern.CHAIN, { "chain" : [A, B, C], "closed" : true }), [[A, B], [B, C], [C, A]]);
    // Two points close into the same line, so it isn't drawn twice
    expectEqual(ribSegments(RibPattern.CHAIN, { "chain" : [A, B], "closed" : true }), [[A, B]]);
}

export function testPointToPoint()
{
    expectEqual(ribSegments(RibPattern.POINT_TO_POINT, { "start" : A, "end" : C }), [[A, C]]);
}

export function testUniqueSegments()
{
    // Repeats either way round, and lines with no length, are dropped
    expectEqual(uniqueSegments([[A, B], [B, A], [C, C], [A, C], [A, B]]), [[A, B], [A, C]]);
}
