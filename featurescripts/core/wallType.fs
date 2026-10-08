FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

WallThickness::import(path : "bd6028cdede1b8e094b97d84", version : "7709795652c9d954b3d0d2e4");

export const HOLE_OUTER_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.0075, 500],
            (centimeter) : 0.75,
            (millimeter) : 7.5,
            (inch) : 0.375,
            (foot) : 0.03,
            (yard) : 0.01
        } as LengthBoundSpec;


export enum WallType
{
    annotation { "Name" : "Wall thickness" }
    WALL_THICKNESS,
    annotation { "Name" : "Outer diameter" }
    OUTER_DIAMETER
}

/**
 * A predicate specifying parameters which allow users to choose between a wall thickness and an outer diameter setting.
 */
export predicate wallPredicate(definition is map)
{
    annotation { "Name" : "Wall type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.wallType is WallType;

    if (definition.wallType == WallType.WALL_THICKNESS)
    {
        annotation { "Name" : "Wall thickness", "Icon" : WallThickness::BLOB_DATA, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.wallThickness, BLEND_BOUNDS);
    }
    else if (definition.wallType == WallType.OUTER_DIAMETER)
    {
        annotation { "Name" : "Outer diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.outerDiameter, HOLE_OUTER_DIAMETER_BOUNDS);
    }
}

export function getWallDiameter(definition is map, diameter is ValueWithUnits) returns ValueWithUnits
{
    verifyWallDiameter(definition, diameter);

    if (definition.wallType == WallType.WALL_THICKNESS)
    {
        return diameter + definition.wallThickness * 2;
    }
    else if (definition.wallType == WallType.OUTER_DIAMETER)
    {
        return definition.outerDiameter;
    }
}

export function verifyWallDiameter(definition is map, diameter is ValueWithUnits)
{
    if (definition.wallType == WallType.OUTER_DIAMETER && tolerantLessThanOrEqual(definition.outerDiameter, diameter))
    {
        throw regenError("The outer diameter must greater than than the inner diameter.", ["outerDiameter"]);
    }
}

/**
 * If the wall type is changed to outer diameter and the outer diameter hasn't been specified, sets the outer diamter to twice `diameter`.
 */
export function wallEditLogic(oldDefinition is map, definition is map, specifiedParameters is map, oldDiameter is ValueWithUnits, diameter is ValueWithUnits) returns map
{
    if (oldDefinition == {} || specifiedParameters.outerDiameter || definition.wallType != WallType.OUTER_DIAMETER)
    {
        return definition;
    }
    if (oldDefinition.wallType != definition.wallType || oldDiameter != diameter)
    {
        definition.outerDiameter = diameter + definition.wallThickness;
    }
    return definition;
}
