FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

export enum AnnotateType
{
    annotation { "Name" : "Partial" }
    PARTIAL,
    annotation { "Name" : "Full" }
    FULL
}

export enum TubeReference
{
    annotation { "Name" : "Start" }
    START,
    annotation { "Name" : "End" }
    END,
    annotation { "Name" : "Reference" }
    REFERENCE
}

/**
 * Marks a tube with a single run of tube holes.
 * Each tube hole is represented by a sketch point which can be instantiated as desired.
 */
annotation { "Feature Type Name" : "INTERNAL - Tube point" }
export const annotateTube = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Annotate type", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.annotateType is AnnotateType;

        if (definition.annotateType == AnnotateType.PARTIAL)
        {
            annotation { "Name" : "Tube reference", "UIHint" : ["HORIZONTAL_ENUM"] }
            definition.tubeReference is TubeReference;
        }

        annotation { "Name" : "Plane to use", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.tubeFace is TubeFace;

        annotation { "Name" : "Tube to use", "Filter" : EntityType.BODY && (BodyType.SHEET || BodyType.SOLID) && ModifiableEntityOnly.YES && ConstructionObject.NO, "Max Number Of Picks" : 1 }
        definition.tube is Query;

        if (definition.annotateType == AnnotateType.PARTIAL)
        {
            annotation { "Name" : "Both faces" }
            definition.bothFaces is boolean;

            if (!definition.bothFaces)
            {
                annotation { "Name" : "Opposite face" }
                definition.oppositeFace is boolean;
            }
        }
        else if (definition.annotateType == AnnotateType.FULL)
        {
            annotation { "Name" : "Centered", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.centered is boolean;

            if (!definition.centered)
            {
                annotation { "Name" : "Edge distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.edgeDistance, LENGTH_BOUNDS);

                annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION", "REMEMBER_PREVIOUS_VALUE"] }
                definition.oppositeDirection is boolean;
            }

            annotation { "Name" : "Bottom distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.bottomDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.distance, NONNEGATIVE_LENGTH_BOUNDS);

            annotation { "Name" : "Allow partial holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.allowPartialHoles is boolean;

            annotation { "Name" : "Mark as holes", "Default" : true }
            definition.markAsHoles is boolean;
        }
    }
    {
        verifyTubeSelections(context, definition);

        var tubeAttribute = getAttribute(context, {
                "entity" : definition.tube,
                "name" : TUBE_ATTRIBUTE
            });

        if (definition.annotateType == AnnotateType.FULL)
        {
            var tubeFaceDefinition = tubeAttribute.tubeFaceDefinitions[definition.tubeFace];

            var xOffset;
            if (definition.centered)
            {
                xOffset = tubeFaceDefinition.width / 2;
            }
            else
            {
                if (definition.edgeDistance - TOLERANCE.zeroLength * meter > tubeFaceDefinition.width)
                {
                    reportFeatureWarning(context, id, "The specified edge distance should be less than the width of the tube.");
                }
                xOffset = (definition.oppositeDirection ? tubeFaceDefinition.width - definition.edgeDistance : definition.edgeDistance);
            }

            const numPoints = ceil((tubeAttribute.length - definition.bottomDistance + TOLERANCE.zeroLength * meter) / definition.distance);

            const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : tubeFaceDefinition.coordSystem->plane() });
            const oppositeSketch = newSketchOnPlane(context, id + "oppositeSketch", {
                        "sketchPlane" : oppositeTubeFaceCoordSystem(tubeAttribute, definition.tubeFace)->plane()
                    });

            for (var i = 0; i < numPoints; i += 1)
            {
                const location = vector(xOffset, definition.bottomDistance + i * definition.distance);

                skPoint(sketch, "point" ~ i, { "position" : location });
                skPoint(oppositeSketch, "point" ~ i, { "position" : vector(tubeFaceDefinition.width - xOffset, definition.bottomDistance + i * definition.distance) });
            }

            skSolve(sketch);
            skSolve(oppositeSketch);

            if (definition.markAsHoles)
            {
                for (var i = 0; i < numPoints; i += 1)
                {
                    const location = vector(xOffset, definition.bottomDistance + i * definition.distance);

                    tubeFaceDefinition.holeLocations[location] = true;
                    const tubeHoleAttribute = tubeHoleAttribute(tubeAttribute, definition.tubeFace, location);
                    setAttribute(context, {
                                "entities" : qUnion(sketchEntityQuery(id + "sketch", undefined, "point" ~ i),
                                sketchEntityQuery(id + "oppositeSketch", undefined, "point" ~ i)),
                                "name" : TUBE_HOLE_ATTRIBUTE,
                                "attribute" : tubeHoleAttribute
                            });
                }

                tubeAttribute.tubeFaceDefinitions[definition.tubeFace] = tubeFaceDefinition;
                tubeAttribute.holes = qCreatedBy(id, EntityType.VERTEX);

                setAttribute(context, {
                            "entities" : definition.tube,
                            "name" : TUBE_ATTRIBUTE,
                            "attribute" : tubeAttribute
                        });
            }
            else
            {
                setAttribute(context, {
                            "entities" : qCreatedBy(id, EntityType.BODY),
                            "name" : TUBE_POINT_ATTRIBUTE,
                            "attribute" : "tubePoint"
                        });
            }
        }
    });
