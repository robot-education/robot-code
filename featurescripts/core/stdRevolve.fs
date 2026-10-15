FeatureScript 2960;
/**
 * Std revolve's angles, for features which turn something through an angle about an axis: its blind end (Angle,
 * Opposite direction, and Symmetric) and second end position (Start angle), as std's Revolve has them.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Std revolve's blind end and second end position: Angle (`angle`), Opposite direction (`oppositeDirection`),
 * Symmetric (`symmetric`), and Second end position (`hasStartBound`) with Start angle (`angleBack`).
 */
export predicate revolveAnglePredicate(definition is map)
{
    annotation { "Name" : "Angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    isAngle(definition.angle, ANGLE_360_BOUNDS);

    annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION_CIRCULAR"] }
    definition.oppositeDirection is boolean;

    annotation { "Name" : "Symmetric", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.symmetric is boolean;

    if (!definition.symmetric)
    {
        annotation { "Name" : "Second end position" }
        definition.hasStartBound is boolean;

        if (definition.hasStartBound)
        {
            annotation { "Group Name" : "Second direction", "Driving Parameter" : "hasStartBound", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Start angle" }
                isAngle(definition.angleBack, ANGLE_360_REVERSE_DEFAULT_BOUNDS);
            }
        }
    }
}

/**
 * Where a revolve by `revolveAnglePredicate` starts and ends, counterclockwise about its axis from where it's measured:
 * `angle` that way (or the other, with Opposite direction), from `angleBack` the other way with a second end position;
 * or with Symmetric, half of `angle` each way.
 *
 * @returns {{
 *      @field start {ValueWithUnits} :
 *      @field end {ValueWithUnits} : At least `start`.
 * }}
 */
export function revolveAngles(definition is map) returns map
{
    if (definition.symmetric)
    {
        return { "start" : -definition.angle / 2, "end" : definition.angle / 2 };
    }
    const direction = definition.oppositeDirection ? -1 : 1;
    const far = direction * definition.angle;
    const near = definition.hasStartBound ? -direction * definition.angleBack : 0 * degree;
    return { "start" : min(near, far), "end" : max(near, far) };
}
