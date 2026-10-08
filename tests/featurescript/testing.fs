FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Checks for FeatureScript tests (`*_test.fs`, run by pytest with the evaluator in fs_eval): each throws a message
 * saying what was wrong when its check fails.
 */

/**
 * Throws `message` unless `condition` is true.
 */
export function expectTrue(condition is boolean, message is string)
{
    if (!condition)
    {
        throw message;
    }
}

/**
 * Throws unless `actual` equals `expected` (as `==` compares them).
 */
export function expectEqual(actual, expected)
{
    if (actual != expected)
    {
        throw "Expected " ~ toString(expected) ~ ", but got " ~ toString(actual);
    }
}

/**
 * Throws unless `actual` is within `tolerance` of `expected` (numbers, or values with units).
 */
export function expectNear(actual, expected, tolerance)
{
    if (abs(actual - expected) > tolerance)
    {
        throw "Expected " ~ toString(expected) ~ " (within " ~ toString(tolerance) ~ "), but got " ~ toString(actual);
    }
}

/**
 * Throws unless calling `f` throws. Returns what it threw's message: a `regenError`'s custom message, or what was
 * thrown, as a string.
 */
export function expectThrows(f is function) returns string
{
    try
    {
        f();
    }
    catch (error)
    {
        if (error is map)
        {
            return error.customMessage ?? toString(error.message);
        }
        return toString(error);
    }
    throw "Expected it to throw, but it didn't";
}

/**
 * Throws unless `text` contains `part`.
 */
export function expectContains(text is string, part is string)
{
    if (indexOf(text, part) == -1)
    {
        throw "Expected \"" ~ text ~ "\" to contain \"" ~ part ~ "\"";
    }
}
