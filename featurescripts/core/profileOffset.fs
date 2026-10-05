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
 * A variant of Profile offset manipulator for features with bores (that are possibly different from the main profile).
 */
export predicate boreProfileOffsetPredicate(definition is map)
{
    annotation { "Name" : "Offset bore profile", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "DISPLAY_SHORT"] }
    definition.offsetBoreProfile is boolean;

    if (definition.offsetBoreProfile)
    {
        // annotation { "Group Name" : "Offset bore profile", "Collapsed By Default" : false, "Driving Parameter" : "offsetBoreProfile" }
        // {
        annotation { "Name" : "Offset distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION", "DISPLAY_SHORT"] }
        isLength(definition.boreProfileOffsetDistance, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
        definition.boreProfileOffsetOppositeDirection is boolean;
        // }
    }
}

export function getBoreProfileOffset(definition is map)
{
    if (!definition.offsetBoreProfile)
    {
        return 0 * meter;
    }
    return definition.boreProfileOffsetDistance * (definition.boreProfileOffsetOppositeDirection ? -1 : 1);
}


export const BORE_PROFILE_OFFSET_MANIPULATOR = "boreProfileOffsetManipulator";
export const BORE_PROFILE_OFFSET_FLIP = "boreProfileOffsetOppositeDirection";

/**
 * Adds a profile offset manipulator at the first intersection of `profileAxis` with `faces`.
 *
 * @param flipDirection : @optional
 *          `true` to reverse the base direction of the manipulator. Defaults to `false`.
 */
export function addProfileOffsetManipulator(context is Context, id is Id, manipulatorKey is string, profileAxis is Line, faces is Query, flipped is boolean, flipDirection is boolean)
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
    const base = raycastResults[0].intersection;
    addManipulators(context, id, {
                (manipulatorKey) : flipManipulator({
                        "base" : base,
                        "direction" : profileAxis.direction * (flipDirection ? -1 : 1),
                        "flipped" : flipped
                    })
            });
}

export function addProfileOffsetManipulator(context is Context, id is Id, manipulatorKey is string, profileAxis is Line, faces is Query, flipped is boolean)
{
    addProfileOffsetManipulator(context, id, manipulatorKey, profileAxis, faces, flipped, false);
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
