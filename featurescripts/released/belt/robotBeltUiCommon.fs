FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "d82c5bf9082d0054f8f0b419", version : "512461979234ba53a425b7f4");

/**
 * A custom belt width's bounds: a belt's width, when its table's Belt width is Custom.
 */
export const BELT_WIDTH_BOUNDS = { (meter) : [1e-4, 0.015, 500], (inch) : 0.5, (millimeter) : 15 } as LengthBoundSpec;

/**
 * A belt width level's option for a width of the feature's own (its Custom width), as `robotBeltTables.py`'s
 * `CUSTOM_WIDTH`.
 */
export const CUSTOM_BELT_WIDTH = "Custom";

/**
 * A belt table's supplier for belts of any width and teeth, as `robotBeltTables.py`'s `GENERIC_SUPPLIER`.
 */
export const GENERIC_BELT_SUPPLIER = "Generic";

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

/**
 * Whether a belt table's path is to a Generic belt, whose teeth are any (the feature's Belt teeth), rather than a
 * supplier's, whose teeth are the table's.
 */
export function hasCustomTeeth(path is LookupTablePath)
{
    return path.supplier == GENERIC_BELT_SUPPLIER;
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
 * The belt chosen in a table (`tableAndPath`'s `table`, at its `path`), with its `customWidth` as its `beltWidth` if
 * its width is Custom.
 */
export function getBeltValue(tableAndPath is map)
{
    var value = getLookupTable(tableAndPath.table, tableAndPath.path);
    if (value.beltWidth == undefined)
    {
        value.beltWidth = tableAndPath.customWidth;
    }
    return value;
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
    return getBeltValue({ "table" : beltTypeTable, "path" : definition.beltTypePath, "customWidth" : definition.customBeltWidth });
}

/**
 * Whether a belt table's path is to a belt of a custom width (the feature's Custom width).
 */
export function hasCustomWidth(path is LookupTablePath) returns boolean
{
    return path.width == CUSTOM_BELT_WIDTH;
}

/**
 * A belt of a custom width's Custom width: shown by the feature when its table's width is Custom (`hasCustomWidth`, in a
 * hidden parameter set by editing logic, as the precondition can't read the table).
 */
export predicate customBeltWidthPredicate(definition is map)
{
    annotation { "Name" : "Custom width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    isLength(definition.customBeltWidth, BELT_WIDTH_BOUNDS);
}
