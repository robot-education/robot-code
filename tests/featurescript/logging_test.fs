FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/logging.fs", version : "");
import(path : "testing.fs", version : "");

export function testLengthText()
{
    expectEqual(lengthText(0.25 * inch), "0.25 in");
    expectEqual(lengthText(6.35 * millimeter), "0.25 in");
    expectEqual(lengthText(1 / 3 * inch), "0.3333 in");
}

export function testPointText()
{
    expectEqual(pointText(vector(1, -0.5) * inch), "(1, -0.5)");
}
