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
            "hexType" : HexType.ROUNDED_HEX,
            "hexSize" : HexSize._7_MM,
            "endBound" : StockBoundingType.UP_TO_SURFACE,
            "depth" : 1 * inch,
            "symmetric" : true,
            "mirrorShaft" : true,
            "firstEndOperation" : EndOperation.TAPPED_HOLE,
            "secondEndOperation" : SecondEndOperation.TAPPED_HOLE,
            "firstEndMetricOperation" : MetricEndOperation.TAPPED_HOLE,
            "secondEndMetricOperation" : MetricSecondEndOperation.TAPPED_HOLE,
            "firstEndOverrideGroove" : false,
            "secondEndOverrideGroove" : false,
            "firstEndTappedHolePath" : { "size" : "M4", "pitch" : "0.70 mm (Coarse)" } as LookupTablePath,
            "secondEndTappedHolePath" : { "size" : "M4", "pitch" : "0.70 mm (Coarse)" } as LookupTablePath
        };
}

/**
 * A custom 12mm REX shaft (FTC) with an e-clip on its first end, `override` its groove diameter or not.
 */
function eClipShaft(overrideGroove is boolean) returns map
{
    var definition = ftcShaft(ShaftSource.CUSTOM);
    definition.hexSize = HexSize._11_MM;
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

/**
 * goBILDA's 8mm REX with an e-clip, at a Custom length: the shortest length it's sold in that it fits in, cut down.
 */
function customLengthShaft() returns map
{
    var definition = ftcShaft(ShaftSource.COTS);
    definition.ftcShaft = { "vendor" : "goBILDA", "type" : "8 mm REX (e-clip)", "length" : "Custom" } as LookupTablePath;
    return definition;
}

export function testCustomLengthIsCutDownWithItsFactoryEnd()
{
    const definition = robotShaftEditLogic(newContext(), newId(), customLengthShaft(), customLengthShaft(), false, {}, qNothing());
    expectTrue(!definition.presetShaft, "a Custom length isn't a preset");
    expectTrue(definition.factoryEnd, "a Custom length keeps its factory end");
    expectTrue(definition.factoryGrooved, "its factory end has its grooves");
    // Its length is its own, its profile is the shaft's, and it isn't mirrored (both ends would be factory ends)
    expectEqual(definition.endBound, StockBoundingType.UP_TO_SURFACE);
    expectEqual(definition.mirrorShaft, false);
    expectEqual(definition.hexSize, HexSize._7_MM);
    expectEqual(definition.hexType, HexType.ROUNDED_HEX);
    const entry = getLookupTable(ftcShaftTable, customLengthShaft().ftcShaft);
    expectEqual(entry.presetLength, undefined);
    expectEqual(entry.factoryHole.size, "M4");
    expectEqual(size(entry.factoryGrooves), 1);
    expectEqual(entry.partName, "8mm REX Shaft with E-Clip (goBILDA)");
    // Every length it's sold in, shortest first
    expectEqual(size(entry.stock), 28);
    expectEqual(entry.stock[0].length, 24 * millimeter);
    expectEqual(entry.stock[27].partNumber, "2106-4008-6240");
}

export function testCustomLengthWithoutFactoryFeaturesIsCutFromStock()
{
    // goBILDA's plain stainless 12mm REX has no holes or grooves: both ends can be modified, and it can be mirrored
    var plain = customLengthShaft();
    plain.ftcShaft = { "vendor" : "goBILDA", "type" : "12 mm REX", "material" : "Stainless steel", "length" : "Custom" } as LookupTablePath;
    const definition = robotShaftEditLogic(newContext(), newId(), plain, plain, false, {}, qNothing());
    expectTrue(!definition.presetShaft, "a Custom length isn't a preset");
    expectTrue(!definition.factoryEnd, "it has no factory features");
    expectEqual(definition.mirrorShaft, true);
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

/**
 * A custom hex shaft `hexSize` across, with tapped ends from the hole table's `path`.
 */
function tappedShaft(hexSize is HexSize, path is LookupTablePath) returns map
{
    var definition = ringShaft(false);
    definition.hexType = HexType.ROUNDED_HEX;
    definition.hexSize = hexSize;
    definition.firstEndOperation = EndOperation.TAPPED_HOLE;
    definition.firstEndMetricOperation = MetricEndOperation.TAPPED_HOLE;
    definition.secondEndOperation = SecondEndOperation.TAPPED_HOLE;
    definition.secondEndMetricOperation = MetricSecondEndOperation.TAPPED_HOLE;
    definition.firstEndTappedHolePath = path;
    definition.secondEndTappedHolePath = path;
    return definition;
}

const TEN_32 = { "size" : "#10", "pitch" : "32 tpi (UNF)" } as LookupTablePath;
const M3 = { "size" : "M3", "pitch" : "0.50 mm (Coarse)" } as LookupTablePath;
const M4 = { "size" : "M4", "pitch" : "0.70 mm (Coarse)" } as LookupTablePath;

export function testMetricTappedHolesAreStdsIsoSizes()
{
    expectEqual(getLookupTable(tappedHoleTable, M3).tapDrillDiameter, "2.5 mm");
    expectEqual(getLookupTable(tappedHoleTable, M4).tapDrillDiameter, "3.3 mm");
    expectEqual(getLookupTable(tappedHoleTable, M4).majorDiameter, 4 * millimeter);
    expectTrue(getLookupTable(tappedHoleTable, M4).metric, "M4 is in std's ISO table");
    expectTrue(!getLookupTable(tappedHoleTable, TEN_32).metric, "#10-32 is in std's ANSI table");
}

export function testMetricHexShaftsAreTappedM4()
{
    // A new 8mm REX shaft (from the inch default), and one changed from 1/2 in. hex
    const created = robotShaftEditLogic(newContext(), newId(), {}, tappedShaft(HexSize._7_MM, TEN_32), false, {}, qNothing());
    expectEqual(created.firstEndTappedHolePath, M4);
    expectEqual(created.firstEndHoleDiameter, "3.3 mm");
    const changed = robotShaftEditLogic(newContext(), newId(), tappedShaft(HexSize._1_2_IN, TEN_32), tappedShaft(HexSize._11_MM, TEN_32), false, {}, qNothing());
    expectEqual(changed.secondEndTappedHolePath, M4);
}

export function testTappedHoleKeepsItsSystemsChoice()
{
    // M3 stays from 8mm to 12mm REX; back to inch hex, it's #10-32
    const metric = robotShaftEditLogic(newContext(), newId(), tappedShaft(HexSize._7_MM, M3), tappedShaft(HexSize._11_MM, M3), false, {}, qNothing());
    expectEqual(metric.firstEndTappedHolePath, M3);
    const inch = robotShaftEditLogic(newContext(), newId(), tappedShaft(HexSize._11_MM, M3), tappedShaft(HexSize._1_2_IN, M3), false, {}, qNothing());
    expectEqual(inch.firstEndTappedHolePath, TEN_32);
    expectEqual(inch.firstEndHoleDiameter, "0.1590 in");
}
