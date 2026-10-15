FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/frame/robotFrame.fs", version : "");
import(path : "testing.fs", version : "");

export function testHoleStartOffsetMovesEveryRow()
{
    const rows = [{ "start" : 0.5 * inch, "pitch" : 0.5 * inch, "shapes" : [] }, { "start" : 0.25 * inch, "pitch" : 1 * inch, "shapes" : [] }];
    const shifted = holeRows(rows, 0.125 * inch);
    expectNear(shifted[0].start, 0.625 * inch, 1e-12 * meter);
    expectNear(shifted[1].start, 0.375 * inch, 1e-12 * meter);
    // Their spacing is as sold
    expectEqual(shifted[1].pitch, 1 * inch);
}
