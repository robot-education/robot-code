FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

export const PLATE_ATTRIBUTE = "plateAttribute";

export type PlateAttribute typecheck canBePlateAttribute;

export predicate canBePlateAttribute(value)
{
    value is map;
}

export const ROBOT_HOLE_ATTRIBUTE = "robotHoleAttribute";

export type RobotHoleAttribute typecheck canBeRobotHoleAttribute;

export predicate canBeRobotHoleAttribute(value)
{
    value is map;
}

export const ROBOT_COMPONENT_ATTRIBUTE = "robotComponentAttribute";

export type RobotComponentAttribute typecheck canBeRobotComponentAttribute;

export predicate canBeRobotComponentAttribute(value)
{
    value is map;
}