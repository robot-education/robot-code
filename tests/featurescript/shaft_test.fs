FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/shaft/robotShaft.fs", version : "");
import(path : "released/shaft/robotShaftTables.gen.fs", version : "");
import(path : "testing.fs", version : "");

/**
 * A custom 1/2 in. hex shaft with retaining rings on both ends, `override` their groove diameters or not.
 */
function ringShaft(overrideGroove is boolean) returns map
{
    return {
            "program" : Program.FRC,
            "shaftSource" : ShaftSource.CUSTOM,
            "shaftType" : ShaftType.HEX,
            "hexSize" : HexSize._1_2_IN,
            "firstEndOperation" : EndOperation.RETAINING_RING,
            "secondEndOperation" : SecondEndOperation.RETAINING_RING,
            "firstEndSideMount" : false,
            "secondEndSideMount" : true,
            "firstEndOverrideGroove" : overrideGroove,
            "secondEndOverrideGroove" : overrideGroove,
            "firstEndGrooveDiameter" : 0.25 * inch,
            "secondEndGrooveDiameter" : 0.25 * inch
        };
}

export function testGrooveOverrideStartsAtTheUsualGroove()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(false), ringShaft(true), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, "0.39 in");
    expectEqual(definition.secondEndGrooveDiameter, "0.396 in");
}

export function testGrooveOverrideKeepsItsValue()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(true), ringShaft(true), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, 0.25 * inch);
}

export function testGrooveOverrideOffLeavesIt()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(false), ringShaft(false), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, 0.25 * inch);
}

/**
 * An FTC shaft: goBILDA's 8mm REX with an e-clip, 48 mm long, or a custom one.
 */
function ftcShaft(source is ShaftSource) returns map
{
    return {
            "program" : Program.FTC,
            "shaftSource" : source,
            "ftcShaft" : { "vendor" : "goBILDA", "type" : "8 mm REX (e-clip)", "length" : "48 mm" } as LookupTablePath,
            "shaftType" : ShaftType.HEX,
            "hexSize" : HexSize._7_MM,
            "ftcCustomShaft" : FtcCustomShaft.REX_8_MM,
            "endBound" : StockBoundingType.UP_TO_SURFACE,
            "depth" : 1 * inch,
            "symmetric" : true,
            "mirrorShaft" : true,
            "firstEndOperation" : EndOperation.TAPPED_HOLE,
            "secondEndOperation" : SecondEndOperation.TAPPED_HOLE,
            "firstEndMetricOperation" : MetricEndOperation.TAPPED_HOLE,
            "secondEndMetricOperation" : MetricSecondEndOperation.TAPPED_HOLE,
            "firstEndOverrideGroove" : false,
            "secondEndOverrideGroove" : false
        };
}

/**
 * A custom 12mm REX shaft (FTC) with an e-clip on its first end, `override` its groove diameter or not.
 */
function eClipShaft(overrideGroove is boolean) returns map
{
    var definition = ftcShaft(ShaftSource.CUSTOM);
    definition.ftcCustomShaft = FtcCustomShaft.REX_12_MM;
    definition.firstEndMetricOperation = MetricEndOperation.E_CLIP;
    definition.firstEndSideMount = false;
    definition.secondEndSideMount = false;
    definition.firstEndOverrideGroove = overrideGroove;
    definition.firstEndGrooveDiameter = 5 * millimeter;
    return definition;
}

export function testEClipGrooveOverrideStartsAtGoBildasGroove()
{
    const definition = robotShaftEditLogic(newContext(), newId(), eClipShaft(false), eClipShaft(true), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, "9 mm");
}

export function testPresetLengthIsExtrudedThatLong()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ftcShaft(ShaftSource.COTS), ftcShaft(ShaftSource.COTS), false, {}, qNothing());
    expectEqual(definition.endBound, StockBoundingType.BLIND);
    expectEqual(definition.depth, 48 * millimeter);
    expectEqual(definition.symmetric, false);
    expectEqual(definition.mirrorShaft, false);
}

export function testCustomShaftKeepsItsExtrude()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ftcShaft(ShaftSource.CUSTOM), ftcShaft(ShaftSource.CUSTOM), false, {}, qNothing());
    expectEqual(definition.endBound, StockBoundingType.UP_TO_SURFACE);
    expectEqual(definition.depth, 1 * inch);
    expectEqual(definition.mirrorShaft, true);
}

export function testPresetsHaveTheirFactoryFeatures()
{
    const eClip = getLookupTable(ftcShaftTable, ftcShaft(ShaftSource.COTS).ftcShaft);
    expectEqual(eClip.factoryHole.size, "M4");
    expectEqual(eClip.factoryHole.depth, 11 * millimeter);
    expectEqual(size(eClip.factoryGrooves), 1);
    expectEqual(eClip.stock, [{ "length" : 48 * millimeter, "partNumber" : "2106-4008-0480", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-48mm-length/" }]);
    const plain = getLookupTable(ftcShaftTable, { "vendor" : "goBILDA", "type" : "12 mm REX", "material" : "Stainless steel", "length" : "140 mm" } as LookupTablePath);
    expectEqual(plain.factoryHole, undefined);
    expectEqual(plain.factoryGrooves, []);
    expectEqual(plain.stock[0].partNumber, "2102-0012-0140");
    const robits = getLookupTable(ftcShaftTable, { "vendor" : "AndyMark", "type" : "3/8 in. Robits hex", "length" : "4 in." } as LookupTablePath);
    expectEqual(robits.factoryHole.size, "#10");
    expectEqual(robits.factoryHole.depth, 0.7 * inch);
    expectEqual(robits.presetLength, 4 * inch);
}

export function testCustomFtcShaftIsItsProfile()
{
    var custom = ftcShaft(ShaftSource.CUSTOM);
    custom.ftcCustomShaft = FtcCustomShaft.ROBITS_HEX;
    const definition = robotShaftEditLogic(newContext(), newId(), custom, custom, false, {}, qNothing());
    expectEqual(definition.shaftType, ShaftType.HEX);
    expectEqual(definition.hexType, HexType.STOCK);
    expectEqual(definition.hexSize, HexSize._3_8_IN);
}

export function testEditingLogicSetsPresetAndGrooves()
{
    // The precondition can't read the table's levels, so editing logic sets these from the entry
    const eClip = robotShaftEditLogic(newContext(), newId(), ftcShaft(ShaftSource.COTS), ftcShaft(ShaftSource.COTS), false, {}, qNothing());
    expectTrue(eClip.presetShaft, "an e-clip REX shaft is sold in set lengths");
    expectTrue(eClip.factoryGrooved, "an e-clip REX shaft has its grooves");
    var plain = ftcShaft(ShaftSource.COTS);
    plain.ftcShaft = { "vendor" : "goBILDA", "type" : "12 mm REX", "material" : "Stainless steel", "length" : "100 mm" } as LookupTablePath;
    const rex = robotShaftEditLogic(newContext(), newId(), plain, plain, false, {}, qNothing());
    expectTrue(rex.presetShaft, "a REX shaft is sold in set lengths");
    expectTrue(!rex.factoryGrooved, "without an e-clip, it has no grooves");
}
