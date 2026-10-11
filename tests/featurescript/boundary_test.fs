FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/boundary.fs", version : "");
import(path : "testing.fs", version : "");

function circle(x is number, y is number, radius is number) returns BoundaryCircle
{
    return { "location" : vector(x, y) * inch, "radius" : radius * inch, "flipped" : false } as BoundaryCircle;
}

export function testCircleToCircle()
{
    // Equal circles: the tangent line runs alongside the line between their centers
    const points = circleToCircle(circle(0, 0, 1), circle(4, 0, 1), false);
    expectNear(points[0][0], 0 * inch, 1e-9 * inch);
    expectNear(points[0][1], 1 * inch, 1e-9 * inch);
    expectNear(points[1][0], 4 * inch, 1e-9 * inch);
    expectNear(points[1][1], 1 * inch, 1e-9 * inch);
}

export function testCircleToCircleRejectsCoincidentAndNestedCircles()
{
    expectThrows(function() { circleToCircle(circle(1, 1, 1), circle(1, 1, 2), false); });
    // One inside the other: no line is tangent to both
    expectThrows(function() { circleToCircle(circle(0, 0, 3), circle(1, 0, 1), false); });
}
