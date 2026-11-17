FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/belt/robotBelt.fs", version : "");
import(path : "released/belt/robotPulleyCommon.fs", version : "");
import(path : "testing.fs", version : "");

function beltDefinition(teeth is string) returns map
{
    return {
            "program" : Program.FTC,
            "beltMode" : BeltMode.SIMPLE,
            // Selections can't be evaluated here: a standalone belt has none
            "isStandaloneBelt" : true,
            "openBelt" : false,
            "isDoubleSidedBelt" : false,
            // goBILDA's 2mm GT2 belts come in one width, so they have no width to choose
            "ftcBeltPath" : { "supplier" : "goBILDA", "beltType" : "2mm GT2", "teeth" : teeth } as LookupTablePath,
            "beltTeeth" : 0,
            "isCustomBelt" : false,
            "useClosestBelt" : false,
            "customBeltType" : CustomBeltType._3_MM_HTD,
            "customBeltWidth" : 20 * millimeter
        };
}

export function testGoBildaGt2Belts()
{
    const value = getBeltValue(getBeltTableAndPath(beltDefinition("44T")));
    expectEqual(value.beltWidth, 6 * millimeter);
    expectEqual(value.beltTeeth, 44);
    const options = getCurrentBeltOptionsArray({ "table" : ftcBeltTable, "path" : beltDefinition("44T").ftcBeltPath });
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

export function testCustomBelt()
{
    // Custom: a belt of the feature's own type, width, and teeth
    var definition = beltDefinition("44T");
    definition.ftcBeltPath = { "supplier" : "Custom" } as LookupTablePath;
    definition.beltTeeth = 123;
    expectTrue(isCustomBeltChosen(definition), "Custom should be custom");
    expectTrue(!isCustomBeltChosen(beltDefinition("44T")), "goBILDA's shouldn't be custom");
    const value = getBeltValue(getBeltTableAndPath(definition));
    expectEqual(value.beltType, BeltType._3_MM_HTD);
    expectEqual(value.beltWidth, 20 * millimeter);
    expectEqual(value.beltTeeth, 123);
    // Editing logic shows its parameters
    expectTrue(robotBeltEditLogic(newContext(), newId(), definition, definition, false, {}, qNothing(), "").isCustomBelt,
        "editing logic should set Custom belt");
    // An open belt's custom by its width
    definition.beltMode = BeltMode.COMPLEX;
    definition.openBelt = true;
    definition.ftcBeltTypePath = { "beltType" : "5mm HTD", "width" : "Custom" } as LookupTablePath;
    expectTrue(isCustomBeltChosen(definition), "an open belt of a Custom width should be custom");
    expectEqual(getBeltValue(getBeltTableAndPath(definition)).beltType, BeltType._5_MM_HTD);
}

export function testSuppliersByProgram()
{
    // FRC's and FTC's own suppliers, after Custom, with the most common by default
    expectEqual(frcBeltTable["default"], "WCP");
    expectEqual(ftcBeltTable["default"], "goBILDA");
    expectEqual(frcBeltTable.entries.goBILDA, undefined);
    expectEqual(ftcBeltTable.entries.WCP, undefined);
    expectEqual(ftcBeltTable.entries.REV.entries["3mm GT2"] != undefined, true);
    expectEqual(frcBeltTable.entries.REV.entries.RT25 != undefined, true);
}

export function testRubberCoatedBelts()
{
    var definition = beltDefinition("44T");
    definition.program = Program.FRC;
    definition.frcBeltPath = { "supplier" : "WCP", "beltType" : "5mm HTD", "width" : "15 mm rubber coated", "teeth" : "100T" } as LookupTablePath;
    const value = getBeltValue(getBeltTableAndPath(definition));
    expectEqual(value.beltWidth, 15 * millimeter);
    // Its back's thicker than a plain belt's
    expectEqual(getBeltOutsideThickness(value), 4 * millimeter);
    definition.frcBeltPath.width = "15 mm";
    expectNear(getBeltOutsideThickness(getBeltValue(getBeltTableAndPath(definition))), 1.16 * millimeter, 1e-12 * meter);
}

export function testWcp44TBelts()
{
    for (var width in ["9 mm", "15 mm"])
    {
        const path = { "supplier" : "WCP", "beltType" : "5mm HTD", "width" : width, "teeth" : "44T" } as LookupTablePath;
        expectTrue(isIn(44, getCurrentBeltOptionsArray({ "table" : frcBeltTable, "path" : path })), "WCP sells 44T " ~ width ~ " belts");
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

export function testClosestBeltsSizes()
{
    // A simple belt using the closest belt chooses its size (no teeth), and its belts are the full table's there
    var definition = beltDefinition("44T");
    definition.isStandaloneBelt = false;
    definition.useClosestBelt = true;
    definition.program = Program.FRC;
    definition.frcBeltSizePath = { "supplier" : "WCP", "beltType" : "5mm HTD", "width" : "15 mm" } as LookupTablePath;
    expectEqual(getBeltPathName(definition), "frcBeltSizePath");
    const tableAndPath = getBeltTableAndPath(definition);
    expectEqual(getBeltValue(tableAndPath).beltWidth, 15 * millimeter);
    const options = getCurrentBeltOptionsArray(tableAndPath);
    expectTrue(isIn(44, options) && !isIn(43, options), "WCP's 15 mm belts come in 44T, not 43T");
    // One width: straight to its teeth
    definition.frcBeltSizePath = { "supplier" : "REV", "beltType" : "RT25" } as LookupTablePath;
    expectEqual(min(getCurrentBeltOptionsArray(getBeltTableAndPath(definition))), 32);
    // Custom: any teeth
    definition.frcBeltSizePath = { "supplier" : "Custom" } as LookupTablePath;
    expectEqual(getCurrentBeltOptionsArray(getBeltTableAndPath(definition)), undefined);
}

export function testClickingAPulleysPointChoosesIt()
{
    var definition = beltDefinition("44T");
    definition.startOffset = false;
    definition.movePulleyNumber = 1;
    definition.pulleys = [];
    // The points manipulator's third point: pulley 3
    const changed = robotBeltManipulatorChange(newContext(), definition, { "movePulleyManipulator" : { "index" : 2 } });
    expectEqual(changed.movePulleyNumber, 3);
}
