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
 * goBILDA's 8mm REX shaft, stainless steel (with an e-clip), at `length` ("48 mm", or "Custom").
 */
function rexShaft(length is string) returns map
{
    return {
            "program" : Program.FTC,
            "shaftSource" : ShaftSource.COTS,
            "ftcShaft" : { "vendor" : "goBILDA", "type" : "REX", "size" : "8 mm", "material" : "Stainless steel", "length" : length } as LookupTablePath,
            "endBound" : StockBoundingType.UP_TO_SURFACE,
            "depth" : 1 * inch,
            "symmetric" : true,
            "mirrorShaft" : true,
            "firstEndOperation" : EndOperation.TAPPED_HOLE,
            "secondEndOperation" : SecondEndOperation.TAPPED_HOLE,
            "firstEndOverrideGroove" : false,
            "secondEndOverrideGroove" : false
        };
}

export function testPresetLengthIsExtrudedThatLong()
{
    const definition = robotShaftEditLogic(newContext(), newId(), rexShaft("48 mm"), rexShaft("48 mm"), false, {}, qNothing());
    expectEqual(definition.endBound, StockBoundingType.BLIND);
    expectEqual(definition.depth, 48 * millimeter);
    expectEqual(definition.symmetric, false);
    expectEqual(definition.mirrorShaft, false);
    expectEqual(definition.hexSize, HexSize._7_MM);
}

export function testCustomLengthKeepsItsExtrude()
{
    const definition = robotShaftEditLogic(newContext(), newId(), rexShaft("Custom"), rexShaft("Custom"), false, {}, qNothing());
    expectEqual(definition.endBound, StockBoundingType.UP_TO_SURFACE);
    expectEqual(definition.depth, 1 * inch);
    expectEqual(definition.mirrorShaft, true);
}

export function testPresetsHaveTheirFactoryFeatures()
{
    const rex8 = getLookupTable(ftcShaftTable, rexShaft("48 mm").ftcShaft);
    expectEqual(rex8.factoryHoleDepth, 11 * millimeter);
    expectEqual(size(rex8.factoryGrooves), 1);
    expectEqual(rex8.stock[0].partNumber, "2106-4008-0480");
    const plain = getLookupTable(ftcShaftTable, { "vendor" : "goBILDA", "type" : "REX", "size" : "12 mm", "material" : "Stainless steel, no e-clip", "length" : "140 mm" } as LookupTablePath);
    expectEqual(plain.factoryHoleDepth, undefined);
    expectEqual(plain.stock[0].partNumber, "2102-0012-0140");
}
