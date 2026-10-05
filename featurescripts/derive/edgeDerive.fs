FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "45dff3bbc433a900eed1ccbc", version : "14842857f454165acd38a67e");

import(path : "0fb7aae3e8fad817927ae062", version : "22c5f762329d1b71c9e2a020");

/**
 * A feature which derives a Part Studio onto each of several edges (see derive/opPointTransform.fs).
 * The Part Studio is regenerated for each edge with its `length` configuration input set to the edge's length.
 *
 * @param definition {{
 *          @field partStudioData {PartStudioData} :
 *                  The entities to derive. Should have a configuration input named `length`.
 *          @field edges {Query} :
 *                  The edges to derive entities onto.
 *          @field parameters {array} : @optional
 *                  For each of `edgeQuery`, a map of its `index`, `flipPrimaryAxis`, and `secondaryAxisIndex`,
 *                  set by manipulators. @seealso [edgeManipulatorsEditLogic]
 *          @field edgeQuery {Query} : @optional
 *                  The edges `parameters` corresponds to. @seealso [edgeManipulatorsEditLogic]
 *          @field transform {boolean} : @optional
 *                  Whether to move the result by `translationX`, `translationY`, and `translationZ`, and rotate it
 *                  by `rotation` about its Z axis. Defaults to `false`.
 *          @field oppositeDirection {boolean} : @optional
 *                  Whether to reverse `rotation`. Defaults to `false`.
 *          @field deletePlanesAndSketches {boolean} : @optional
 *                  Whether to leave out the Part Studio's planes and sketches. Defaults to `true`.
 * }}
 */
annotation { "Feature Type Name" : "Edge derive",
        "Manipulator Change Function" : "edgeDeriveManipulatorChange",
        "Editing Logic Function" : "edgeDeriveEditLogic",
        "Feature Type Description" : "Derive parts to specific edges in part studios.<br>" ~
        "For full documentation, visit: <br>" ~
        "alexkempen.github.io/alex-featurescript-docs<br>" ~
        "FeatureScript by Alex Kempen."
    }
export const edgeDerive = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Entities to import", "ComputedConfigurationInputs" : ["length"] }
        definition.partStudioData is PartStudioData;

        annotation { "Name" : "Edges", "Filter" : EntityType.EDGE, "UIHint" : ["UNCONFIGURABLE"] }
        definition.edges is Query;

        edgeManipulatorsPredicate(definition);

        edgeDeriveTransformPredicate(definition);

        annotation { "Name" : "Delete planes and sketches", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.deletePlanesAndSketches is boolean;
    }
    {
        verifyNonemptyStudioReference(context, definition, "partStudioData", ErrorStringEnum.IMPORT_DERIVED_NO_PARTS);
        const edges = verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to use.");
        const remainingTransform = getRemainderPatternTransform(context, { "references" : definition.edges });

        var partStudioData = definition.partStudioData;
        if (definition.deletePlanesAndSketches)
        {
            partStudioData = removePlanesAndSketches(partStudioData);
        }
        const parameters = edgeParameters(definition, size(edges));
        const rotation = definition.transform ? adjustAngle(context, definition.rotation) * (definition.oppositeDirection ? -1 : 1) : 0 * degree;

        var manipulatorRadius;
        for (var i, edge in edges)
        {
            const deriveId = id + "location" + unstableIdComponent(i);
            setExternalDisambiguation(context, deriveId, edge);

            var edgeStudio = partStudioData;
            edgeStudio.configuration = mergeMaps(edgeStudio.configuration ?? {}, { "length" : evLength(context, { "entities" : edge }) });
            const entities = instantiatePointDerivePartStudio(context, deriveId, edgeStudio);

            if (manipulatorRadius == undefined)
            {
                const boundingBox = evBox3d(context, { "topology" : entities, "tight" : false });
                manipulatorRadius = norm(project(XY_PLANE, boundingBox.minCorner) - project(XY_PLANE, boundingBox.maxCorner)) * 1.25;
            }

            const result = callSubfeatureAndProcessStatus(id, opPointPattern, context, deriveId, {
                        "entities" : entities,
                        "locations" : [edgeCoordSystem(context, edge)],
                        "identities" : [edge],
                        "index" : parameters[i].index ?? 0,
                        "flipPrimaryAxis" : parameters[i].flipPrimaryAxis,
                        "secondaryAxisType" : secondaryAxisType(parameters[i]),
                        "transform" : definition.transform,
                        "absoluteToWorld" : true,
                        "translationX" : definition.translationX,
                        "translationY" : definition.translationY,
                        "translationZ" : definition.translationZ,
                        "rotation" : rotation
                    }, { "propagateErrorDisplay" : true });

            addEdgeManipulators(context, id, i, result.firstLocation, parameters[i], result.points, result.index, manipulatorRadius);
        }

        const mateConnectors = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR);
        if (!isQueryEmpty(context, mateConnectors))
        {
            opDeleteBodies(context, id + "deleteMateConnectors", { "entities" : mateConnectors });
        }

        transformResultIfNecessary(context, id, remainingTransform);
    },
    {
            "parameters" : [],
            "edgeQuery" : qNothing(),
            "transform" : false,
            "oppositeDirection" : false,
            "deletePlanesAndSketches" : true
        });

/**
 * The options for moving the derived entities away from each edge.
 */
export predicate edgeDeriveTransformPredicate(definition is map)
{
    annotation { "Name" : "Move" }
    definition.transform is boolean;

    if (definition.transform)
    {
        annotation { "Name" : "X translation" }
        isLength(definition.translationX, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Y translation" }
        isLength(definition.translationY, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Z translation" }
        isLength(definition.translationZ, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Rotation angle" }
        isAngle(definition.rotation, ANGLE_360_ZERO_DEFAULT_BOUNDS);

        annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION_CIRCULAR"] }
        definition.oppositeDirection is boolean;
    }
}

/**
 * @internal
 * The manipulator change function for edge derive.
 */
export function edgeDeriveManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return edgeManipulatorsChange(definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for edge derive.
 */
export function edgeDeriveEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    return edgeManipulatorsEditLogic(context, oldDefinition, definition);
}
