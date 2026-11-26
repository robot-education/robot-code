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
    const groups = seedGroups(tube(), WORLD_COORD_SYSTEM, 6 * inch, tie, 1);
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
    const groups = seedGroups(stock, WORLD_COORD_SYSTEM, 6 * inch, tie, 1);
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
