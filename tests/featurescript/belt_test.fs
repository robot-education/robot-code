FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/belt/robotBelt.fs", version : "");
import(path : "released/belt/robotPulleyCommon.fs", version : "");
import(path : "testing.fs", version : "");

function beltDefinition(teeth is string) returns map
{
    return {
            "beltMode" : BeltMode.SIMPLE,
            // Selections can't be evaluated here: a standalone belt has none
            "isStandaloneBelt" : true,
            "isDoubleSidedBelt" : false,
            "beltPath" : { "beltType" : "2mm GT2", "width" : "6 mm", "supplier" : "goBILDA", "teeth" : teeth } as LookupTablePath,
            "beltTeeth" : 0,
            "isCustomBeltWidth" : false,
            "customBeltWidth" : 20 * millimeter
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
    const definition = robotBeltEditLogic(newContext(), newId(), beltDefinition("44T"), beltDefinition("92T"), false, {}, qNothing(), "");
    expectEqual(definition.beltTeeth, 92);
}

export function testCustomBeltWidth()
{
    var definition = beltDefinition("44T");
    definition.beltPath = { "beltType" : "5mm HTD", "width" : "Custom", "supplier" : "Custom" } as LookupTablePath;
    expectTrue(isCustomWidthBelt(definition), "a Custom width should be custom");
    expectTrue(!isCustomWidthBelt(beltDefinition("44T")), "6 mm shouldn't be custom");
    const value = getBeltValue(getBeltTableAndPath(definition));
    expectEqual(value.beltWidth, 20 * millimeter);
    expectEqual(value.beltType, BeltType._5_MM_HTD);
    // Editing logic shows Belt width
    expectTrue(robotBeltEditLogic(newContext(), newId(), definition, definition, false, {}, qNothing(), "").isCustomBeltWidth,
        "editing logic should set Custom belt width");
}

export function testRubberCoatedBelts()
{
    var definition = beltDefinition("44T");
    definition.beltPath = { "beltType" : "5mm HTD", "width" : "15 mm rubber coated", "supplier" : "WCP", "teeth" : "100T" } as LookupTablePath;
    const value = getBeltValue(getBeltTableAndPath(definition));
    expectEqual(value.beltWidth, 15 * millimeter);
    // Its back's thicker than a plain belt's
    expectEqual(getBeltOutsideThickness(value), 4 * millimeter);
    definition.beltPath.width = "15 mm";
    expectNear(getBeltOutsideThickness(getBeltValue(getBeltTableAndPath(definition))), 1.16 * millimeter, 1e-12 * meter);
}

export function testWcp44TBelts()
{
    for (var width in ["9 mm", "15 mm"])
    {
        const path = { "beltType" : "5mm HTD", "width" : width, "supplier" : "WCP", "teeth" : "44T" } as LookupTablePath;
        expectTrue(isIn(44, getCurrentBeltOptionsArray({ "table" : beltTable, "path" : path })), "WCP sells 44T " ~ width ~ " belts");
    }
}

export function testPulleyNeedsEnoughTeeth()
{
    expectContains(expectThrows(function()
            {
                sketchPulleyProfile(newContext(), newId() + "profile", XY_PLANE, BeltType._3_MM_GT2, 5, 0 * meter);
            }), "needs more teeth");
}

export function testPulleyProfileTooFarOffset()
{
    expectContains(expectThrows(function()
            {
                sketchPulleyProfile(newContext(), newId() + "profile", XY_PLANE, BeltType._2_MM_GT2, 20, 1 * millimeter);
            }), "profile offset is too large");
}
