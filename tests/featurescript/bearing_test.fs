FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/bearing/robotBearing.fs", version : "");
import(path : "bearings/bearingTables.gen.fs", version : "");
import(path : "testing.fs", version : "");

// Every option's values, through every level
function leaves(level is map) returns array
{
    var found = [];
    for (var _, entry in level.entries)
    {
        found = append(found, entry.entries == undefined ? [entry] : leaves(entry));
    }
    return concatenateArrays(found);
}

export function testEveryBearingIsOneTheBearingSetMakes()
{
    for (var entry in leaves(robotBearingTable))
    {
        const made = BEARINGS[entry.vendor][entry.bearingType][entry.bore][entry.size][entry.dimensions][entry.seal];
        expectTrue(made != undefined, entry.product.name ~ " isn't one the bearings set makes");
        expectTrue(canBeProduct(entry.product), entry.product.name ~ " has no product");
        // Its studio's configuration names the set's inputs
        expectEqual(entry.product.configuration.vendor, toString(entry.vendor));
    }
}

export function testWcpMakesHatsForItsTwoBores()
{
    for (var bore in ["0.875 in", "1.125 in"])
    {
        expectTrue(canBeProduct(leaves(wcpHatTable.entries[bore])[0].product), bore);
    }
}

export function testScrewHolesClearTheBore()
{
    const part = { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "innerDiameter" : 0.5 * inch,
            "product" : { "documentId" : "d", "elementId" : "e", "configuration" : {}, "name" : "Bearing" } } as BearingPart;
    for (var count in [ScrewCount.TWO, ScrewCount.THREE])
    {
        const definition = { "screwCount" : count, "overrideHoleCircle" : false, "holeDiameter" : 0.201 * inch };
        const positions = screwPositions(definition, part);
        expectEqual(size(positions), count == ScrewCount.TWO ? 2 : 3);
        for (var position in positions)
        {
            // Each hole's edge clears the bore by a wall
            expectNear(norm(position) - 0.201 * inch / 2, 1.125 * inch / 2 + 0.03 * inch, 1e-9 * meter);
        }
    }
    const overridden = screwPositions({ "screwCount" : ScrewCount.THREE, "overrideHoleCircle" : true, "holeCircleDiameter" : 2 * inch }, part);
    expectNear(norm(overridden[1]), 1 * inch, 1e-9 * meter);
}
