FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "bearing/robotBearing.fs", version : "");
import(path : "released/bearing/robotBearingRetention.fs", version : "");
import(path : "bearings/bearingTables.gen.fs", version : "");
import(path : "bearingHats/bearingHats.fs", version : "");
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
        expectTrue(canBeProduct(leaves(hatTable.entries[bore])[0].product), bore);
    }
}

export function testScrewHolesClearTheBore()
{
    for (var count in [ScrewCount.TWO, ScrewCount.THREE])
    {
        const definition = { "retentionScrewCount" : count, "overrideHoleCircle" : false, "holeDiameter" : 0.201 * inch };
        const positions = screwPositions(definition, 1.125 * inch);
        expectEqual(size(positions), count == ScrewCount.TWO ? 2 : 3);
        for (var position in positions)
        {
            // Each hole's edge clears the bore by a wall
            expectNear(norm(position) - 0.201 * inch / 2, 1.125 * inch / 2 + 0.03 * inch, 1e-9 * meter);
        }
    }
    const overridden = screwPositions({ "retentionScrewCount" : ScrewCount.THREE, "overrideHoleCircle" : true, "holeCircleDiameter" : 2 * inch }, 1.125 * inch);
    expectNear(norm(overridden[1]), 1 * inch, 1e-9 * meter);
}

export function testEveryCotsHatIsOneTheHatSetMakes()
{
    for (var bore in ["0.875 in", "1.125 in"])
    {
        for (var entry in leaves(hatTable.entries[bore]))
        {
            const configuration = entry.product.configuration;
            const made = BEARING_HATS[BearingHatVendor.WCP][BearingHatSize[configuration.size]][BearingHatCenters[configuration.centers]];
            expectTrue(made != undefined, entry.product.name);
            expectEqual(entry.thickness, made.thickness);
            expectEqual(entry.centers, made.centers);
            expectEqual(entry.outerDiameter, made.outerDiameter);
            expectEqual(entry.holeDiameter, made.holeDiameter);
        }
    }
}

export function testEveryHatReachesOverItsBearingAndHasItsHolesAtItsCenters()
{
    for (var _, bySize in BEARING_HATS[BearingHatVendor.WCP])
    {
        for (var _, hat in bySize)
        {
            expectTrue(hat.innerDiameter < hat.outerDiameter, hat.name);
            const holes = hatHoles(hat);
            expectNear(norm(holes[1] - holes[0]), hat.centers, 1e-12 * meter);
            // Each hole clears the outline about the bore
            expectTrue(hat.centers / 2 - hat.holeDiameter / 2 > hat.outerDiameter / 2, hat.name);
        }
    }
}

export function testACotsHatIsTheOneForTheNearestBearing()
{
    expectEqual(cotsHatBore(0.875 * inch), "0.875 in");
    // A bore cut with a free fit's clearance
    expectEqual(cotsHatBore(1.125 * inch + 1 / 32 * inch), "1.125 in");
    expectEqual(cotsHatBore(0.9 * inch), "0.875 in");
}

export function testEveryScrewIsANumber10WithItsLengthOrAutomatic()
{
    for (var _, byVendor in screwTable.entries)
    {
        for (var entry in leaves(byVendor))
        {
            expectTrue(canBeProduct(entry.product), entry.product.name);
            // Size is left out where the vendor makes one, as the studio hides it
            const size = entry.product.configuration.size;
            expectTrue(size == undefined || size == "NO_10_32", entry.product.name);
        }
    }
    // Every head's lengths start with Automatic, which leaves the length to Auto Assembly
    const socket = screwTable.entries["Generic"].entries["Socket head"].entries;
    expectEqual(socket["Automatic"].product.configuration.length, undefined);
    expectEqual(socket["1/2 in"].product.configuration.length, "IN_0_5");
}

export function testEveryNutIsANumber10()
{
    for (var entry in leaves(nutTable))
    {
        expectTrue(canBeProduct(entry.product), entry.product.name);
        expectEqual(entry.product.configuration.size, "NO_10_32");
    }
}

// Every option's path and its values, through every level
function paths(level is map, path is map) returns array
{
    var found = [];
    for (var key, entry in level.entries)
    {
        var next = path;
        next[level.name] = key;
        found = append(found, entry.entries == undefined ? [{ "path" : lookupTablePath(next), "entry" : entry }] : paths(entry, next));
    }
    return concatenateArrays(found);
}

export function testAFlangedBearingsCounterboreHoldsItsFlangeFlush()
{
    var flanged = 0;
    for (var option in paths(robotBearingTable, {}))
    {
        const definition = { "bearing" : option.path, "boreFit" : Fit.FREE, "boreFitClearance" : 0 * inch };
        const isFlanged = option.entry.bearingType != BearingType.RADIAL;
        expectEqual(isFlangedBearing(definition), isFlanged);
        if (isFlanged)
        {
            flanged += 1;
            const entry = option.entry;
            const made = BEARINGS[entry.vendor][entry.bearingType][entry.bore][entry.size][entry.dimensions][entry.seal];
            const counterbore = flangeCounterbore(definition);
            expectEqual(counterbore.depth, made.flangeThickness);
            expectTrue(counterbore.diameter > made.flangeDiameter, entry.product.name);
        }
    }
    expectTrue(flanged > 0, "no flanged bearings");
    // A path that's no whole entry isn't flanged
    expectTrue(!isFlangedBearing({ "bearing" : lookupTablePath({}) }), "an empty path is flanged");
}

export function testAHoleHasTheConnectorsOfThePartInIt()
{
    const seat = plane(vector(0, 0, 0) * inch, vector(0, 0, 1), vector(1, 0, 0));
    const product = { "documentId" : "d", "elementId" : "e", "configuration" : {}, "name" : "Bearing" };
    const flanged = bearingConnectors(seat, { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "innerDiameter" : 0.5 * inch,
                "flangeDiameter" : 1.25 * inch, "flangeThickness" : 0.063 * inch, "product" : product } as BearingPart);
    expectNear(norm(flanged.mateConnector.origin - vector(0, 0, 0) * inch), 0 * meter, 1e-12 * meter);
    // The flange's top, up, and the bottom face, down, as the bearings set's
    expectNear(norm(flanged.topMateConnector.origin - vector(0, 0, 0.063) * inch), 0 * meter, 1e-12 * meter);
    expectNear(norm(flanged.bottomMateConnector.origin - vector(0, 0, -0.25) * inch), 0 * meter, 1e-12 * meter);
    expectTrue(tolerantEquals(flanged.bottomMateConnector.zAxis, vector(0, 0, -1)), "the bottom doesn't point down");
    const radial = bearingConnectors(seat, { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "innerDiameter" : 0.5 * inch,
                "product" : product } as BearingPart);
    // A radial bearing's top is its seat
    expectEqual(radial.topMateConnector, undefined);
    expectNear(norm(radial.bottomMateConnector.origin - vector(0, 0, -0.313) * inch), 0 * meter, 1e-12 * meter);
}
