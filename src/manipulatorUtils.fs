FeatureScript 1930;
// import(path : "onshape/std/common.fs", version : "1930.0");

// /**
//  * A module for creating and adding manipulators to custom features.
//  * Features using this module should have `customManipulatorPredicate` in the precondition
//  * and `customManipulatorChange` as a part of the manipulator change function.
//  * Manipulators can be added using `addCustomManipulatorType`.
//  * Their output can be applied to features using `applyCustomManipulator`.
//  *
//  * Multiple attributes of the same type can be added to a feature by taking advantage of the `index` argument of
//  * `addCustomManipulator`. `index` can specify the appropriate branch of an array parameter or the index into an input query parameter.
//  * For example, if definition.entities is a query of points and edges, the indicies of each edge in definition.entities
//  * can be used as the `index` argument to create edge manipulators at each selected edge.
//  */

// /**
//  * Retrieves a string from a map keyed by `key`, or `""` if the map has no such key.
//  */
// function getString(value is map, key) returns string
// {
//     return value[key] == undefined ? "" : toString(value[key]);
// }

// function getString(value) returns string
// {
//     return value == undefined ? "" : toString(value);
// }


// /**
//  * @value PLANE : A 3D triad manipulator restricted to 2D movement along a plane.
//  * @value EDGE : A tangential linear manipulator which moves along an edge.
//  * @value LINE : A linear manipulator.
//  * @value LENGTH : A linear manipulator which moves in a single direction (without flipping).
//  * @value POINT : A point manipulator.
//  * @value TOGGLE_POINT : A point manipulator that can be used to toggle items on or off.
//  */
// export enum CustomManipulatorType
// {
//     TRIAD,
//     LINE,
//     LENGTH,
//     POINT,
//     TOGGLE_POINT,
//     EDGE
// }

// export enum TriadManipulatorBehavior
// {
//     TRIAD_3D,
//     TRIAD_2D
// }

// export enum PointManipulatorBehavior
// {
//     POINT,
//     TOGGLE_POINT
// }

// export const MANIPULATOR_PARAMETER = "customManipulatorMap";

// /**
//  * A predicate for the parameter `customManipulatorMap`. Should be included in any features seeking to use other functions in this module.
//  */
// export predicate customManipulatorPredicate(definition is map)
// {
//     annotation { "Name" : "Custom manipulator map", "Default" : "{}", "UIHint" : ["ALWAYS_HIDDEN", "UNCONFIGURABLE"] }
//     isAnything(definition.customManipulatorMap);
// }

// /**
//  * Checks if a string is a valid string which can be used as a manipulator id.
//  * This module uses regex to pass information through the manipulator id, which is broken into several components.
//  *
//  * To prevent issues with definition key access, a manipulator key is not a specific type.
//  *
//  * @ex `EDGE!2<MY_LOCATION*GEOMETRIC_TOLERANCE`
//  * @ex `PLANE!2<OTHER_LOCATION*^`
//  */
// export predicate canBeManipulatorKey(value)
// {
//     value is string;
//     replace(value, MANIPULATOR_KEY_MATCH_REGEX, "") == "";
// }

// const MANIPULATOR_KEY_MATCH_REGEX = "([\\w_]+)!([\\d\\.]*)<([\\d\\w_]*)\\*([\\w\\d_]*)";

// function manipulatorKey(evManipulatorKey is EvManipulatorKey) returns string
// {
//     return getString(evManipulatorKey.customManipulator)
//         ~ "!" ~ getString(evManipulatorKey.index)
//         ~ "*" ~ getString(evManipulatorKey.edgeDefinition);
// }

// function manipulatorKey(input is map) returns string
// precondition
// {
//     canBeEvManipulatorKey(input);
// }
// {
//     return evManipulatorKey(input)->manipulatorKey();
// }

// /**
//  * Returns a `Query` representing an edge defined by an `EDGE` manipulator from the `definition`.
//  *
//  * This function should not be confused with calling `definition[MANIPULATOR_PARAMETER][manipulatorKey]`, which should
//  * be used when attempting to set or retrieve the saved value of a manipulator.
//  */
// function getEdgeQuery(definition is map, evManipulatorKey is EvManipulatorKey) returns Query
// precondition
// {
//     evManipulatorKey.customManipulator == CustomManipulatorType.EDGE;
//     canBeCustomEdgeManipulator(evManipulatorKey);
// }
// {
//     const edgeDefinition = EDGE_DEFINITION_MAP[evManipulatorKey.edgeDefinition];

//     if (evManipulatorKey.index != undefined)
//     {
//         if (edgeDefinition.arrayParameter != undefined)
//         {
//             return definition[edgeDefinition.arrayParameter][evManipulatorKey.index][edgeDefinition.parameter];
//         }

//         return definition[edgeDefinition.parameter]->qNthElement(evManipulatorKey.index);
//     }

//     return definition[edgeDefinition.parameter];
// }

// /**
//  * A data type containing the parsed data stored on a manipulator key.
//  * @type {{
//  *      @field customManipulator {CustomManipulatorType} :
//  *              A string representation of an CustomManipulatorType.
//  *      @field index {number} : @optional
//  *              An index used to indentify a manipulator. Has various meanings depending on other contexts.
//  *              If `customManipulator == CustomManipulatorType.EDGE` and `arrayParameter != undefined`,
//  *              the index is used to specify the index into the array parameter.
//  *              If `customManipulator == CustomManipulatorType.EDGE` and `arrayParameter == undefined`,
//  *              the index may be used to specify an index into an individual query parameter.
//  *              Otherwise, the index may be used to disambiguate different manipulators added to a feature.
//  * }}
//  */
// type EvManipulatorKey typecheck canBeEvManipulatorKey;

// predicate canBeEvManipulatorKey(value)
// {
//     value is map;
//     value.customManipulator is CustomManipulatorType;
//     value.index is undefined || value.index is number;
// }

// function evManipulatorKey(input is map) returns EvManipulatorKey
// precondition
// {
//     canBeEvManipulatorKey(input);
// }
// {
//     return {
//                 "customManipulator" : input.customManipulator,
//                 "index" : input.index,
//                 "edgeDefinition" : input.edgeDefinition
//             } as EvManipulatorKey;
// }

// function evManipulatorKey(manipulatorKey is string) returns EvManipulatorKey
// precondition
// {
//     canBeManipulatorKey(manipulatorKey);
// }
// {
//     const captures = match(manipulatorKey, MANIPULATOR_KEY_MATCH_REGEX).captures;
//     return {
//                 "customManipulator" : captures[1] as CustomManipulatorType,
//                 "index" : captures[2] == "" ? undefined : captures[2]->stringToNumber(),
//             } as EvManipulatorKey;
// }

// /**
//  * Reports feature info if `customManipulatorResetPredicate` is active but has not yet been confirmed.
//  */
// export function customManipulatorResetInfo(context is Context, id is Id, definition is map)
// {
//     if (definition.resetLocation)
//     {
//         reportFeatureInfo(context, id, "Select \"Confirm\" to reset the position of this feature's manipulators to the location specified by the default location id.");
//     }
// }

// /**
//  * The top level manipulator change function for custom features. Automatically handles manipulator changes for all custom manipulators
//  * by setting the value of definition[MANIPULATOR_PARAMETER] automatically based on the manipulators which have been added to the feature.
//  * @seealso [customManipulatorPredicate]
//  * @seealso [addCustomManipulatorType]
//  * @seealso [applyCustomManipulatorType]
//  */
// export function customManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
// {
//     if (!(definition[MANIPULATOR_PARAMETER] is map))
//     {
//         definition[MANIPULATOR_PARAMETER] = {};
//     }

//     for (var manipulatorKey, manipulator in newManipulators)
//     {
//         if (!canBeManipulatorKey(manipulatorKey))
//         {
//             continue;
//         }

//         const evManipulatorKey = evManipulatorKey(manipulatorKey);

//         if (evManipulatorKey.customManipulator == CustomManipulatorType.PLANE)
//         {
//             try
//             {
//                 definition[MANIPULATOR_PARAMETER][manipulatorKey] = manipulator.offset;
//             }
//         }
//         else if (evManipulatorKey.customManipulator == CustomManipulatorType.EDGE)
//         {
//             try
//             {
//                 const edgeQuery = getEdgeQuery(definition, evManipulatorKey);

//                 const position = manipulator.base + manipulator.direction * manipulator.offset;

//                 var distanceResult is DistanceResult = evDistance(context, {
//                         "side0" : position,
//                         "side1" : edgeQuery->qNthElement(0),
//                         "arcLengthParameterization" : false
//                     });

//                 var parameter = distanceResult.sides[1].parameter;

//                 if (parameter < 0 + TOLERANCE.zeroLength || parameter > 1 - TOLERANCE.zeroLength)
//                 {
//                     const tangentLines = evEdgeTangentLines(context, {
//                                 "edge" : edgeQuery,
//                                 "parameters" : [0, 1]
//                             });
//                     const tangentResult = evDistance(context, {
//                                 "side0" : position,
//                                 "side1" : tangentLines[0]
//                             });

//                     const secondTangentResult = evDistance(context, {
//                                 "side0" : position,
//                                 "side1" : tangentLines[1]
//                             });

//                     var line;
//                     if (tangentResult.distance < secondTangentResult.distance)
//                     {
//                         line = tangentLines[0];
//                         line.origin += line.direction * tangentResult.sides[1].parameter;
//                     }
//                     else
//                     {
//                         line = tangentLines[1];
//                         line.origin += line.direction * secondTangentResult.sides[1].parameter;
//                     }

//                     definition[MANIPULATOR_PARAMETER][manipulatorKey] = { "hasOvershoot" : true, "line" : line };
//                 }
//                 else
//                 {
//                     const line = evEdgeTangentLine(context, {
//                                 "edge" : edgeQuery,
//                                 "parameter" : parameter
//                             });

//                     definition[MANIPULATOR_PARAMETER][manipulatorKey] = { "hasOvershoot" : false, "line" : line };
//                 }
//             }
//         }
//         else if (evManipulatorKey.customManipulator == CustomManipulatorType.LINE)
//         {
//             try
//             {
//                 var value = { "offset" : abs(manipulator.offset), "flip" : manipulator.offset < 0 };
//                 definition[MANIPULATOR_PARAMETER][manipulatorKey] = value;
//             }
//         }
//         else if (evManipulatorKey.customManipulator == CustomManipulatorType.LENGTH)
//         {
//             try
//             {
//                 definition[MANIPULATOR_PARAMETER][manipulatorKey] = max(0 * meter, manipulator.offset);
//             }
//         }
//         else if (evManipulatorKey.customManipulator == CustomManipulatorType.POINT)
//         {
//             try
//             {
//                 definition[MANIPULATOR_PARAMETER][manipulatorKey] = manipulator.index;
//             }
//         }
//         else if (evManipulatorKey.customManipulator == CustomManipulatorType.TOGGLE_POINT)
//         {
//             try
//             {
//                 var value = definition[MANIPULATOR_PARAMETER][manipulatorKey];

//                 if (!(value is map))
//                 {
//                     value = {};
//                 }

//                 if (value[manipulator.index] == undefined)
//                 {
//                     value[manipulator.index] = false; // default is true, so change to false
//                 }
//                 else
//                 {
//                     // flip value
//                     value[manipulator.index] = !value[manipulator.index];
//                 }

//                 definition[MANIPULATOR_PARAMETER][manipulatorKey] = value;
//             }
//         }
//     }
//     return definition;
// }

// /**
//  * Adds an custom manipulator to a feature, showing it to the user and allowing them to edit it as desired.
//  *
//  * @param options {{
//  *      @field customManipulator {CustomManipulatorType} :
//  *              The type of custom manipulator to create.
//  *      @field base {Vector} : @requiredif {`customManipulator` == `CustomManipulatorType.PLANE` ||
//   `customManipulator` == `CustomManipulatorType.LINE` ||
//   `customManipulator` == `CustomManipulatorType.LENGTH`}
//  *              The base of a linear or planar manipulator.
//  *              @autocomplete `getManipulatorBase(context, definition, "myParameter", attribute.plane)`
//  *      @field location {Vector} : @requiredif {`customManipulator` == `CustomManipulatorType.PLANE`}
//  *              The location of the plane manipulator in 3D space. Helps to avoid redundant base subtractions.
//  *              @autocomplete `attribute.plane.origin`
//  *      @field direction {Vector} : @requiredif {`customManipulator` == `CustomManipulatorType.LINE` ||
//   `customManipulator` == `CustomManipulatorType.LENGTH`}
//  *              The direction of the linear manipulator.
//  *      @field points {array} : @requiredif {`customManipulator == CustomManipulatorType.POINT` || `customManipulator` == `CustomManipulatorType.TOGGLE_POINT`}
//  *              An array of 3D `Vector`s for the point manipulator.
//  *      @field index {number} : @optional
//  *              A value to append to the manipulator key. Allows multiple instances of a single manipulator type to be created.
//  * }}
//  */
// export function addCustomManipulatorType(context is Context, id is Id, definition is map, options is map)
// precondition
// {
//     options.customManipulator is CustomManipulatorType;

//     (options.customManipulator == CustomManipulatorType.PLANE ||
//                 options.customManipulator == CustomManipulatorType.LINE ||
//                 options.customManipulator == CustomManipulatorType.LENGTH) ?
//         is3dLengthVector(options.base) : options.base is undefined;

//     options.customManipulator == CustomManipulatorType.PLANE ?
//         is3dLengthVector(options.location) : options.location is undefined;

//     if (options.customManipulator == CustomManipulatorType.POINT ||
//         options.customManipulator == CustomManipulatorType.TOGGLE_POINT)
//     {
//         options.points is array;
//         for (var point in options.points)
//         {
//             is3dLengthVector(point);
//         }
//     }
// }
// {
//     const evManipulatorKey = evManipulatorKey(options);

//     const manipulatorKey = manipulatorKey(evManipulatorKey);

//     if (evManipulatorKey.customManipulator == CustomManipulatorType.PLANE)
//     {
//         addManipulators(context, id, {
//                     (manipulatorKey) : triadManipulator({
//                             "base" : options.base,
//                             "offset" : options.location - options.base
//                         })
//                 });
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.EDGE)
//     {
//         const edgeQuery = getEdgeQuery(definition, evManipulatorKey);

//         const value = definition[MANIPULATOR_PARAMETER][manipulatorKey];

//         var line;
//         if (value == undefined || (!(value is map) || value.line == undefined))
//         {
//             line = evEdgeTangentLine(context, {
//                         "edge" : edgeQuery,
//                         "parameter" : 0.5
//                     });
//         }
//         else
//         {
//             line = typecast(value.line, Type.LINE);
//         }

//         addManipulators(context, id, {
//                     (manipulatorKey) : linearManipulator({
//                             "base" : line.origin,
//                             "direction" : line.direction,
//                             "offset" : 0 * meter,
//                             "style" : ManipulatorStyleEnum.TANGENTIAL,
//                             "primaryManipulatorId" : getPrimaryManipulatorId(evManipulatorKey)
//                         })
//                 });
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.LINE)
//     {
//         var value = definition[MANIPULATOR_PARAMETER][manipulatorAccessKey];
//         if (value == undefined || !(value is map))
//         {
//             value = { "offset" : 0 * meter, "flip" : false };
//         }

//         addManipulators(context, id, {
//                     (manipulatorKey) : linearManipulator({
//                             "base" : options.base,
//                             "direction" : options.direction,
//                             "offset" : value.flip ? -value.offset : value.offset
//                         })
//                 });
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.LENGTH)
//     {
//         var value = definition[MANIPULATOR_PARAMETER][manipulatorAccessKey];
//         if (value == undefined)
//         {
//             value = 0 * meter;
//         }
//         addManipulators(context, id, {
//                     (manipulatorKey) : linearManipulator({
//                             "base" : options.base,
//                             "direction" : options.direction,
//                             "offset" : value
//                         })
//                 });
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.POINT)
//     {
//         var value = definition[MANIPULATOR_PARAMETER][manipulatorAccessKey];

//         addManipulators(context, id, {
//                     (manipulatorKey) : pointsManipulator({
//                             "points" : options.points,
//                             "index" : (value == undefined || value > size(options.points) - 1 ? 0 : value)
//                         })
//                 });
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.TOGGLE_POINT)
//     {
//         addManipulators(context, id, {
//                     (manipulatorKey) : pointsManipulator({
//                             "points" : options.points,
//                             "index" : -1
//                         })
//                 });
//     }
//     else
//     {
//         throw regenError("The specified customManipulator style is not supported.");
//     }
// }

// /**
//  * A type defining the result of `applyCustomManipulatorType` when `allowOvershoot` is set to `true`.
//  * Used to return additional data regarding the line which is being returned in order to enable drawing a connecting
//  * line from the end of the edge to the manipulator location.
//  *
//  * @type {{
//  *          @field hasOvershoot {boolean} : Whether an overshoot exists.
//  *          @field overshootStartPoint {Vector} : The start point of the overshoot (where it connects to model geometry).
//  *          @field line {Line} : The line representing the location of the manipulator and it's relative direction.
//  * }}
//  */
// export type OvershootResult typecheck canBeOvershootResult;

// export predicate canBeOvershootResult(value)
// {
//     value is map;
//     value.hasOvershoot is boolean;
//     if (value.hasOvershoot)
//     {
//         value.overshootStartPoint is Vector;
//     }
//     value.line is Line;
// }

// predicate allowsOvershoot(manipulatorDefinition is map)
// {
//     manipulatorDefinition.allowOvershoot != undefined && manipulatorDefinition.allowOvershoot;
// }

// /**
//  * Gets the result of an added custom from the definition parameter.
//  * Notably, projecting is weird in the context of the offset vector. Since the final results needs to be projected, but not the intermediate result,
//  * the returned vector is not guaranteed to lie on the plane.
//  *
//  * @param options {{
//  *      @field customManipulator {CustomManipulatorType} :
//  *              The custom manipulator to use.
//  *              @autocomplete `CustomManipulatorType.PLANE`
//  *      @field index {number} : @optional
//  *              The index number of the manipulator. Should match the index passed into `addCustomManipulatorType`.
//  * }}
//  *
//  * @returns : A `Vector` representing the shift from the base if the manipulator is a `PLANE` manipulator,
//  *      a `ValueWithUnits` representing the distance along the direction if the manipulator is a `LINE` manipulator,
//  *      an `overshootResult` or a `Line` representing the position if the manipulator is an `EDGE` manipulator,
//  *      a `number` representing the currently selected index if the manipulator is a `POINT` manipulator,
//  *      and a `map` with key-value pairs mapping indicies to their set state (or undefined, if not yet toggled)
//  *      if the manipulator is a `TOGGLE_MANIPULATOR`.
//  */
// export function applyCustomManipulatorType(context is Context, definition is map, options is map)
// precondition
// {
//     options.customManipulator is CustomManipulatorType;
//     options.index is number || options.index is undefined;
// }
// {
//     const evManipulatorKey = evManipulatorKey(options);

//     const manipulatorKey = manipulatorKey(evManipulatorKey);

//     const value = definition[MANIPULATOR_PARAMETER][manipulatorAccessKey];

//     if (evManipulatorKey.customManipulator == CustomManipulatorType.PLANE)
//     {
//         return (value == undefined ? zeroVector(3) * meter : value as Vector);
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.EDGE)
//     {
//         const edgeQuery = getEdgeQuery(definition, evManipulatorKey);

//         if (value == undefined || !(value is map) || value.line == undefined || value.hasOvershoot == undefined)
//         {
//             return { "hasOvershoot" : false, "line" : evEdgeTangentLine(context, {
//                                 "edge" : edgeQuery,
//                                 "parameter" : 0.5,
//                                 "arcLengthParameterization" : false
//                             }) } as OvershootResult;
//         }

//         var overshootResult = {
//             "hasOvershoot" : value.hasOvershoot,
//             "line" : typecast(value.line, Type.LINE) };

//         if (overshootResult.hasOvershoot)
//         {
//             overshootResult.overshootStartPoint = evDistance(context, {
//                             "side0" : edgeQuery->qNthElement(0),
//                             "side1" : value.line.origin
//                         }).sides[0].point;
//         }
//         return overshootResult as OvershootResult;
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.LINE)
//     {
//         if (value != undefined && value is map && value.offset != undefined && value.flip != undefined)
//         {
//             return value.offset * (value.flip ? -1 : 1);
//         }
//         else
//         {
//             return 0 * meter;
//         }
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.LENGTH)
//     {
//         return (value != undefined) ? value as ValueWithUnits : 0 * meter;
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.POINT)
//     {
//         return (value == undefined) ? 0 : value;
//     }
//     else if (evManipulatorKey.customManipulator == CustomManipulatorType.TOGGLE_POINT)
//     {
//         return (value == undefined || !(value is map)) ? {} : value;
//         // return value == undefined || value[options.pointIndex] == undefined ? true : value[options.pointIndex]; // default toggle point value is true
//     }
//     else
//     {
//         throw regenError("The specified customManipulator style is not supported.");
//     }
// }

// /**
//  * Sets a new value for an custom manipulator. Can be used in editing logic to update specific manipulators when required.
//  *
//  * @param options {{
//  *      @field customManipulator {CustomManipulatorType} :
//  *              The custom manipulator to use.
//  *              @autocomplete `CustomManipulatorType.PLANE`
//  *      @field index {number} : @optional
//  *              The index number of the manipulator. Should match the index passed into `addCustomManipulatorType`.
//  *      @field value :
//  *              The new manipulator value to set.
//  * }}
//  */
// export function updateCustomManipulator(context is Context, definition is map, options is map) returns map
// precondition
// {
//     options.customManipulator is CustomManipulatorType;
//     options.index is number || options.index is undefined;
//     options.value != undefined;
// }
// {
//     definition[MANIPULATOR_PARAMETER][evManipulatorKey(options)->manipulatorKey()] = options.value;
//     return definition;
// }