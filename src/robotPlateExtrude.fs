FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");
import(path : "a31342637c8f0fafa3d91dec", version : "38b5e46c310f2ee3b6ee8519");

export enum PlateBoundingType
{
    annotation { "Name" : "Blind" }
    BLIND,
    annotation { "Name" : "Up to" }
    UP_TO,
    annotation { "Name" : "Through all" }
    THROUGH_ALL
}

/**
 * Computes the platePlane, oppositePlane, and depth of the plate.
 */
export function getPlateInfo(context is Context, definition is map) returns map
{
    verifyNonemptyQuery(context, definition, "platePlane", customError(CustomError.SELECT_PLATE_PLANE));

    var platePlane = evPlane(context, { "face" : definition.platePlane });

    var depth;
    if (definition.endBound == PlateBoundingType.BLIND)
    {
        depth = definition.depth;
    }
    else if (definition.endBound == PlateBoundingType.THROUGH_ALL)
    {
        if (isQueryEmpty(context, qAllModifiableSolidBodies()))
        {
            throw regenError(ErrorStringEnum.CANNOT_BE_EMPTY, ["endBoundEntity"]);
        }

        depth = evBox3d(context, {
                        "topology" : qAllModifiableSolidBodies(),
                        "tight" : false
                    })->box3dDiagonalLength();
    }
    else if (definition.endBound == PlateBoundingType.UP_TO)
    {
        verifyEndBoundEntity(context, definition, platePlane);
        depth = evDistance(context, {
                        "side0" : platePlane,
                        "side1" : definition.endBoundEntity
                    }).distance;

        if (definition.hasOffset)
        {
            depth += definition.offsetDistance * (definition.offsetOppositeDirection ? -1 : 1);
        }
        depth *= definition.symmetric ? 2 : 1;
    }

    platePlane = adjustPlatePlane(context, definition, depth, platePlane);

    return {
            "depth" : depth,
            "platePlane" : platePlane,
            "oppositePlane" : getOppositePlane(depth, platePlane)
        };
}

/**
 * Adjusts the plate plane to its final position.
 */
function adjustPlatePlane(context is Context, definition is map, depth is ValueWithUnits, platePlane is Plane)
{
    platePlane.normal *= (definition.plateOppositeDirection ? -1 : 1);
    platePlane.origin -= platePlane.normal * (definition.symmetric ? depth / 2 : 0 * meter);
    return platePlane;
}

function getOppositePlane(depth is ValueWithUnits, platePlane is Plane) returns Plane
{
    return plane(platePlane.origin + platePlane.normal * depth, -platePlane.normal, platePlane.x);
}

/**
 * Returns true if endBoundEntity is planar and parallel to platePlane.
 */
export predicate hasValidEndBoundEntity(context is Context, definition is map, platePlane is Plane)
{
    (!isQueryEmpty(context, definition.endBoundEntity->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.PLANE)) &&
                !isQueryEmpty(context, qParallelPlanes(definition.endBoundEntity, platePlane, true))) ||
        !isQueryEmpty(context, definition.endBoundEntity->qEntityFilter(EntityType.VERTEX)) ||
        !isQueryEmpty(context, definition.endBoundEntity->qBodyType(BodyType.MATE_CONNECTOR));

    isQueryEmpty(context, qCoincidesWithPlane(definition.endBoundEntity, platePlane));
}

function verifyEndBoundEntity(context is Context, definition is map, platePlane is Plane)
{
    if (definition.endBound == PlateBoundingType.UP_TO)
    {
        definition.endBoundEntity = definition.endBoundEntity->qNthElement(0);
        if (!isQueryEmpty(context, qCoincidesWithPlane(definition.endBoundEntity, platePlane)))
        {
            throw platePlaneAwareRegenError(definition, "endBoundEntity", customError(CustomError.EXTRUDE_UP_TO_COINCIDENT));
        }
        // entity is a non-parallel plane
        else if (!isQueryEmpty(context, definition.endBoundEntity->qEntityFilter(EntityType.FACE)) &&
            isQueryEmpty(context, qParallelPlanes(definition.endBoundEntity, platePlane, true)))
        {
            throw platePlaneAwareRegenError(definition, "endBoundEntity", customError(CustomError.EXTRUDE_UP_TO_NOT_PARALLEL));
        }
    }
}

/**
 * Allows errors to highlight the underlying source of a plate plane appropriately. In particular,
 * highlights platePlane if a platePlane is selected. Otherwise, throws normally.
 */
export function platePlaneAwareRegenError(definition is map, faultyParameter is string, errorString is string)
{
    throw regenError(errorString, [faultyParameter, "platePlane"], definition[faultyParameter]);
}
