FeatureScript 2960;

/**
 * A collection of predicates and functions for features which cut standard mounting patterns in parts.
 *
 * Designed to be used with `holeMergeScopePredicate` from `stdHole.fs` and `location.fs`, although not every function requires those features.
 *
 * The proper order call the functions in this module is:
 * applyAngleReference
 * applyAxisOrientation
 * addAngleOffsetManipulator
 * applyAngleOffset
 */
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");
import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");

import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");
import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");

/**
 * Creates two buttons for flipping and rotating in 90 degree increments about the selected location.
 * Parameters have the names `oppositeDirection` and `secondaryAxisType`.
 */
export predicate axisOrientationPredicate(definition is map)
{
    annotation { "Name" : "Flip primary axis", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
    definition.oppositeDirection is boolean;

    secondaryAxisPredicate(definition);
}

/**
 * Creates the button for rotating in 90 degree increments from `axisOrientationPredicate`, for features which
 * already have an `oppositeDirection` parameter (such as an extrude's) to put it next to.
 */
export predicate secondaryAxisPredicate(definition is map)
{
    annotation { "Name" : "Reorient secondary axis", "UIHint" : UIHint.MATE_CONNECTOR_AXIS_TYPE, "Default" : MateConnectorAxisType.PLUS_X }
    definition.secondaryAxisType is MateConnectorAxisType;
}

/**
 * Allows selecting an angle reference.
 */
export predicate angleReferencePredicate(definition is map)
{
    annotation { "Name" : "Angle reference", "Filter" : EntityType.VERTEX || GeometryType.LINE, "MaxNumberOfPicks" : 1 }
    definition.angleReference is Query;
}


/**
 * Allows selecting an angle offset.
 */
export predicate angleOffsetPredicate(definition is map)
{
    annotation { "Name" : "Angle" }
    isAngle(definition.angleOffset, ANGLE_360_ZERO_DEFAULT_BOUNDS);

    annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.angleOffsetOppositeDirection is boolean;
}


/**
 * Applies the user's selected angle reference.
 */
export function applyAngleReference(context is Context, definition is map, plane is Plane, locationParameterName is string) returns Plane
{
    const angleRef = definition.angleReference->qNthElement(0);
    if (isQueryEmpty(context, angleRef))
    {
        return plane;
    }

    const isVertex = !isQueryEmpty(context, angleRef->qEntityFilter(EntityType.VERTEX));
    if (isVertex)
    {
        const point = evVertexPoint(context, { "vertex" : angleRef });
        const projectedPoint = project(plane, point);
        if (tolerantEquals(projectedPoint, plane.origin))
        {
            throw regenError("The selected angle reference cannot be coincident with your selection.", [locationParameterName, "angleReference"], qUnion(getParameter(definition, locationParameterName), angleRef));
        }
        plane.x = normalize(projectedPoint - plane.origin);
        return plane;
    }

    const isLine = !isQueryEmpty(context, angleRef->qGeometry(GeometryType.LINE));
    if (isLine)
    {
        const direction = extractDirection(context, angleRef);
        if (!perpendicularVectors(direction, plane.normal))
        {
            throw regenError("The selected angle reference must be parallel your selection.", [locationParameterName, "angleReference"], qUnion(getParameter(definition, locationParameterName), angleRef));
        }
        plane.x = direction;
        return plane;
    }

    throw regenError("The selected angle reference is not a point or line.", ["angleReference"], definition.angleReference);
}

/**
 * Applies an angleReference. Assumes `locationPredicate` is also being used.
 */
export function applyAngleReference(context is Context, definition is map, plane is Plane) returns Plane
{
    return applyAngleReference(context, definition, plane, "location");
}

export function applyAxisOrientation(definition is map, startPlane is Plane) returns Plane
{
    var xAxis = startPlane.x;
    var zAxis = startPlane.normal * (definition.oppositeDirection ? -1 : 1);
    if (definition.secondaryAxisType != undefined)
    {
        if (definition.secondaryAxisType == MateConnectorAxisType.PLUS_Y)
        {
            xAxis = cross(zAxis, xAxis);
        }
        else if (definition.secondaryAxisType == MateConnectorAxisType.MINUS_X)
        {
            xAxis = -xAxis;
        }
        else if (definition.secondaryAxisType == MateConnectorAxisType.MINUS_Y)
        {
            xAxis = -cross(zAxis, xAxis);
        }
    }
    return plane(startPlane.origin, zAxis, xAxis);
}


/**
 * Reduce from [-2 pi, +2 pi] to [-pi, +pi] radians
 * Borrowed from Onshape's transformCopy feature.
 */
function reduceAngle(angle is ValueWithUnits) returns ValueWithUnits
precondition
{
    isAngle(angle);
}
{
    const CIRCLE = 2 * PI * radian;
    const REDUCE_ADD = 3 * PI * radian;
    const REDUCE_SUB = PI * radian;
    return (angle + REDUCE_ADD) % CIRCLE - REDUCE_SUB;
}

/**
 * Applies the user specified angleOffset.
 * Call this function AFTER `addAngleOffsetManipulator`.
 */
export function applyAngleOffset(definition is map, plane is Plane) returns Plane
{
    const angle = reduceAngle(definition.angleOffset) * (definition.angleOffsetOppositeDirection ? -1 : 1);
    const transform = rotationAround(line(plane.origin, plane.normal), angle);
    return transform * plane;
}

const ANGLE_OFFSET_MANIPULATOR = "angleOffsetManipulator";

/**
 * Adds the angle offset manipulator.
 * Call this function BEFORE `applyAngleOffset`.
 */
export function addAngleOffsetManipulator(context is Context, id is Id, definition is map, plane is Plane, radius is ValueWithUnits)
precondition
{
    isTopLevelId(id);
}
{
    const angle = reduceAngle(definition.angleOffset) * (definition.angleOffsetOppositeDirection ? -1 : 1);
    addManipulators(context, id, {
                (ANGLE_OFFSET_MANIPULATOR) : angularManipulator({
                        "axisOrigin" : plane.origin,
                        "axisDirection" : plane.normal,
                        "rotationOrigin" : plane.origin + plane.x * radius,
                        "angle" : angle,
                        "primaryParameterId" : "angleOffset"
                    })
            });
}

export function angleOffsetManipulatorChange(definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[ANGLE_OFFSET_MANIPULATOR];
    if (manipulator == undefined)
    {
        return definition;
    }
    const angle = reduceAngle(manipulator.angle);
    definition.angleOffset = abs(angle);
    definition.angleOffsetOppositeDirection = angle < 0 * radian;
    return definition;
}

/**
 * A shim between std hole scope and flip editing logic in `stdHole.fs` and `location.fs`.
 */
export function mountingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map, hiddenBodies is Query) returns map
{
    const location = try silent(sketchHoleLocation(context, id, getLocationPlane(context, definition))) ?? qNothing();
    const oldLocation = try silent(sketchHoleLocation(context, id, getLocationPlane(context, oldDefinition))) ?? qNothing();

    const holeDefinition = {
            "locations" : location,
            "scope" : definition.scope,
            "oppositeDirection" : definition.oppositeDirection,
            "startStyle" : HoleStartStyle.SKETCH,
            "endStyle" : HoleEndStyle.THROUGH
        };

    const oldHoleDefinition = oldDefinition == {} ? {} : {
                "locations" : oldLocation,
                "scope" : oldDefinition.scope,
                "oppositeDirection" : oldDefinition.oppositeDirection,
                "startStyle" : HoleStartStyle.SKETCH,
                "endStyle" : HoleEndStyle.THROUGH
            };

    const newHoleDefinition = holeScopeFlipHeuristicsCall(context, oldHoleDefinition, holeDefinition, specifiedParameters, hiddenBodies);
    definition.scope = newHoleDefinition.scope;
    definition.oppositeDirection = newHoleDefinition.oppositeDirection;
    return definition;
}
