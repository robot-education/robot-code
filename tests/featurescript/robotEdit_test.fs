FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "edit/robotEdit.fs", version : "");
import(path : "testing.fs", version : "");

/**
 * Modify hole, making a `holeType` hole: a #10 tapped hole (32 tpi) or free clearance hole, and a 1/4 in. hole.
 */
function modifyHole(holeType is ModifyHoleType) returns map
{
    return {
            "editType" : RobotEditType.MODIFY_HOLE,
            "holeType" : holeType,
            "tappedHolePath" : { "size" : "#10", "pitch" : "32 tpi (UNF)" } as LookupTablePath,
            "clearanceHolePath" : { "size" : "#10", "fit" : "Free" } as LookupTablePath,
            "holeDiameter" : 0.25 * inch
        };
}

function editLogic(oldDefinition is map, definition is map) returns map
{
    return robotEditEditLogic(newContext(), newId(), oldDefinition, definition, false);
}

export function testTappedHoleStartsAtItsTapDrill()
{
    expectNear(editLogic(modifyHole(ModifyHoleType.SIMPLE), modifyHole(ModifyHoleType.TAPPED)).holeDiameter, 0.159 * inch, 1e-6 * inch);
}

export function testClearanceHoleStartsAtItsFit()
{
    expectNear(editLogic(modifyHole(ModifyHoleType.SIMPLE), modifyHole(ModifyHoleType.CLEARANCE)).holeDiameter, 0.201 * inch, 1e-6 * inch);
}

export function testNewFeatureStartsAtTheTable()
{
    expectNear(editLogic({}, modifyHole(ModifyHoleType.TAPPED)).holeDiameter, 0.159 * inch, 1e-6 * inch);
}

export function testChangingThreadSetsItsTapDrill()
{
    var unc = modifyHole(ModifyHoleType.TAPPED);
    unc.tappedHolePath.pitch = "24 tpi (UNC)";
    expectNear(editLogic(modifyHole(ModifyHoleType.TAPPED), unc).holeDiameter, 0.1495 * inch, 1e-6 * inch);
}

export function testOverriddenDiameterIsKept()
{
    // The table's diameter didn't change, so what's typed stays
    expectEqual(editLogic(modifyHole(ModifyHoleType.TAPPED), modifyHole(ModifyHoleType.TAPPED)).holeDiameter, 0.25 * inch);
}

export function testSimpleHoleKeepsItsDiameter()
{
    expectEqual(editLogic(modifyHole(ModifyHoleType.TAPPED), modifyHole(ModifyHoleType.SIMPLE)).holeDiameter, 0.25 * inch);
}

export function testTappedHoleSpecHasItsThread()
{
    const spec = getHoleSpec(modifyHole(ModifyHoleType.TAPPED));
    expectEqual(spec.holeDiameter, 0.25 * inch);
    expectEqual(spec.size, "#10");
    expectEqual(spec.thread.pitch, "32 tpi (UNF)");
    expectNear(spec.thread.majorDiameter, 0.19 * inch, 1e-6 * inch);
}

export function testClearanceHoleSpecHasItsSize()
{
    const spec = getHoleSpec(modifyHole(ModifyHoleType.CLEARANCE));
    expectEqual(spec.size, "#10");
    expectEqual(spec.thread, undefined);
}

export function testSimpleHoleSpecIsJustItsDiameter()
{
    expectEqual(getHoleSpec(modifyHole(ModifyHoleType.SIMPLE)), { "holeDiameter" : 0.25 * inch });
}

export function testSameCylinderAnywhereAlongItsAxis()
{
    const a = cylinder(coordSystem(vector(0, 0, 0) * inch, vector(1, 0, 0), vector(0, 0, 1)), 0.1 * inch);
    const b = cylinder(coordSystem(vector(0, 0, 2) * inch, vector(0, 1, 0), vector(0, 0, -1)), 0.1 * inch);
    expectTrue(isSameCylinder(a, b), "Faces of one cylinder are the same");
}

export function testOffsetCylinderIsntTheSame()
{
    const a = cylinder(coordSystem(vector(0, 0, 0) * inch, vector(1, 0, 0), vector(0, 0, 1)), 0.1 * inch);
    const b = cylinder(coordSystem(vector(0.5, 0, 0) * inch, vector(1, 0, 0), vector(0, 0, 1)), 0.1 * inch);
    expectTrue(!isSameCylinder(a, b), "Parallel cylinders are different");
    const bigger = cylinder(coordSystem(vector(0, 0, 0) * inch, vector(1, 0, 0), vector(0, 0, 1)), 0.2 * inch);
    expectTrue(!isSameCylinder(a, bigger), "Bigger cylinders are different");
}

export function testBottomIsWithinRadius()
{
    // A drill point's cone, under a 0.1 in. radius hole
    expectTrue(isWithinRadius(box3d(vector(-0.1, -0.1, 0.5) * inch, vector(0.1, 0.1, 0.56) * inch), 0.1 * inch), "A bottom is within the radius");
    // A face the hole goes through
    expectTrue(!isWithinRadius(box3d(vector(-1, -0.5, 0) * inch, vector(1, 0.5, 0) * inch), 0.1 * inch), "An entrance is not");
}
