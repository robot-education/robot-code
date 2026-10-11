FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "lighten/robotLighten.fs", version : "");
import(path : "testing.fs", version : "");

const HAIR = TOLERANCE.booleanDefaultTolerance * meter;

export function testIsNarrowSide()
{
    // Square corners: a hair round takes a hair of the side at each, and a hair more must be left
    expectEqual(isNarrowSide(2.5 * HAIR, [90 * degree, 90 * degree]), true);
    expectEqual(isNarrowSide(3.5 * HAIR, [90 * degree, 90 * degree]), false);
    // Sharper corners take more of it
    expectEqual(isNarrowSide(3.5 * HAIR, [150 * degree, 150 * degree]), true);
    // A wide side isn't narrow, however sharp its corners
    expectEqual(isNarrowSide(1 * millimeter, [170 * degree, 170 * degree]), false);
}

export function testIsSmallPocket()
{
    const radius = 0.0625 * inch;
    const wall = 0.125 * inch;
    // Under a circle twice as wide as the bit, as cut: a point grows to the bit itself
    expectEqual(isSmallPocket(0 * inch ^ 2, 0 * inch, radius, wall), true);
    // A square 0.25 in. on a side, as cut: its inset is 0.125 in. on a side
    expectEqual(isSmallPocket(0.125 ^ 2 * inch ^ 2, 0.5 * inch, radius, wall), false);
    // A 0.25 in. wall is wider than the bit: the circle's 0.5 in. across
    expectEqual(isSmallPocket(0.125 ^ 2 * inch ^ 2, 0.5 * inch, radius, 0.25 * inch), true);
    // Without Fillet corners, it's the pocket as it is, against twice the wall
    expectEqual(isSmallPocket(0.25 ^ 2 * inch ^ 2, 1 * inch, 0 * inch, wall), false);
    expectEqual(isSmallPocket(0.1 ^ 2 * inch ^ 2, 0.4 * inch, 0 * inch, wall), true);
}
