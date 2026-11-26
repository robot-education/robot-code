FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/linearStock.fs", version : "");
import(path : "core/cutStock.fs", version : "");
import(path : "testing.fs", version : "");

function positions(tied is array) returns array
{
    return mapArray(range(0, size(tied) - 1), function(i)
        {
            return { "position" : (0.5 + i * 0.5) * inch, "tied" : tied[i] };
        });
}

export function testUntiedHolesAreNamedFromTheStart()
{
    const instances = holeInstances(positions([false, false, false]));
    expectEqual(instances.seed, 0);
    expectEqual(instances.sign, 1);
    expectEqual(instances.names, ["1", "2"]);
    expectEqual(instances.seedTied, false);
    expectEqual(instances.tiedNames, []);
}

export function testTiedHolesAreNamedFromTheEnd()
{
    const instances = holeInstances(positions([false, false, true, true]));
    // One seed and pattern for both: the tied holes' names count back from the last
    expectEqual(instances.seed, 0);
    expectEqual(instances.names, ["1", "e1", "e0"]);
    expectEqual(instances.tiedNames, ["e1", "e0"]);
}

export function testTiedHolesKeepTheirNamesAsTheLengthChanges()
{
    const shorter = holeInstances(positions([false, true, true]));
    const longer = holeInstances(positions([false, false, false, true, true]));
    expectEqual(shorter.tiedNames, ["e1", "e0"]);
    expectEqual(longer.tiedNames, ["e1", "e0"]);
}

export function testEveryHoleTiedIsSeededAtTheLast()
{
    const instances = holeInstances(positions([true, true, true]));
    expectEqual(instances.seed, 2);
    expectEqual(instances.sign, -1);
    expectEqual(instances.names, ["e1", "e2"]);
    expectEqual(instances.seedTied, true);
}

function tube() returns Stock
{
    return {
            "partName" : "Test",
            "stock" : [],
            "width" : 2 * inch,
            "height" : 1 * inch,
            "wallX" : 0.0625 * inch,
            "wallY" : 0.0625 * inch,
            "holeDiameter" : 0.196 * inch,
            "xRows" : [holeRow(0.5 * inch, 0.5 * inch)],
            "yRows" : [holeRow(0.5 * inch, 0.5 * inch)],
            "tieStart" : 0.5 * inch,
            "tieUnit" : 0.5 * inch
        } as Stock;
}

export function testRowsWithTiedHolesShareOneSeedGroup()
{
    const tie = getTie({ "tieHoles" : true, "tieHolesBy" : TieHolesBy.HALF, "tiedHoleCount" : 3, "tieLength" : 1 * inch }, 0.5 * inch, 0.5 * inch);
    const groups = seedGroups(tube(), WORLD_COORD_SYSTEM, 6 * inch, tie, 1, 0 * meter);
    // Both rows' holes, tied and not, are cut by one boolean and one pattern
    expectEqual(size(groups), 1);
    expectEqual(size(groups[0].seeds), 2);
    // 11 holes, the last 5 tied
    expectEqual(groups[0].names, ["1", "2", "3", "4", "5", "e4", "e3", "e2", "e1", "e0"]);
}

export function testRowsStartingApartAreGroupedApart()
{
    var stock = tube();
    stock.yRows = [holeRow(0.75 * inch, 0.5 * inch)];
    const tie = getTie({ "tieHoles" : false, "tieHolesBy" : TieHolesBy.HALF, "tiedHoleCount" : 3, "tieLength" : 1 * inch }, 0.5 * inch, 0.5 * inch);
    const groups = seedGroups(stock, WORLD_COORD_SYSTEM, 6 * inch, tie, 1, 0 * meter);
    // Different counts of holes, so different names
    expectEqual(size(groups), 2);
}

export function testSelectionShift()
{
    expectEqual(selectionShift(SelectionAt.TOP, 2 * inch), -1 * inch);
    expectEqual(selectionShift(SelectionAt.MIDDLE, 2 * inch), 0 * meter);
    expectEqual(selectionShift(SelectionAt.BOTTOM, 2 * inch), 1 * inch);
}

export function testHolesAlignByTheirMiddleHole()
{
    const shapes = mapArray([-0.5, 0, 0.5], function(offset)
        {
            return { "along" : 0 * meter, "offset" : offset * inch };
        });
    const hole = alignedHole([{ "start" : 0.5 * inch, "pitch" : 0.5 * inch, "shapes" : shapes } as HoleRow]);
    expectEqual(hole.offset, 0 * inch);
    expectEqual(hole.position, 0.5 * inch);
    expectEqual(hole.pitch, 0.5 * inch);
}

export function testHolesAlignByTheFirstOfTheMiddleHoles()
{
    const hole = alignedHole([holeRow(0.75 * inch, 1 * inch), holeRow(0.25 * inch, 0.5 * inch)]);
    expectEqual(hole.position, 0.25 * inch);
    expectEqual(hole.pitch, 0.5 * inch);
}

export function testSplineCutoutsAreNotHolesToAlignBy()
{
    const cutouts = { "start" : 1 * inch, "pitch" : 2 * inch, "shapes" : [{ "along" : 0 * meter, "offset" : 0 * meter, "maxSpline" : true }] } as HoleRow;
    expectEqual(alignedHole([cutouts]), undefined);
    expectEqual(alignedHole([]), undefined);
}

export function testThreePointManipulatorSetsFaceAt()
{
    const moved = { "selectionAtManipulator" : { "index" : 0 } };
    const onFace = stockManipulatorChange(newContext(), { "placement" : StockPlacement.FACE, "faceAt" : SelectionAt.BOTTOM, "flip" : false }, moved);
    expectEqual(onFace.faceAt, SelectionAt.TOP);
    // Another manipulator leaves it
    const flipped = stockManipulatorChange(newContext(), { "placement" : StockPlacement.FACE, "faceAt" : SelectionAt.MIDDLE, "flip" : false },
        { "stockFlipManipulator" : { "flipped" : true } });
    expectEqual(flipped.faceAt, SelectionAt.MIDDLE);
    expectEqual(flipped.flip, true);
}

export function testThreePointManipulatorSetsHolesAt()
{
    const onHoles = stockManipulatorChange(newContext(), { "placement" : StockPlacement.HOLES, "holesAt" : SelectionAt.BOTTOM, "flip" : false },
        { "selectionAtManipulator" : { "index" : 1 } });
    expectEqual(onHoles.holesAt, SelectionAt.MIDDLE);
}

export function testStartingOffsetDraggedPastTheFaceFlips()
{
    for (var placement in [StockPlacement.FACE, StockPlacement.HOLES])
    {
        const dragged = stockManipulatorChange(newContext(), { "placement" : placement, "flip" : false, "startOffsetDistance" : 0.25 * inch,
                    "startOffsetOppositeDirection" : false }, { "startingOffsetManipulator" : { "offset" : -1 * inch } });
        expectEqual(dragged.startOffsetDistance, 1 * inch);
        expectEqual(dragged.startOffsetOppositeDirection, true);
    }
}

export function testHolesOffsetsDraggedPastTheirEndFlip()
{
    const dragged = stockManipulatorChange(newContext(), { "placement" : StockPlacement.HOLES, "flip" : false, "holesAt" : SelectionAt.BOTTOM,
                "edgeStartOffset" : 0.25 * inch, "edgeStartOffsetOppositeDirection" : false }, { "startOffsetManipulator" : { "offset" : -1 * inch } });
    expectEqual(dragged.edgeStartOffset, 1 * inch);
    expectEqual(dragged.edgeStartOffsetOppositeDirection, true);
}

/** WCP's 1/2 in. nut strip: rows of #10-32 holes 1/2 in. apart, alternating every 1/4 in. (see nutStripTables.py). */
function wcpNutStrip() returns Stock
{
    return {
            "partName" : "Test",
            "stock" : [],
            "width" : 0.5 * inch,
            "height" : 0.5 * inch,
            "solid" : true,
            "holeDiameter" : 0.159 * inch,
            "xRows" : [holeRow(0.25 * inch, 0.5 * inch)],
            "yRows" : [holeRow(0.5 * inch, 0.5 * inch)],
            "tieStart" : 0.25 * inch,
            "tieUnit" : 0.25 * inch
        } as Stock;
}

function untied(stock is Stock) returns map
{
    return getTie({ "tieHoles" : false, "tieHolesBy" : TieHolesBy.HALF, "tiedHoleCount" : 3, "tieLength" : 1 * inch }, stock.tieStart, stock.tieUnit);
}

/** Where a row's holes are along stock between holes, from its start (see `holeEnds`). */
function rowHoles(stock is Stock, row is HoleRow, ends is map) returns array
{
    return mapArray(holePositions(row, rowExtent(row, stock.holeDiameter), ends.length + ends.lead, untied(stock), ends.lead),
        function(hole)
        {
            return roundToPrecision((hole.position - ends.lead) / inch, 6);
        });
}

export function testANutStripBetweenHolesEndsAsItsSold()
{
    const strip = wcpNutStrip();
    // Holes 1 in. apart (one between them), lined up by the row from 1/4 in.: 1/4 in. past each
    const byX = holeEnds(strip, 0.25 * inch, 1 * inch, 0 * meter, 0 * meter);
    expectNear(byX.length, 1.5 * inch, 1e-9 * meter);
    expectEqual(rowHoles(strip, strip.yRows[0], byX), [0.5, 1]);
    // By the row from 1/2 in., 1/2 in. before the start hole, but 1/4 in. past the end hole, as a nut strip ends
    const byY = holeEnds(strip, 0.5 * inch, 1 * inch, 0 * meter, 0 * meter);
    expectNear(byY.length, 1.75 * inch, 1e-9 * meter);
    expectEqual(rowHoles(strip, strip.xRows[0], byY), [0.25, 0.75, 1.25]);
    expectEqual(rowHoles(strip, strip.yRows[0], byY), [0.5, 1, 1.5]);
}

export function testAHoleTubeBetweenHolesReachesAsFarPastEach()
{
    // Holes from 1/2 in., every 1/2 in.: 1/2 in. past each selected hole
    expectNear(holeEnds(tube(), 0.5 * inch, 2 * inch, 0 * meter, 0 * meter).length, 3 * inch, 1e-9 * meter);
}

export function testANutStripsHolesGoOnIntoALongerStart()
{
    const strip = wcpNutStrip();
    // Its start 0.37 in. further out: every hole of both rows which fits is there, on the selected holes
    const ends = holeEnds(strip, 0.25 * inch, 1 * inch, -0.37 * inch, 0 * meter);
    expectNear(ends.length, 1.87 * inch, 1e-9 * meter);
    expectEqual(rowHoles(strip, strip.xRows[0], ends), [0.12, 0.62, 1.12, 1.62]);
    expectEqual(rowHoles(strip, strip.yRows[0], ends), [0.37, 0.87, 1.37]);
}

export function testHolesAtAShorterStartAreCutThrough()
{
    // Its start 0.3 in. in: the hole 0.25 in. from where it started is cut through by it; those wholly past it go on
    const ends = holeEnds(tube(), 0.5 * inch, 2 * inch, 0.3 * inch, 0 * meter);
    expectEqual(rowHoles(tube(), tube().xRows[0], ends), [0.2, 0.7, 1.2, 1.7, 2.2]);
}

export function testAStartAWholePatternOutIsAsSold()
{
    // Out a whole pattern (1/2 in.): its holes are as on stock as it's sold, so it's laid out from its start
    const strip = wcpNutStrip();
    const ends = holeEnds(strip, 0.25 * inch, 1 * inch, -0.5 * inch, 0 * meter);
    expectNear(ends.lead, 0 * meter, 1e-9 * meter);
    expectEqual(rowHoles(strip, strip.xRows[0], ends), [0.25, 0.75, 1.25, 1.75]);
}

export function testHolesBeforeTheStartArentHalfTheTiedHoles()
{
    const strip = wcpNutStrip();
    const tie = getTie({ "tieHoles" : true, "tieHolesBy" : TieHolesBy.HALF, "tiedHoleCount" : 3, "tieLength" : 1 * inch }, strip.tieStart, strip.tieUnit);
    // Laid out from 0.3 in. before its start: the hole which counts at 0.25 in. is before it (though its start cuts
    // through it), so of the 5 on it (at 0.5 in. to 1.5 in., the last cut through by its end), the last 2 are tied
    var tied = [];
    for (var row in [strip.xRows[0], strip.yRows[0]])
    {
        for (var hole in holePositions(row, rowExtent(row, strip.holeDiameter), 1.55 * inch, tie, 0.3 * inch))
        {
            if (hole.tied)
            {
                tied = append(tied, roundToPrecision(hole.position / inch, 6));
            }
        }
    }
    expectEqual(sort(tied, function(a, b) { return a - b; }), [1.25, 1.5]);
}

export function testHolePatternPeriod()
{
    expectNear(holePatternPeriod(wcpNutStrip()), 0.5 * inch, 1e-9 * meter);
    expectNear(holePatternPeriod(tube()), 0.5 * inch, 1e-9 * meter);
    var max = tube();
    max.xRows = [holeRow(1.5 * inch, 2 * inch)];
    expectNear(holePatternPeriod(max), 2 * inch, 1e-9 * meter);
}
