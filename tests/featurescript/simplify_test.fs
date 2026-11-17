FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/simplify.fs", version : "");
import(path : "testing.fs", version : "");

export function testNotSimplifiedWithoutTheVariable()
{
    // A Part Studio without the Simplified variable is never simplified (the evaluator can't set variables, so its
    // being on is untested here)
    const context = newContext();
    expectTrue(!isSimplified(context, { "allowSimplification" : true }), "no Simplified variable: not simplified");
    expectTrue(!isSimplified(context, { "allowSimplification" : false }), "not allowed: not simplified");
}
