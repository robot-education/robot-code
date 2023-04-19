FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

import(path : "8862d241df6ece50b11a2d5b", version : "5485d4ff0ac5eec8b450b708");


export enum System
{
    CUSTOM,
    VERSA_FRAME,
    MAX_TUBE
}

export enum CustomType
{
    TUBE,
    ANGLE,
    CIRCULAR_TUBE
}

export enum VersaFrameType
{
    ALUMINUM_TUBE,
    PLASTIC_TUBE,
    C_CHANNEL,
    ANGLE
}

export enum MaxTubeSize
{
    ONE_BY_ONE,
    TWO_BY_ONE_LIGHT,
    TWO_BY_ONE
}

export enum MaxTubePattern
{
    NONE,
    GRID,
    MAX,
    CUSTOM
}


export enum PatternLocation
{
    annotation { "Name" : "End" }
    END,
    annotation { "Name" : "Centered" }
    CENTER,
}

const HOLE_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.25,
            (foot) : 0.02,
            (yard) : 0.007
        } as LengthBoundSpec;

const NONNEGATIVE_COUNT_BOUNDS =
{
            (unitless) : [0, 1, 1e5]
        } as IntegerBoundSpec;

export predicate frcTubePredicate(definition is map)
{

}

export function executeFrcTube(context is Context, id is Id, definition is map)
{

}

export function frcTubeEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{

    return definition;
}
