FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

annotation { "Feature Type Name" : "My Feature" }
export const myFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "My Query", "Filter" : QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
        definition.myQuery is Query;
        
        annotation { "Name" : "Points", "Filter" : EntityType.VERTEX && SketchObject.NO && BodyType.POINT, "MaxNumberOfPicks" : 1 }
        definition.point is Query;
    }
    {
        opPoint(context, id + "point1", {
                    "point" : evVertexPoint(context, {
                            "vertex" : definition.myQuery
                        })
                });
    });

export enum TagType
{
    HOLE,
    PROFILE
}

export enum ComponentType
{
    _775_MOTOR,
    _550_MOTOR,
    CIM_MOTOR
}

/**
 * Defines a set of tags owned by a single entity.
 */
export type TagGroup typecheck canBeTagGroup;

export predicate canBeTagGroup(value)
{
    value is map;
    value.robotTags is array;
    for (var tag in value.robotTags)
    {
        tag is RobotTag;
    }
}

/**
 * A type defining the contents of a robot tag.
 * Tags should be attatched to an owner point body.
 * @type {{
 *      @field tagType {TagType} :
 *              The type of the tag.
 * }}
 */
export type RobotTag typecheck canBeRobotTag;

export predicate canBeRobotTag(value)
{
    value is map;
    value.tagType is TagType;
    if (value.tagType == TagType.HOLE)
    {
        // Hole depth is weird, can probably get left behind assuming configurable UI scheme
        // Show tap settings if holeDefinition is tapped, otherwise show depth options
        // Don't allow tagging blind in last, tag blind and tag tapped instead?
        // Specify default depth only or something?
        // Or specify through all?
        
        // used to determine mandatory hole depth options; 
        // if through all, no additional options
        // if blind in last, second scope mandatory, plus depth and tap options
        // if blind and tap, depth and tap, otherwise just tap
        // value.endStyle is HoleEndStyle;
        // if (value.endStyle == HoleEndStyle.BLIND)
        // {
        //     value.tapped is boolean;
        // }
        
        value.holeSpec is HoleSpec;
        
    }
    else if (value.tagType == TagType.PROFILE)
    {
        value.sketchDefinition != undefined;
    }
    value.instantiated is boolean;
}

export type HoleSpec typecheck canBeHoleSpec;

export predicate canBeHoleSpec(value)
{
    
}

export function getEndStyle(holeSpec is HoleSpec)
{
    
}

export function isTapped(holeSpec is HoleSpec) returns boolean
{
    const endStyle = getEndStyle(holeSpec);
}




