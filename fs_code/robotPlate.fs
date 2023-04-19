FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");
export import(path : "onshape/std/tool.fs", version : "2014.0");

import(path : "a31342637c8f0fafa3d91dec", version : "a0e9b2cd23196f5e48dc48b7");
import(path : "472bc4c291e1d2d6f9b98937", version : "a912ae70fed2318ae5be227c");

export import(path : "21a8778ebd60c8309aede4f7", version : "91a7e3885f16ee811c98bfea");
import(path : "d7162e52f6806a197a034151", version : "e53940e47b83338a6f0a8ea0");
import(path : "2a1fbdd680ed055fe57e372f", version : "b09d630978b71b04194fc4a8");
import(path : "9cb13882ac97598c2be31cc1", version : "68db899741d9fca7b9ee84df");

PlateIcon::import(path : "f4499b93a8838a7034e96418", version : "912d892fd2f79526ef56afec");

/**
 * The top level plate UI predicate.
 */
export predicate robotPlatePredicate(definition is map)
{
    booleanStepTypePredicate(definition);

    annotation { "Name" : "Outer geometry", "UIHint" : ["INITIAL_FOCUS", "ALLOW_QUERY_ORDER"],
                "Filter" : GeometryType.ARC || EntityType.VERTEX || GeometryType.CIRCLE,
                "UIHint" : UIHint.ALLOW_QUERY_ORDER,
                "Description" : "Select geometry which lies on the outside of the plate in a clockwise or counterclockwise fashion.<br>" ~
                "Select the <b>outer edge</b> of circles to use the circle as the outer boundary of the plate. <br>" ~
                "Select <b>arcs</>, or <b>points</b> to use them to define the outer boundary of the plate." }
    definition.selections is Query;

    annotation { "Name" : "Arcs", "Filter" : GeometryType.ARC, "UIHint" : UIHint.ALWAYS_HIDDEN }
    definition.arcQueries is Query;

    annotation { "Name" : "Arc directions", "Default" : "{}", "UIHint" : UIHint.ALWAYS_HIDDEN }
    isAnything(definition.arcDirections);

    platePositionPredicate(definition);

    booleanStepScopePredicate(definition);
}

export predicate platePositionPredicate(definition is map)
{
    annotation { "Name" : "Plate plane", "Filter" : QueryFilterCompound.ALLOWS_PLANE, "MaxNumberOfPicks" : 1,
                "Description" : "A flat face or plane to locate the plate on." }
    definition.platePlane is Query;

    annotation { "Name" : "End type" }
    definition.endBound is PlateBoundingType;

    annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
    definition.plateOppositeDirection is boolean;

    plateThicknessPredicate(definition);

    annotation { "Name" : "Symmetric" }
    definition.symmetric is boolean;
}

export predicate plateThicknessPredicate(definition is map)
{
    if (definition.endBound == PlateBoundingType.BLIND || definition.endBound == PlateBoundingType.THROUGH_ALL)
    {
        annotation { "Name" : "Depth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.depth, LENGTH_BOUNDS);
    }
    else if (definition.endBound == PlateBoundingType.UP_TO)
    {
        annotation { "Name" : "Up to entity",
                    "Filter" : (EntityType.FACE && GeometryType.PLANE && SketchObject.NO) || QueryFilterCompound.ALLOWS_VERTEX,
                    "MaxNumberOfPicks" : 1 }
        definition.endBoundEntity is Query;

        annotation { "Name" : "Offset distance", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasOffset is boolean;

        if (definition.hasOffset)
        {
            annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.offsetDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Offset opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.offsetOppositeDirection is boolean;
        }
    }
}

annotation { "Feature Type Name" : "Robot plate",
        "Manipulator Change Function" : "robotPlateManipulatorChange",
        "Editing Logic Function" : "robotPlateEditLogic",
        "Icon" : PlateIcon::BLOB_DATA,
        "Feature Type Description" : "Quickly create complex plates from basic geometry. <br>" ~
        "FeatureScript created by Alex Kempen. Icons by Evan Reese."
    }
export const robotPlate = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        robotPlatePredicate(definition);
    }
    {
        definition.selections = verifyNonemptyQuery(context, definition, "selections", customError(CustomError.SELECT_GEOMETRY));

        const plateInfo = getPlateInfo(context, definition);
        const platePlane = plateInfo.platePlane;
        const depth = plateInfo.depth;
        const oppositePlane = plateInfo.oppositePlane;

        var flipMap = {};
        for (var i, arcQuery in evaluateQuery(context, definition.arcQueries))
        {
            flipMap[arcQuery] = definition.arcDirections[i];
        }
        var toDelete = new box([]);
        const result = getPlateGeometryFaces(context, id, platePlane, {
                    "flipMap" : flipMap,
                    "selections" : definition.selections,
                    "toDelete" : toDelete
                });

        var manipulators = result.manipulators;
        if (definition.endBound == PlateBoundingType.BLIND)
        {
            manipulators = mergeMaps(manipulators, getPlateExtrudeManipulator(context, id, definition, result.centroid, platePlane));
        }
        addManipulators(context, id, manipulators);

        callSubfeatureAndProcessStatus(id, opExtrude, context, id + "extrudePlate", {
                    "entities" : result.plateFaces,
                    "direction" : platePlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : depth
                });

        { // empty scope
            const reconstructOp = function(id)
                {
                    callSubfeatureAndProcessStatus(id, opExtrude, context, id + "extrudePlate", {
                                "entities" : result.plateFaces,
                                "direction" : platePlane.normal,
                                "endBound" : BoundingType.BLIND,
                                "endDepth" : depth
                            });
                    cleanup(context, id + "delete", qUnion(toDelete[]));
                };
            processNewBodyIfNeeded(context, id, definition, reconstructOp);
        }

        cleanup(context, id + "delete", qUnion(toDelete[]));

        if (definition.operationType == NewBodyOperationType.NEW || definition.operationType == NewBodyOperationType.ADD)
        {
            const plateAttribute = plateAttribute(qCreatedBy(id, EntityType.BODY), platePlane, depth, result.boundary);
            setPlateAttribute(context, plateAttribute);
        }
    });

function getPlateExtrudeManipulator(context is Context, id is Id, definition is map, centroid is Vector, platePlane is Plane) returns map
precondition
{
    is2dPoint(centroid);
}
{
    return {
            (PLATE_EXTRUDE_MANIPULATOR) : linearManipulator({
                    "base" : planeToWorld(platePlane, centroid),
                    "direction" : definition.plateOppositeDirection ? platePlane.normal : -platePlane.normal,
                    "offset" : definition.depth * (definition.plateOppositeDirection ? 1 : -1),
                    "primaryParameterId" : "depth"
                })
        };
}

export function robotPlateManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var manipulatorKey, manipulator in newManipulators)
    {
        if (manipulatorKey == PLATE_EXTRUDE_MANIPULATOR)
        {
            definition = handleExtrudeManipulator(definition, manipulator);
        }
        else if (manipulatorKey == POINT_MANIPULATOR)
        {
            definition = handlePointManipulator(context, definition, manipulator);
        }
        else
        {
            const parsed = match(manipulatorKey, FLIP_MANIPULATOR ~ "(\\d+)");
            definition = handleArcFlipManipulatorChange(definition, stringToNumber(parsed.captures[1]), manipulator);
        }
    }
    return definition;
}

function handleArcFlipManipulatorChange(definition is map, index is number, manipulator is Manipulator) returns map
{
    // const arc = definition.selections->qGeometry(GeometryType.ARC)->qNthElement(index);
    definition.arcDirections[index] = manipulator.flipped;
    return definition;
}

function handleExtrudeManipulator(definition is map, manipulator is Manipulator) returns map
{
    const newDepth = manipulator.offset;
    definition.depth = abs(newDepth);
    definition.plateOppositeDirection = newDepth > 0;
    return definition;
}

function handlePointManipulator(context is Context, definition is map, manipulator is Manipulator) returns map
{
    var selections = evaluateQuery(context, definition.selections);
    const targetIndex = manipulator.index;
    var currentIndex = size(selections) - 1;
    // move currentIndex to targetIndex
    while (currentIndex != targetIndex)
    {
        currentIndex = getPrevious(size(selections), currentIndex);
        selections = shuffleBackward(selections);
    }
    definition.selections = qUnion(selections);
    return definition;
}

export function robotPlateEditLogic(
    context is Context,
    id is Id,
    oldDefinition is map,
    definition is map,
    isCreating is boolean,
    specifiedParameters is map) returns map
{
    definition = updateArcs(context, definition);
    if (oldDefinition == {} || !isCreating)
    {
        return definition;
    }
    definition.platePlane = autoFillPlatePlane(context, oldDefinition, definition, specifiedParameters);
    return definition;
}

function updateArcs(context is Context, definition is map) returns map
{
    var mapOld = {};
    for (var i, oldQuery in evaluateQuery(context, definition.arcQueries))
    {
        mapOld[oldQuery] = definition.arcDirections[i];
    }

    // Construct current arcParameters
    const nowQueries = evaluateQuery(context, qGeometry(definition.selections, GeometryType.ARC));
    definition.arcDirections = mapArray(nowQueries, function(nowQuery is Query)
        {
            const oldDirection = mapOld[nowQuery];
            return (oldDirection == undefined ? false : oldDirection);
        });
    definition.arcQueries = qUnion(nowQueries);
    return definition;
}

function autoFillPlatePlane(context is Context, oldDefinition is map, definition is map, specifiedParameters is map) returns Query
{
    if (specifiedParameters.platePlane ||
        !isQueryEmpty(context, definition.platePlane) ||
        oldDefinition.selections == definition.selections)
    {
        return definition.platePlane;
    }

    for (var tId in evaluateQuery(context, definition.selections))
    {
        const entityPlane = try(!isQueryEmpty(context, tId->qSketchFilter(SketchObject.YES)) ?
                evOwnerSketchPlane(context, { "entity" : tId }) : evPlane(context, { "face" : tId }));

        if (entityPlane == undefined)
        {
            continue;
        }
        const adjacentConstructionPlanes = qCoincidesWithPlane(qEverything(EntityType.FACE)->qConstructionFilter(ConstructionObject.YES), entityPlane);

        // look for a coincident construction plane
        if (!isQueryEmpty(context, adjacentConstructionPlanes))
        {
            return adjacentConstructionPlanes->qNthElement(0);
        }
        else // use any coincident face
        {
            return qCoincidesWithPlane(qEverything(EntityType.FACE), entityPlane)->qNthElement(0);
        }
    }
    return definition.platePlane;
}

