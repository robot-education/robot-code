FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

import(path : "onshape/std/frameAttributes.fs", version : "1948.0");

export import(path : "onshape/std/tool.fs", version : "1948.0");

export import(path : "5b9ab18e3cb91df78334f11f", version : "73ab93a2f6f67add1fe73a12");

// import(path : "17d4441aee17c4a912b0b6a3", version : "c6c0ffdea5e1387d0134e39a");

// import(path : "eb11a2948f8123134339137f", version : "7624e09d0d1f80a1cbca4112");

export import(path : "8862d241df6ece50b11a2d5b", version : "0da8bd5a705d6d7d56bc3897");

export import(path : "55722fa6852c844195b1ba83", version : "4d6af1967bf58ec7bdf09fd6");
// export import(path : "dabe8d276854435b28430bd8", version : "58df2f87c8abbe2ac501e70a");

VexFrame::import(path : "d5430ccb01b59af6656f00b0", version : "4723e5039bebca55973e2bbf");


annotation { "Feature Type Name" : "Robot frame",
        "Editing Logic Function" : "robotFrameEditLogic",
        "Manipulator Change Function" : "robotFrameManipulatorChange"
    }
export const robotFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Competition", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.competition is Competition;

        annotation { "Name" : "Body type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.bodyType is ToolBodyType;

        if (definition.competition == Competition.FRC)
        {
            // frcFramePredicate(definition);
        }
        else if (definition.competition == Competition.VEX)
        {
            vexFramePredicate(definition);
        }

        framePredicate(definition);
    }
    {
        definition.profileSketch = getProfileSketch(definition);

        const frameId = id + "frame";
        callSubfeatureAndProcessStatus(id, frame, context, frameId, definition);
        const frames = evaluateQuery(context, qCreatedBy(frameId, EntityType.BODY));
        for (var frame in frames)
        {
            
        }
        
        // if (definition.competition == Competition.FRC)
        // {
        //     executeFrcFrame(context, id, definition);
        // }
        // else if (definition.competition == Competition.VEX)
        // {
        //     executeVexFrame(context, id, definition);
        // }
    });

function getProfileSketch(definition is map) returns PartStudioData
{
    var buildFunction;
    var configuration = {};
    if (definition.competition == Competition.VEX)
    {
        buildFunction = VexFrame::build;
        configuration.vexFrameType = definition.vexFrameType;
    }

    return { "buildFunction" : buildFunction, "configuration" : configuration, "partQuery" : qEverything() } as PartStudioData;
}

export function robotFrameManipulatorChange(context is Context, definition is map, newManipulators is map)
{
    definition = frameManipulators(context, definition, newManipulators);
    return definition; // pointDeriveManipulatorChange(context, definition, newManipulators);
}

export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean) returns map
{
    definition = frameEditLogicFunction(context, id, oldDefinition, definition, isCreating);

    return definition;
}
