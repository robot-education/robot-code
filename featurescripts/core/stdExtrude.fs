FeatureScript 2960;
/**
 * Defines portions of the extrude UI useful to robot features.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/extrudeCommon.fs", version : "2960.0");
export import(path : "onshape/std/extrude.fs", version : "2960.0");


/**
 * A predicate for most generic extrude parameters with the exception of draft and extrude direction: `extrudeEndPredicate`,
 * `extrudeOffsetPredicate`, and `extrudeOptionsPredicate`, which features can call themselves to put parameters of
 * their own between them.
 */
export predicate extrudePredicate(definition is map)
{
    extrudeEndPredicate(definition);

    extrudeOffsetPredicate(definition);

    extrudeOptionsPredicate(definition);
}

/**
 * The end type of a generic extrude, the opposite direction button next to it, and its bounds.
 */
export predicate extrudeEndPredicate(definition is map)
{
    annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.endBound is BoundingType;

    annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.oppositeDirection is boolean;

    extrudeBoundsPredicate(definition);
}

/**
 * Symmetric, and the second end position, of a generic extrude.
 */
export predicate extrudeOptionsPredicate(definition is map)
{
    if (definition.endBound == BoundingType.BLIND || definition.endBound == BoundingType.THROUGH_ALL)
    {
        annotation { "Name" : "Symmetric", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.symmetric is boolean;
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
                annotation { "Name" : "End type", "Column Name" : "Second end type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.secondDirectionBound is BoundingType;

                annotation { "Name" : "Opposite direction", "Column Name" : "Second opposite direction",
                            "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : true }
                definition.secondDirectionOppositeDirection is boolean;

                extrudeSecondBoundsPredicate(definition);
            }
        }
    }
}

/**
 * Std's `extrudeBoundParametersPredicate` (in `extrudeCommon.fs`) for a `BoundingType`, without field tolerancing,
 * which our parameters never allow (see docs/featurescript-style.md).
 */
export predicate extrudeBoundsPredicate(definition is map)
{
    if (definition.endBound == BoundingType.BLIND)
    {
        annotation { "Name" : "Depth" }
        isLength(definition.depth, LENGTH_BOUNDS);
    }
    else if (definition.endBound == BoundingType.UP_TO_SURFACE)
    {
        annotation { "Name" : "Up to face",
                    "Filter" : (EntityType.FACE && SketchObject.NO && AllowMeshGeometry.YES) || BodyType.MATE_CONNECTOR,
                    "MaxNumberOfPicks" : 1 }
        definition.endBoundEntityFace is Query;
    }
    else if (definition.endBound == BoundingType.UP_TO_BODY)
    {
        annotation { "Name" : "Up to surface or part",
                    "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && SketchObject.NO && AllowMeshGeometry.YES,
                    "MaxNumberOfPicks" : 1 }
        definition.endBoundEntityBody is Query;
    }
    else if (definition.endBound == BoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Up to vertex or mate connector", "Filter" : QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
        definition.endBoundEntityVertex is Query;
    }

    if (definition.endBound == BoundingType.UP_TO_NEXT || definition.endBound == BoundingType.UP_TO_SURFACE ||
        definition.endBound == BoundingType.UP_TO_BODY || definition.endBound == BoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Offset distance", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasOffset is boolean;

        if (definition.hasOffset)
        {
            annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.offsetDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.offsetOppositeDirection is boolean;
        }
    }
}

/**
 * Std's `extrudeSecondDirectionBoundParametersPredicate` for a `BoundingType`, without field tolerancing.
 */
export predicate extrudeSecondBoundsPredicate(definition is map)
{
    if (definition.secondDirectionBound == BoundingType.BLIND)
    {
        annotation { "Name" : "Depth", "Column Name" : "Second depth" }
        isLength(definition.secondDirectionDepth, LENGTH_BOUNDS);
    }
    else if (definition.secondDirectionBound == BoundingType.UP_TO_SURFACE)
    {
        annotation { "Name" : "Up to face", "Column Name" : "Second up to face",
                    "Filter" : (EntityType.FACE && SketchObject.NO && AllowMeshGeometry.YES) || BodyType.MATE_CONNECTOR,
                    "MaxNumberOfPicks" : 1 }
        definition.secondDirectionBoundEntityFace is Query;
    }
    else if (definition.secondDirectionBound == BoundingType.UP_TO_BODY)
    {
        annotation { "Name" : "Up to surface or part", "Column Name" : "Second up to surface or part",
                    "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && SketchObject.NO && AllowMeshGeometry.YES,
                    "MaxNumberOfPicks" : 1 }
        definition.secondDirectionBoundEntityBody is Query;
    }
    else if (definition.secondDirectionBound == BoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Up to vertex or mate connector", "Column Name" : "Second up to vertex or mate connector",
                    "Filter" : QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
        definition.secondDirectionBoundEntityVertex is Query;
    }

    if (definition.secondDirectionBound == BoundingType.UP_TO_NEXT || definition.secondDirectionBound == BoundingType.UP_TO_SURFACE ||
        definition.secondDirectionBound == BoundingType.UP_TO_BODY || definition.secondDirectionBound == BoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Offset distance", "Column Name" : "Second direction has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasSecondDirectionOffset is boolean;

        if (definition.hasSecondDirectionOffset)
        {
            annotation { "Name" : "Offset distance", "Column Name" : "Second offset distance", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.secondDirectionOffsetDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Second offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.secondDirectionOffsetOppositeDirection is boolean;
        }
    }
}

/**
 * The end types of an extrude of a part cut from stock, like a shaft, spacer, or frame: std's, but for Through all
 * and Up to part. Stock is cut flat, so its ends must be flat: up to faces must be planar (see `verifyFlatEnds`).
 * Std's extrude takes them as `BoundingType`s (see `transformDefintionForNewExtrude`).
 */
export enum StockBoundingType
{
    annotation { "Name" : "Blind" }
    BLIND,
    annotation { "Name" : "Up to next" }
    UP_TO_NEXT,
    annotation { "Name" : "Up to face" }
    UP_TO_SURFACE,
    annotation { "Name" : "Up to vertex" }
    UP_TO_VERTEX
}

/**
 * A predicate for common std extrude parameters relevant to an extrude of stock (which only creates new parts), with
 * the end types of `StockBoundingType`.
 *
 * @seealso `transformDefinitionForNewExtrude`
 */
export predicate newExtrudePredicate(definition is map)
{
    newExtrudeEndTypePredicate(definition);

    newExtrudeBoundsPredicate(definition);
}

/**
 * The first part of `newExtrudePredicate`: the end type and the opposite direction button next to it.
 * Split out so features can put buttons of their own next to it.
 */
export predicate newExtrudeEndTypePredicate(definition is map)
{
    annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.endBound is StockBoundingType;

    annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.oppositeDirection is boolean;
}

/**
 * The rest of `newExtrudePredicate`, after `newExtrudeEndTypePredicate`.
 */
export predicate newExtrudeBoundsPredicate(definition is map)
{
    if (definition.endBound == StockBoundingType.BLIND)
    {
        annotation { "Name" : "Depth" }
        isLength(definition.depth, LENGTH_BOUNDS);
    }
    upToBoundParametersPredicate(definition);

    newExtrudeOptionsPredicate(definition);
}

/**
 * The bounds of `newExtrudeBoundsPredicate`, with `depth` named Length, for stock placed by length (see linearStock.fs).
 */
export predicate lengthBoundParametersPredicate(definition is map)
{
    if (definition.endBound == StockBoundingType.BLIND)
    {
        annotation { "Name" : "Length" }
        isLength(definition.depth, LENGTH_BOUNDS);
    }
    upToBoundParametersPredicate(definition);
}

/**
 * The bounds of std's `extrudeBoundParametersPredicate` other than Blind's depth, for a `StockBoundingType`, without
 * tolerances: up to a planar face or vertex, and an offset.
 */
export predicate upToBoundParametersPredicate(definition is map)
{
    if (definition.endBound == StockBoundingType.UP_TO_SURFACE)
    {
        annotation { "Name" : "Up to face",
                    "Filter" : (EntityType.FACE && GeometryType.PLANE && SketchObject.NO) || BodyType.MATE_CONNECTOR,
                    "MaxNumberOfPicks" : 1 }
        definition.endBoundEntityFace is Query;
    }
    else if (definition.endBound == StockBoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Up to vertex or mate connector", "Filter" : QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
        definition.endBoundEntityVertex is Query;
    }

    if (definition.endBound != StockBoundingType.BLIND)
    {
        annotation { "Name" : "Offset distance", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasOffset is boolean;

        if (definition.hasOffset)
        {
            annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.offsetDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.offsetOppositeDirection is boolean;
        }
    }
}

/**
 * The options of a new extrude after its bounds: starting offset, symmetric, and second end position.
 */
export predicate newExtrudeOptionsPredicate(definition is map)
{
    extrudeOffsetPredicate(definition);

    if (definition.endBound == StockBoundingType.BLIND)
    {
        annotation { "Name" : "Symmetric", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.symmetric is boolean;
    }

    if (!(definition.endBound == StockBoundingType.BLIND && definition.symmetric))
    {
        annotation { "Name" : "Second end position",
                    "UIHint" : UIHint.FIRST_IN_ROW }
        definition.hasSecondDirection is boolean;

        annotation { "Group Name" : "Second end position", "Driving Parameter" : "hasSecondDirection", "Collapsed By Default" : false }
        {
            if (definition.hasSecondDirection)
            {
                annotation { "Name" : "End type", "Column Name" : "Second end type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.secondDirectionBound is StockBoundingType;

                annotation { "Name" : "Opposite direction", "Column Name" : "Second opposite direction",
                            "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : true }
                definition.secondDirectionOppositeDirection is boolean;

                secondDirectionBoundParametersPredicate(definition);
            }
        }
    }
}

/**
 * Std's `extrudeSecondDirectionBoundParametersPredicate` for a `StockBoundingType`, without tolerances.
 */
export predicate secondDirectionBoundParametersPredicate(definition is map)
{
    if (definition.secondDirectionBound == StockBoundingType.BLIND)
    {
        annotation { "Name" : "Depth", "Column Name" : "Second depth" }
        isLength(definition.secondDirectionDepth, LENGTH_BOUNDS);
    }
    else if (definition.secondDirectionBound == StockBoundingType.UP_TO_SURFACE)
    {
        annotation { "Name" : "Up to face", "Column Name" : "Second up to face",
                    "Filter" : (EntityType.FACE && GeometryType.PLANE && SketchObject.NO) || BodyType.MATE_CONNECTOR,
                    "MaxNumberOfPicks" : 1 }
        definition.secondDirectionBoundEntityFace is Query;
    }
    else if (definition.secondDirectionBound == StockBoundingType.UP_TO_VERTEX)
    {
        annotation { "Name" : "Up to vertex or mate connector", "Column Name" : "Second up to vertex or mate connector",
                    "Filter" : QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
        definition.secondDirectionBoundEntityVertex is Query;
    }

    if (definition.secondDirectionBound != StockBoundingType.BLIND)
    {
        annotation { "Name" : "Offset distance", "Column Name" : "Second direction has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
        definition.hasSecondDirectionOffset is boolean;

        if (definition.hasSecondDirectionOffset)
        {
            annotation { "Name" : "Offset distance", "Column Name" : "Second offset distance", "UIHint" : ["DISPLAY_SHORT"] }
            isLength(definition.secondDirectionOffsetDistance, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "Column Name" : "Second offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            definition.secondDirectionOffsetOppositeDirection is boolean;
        }
    }
}

/**
 * Throws an error unless the ends of the stock extruded by `id` (its caps) are flat, as when it's extruded up to the
 * next face and that face is curved.
 */
export function verifyFlatEnds(context is Context, id is Id, body is Query)
{
    for (var capType in [CapType.START, CapType.END])
    {
        const cap = qCapEntity(id, capType, EntityType.FACE)->qOwnedByBody(body);
        if (!isQueryEmpty(context, cap) && size(evaluateQuery(context, cap)) != size(evaluateQuery(context, cap->qGeometry(GeometryType.PLANE))))
        {
            throw regenError("The ends must be flat: extrude up to a planar face.", ["endBound"], cap);
        }
    }
}

/**
 * Copied from `extrude.fs`, which std's extrude reads: `hasExtrudeDirection` and `extrudeDirection`.
 */
export predicate extrudeDirectionPredicate(definition is map)
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

/**
 * Copied from `extrude.fs`.
 */
export predicate extrudeOffsetPredicate(definition is map)
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

/**
 * Copied from `extrude.fs`.
 * Applies the user selected extrude direction to `planeNormal`.
 */
export function processExtrudeDirection(context is Context, definition is map, planeNormal is Vector) returns Vector
{
    if (!(definition.hasExtrudeDirection ?? false) || isQueryEmpty(context, definition.extrudeDirection ?? qNothing()))
    {
        return planeNormal;
    }
    const userProvidedExtrudeDirection = extractDirection(context, definition.extrudeDirection);
    // Makes sure the direction picked by the user aligns with the original extrude direction to avoid flips
    return dot(userProvidedExtrudeDirection, planeNormal) < 0 ? -userProvidedExtrudeDirection : userProvidedExtrudeDirection;
}

/**
 * The plane at `profilePlane`'s origin normal to the extrude's direction (see `processExtrudeDirection`), with X as
 * close to `profilePlane`'s as it can be. Sketching a profile on it extrudes it straight along the direction; std's
 * extrude doesn't allow a direction parallel to the profile, as a direction in `profilePlane` would be.
 */
export function extrudeDirectionPlane(context is Context, definition is map, profilePlane is Plane) returns Plane
{
    const direction = processExtrudeDirection(context, definition, profilePlane.normal);
    const projectedX = profilePlane.x - direction * dot(profilePlane.x, direction);
    const xAxis = tolerantEquals(norm(projectedX), 0) ? perpendicularVector(direction) : normalize(projectedX);
    return plane(profilePlane.origin, direction, xAxis);
}

/**
 * A wrapper for extrude logic.
 * Note `definition.entities` should be set beforehand.
 */
export function stdExtrudeEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.domain = OperationDomain.MODEL;
    definition.bodyType = ExtendedToolBodyType.SOLID;
    if (definition.entities == undefined)
    {
        definition.entities = qNothing();
    }
    definition = extrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
    definition.domain = undefined;
    definition.bodyType = undefined;
    definition.entities = undefined;
    return definition;
}

/**
 * For a new extrude, sets `definition` parameters so `extrude` works as expected.
 */
export function transformDefintionForNewExtrude(definition is map, entities is Query) returns map
{
    definition.endBound = definition.endBound as BoundingType;
    definition.secondDirectionBound = definition.secondDirectionBound as BoundingType;
    definition.entities = entities;
    return definition;
}

/**
 * A wrapper for extrude logic for a new extrude.
 * Note `definition.entities` should be set beforehand.
 *
 * @seealso `newExtrudePredicate`
 */
export function stdNewExtrudeEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    // Std's logic knows its own end types, which are named the same
    definition.endBound = definition.endBound as SMExtrudeBoundingType;
    definition.secondDirectionBound = definition.secondDirectionBound as SMExtrudeBoundingType;
    if (canSetExtrudeFlips(definition, specifiedParameters))
    {
        if (canSetExtrudeUpToFlip(definition, specifiedParameters))
        {
            definition = upToBoundaryFlip(context, definition);
        }
    }
    definition = setExtrudeSecondDirectionFlip(definition, specifiedParameters);

    definition.entities = undefined;
    definition.endBound = definition.endBound as StockBoundingType;
    definition.secondDirectionBound = definition.secondDirectionBound as StockBoundingType;
    return definition;
}

function upToBoundaryFlip(context is Context, definition is map) returns map
{
    const usedEntities = getEntitiesToUse(context, definition);
    const resolvedEntities = evaluateQuery(context, usedEntities);
    if (size(resolvedEntities) == 0)
    {
        return definition;
    }
    const profilePlaneNormal = computeProfilePlaneNormal(context, definition, resolvedEntities[0], definition.transform);
    if (profilePlaneNormal == undefined)
    {
        return definition;
    }
    return extrudeUpToBoundaryFlipCommon(context, profilePlaneNormal, definition);
}

/**
 * Copied from `extrude.fs`.
 */
function getEntitiesToUse(context is Context, definition is map) returns Query
{
    verifyNoMesh(context, definition, "entities");
    return definition.entities;
}

/**
 * copied from `extrude.fs`.
 */
function computeProfilePlaneNormal(context is Context, definition is map, entity is Query, transform)
precondition
{
    transform is undefined || transform is Transform;
}
{
    const planes = evaluateQuery(context, qGeometry(entity, GeometryType.PLANE));

    var extrudeAxis;
    if (size(planes) >= 1)
    {
        const entityPlane = evPlane(context, { "face" : planes[0] });
        extrudeAxis = line(entityPlane.origin, entityPlane.normal);
        if (transform == undefined || transform == identityTransform())
            return extrudeAxis;
        else
            return transform * extrudeAxis;
    }
    else
    {
        //The extrude axis should start in the middle of the edge and point in the sketch plane normal
        const tangentAtEdge = evEdgeTangentLine(context, { "edge" : entity, "parameter" : 0.5 });
        const entityPlane = evOwnerSketchPlane(context, { "entity" : entity });
        var direction = entityPlane.normal;
        if (transform != undefined && transform != identityTransform())
        {
            // More recently the sketch plane IS transformed so we don't need the full transform
            // but we still need to transform the sketch normal by the provided transform
            direction = transform.linear * entityPlane.normal;
        }
        //make sure to handle the origin and transform with remainder transform passed in
        if (transform == undefined || transform == identityTransform())
            return line(tangentAtEdge.origin, direction);
        else
            return line(transform * tangentAtEdge.origin, direction);
    }
}
