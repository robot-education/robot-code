FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "lighten/robotLighten.fs", version : "");
import(path : "testing.fs", version : "");

export function testIsSmallPocket()
{
    const radius = 0.0625 * inch;
    const wall = 0.125 * inch;
    // Under a circle twice as wide as the wall, as cut: a point grows to the bit itself
    expectEqual(isSmallPocket(0 * inch ^ 2, 0 * inch, radius, wall), true);
    // A square 0.25 in. on a side, as cut: its inset is 0.125 in. on a side
    expectEqual(isSmallPocket(0.125 ^ 2 * inch ^ 2, 0.5 * inch, radius, wall), false);
    // A 0.25 in. wall: the circle's 0.5 in. across
    expectEqual(isSmallPocket(0.125 ^ 2 * inch ^ 2, 0.5 * inch, radius, 0.25 * inch), true);
    // A bit as wide as twice the wall: even a point, as cut, is the bit, a circle that size
    expectEqual(isSmallPocket(0 * inch ^ 2, 0 * inch, 0.375 * inch, 0.25 * inch), false);
    // A sliver 0.05 in. wide and 1 in. long, under a 3/8 in. radius bit, cuts a slot 0.8 in. wide: not small, though
    // it's far under a circle twice as wide as the bit
    expectEqual(isSmallPocket(0.05 * inch ^ 2, 2.1 * inch, 0.375 * inch, 0.25 * inch), false);
    // Without Fillet corners, it's the pocket as it is, against twice the wall
    expectEqual(isSmallPocket(0.25 ^ 2 * inch ^ 2, 1 * inch, 0 * inch, wall), false);
    expectEqual(isSmallPocket(0.1 ^ 2 * inch ^ 2, 0.4 * inch, 0 * inch, wall), true);
}
