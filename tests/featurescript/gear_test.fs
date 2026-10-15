FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "gear/robotGear.fs", version : "");
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
