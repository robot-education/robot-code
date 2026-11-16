FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "bearings/bearings.fs", version : "");
import(path : "testing.fs", version : "");

export function testRoundedHexBoreReachesItsRound()
{
    // 1/2 in hex's corners are 0.2887 in out, past its 13.75 mm round
    expectNear(boreRadius(BEARING_BORES[BearingBore.HEX][BearingSize.IN_1_2]), 0.5 * inch / sqrt(3), 1e-9 * meter);
    expectNear(boreRadius(BEARING_BORES[BearingBore.ROUNDED_HEX][BearingSize.IN_1_2]), 13.75 / 2 * millimeter, 1e-9 * meter);
    // A ThunderSquare's corners, 0.177 in out, are rounded to 0.151 in
    expectNear(boreRadius(BEARING_BORES[BearingBore.THUNDERSQUARE][BearingSize.IN_1_4]), 0.151 * inch, 1e-9 * meter);
    expectNear(boreRadius(BEARING_BORES[BearingBore.ROUND][BearingSize.MM_8]), 4 * millimeter, 1e-9 * meter);
    // goBILDA's 8 mm REX bearing is a 7 mm hex, its corners unrounded
    expectNear(boreRadius(BEARING_BORES[BearingBore.REX][BearingSize.MM_8]), 7 * millimeter / sqrt(3), 1e-9 * meter);
}

export function testEveryBearingsProfileFitsAroundItsBore()
{
    for (var vendor, types in BEARINGS)
    {
        for (var type, bores in types)
        {
            for (var bore, sizes in bores)
            {
                for (var sizeId, dimensions in sizes)
                {
                    const radius = boreRadius(BEARING_BORES[bore][sizeId]);
                    for (var dimension, seals in dimensions)
                    {
                        for (var seal, bearing in seals)
                        {
                            const isFlanged = type == BearingType.FLANGED;
                            const points = bearingProfile(bearing, radius, isFlanged);
                            // Closed, on the axis
                            expectSamePoint(points[0], points[size(points) - 1]);
                            // Its inner race clears the bore, and its outer race is inside its outer diameter
                            expectTrue(points[1][0] > radius, "Inner race inside the bore");
                            expectTrue(points[3][0] < bearing.outerDiameter / 2, "Outer race past the outer diameter");
                            expectTrue(points[1][0] < points[3][0], "Races overlap");
                            expectNear(points[size(points) - 3][1] - points[0][1], bearing.width, 1e-9 * meter);
                            if (isFlanged)
                            {
                                expectTrue(bearing.flangeDiameter > bearing.outerDiameter, "Flange inside the outer diameter");
                                expectNear(points[size(points) - 3][1], bearing.flangeThickness, 1e-9 * meter);
                            }
                        }
                    }
                }
            }
        }
    }
}

export function testFlangedBearingSeatsOnItsFlange()
{
    const bearing = BEARINGS[BearingVendor.GENERIC][BearingType.FLANGED][BearingBore.ROUNDED_HEX][BearingSize.IN_1_2][BearingDimensions.OD_1_125_X_0_313][BearingSeal.SHIELDED];
    const points = bearingProfile(bearing, boreRadius(BEARING_BORES[BearingBore.ROUNDED_HEX][BearingSize.IN_1_2]), true);
    // Its flange's underside at the origin, the rest of it below
    expectSamePoint(points[0], vector(0 * meter, (0.063 - 0.313) * inch));
    expectSamePoint(points[7], vector(1.225 / 2 * inch, 0 * meter));
}

export function testExtendedInnerRaceStandsOutOfTheOuterRace()
{
    const bearing = BEARINGS[BearingVendor.GENERIC][BearingType.RADIAL][BearingBore.ROUND][BearingSize.NO_10][BearingDimensions.OD_0_375_X_0_156_EXTENDED][BearingSeal.SHIELDED];
    const points = bearingProfile(bearing, boreRadius(BEARING_BORES[BearingBore.ROUND][BearingSize.NO_10]), false);
    // The outer race, 0.125 in wide, centered on the inner race's 0.156 in
    expectSamePoint(points[5], vector(0.375 / 2 * inch, -0.1405 * inch));
    expectSamePoint(points[6], vector(0.375 / 2 * inch, -0.0155 * inch));
    expectSamePoint(points[size(points) - 3], vector(points[1][0], 0 * meter));
}

export function testHiddenChoicesResolveToTheOneMade()
{
    // WCP makes one 3/8 in rounded hex flanged bearing: a hidden Dimensions holding another stands for it
    var definition = {
            "vendor" : BearingVendor.WCP,
            "bearingType" : BearingType.FLANGED,
            "bore" : BearingBore.ROUNDED_HEX,
            "size" : BearingSize.IN_3_8,
            "dimensions" : BearingDimensions.OD_1_125_X_0_313,
            "seal" : BearingSeal.SEALED
        };
    const choices = resolveChoices(BEARINGS, definition, ["vendor", "bearingType", "bore", "size", "dimensions", "seal"]);
    expectEqual(choices.definition.dimensions, BearingDimensions.OD_0_875_X_0_281);
    expectEqual(choices.definition.seal, BearingSeal.SHIELDED);
    expectEqual(choices.made.partNumber, "WCP-0784");
    // Where several are made, one that isn't is an error
    definition.size = BearingSize.IN_1_2;
    definition.dimensions = BearingDimensions.OD_1_375_X_0_438;
    expectEqual(resolveChoices(BEARINGS, definition, ["vendor", "bearingType", "bore", "size", "dimensions", "seal"]).unmade, "dimensions");
}

function expectSamePoint(actual is Vector, expected is Vector)
{
    expectNear(norm(actual - expected), 0 * meter, 1e-9 * meter);
}
