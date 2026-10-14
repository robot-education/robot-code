FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "released/shaft/robotShaft.fs", version : "");
import(path : "testing.fs", version : "");

/**
 * A custom 1/2 in. hex shaft with retaining rings on both ends, `override` their groove diameters or not.
 */
function ringShaft(overrideGroove is boolean) returns map
{
    return {
            "program" : Program.FRC,
            "shaftSource" : ShaftSource.CUSTOM,
            "shaftType" : ShaftType.HEX,
            "hexSize" : HexSize._1_2_IN,
            "firstEndOperation" : EndOperation.RETAINING_RING,
            "secondEndOperation" : SecondEndOperation.RETAINING_RING,
            "firstEndSideMount" : false,
            "secondEndSideMount" : true,
            "firstEndOverrideGroove" : overrideGroove,
            "secondEndOverrideGroove" : overrideGroove,
            "firstEndGrooveDiameter" : 0.25 * inch,
            "secondEndGrooveDiameter" : 0.25 * inch
        };
}

export function testGrooveOverrideStartsAtTheUsualGroove()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(false), ringShaft(true), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, "0.39 in");
    expectEqual(definition.secondEndGrooveDiameter, "0.396 in");
}

export function testGrooveOverrideKeepsItsValue()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(true), ringShaft(true), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, 0.25 * inch);
}

export function testGrooveOverrideOffLeavesIt()
{
    const definition = robotShaftEditLogic(newContext(), newId(), ringShaft(false), ringShaft(false), false, {}, qNothing());
    expectEqual(definition.firstEndGrooveDiameter, 0.25 * inch);
}
