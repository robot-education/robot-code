FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "b75434df23d86ba9542f761e", version : "ba222d9a7c55b13cef9c1c62");

import(path : "eb11a2948f8123134339137f", version : "aa3b93f58a282fb8286a97ca");
import(path : "splineProfile/splineProfiles.gen.fs", version : "");

export enum SplineType
{
    annotation { "Name" : "MAXSpline" }
    MAX_SPLINE,
    annotation { "Name" : "SplineXL" }
    SPLINE_XL
}

/**
 * Adds a spline profile to a sketch.
 *
 * @param definition {{
 *          @field splineType {SplineType} :
 *          @field location {Vector} : @optional
 *          @field profileSide {ProfileSide} : @optional
 *                  The profile side to create. Defaults to `ProfileSide.OUTSIDE`.
 * }}
 */
export function skSplineProfile(sketch is Sketch, textId is string, definition is map)
precondition
{
    definition.splineType is SplineType;
    definition.location is Vector || definition.location is undefined;
    definition.profileSide is ProfileSide || definition.profileSide is undefined;
}
{
    definition = mergeMaps({ "profileSide" : ProfileSide.OUTSIDE, "location" : zeroVector(2) * meter }, definition);
    const profileMap = (definition.splineType == SplineType.MAX_SPLINE ? MAX_SPLINE : SPLINE_XL);
    skDataArray(sketch, textId, {
                "sketchDataArray" : profileMap[definition.profileSide],
                "location" : definition.location
            });
}

