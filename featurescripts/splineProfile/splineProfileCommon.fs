FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "b75434df23d86ba9542f761e", version : "ba222d9a7c55b13cef9c1c62");

import(path : "eb11a2948f8123134339137f", version : "aa3b93f58a282fb8286a97ca");
import(path : "aa47f3d3eb754118903deeec", version : "0cf078513442fad9ec35fd7b");

export enum SplineType
{
    annotation { "Name" : "MAXSpline" }
    MAX_SPLINE,
    annotation { "Name" : "SplineXL" }
    SPLINE_XL,
    annotation { "Name" : "SplineXS" }
    SPLINE_XS
}

/**
 * The name of a spline, e.g. `MAXSpline`.
 */
export function splineName(splineType is SplineType) returns string
{
    return switch (splineType) {
            SplineType.MAX_SPLINE : "MAXSpline",
            SplineType.SPLINE_XL : "SplineXL",
            SplineType.SPLINE_XS : "SplineXS"
        };
}

/**
 * Whether a spline's shafts are tubes, with an `ProfileSide.INSIDE` profile. SplineXS shafts are solid.
 */
export predicate isTubeSpline(splineType is SplineType)
{
    splineType != SplineType.SPLINE_XS;
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
    const profileMap = switch (definition.splineType) {
            SplineType.MAX_SPLINE : MAX_SPLINE,
            SplineType.SPLINE_XL : SPLINE_XL,
            SplineType.SPLINE_XS : SPLINE_XS
        };
    if (profileMap[definition.profileSide] == undefined)
    {
        throw regenError(splineName(definition.splineType) ~ " shafts are solid, so have no inside profile.");
    }
    skDataArray(sketch, textId, {
                "sketchDataArray" : profileMap[definition.profileSide],
                "location" : definition.location
            });
}

