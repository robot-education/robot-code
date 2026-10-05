FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export RobotIcon::import(path : "233760ca14ddd2085de9c219", version : "f7c25b250b6b31d37d4e95ff");

/**
 * Appended to a feature name to indicate a beta version or release.
 */
export const BETA = " (BETA)";

/**
 * Appended to a feature name to indicate the feature is deprecated and is no longer available.
 */
export const DEPRECATED = " (DEPRECATED)";

/**
 * Appended to a feature description to indicate credit.
 * @example ```
 * "This awesome feature will change your life." ~ CREDIT
 * ```
 */
export const CREDIT = "<br><br>FeatureScript by Alex Kempen.";

/* Functions can't be called from feature definitions */
// export function seeAlso(features is array) returns string
// {
//     var featuresString = "";
//     var len = size(features);
//     for (var i, feature in features)
//     {
//         featuresString ~= "Robot " ~ feature;
//         if (i == len - 2) // second to last
//         {
//             featuresString ~= ", and ";
//         }
//         else if (i < len - 2)
//         {
//             featuresString ~= ", ";
//         }
//     }
//     return "<br>See also the " ~ featuresString ~ " FeatureScripts, which work with this feature directly.";
// }

// export function seeAlso(feature is string) returns string
// {
//     return "<br>See also the Robot " ~ feature ~ " FeatureScript, which works with this feature directly.";
// }
