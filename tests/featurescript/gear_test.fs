FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "gear/robotGear.fs", version : "");
import(path : "core/stdRevolve.fs", version : "");
import(path : "testing.fs", version : "");

const MODULE = inch / 20;

/**
 * Spur gears with `teeth` and `mateTeeth`, as `gearMesh` sees them.
 */
function spur(teeth is number, mateTeeth is number) returns map
{
    return gearMesh({ "gearType" : GearType.GEAR }, MODULE, teeth, mateTeeth);
}

export function testSpurMateIsThePitchRadiiAway()
{
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, spur(20, 30));
    expectNear(norm(mate.origin - vector(25, 0, 0) * MODULE), 0 * meter, 1e-9 * meter);
    expectNear(norm(mate.normal - Z_DIRECTION), 0, 1e-9);
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
    const sector = gearMesh({ "gearType" : GearType.GEAR, "sector" : true, "sectorTeeth" : 4 }, MODULE, 20, 20);
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
            }), "it would be a crown or internal gear");
}

export function testBevelGearsMateCantBeACrownGear()
{
    expectContains(expectThrows(function()
            {
                bevelPairCones(MODULE, 10, 40, 120 * degree);
            }), "its mate would be a crown or internal gear");
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
 * A 20 DP bevel gear with `teeth`, meshing with `mateTeeth` at `shaftAngle`, `thickness` thick.
 */
function bevel(teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits) returns map
{
    return bevelShape({
                "gearType" : GearType.BEVEL,
                "unitSystem" : UnitSystem.IMPERIAL,
                "mateTeeth" : mateTeeth,
                "shaftAngle" : shaftAngle,
                "thickness" : thickness,
                "addProfileOffset" : false
            }, MODULE, teeth);
}

export function testBevelThicknessRunsFromItsBackToItsTop()
{
    // 45 degree cones: the apex is a pitch radius (half an inch) above the back's pitch circle
    const shape = bevel(20, 20, 90 * degree, 0.25 * inch);
    expectNear(shape.apex, 0.5 * inch, 1e-9 * meter);
    // Its flat back is under its teeth's roots, along the back cone
    expectNear(shape.bottom, -1.25 * MODULE * sin(45 * degree), 1e-9 * meter);
    expectNear(shape.top - shape.bottom, 0.25 * inch, 1e-9 * meter);
    // Its teeth run along the pitch cone to its top
    expectNear(shape.faceWidth * cos(45 * degree), shape.top, 1e-9 * meter);
}

export function testBevelGearsPointsAreItsBottomMiddleTopAndApex()
{
    const definition = {
            "gearType" : GearType.BEVEL,
            "unitSystem" : UnitSystem.IMPERIAL,
            "mateTeeth" : 20,
            "shaftAngle" : 90 * degree,
            "thickness" : 0.25 * inch,
            "addProfileOffset" : false
        };
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
    expectContains(expectThrows(function()
            {
                bevel(20, 20, 90 * degree, 0.05 * inch);
            }), "too thin for its teeth");
}

export function testBevelGearTooThickReachesItsApex()
{
    expectContains(expectThrows(function()
            {
                bevel(20, 20, 90 * degree, 0.6 * inch);
            }), "its top would reach its apex");
}

export function testBevelGearTooThinForItsMatesTeeth()
{
    // The pinion's thick enough for its own steep teeth, but its mate's, nearly flat, need a longer face
    expectContains(expectThrows(function()
            {
                bevel(10, 40, 90 * degree, 0.15 * inch);
            }), "too thin for its mate's teeth");
    bevel(10, 40, 90 * degree, 0.25 * inch);
}

export function testBevelMateHasTheGearsFaceWidth()
{
    const gear = bevel(10, 40, 90 * degree, 0.25 * inch);
    const mate = bevelShape({
                "gearType" : GearType.BEVEL,
                "mateTeeth" : 10,
                "shaftAngle" : 90 * degree,
                "faceWidth" : gear.faceWidth,
                "addProfileOffset" : false
            }, MODULE, 40);
    expectNear(mate.faceWidth, gear.faceWidth, 1e-12 * meter);
    expectNear(mate.top, gear.faceWidth * cos(mate.cones.gear.pitchAngle), 1e-12 * meter);
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
