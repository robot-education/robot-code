FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

// tubeTypePredicate

/**
 * Extrudes a sketch into a valid tube profile.
 * Used to create valid tube parts which can be imported.
 */
annotation { "Feature Type Name" : "INTERNAL - Tube extrude",
        "Editing Logic Function" : "tubeExtrudeEditLogic"
    }
export const tubeExtrude = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        tubeTypePredicate(definition);

        if (isSolidTube(definition))
        {
            annotation { "Name" : "Sketch faces", "Filter" : EntityType.FACE && GeometryType.PLANE && SketchObject.YES }
            definition.sketchFaces is Query;
        }
        else if (isSurfaceTube(definition))
        {
            annotation { "Name" : "Sketch edges", "Filter" : EntityType.EDGE && SketchObject.YES }
            definition.sketchEdges is Query;
        }

        annotation { "Name" : "Custom location pattern" }
        definition.customLocationPattern is boolean;

        if (definition.customLocationPattern)
        {
            annotation { "Name" : "Mate connector locations", "Filter" : QueryFilterCompound.ALLOWS_VERTEX }
            definition.locations is Query;
        }

        annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.holeDiameter, HOLE_DIAMETER);
    }
    {
        var entitiesToExtrude;
        if (isSolidTube(definition))
        {
            entitiesToExtrude = definition.sketchFaces;
        }
        else if (isSurfaceTube(definition))
        {
            entitiesToExtrude = definition.sketchEdges;
        }

        if (!isQueryEmpty(context, entitiesToExtrude->qSubtraction(qCoincidesWithPlane(definition.sketchFaces, XY_PLANE))))
        {
            throw regenError("Selected sketch faces are not conicident with the world top plane.", ["sketchFaces"], definition.sketchFaces);
        }

        const length = try(getVariable(context, "length"));
        if (length == undefined || !isLength(length))
        {
            throw regenError("This part studio is missing a valid configuration variable named \"length\". Add one using the configuration panel.");
        }

        opExtrude(context, id + "tubeExtrude", {
                    "entities" : entitiesToExtrude,
                    "direction" : Z_DIRECTION,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : length / 2,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length / 2
                });

        const tubeQuery = qCreatedBy(id + "tubeExtrude", EntityType.BODY);

        if (size(evaluateQuery(context, tubeQuery)) > 1)
        {
            throw regenError("The tube extrude failed to create a single tube. Check input.", ["sketchFaces", "sketchEdges"], entitiesToExtrude);
        }

        const boundingBox = evBox3d(context, {
                    "topology" : tubeQuery,
                    "tight" : true
                });

        const tubeAttribute = tubeAttribute(boundingBox, definition.holeDiameter / 2);

        var locations;
        if (definition.customLocationPattern)
        {
            if (!isQueryEmpty(context, definition.locations->qSubtraction(definition.locations->qCoincidesWithPlane(XY_PLANE))))
            {
                throw regenError("Selected locations must be coincident with the world top plane.", ["locations"],
                    definition.locations->qSubtraction(definition.locations->qCoincidesWithPlane(XY_PLANE)));
            }

            // second to take advantage of evalauteQuery
            definition.locations = verifyNonemptyQuery(context, definition, "locations", "Select one or more vertices to use when locating the tube.");

            if (size(definition.locations) < 4)
            {
                reportFeatureInfo(context, id, "It is recommended to have four or more vertices to use in order to make locating tubes easier.");
            }

            locations = mapArray(definition.locations, function(vertex is Query)
                {
                    return evVertexPoint(context, {
                                "vertex" : vertex
                            });
                });
        }
        else
        {
            const xWidth = tubeAttribute.tubeFaceDefinitions[TubeFace.X].width / 2;
            const yWidth = tubeAttribute.tubeFaceDefinitions[TubeFace.Y].width / 2;
            locations = [zeroVector(3) * meter,
                    vector([xWidth, yWidth, 0 * meter]),
                    vector([-xWidth, yWidth, 0 * meter]),
                    vector([xWidth, -yWidth, 0 * meter]),
                    vector([-xWidth, -yWidth, 0 * meter]),
                    vector([0 * meter, yWidth, 0 * meter]),
                    vector([0 * meter, -yWidth, 0 * meter]),
                    vector([xWidth, 0 * meter, 0 * meter]),
                    vector([-xWidth, 0 * meter, 0 * meter])];
        }

        for (var i, location in locations)
        {
            var coordSystem = WORLD_COORD_SYSTEM;
            coordSystem.origin += location;
            opMateConnector(context, id + ("mateConnector" ~ i), {
                        "coordSystem" : coordSystem,
                        "owner" : tubeQuery
                    });
        }

        setAttribute(context, {
                    "entities" : tubeQuery,
                    "name" : TUBE_ATTRIBUTE,
                    "attribute" : tubeAttribute
                });

        reportFeatureInfo(context, id, "Note: this feature is used to set up configurable tubes which can be derived later. Most users shouldn't need to use this feature directly.");
    });

/**
 * Retrieves the outer radius from `definition`. Throws if the `definition` is not valid.
 */
function getOuterRadius(definition is map) returns ValueWithUnits
{
    if (definition.outerRadiusType == OuterRadiusType.WALL_THICKNESS)
    {
        return definition.holeDiameter / 2 + definition.wallThickness;
    }
    else
    {
        if (definition.outerDiameter < definition.holeDiameter)
        {
            throw regenError("The wall outer diameter must be greater than the hole diameter.", ["holeDiameter", "outerDiameter"]);
        }
        return definition.outerDiameter / 2;
    }
}

/**
 * @internal
 * Editing logic for tube extrude.
 */
export function tubeExtrudeEditLogic(context is Context, id is Id, oldDefinition is map, definition is map)
{
    if (oldDefinition != {} && isSurfaceTube(definition) && isQueryEmpty(context, oldDefinition.sketchEdges) && !isQueryEmpty(context, definition.sketchEdges))
    {
        definition.sketchEdges = qUnion(definition.sketchEdges, qLoopEdges(definition.sketchEdges->qNthElement(0))->qSketchFilter(SketchObject.YES));
    }
    return definition;
}
