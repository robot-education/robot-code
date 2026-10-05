FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
// The std revolve feature, its manipulators, and its editing logic; also exports RevolveBoundingType, a parameter type
export import(path : "onshape/std/revolve.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");

/**
 * Revolves each selected face, sketch region, or curve on its own, so that adjacent regions become separate parts
 * (or surfaces) instead of merging. The revolve version of Multi extrude.
 *
 * Everything else works like the std Revolve feature, which this calls once per selection.
 */
annotation {
        "Feature Type Name" : "Multi revolve",
        "Feature Type Description" : "Revolve distinct faces as new parts even when they're adjacent to each other." ~ CREDIT,
        "Manipulator Change Function" : "revolveManipulatorChange",
        "Filter Selector" : "allparts",
        "Editing Logic Function" : "revolveEditLogic"
    }
export const multiRevolve = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        revolvePredicate(definition);
    }
    {
        const parameter = getSelectionParameter(definition);
        const selections = evaluateQuery(context, definition[parameter]);
        if (selections == [])
        {
            if (definition.bodyType == ExtendedToolBodyType.SURFACE)
            {
                throw regenError(ErrorStringEnum.REVOLVE_SURF_NO_CURVE, [parameter]);
            }
            throw regenError(ErrorStringEnum.REVOLVE_SELECT_FACES, [parameter]);
        }

        // Revolve everything as new bodies and then delete them, so the feature gets revolve's manipulators
        // (which are only shown when added with the top level id)
        var manipulatorDefinition = definition;
        manipulatorDefinition.operationType = NewBodyOperationType.NEW;
        manipulatorDefinition.surfaceOperationType = NewSurfaceOperationType.NEW;
        revolve(context, id, manipulatorDefinition);
        opDeleteBodies(context, id + "deleteManipulators", { "entities" : qCreatedBy(id, EntityType.BODY) });

        for (var i, selection in selections)
        {
            const revolveId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, revolveId, selection);
            definition[parameter] = selection;
            revolve(context, revolveId, definition);
            processSubfeatureStatus(context, id, {
                        "subfeatureId" : revolveId,
                        "propagateErrorDisplay" : true
                    });
        }
    }, { bodyType : ExtendedToolBodyType.SOLID, oppositeDirection : false, operationType : NewBodyOperationType.NEW, surfaceOperationType : NewSurfaceOperationType.NEW, defaultSurfaceScope : true,
            fullRevolve : true, endBound : RevolveBoundingType.BLIND, symmetric : false, hasStartBound : false, endBoundHasOffset : false, startBoundHasOffset : false, startOppositeDirection : false });

/**
 * The parameter holding the selections to revolve, which depends on the creation type.
 */
function getSelectionParameter(definition is map) returns string
{
    if (definition.bodyType == ExtendedToolBodyType.SURFACE)
    {
        return "surfaceEntities";
    }
    if (definition.bodyType == ExtendedToolBodyType.THIN)
    {
        return "wallShape";
    }
    return "entities";
}

/**
 * The std Revolve feature's parameters, forked from its (inline) precondition in revolve.fs.
 */
predicate revolvePredicate(definition is map)
{
    annotation { "Name" : "Creation type", "UIHint" : [UIHint.HORIZONTAL_ENUM, UIHint.REMEMBER_PREVIOUS_VALUE] }
    definition.bodyType is ExtendedToolBodyType;

    if (definition.bodyType != ExtendedToolBodyType.SURFACE)
    {
        booleanStepTypePredicate(definition);
    }
    else
    {
        surfaceOperationTypePredicate(definition);
    }

    if (definition.bodyType == ExtendedToolBodyType.SOLID)
    {
        annotation { "Name" : "Faces and sketch regions to revolve",
                    "Filter" : (EntityType.FACE && GeometryType.PLANE) && ConstructionObject.NO }
        definition.entities is Query;
    }
    else if (definition.bodyType == ExtendedToolBodyType.SURFACE)
    {
        annotation { "Name" : "Edges and sketch curves to revolve",
                    "Filter" : (EntityType.EDGE && ConstructionObject.NO) || (EntityType.BODY && BodyType.WIRE && SketchObject.NO) }
        definition.surfaceEntities is Query;
    }
    else
    {
        annotation { "Name" : "Faces and sketch regions to revolve",
                    "Filter" : (EntityType.FACE && GeometryType.PLANE && ConstructionObject.NO && ModifiableEntityOnly.NO) ||
                    (EntityType.EDGE && SketchObject.YES && ConstructionObject.NO && ModifiableEntityOnly.YES) }
        definition.wallShape is Query;

        annotation { "Name" : "Mid plane", "Default" : false }
        definition.midplane is boolean;

        if (!definition.midplane)
        {
            annotation { "Name" : "Thickness 1", "UIHint" : UIHint.CAN_BE_TOLERANT }
            isLength(definition.thickness1, ZERO_INCLUSIVE_OFFSET_BOUNDS);

            annotation { "Name" : "Flip wall", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.flipWall is boolean;

            annotation { "Name" : "Thickness 2", "UIHint" : UIHint.CAN_BE_TOLERANT }
            isLength(definition.thickness2, NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Thickness", "UIHint" : UIHint.CAN_BE_TOLERANT }
            isLength(definition.thickness, ZERO_INCLUSIVE_OFFSET_BOUNDS);
        }
    }

    annotation { "Name" : "Revolve axis", "Filter" : QueryFilterCompound.ALLOWS_AXIS, "MaxNumberOfPicks" : 1 }
    definition.axis is Query;

    annotation { "Name" : "Full revolve", "Default" : true }
    definition.fullRevolve is boolean;

    if (!definition.fullRevolve)
    {
        annotation { "Name" : "End type" }
        definition.endBound is RevolveBoundingType;
        annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
        definition.oppositeDirection is boolean;

        if (definition.endBound == RevolveBoundingType.BLIND)
        {
            annotation { "Name" : "Revolve angle", "UIHint" : UIHint.CAN_BE_TOLERANT }
            isAngle(definition.angle, ANGLE_360_BOUNDS);

            annotation { "Name" : "Symmetric" }
            definition.symmetric is boolean;
        }
        else if (definition.endBound == RevolveBoundingType.UP_TO_SURFACE)
        {
            annotation { "Name" : "Up to face",
                        "Filter" : (EntityType.FACE && SketchObject.NO && AllowMeshGeometry.YES) || BodyType.MATE_CONNECTOR,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityFace is Query;
        }
        else if (definition.endBound == RevolveBoundingType.UP_TO_BODY)
        {
            annotation { "Name" : "Up to surface or part",
                        "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && SketchObject.NO && AllowMeshGeometry.YES,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityBody is Query;
        }
        else if (definition.endBound == RevolveBoundingType.UP_TO_VERTEX)
        {
            annotation { "Name" : "Up to vertex or mate connector",
                        "Filter" : QueryFilterCompound.ALLOWS_VERTEX,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityVertex is Query;
        }
        if (definition.endBound != RevolveBoundingType.BLIND)
        {
            annotation { "Name" : "Offset", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
            definition.endBoundHasOffset is boolean;
            if (definition.endBoundHasOffset)
            {
                annotation { "Name" : "Offset", "Column Name" : "Offset angle", "UIHint" : ["DISPLAY_SHORT"] }
                isAngle(definition.endBoundOffset, ANGLE_360_ZERO_DEFAULT_BOUNDS);
                annotation { "Name" : "Flip offset", "Column Name" : "Offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                definition.endBoundOffsetFlip is boolean;
            }
        }

        if (definition.endBound != RevolveBoundingType.BLIND || !definition.symmetric)
        {
            annotation { "Name" : "Second end position" }
            definition.hasStartBound is boolean;

            if (definition.hasStartBound)
            {
                annotation { "Group Name" : "Second direction", "Driving Parameter" : "hasStartBound", "Collapsed By Default" : false }
                {
                    annotation { "Name" : "Start type" }
                    definition.startBound is RevolveBoundingType;

                    if (definition.startBound == RevolveBoundingType.BLIND)
                    {
                        annotation { "Name" : "Start angle" }
                        isAngle(definition.angleBack, ANGLE_360_REVERSE_DEFAULT_BOUNDS);
                    }
                    else
                    {
                        annotation { "Name" : "Opposite direction", "Column Name" : "Start opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                        definition.startOppositeDirection is boolean;

                        if (definition.startBound == RevolveBoundingType.UP_TO_SURFACE)
                        {
                            annotation { "Name" : "Up to face", "Column Name" : "Start up to face",
                                        "Filter" : (EntityType.FACE && SketchObject.NO && AllowMeshGeometry.YES) || BodyType.MATE_CONNECTOR,
                                        "MaxNumberOfPicks" : 1 }
                            definition.startBoundEntityFace is Query;
                        }
                        else if (definition.startBound == RevolveBoundingType.UP_TO_BODY)
                        {
                            annotation { "Name" : "Up to surface or part", "Column Name" : "Start up to surface or part",
                                        "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && SketchObject.NO && AllowMeshGeometry.YES,
                                        "MaxNumberOfPicks" : 1 }
                            definition.startBoundEntityBody is Query;
                        }
                        else if (definition.startBound == RevolveBoundingType.UP_TO_VERTEX)
                        {
                            annotation { "Name" : "Up to vertex or mate connector", "Column Name" : "Start up to vertex or mate connector",
                                        "Filter" : QueryFilterCompound.ALLOWS_VERTEX,
                                        "MaxNumberOfPicks" : 1 }
                            definition.startBoundEntityVertex is Query;
                        }
                        annotation { "Name" : "Start offset", "Column Name" : "Start direction has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
                        definition.startBoundHasOffset is boolean;
                        if (definition.startBoundHasOffset)
                        {
                            annotation { "Name" : "Start offset", "Column Name" : "Start direction offset angle", "UIHint" : ["DISPLAY_SHORT"] }
                            isAngle(definition.startBoundOffset, ANGLE_360_ZERO_DEFAULT_BOUNDS);
                            annotation { "Name" : "Flip offset", "Column Name" : "Start offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                            definition.startBoundOffsetFlip is boolean;
                        }
                    }
                }
            }
        }
    }

    if (definition.bodyType == ExtendedToolBodyType.SOLID || definition.bodyType == ExtendedToolBodyType.THIN)
    {
        booleanStepScopePredicate(definition);
    }
    else
    {
        surfaceJoinStepScopePredicate(definition);
    }
}
