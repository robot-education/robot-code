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
 * A belt table's supplier for a belt of the feature's own (its Custom belt parameters), as `robotBeltTables.py`'s
 * `CUSTOM_SUPPLIER`.
 */
export const CUSTOM_BELT_SUPPLIER = "Custom";

/**
 * Whether a belt table's path is to a Custom belt, whose type, width, and teeth are the feature's own (its Custom belt
 * parameters), rather than a supplier's, which are the table's.
 */
export function hasCustomTeeth(path is LookupTablePath)
{
    return path.supplier == CUSTOM_BELT_SUPPLIER;
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
 * The belt chosen in a table (`tableAndPath`'s `table`, at its `path`), with what the table leaves to the feature from
 * `tableAndPath`: its `customType`, `customWidth`, and `customTeeth`, for a custom belt (or a custom width).
 */
export function getBeltValue(tableAndPath is map)
{
    var value = getLookupTable(tableAndPath.table, tableAndPath.path);
    value.beltType = value.beltType ?? tableAndPath.customType;
    value.beltWidth = value.beltWidth ?? tableAndPath.customWidth;
    value.beltTeeth = value.beltTeeth ?? tableAndPath.customTeeth;
    return value;
}

/**
 * A custom belt's `BeltType`.
 */
export function getCustomBeltType(customBeltType is CustomBeltType) returns BeltType
{
    return switch (customBeltType) {
                CustomBeltType._5_MM_HTD : BeltType._5_MM_HTD,
                CustomBeltType._3_MM_GT2 : BeltType._3_MM_GT2,
                CustomBeltType._3_MM_HTD : BeltType._3_MM_HTD,
                CustomBeltType._2_MM_GT2 : BeltType._2_MM_GT2
            };
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
