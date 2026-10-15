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

export function testChoosingACotsFrameSetsItsHoleStart()
{
    const old = { "program" : Program.FRC, "source" : FrameSource.CUSTOM, "holeStart" : 0.75 * inch };
    const definition = frameChangeEditLogic(old, mergeMaps(old, {
                    "source" : FrameSource.COTS,
                    "frcFrame" : { "vendor" : "WCP", "profile" : "Tube", "size" : "2x1", "pattern" : "Full", "wall" : "1/16 in." } as LookupTablePath
                }), {});
    expectNear(definition.holeStart, 0.5 * inch, 1e-12 * meter);
}
