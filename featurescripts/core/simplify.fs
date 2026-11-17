FeatureScript 2960;
/**
 * Simplification: one switch, a Part Studio's Simplified configuration variable, which makes the features allowing it
 * build something lighter (their teeth, pockets, and other detail left out), so a large robot regenerates faster.
 *
 * A feature allowing it declares `simplificationPredicate`'s parameter, says so in its annotation
 * (`"Simplification" : true`, which `fs check` keeps honest and `fs release` writes into the manifest), and builds
 * simplified when `isSimplified`.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Creates the parameter `allowSimplification`: put it in the feature's last group. Off, the feature always builds in
 * full: for a part whose detail matters (a gear checked for interference, a plate going to the router).
 */
export predicate simplificationPredicate(definition is map)
{
    annotation { "Name" : "Allow simplification", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "Builds it simplified while the Part Studio's Simplified configuration variable is on." }
    definition.allowSimplification is boolean;
}

/**
 * Whether to build simplified: it's allowed, and the Part Studio's Simplified variable (a configuration variable) is
 * true. A Part Studio without it is never simplified.
 */
export function isSimplified(context is Context, definition is map) returns boolean
{
    if (!definition.allowSimplification)
    {
        return false;
    }
    // A guard: a Part Studio without the variable isn't simplified
    const simplified = try silent(getVariable(context, "Simplified"));
    return simplified is boolean && simplified;
}
