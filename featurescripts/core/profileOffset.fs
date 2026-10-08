FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");


export predicate profileOffsetPredicate(definition is map)
{
    annotation { "Name" : "Offset profile", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "DISPLAY_SHORT"] }
    // definition.profileOffset is taken by extrude
    definition.addProfileOffset is boolean;

    if (offsetProfile(definition))
    {
        annotation { "Name" : "Profile offset", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "DISPLAY_SHORT"] }
        isLength(definition.profileOffsetDistance, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
        definition.profileOffsetOppositeDirection is boolean;
    }
}

/**
 * Returns `true` if we should offset the profile.
 */
export predicate offsetProfile(definition is map)
{
    definition.addProfileOffset;
}

export function getProfileOffset(definition is map) returns ValueWithUnits
{
    return offsetProfile(definition) ? definition.profileOffsetDistance * (definition.profileOffsetOppositeDirection ? -1 : 1) : 0 * meter;
}

export const PROFILE_OFFSET_MANIPULATOR = "profileOffsetManipulator";
export const PROFILE_OFFSET_FLIP = "profileOffsetOppositeDirection";

/**
 * Adds a profile offset manipulator at the first intersection of `profileAxis` with `faces`.
 */
export function addProfileOffsetManipulator(context is Context, id is Id, manipulatorKey is string, profileAxis is Line, faces is Query, flipped is boolean)
{
    const raycastResults = try(evRaycast(context, {
                    "entities" : faces,
                    "ray" : profileAxis,
                    "closest" : true
                }));
    if (raycastResults == undefined || size(raycastResults) < 1)
    {
        return;
    }
    addManipulators(context, id, {
                (manipulatorKey) : flipManipulator({
                        "base" : raycastResults[0].intersection,
                        "direction" : profileAxis.direction,
                        "flipped" : flipped
                    })
            });
}

/**
 * @param manipulator: @autocomplete `newManipulators[PROFILE_OFFSET_MANIPULATOR]`
 * @param flipParameter : @autocomplete `PROFILE_OFFSET_FLIP`
 */
export function profileOffsetManipulatorChange(definition is map, manipulator, flipParameter is string) returns map
{
    if (manipulator == undefined)
    {
        return definition;
    }
    definition[flipParameter] = manipulator.flipped;
    return definition;
}
