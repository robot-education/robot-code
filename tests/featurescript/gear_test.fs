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
    return gearMesh({ "gearType" : GearType.GEAR, "mateTeeth" : mateTeeth }, MODULE, teeth);
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
    const sector = gearMesh({ "gearType" : GearType.SECTOR, "sectorTeeth" : 4, "mateTeeth" : 20 }, MODULE, 20);
    expectNear(sector.toothPhase, 9 * degree, 1e-9 * degree);
    // So their mate has a tooth pointing back at them
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, sector);
    expectNear(norm(mate.x + X_DIRECTION), 0, 1e-9);
}

export function testBevelMateSharesTheApex()
{
    const pair = gearMesh({ "gearType" : GearType.BEVEL, "mateTeeth" : 20, "shaftAngle" : 90 * degree }, MODULE, 20);
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
