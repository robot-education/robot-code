FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");
import(path : "derive/edgeDerive.fs", version : "");

// TODO: import the nut strip Part Studio from FRCDesignLib, and opPointTransform.fs for partStudioData

/**
 * The thread of a nut strip's holes.
 */
export enum NutStripThread
{
    annotation { "Name" : "#10-32" }
    NUMBER_10_32,
    annotation { "Name" : "1/4-20" }
    QUARTER_20
}

/**
 * Places a nut strip along each selected edge, sized to the edge's length. Built on edge derive.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add nut strips along edges, such as the inside edges of tube." ~ CREDIT,
        "Manipulator Change Function" : "robotNutStripManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic"
    }
export const robotNutStrip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Thread", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.thread is NutStripThread;

        annotation { "Name" : "Edges", "Filter" : EntityType.EDGE && GeometryType.LINE, "UIHint" : ["UNCONFIGURABLE"] }
        definition.edges is Query;

        edgeDeriveParametersPredicate(definition);

        edgeDeriveTransformPredicate(definition);
    }
    {
        verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to add nut strips to.");

        definition.partStudioData = nutStripPartStudioData(definition);
        // use the top level id to get edge derive's manipulators
        edgeDerive(context, id, definition);
    }, {
            "parameters" : [],
            "edgeQuery" : qNothing(),
            "transform" : false,
            "oppositeDirection" : false
        });

/**
 * The nut strip to derive onto each edge. Edge derive sets its `length` configuration input to the edge's length.
 */
function nutStripPartStudioData(definition is map) // returns PartStudioData
{
    // TODO: return the FRCDesignLib nut strip once it's imported and standardized to take a `length` configuration
    // input, e.g. partStudioData(NutStrip::build, { "thread" : definition.thread });
    throw regenError("Nut strips aren't available yet.");
}

/**
 * @internal
 * The manipulator change function for robot nut strip.
 */
export function robotNutStripManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return edgeDeriveManipulatorChange(context, definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for robot nut strip.
 */
export function robotNutStripEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    return edgeDeriveEditLogic(context, id, oldDefinition, definition, isCreating);
}
