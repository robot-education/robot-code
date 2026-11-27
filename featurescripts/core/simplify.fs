FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Simplification: features which allow it build something lighter while the Part Studio's Simplified configuration
 * parameter is on, a configuration variable `#Simplified` of `true` (see `docs/auto-assembly.md`). A Part Studio
 * without it is never simplified. A feature calling `simplificationPredicate` says `"Simplification" : true` in its
 * annotation, which `fs check` requires.
 */
export const SIMPLIFIED_VARIABLE = "Simplified";

/**
 * Allow simplification: off, the feature always builds in full, for a part whose detail matters.
 */
export predicate simplificationPredicate(definition is map)
{
    annotation { "Name" : "Allow simplification", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "Build less while the Part Studio's Simplified configuration is on." }
    definition.allowSimplification is boolean;
}

/**
 * Whether to build simplified: allowed, and the Part Studio's Simplified is on.
 */
export function isSimplified(context is Context, definition is map) returns boolean
{
    return definition.allowSimplification && isSimplifiedValue(try silent(getVariable(context, SIMPLIFIED_VARIABLE)));
}

/**
 * Whether the Simplified variable's value turns simplification on: `true`; anything else, or none, is off.
 */
export function isSimplifiedValue(value) returns boolean
{
    return value is boolean && value;
}
