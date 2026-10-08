FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/fit.fs", version : "");
import(path : "testing.fs", version : "");

export function testCloseAndFreeFromHalfInch()
{
    // std's ANSI clearance holes for 1/2 in.: 0.5156 and 0.5312
    expectNear(standardFitClearance(Fit.CLOSE, undefined, 0.5 * inch), 0.0156 * inch, 1e-9 * meter);
    expectNear(standardFitClearance(Fit.FREE, undefined, 0.5 * inch), 0.0312 * inch, 1e-9 * meter);
}

export function testNoneAndCustom()
{
    expectEqual(standardFitClearance(Fit.NONE, undefined, 0.5 * inch), 0 * meter);
    expectEqual(standardFitClearance(Fit.CUSTOM, 0.2 * millimeter, 0.5 * inch), 0.2 * millimeter);
}

export function testFastenerHoles()
{
    expectNear(fastenerHoleDiameter({ "fit" : Fit.CLOSE }, "#10"), 0.196 * inch, 1e-9 * meter);
    // std's ANSI table's free hole, an H drill
    expectNear(fastenerHoleDiameter({ "fit" : Fit.FREE }, "1/4"), 0.266 * inch, 1e-9 * meter);
    // ISO has no free fit: normal stands in
    expectNear(fastenerHoleDiameter({ "fit" : Fit.FREE }, "M3"), 3.4 * millimeter, 1e-9 * meter);
    expectNear(fastenerHoleDiameter({ "fit" : Fit.NONE }, "M5"), 5 * millimeter, 1e-9 * meter);
}

export function testUnknownFastener()
{
    expectContains(expectThrows(function()
            {
                fastenerHoleDiameter({ "fit" : Fit.CLOSE }, "#99");
            }), "no clearance hole");
}
