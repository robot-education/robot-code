FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/loop.fs", version : "");
import(path : "testing.fs", version : "");

function circle(x is number, y is number, radius is number, flipped is boolean) returns BoundaryCircle
{
    return { "location" : vector(x, y) * inch, "radius" : radius * inch, "flipped" : flipped } as BoundaryCircle;
}

export function testTwoEqualCircles()
{
    // Two straight runs, and half of each circle
    const circles = [circle(0, 0, 1, false), circle(4, 0, 1, false)];
    expectNear(loopLength(circles, true), (8 + 2 * PI) * inch, 1e-9 * meter);
    expectNear(loopLength(circles, false), (8 + 2 * PI) * inch, 1e-9 * meter);
}

export function testUnequalCircles()
{
    // The open belt formula: 2 sqrt(C^2 - (R - r)^2) + pi (R + r) + 2 (R - r) asin((R - r) / C)
    const circles = [circle(0, 0, 2, false), circle(5, 0, 1, false)];
    const expected = (2 * sqrt(25 - 1) + PI * 3 + 2 * asin(1 / 5) / radian) * inch;
    expectNear(loopLength(circles, true), expected, 1e-9 * meter);
}

export function testIdlerOutsideShortensPath()
{
    // An idler pushing in from outside, between two pulleys, makes the path longer than the open belt's
    const open = loopLength([circle(0, 0, 1, false), circle(6, 0, 1, false)], true);
    const idled = loopLength([circle(0, 0, 1, false), circle(3, -0.5, 0.5, true), circle(6, 0, 1, false)], true);
    expectTrue(idled > open, "An idler pushed into the belt should lengthen its path");
}

export function testNoPathAroundNestedCircles()
{
    expectEqual(tryLoopLength([circle(0, 0, 2, false), circle(0.5, 0, 1, false)], true), undefined);
}

export function testNearestRoot()
{
    const root = nearestRoot(function(x)
        {
            return x * x / meter - 2 * meter;
        }, 0.1 * millimeter, 1e-12 * meter);
    // Either root is as near: the first found, going up
    expectNear(abs(root), sqrt(2) * meter, 1e-9 * meter);
}

export function testNearestRootPicksNearer()
{
    // Roots at -1 mm and 5 mm
    const root = nearestRoot(function(x)
        {
            return (x + 1 * millimeter) * (x - 5 * millimeter) / meter;
        }, 0.1 * millimeter, 1e-12 * meter);
    expectNear(root, -1 * millimeter, 1e-9 * meter);
}

export function testNearestRootNone()
{
    expectEqual(nearestRoot(function(x)
                {
                    return x * x / meter + 1 * meter;
                }, 0.1 * millimeter, 1e-12 * meter), undefined);
}

export function testTwoCircleDistance()
{
    // Two 1 in. circles 4 in. apart: 8 in. of straight runs and 2 pi in. around
    expectNear(twoCircleDistance([1 * inch, 1 * inch], (8 + 2 * PI) * inch, 0 * meter), 4 * inch, 1e-9 * meter);
    // Unequal ones, from near a distance, come back to where the path's that long
    const length = tryTwoCircleLength([2 * inch, 1 * inch], 5 * inch);
    expectNear(twoCircleDistance([2 * inch, 1 * inch], length, 4.9 * inch), 5 * inch, 1e-9 * meter);
}

export function testTwoCircleDistanceTooShort()
{
    // Shorter than around the circles
    expectEqual(twoCircleDistance([1 * inch, 1 * inch], 5 * inch, 0 * meter), undefined);
}
