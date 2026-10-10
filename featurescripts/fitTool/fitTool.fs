FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/chamfer.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");

export enum FitToolType
{
    annotation { "Name" : "Internal" }
    INTERNAL,
    annotation { "Name" : "External" }
    EXTERNAL
}

/**
 * Current idea: Put hatch marks every 0.1 inches or so
 * Then add text on top of that
 * We need the distance between marks to be consistent so the text size is correct
 * We can then measure it correctly
 *
 * So we don't offer that many choices, fine
 * We could also autocompute the tool length, that's probably not a bad option
 * Choose your fit targets and it decides the length automatically
 * You can choose the precision?
 * So we specify an (arbitrary) amount of fit per unit of length
 * Basically we design our ideal hatch mark triangles and parameters
 * With parameters like handle and whatnot already figured out
 * You then print it out and you're off to the races
 */

// export enum WallType
// {
//     annotation { "Name" : "Circle" }
//     CIRCLE,
//     annotation { "Name" : "Rectangle" }
//     RECTANGLE
// }

export enum HatchMarkMeasurement
{
    annotation { "Name" : "Fit" }
    FIT,
    annotation { "Name" : "Count" }
    COUNT,
    annotation { "Name" : "Distance" }
    DISTANCE,

}

annotation { "Feature Type Name" : "Fit tool", "Editing Logic Function" : "fitToolEditLogic", "Manipulator Change Function" : "fitToolManipulatorChange" }
export const fitTool = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Fit tool type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
        definition.fitToolType is FitToolType;

        annotation { "Name" : "Sketch profile face", "Filter" : EntityType.FACE && SketchObject.YES && GeometryType.PLANE }
        definition.profile is Query;

        annotation { "Name" : "Minimum fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.minFit, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Maximum fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.maxFit, SHELL_OFFSET_BOUNDS);

        annotation { "Name" : "Tool length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.toolLength, NONNEGATIVE_LENGTH_BOUNDS);

        if (definition.fitToolType == FitToolType.EXTERNAL)
        {
            // annotation { "Name" : "Wall type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
            // definition.wallType is WallType;
            annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.wallThickness, SHELL_OFFSET_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Add handle", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addHandle is boolean;
        }

        annotation { "Name" : "Add hatch marks", "Default" : true }
        definition.addHatchMarks is boolean;

        if (definition.addHatchMarks)
        {
            annotation { "Group Name" : "Hatch marks", "Collapsed By Default" : false, "Driving Parameter" : "addHatchMarks" }
            {
                annotation { "Name" : "Measurement", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
                definition.hatchMarkMeasurement is HatchMarkMeasurement;

                if (definition.hatchMarkMeasurement == HatchMarkMeasurement.FIT)
                {
                    annotation { "Name" : "Fit per mark" }
                    isLength(definition.fitPerMark, SHELL_OFFSET_BOUNDS);
                }
                else if (definition.hatchMarkMeasurement == HatchMarkMeasurement.COUNT)
                {
                    annotation { "Name" : "Count" }
                    isInteger(definition.count, { (unitless) : [1, 5, 1e5] } as IntegerBoundSpec);
                }
                else
                {
                    annotation { "Name" : "Distance" }
                    isLength(definition.distance, SHELL_OFFSET_BOUNDS);
                }
            }
        }

        annotation { "Name" : "Preview part" }
        definition.preview is boolean;

        if (definition.preview)
        {
            annotation { "Group Name" : "Preview part", "Collapsed By Default" : false, "Driving Parameter" : "preview" }
            {
                annotation { "Name" : "Preview height", "UIHint" : ["ALWAYS_HIDDEN"] }
                isLength(definition.previewHeight, NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS);

                annotation { "Name" : "Fit", "UIHint" : ["READ_ONLY"] }
                isLength(definition.previewFit, LENGTH_BOUNDS);

                annotation { "Name" : "Offset", "UIHint" : ["READ_ONLY"] }
                isLength(definition.previewOffset, LENGTH_BOUNDS);
            }
        }
    }
    {
        verifyNonemptyQuery(context, definition, "profile", "Select a profile to use.");

        const plane = evOwnerSketchPlane(context, { "entity" : definition.profile });

        if (!areQueriesEquivalent(context, definition.profile, qCoincidesWithPlane(definition.profile, plane)))
        {
            throw regenError("The selected profiles must be coplanar.", ["profile"], definition.profile);
        }

        const path = constructPath(context, qLoopEdges(definition.profile));
        definition.outerEdges = path.edges->qUnion();

        addFitToolManipulators(context, id, definition, plane);

        if (!tolerantLessThan(definition.minFit, definition.maxFit))
        {
            throw regenError("The maximum fit must be greater than the minimum fit.", ["minFit", "maxFit"]);
        }

        const triangleMap = computeTriangleMap(definition);

        const toolId = id + "fitTool";
        const outsideEdges = createFitTool(context, toolId, definition, plane, triangleMap);
        const tool = qCreatedBy(toolId, EntityType.BODY)->qBodyType(BodyType.SOLID);

        const hatchMarksId = id + "hatchMarks";
        addHatchMarks(context, hatchMarksId, definition, plane, triangleMap, outsideEdges, tool);

        const previewId = id + "preview";
        createPreview(context, previewId, definition, plane);

        cleanup(context, id + "cleanup", qUnion(qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SHEET), qCreatedBy(previewId, EntityType.BODY)));
    });

/**
 * @param outsideEdges {Query} : A ring of edges in the shape of profile which is used to generate the outer most extent of the hatch marks.
 *          Note the edges don't have to exactly align with the tool; they just have to be at least as large as the actual tool.
 */
function addHatchMarks(context is Context, id is Id, definition is map, plane is Plane, triangleMap is map, outsideEdges is Query, tool is Query)
{
    if (!definition.addHatchMarks)
    {
        return;
    }

    var distance;
    var count;
    if (definition.hatchMarkMeasurement == HatchMarkMeasurement.COUNT)
    {
        count = definition.count;
        distance = definition.toolLength / (count + 1);
    }
    else if (definition.hatchMarkMeasurement == HatchMarkMeasurement.DISTANCE)
    {
        distance = definition.distance;
        count = round(definition.toolLength / distance) - 1;
    }

    const hatchSize = distance / (definition.fitToolType == FitToolType.EXTERNAL ? 12 : 6);
    for (var i = 0; i < count; i += 1)
    {
        const bottom = distance * (i + 1) - hatchSize / 2;
        const top = bottom + hatchSize;

        opExtrude(context, id + ("extrude" ~ i), {
                    "entities" : outsideEdges,
                    "direction" : plane.normal,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : -bottom,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : top,
                });

        if (definition.fitToolType == FitToolType.EXTERNAL)
        {
            opThicken(context, id + ("thicken" ~ i), {
                        "entities" : qCreatedBy(id + ("extrude" ~ i), EntityType.FACE),
                        "thickness1" : 0 * meter,
                        "thickness2" : hatchSize / 2
                    });

            var edges = qCapEntity(id + ("thicken" ~ i), CapType.END, EntityType.EDGE);
            edges = edges->qSubtraction(qParallelEdges(edges, plane.normal));
            opChamfer(context, id + ("chamfer" ~ i), {
                        "entities" : edges,
                        "chamferType" : ChamferType.EQUAL_OFFSETS,
                        "width" : hatchSize / 2
                    });
        }
        else
        {
            const thickness = tan(triangleMap.angle) * (triangleMap.splitOffset + top);
            opThicken(context, id + ("thicken" ~ i), {
                        "entities" : qCreatedBy(id + ("extrude" ~ i), EntityType.FACE),
                        "thickness1" : thickness,
                        "thickness2" : 0 * meter
                    });
        }
    }

    opBoolean(context, id + "cutHatchMarks", {
                "tools" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "targets" : tool,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
}

function createPreview(context is Context, id is Id, definition is map, plane is Plane)
{
    if (!definition.preview)
    {
        return;
    }

    var profile;
    if (definition.fitToolType == FitToolType.INTERNAL)
    {
        const diagonalLength = boundingBoxLength(context, definition.profile);
        opOffsetWire(context, id + "profile", {
                    "edges" : definition.outerEdges,
                    "normal" : plane.normal,
                    "offset1" : diagonalLength / 18,
                    "offset2" : 0 * inch,
                    "makeRegions" : true,
                });
        profile = qCreatedBy(id + "profile", EntityType.FACE);
    }
    else
    {
        profile = definition.profile;
    }

    opExtrude(context, id, {
                "entities" : profile,
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : max(1 * inch, definition.toolLength * 4),
                "startBound" : BoundingType.BLIND,
                "startDepth" : -(definition.toolLength - definition.previewHeight)
            });

    addDebugEntities(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID), DebugColor.BLUE);
}


function computeTriangleMap(definition is map) returns map
{
    const minOffset = definition.minFit / 2;
    const maxOffset = definition.maxFit / 2;
    if (tolerantEqualsZero(minOffset))
    {
        return {
                "splitOffset" : 0 * meter,
                "angle" : atan(maxOffset / definition.toolLength)
            };
    }
    const splitOffset = definition.toolLength / ((maxOffset / minOffset) - 1);
    return {
            "splitOffset" : splitOffset,
            "angle" : atan(minOffset / splitOffset)
        };
}

/**
 * Returns a query for the outer-most edges of the tool.
 */
function createFitTool(context is Context, id is Id, definition is map, plane is Plane, triangleMap is map) returns Query
{
    opExtrude(context, id + "extrude", {
                "entities" : definition.profile,
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : definition.toolLength + triangleMap.splitOffset
            });

    opDraft(context, id + "draft", {
                "draftType" : DraftType.REFERENCE_SURFACE,
                "draftFaces" : qNonCapEntity(id + "extrude", EntityType.FACE),
                "referenceSurface" : plane,
                "pullVec" : plane.normal * (definition.fitToolType == FitToolType.EXTERNAL ? -1 : 1),
                "angle" : triangleMap.angle
            });

    const tool = qCreatedBy(id + "extrude", EntityType.BODY);
    var startFace = qCapEntity(id + "extrude", CapType.START, EntityType.FACE);

    // splitOffset is set to exactly 0
    if (triangleMap.splitOffset != 0 * meter)
    {
        var splitPlane = plane;
        splitPlane.origin += splitPlane.normal * triangleMap.splitOffset;

        opSplitPart(context, id + "split", {
                    "targets" : tool,
                    "tool" : splitPlane
                });
        // A split creates the faces adjoining the split
        startFace = qCreatedBy(id + "split", EntityType.FACE);

        opDeleteBodies(context, id + "deleteBodies", {
                    "entities" : qSplitBy(id + "split", EntityType.BODY, true)
                });

        // Shift tool down so it's still on the plane
        const centerTransform = transform(-splitPlane.normal * triangleMap.splitOffset);
        opTransform(context, id + "centerTransform", {
                    "bodies" : tool,
                    "transform" : centerTransform
                });
    }

    if (definition.fitToolType == FitToolType.INTERNAL)
    {
        if (!definition.addHandle)
        {
            return definition.outerEdges;
        }
        // Add a handle to the tool
        opExtrude(context, id + "handleExtrude", {
                    "entities" : startFace,
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : (isImperial(definition) ? 0.5 * inch : 12.5 * millimeter)
                });
        opBoolean(context, id + "handleBoolean", {
                    "tools" : qUnion(tool, qCreatedBy(id + "handleExtrude", EntityType.BODY)),
                    "operationType" : BooleanOperationType.UNION
                });
        return definition.outerEdges;
    }

    // Extrude outer profile
    opOffsetWire(context, id + "externalFace", {
                "edges" : definition.outerEdges,
                "normal" : plane.normal,
                "offset1" : definition.wallThickness + (definition.maxFit / 2),
                "offset2" : 0 * inch,
                "makeRegions" : true,
            });

    opExtrude(context, id + "externalExtrude", {
                "entities" : qCreatedBy(id + "externalFace", EntityType.FACE),
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : definition.toolLength,
            });


    opBoolean(context, id + "externalCut", {
                "tools" : tool,
                "targets" : qCreatedBy(id + "externalExtrude", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    return qCreatedBy(id + "externalFace", EntityType.EDGE)->qLoopEdges()->qLargest();


}

const MIN_OFFSET_MANIPULATOR = "MIN_OFFSET";
const MAX_OFFSET_MANIPULATOR = "MAX_OFFSET";

const PREVIEW_MANIPULATOR = "PREVIEW";

function addFitToolManipulators(context is Context, id is Id, definition is map, plane is Plane)
{
    const line = evEdgeTangentLine(context, {
                "edge" : definition.outerEdges->qNthElement(0),
                "parameter" : 0.5
            });
    const direction = cross(line.direction, plane.normal);
    const minOffsetManipulator = linearManipulator({
                "base" : line.origin,
                "direction" : direction,
                "offset" : definition.minFit / 2,
                "sources" : definition.profiles,
                "minValue" : 0 * meter
            });
    const maxOffsetManipulator = linearManipulator({
                "base" : line.origin + plane.normal * definition.toolLength,
                "direction" : direction,
                "offset" : definition.maxFit / 2,
                "sources" : definition.profiles,
                "minValue" : 1e-5 * meter
            });

    var previewManipulator = undefined;
    if (definition.preview)
    {
        previewManipulator = linearManipulator({
                    "base" : plane.origin + plane.normal * definition.toolLength,
                    "direction" : -plane.normal,
                    "offset" : definition.previewHeight,
                    "minValue" : 0 * meter,
                    "maxValue" : definition.toolLength
                });
    }

    addManipulators(context, id, {
                (MIN_OFFSET_MANIPULATOR) : minOffsetManipulator,
                (MAX_OFFSET_MANIPULATOR) : maxOffsetManipulator,
                (PREVIEW_MANIPULATOR) : previewManipulator
            });
}


export function fitToolManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators[MIN_OFFSET_MANIPULATOR] != undefined)
    {
        const manipulator = newManipulators[MIN_OFFSET_MANIPULATOR];
        definition.minFit = manipulator.offset * 2;
    }
    if (newManipulators[MAX_OFFSET_MANIPULATOR] != undefined)
    {
        const manipulator = newManipulators[MAX_OFFSET_MANIPULATOR];
        definition.maxFit = manipulator.offset * 2;
    }
    if (newManipulators[PREVIEW_MANIPULATOR] != undefined)
    {
        const manipulator = newManipulators[PREVIEW_MANIPULATOR];
        definition.previewHeight = manipulator.offset;
    }
    return definition;
}

export function fitToolEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    if (oldDefinition != definition)
    {
        const triangleMap = computeTriangleMap(definition);
        definition.previewOffset = tan(triangleMap.angle) * (triangleMap.splitOffset + (definition.toolLength - definition.previewHeight));
        definition.previewFit = definition.previewOffset * 2;
    }

    if (oldDefinition == {})
    {
        const profiles = qEverything(EntityType.FACE)->qSketchFilter(SketchObject.YES);
        if (evaluateQueryCount(context, profiles) == 1)
        {
            definition.profile = profiles;
        }
    }

    return definition;
}
