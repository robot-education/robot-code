FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * A feature with one of every kind of parameter and UI hint, to compare how Onshape draws them with how `fs ui`
 * does (see fs_cli/ui.py). It makes nothing.
 */

export enum TwoOptions
{
    annotation { "Name" : "First" }
    FIRST,
    annotation { "Name" : "Second" }
    SECOND
}

export enum ThreeOptions
{
    annotation { "Name" : "One" }
    ONE,
    annotation { "Name" : "Two" }
    TWO,
    annotation { "Name" : "Three" }
    THREE
}

export enum FourOptions
{
    annotation { "Name" : "New" }
    NEW,
    annotation { "Name" : "Add" }
    ADD,
    annotation { "Name" : "Remove" }
    REMOVE,
    annotation { "Name" : "Intersect" }
    INTERSECT
}

const TEST_TABLE = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "WCP" : {
                "name" : "size",
                "displayName" : "Size",
                "entries" : {
                    "1/2 in." : { "name" : "type", "displayName" : "Type", "entries" : { "Rounded" : 1, "Plain" : 2 } },
                    "3/8 in." : { "name" : "type", "displayName" : "Type", "entries" : { "Rounded" : 3 } }
                }
            },
            "REV" : { "name" : "size", "displayName" : "Size", "entries" : { "1/2 in." : 4 } }
        }
    };

const LENGTH_BOUNDS = { (meter) : [-500, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;
const ANGLE_BOUNDS = { (degree) : [-360, 45, 360] } as AngleBoundSpec;
const COUNT_BOUNDS = { (unitless) : [1, 4, 100] } as IntegerBoundSpec;
const REAL_BOUNDS = { (unitless) : [0, 0.5, 1] } as RealBoundSpec;

annotation { "Feature Type Name" : "UI test bench", "Feature Type Description" : "Every kind of parameter, for comparing UIs." }
export const uiTestBench = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Horizontal enums of 2, 3, and 4 options, as at the top of most features
        annotation { "Name" : "Two options", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.twoOptions is TwoOptions;

        annotation { "Name" : "Three options", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.threeOptions is ThreeOptions;

        annotation { "Name" : "Four options", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.fourOptions is FourOptions;

        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Edges", "Filter" : EntityType.EDGE }
            definition.edges is Query;

            annotation { "Name" : "One face", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
            definition.face is Query;

            // Shows a button to create a mate connector
            annotation { "Name" : "Location", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.location is Query;

            annotation { "Name" : "Flip", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.flip is boolean;

            annotation { "Name" : "Flip around", "UIHint" : ["OPPOSITE_DIRECTION_CIRCULAR"] }
            definition.flipAround is boolean;

            annotation { "Name" : "Secondary axis", "UIHint" : ["MATE_CONNECTOR_AXIS_TYPE"] }
            definition.secondaryAxis is MateConnectorAxisType;

            annotation { "Name" : "Primary axis", "UIHint" : ["PRIMARY_AXIS"] }
            definition.primaryAxis is boolean;
        }

        annotation { "Group Name" : "Values", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Dropdown" }
            definition.dropdown is ThreeOptions;

            annotation { "Name" : "Dropdown with label", "UIHint" : ["SHOW_LABEL"] }
            definition.labeledDropdown is ThreeOptions;

            annotation { "Name" : "Lookup table", "Lookup Table" : TEST_TABLE }
            definition.lookup is LookupTablePath;

            annotation { "Name" : "Length" }
            isLength(definition.length, LENGTH_BOUNDS);

            annotation { "Name" : "Tolerant length", "UIHint" : ["CAN_BE_TOLERANT"] }
            isLength(definition.tolerantLength, LENGTH_BOUNDS);

            annotation { "Name" : "Expression length", "UIHint" : ["SHOW_EXPRESSION"] }
            isLength(definition.expressionLength, LENGTH_BOUNDS);

            annotation { "Name" : "Read only length", "UIHint" : ["READ_ONLY"] }
            isLength(definition.readOnlyLength, LENGTH_BOUNDS);

            annotation { "Name" : "Angle" }
            isAngle(definition.angle, ANGLE_BOUNDS);

            annotation { "Name" : "Count" }
            isInteger(definition.count, COUNT_BOUNDS);

            annotation { "Name" : "Ratio" }
            isReal(definition.ratio, REAL_BOUNDS);

            annotation { "Name" : "Text", "Default" : "Some text" }
            definition.text is string;

            annotation { "Name" : "Checkbox", "Description" : "A description, shown when it's hovered." }
            definition.checkbox is boolean;
        }

        annotation { "Group Name" : "Short rows", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Offset", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
            definition.hasOffset is boolean;

            annotation { "Name" : "Offset", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.offset, LENGTH_BOUNDS);

            annotation { "Name" : "Depth", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
            isLength(definition.shortDepth, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.shortFlip is boolean;
        }

        annotation { "Group Name" : "Icons", "Collapsed By Default" : false }
        {
            // Hole parameter ids, which Onshape shows with icons
            annotation { "Name" : "Hole diameter" }
            isLength(definition.holeDiameter, LENGTH_BOUNDS);

            annotation { "Name" : "Hole depth" }
            isLength(definition.holeDepth, LENGTH_BOUNDS);

            annotation { "Name" : "Tap drill diameter" }
            isLength(definition.tapDrillDiameter, LENGTH_BOUNDS);

            // Icons chosen with the Icon annotation
            annotation { "Name" : "Counterbore diameter", "Icon" : Icon.HOLE_COUNTERBORE_DIAMETER }
            isLength(definition.counterboreDiameter, LENGTH_BOUNDS);

            annotation { "Name" : "Counterbore depth", "Icon" : Icon.HOLE_COUNTERBORE_DEPTH }
            isLength(definition.counterboreDepth, LENGTH_BOUNDS);

            annotation { "Name" : "Countersink angle", "Icon" : Icon.HOLE_COUNTERSINK_ANGLE }
            isAngle(definition.countersinkAngle, ANGLE_BOUNDS);

            annotation { "Name" : "Tapped depth", "Icon" : Icon.HOLE_TAPPED_DEPTH }
            isLength(definition.tappedDepth, LENGTH_BOUNDS);
        }

        annotation { "Group Name" : "Collapsed group", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Hidden until expanded" }
            definition.collapsedCheckbox is boolean;
        }

        annotation { "Name" : "Checked group", "Default" : true }
        definition.checkedGroup is boolean;

        if (definition.checkedGroup)
        {
            annotation { "Group Name" : "Checked group", "Collapsed By Default" : false, "Driving Parameter" : "checkedGroup" }
            {
                annotation { "Name" : "Inside a checked group" }
                isLength(definition.checkedLength, LENGTH_BOUNDS);
            }
        }

        annotation { "Name" : "Unchecked group" }
        definition.uncheckedGroup is boolean;

        if (definition.uncheckedGroup)
        {
            annotation { "Group Name" : "Unchecked group", "Collapsed By Default" : false, "Driving Parameter" : "uncheckedGroup" }
            {
                annotation { "Name" : "Inside an unchecked group" }
                isLength(definition.uncheckedLength, LENGTH_BOUNDS);
            }
        }

        annotation { "Name" : "Items", "Item name" : "item", "Item label template" : "#itemName" }
        definition.items is array;
        for (var item in definition.items)
        {
            annotation { "Name" : "Item name", "Default" : "Item" }
            item.itemName is string;

            annotation { "Name" : "Item length" }
            isLength(item.itemLength, LENGTH_BOUNDS);
        }
    }
    {
    });
