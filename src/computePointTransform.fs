FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "1930.0");
export import(path : "onshape/std/rotationtype.gen.fs", version : "1930.0");

/**
 * Moves entities in a part studio to a specified location.
 *
 * @seealso [instantiatePointDerivePartStudio]
 * @seealso [addPointDeriveManipulator]
 *
 * @param id : @autocomplete `id + "pointTransform"`
 * @param definition {{
 *      @field index {number} :
 *              The index of the currently selected mate connector.
 *              `index` is automatically clamped to the current number of mate connectors.
 *              @autocomplete `definition.index`
 *      @field location {CoordSystem} :
 *              A selected [CoordSystem] to derive entities to.
 *      @field points {array} :
 *              An array of 3D `Vector`s representing user selectable points.
 *      @field rotation {ValueWithUnits} : @optional
 *              The angle to rotate by.
 *              Defaults to `0 * degree`.
 *      @field flipPrimaryAxis {boolean} : @optional
 *              Whether to flip the primary axis of derived parts.
 *              Defaults to `false`.
 *              @autocomplete `false`
 *      @field secondaryAxisType {MateConnectorAxisType} : @optional
 *              The secondary axis to use when orienting derived parts.
 *              Defaults to `MateConnectorAxisType.PLUS_X`.
 *              @autocomplete `MateConnectorAxisType.PLUS_X`
 * }}
 *
 * @returns {{
 *      @field points {array} :
 *              An array of 3D `Vector`s representing the final location of each mate connector in the derived entities,
 *              or `[]` if no such mate connectors exist (and no point manipulator should be created).
 *      @field index {number} :
 *              The index of the currently selected mate connector. May be different from the passed in `index` due
 *              to the need to clamp the index to the actual number of mate connectors attatched to derived entities.
 *      @field transform {Transform} :
 *              The transform which was applied to the entity.
 * }}
 */
export function computePointTransform(definition is map) returns map
precondition
{
    definition.index is number;
    definition.location is CoordSystem;
    definition.points is array || definition.points is undefined;

    definition.rotation is ValueWithUnits || definition.rotation is undefined;
    definition.flipPrimaryAxis is boolean || definition.flipPrimaryAxis is undefined;
    definition.secondaryAxisType is MateConnectorAxisType || definition.secondaryAxisType is undefined;
}
{
    definition = {
                "points" : [],
                "rotationType" : RotationType.ABOUT_Z,
                "rotation" : 0 * degree,
                "flipPrimaryAxis" : false,
                "secondaryAxisType" : MateConnectorAxisType.PLUS_X
            }->mergeMaps(definition);

    var baseTransform = computeTransform(definition);

    var pointTransform = identityTransform();
    if (definition.points != [])
    {
        definition.index = clamp(definition.index, 0, size(definition.points) - 1);

        // transform from start point to center of points
        pointTransform = transform(definition.points[definition.index])->inverse();

        definition.points = mapArray(definition.points, function(point is Vector)
            {
                return toWorld(definition.location) * baseTransform * pointTransform * point;
            });
    }

    const transform = toWorld(definition.location) * baseTransform * pointTransform;

    // opTransform(context, id, { "bodies" : definition.entities, "transform" : transform });

    return { "points" : definition.points, "index" : definition.index, "transform" : transform }; // "baseTransform" : toWorld(definition.location)
}

/**
 * Computes the transform of the `primaryAxis` and `secondaryAxisType` options.
 */
function computeTransform(definition is map) returns Transform
{
    var planeToUse = XY_PLANE;
    var zAxis = WORLD_COORD_SYSTEM.zAxis;
    var xAxis = WORLD_COORD_SYSTEM.xAxis;

    // code based on the Onshape STD transform feature
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

    zAxis *= definition.flipPrimaryAxis ? -1 : 1;

    return toWorld(planeToCSys(planeToUse)) * toWorld(coordSystem(WORLD_ORIGIN, xAxis, zAxis));
}

function getUserTransform(definition is map) returns Transform
precondition
{
    definition.transform;
}
{
    const base = [definition.translationX, definition.translationY, definition.translationZ]->vector();
    var rotation = identityTransform();
    if (!tolerantEquals(definition.rotation, 0 * degree))
    {
        var rotationLine;
        if (definition.rotationType == RotationType.ABOUT_Z)
        {
            rotationLine = Z_AXIS;
        }
        else if (definition.rotationType == RotationType.ABOUT_Y)
        {
            rotationLine = Y_AXIS;
        }
        else if (definition.rotationType == RotationType.ABOUT_X)
        {
            rotationLine = X_AXIS;
        }
        rotation = rotationAround(rotationLine, definition.rotation);
    }
    return transform(base) * rotation;
}

/**
 * @internal
 */
export const INDEX_BOUNDS =
{
            (unitless) : [0, 0, 1e5]
        } as IntegerBoundSpec;

/**
 * A predicate for the parameter `index`, which is configured automatically by
 * `addPointDeriveManipulator` and `pointDeriveManipulatorChange`.
 */
export predicate pointDerivePredicate(definition is map)
{
    annotation { "Name" : "Index", "UIHint" : ["ALWAYS_HIDDEN"] }
    isInteger(definition.index, INDEX_BOUNDS);
}

const POINT_DERIVE_MANIPULATOR = "pointDeriveManipulator";

/**
 * Adds a point derive manipulator to the `context`.
 *
 * @param options {{
 *      @field points {array} :
 *              An array of points to add. If points equals `[]`, the point manipulator is skipped.
 *              @eg `pointDeriveResult.points`
 *      @field index {number} :
 *              The index of the currently selected point.
 *              @eg `pointDeriveResult.index`
 * }}
 */
export function addPointDeriveManipulator(context is Context, id is Id, options is map)
precondition
{
    options.points is array;
    for (var point in options.points)
    {
        is3dLengthVector(point);
    }
    options.index is number;
}
{
    if (options.points == [])
    {
        return;
    }
    addManipulators(context, id, { (POINT_DERIVE_MANIPULATOR) : pointsManipulator(options) });
}

/**
 * The manipulator change function for a feature using addPointDeriveManipulator.
 */
export function pointDeriveManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators[POINT_DERIVE_MANIPULATOR] is Manipulator)
    {
        definition.index = newManipulators[POINT_DERIVE_MANIPULATOR].index;
    }

    return definition;
}
