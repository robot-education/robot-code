FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export enum ProfileSide
{
    annotation { "Name" : "Outside" }
    OUTSIDE,
    annotation { "Name" : "Inside" }
    INSIDE
}

/**
 * A predicate for a horizontal enum defining a `profileSide`.
 */
export predicate profileSidePredicate(definition is map)
{
    annotation { "Name" : "Profile side", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"], "Default" : ProfileSide.OUTSIDE }
    definition.profileSide is ProfileSide;
}
