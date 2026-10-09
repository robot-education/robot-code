FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "motor/robotMotor.fs", version : "");
import(path : "testing.fs", version : "");

function faces() returns array
{
    var faces = [];
    for (var table in [motorTable, gearboxTable])
    {
        for (var _, entry in table.entries)
        {
            faces = append(faces, entry);
        }
    }
    return faces;
}

export function testEveryEntryIsAFace()
{
    for (var face in faces())
    {
        // Throws if it doesn't typecheck
        face as MotorFace;
        // A block model needs its body's length, and a shaft its length
        expectEqual(face.bodyDiameter == undefined, face.bodyLength == undefined);
        expectEqual(face.shaftDiameter == undefined, face.shaftLength == undefined);
        // Screws std's hole tables know
        fastenerHoleDiameter({ "fit" : Fit.FREE }, face.screw);
    }
}

export function testHolesClearThePilot()
{
    // With free fits, each screw's hole stays clear of the pilot's, and of the other holes
    for (var face in faces())
    {
        const definition = { "fit" : Fit.FREE, "boreFit" : Fit.FREE, "skipHoles" : false };
        const pilotRadius = (face.pilotDiameter + boreFitClearance(definition, face.pilotDiameter)) / 2;
        const holeRadius = fastenerHoleDiameter(definition, face.screw) / 2;
        expectTrue(pilotRadius + holeRadius < face.boltCircleDiameter / 2, face.partName ~ "'s holes cut into its pilot's");
        const positions = holePositions(face as MotorFace);
        for (var i = 0; i < size(positions); i += 1)
        {
            for (var j = i + 1; j < size(positions); j += 1)
            {
                expectTrue(norm(positions[i] - positions[j]) > 2 * holeRadius, face.partName ~ "'s holes overlap");
            }
        }
    }
}

export function testHolePositions()
{
    const neo = getLookupTable(motorTable, lookupTablePath({ "motor" : "NEO" })) as MotorFace;
    const positions = holePositions(neo);
    expectEqual(size(positions), 4);
    expectNear(norm(positions[0] - vector(1, 0) * inch), 0 * meter, 1e-9 * meter);
    expectNear(norm(positions[1] - vector(0, 1) * inch), 0 * meter, 1e-9 * meter);
    // The Kraken X60 has every 30°, but for 270°
    const kraken = getLookupTable(motorTable, lookupTablePath({ "motor" : "Kraken X60" })) as MotorFace;
    expectEqual(size(holePositions(kraken)), 11);
}

export function testSkippedHoles()
{
    expectEqual(skippedHoleIndices({ "skipHoles" : false, "skippedHoles" : [{ "index" : 1 }] }, 4), []);
    // Counted from 1; repeats and holes past the last are left out
    const definition = { "skipHoles" : true, "skippedHoles" : [{ "index" : 2 }, { "index" : 2 }, { "index" : 9 }, { "index" : 4 }] };
    expectEqual(skippedHoleIndices(definition, 4), [1, 3]);
    const positions = [vector(1, 0) * inch, vector(0, 1) * inch, vector(-1, 0) * inch, vector(0, -1) * inch];
    expectEqual(keptHolePositions(definition, positions), [vector(1, 0) * inch, vector(-1, 0) * inch]);
}

export function testManipulatorChange()
{
    const definition = { "skipHoles" : true, "skippedHoles" : [], "angleOffset" : 0 * degree, "angleOffsetOppositeDirection" : false };
    // Toggling points sets the holes to skip, counted from 1
    const toggled = robotMotorManipulatorChange(newContext(), definition, { "skippedHoles" : { "selectedIndices" : [0, 3] } });
    expectEqual(toggled.skippedHoles, [{ "index" : 1 }, { "index" : 4 }]);
    // Dragging the angle sets its value and direction
    const dragged = robotMotorManipulatorChange(newContext(), definition, { "angleOffsetManipulator" : { "angle" : -30 * degree } });
    expectNear(dragged.angleOffset, 30 * degree, 1e-9 * radian);
    expectTrue(dragged.angleOffsetOppositeDirection, "Dragging back doesn't flip the angle");
    expectEqual(dragged.skippedHoles, []);
}
