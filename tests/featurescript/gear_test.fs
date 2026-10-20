FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "gear/robotGear.fs", version : "");
import(path : "gear/gearCommon.fs", version : "");
import(path : "core/stdRevolve.fs", version : "");
import(path : "testing.fs", version : "");

const MODULE = inch / 20;

/**
 * Spur gears with `teeth` and `mateTeeth`, as `gearMesh` sees them.
 */
function spur(teeth is number, mateTeeth is number) returns map
{
    return gearMesh({ "gearType" : GearType.GEAR, "centerToCenterAdjustment" : 0 * meter }, MODULE, teeth, mateTeeth);
}

export function testSpurMateIsThePitchRadiiAway()
{
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, spur(20, 30));
    expectNear(norm(mate.origin - vector(25, 0, 0) * MODULE), 0 * meter, 1e-9 * meter);
    expectNear(norm(mate.normal - Z_DIRECTION), 0, 1e-9);
}

export function testCenterToCenterAdjustmentMovesTheMate()
{
    const mesh = gearMesh({ "gearType" : GearType.GEAR, "centerToCenterAdjustment" : 0.003 * inch }, MODULE, 20, 30);
    expectNear(mesh.centerDistance, 25 * MODULE + 0.003 * inch, 1e-12 * meter);
    const mate = gearMatePlane(XY_PLANE, Y_DIRECTION, mesh);
    expectNear(norm(mate.origin - Y_DIRECTION * (25 * MODULE + 0.003 * inch)), 0 * meter, 1e-9 * meter);
    // Its teeth turn as they would without it: it moves straight out
    const plain = gearMatePlane(XY_PLANE, Y_DIRECTION, spur(20, 30));
    expectNear(norm(mate.x - plain.x), 0, 1e-9);
}

export function testSpurMateHasAGapAtTheGearsTooth()
{
    // The gear's first tooth points at its mate, whose x axis (a tooth) is half a tooth (counterclockwise) from pointing back at it
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, spur(20, 20));
    expectNear(norm(mate.x - vector(-cos(9 * degree), -sin(9 * degree), 0)), 0, 1e-9);
}

export function testSpurMateHasAToothAtTheGearsGap()
{
    // Half a tooth around, the gear has a gap toward its mate, so the mate has a tooth pointing back at it
    const toward = vector(cos(9 * degree), sin(9 * degree), 0);
    const mate = gearMatePlane(XY_PLANE, toward, spur(20, 20));
    const back = -toward;
    // A tooth: the mate's x axis is a whole number of teeth (18 degrees) from pointing back
    const turned = atan2(dot(cross(back, mate.x), Z_DIRECTION), dot(back, mate.x));
    expectNear((turned / degree + 360) % 18, 0, 1e-6);
}

export function testEvenSectorsHaveAGapOnTheirAxis()
{
    const sector = gearMesh({ "gearType" : GearType.GEAR, "sector" : true, "sectorTeeth" : 4, "centerToCenterAdjustment" : 0 * meter }, MODULE, 20, 20);
    expectNear(sector.toothPhase, 9 * degree, 1e-9 * degree);
    // So their mate has a tooth pointing back at them
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, sector);
    expectNear(norm(mate.x + X_DIRECTION), 0, 1e-9);
}

export function testBevelMateSharesTheApex()
{
    const pair = gearMesh({ "gearType" : GearType.BEVEL, "shaftAngle" : 90 * degree }, MODULE, 20, 20);
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, pair);
    // 45 degree cones: the apex is a pitch radius above the gear's back, and the mate's back a pitch radius out
    const radius = 10 * MODULE;
    expectNear(norm(mate.origin - vector(radius, 0 * meter, radius)), 0 * meter, 1e-9 * meter);
    expectNear(norm(mate.normal + X_DIRECTION), 0, 1e-9);
}

export function testBevelGearCantBeACrownGear()
{
    expectContains(expectThrows(function()
            {
                bevelPairCones(MODULE, 40, 10, 120 * degree);
            }), "too large for a bevel gear");
}

export function testBevelGearsMateCantBeACrownGear()
{
    expectContains(expectThrows(function()
            {
                bevelPairCones(MODULE, 10, 40, 120 * degree);
            }), "too large for a bevel mating gear");
}

export function testBevelPairConesAddToTheShaftAngle()
{
    const cones = bevelPairCones(MODULE, 15, 45, 80 * degree);
    expectNear(cones.gear.pitchAngle + cones.mate.pitchAngle, 80 * degree, 1e-9 * degree);
}

export function testStraightRackIsOnePiece()
{
    expectEqual(rackPieces({ "helical" : false }, inch), [{ "start" : 0 * meter, "width" : inch, "slant" : 0, "shift" : 0 * meter }]);
}

export function testHelicalRackSlantsThroughItsMiddle()
{
    // Its teeth are where a straight rack's are at the middle of its width, and move along it going across
    const piece = rackPieces({ "helical" : true, "doubleHelical" : false, "helixAngle" : 45 * degree, "hand" : GearHand.LEFT }, inch)[0];
    expectNear(piece.slant, 1, 1e-9);
    expectNear(piece.shift + piece.slant * inch / 2, 0 * meter, 1e-12 * meter);
    // A right hand rack's go back
    const right = rackPieces({ "helical" : true, "doubleHelical" : false, "helixAngle" : 45 * degree, "hand" : GearHand.RIGHT }, inch)[0];
    expectNear(right.slant, -1, 1e-9);
}

export function testDoubleHelicalRackMeetsInTheMiddle()
{
    const pieces = rackPieces({ "helical" : true, "doubleHelical" : true, "helixAngle" : 30 * degree, "hand" : GearHand.RIGHT }, inch);
    expectEqual(size(pieces), 2);
    // The first half ends where the second starts: at the middle, where the teeth are a straight rack's
    expectNear(pieces[0].shift + pieces[0].slant * pieces[0].width, pieces[1].shift, 1e-12 * meter);
    expectNear(pieces[1].shift, 0 * meter, 1e-12 * meter);
    expectNear(pieces[0].slant, -pieces[1].slant, 1e-9);
}

export function testWholeTeethPitchCircles()
{
    // 24 teeth at 20 DP: 1.2 in. across
    expectTrue(isWholeTeeth(0.6 * inch, MODULE), "24 teeth");
    expectTrue(!isWholeTeeth(0.61 * inch, MODULE), "between sizes");
    // Too few teeth for a gear
    expectTrue(!isWholeTeeth(0.05 * inch, MODULE), "2 teeth");
}

export function testRacksHaveNoEditingLogic()
{
    const definition = { "gearType" : GearType.RACK };
    expectEqual(robotGearEditLogic(newContext(), newId(), {}, definition, true, {}, qNothing()), definition);
}

export function testSectorByAngleFitsWholeTeeth()
{
    // 20 teeth: one every 18 degrees, so 100 degrees fits 5, centered in it
    const span = sectorSpan({ "start" : 0 * degree, "end" : 100 * degree }, 20);
    expectEqual(span.teeth, 5);
    expectNear(span.middle, 50 * degree, 1e-9 * degree);
    // Exactly 90 degrees is 5 whole teeth
    expectEqual(sectorSpan({ "start" : -45 * degree, "end" : 45 * degree }, 20).teeth, 5);
}

export function testRevolveAnglesAsStdRevolve()
{
    const base = { "angle" : 90 * degree, "oppositeDirection" : false, "symmetric" : false, "hasStartBound" : false, "angleBack" : 30 * degree };
    expectEqual(revolveAngles(base), { "start" : 0 * degree, "end" : 90 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "oppositeDirection" : true })), { "start" : -90 * degree, "end" : 0 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "symmetric" : true })), { "start" : -45 * degree, "end" : 45 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "hasStartBound" : true })), { "start" : -30 * degree, "end" : 90 * degree });
}

/**
 * A 20 DP bevel gear's definition, meshing with `mateTeeth` at `shaftAngle`, `thickness` thick: Simplified bevels (a
 * spur gear's proportions), or Gleason's.
 */
function bevelDefinition(mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits, simplified is boolean) returns map
{
    return {
            "gearType" : GearType.BEVEL,
            "unitSystem" : UnitSystem.IMPERIAL,
            "pitch" : GearPitch.DP_20,
            "mateTeeth" : mateTeeth,
            "shaftAngle" : shaftAngle,
            "thickness" : thickness,
            "bevelForm" : simplified ? BevelForm.SIMPLIFIED : BevelForm.SPHERICAL_INVOLUTE,
            "addProfileOffset" : false
        };
}

/**
 * A 20 DP bevel gear's shape (see `bevelDefinition`), with `teeth`.
 */
function bevel(teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits, simplified is boolean) returns map
{
    return bevelShape(bevelDefinition(mateTeeth, shaftAngle, thickness, simplified), MODULE, teeth);
}

export function testBevelThicknessRunsFromItsBackToItsTop()
{
    // 45 degree cones: the apex is a pitch radius (half an inch) above the back's pitch circle
    const shape = bevel(20, 20, 90 * degree, 0.25 * inch, true);
    expectNear(shape.apex, 0.5 * inch, 1e-9 * meter);
    // Its flat back is under its teeth's roots, along the back cone
    expectNear(shape.bottom, -1.25 * MODULE * sin(45 * degree), 1e-9 * meter);
    expectNear(shape.top - shape.bottom, 0.25 * inch, 1e-9 * meter);
    // Its teeth run along the pitch cone to its top
    expectNear(shape.faceWidth * cos(45 * degree), shape.top, 1e-9 * meter);
}

export function testGleasonBevelsBackIsUnderItsDeeperRoots()
{
    // A miter pair's teeth are a module each side of the pitch cone, and 0.188 m (and Gleason's allowance) deeper
    const shape = bevel(20, 20, 90 * degree, 0.25 * inch, false);
    expectNear(shape.depths.addendum, MODULE, 1e-12 * meter);
    expectNear(shape.bottom, -(1.188 * MODULE + 0.002 * inch) * sin(45 * degree), 1e-12 * meter);
    expectNear(shape.top - shape.bottom, 0.25 * inch, 1e-9 * meter);
}

export function testBevelGearsPointsAreItsBottomMiddleTopAndApex()
{
    const definition = bevelDefinition(20, 90 * degree, 0.25 * inch, false);
    const shape = bevelShape(definition, MODULE, 20);
    const points = gearPointDistances(definition, MODULE, 20);
    expectEqual(size(points), 4);
    expectNear(points[0], shape.bottom, 1e-12 * meter);
    expectNear(points[1], shape.bottom + 0.125 * inch, 1e-12 * meter);
    expectNear(points[2], shape.top, 1e-12 * meter);
    expectNear(points[3], shape.apex, 1e-12 * meter);
}

export function testSpurGearsPointsStartAtItsBottom()
{
    const points = gearPointDistances({ "gearType" : GearType.GEAR, "thickness" : 0.5 * inch }, MODULE, 24);
    expectEqual(points, vector([-0.25 * inch, 0 * meter, 0.25 * inch]));
    // As `applyPointManipulator` takes them
    expectTrue(isLengthVector(points), "a length vector");
}

export function testBevelGearTooThinForItsTeeth()
{
    for (var simplified in [true, false])
    {
        expectContains(expectThrows(function()
                {
                    bevel(20, 20, 90 * degree, 0.05 * inch, simplified);
                }), "too small for the teeth");
    }
}

export function testBevelGearTooThickReachesItsApex()
{
    for (var simplified in [true, false])
    {
        expectContains(expectThrows(function()
                {
                    bevel(20, 20, 90 * degree, 0.6 * inch, simplified);
                }), "reaching the apex");
    }
}

export function testBevelGearTooThinForItsMatesTeeth()
{
    // The pinion's thick enough for its own steep teeth, but its mate's, nearly flat, need a longer face
    expectContains(expectThrows(function()
            {
                bevel(10, 40, 90 * degree, 0.15 * inch, true);
            }), "too small for the mating gear's teeth");
    bevel(10, 40, 90 * degree, 0.25 * inch, true);
    // By Gleason's proportions, the mate's teeth are shorter (and the pinion's longer)
    expectContains(expectThrows(function()
            {
                bevel(10, 40, 90 * degree, 0.12 * inch, false);
            }), "too small for the mating gear's teeth");
    bevel(10, 40, 90 * degree, 0.15 * inch, false);
}

export function testBevelGearsFaceConeCantBeFlat()
{
    // Its pitch cone's under 90 degrees, but its tips' isn't
    expectContains(expectThrows(function()
            {
                bevel(60, 10, 100 * degree, 0.25 * inch, false);
            }), "too large for a bevel gear");
    expectContains(expectThrows(function()
            {
                bevel(10, 60, 100 * degree, 0.25 * inch, false);
            }), "too large for a bevel mating gear");
}

export function testBevelMateHasTheGearsFaceWidth()
{
    const gear = bevel(10, 40, 90 * degree, 0.25 * inch, false);
    var definition = bevelDefinition(10, 90 * degree, 0.25 * inch, false);
    definition.faceWidth = gear.faceWidth;
    const mate = bevelShape(definition, MODULE, 40);
    expectNear(mate.faceWidth, gear.faceWidth, 1e-12 * meter);
    expectNear(mate.top, gear.faceWidth * cos(mate.cones.gear.pitchAngle), 1e-12 * meter);
}

export function testGleasonAddendaShareTheWorkingDepth()
{
    // At right angles, the gear's is 0.54 m + 0.46 m over the ratio squared, and the pinion's the rest of 2 m
    const pinion = bevelPairCones(MODULE, 13, 31, 90 * degree);
    const small = gleasonProportions(MODULE, 13, 31, pinion.gear.pitchAngle, pinion.mate.pitchAngle, 20 * degree);
    const large = gleasonProportions(MODULE, 31, 13, pinion.mate.pitchAngle, pinion.gear.pitchAngle, 20 * degree);
    const gearAddendum = (0.54 + 0.46 / (31 / 13) ^ 2) * MODULE;
    expectNear(large.addendum, gearAddendum, 1e-12 * meter);
    expectNear(small.addendum, 2 * MODULE - gearAddendum, 1e-12 * meter);
    expectNear(small.mateAddendum, large.addendum, 1e-12 * meter);
    expectNear(small.mateDedendum, large.dedendum, 1e-12 * meter);
    // Whole depth 2.188 m, and Gleason's allowance
    expectNear(small.addendum + small.dedendum, 2.188 * MODULE + 0.002 * inch, 1e-12 * meter);
    // Their thicknesses add up to the circular pitch, the pinion's thicker by the addenda's difference times tan(20)
    expectNear(small.thickness + large.thickness, PI * MODULE, 1e-12 * meter);
    expectNear(small.thickness - large.thickness, 2 * (small.addendum - large.addendum) * tan(20 * degree), 1e-12 * meter);
    // The same teeth share it equally
    const miter = bevelPairCones(MODULE, 20, 20, 90 * degree);
    const even = gleasonProportions(MODULE, 20, 20, miter.gear.pitchAngle, miter.mate.pitchAngle, 20 * degree);
    expectNear(even.addendum, MODULE, 1e-12 * meter);
    expectNear(even.thickness, PI * MODULE / 2, 1e-12 * meter);
}

export function testGleasonRatioAtOtherShaftAngles()
{
    // The equivalent 90 degree ratio, squared: z_gear cos(pinion's pitch angle) / (z_pinion cos(gear's pitch angle))
    const cones = bevelPairCones(MODULE, 15, 30, 60 * degree);
    const large = gleasonProportions(MODULE, 30, 15, cones.mate.pitchAngle, cones.gear.pitchAngle, 20 * degree);
    const ratio = 30 * cos(cones.gear.pitchAngle) / (15 * cos(cones.mate.pitchAngle));
    expectNear(large.addendum, (0.54 + 0.46 / ratio) * MODULE, 1e-12 * meter);
}

export function testGleasonFaceConeIsParallelToItsMatesRootCone()
{
    // Its tips' cone is as far from its pitch cone as its mate's roots' is, so the clearance between them is the same all along
    const definition = bevelDefinition(31, 90 * degree, 0.25 * inch, false);
    const cones = bevelPairCones(MODULE, 13, 31, 90 * degree);
    const depths = bevelDepths(definition, MODULE, 13, cones);
    const mate = gleasonProportions(MODULE, 31, 13, cones.mate.pitchAngle, cones.gear.pitchAngle, 20 * degree);
    expectNear(depths.faceAngle, atan(mate.dedendum / cones.gear.coneDistance), 1e-12 * degree);
    // Simplified, its tips' cone runs to its apex
    var simplified = definition;
    simplified.bevelForm = BevelForm.SIMPLIFIED;
    const spur = bevelDepths(simplified, MODULE, 13, cones);
    expectNear(spur.faceAngle, atan(MODULE / cones.gear.coneDistance), 1e-12 * degree);
    expectNear(spur.dedendum, 1.25 * MODULE, 1e-12 * meter);
}

export function testLowestContactRadius()
{
    // 24T meshing with 24T, module 1 at 20 degrees: its mate's tips cross the line of action 1.74 mm from its base circle
    const base = 12 * millimeter * cos(20 * degree);
    const along = 24 * millimeter * sin(20 * degree) - sqrt((13 * millimeter) ^ 2 - base ^ 2);
    expectNear(lowestContactRadius(12 * millimeter, 12 * millimeter, millimeter, 20 * degree), sqrt(base ^ 2 + along ^ 2), 1e-12 * meter);
    // A mate whose tips reach past the line of action's end interferes there: its base circle
    expectNear(lowestContactRadius(4 * millimeter, 40 * millimeter, 3 * millimeter, 20 * degree), 4 * millimeter * cos(20 * degree), 1e-12 * meter);
}

export function testRootsRadialLinesStayBelowWhereTheMateTouches()
{
    // A 24T gear's full round's line runs up past where its mate touches it; with that, it stops there
    var form = getGearForm(millimeter, 24, 20 * degree, 1, 0 * meter);
    expectTrue(gearRoot(form).lineTop > 11.5 * millimeter, "past it");
    form.contactRadius = 11.4 * millimeter;
    expectNear(gearRoot(form).lineTop, 11.4 * millimeter, 1e-12 * meter);
    expectTrue(gearRoot(form).fullRound, "a full round, which fits below it");
    // Below the base circle, at the base circle; too low for a full round, it's the largest fillet that fits, tangent to
    // the line at its top
    form.contactRadius = 11 * millimeter;
    const root = gearRoot(form);
    expectNear(root.lineTop, form.baseRadius, 1e-12 * meter);
    expectTrue(!root.fullRound && root.filletMiddle != undefined, "a fillet");
    expectNear(norm(root.lineTangent), form.baseRadius, 1e-9 * meter);
}

export function testSphericalInvoluteLeavesItsBaseCone()
{
    expectNear(sphericalInvolute(30 * degree, 30 * degree), 0 * degree, 1e-12 * degree);
    // Out from it, it turns further around, faster and faster
    const near = sphericalInvolute(30 * degree, 32 * degree);
    const far = sphericalInvolute(30 * degree, 34 * degree);
    expectTrue(near > 0 * degree && far > 2 * near, "turns further out");
}

export function testSpansJoinWhereTheyMeet()
{
    expectEqual(joinSpans([{ "start" : 0 * degree, "end" : 90 * degree }, { "start" : 90 * degree, "end" : 180 * degree }]),
        { "full" : false, "start" : 0 * degree, "end" : 180 * degree });
    // Before the first, and a turn around
    const before = joinSpans([{ "start" : 0 * degree, "end" : 90 * degree }, { "start" : 260 * degree, "end" : 360 * degree }]);
    expectNear(before.start, -100 * degree, 1e-9 * degree);
    expectNear(before.end, 90 * degree, 1e-9 * degree);
    // All the way around
    expectEqual(joinSpans([{ "start" : 0 * degree, "end" : 180 * degree }, { "start" : -180 * degree, "end" : 0 * degree }]), { "full" : true });
    // Apart, it's the first's
    expectEqual(joinSpans([{ "start" : 0 * degree, "end" : 10 * degree }, { "start" : 100 * degree, "end" : 110 * degree }]),
        { "full" : false, "start" : 0 * degree, "end" : 10 * degree });
}

export function testPointManipulatorSetsTheIndex()
{
    const changed = robotGearManipulatorChange(newContext(), { "index" : 0 }, { "pointManipulator" : { "index" : 3 } });
    expectEqual(changed.index, 3);
}
