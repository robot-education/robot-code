FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/belt/robotBelt.fs", version : "");
import(path : "released/belt/robotPulleyCommon.fs", version : "");
import(path : "testing.fs", version : "");

function beltDefinition(teeth is string) returns map
{
    return {
            "beltMode" : BeltMode.SIMPLE,
            "isDoubleSidedBelt" : false,
            "beltPath" : { "beltType" : "2mm GT2", "supplier" : "goBILDA", "teeth" : teeth } as LookupTablePath,
            "beltTeeth" : 0
        };
}

export function testGoBildaGt2Belts()
{
    const options = getCurrentBeltOptionsArray({ "table" : beltTable, "path" : beltDefinition("44T").beltPath });
    expectEqual(size(options), 22);
    // In the table's key order ("108T" before "44T"), as maps are sorted
    expectEqual(min(options), 44);
    expectEqual(max(options), 324);
}

export function testChoosingABeltSetsItsTeeth()
{
    const definition = robotBeltEditLogic(newContext(), newId(), beltDefinition("44T"), beltDefinition("92T"), false, "");
    expectEqual(definition.beltTeeth, 92);
}

export function testPulleyNeedsEnoughTeeth()
{
    expectContains(expectThrows(function()
            {
                sketchPulleyProfile(newContext(), newId() + "profile", XY_PLANE, BeltType._3_MM_GT2, 10, 0 * meter);
            }), "needs more teeth");
}

export function testPulleyProfileTooFarOffset()
{
    expectContains(expectThrows(function()
            {
                sketchPulleyProfile(newContext(), newId() + "profile", XY_PLANE, BeltType._2_MM_GT2, 20, 1 * millimeter);
            }), "profile offset is too large");
}
