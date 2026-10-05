FeatureScript 2960;
/**
 * A predicate and related utilities to add a custom starting offset to features.
 *
 * The logic is similar in nature to the version provided in Onshape's extrude feature, with a few tweaks:
 * Support is provided for specifying both a start offset distance as well as a reference at the same time.
 * Entities are always projected onto the start offset reference, rather than being simply transformed based on the first selection.
 */
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "0103ad63394d7713fbf44448", version : "d9ead1a79bded860ba8f3ddf");

/**
 * A custom start offset predicate based on the std extrude start offset.
 */
export predicate startOffsetPredicate(definition is map)
{
    annotation { "Name" : "Starting offset" }
    definition.startOffset is boolean;

    if (definition.startOffset)
    {
        annotation { "Group Name" : "Starting offset", "Collapsed By Default" : false, "Driving Parameter" : "startOffset" }
        {
            annotation { "Name" : "Depth" }
            isLength(definition.startOffsetDistance, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.startOffsetOppositeDirection is boolean;

            annotation { "Name" : "Start offset reference", "UIHInt" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.startOffsetReference is boolean;

            if (definition.startOffsetReference)
            {
                annotation { "Name" : "Reference entity",
                            "Filter" : (GeometryType.PLANE && EntityType.FACE) || EntityType.EDGE || EntityType.VERTEX || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 1,
                            "Description" : "A reference point, planar edge, or plane to measure the offset from." }
                definition.startOffsetEntity is Query;
            }
        }
    }
}

/**
 * Applies the start offset to the given plane.
 *
 * @param pointDistances {array}: An array of `ValueWithUnits` representing the distances of each point from the `basePlane`.
 */
export function applyStartOffset(context is Context, definition is map, basePlane is Plane) returns Plane
{
    if (!definition.startOffset)
    {
        return basePlane;
    }

    var distance = getStartOffsetDistance(definition);

    if (definition.startOffsetReference)
    {
        checkStartOffsetEntity(context, definition, basePlane.normal);

        // We take the minimum distance between the start offset entity and the basePlane along the plane normal
        const distanceResult = evDistance(context, {
                    "side0" : basePlane.origin,
                    "side1" : definition.startOffsetEntity
                });
        const distanceVector = distanceResult.sides[1].point - basePlane.origin;
        // Compute length of distanceVector when projected onto basePlane.normal
        distance += dot(distanceVector, basePlane.normal) / squaredNorm(basePlane.normal);
    }

    basePlane.origin += basePlane.normal * distance;
    return basePlane;
}

function getStartOffsetDistance(definition is map) returns ValueWithUnits
{
    return definition.startOffsetDistance * (definition.startOffsetOppositeDirection ? -1 : 1);
}

const START_OFFSET_MANIPULATOR = "startOffsetManipulator";

/**
 * Adds a start offset linear manipulator to the context.
 */
export function addStartOffsetManipulator(context is Context, id is Id, definition is map, plane is Plane)
{
    if (!definition.startOffset)
    {
        return;
    }
    const offsetDistance = getStartOffsetDistance(definition);
    const base = plane.origin - plane.normal * offsetDistance;
    addManipulators(context, id, {
                (START_OFFSET_MANIPULATOR) : linearManipulator({
                        "base" : base,
                        "direction" : plane.normal,
                        "offset" : offsetDistance,
                        "primaryParameterId" : "startOffsetDistance"
                    })
            });
}

export function startOffsetManipulatorChange(definition is map, newManipulators is map)
{
    const manipulator = newManipulators[START_OFFSET_MANIPULATOR];
    if (manipulator == undefined)
    {
        return definition;
    }
    definition.startOffsetDistance = abs(manipulator.offset);
    definition.startOffsetOppositeDirection = manipulator.offset < 0;
    return definition;
}

function checkPlaneParallel(context is Context, entity is Query, planeNormal is Vector)
{
    // If we have an entity that is an edge or a face, it needs to be planar
    const edge = qEntityFilter(entity, EntityType.EDGE);
    const face = qEntityFilter(entity, EntityType.FACE);
    var plane;
    if (!isQueryEmpty(context, edge))
    {
        try silent
        {
            plane = evPlanarEdge(context, { "edge" : edge });
        }
    }
    else if (!isQueryEmpty(context, face))
    {
        try silent
        {
            plane = evPlane(context, { "face" : face });
        }
    }
    else
    {
        // We have a vertex or plane connector, we don't need to check for a plane
        return;
    }
    if (plane == undefined)
    {
        throw regenError(ErrorStringEnum.EXTRUDE_START_OFFSET_BOUND_NOT_PLANAR, ["startOffsetEntity"], entity);
    }
    // If we have a planar entity, its plane needs to be parallel to the extruded profiles
    if (!parallelVectors(plane.normal, planeNormal))
    {
        throw regenError(ErrorStringEnum.EXTRUDE_START_OFFSET_BOUND_NOT_PARALLEL_TO_EXTRUDED_ENTITIES, ["startOffsetEntity"], entity);
    }
}

function checkLineParallel(context is Context, edge is Query, planeNormal is Vector) returns boolean
{
    // If the entity is a line, it needs to be normal to the extruded profile's plane normal
    var line;
    try silent
    {
        line = evLine(context, { "edge" : edge });
    }
    if (line == undefined)
    {
        // The entity is not a line
        return false;
    }
    if (!tolerantEquals(dot(line.direction, planeNormal), 0))
    {
        throw regenError(ErrorStringEnum.EXTRUDE_START_OFFSET_BOUND_NOT_PARALLEL_TO_EXTRUDED_ENTITIES, ["startOffsetEntity"], edge);
    }
    // The entity is a line and it's a valid offset entity
    return true;
}

function checkStartOffsetEntity(context is Context, definition is map, planeNormal is Vector)
{
    if (!definition.startOffset || !definition.startOffsetReference)
    {
        return;
    }
    // We need an entity to offset up to.
    if (isQueryEmpty(context, definition.startOffsetEntity))
    {
        // ErrorStringEnum.EXTRUDE_SELECT_START_OFFSET_ENTITY
        throw regenError("Select a valid start offset entity.", ["startOffsetEntity"]);
    }

    if (checkLineParallel(context, definition.startOffsetEntity, planeNormal))
    {
        return;
    }
    checkPlaneParallel(context, definition.startOffsetEntity, planeNormal);
}