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

export function testCustomSizeTakesItsSizeAndRowsFromTheDialog()
{
    const size = customFrameSize({
                "program" : Program.FRC,
                "source" : FrameSource.CUSTOM,
                "customFrame" : { "profile" : "Tube", "size" : "Custom" } as LookupTablePath,
                "width" : 3 * inch,
                "height" : 1.5 * inch,
                "topHoles" : true,
                "sideHoles" : false,
                "topRows" : 2,
                "sideRows" : 3,
                "topRowSpacing" : 1 * inch,
                "sideRowSpacing" : 0.25 * inch
            });
    expectEqual(size.width, 3 * inch);
    expectEqual(size.height, 1.5 * inch);
    expectEqual(size.angle, false);
    expectEqual(size.topRows, 2);
    expectEqual(size.sideRows, 0);
    expectEqual(size.topRowSpacing, 1 * inch);
    expectEqual(size.sideRowSpacing, 0.25 * inch);
}

export function testATableSizeTakesItsSizeAndRowsFromTheTable()
{
    const size = customFrameSize({
                "program" : Program.FRC,
                "source" : FrameSource.CUSTOM,
                "customFrame" : { "profile" : "Tube", "size" : "2x1", "topRows" : "3", "sideRows" : "1" } as LookupTablePath,
                // Ignored: they're the custom size's
                "width" : 3 * inch,
                "topRows" : 1,
                "topRowSpacing" : 1 * inch,
                "tableTopRowSpacing" : 0.5 * inch,
                "tableSideRowSpacing" : 0.375 * inch
            });
    expectEqual(size.width, 2 * inch);
    expectEqual(size.topRows, 3);
    expectEqual(size.sideRows, 1);
    expectEqual(size.topRowSpacing, 0.5 * inch);
    expectEqual(size.sideRowSpacing, 0.375 * inch);
}
