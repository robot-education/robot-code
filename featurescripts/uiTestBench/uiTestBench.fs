FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

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

const BENCH_LENGTH_BOUNDS = { (meter) : [-500, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;
const BENCH_ANGLE_BOUNDS = { (degree) : [-360, 45, 360] } as AngleBoundSpec;
const BENCH_COUNT_BOUNDS = { (unitless) : [1, 4, 100] } as IntegerBoundSpec;
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
            isLength(definition.length, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Tolerant length", "UIHint" : ["CAN_BE_TOLERANT"] }
            isLength(definition.tolerantLength, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Expression length", "UIHint" : ["SHOW_EXPRESSION"] }
            isLength(definition.expressionLength, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Read only length", "UIHint" : ["READ_ONLY"] }
            isLength(definition.readOnlyLength, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Angle" }
            isAngle(definition.angle, BENCH_ANGLE_BOUNDS);

            annotation { "Name" : "Count" }
            isInteger(definition.count, BENCH_COUNT_BOUNDS);

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
            isLength(definition.offset, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Depth", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
            isLength(definition.shortDepth, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.shortFlip is boolean;
        }

        annotation { "Group Name" : "Icons", "Collapsed By Default" : false }
        {
            // Hole parameter ids, which Onshape shows with icons
            annotation { "Name" : "Hole diameter" }
            isLength(definition.holeDiameter, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Hole depth" }
            isLength(definition.holeDepth, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Tap drill diameter" }
            isLength(definition.tapDrillDiameter, BENCH_LENGTH_BOUNDS);

            // Icons chosen with the Icon annotation
            annotation { "Name" : "Counterbore diameter", "Icon" : Icon.HOLE_COUNTERBORE_DIAMETER }
            isLength(definition.counterboreDiameter, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Counterbore depth", "Icon" : Icon.HOLE_COUNTERBORE_DEPTH }
            isLength(definition.counterboreDepth, BENCH_LENGTH_BOUNDS);

            annotation { "Name" : "Countersink angle", "Icon" : Icon.HOLE_COUNTERSINK_ANGLE }
            isAngle(definition.countersinkAngle, BENCH_ANGLE_BOUNDS);

            annotation { "Name" : "Tapped depth", "Icon" : Icon.HOLE_TAPPED_DEPTH }
            isLength(definition.tappedDepth, BENCH_LENGTH_BOUNDS);
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
                isLength(definition.checkedLength, BENCH_LENGTH_BOUNDS);
            }
        }

        annotation { "Name" : "Unchecked group" }
        definition.uncheckedGroup is boolean;

        if (definition.uncheckedGroup)
        {
            annotation { "Group Name" : "Unchecked group", "Collapsed By Default" : false, "Driving Parameter" : "uncheckedGroup" }
            {
                annotation { "Name" : "Inside an unchecked group" }
                isLength(definition.uncheckedLength, BENCH_LENGTH_BOUNDS);
            }
        }

        // Groups inside groups: plain, collapsed, driven by a checkbox (checked and not), and two levels deep
        annotation { "Group Name" : "Nested groups", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Before nested groups" }
            isLength(definition.nestedLength, BENCH_LENGTH_BOUNDS);

            annotation { "Group Name" : "Nested group", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Inside a nested group" }
                isLength(definition.innerLength, BENCH_LENGTH_BOUNDS);

                annotation { "Group Name" : "Doubly nested group", "Collapsed By Default" : false }
                {
                    annotation { "Name" : "Two groups deep" }
                    definition.deepCheckbox is boolean;
                }
            }

            annotation { "Group Name" : "Collapsed nested group", "Collapsed By Default" : true }
            {
                annotation { "Name" : "Hidden until its group is expanded" }
                definition.collapsedNestedCheckbox is boolean;
            }

            annotation { "Name" : "Checked nested group", "Default" : true }
            definition.checkedNestedGroup is boolean;

            if (definition.checkedNestedGroup)
            {
                annotation { "Group Name" : "Checked nested group", "Collapsed By Default" : false, "Driving Parameter" : "checkedNestedGroup" }
                {
                    annotation { "Name" : "Inside a checked nested group" }
                    isLength(definition.checkedNestedLength, BENCH_LENGTH_BOUNDS);
                }
            }

            annotation { "Name" : "Unchecked nested group" }
            definition.uncheckedNestedGroup is boolean;

            if (definition.uncheckedNestedGroup)
            {
                annotation { "Group Name" : "Unchecked nested group", "Collapsed By Default" : false, "Driving Parameter" : "uncheckedNestedGroup" }
                {
                    annotation { "Name" : "Inside an unchecked nested group" }
                    isLength(definition.uncheckedNestedLength, BENCH_LENGTH_BOUNDS);
                }
            }

            annotation { "Name" : "After nested groups" }
            definition.afterNestedCheckbox is boolean;
        }

        annotation { "Name" : "Items", "Item name" : "item", "Item label template" : "#itemName" }
        definition.items is array;
        for (var item in definition.items)
        {
            annotation { "Name" : "Item name", "Default" : "Item" }
            item.itemName is string;

            annotation { "Name" : "Item length" }
            isLength(item.itemLength, BENCH_LENGTH_BOUNDS);

            // A group in an array item
            annotation { "Group Name" : "Item group", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Inside an item's group" }
                item.itemCheckbox is boolean;
            }
        }
    }
    {
    });
