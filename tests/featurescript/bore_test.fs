FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/bore.fs", version : "");
import(path : "core/boreProfiles.gen.fs", version : "");
import(path : "testing.fs", version : "");

/**
 * A bore's definition, as `borePredicate` declares it.
 */
function bore(shape is BoreShape, settings is map) returns map
{
    return mergeMaps({
                "addBore" : true,
                "boreShape" : shape,
                "boreHexSize" : BoreHexSize._1_2_IN,
                "hexWidth" : 0.25 * inch,
                "boreDiameter" : 0.25 * inch,
                "splineType" : SplineType.SPLINE_XS,
                "fit" : Fit.FREE,
                "fitClearance" : 0.1 * millimeter,
                "entranceChamfer" : false,
                "chamferDistance" : 0.5 * millimeter
            }, settings);
}

export function testSplineXsBoreClearancesAreTheStandardFits()
{
    for (var fit in [Fit.FREE, Fit.CLOSE, Fit.NONE])
    {
        expectNear(SPLINE_XS_BORE_CLEARANCES[fit], standardFitClearance(fit, undefined, splineDiameter(SplineType.SPLINE_XS)), 1e-9 * meter);
    }
}

export function testStandardHexesHaveAFit()
{
    const half = definitionBore(bore(BoreShape.HEX, {}));
    expectEqual(half.across, 0.5 * inch);
    expectNear(half.clearance, standardFitClearance(Fit.FREE, undefined, 0.5 * inch), 1e-12 * meter);
    const threeEighths = definitionBore(bore(BoreShape.HEX, { "boreHexSize" : BoreHexSize._3_8_IN, "fit" : Fit.CLOSE }));
    expectEqual(threeEighths.across, 0.375 * inch);
    expectNear(threeEighths.clearance, standardFitClearance(Fit.CLOSE, undefined, 0.375 * inch), 1e-12 * meter);
}

export function testCustomHexesAndRoundBoresAreTheirSize()
{
    // Whatever the fit (which isn't shown)
    const hex = definitionBore(bore(BoreShape.HEX, { "boreHexSize" : BoreHexSize.CUSTOM }));
    expectEqual(hex.across, 0.25 * inch);
    expectEqual(hex.clearance, 0 * meter);
    expectEqual(hex.fit, Fit.NONE);
    const round = definitionBore(bore(BoreShape.ROUND, { "fit" : Fit.CUSTOM }));
    expectEqual(round.clearance, 0 * meter);
    expectEqual(round.fit, Fit.NONE);
}

export function testSplinesHaveAFit()
{
    const spline = definitionBore(bore(BoreShape.SPLINE, { "fit" : Fit.CLOSE }));
    expectEqual(spline.fit, Fit.CLOSE);
    expectNear(spline.clearance, SPLINE_XS_BORE_CLEARANCES[Fit.CLOSE], 1e-9 * meter);
}

export function testNoBore()
{
    expectEqual(definitionBore(bore(BoreShape.HEX, { "addBore" : false })), undefined);
}

export function testMateBoreIsItsOwn()
{
    // The mating gear's own parameters, not the gear's: a 3/8 in. hex with a close fit and a chamfer, beside a gear's
    // 1/2 in. hex with none
    const definition = mergeMaps(bore(BoreShape.HEX, {}), {
                "mateAddBore" : true,
                "mateBoreShape" : BoreShape.HEX,
                "mateBoreHexSize" : BoreHexSize._3_8_IN,
                "mateHexWidth" : 0.3 * inch,
                "mateBoreDiameter" : 0.3 * inch,
                "mateSplineType" : SplineType.SPLINE_XS,
                "mateFit" : Fit.CLOSE,
                "mateFitClearance" : 0.2 * millimeter,
                "mateEntranceChamfer" : true,
                "mateChamferDistance" : 0.4 * millimeter
            });
    const mate = definitionMateBore(definition);
    expectEqual(mate.across, 0.375 * inch);
    expectNear(mate.clearance, standardFitClearance(Fit.CLOSE, undefined, 0.375 * inch), 1e-12 * meter);
    expectEqual(mate.chamfer, 0.4 * millimeter);
    // Its errors highlight its own parameters
    expectEqual(mate.parameters, MATE_BORE_PARAMETERS);
    expectEqual(mate.chamferParameters, ["mateChamferDistance"]);
    expectEqual(definitionBore(definition).parameters, BORE_PARAMETERS);
    var none = definition;
    none.mateAddBore = false;
    expectEqual(definitionMateBore(none), undefined);
}
