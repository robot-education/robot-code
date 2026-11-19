FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/gear/robotGear.fs", version : "");
import(path : "released/gear/gearCommon.fs", version : "");
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

/**
 * An internal gear with `teeth`, and its pinion with `mateTeeth`, as `gearMesh` sees them.
 */
function internalPair(teeth is number, mateTeeth is number) returns map
{
    return gearMesh({ "gearType" : GearType.INTERNAL, "centerToCenterAdjustment" : 0 * meter }, MODULE, teeth, mateTeeth);
}

export function testInternalPinionIsInsideThePitchRadiiLessApart()
{
    const mesh = internalPair(72, 12);
    expectNear(mesh.centerDistance, 30 * MODULE, 1e-12 * meter);
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, mesh);
    expectNear(norm(mate.origin - vector(30, 0, 0) * MODULE), 0 * meter, 1e-9 * meter);
    expectNear(norm(mate.normal - Z_DIRECTION), 0, 1e-9);
    // The adjustment moves it out, as a spur pair's
    const adjusted = gearMesh({ "gearType" : GearType.INTERNAL, "centerToCenterAdjustment" : 0.003 * inch }, MODULE, 72, 12);
    expectNear(adjusted.centerDistance, 30 * MODULE + 0.003 * inch, 1e-12 * meter);
}

export function testInternalPinionHasAGapAtTheGearsTooth()
{
    // The gear's first tooth (pointing in) is toward the pinion, whose x axis (a tooth) is half a tooth (15 degrees)
    // around from the pitch point, beyond it
    const mate = gearMatePlane(XY_PLANE, X_DIRECTION, internalPair(72, 12));
    expectNear(norm(mate.x - vector(cos(15 * degree), sin(15 * degree), 0)), 0, 1e-9);
}

export function testInternalPinionTurnsWithTheGear()
{
    // A whole tooth of the gear (5 degrees) around, it still has a tooth toward the pinion, so the pinion has a gap at
    // the pitch point: its teeth are a whole number of teeth (30 degrees) and a half from it. Half a tooth around, a gap,
    // so a tooth.
    for (var teethAround in [1, 0.5, 3.25])
    {
        const toward = vector(cos(5 * teethAround * degree), sin(5 * teethAround * degree), 0);
        const mate = gearMatePlane(XY_PLANE, toward, internalPair(72, 12));
        const turned = atan2(dot(cross(toward, mate.x), Z_DIRECTION), dot(toward, mate.x));
        // Turning the gear by a tooth turns the pinion by one of its own, the same way
        const expected = (15 - 30 * (teethAround - floor(teethAround)) + 3600) % 30;
        expectNear((turned / degree + 3600) % 30, expected, 1e-6);
    }
}

export function testInternalMesh()
{
    // A 10T pinion in a 60T 20 degree gear: the gear's tips stop at where the line of action touches the pinion's base
    // circle, so they touch the pinion down to it
    const mesh = internalMesh(MODULE, 60, 10, 20 * degree);
    const base = 30 * MODULE * cos(20 * degree);
    const between = 25 * MODULE * sin(20 * degree);
    expectNear(mesh.tipRadius, sqrt(base ^ 2 + between ^ 2), 1e-12 * meter);
    expectNear(mesh.mateContactRadius, 5 * MODULE * cos(20 * degree), 1e-12 * meter);
    // The pinion's tips cross the line of action √(r_a² - r_b²) past where it touches the pinion's base circle
    const mateBase = 5 * MODULE * cos(20 * degree);
    expectNear(mesh.contactRadius, sqrt(base ^ 2 + (between + sqrt((6 * MODULE) ^ 2 - mateBase ^ 2)) ^ 2), 1e-12 * meter);
    // A 60T pinion in a 100T gear: its tips are a module in, so they touch the pinion above its base circle
    const large = internalMesh(MODULE, 100, 60, 20 * degree);
    expectNear(large.tipRadius, 49 * MODULE, 1e-12 * meter);
    expectTrue(large.mateContactRadius > 30 * MODULE * cos(20 * degree), "above its base circle");
}

export function testInternalMeshInterferesWithFewTeethBetween()
{
    // As simulated (shapes turned through the mesh): a 10T pinion's tips hit a 14T gear's coming out of mesh, and a 16T
    // one's at 14.5 degrees, but not at 20
    expectTrue(internalMeshInterferes(MODULE, 14, 10, 20 * degree), "10T in 14T, 20 degrees");
    expectTrue(!internalMeshInterferes(MODULE, 16, 10, 20 * degree), "10T in 16T, 20 degrees");
    expectTrue(internalMeshInterferes(MODULE, 16, 10, 14.5 * degree), "10T in 16T, 14.5 degrees");
    expectTrue(!internalMeshInterferes(MODULE, 18, 10, 14.5 * degree), "10T in 18T, 14.5 degrees");
    expectTrue(internalMeshInterferes(MODULE, 26, 20, 20 * degree), "20T in 26T, 20 degrees");
    expectTrue(!internalMeshInterferes(MODULE, 28, 20, 20 * degree), "20T in 28T, 20 degrees");
    expectTrue(internalMeshInterferes(MODULE, 40, 30, 14.5 * degree), "30T in 40T, 14.5 degrees");
    expectTrue(!internalMeshInterferes(MODULE, 40, 30, 20 * degree), "30T in 40T, 20 degrees");
    // Small pinions in large gears don't, their tips trimmed (they would, a module in)
    expectTrue(!internalMeshInterferes(MODULE, 60, 10, 14.5 * degree), "10T in 60T, 14.5 degrees");
    expectTrue(!internalMeshInterferes(MODULE, 200, 10, 20 * degree), "10T in 200T, 20 degrees");
}

export function testInternalRootMeetsTheFlankWherePinionTipsTouch()
{
    // A 72T 14.5 degree gear's full round meets its flanks below where a 36T pinion's tips touch them: it's the fillet
    // meeting them there instead
    const form = getInternalGearForm(MODULE, 72, 36, 14.5 * degree, 1, 0 * meter);
    const full = internalFullRound(form);
    expectTrue(norm(full.tangent) < form.contactRadius, "the full round's below it");
    const root = internalRoot(form);
    expectNear(norm(root.flankTop), form.contactRadius, 1e-9 * meter);
    // A 12T pinion's tips touch them lower down: the full round
    const small = getInternalGearForm(MODULE, 72, 12, 14.5 * degree, 1, 0 * meter);
    expectNear(norm(internalRoot(small).flankTop), norm(internalFullRound(small).tangent), 1e-12 * meter);
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

export function testRevolveAnglesAsStdRevolve()
{
    const base = { "angle" : 90 * degree, "oppositeDirection" : false, "symmetric" : false, "hasStartBound" : false, "angleBack" : 30 * degree };
    expectEqual(revolveAngles(base), { "start" : 0 * degree, "end" : 90 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "oppositeDirection" : true })), { "start" : -90 * degree, "end" : 0 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "symmetric" : true })), { "start" : -45 * degree, "end" : 45 * degree });
    expectEqual(revolveAngles(mergeMaps(base, { "hasStartBound" : true })), { "start" : -30 * degree, "end" : 90 * degree });
}

/**
 * A 20 DP bevel gear's definition, meshing with `mateTeeth` at `shaftAngle`, `thickness` thick.
 */
function bevelDefinition(mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits) returns map
{
    return {
            "gearType" : GearType.BEVEL,
            "pitch" : GearPitch.DP_20,
            "mateTeeth" : mateTeeth,
            "shaftAngle" : shaftAngle,
            "thickness" : thickness,
            "flatTop" : false,
            "addProfileOffset" : false
        };
}

/**
 * A 20 DP bevel gear's shape (see `bevelDefinition`), with `teeth`.
 */
function bevel(teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits) returns map
{
    return bevelShape(bevelDefinition(mateTeeth, shaftAngle, thickness), MODULE, teeth);
}

export function testBevelThicknessRunsFromItsBackToItsTop()
{
    // 45 degree cones: the apex is a pitch radius (half an inch) above the back's pitch circle
    const shape = bevel(20, 20, 90 * degree, 0.25 * inch);
    expectNear(shape.apex, 0.5 * inch, 1e-9 * meter);
    // Its flat back is under its teeth's roots, along the back cone (see testGleasonBevelsBackIsUnderItsDeeperRoots)
    expectNear(shape.top - shape.bottom, 0.25 * inch, 1e-9 * meter);
    // Its teeth run along the pitch cone to its top
    expectNear(shape.faceWidth * cos(45 * degree), shape.top, 1e-9 * meter);
}

export function testGleasonBevelsBackIsUnderItsDeeperRoots()
{
    // A miter pair's teeth are a module each side of the pitch cone, and 0.188 m (and Gleason's allowance) deeper
    const shape = bevel(20, 20, 90 * degree, 0.25 * inch);
    expectNear(shape.depths.addendum, MODULE, 1e-12 * meter);
    expectNear(shape.bottom, -(1.188 * MODULE + 0.002 * inch) * sin(45 * degree), 1e-12 * meter);
    expectNear(shape.top - shape.bottom, 0.25 * inch, 1e-9 * meter);
}

export function testBevelGearsPointsAreItsBottomMiddleTopAndApex()
{
    const definition = bevelDefinition(20, 90 * degree, 0.25 * inch);
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
            }), "Thickness is too small");
}

export function testBevelGearTooThickReachesItsApex()
{
    expectContains(expectThrows(function()
            {
                bevel(20, 20, 90 * degree, 0.6 * inch);
            }), "thickness is too large");
}

export function testBevelGearTooThinForItsMatesTeeth()
{
    // The pinion's thick enough for its own steep teeth, but its mate's, nearly flat, need a longer face
    expectContains(expectThrows(function()
            {
                bevel(10, 40, 90 * degree, 0.12 * inch);
            }), "Thickness is too small");
    bevel(10, 40, 90 * degree, 0.15 * inch);
}

export function testBevelGearsFaceConeCantBeFlat()
{
    // Its pitch cone's under 90 degrees, but its tips' isn't
    expectContains(expectThrows(function()
            {
                bevel(60, 10, 100 * degree, 0.25 * inch);
            }), "too large for a bevel gear");
    expectContains(expectThrows(function()
            {
                bevel(10, 60, 100 * degree, 0.25 * inch);
            }), "too large for a bevel mating gear");
}

export function testBevelMateHasTheGearsFaceWidth()
{
    const gear = bevel(10, 40, 90 * degree, 0.25 * inch);
    var definition = bevelDefinition(10, 90 * degree, 0.25 * inch);
    definition.faceWidth = gear.faceWidth;
    const mate = bevelShape(definition, MODULE, 40);
    expectNear(mate.faceWidth, gear.faceWidth, 1e-12 * meter);
    expectNear(mate.top, gear.faceWidth * cos(mate.cones.gear.pitchAngle), 1e-12 * meter);
}

export function testBevelSectionIsCutOffByItsTop()
{
    const gear = bevel(20, 30, 90 * degree, 0.3 * inch);
    expectNear(gear.topFace, gear.top, 1e-12 * meter);
    const outline = bevelSection(gear).outline;
    expectEqual(size(outline), 5);
    // Its tips run up to its top, which runs in to its axis
    expectNear(outline[2][1], gear.top, 1e-12 * meter);
    expectNear(norm(outline[3] - vector(0 * meter, gear.top)), 0 * meter, 1e-12 * meter);
}

/**
 * A 20 DP bevel gear's shape with Flat top (see `bevelDefinition`), with `teeth`.
 */
function flatTopBevel(teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits, thickness is ValueWithUnits) returns map
{
    var definition = bevelDefinition(mateTeeth, shaftAngle, thickness);
    definition.flatTop = true;
    return bevelShape(definition, MODULE, teeth);
}

export function testBevelFlatTopsTeethEndSquareToItsPitchConeOnItsTopFace()
{
    for (var shaftAngle in [45 * degree, 90 * degree, 110 * degree])
    {
        for (var teeth in [[20, 30], [30, 20]])
        {
            const gear = flatTopBevel(teeth[0], teeth[1], shaftAngle, 0.25 * inch);
            const pitchAngle = gear.cones.gear.pitchAngle;
            const along = vector(-sin(pitchAngle), cos(pitchAngle));
            const backPitch = vector(gear.cones.gear.pitchRadius, 0 * meter);
            const frontPitch = backPitch + along * gear.faceWidth;
            // Its tips at its front are Thickness above its bottom, and its roots there on its top face
            expectNear(gear.top - gear.bottom, 0.25 * inch, 1e-12 * meter);
            expectNear(gear.tipFront[1], gear.top, 1e-12 * meter);
            expectNear(gear.rootFront[1], gear.topFace, 1e-12 * meter);
            expectTrue(gear.topFace < gear.top, "its teeth stand above its top face");
            // Its teeth's ends are square to its pitch cone, through its pitch circle a face width from its back's
            expectNear(dot(gear.tipFront - frontPitch, along), 0 * meter, 1e-12 * meter);
            expectNear(dot(gear.rootFront - frontPitch, along), 0 * meter, 1e-12 * meter);
            // Its root runs straight to its apex
            const toRoot = gear.rootFront - vector(0 * meter, gear.apex);
            const toBackRoot = backPitch - vector(cos(pitchAngle), sin(pitchAngle)) * gear.depths.dedendum - vector(0 * meter, gear.apex);
            expectNear(toRoot[0] * toBackRoot[1] - toRoot[1] * toBackRoot[0], 0 * meter * meter, 1e-15 * meter * meter);
            const section = bevelSection(gear);
            const outline = section.outline;
            expectEqual(size(outline), 6);
            expectNear(norm(outline[4] - vector(0 * meter, gear.topFace)), 0 * meter, 1e-12 * meter);
            // What its gaps mustn't cut starts at its front cone, on its axis, and runs out past its tips
            const solid = section.solid;
            expectNear(solid[0][0], 0 * meter, 1e-12 * meter);
            for (var point in [solid[0], solid[1]])
            {
                expectNear(dot(point - frontPitch, along), 0 * meter, 1e-12 * meter);
            }
            expectTrue(norm(solid[1] - frontPitch) > norm(gear.tipFront - frontPitch), "past its tips");
        }
    }
}

export function testBevelFlatTopsMateEndsAtTheSameConeDistance()
{
    const gear = flatTopBevel(20, 30, 90 * degree, 0.25 * inch);
    var definition = bevelDefinition(20, 90 * degree, 0.25 * inch);
    definition.flatTop = true;
    definition.faceWidth = gear.faceWidth;
    const mate = bevelShape(definition, MODULE, 30);
    expectNear(mate.faceWidth, gear.faceWidth, 1e-12 * meter);
    expectNear(dot(mate.tipFront - vector(0 * meter, mate.apex), vector(sin(mate.cones.gear.pitchAngle), -cos(mate.cones.gear.pitchAngle))),
        mate.cones.gear.coneDistance - gear.faceWidth, 1e-12 * meter);
}

export function testBevelFlatTopsTipsMustReachItsFrontCone()
{
    // Nearly to its apex: its tips, coming down toward it, are below its pitch cone there
    bevel(30, 20, 110 * degree, 0.3 * inch);
    expectContains(expectThrows(function()
            {
                flatTopBevel(30, 20, 110 * degree, 0.3 * inch);
            }), "thickness is too large");
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
    const definition = bevelDefinition(31, 90 * degree, 0.25 * inch);
    const cones = bevelPairCones(MODULE, 13, 31, 90 * degree);
    const depths = bevelDepths(definition, MODULE, 13, cones);
    const mate = gleasonProportions(MODULE, 31, 13, cones.mate.pitchAngle, cones.gear.pitchAngle, 20 * degree);
    expectNear(depths.faceAngle, atan(mate.dedendum / cones.gear.coneDistance), 1e-12 * degree);
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

export function testRackStopManipulatorSetsTheDistance()
{
    // A flat face's: dragged along it (a round face's turns, and needs the face)
    const changed = robotGearManipulatorChange(newContext(), { "rackStopDistance" : 1 * inch },
        { "rackStop" : { "manipulatorType" : ManipulatorType.LINEAR_1D, "offset" : -0.5 * inch } });
    expectEqual(changed.rackStopDistance, -0.5 * inch);
}

/**
 * A 72T 20 DP internal gear's definition, its ring 1/4 in. thick, measured from `from`.
 */
function internalGear(from is InternalRingFrom) returns map
{
    return {
            "gearType" : GearType.INTERNAL,
            "sizeBy" : GearSize.TEETH,
            "teeth" : 72,
            "pitch" : GearPitch.DP_20,
            "location" : qNothing(),
            "ringSizeBy" : InternalRingSize.THICKNESS,
            "ringThickness" : 0.25 * inch,
            "ringFrom" : from,
            "outsideDiameter" : 4 * inch,
            "internalMateTeeth" : 12
        };
}

export function testInternalRingIsMeasuredFromItsTeeth()
{
    // Its pitch radius is 1.8 in., its roots 1.25 modules out, and its tips a module in (above its base circle, at 14.5°)
    expectNear(internalRingRadius(internalGear(InternalRingFrom.ROOT), MODULE, 72), (1.8 + 0.0625 + 0.25) * inch, 1e-12 * meter);
    expectNear(internalRingRadius(internalGear(InternalRingFrom.PITCH), MODULE, 72), (1.8 + 0.25) * inch, 1e-12 * meter);
    // With a 12T pinion, its tips are where the line of action touches the pinion's base circle, outside a module in
    const tip = sqrt((1.8 * cos(14.5 * degree)) ^ 2 + (1.5 * sin(14.5 * degree)) ^ 2) * inch;
    expectTrue(tip > 1.75 * inch, "outside a module in");
    expectNear(internalRingRadius(internalGear(InternalRingFrom.TIP), MODULE, 72), tip + 0.25 * inch, 1e-12 * meter);
    // With a 60T one, a module in
    var large = internalGear(InternalRingFrom.TIP);
    large.internalMateTeeth = 60;
    expectNear(internalRingRadius(large, MODULE, 72), (1.8 - 0.05 + 0.25) * inch, 1e-12 * meter);
    var outside = internalGear(InternalRingFrom.ROOT);
    outside.ringSizeBy = InternalRingSize.OUTSIDE_DIAMETER;
    expectNear(internalRingRadius(outside, MODULE, 72), 2 * inch, 1e-12 * meter);
}

export function testInternalRingOutsideDiameterKeepsTheRing()
{
    // Switching to Outside diameter sets it to the ring's, unless it's been set
    const definition = internalGear(InternalRingFrom.ROOT);
    var switched = definition;
    switched.ringSizeBy = InternalRingSize.OUTSIDE_DIAMETER;
    const result = robotGearEditLogic(newContext(), newId(), definition, switched, false, {}, qNothing());
    expectNear(result.outsideDiameter, 2 * (1.8 + 0.0625 + 0.25) * inch, 1e-12 * meter);
    const specified = robotGearEditLogic(newContext(), newId(), definition, switched, false, { "outsideDiameter" : true }, qNothing());
    expectEqual(specified.outsideDiameter, 4 * inch);
}
