FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "d82c5bf9082d0054f8f0b419", version : "5ddffe8574f5098d20aa559d");

export predicate beltTablePredicate(definition is map)
{
    annotation { "Name" : "Belt table", "Lookup Table" : beltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.beltPath is LookupTablePath;
}

export predicate doubleBeltTablePredicate(definition is map)
{
    annotation { "Name" : "Double belt table", "Lookup Table" : doubleBeltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.doubleBeltPath is LookupTablePath;
}

export function hasCustomTeeth(path is LookupTablePath)
{
    return path.supplier == "Custom";
}

/**
 * Returns an array of the currently possible belts, or undefined if every belt is allowed.
 */
export function getCurrentBeltOptionsArray(tableAndPath is map)
{
    var path = tableAndPath.path;
    if (hasCustomTeeth(path))
    {
        return undefined;
    }
    // Follow the path to the teeth node, whose entries are the belts
    var node = tableAndPath.table;
    while (node.name != "teeth")
    {
        node = node.entries[path[node.name]];
    }
    var options = [];
    for (var _, belt in node.entries)
    {
        options = append(options, belt.beltTeeth);
    }
    return options;
}

/**
 * Extracts a belt value from a given tableAndPath.
 */
export function getBeltValue(tableAndPath is map)
{
    return getLookupTable(tableAndPath.table, tableAndPath.path);
}

/**
 * Returns the user's currently selected beltType. Used by Robot pulley.
 *
 * @returns {{
 *      @field beltTeeth {number} : The number of teeth of the currently selected belt.
 *              Note this should only be used for editing logic, as that makes definition.beltTeeth the source of truth.
 *      @field beltType {BeltType} :
 *      @field beltWidth {ValueWithUnits} :
 * }}
 */
export function getBeltTypeValue(definition is map) returns map
{
    return getLookupTable(beltTypeTable, definition.beltTypePath);
}
