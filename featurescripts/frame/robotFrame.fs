FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "45dff3bbc433a900eed1ccbc", version : "14842857f454165acd38a67e");

export import(path : "a816414b5bd99693e25e303c", version : "021db05cf4ac12aff11e01b6");

import(path : "51af66120369fafcb150f4ee", version : "2854eb11ba23a4d0352693d1");

import(path : "onshape/std/frameAttributes.fs", version : "2960.0");
import(path : "onshape/std/frameUtils.fs", version : "2960.0");

import(path : "onshape/std/endcap.fs", version : "2960.0");

MaxTube::import(path : "594bc6a6f01a4185bd3f8065", version : "4f4aa2ce224db4bcc20fcd44");
MaxTubeNoScribe::import(path : "6763435fbc91a950d84e35a6", version : "c32742ed7bebbe949c0ca9a4");
CustomTube::import(path : "68deb23a1fa11686e0c38aae", version : "d04aaed3fbdb6314a1340bb9");

annotation { "Feature Type Name" : "Robot frame",
        "Editing Logic Function" : "robotFrameEditLogic",
        "Manipulator Change Function" : "robotFrameManipulatorChange"
    }
export const robotFrame = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        robotFramePredicate(definition);
    }
    {
        verifyNonemptyQuery(context, definition, "selections", ErrorStringEnum.FRAME_SELECT_PATH);
        
        const tubeDefinition = getTubeDefinition(definition);
        definition.profileSketch = getPartStudioData(tubeDefinition);
        // use top level id to get manipulators
        frame(context, id, definition);
        const frames = qCreatedBy(id, EntityType.BODY);
        setFrameProperties(context, frames); // set properties before holes to avoid double counting robot frames
        addFrameHoles(context, id, frames, definition, tubeDefinition);
        addStartFlipManipulator(context, id, definition.flipStart, frames->qNthElement(0));
    });

function getPartStudioData(tubeDefinition is map) // returns PartStudioData
{
    var buildFunction;
    var configuration = tubeDefinition;
    if (tubeDefinition.isMaxTube)
    {
        buildFunction = tubeDefinition.hasScribeLines ? MaxTube::build : MaxTubeNoScribe::build;
    }
    else
    {
        buildFunction = CustomTube::build;
        configuration = {
                "firstFaceWidth" : tubeDefinition[TubeFace.FIRST].width,
                "secondFaceWidth" : tubeDefinition[TubeFace.SECOND].width,
                "wallThickness" : tubeDefinition.wallThickness
            };
    }
    return partStudioData(buildFunction, configuration);
}

function addFrameHoles(context is Context, id is Id, frames is Query, definition is map, tubeDefinition is map)
precondition
{
    isTopLevelId(id);
}
{
    if (!tubeDefinition.hasHoles)
    {
        return;
    }
    forEachEntity(context, id + "operation", frames, function(frame is Query, frameId is Id)
        {
            setRobotFrameAttribute(context, frame, definition, tubeDefinition, definition.finish);
            if (definition.finish)
            {
                callSubfeatureAndProcessStatus(id, opRobotFrameHoles, context, id + frameId, { "body" : frame });
            }
        });
}

function setFrameProperties(context is Context, frames is Query)
{
    // color as aluminium
    setProperty(context, {
                "entities" : frames,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : color(208 / 255, 213 / 255, 217 / 255)
            });

    setProperty(context, {
                "entities" : frames,
                "propertyType" : PropertyType.MATERIAL,
                "value" : material("Aluminum 6061", 2.70 * gram / centimeter ^ 3)
            });

    const baseIndex = 1 + size(evaluateQuery(context, qRobotFrames(qAllModifiableSolidBodiesNoMesh())));
    for (var i, frame in evaluateQuery(context, frames))
    {
        setProperty(context, {
                    "entities" : frame,
                    "propertyType" : PropertyType.NAME,
                    "value" : "Robot frame " ~ (baseIndex + i)
                });
    }
}

const START_FLIP_MANIPULATOR = "flipStart";

function addStartFlipManipulator(context is Context, id is Id, flipStart is boolean, frame is Query)
{
    const frameStart = flipStart ? qFrameEndFace(frame) : qFrameStartFace(frame);
    const centroid = evApproximateCentroid(context, { "entities" : frameStart });

    const frameLine = getFrameLine(context, frame);
    addManipulators(context, id, {
                (START_FLIP_MANIPULATOR) : flipManipulator({
                        "base" : centroid,
                        "direction" : frameLine.direction,
                        "flipped" : flipStart
                    })
            });
}

export function robotFrameManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators[START_FLIP_MANIPULATOR] is Manipulator)
    {
        definition.flipStart = newManipulators[START_FLIP_MANIPULATOR].flipped;
    }
    definition = frameManipulators(context, definition, newManipulators);
    return definition;
}

export function robotFrameEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean) returns map
{
    definition = frameEditLogicFunction(context, id, oldDefinition, definition, isCreating);
    return definition;
}
