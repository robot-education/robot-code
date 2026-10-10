FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "lighten/quickRibs.fs", version : "");
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
