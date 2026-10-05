FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/extrude.fs", version : "2960.0");
export import(path : "onshape/std/extrudeCommon.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

ExtrudeIcon::import(path : "0d9de9de05469ac12e79dfa9", version : "c3a60d437e0b6b2e5452f17a");

annotation {
        "Feature Type Name" : "Multi extrude",
        "Feature Type Description" : "Extrude distinct faces as new parts even when they're adjacent to each other." ~ CREDIT,
        "Manipulator Change Function" : "extrudeManipulatorChange",
        "Editing Logic Function" : "extrudeEditLogic",
        "Icon" : ExtrudeIcon::BLOB_DATA
    }
export const multiExtrude = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "UIHint" : UIHint.ALWAYS_HIDDEN, "Default" : OperationDomain.MODEL }
        definition.domain is OperationDomain;

        if (definition.domain != OperationDomain.FLAT)
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
        }
        else
        {
            annotation { "Name" : "Creation type", "UIHint" : UIHint.HORIZONTAL_ENUM, "Default" : FlatOperationType.REMOVE }
            definition.flatOperationType is FlatOperationType;
        }

        if (definition.bodyType != ExtendedToolBodyType.SURFACE || definition.domain == OperationDomain.FLAT)
        {
            if (definition.bodyType != ExtendedToolBodyType.THIN)
            {
                annotation { "Name" : "Faces and sketch regions to extrude",
                            "Filter" : ((AllowFlattenedGeometry.YES && SketchObject.YES && EntityType.FACE) ||
                                    (GeometryType.PLANE && AllowFlattenedGeometry.NO && EntityType.FACE)) && ConstructionObject.NO
                        }
                definition.entities is Query;
            }
        }
        else
        {
            if (definition.bodyType != ExtendedToolBodyType.THIN)
            {
                {
                    annotation { "Name" : "Sketch curves to extrude",
                                "Filter" : (EntityType.EDGE && SketchObject.YES && ModifiableEntityOnly.YES && ConstructionObject.NO) }
                    definition.surfaceEntities is Query;
                }
            }
        }

        if (definition.bodyType == ExtendedToolBodyType.THIN)
        {
            annotation { "Name" : "Faces and sketch regions to extrude",
                        "Filter" : (EntityType.FACE && GeometryType.PLANE && ConstructionObject.NO && ModifiableEntityOnly.NO) ||
                        (EntityType.EDGE && SketchObject.YES && ConstructionObject.NO && ModifiableEntityOnly.YES) }
            definition.wallShape is Query;

            annotation { "Name" : "Mid plane", "Default" : false }
            definition.midplane is boolean;

            if (!definition.midplane)
            {
                annotation { "Name" : "Thickness 1" }
                isLength(definition.thickness1, ZERO_INCLUSIVE_OFFSET_BOUNDS);

                annotation { "Name" : "Flip wall", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.flipWall is boolean;

                annotation { "Name" : "Thickness 2" }
                isLength(definition.thickness2, NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Thickness" }
                isLength(definition.thickness, ZERO_INCLUSIVE_OFFSET_BOUNDS);
            }
        }

        if (definition.domain != OperationDomain.FLAT)
        {
            mainViewExtrudePredicate(definition);
        }
    }
    {

        const entities = verifyNonemptyQuery(context, definition, "entities", ErrorStringEnum.EXTRUDE_NO_SELECTED_REGION);
        
        // Extrude new, then delete so we get manipulators
        const operationType = definition.newBodyOperationType;
        definition.newBodyOperationType = NewBodyOperationType.NEW;
        
        extrude(context, id, definition);
        opDeleteBodies(context, id + "deleteExtrude", { "entities" : qCreatedBy(id, EntityType.BODY) });
        
        definition.newBodyOperationType = operationType;

        for (var i, entity in entities)
        {
            const extrudeId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, extrudeId, entity);
            definition.entities = entity;
            extrude(context, extrudeId, definition);
            processSubfeatureStatus(context, id, {
                        "subfeatureId" : id + i,
                        "propagateErrorDisplay" : true
                    });
        }
    });

predicate extrudeDirectionPredicate(definition is map)
{
    annotation { "Name" : "Direction" }
    definition.hasExtrudeDirection is boolean;

    annotation { "Group Name" : "Direction", "Driving Parameter" : "hasExtrudeDirection", "Collapsed By Default" : false }
    {
        if (definition.hasExtrudeDirection)
        {
            annotation { "Name" : "Extrude direction", "Filter" : QueryFilterCompound.ALLOWS_DIRECTION || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.extrudeDirection is Query;
        }
    }
}

predicate mainViewExtrudePredicate(definition is map)
{
    annotation { "Name" : "End type" }
    definition.endBound is BoundingType;

    annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.oppositeDirection is boolean;

    extrudeBoundParametersPredicate(definition);

    if (definition.bodyType != ExtendedToolBodyType.THIN)
    {
        extrudeDirectionPredicate(definition);
    }

    extrudeOffsetPredicate(definition);

    if (definition.endBound == BoundingType.BLIND || definition.endBound == BoundingType.THROUGH_ALL)
    {
        annotation { "Name" : "Symmetric" }
        definition.symmetric is boolean;
    }

    if (definition.bodyType == ExtendedToolBodyType.SOLID || definition.bodyType == ExtendedToolBodyType.THIN)
    {
        annotation { "Name" : "Draft", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasDraft is boolean;

        if (definition.hasDraft == true)
        {
            annotation { "Name" : "Draft angle", "UIHint" : UIHint.DISPLAY_SHORT }
            isAngle(definition.draftAngle, ANGLE_STRICT_90_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Draft opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
            definition.draftPullDirection is boolean;
        }
    }

    if (!isSymmetricExtrude(definition))
    {
        annotation { "Name" : "Second end position",
                    "UIHint" : UIHint.FIRST_IN_ROW }
        definition.hasSecondDirection is boolean;

        annotation { "Group Name" : "Second end position", "Driving Parameter" : "hasSecondDirection", "Collapsed By Default" : false }
        {
            if (definition.hasSecondDirection)
            {
                annotation { "Name" : "End type", "Column Name" : "Second end type" }
                definition.secondDirectionBound is BoundingType;

                annotation { "Name" : "Opposite direction", "Column Name" : "Second opposite direction",
                            "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : true }
                definition.secondDirectionOppositeDirection is boolean;

                extrudeSecondDirectionBoundParametersPredicate(definition);

                if ((definition.bodyType == ExtendedToolBodyType.SOLID || definition.bodyType == ExtendedToolBodyType.THIN) &&
                    ((definition.secondDirectionOppositeDirection && !definition.oppositeDirection) ||
                            (!definition.secondDirectionOppositeDirection && definition.oppositeDirection)))
                {
                    annotation { "Name" : "Draft", "Column Name" : "Second draft", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
                    definition.hasSecondDirectionDraft is boolean;

                    if (definition.hasSecondDirectionDraft)
                    {
                        annotation { "Name" : "Draft angle", "Column Name" : "Second draft angle", "UIHint" : UIHint.DISPLAY_SHORT }
                        isAngle(definition.secondDirectionDraftAngle, ANGLE_STRICT_90_BOUNDS);

                        annotation { "Name" : "Opposite direction", "Column Name" : "Second draft opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                        definition.secondDirectionDraftPullDirection is boolean;
                    }
                }
            }
        }
    }
    if (definition.bodyType != ExtendedToolBodyType.SURFACE)
    {
        booleanStepScopePredicate(definition);
    }
    else
    {
        surfaceJoinStepScopePredicate(definition);
    }
}

predicate extrudeOffsetPredicate(definition is map)
{
    annotation { "Name" : "Starting offset" }
    definition.startOffset is boolean;
    if (definition.startOffset)
    {
        annotation { "Group Name" : "Starting offset", "Driving Parameter" : "startOffset", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Starting offset bound" }
            definition.startOffsetBound is StartOffsetType;
            if (definition.startOffsetBound == StartOffsetType.BLIND)
            {
                annotation { "Name" : "Depth", "Column Name" : "Starting offset depth" }
                isLength(definition.startOffsetDistance, LENGTH_BOUNDS);
                annotation { "Name" : "Opposite direction", "Column Name" : "Starting offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.startOffsetOppositeDirection is boolean;
            }
            else
            {
                annotation { "Name" : "Entity",
                            "Filter" : (GeometryType.PLANE && EntityType.FACE) || EntityType.EDGE || EntityType.VERTEX || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 1, "Column Name" : "Starting offset entity" }
                definition.startOffsetEntity is Query;
            }
        }
    }
}
