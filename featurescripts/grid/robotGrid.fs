FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");
import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");
export import(path : "a6eeed056b8f09ac4e8ae12e", version : "bdabf6a4e955483d02698cb0");

annotation {
        "Feature Type Name" : "Robot grid",
        "Feature Type Description" : "Create a grid of holes which automatically ignore any nearby points." ~ CREDIT,
        "Editing Logic Function" : "robotGridEditLogic",
        "Manipulator Change Function" : "robotGridManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA

    }
export const robotGrid = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face to cut", "Filter" : EntityType.FACE && ModifiableEntityOnly.YES && GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.face is Query;

        annotation { "Name" : "Distance between holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.distance, NONNEGATIVE_LENGTH_BOUNDS);

        holeDiameterPredicate(definition);

        annotation { "Group Name" : "Minimum space", "Collapsed By Default" : true }
        {
            wallPredicate(definition);

            annotation { "Name" : "Recalculate holes" }
            isButton(definition.recalculateHoles);
        }

        annotation { "Name" : "Pause regeneration", "Description" : "Pause feature regeneration in order to improve performance and/or edit more easily." }
        definition.pauseRegeneration is boolean;

        annotation { "Name" : "Holes to skip", "UIHint" : ["ALWAYS_HIDDEN"], "Default" : "{}" }
        isAnything(definition.holesToSkip);

    }
    {
        verifyNonemptyQuery(context, definition, "face", "Select a face to cut.");

        const facePlanes = evFaceTangentPlanes(context, {
                    "face" : definition.face,
                    "parameters" : [vector(0, 0), vector(1, 1)]
                });

        if (!coplanarPlanes(facePlanes[0], facePlanes[1]))
        {
            throw regenError("Face must be flat.", {
                        "faultyParameters" : "face",
                        "entities" : definition.face
                    });
        }

        const part = qOwnerBody(definition.face);
        if (isQueryEmpty(context, part))
        {
            throw regenError("Failed to find selected face's owning part.", {
                        "faultyParameters" : "face",
                        "entities" : definition.face
                    });
        }

        const start = facePlanes[0].origin;
        const end = facePlanes[1].origin;

        const xLine = line(start, facePlanes[0].x);
        const yLine = line(start, yAxis(facePlanes[0]));
        const xEnd = project(xLine, end) - start;
        const yEnd = project(yLine, end) - start;
        const xLength = norm(xEnd);
        const yLength = norm(yEnd);
        const x = normalize(xEnd);
        const y = normalize(yEnd);

        // Add tolerance to avoid, e.g. flooring 27.999999 incorectly
        const xHoles = floor((xLength + TOLERANCE.zeroLength * meter) / definition.distance);
        const yHoles = floor((yLength + TOLERANCE.zeroLength * meter) / definition.distance);

        const depth = evBox3d(context, {
                        "topology" : part,
                        "tight" : false
                    })->box3dDiagonalLength();

        const startOffset = definition.distance / 2;
        const topStart = start + x * startOffset + y * startOffset;

        var points = [];
        var selectedIndices = [];
        var transforms = [];
        var names = [];
        var curr = 0;
        var first = true;
        var firstOffset;
        for (var i = 0; i < xHoles; i += 1)
        {
            const xOffset = x * i * definition.distance;
            for (var j = 0; j < yHoles; j += 1)
            {
                const yOffset = y * j * definition.distance;
                points = append(points, topStart + xOffset + yOffset);
                if (definition.holesToSkip[i ~ "." ~ j] != undefined)
                {
                    curr += 1;
                    continue;
                }
                selectedIndices = append(selectedIndices, curr);

                // Skip the first instance
                if (first)
                {
                    first = false;
                    firstOffset = xOffset + yOffset;
                }
                else
                {
                    transforms = append(transforms, transform(xOffset + yOffset - firstOffset));
                    // . is invalid in a pattern name
                    names = append(names, i ~ "-" ~ j);
                }
                curr += 1;
            }
        }

        addManipulators(context, id, {
                    (HOLES_TO_SKIP_MANIPULATOR) ~ "." ~ xHoles ~ "." ~ yHoles : togglePointsManipulator({
                            "points" : points,
                            "selectedIndices" : selectedIndices,
                            "suppressedIndices" : []
                        })
                });

        if (definition.pauseRegeneration)
        {
            reportFeatureWarning(context, id, "Regeneration is paused.");
            return;
        }

        fCylinder(context, id + "cylinder", {
                    "topCenter" : topStart + firstOffset,
                    "bottomCenter" : topStart + firstOffset - facePlanes[0].normal * depth,
                    "radius" : definition.holeDiameter / 2
                });
        const cylinder = qCreatedBy(id + "cylinder", EntityType.BODY);

        try
        {
            opBoolean(context, id + "cut", {
                        "tools" : cylinder,
                        "targets" : part,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            fCylinder(context, id + "errorCylinder", {
                        "topCenter" : topStart + firstOffset,
                        "bottomCenter" : topStart + firstOffset - facePlanes[0].normal * depth,
                        "radius" : definition.holeDiameter / 2
                    });
            throw regenError("Failed to cut face. Check input.", ["face"], qCreatedBy(id + "errorCylinder", EntityType.BODY));
        }

        const tool = qCreatedBy(id + "cut", EntityType.FACE);
        if (isQueryEmpty(context, tool))
        {
            fCylinder(context, id + "errorCylinder", {
                        "topCenter" : topStart + firstOffset,
                        "bottomCenter" : topStart + firstOffset - facePlanes[0].normal * depth,
                        "radius" : definition.holeDiameter / 2
                    });
            throw regenError("Failed to cut face. Check input.", ["face"], qCreatedBy(id + "errorCylinder", EntityType.BODY));
        }

        opPattern(context, id + "pattern", {
                    "entities" : tool,
                    "transforms" : transforms,
                    "instanceNames" : names
                });

        processSubfeatureStatus(context, id, {
                    "subfeatureId" : id + "pattern"
                });
    });

const HOLES_TO_SKIP_MANIPULATOR = "holesToSkipManipulator";

export function robotGridManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        const parsed = match(key, HOLES_TO_SKIP_MANIPULATOR ~ "\\.(\\d+)\\.(\\d+)");
        if (!parsed.hasMatch)
        {
            continue;
        }
        definition.holesToSkip = {};
        const maxI = stringToNumber(parsed.captures[1]);
        const maxJ = stringToNumber(parsed.captures[2]);

        var selectedIndicesMap = {};
        for (var index in manipulator.selectedIndices)
        {
            selectedIndicesMap[index] = true;
        }

        var curr = 0;
        for (var i = 0; i < maxI; i += 1)
        {
            for (var j = 0; j < maxJ; j += 1)
            {
                // Skip all unselected holes
                if (!(selectedIndicesMap[curr] ?? false))
                {
                    definition.holesToSkip[i ~ "." ~ j] = true;
                }
                curr += 1;
            }
        }
    }
    return definition;
}

export function robotGridEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, clickedButton is string) returns map
{
    if (clickedButton == "recalculateHoles")
    {
        definition.holesToSkip = {};
        try
        {
            const targets = qAdjacent(definition.face, AdjacencyType.EDGE, EntityType.FACE);

            const facePlanes = evFaceTangentPlanes(context, {
                        "face" : definition.face,
                        "parameters" : [vector(0, 0), vector(1, 1)]
                    });
            if (!coplanarPlanes(facePlanes[0], facePlanes[1]))
            {
                return;
            }
            const part = qOwnerBody(definition.face);
            if (isQueryEmpty(context, part))
            {
                return;
            }

            const start = facePlanes[0].origin;
            const end = facePlanes[1].origin;

            const xLine = line(start, facePlanes[0].x);
            const yLine = line(start, yAxis(facePlanes[0]));
            const xEnd = project(xLine, end) - start;
            const yEnd = project(yLine, end) - start;
            const xLength = norm(xEnd);
            const yLength = norm(yEnd);
            const x = normalize(xEnd);
            const y = normalize(yEnd);

            const xHoles = floor(xLength / definition.distance) + 1;
            const yHoles = floor(yLength / definition.distance) + 1;

            const depth = evBox3d(context, {
                            "topology" : part,
                            "tight" : false
                        })->box3dDiagonalLength();

            const startOffset = definition.distance / 2;
            const patternOffset = x * startOffset + y * startOffset;
            const topStart = start + patternOffset;
            const bottomStart = start - facePlanes[0].normal * depth + patternOffset;
            for (var i = 0; i < xHoles; i += 1)
            {
                const xOffset = x * i * definition.distance;
                for (var j = 0; j < yHoles; j += 1)
                {
                    const yOffset = y * j * definition.distance;
                    const cylinderId = id + ("cylinder" ~ i ~ "." ~ j);
                    fCylinder(context, cylinderId, {
                                "topCenter" : topStart + xOffset + yOffset,
                                "bottomCenter" : bottomStart + xOffset + yOffset,
                                "radius" : getWallDiameter(definition, definition.holeDiameter) / 2
                            });
                    const cylinder = qCreatedBy(cylinderId, EntityType.BODY);
                    // Skip if it doesn't collide with the face or it collides with a boundary face
                    if (!collidesWithGeometry(context, cylinder, definition.face) || collidesWithGeometry(context, cylinder, targets))
                    {
                        definition.holesToSkip[i ~ "." ~ j] = true;
                    }
                }
            }
        }
    }
    return definition;
}

function collidesWithGeometry(context is Context, tool is Query, targets is Query) returns boolean
{
    const collisions = evCollision(context, {
                "tools" : tool,
                "targets" : targets
            });
    return collisions != [];
}

