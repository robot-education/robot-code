FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "motor/robotMotor.fs", version : "");
import(path : "testing.fs", version : "");

function faces() returns array
{
    var faces = [];
    for (var table in [frcMotorTable, frcGearboxTable, ftcMotorTable, ftcGearboxTable])
    {
        faces = concatenateArrays([faces, leaves(table)]);
    }
    return faces;
}

// Every option's values, through every level
function leaves(level is map) returns array
{
    var found = [];
    for (var _, entry in level.entries)
    {
        found = append(found, entry.entries == undefined ? [entry] : leaves(entry));
    }
    return concatenateArrays(found);
}

function motor(table is map, path is map) returns MotorFace
{
    return getLookupTable(table, lookupTablePath(path)) as MotorFace;
}

export function testEveryEntryIsAFace()
{
    for (var face in faces())
    {
        // Throws if it doesn't typecheck
        face as MotorFace;
        // A block model of our own needs all of its sizes
        for (var key in ["bodyLength", "pilotHeight", "shaftDiameter", "shaftLength"])
        {
            expectEqual(face.bodyDiameter == undefined, face[key] == undefined);
        }
        expectTrue(face.blockMotor == undefined || face.bodyDiameter == undefined, face.partName ~ " has two block models");
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
        const positions = holePositions(face as MotorFace);
        for (var position in positions)
        {
            expectTrue(pilotRadius + holeRadius < norm(position), face.partName ~ "'s holes cut into its pilot's");
        }
        for (var i = 0; i < size(positions); i += 1)
        {
            for (var j = i + 1; j < size(positions); j += 1)
            {
                expectTrue(norm(positions[i] - positions[j]) > 2 * holeRadius, face.partName ~ "'s holes overlap");
            }
        }
    }
}

function expectAt(position is Vector, expected is Vector)
{
    expectNear(norm(position - expected), 0 * meter, 1e-9 * meter);
}

export function testHolePositions()
{
    // As drawn looking at the face: counterclockwise from the right. The plane's x axis points left, so 0° is at -x.
    const neo = holePositions(motor(frcMotorTable, { "motor" : "NEO", "version" : "V1.1" }));
    expectEqual(size(neo), 4);
    expectAt(neo[0], vector(-1, 0) * inch);
    expectAt(neo[1], vector(0, 1) * inch);
    // The Minion's 775 holes, at 53.5° and 233.5°, are up and to the right
    const minion = holePositions(motor(frcMotorTable, { "motor" : "Minion", "pattern" : "775" }));
    expectAt(minion[0], vector(-cos(53.5 * degree), sin(53.5 * degree)) * 14.5 * millimeter);
}

export function testHolePatterns()
{
    // A Kraken X60 fits all its 11 holes, or a Falcon 500's 6, or a CIM's 2
    for (var pattern in [["All", 11], ["Falcon 500", 6], ["CIM", 2]])
    {
        const kraken = motor(frcMotorTable, { "motor" : "Kraken X60", "pattern" : pattern[0] });
        expectEqual(size(holePositions(kraken)), pattern[1]);
        expectEqual(kraken.blockMotor, "Kraken_X60");
    }
    expectEqual(motor(frcMotorTable, { "motor" : "Minion", "pattern" : "550" }).screw, "M3");
    expectEqual(motor(frcMotorTable, { "motor" : "Thrifty Pulsar", "pattern" : "775" }).screw, "M4");
    // goBILDA's 16 mm square, or with two more 24 mm apart
    const square = motor(ftcMotorTable, { "motor" : "Yellow Jacket", "speed" : "435 RPM", "pattern" : "16 mm square" });
    expectEqual(size(holePositions(square)), 4);
    expectAt(holePositions(square)[0], vector(-8, 8) * millimeter);
    expectEqual(size(holePositions(motor(ftcMotorTable, { "motor" : "Yellow Jacket", "speed" : "435 RPM", "pattern" : "All" }))), 6);
}

export function testBlockModels()
{
    // FRCDesign's Block Motor, its Krakens turned so their bumps are by the missing hole
    const kraken = motor(frcMotorTable, { "motor" : "Kraken X44" });
    expectEqual(kraken.blockMotor, "Kraken_X44");
    expectNear(kraken.blockAngle, -90 * degree, 1e-9 * degree);
    expectTrue(hasBlockModel(motor(frcMotorTable, { "motor" : "NEO", "version" : "V2.0" })), "No block model of the NEO 2.0");
    expectTrue(!hasBlockModel(motor(frcMotorTable, { "motor" : "CIM" })), "A block model of the CIM");
    // Versions with the same face, with their own block models
    expectEqual(motor(frcMotorTable, { "motor" : "Falcon 500", "version" : "V3" }).blockMotor, "Falcon_500_V3");
    expectEqual(motor(frcMotorTable, { "motor" : "Falcon 500", "version" : "V1/2" }).blockMotor, "KrakenX60");
    // Our own, longer for gearboxes with more stages
    const fast = motor(ftcMotorTable, { "motor" : "Yellow Jacket", "speed" : "1620 RPM", "pattern" : "All" });
    const slow = motor(ftcMotorTable, { "motor" : "Yellow Jacket", "speed" : "30 RPM", "pattern" : "All" });
    expectTrue(slow.bodyLength > fast.bodyLength, "A 4 stage gearbox isn't longer than a 1 stage one");
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
