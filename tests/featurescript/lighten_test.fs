FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "lighten/robotLighten.fs", version : "");
import(path : "testing.fs", version : "");

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
