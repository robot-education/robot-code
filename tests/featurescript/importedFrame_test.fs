FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "importedFrame/robotImportedFrame.fs", version : "");
import(path : "testing.fs", version : "");

const WCP_2X1 = { "vendor" : "WCP", "profile" : "Tube", "size" : "2x1", "pattern" : "Full", "wall" : "1/16 in." } as LookupTablePath;

export function testHoleStartMovesTheModelAndItsRows()
{
    const stock = getImportedFrame({ "program" : Program.FRC, "frcFrame" : WCP_2X1, "holeStart" : 0.75 * inch });
    expectNear(stock.model.shift, 0.25 * inch, 1e-12 * meter);
    expectNear(stock.xRows[0].start, 0.75 * inch, 1e-12 * meter);
    expectNear(stock.tieStart, 0.75 * inch, 1e-12 * meter);
    // Its model is the frame's, as long as it's sold
    expectEqual(stock.model.length, 47 * inch);
}

export function testChoosingAFrameSetsItsHoleStart()
{
    const old = {
            "placement" : StockPlacement.EDGE,
            "program" : Program.FTC,
            "ftcFrame" : { "vendor" : "goBILDA", "size" : "U-Channel" } as LookupTablePath,
            "holeStart" : 24 * millimeter
        };
    const definition = robotImportedFrameEditLogic(newContext(), newId(), old, mergeMaps(old, { "program" : Program.FRC, "frcFrame" : WCP_2X1 }),
        false, {}, qNothing());
    expectNear(definition.holeStart, 0.5 * inch, 1e-12 * meter);
}

export function testASetHoleStartIsKept()
{
    const old = { "placement" : StockPlacement.EDGE, "program" : Program.FRC, "frcFrame" : WCP_2X1, "holeStart" : 0.75 * inch };
    const definition = robotImportedFrameEditLogic(newContext(), newId(), old,
        mergeMaps(old, { "frcFrame" : { "vendor" : "REV", "profile" : "Tube", "size" : "1x1", "wall" : "1/8 in." } as LookupTablePath }),
        false, { "holeStart" : true }, qNothing());
    expectNear(definition.holeStart, 0.75 * inch, 1e-12 * meter);
}
