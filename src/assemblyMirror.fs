FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

import(path : "472bc4c291e1d2d6f9b98937", version : "515ac497eaa4f72b8c5f1a57");

/**
 * An enum defining the output of the Assembly mirror feature.
 * @value CENTER :
 *          Mirrored mate connectors are placed at the center of parts.
 * @value ORIGIN :
 *          Mirrored mate connectors are placed relative to the world origin, such that constraining them to
 *          the world origin in the assembly results in the part ending in its mirrored position.
 */
export enum MirrorResultType
{
    annotation { "Name" : "Part center" }
    CENTER,
    annotation { "Name" : "World origin" }
    ORIGIN
}

export enum TransformType
{
    annotation { "Name" : "Linear" }
    LINEAR,
    annotation { "Name" : "Z axis" }
    Z,
    annotation { "Name" : "Y axis" }
    Y,
    annotation { "Name" : "X axis" }
    X
}

annotation { "Feature Type Name" : "Assembly mirror", "Editing Logic Function" : "assemblyMirrorEditLogic",
        "Feature Type Description" : "Create mate connectors which can be used to orient parts in their mirrored position in an assembly.<br>" ~
        "For full documentation, visit: <br>" ~
        "alexkempen.github.io/alex-featurescript-docs<br>" ~
        "FeatureScript by Alex Kempen."
    }
export const assemblyMirror = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Preselection", "Filter" : QueryFilterCompound.ALLOWS_PLANE || (ModifiableEntityOnly.YES && EntityType.BODY && (BodyType.SOLID || BodyType.SHEET)), "UIHint" : ["ALWAYS_HIDDEN", "UNCONFIGURABLE"] }
        definition.preselection is Query;

        annotation { "Name" : "Mirror result", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.mirrorResultType is MirrorResultType;

        annotation { "Name" : "Entity to mirror", "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && ModifiableEntityOnly.YES, "MaxNumberOfPicks" : 1, "UIHint" : ["INITIAL_FOCUS_ON_EDIT"] }
        definition.entity is Query;

        annotation { "Name" : "Mirror plane", "Filter" : QueryFilterCompound.ALLOWS_PLANE, "MaxNumberOfPicks" : 1 }
        definition.mirrorPlane is Query;

        annotation { "Name" : "Transform", "UIHint" : ["SHOW_LABEL"] }
        definition.transformType is TransformType;

        annotation { "Name" : "Custom coordinate system" }
        definition.customReferenceSystem is boolean;

        if (definition.customReferenceSystem)
        {
            annotation { "Name" : "Reference coordinate system", "Filter" : BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1,
                        "Description" : "Specify a coordinate system to make transforms relative to." }
            definition.referenceSystem is Query;
        }

        annotation { "Name" : "Show transform result", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Whether to show the result of assembling a copy of the part using the created mate connectors." }
        definition.showTransformResult is boolean;

        if (definition.showTransformResult)
        {
            annotation { "Name" : "Verify transform",
                        "Default" : true,
                        "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Whether to actively check if the transform correctly mirrors the part across the mirror plane." }
            definition.verifyTransform is boolean;
        }
    }
    {
        verifyNonemptyQuery(context, definition, "entity", ErrorStringEnum.MIRROR_SELECT_PARTS);
        verifyNonemptyQuery(context, definition, "mirrorPlane", ErrorStringEnum.MIRROR_NO_PLANE);

        var patternEntities;
        if (definition.customReferenceSystem)
        {
            verifyNonemptyQuery(context, definition, "referenceSystem", "Select a coordinate system to make transforms relative to.");
            patternEntities = qUnion(definition.entity, definition.referenceSystem);
        }
        else
        {
            patternEntities = definition.entity;
        }

        const remainderTransform = getRemainderPatternTransform(context, { "references" : patternEntities });
        applyTransforms(context, id, definition);
        transformResultIfNecessary(context, id, remainderTransform);
    });

function applyTransforms(context is Context, id is Id, definition is map)
{
    var toDelete = [];

    const mirrorTransform = getMirrorTransform(context, definition);

    const coordSys = getCoordSys(context, id, definition);
    const selectedTransform = getSelectedTransform(definition.transformType, coordSys, mirrorTransform);

    if (definition.showTransformResult)
    {
        const target = getTarget(context, id, definition.entity, mirrorTransform);
        toDelete = append(toDelete, target);
        const result = getTarget(context, id + "result", definition.entity, selectedTransform);
        toDelete = append(toDelete, result);

        if (definition.verifyTransform)
        {
            if (!isQueryEmpty(context, definition.entity->qBodyType(BodyType.SHEET)))
            {
                reportFeatureInfo(context, id, "Surface body transforms cannot be verified automatically.");
                addDebugEntities(context, result, DebugColor.BLUE);
            }
            else
            {
                // Logic to highlight bodies in RED if their transform doesn't properly map them, and GREEN if it does
                const checkId = id + "checkTransform";
                const debugColor = checkTransform(context, checkId, selectedTransform, definition.entity, target) ? DebugColor.GREEN : DebugColor.RED;
                addDebugEntities(context, result, debugColor);
                if (debugColor == DebugColor.RED)
                {
                    reportFeatureWarning(context, id, "The specified transform does not map the selected entity to its mirror.");
                }
                toDelete = append(toDelete, qUnion([target, qCreatedBy(checkId, EntityType.BODY), result]));
            }
        }
        else
        {
            addDebugEntities(context, result, DebugColor.BLUE);
        }
    }

    createMirrorMates(context, id + "mirrorMate", definition, selectedTransform, coordSys);

    if (!isQueryEmpty(context, qUnion(toDelete)))
    {
        opDeleteBodies(context, id + "deleteParts", { "entities" : qUnion(toDelete) });
    }
}

/**
 * Returns the mirror `Transform` about `definition.mirrorPlane`.
 */
function getMirrorTransform(context is Context, definition is map) returns Transform
{
    const mirrorPlane = evPlane(context, { "face" : definition.mirrorPlane });
    return mirrorPlane->mirrorAcross()->inverse();
}

/**
 * Pattern copies a part by a given mirror transform.
 */
function getTarget(context is Context, id is Id, part is Query, mirrorTransform is Transform) returns Query
{
    opPattern(context, id + "targetPattern", {
                "entities" : part,
                "transforms" : [mirrorTransform],
                "instanceNames" : ["targetEntity"]
            });
    return qCreatedBy(id + "targetPattern", EntityType.BODY);
}

/**
 * Computes the coord sys to use when transforming and rotating parts.
 */
function getCoordSys(context is Context, id is Id, definition is map) returns CoordSystem
{
    var coordSys = WORLD_COORD_SYSTEM;

    if (definition.customReferenceSystem)
    {
        coordSys = evMateConnector(context, { "mateConnector" : definition.referenceSystem });

        if ((parallelVectors(coordSys.zAxis, WORLD_COORD_SYSTEM.zAxis) || perpendicularVectors(coordSys.zAxis, WORLD_COORD_SYSTEM.zAxis)) &&
            (perpendicularVectors(coordSys.xAxis, WORLD_COORD_SYSTEM.xAxis) || parallelVectors(coordSys.xAxis, WORLD_COORD_SYSTEM.xAxis)))
        {
            reportFeatureInfo(context, id, "The selected reference coordinate system is equivalent to the world coordinate system and will not affect behavior.");
        }
    }

    const partBox = evBox3d(context, {
                "topology" : definition.entity,
                "tight" : true
            });

    coordSys.origin = partBox->box3dCenter();
    return coordSys;
}

/**
 * Returns a `Transform` which applies a given `TransformType`.
 */
function getSelectedTransform(transformType is TransformType, coordSys is CoordSystem, mirrorTransform is Transform)
{
    const linearTransform = getLinearTransform(coordSys, mirrorTransform);
    if (transformType == TransformType.LINEAR)
    {
        return linearTransform;
    }
    else if (transformType == TransformType.Z)
    {
        return rotationAround(line(mirrorTransform * coordSys.origin, coordSys.zAxis), 180 * degree) * linearTransform;
    }
    else if (transformType == TransformType.Y)
    {
        return rotationAround(line(mirrorTransform * coordSys.origin, coordSys->yAxis()), 180 * degree) * linearTransform;
    }
    else if (transformType == TransformType.X)
    {
        return rotationAround(line(mirrorTransform * coordSys.origin, coordSys.xAxis), 180 * degree) * linearTransform;
    }
}

/**
 * Returns a `Transform` mapping a part to its mirrored position linearly.
 */
function getLinearTransform(coordSys is CoordSystem, mirrorTransform is Transform) returns Transform
{
    var targetCoordSys = mirrorTransform * coordSys;
    return rotationAround(line(targetCoordSys.origin, targetCoordSys.zAxis), 180 * degree) * transform(plane(coordSys), plane(targetCoordSys));
}

/**
 * Returns `true` if a given `Transform` succesfully mirrors `part` onto `target`.
 * Returns `false` if `part` is not a solid body.
 */
function checkTransform(context is Context, id is Id, transform is Transform, part is Query, target is Query) returns boolean
{
    // boolean subtract does not work for sheet bodies
    if (!isQueryEmpty(context, part->qBodyType(BodyType.SHEET)))
    {
        return false;
    }

    try
    {
        opPattern(context, id + "targetCopy", {
                    "entities" : target,
                    "transforms" : [identityTransform()],
                    "instanceNames" : ["targetCopy"],
                    "copyPropertiesAndAttributes" : false
                });
        const targetCopy = qCreatedBy(id + "targetCopy", EntityType.BODY);

        opPattern(context, id + "partCopy", {
                    "entities" : part,
                    "transforms" : [transform],
                    "instanceNames" : ["partCopy"],
                    "copyPropertiesAndAttributes" : false
                });

        // evCollision unfortunately doesn't work for detecting perfect overlaps (and also seems to be generally bugged)
        opBoolean(context, id + "subtract", {
                    "tools" : qCreatedBy(id + "partCopy", EntityType.BODY),
                    "targets" : targetCopy,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        return isQueryEmpty(context, targetCopy);
    }
    return false;
}

/**
 * Creates mate connectors relative to a given `coordSys` which are inverses of a given `mateTransform`.
 */
function createMirrorMates(context is Context, id is Id, definition is map, mateTransform is Transform, coordSys is CoordSystem)
{
    var mateCoordSys;
    if (definition.mirrorResultType == MirrorResultType.CENTER)
    {
        opMateConnector(context, id + "startMateConnector", {
                    "coordSystem" : coordSys,
                    "owner" : definition.entity
                });
        mateCoordSys = mateTransform * coordSys;
    }
    else if (definition.mirrorResultType == MirrorResultType.ORIGIN)
    {
        mateCoordSys = mateTransform->inverse() * WORLD_COORD_SYSTEM;
    }

    opMateConnector(context, id + "finalMateConnector", {
                "coordSystem" : mateCoordSys,
                "owner" : definition.entity
            });
}

/**
 * @internal
 * The editing logic function for the `assemblyMirror` feature.
 * This function notably relies heavily on my personal editing logic library (through functions like `isQueryAdded` and `isArrayParamterChanged`)
 * which both help with safely checking and updating array parameters in editing logic.
 */
export function assemblyMirrorEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, specifiedParameters is map) returns map
{
    // Autofill behavior: First plane is used as mirror plane. If multiple faces, first construction face is used and others are disregarded.
    // Remaining entity owner bodies are collected (or bodies adjacent to selections), and the first is added to definition.entity.
    if (oldDefinition == {} && !isQueryEmpty(context, definition.preselection))
    {
        definition = autofillFromPreselection(context, id, definition);
        return definition;
    }

    if (oldDefinition.mirrorPlane != definition.mirrorPlane ||
        oldDefinition.entity != definition.entity ||
        oldDefinition.customCoordSystem != definition.customCoordSystem ||
        oldDefinition.referenceSystem != definition.referenceSystem)
    {
        definition = updateTransform(context, id, definition);
    }
    return definition;
}

function autofillFromPreselection(context is Context, id is Id, definition is map) returns map
{
    const numFaces = size(evaluateQuery(context, definition.preselection->qGeometry(GeometryType.PLANE)));

    var entities = definition.preselection;
    if (numFaces == 1)
    {
        definition.mirrorPlane = definition.preselection->qGeometry(GeometryType.PLANE);
    }
    else if (numFaces > 1)
    {
        const constructionFaces = definition.preselection->qGeometry(GeometryType.PLANE)->qConstructionFilter(ConstructionObject.YES);
        if (!isQueryEmpty(context, constructionFaces))
        {
            definition.mirrorPlane = constructionFaces->qNthElement(0);
        }
    }

    entities = entities->qSubtraction(definition.mirrorPlane);

    // autofill entityArray using preselections
    if (!isQueryEmpty(context, entities))
    {
        for (var entity in evaluateQuery(context, entities))
        {
            if (!isQueryEmpty(context, validMirrorQuery(entity)))
            {
                definition.entity = validMirrorQuery(entity);
                definition = updateTransform(context, id, definition);
                break;
            }
            else if (!isQueryEmpty(context, validMirrorQuery(entity->qOwnerBody())))
            {
                definition.entity = validMirrorQuery(entity->qOwnerBody());
                definition = updateTransform(context, id, definition);
                break;
            }
        }
    }
    return definition;
}

/**
 * Returns a `Query` for a single valid entity to use.
 */
function validMirrorQuery(entity is Query) returns Query
{
    return qUnion(entity->qBodyType(BodyType.SHEET), entity->qBodyType(BodyType.SOLID))->
        qEntityFilter(EntityType.BODY)->
        qConstructionFilter(ConstructionObject.NO)->
        qModifiableEntityFilter()->
        qSketchFilter(SketchObject.NO);
}

/**
 * Given a `Query` for a solid part, checks for a valid `TransformType` which maps part to its mirror.
 * If one is found, `definition` is updated accordingly.
 * @param i {number} : The index into `entityArray` to use.
 */
function updateTransform(context is Context, id is Id, definition is map) returns map
{
    id = id + "computeTransform";
    if (isQueryEmpty(context, definition.entity->qBodyType(BodyType.SOLID)) ||
        isQueryEmpty(context, definition.mirrorPlane) ||
        (definition.customReferenceSystem && isQueryEmpty(context, definition.referenceSystem)))
    {
        return definition;
    }

    const mirrorTransform = getMirrorTransform(context, definition);
    const coordSys = getCoordSys(context, id, definition);
    const target = getTarget(context, id, definition.entity, mirrorTransform);

    const transformTypes = [TransformType.LINEAR, TransformType.Z, TransformType.Y, TransformType.X];

    for (var j, transformType in transformTypes)
    {
        const transform = getSelectedTransform(transformType, coordSys, mirrorTransform);
        if (checkTransform(context, id + ("transformType" ~ j), transform, definition.entity, target))
        {
            definition.transformType = transformType;
            return definition;
        }
    }

    definition.transformType = TransformType.LINEAR;
    return definition;
}
