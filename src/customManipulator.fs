FeatureScript 1948;
// import(path : "onshape/std/geometry.fs", version : "1948.0");

// /**
//  * A module wrapping common behaviors for various manipulator functions.
//  * Notably, the manipulators defined in this module can all be automatically handled via the `MANIPULATOR_CHANGE_FUNCTION` and the `customManipulatorPredicate`.
//  * In order to use these manipulators, add `customManipulatorPredicate` to the feature definition, set `MANIPULATOR_CHANGE_FUNCTION` as the `"Manipulator Change Function"` in
//  * the feature definition, and then call and use the results of any of the `customManipulator` functions.
//  */

// /**
//  * Accesses a value in map according to a given parameterName. Is notably overloaded for array parameters, so syntax like
//  * `"myArrayParameter[i].myArrayParam"` behaves as expected.
//  */
// export function accessParameter(definition is map, parameterName is string)
// {
//     const parsed = match(parameterName, "([\\w\\d]+)\\[(\\d+)\\]\\.([\\w\\d]+)");
//     if (parsed.hasMatch)
//     {
//         return definition[parsed.captures[1]][parsed.captures[2]->stringToNumber()][parsed.captures[3]];
//     }
//     return definition[parameterName];
// }

// export function setParameter(definition is map, parameterName is string, newValue) returns map
// {
//     const parsed = match(parameterName, "([\\w\\d]+)\\[(\\d+)\\]\\.([\\w\\d]+)");
//     if (parsed.hasMatch)
//     {
//         definition[parsed.captures[1]][parsed.captures[2]->stringToNumber()][parsed.captures[3]] = newValue;
//     }
//     else
//     {
//         definition[parameterName] = newValue;
//     }
//     return definition;
// }

// /**
//  * An enum defining possible manipulator types.
//  */
// enum CustomManipulatorType
// {
//     LINEAR_1D,
//     LINEAR_2D,
//     LINEAR_3D,
//     POINTS,
//     TOGGLE_POINTS,
//     FLIP
// }

// /**
//  * A predicate for the parameter `MANIPULATOR_PARAMETER`. Should be included in any features seeking to use other functions in this module.
//  */
// export predicate customManipulatorPredicate(definition is map)
// {
//     annotation { "Name" : "Custom manipulator map", "Default" : "{}", "UIHint" : ["ALWAYS_HIDDEN", "UNCONFIGURABLE"] }
//     isAnything(definition.customManipulatorMap);
// }

// const MANIPULATOR_PARAMETER = "customManipulatorMap";

// /**
//  * @value {string} : A string which starts with a `$` character, followed by the `ManipulatorType`, another `$`, the `customManipulatorType` (if applicable),
//  * and then other values for the specific manipulator type.
//  * To improve performance, only the first charcter is checked, and the rest are assumed to be correct.
//  */
// predicate canBeManipulatorKey(value is string)
// {
//     value is string;
//     splitIntoCharacters(value)[0] == "$";
// }

// /**
//  * @returns {{
//  *      @field manipulatorType {ManipulatorType} :
//  *              The manipulator type to apply.
//  *      @field strippedKey {string} :
//  *              A `manipulatorKey` with the `manipulatorType` and `manipulatorId` components removed.
//  * }}
//  */
// function parseManipulatorCommon(manipulatorKey is string) returns map
// {
//     const parsed = match(manipulatorKey, "\\$(\\w+)\\$\\*?[\\w+-/]*\\$.*");
//     if (!parsed.hasMatch)
//     {
//         throw regenError("Expected manipulator key to match.");
//     }
//     return {
//             "customManipulatorType" : parsed.captures[1] as CustomManipulatorType,
//             "strippedKey" : replace(manipulatorKey, "\\$\\w+\\$\\*?[\\w+/\\-]*\\$", "")
//         };
// }

// /**
//  * @param manipulatorId : @autocomplete `args.manipulatorId`
//  */
// function manipulatorKey(customManipulatorType is CustomManipulatorType, manipulatorId is string) returns string
// {
//     return "$" ~ customManipulatorType ~ "$" ~ manipulatorId ~ "$";
// }

// /**
//  * The manipulator change function.
//  */
// export const MANIPULATOR_CHANGE_FUNCTION = "customManipulatorChange";

// /**
//  * @internal
//  * The top level manipulator change function for custom features. Automatically handles manipulator changes for all custom manipulators
//  * by setting the value of definition[MANIPULATOR_PARAMETER] automatically based on the manipulators which have been added to the feature.
//  * @seealso [customManipulatorPredicate]
//  * @seealso [addCustomManipulator]
//  * @seealso [applyCustomManipulator]
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
//         const manipulatorCommon = parseManipulatorCommon(manipulatorKey);
//         definition = customManipulatorTypeToChangeFunctionMap[manipulatorCommon.customManipulatorType](context, definition, manipulator, manipulatorKey, manipulatorCommon.strippedKey);
//     }

//     return definition;
// }

// export const customManipulatorTypeToChangeFunctionMap = {
//         CustomManipulatorType.LINEAR_1D : customLinearManipulatorChange,
//         CustomManipulatorType.POINTS : customPointsManipulatorChange,
//         CustomManipulatorType.TOGGLE_POINTS : customTogglePointsManipulatorChange,
//         CustomManipulatorType.FLIP : customFlipManipulatorChange
//     };

// predicate verifyCustomManipulatorArgs(id is Id, definition is map, args is map)
// {
//     isTopLevelId(id);
//     verifyCustomManipulatorArgs(definition, args);
// }

// predicate verifyCustomManipulatorArgs(definition is map, args is map)
// {
//     definition[MANIPULATOR_PARAMETER] is map;
//     args.manipulatorId is undefined || args.manipulatorId is string;
// }

// predicate verifyCustomManipulatorArgs(id is Id, args is map)
// {
//     isTopLevelId(id);
//     args.manipulatorId is undefined || args.manipulatorId is string;
// }

// /**
//  * Defines a triad manipulator.
//  *
//  * @param args {{
//  *              Defaults to `TRIAD_3D`.
//  *      @field basePlane {Plane} : @requiredif { `customTriadBehavior` == `CustomTriadBehavior.TRIAD_2D` }
//  *              A `Plane` with its origin at the base of the manipulator and
//  *      @field base {Vector} : @requiredif { `customTriadBehavior` == `CustomTriadBehavior.TRIAD_3D` }
//  *              The default position of the manipulator.
//  *              @autocomplete `WORLD_ORIGIN`
//  *      @field offset {Vector} :
//  *              A 3D `Vector` representing the position of the triad relative to the `base`.
//  *              If `customTriadBehavior` is `TRIAD_2D`, this Vector may also be a 2D `Vector` (otherwise, the 3D `Vector` will be projected onto `basePlane`).
//  *      @field manipulatorId {string} : @optional
//  *              An id to use for distingushing multiple `customTriadManipulator`s which are added to the same feature.
//  *              Should follow the rules of standard ids.
//  * }}
//  */
// export function customTriadManipulator(context is Context, id is Id, definition is map, args is map)
// precondition
// {
//     verifyCustomManipulatorArgs(id, definition, args);
// }
// {
//     args = mergeMaps({ "manipulatorId" : "" }, args);

//     const key = "hi";


//     addManipulators(context, id, { (key) : triadManipulator(args) });

//     return definition[MANIPULATOR_PARAMETER];
// }

// /**
//  * Defines a flip manipulator.
//  *
//  * @param args {{
//  *      @field base {Vector} :
//  *      @field direction {Vector} :
//  *      @field minValue {ValueWithUnits} : @optional
//  *      @field maxValue {ValueWithUnits} : @optional
//  *      @field style {ManipulatorStyleEnum} : @optional
//  *      @field lengthParameterId {string} : @optional
//  *              A string corresponding to a length parameter driving the manipulator.
//  *              @autocomplete `"depth"`
//  *      @field flipParameterId {string} : @optional
//  *              A string corresponding to a boolean parameter driving the manipulator.
//  *              @ex `"oppositeDirection"`
//  *              @ex `"myWidgets[0].oppositeDirection"`
//  *              @autocomplete `"oppositeDirection"`
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customPointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  * }}
//  * @returns {{
//  *      @field flipped {boolean} :
//  *              Whether the linear manipulator is flipped relative to `direction`.
//  *      @field length {ValueWithUnits} :
//  *              The (always positive) distance of the manipulator from `base`.
//  * }}
//  */
// export function customLinearManipulator(context is Context, id is Id, definition is map, args is map)
// precondition
// {
//     verifyCustomManipulatorArgs(id, args);
//     if (args.lengthParameterId == undefined || args.flipParameterId == undefined)
//     {
//         definition[MANIPULATOR_PARAMETER] is map;
//     }
// }
// {
//     args = mergeMaps({ "manipulatorId" : "", "lengthParameterId" : "", "flipParameterId" : "" }, args);
//     const manipulatorKey = manipulatorKey(CustomManipulatorType.LINEAR_1D, args.manipulatorId) ~ args.lengthParameterId ~ "$" ~ args.flipParameterId;

//     var flipped;
//     // both flipped and offset are not both undefined
//     if (args.flipParameterId == "")
//     {
//         flipped = definition[MANIPULATOR_PARAMETER][manipulatorKey];
//         if (flipped != undefined)
//         {
//             flipped = flipped < 0 * meter;
//         }
//     }
//     else
//     {
//         flipped = accessParameter(definition, args.flipParameterId);
//     }

//     var length;
//     if (args.lengthParameterId == "")
//     {
//         length = definition[MANIPULATOR_PARAMETER][manipulatorKey];
//         if (length != undefined)
//         {
//             length = abs(length);
//         }
//     }
//     else
//     {
//         length = accessParameter(definition, args.lengthParameterId);
//     }

//     if (length == undefined)
//     {
//         length = (args.minValue == undefined) ? 0 * meter : args.minValue;
//     }
//     flipped = (flipped == undefined) ? false : flipped;
//     args.offset = length * (flipped ? -1 : 1);
//     println(args.offset);
//     addManipulators(context, id, { (manipulatorKey) : linearManipulator(args) });

//     return {
//             "length" : length,
//             "flipped" : flipped
//         };
// }

// const customLinearManipulatorChange = function(context is Context, definition is map, manipulator is Manipulator, manipulatorKey is string, strippedKey is string) returns map
//     {
//         const parsed = match(strippedKey, "(.*)\\$(.*)");
//         if (!parsed.hasMatch)
//         {
//             throw regenError("Expected linear key to match.");
//         }

//         if (parsed.captures[1] == "" || parsed.captures[2] == "")
//         {
//             definition[MANIPULATOR_PARAMETER][manipulatorKey] = manipulator.offset;
//         }

//         if (parsed.captures[1] != "")
//         {
//             definition = setParameter(definition, parsed.captures[1], abs(manipulator.offset));
//         }

//         if (parsed.captures[2] != "")
//         {
//             definition = setParameter(definition, parsed.captures[2], manipulator.offset < 0 * meter);
//         }
//         return definition;
//     };

// /**
//  * Defines a flip manipulator.
//  *
//  * @param args {{
//  *      @field base {Vector} :
//  *      @field direction {Vector} :
//  *      @field otherDirection {Vector} : @optional
//  *      @field style {ManipulatorStyleEnum} : @optional
//  *      @field parameterId {string} : @optional
//  *              A string corresponding to a boolean parameter driving the manipulator.
//  *              If present, `customManipulatorPredicate` is optional.
//  *              @ex `"myParameter"`
//  *              @ex `"myWidgets[0].myParameter"`
//  *              @autocomplete `"myParameter"`
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customPointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  * }}
//  * @return {boolean} :
//  *      Whether the manipulator is currently flipped.
//  */
// export function customFlipManipulator(context is Context, id is Id, definition is map, args is map)
// precondition
// {
//     verifyCustomManipulatorArgs(id, args);
//     if (args.parameterId == undefined)
//     {
//         definition[MANIPULATOR_PARAMETER] is map;
//     }
//     else
//     {
//         args.parameterId is string;
//     }
// }
// {
//     args = mergeMaps({ "manipulatorId" : "", "parameterId" : "" }, args);
//     const key = manipulatorKey(CustomManipulatorType.FLIP, args.manipulatorId) ~ args.parameterId;

//     const value = (args.parameterId == "" ? definition[MANIPULATOR_PARAMETER][key] : accessParameter(definition, args.parameterId));
//     args.flipped = value == undefined ? false : value;
//     addManipulators(context, id, { (key) : flipManipulator(args) });

//     return args.flipped;
// }

// const customFlipManipulatorChange = function(context is Context, definition is map, manipulator is Manipulator, manipulatorKey is string, strippedKey is string) returns map
//     {
//         if (strippedKey == "")
//         {
//             definition[MANIPULATOR_PARAMETER][manipulatorKey] = manipulator.flipped;
//         }
//         else
//         {
//             definition = setParameter(definition, strippedKey, manipulator.flipped);
//         }
//         return definition;
//     };

// /**
//  * Defines a points manipulator.
//  *
//  * @param args {{
//  *      @field points {array} :
//  *              An array of 3D locations representing selectable points.
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customPointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  * }}
//  *
//  * @return {number} :
//  *      A `number` representing an index into `points` which corresponds to the currently selected point.
//  */
// export function customPointsManipulator(context is Context, id is Id, definition is map, args is map)
// precondition
// {
//     verifyCustomManipulatorArgs(id, definition, args);
//     args.points is array;
//     for (var point in args.points)
//     {
//         is3dLengthVector(point);
//     }
// }
// {
//     args = mergeMaps({ "manipulatorId" : "" }, args);

//     args.index = customPointsManipulatorValue(definition, mergeMaps(args, { "numPoints" : size(args.points) }));
//     const manipulatorKey = manipulatorKey(CustomManipulatorType.POINTS, args.manipulatorId);
//     addManipulators(context, id, { (manipulatorKey) : pointsManipulator(args) });
//     return args.index;
// }

// /**
//  * Returns the current value of a `customPointsManipulator`.
//  * Note that the result is always identical to the return value of the corresponding call to `customPointsManipulator`.
//  *
//  * @param args {{
//  *      @field numPoints {number} :
//  *              The number of points.
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customPointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  *              Should match the `manipulatorId` passed in to `customPointsManipulator`.
//  * }}
//  *
//  * @return {number} :
//  *      A `number` representing an index into `points` which corresponds to the currently selected point.
//  */
// export function customPointsManipulatorValue(definition is map, args is map)
// precondition
// {
//     verifyCustomManipulatorArgs(definition, args);
//     args.numPoints is number;
// }
// {
//     const value = definition[MANIPULATOR_PARAMETER][manipulatorKey(CustomManipulatorType.POINTS, args.manipulatorId)];
//     return (value == undefined ? 0 : clamp(value, 0, args.numPoints));
// }

// const customPointsManipulatorChange = function(context is Context, definition is map, manipulator is Manipulator, manipulatorKey is string, strippedKey is string) returns map
//     {
//         definition[MANIPULATOR_PARAMETER][manipulatorKey] = manipulator.index;
//         return definition;
//     };

// /**
//  * Defines a toggle points manipulator.
//  *
//  * @param args {{
//  *      @field points {array} :
//  *              An array of 3D locations representing selectable points.
//  *      @field defaultState {boolean} : @optional
//  *              The default state of toggle points.
//  *              Defaults to `false`.
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customTogglePointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  * }}
//  *
//  * @return {array} :
//  *      An array of booleans the same size as `points` containing the current state of each point.
//  */
// export function customTogglePointsManipulator(context is Context, id is Id, definition is map, args is map) returns array
// precondition
// {
//     verifyCustomManipulatorArgs(id, definition, args);
//     args.points is array;
//     for (var point in args.points)
//     {
//         is3dLengthVector(point);
//     }
//     args.defaultState is undefined || args.defaultState is boolean;
// }
// {
//     args = mergeMaps({ "manipulatorId" : "" }, args);

//     const manipulatorKey = manipulatorKey(CustomManipulatorType.TOGGLE_POINTS, args.manipulatorId);
//     addManipulators(context, id, { (manipulatorKey) : pointsManipulator({ "points" : args.points, "index" : -1 }) });

//     return customTogglePointsManipulatorValue(definition, mergeMaps(args, { "numPoints" : size(args.points) }));
// }

// /**
//  * Returns the current value of a `customTogglePointsManipulator`.
//  * Note that the result is always identical to the return value of `customTogglePointsManipulator`.
//  *
//  * @param args {{
//  *      @field numPoints {number} :
//  *              The number of points.
//  *              @autocomplete `size(points)`
//  *      @field defaultState {boolean} : @optional
//  *              The default state of toggle points.
//  *              Defaults to `false`.
//  *      @field manipulatorId {string} : @optional
//  *              A string used for distingushing multiple `customTogglePointsManipulator`s which are added to the same feature.
//  *              Defaults to the empty string (`""`).
//  *              Should match the `manipulatorId` passed in to `customTogglePointsManipulator`.
//  * }}
//  *
//  * @return {array} :
//  *      An array of booleans the same size as `points` containing the current state of each point.
//  */
// export function customTogglePointsManipulatorValue(definition is map, args is map) returns array
// precondition
// {
//     verifyCustomManipulatorArgs(definition, args);
//     args.numPoints is number;
//     args.defaultState is undefined || args.defaultState is boolean;
// }
// {
//     args = mergeMaps({ "manipulatorId" : "", "defaultState" : false }, args);

//     const manipulatorKey = manipulatorKey(CustomManipulatorType.TOGGLE_POINTS, args.manipulatorId);
//     var value = definition[MANIPULATOR_PARAMETER][manipulatorKey];
//     if (value == undefined)
//     {
//         value = makeArray(args.numPoints, false);
//     }
//     else if (size(value) != args.numPoints)
//     {
//         value = resize(value, args.numPoints, false);
//     }

//     if (args.defaultState)
//     {
//         value = mapArray(value, function(val)
//             {
//                 return !val;
//             });
//     }
//     return value;
// }

// const customTogglePointsManipulatorChange = function(context is Context, definition is map, manipulator is Manipulator, manipulatorKey is string, strippedKey is string) returns map
//     {
//         const index = manipulator.index;
//         var oldValue = definition[MANIPULATOR_PARAMETER][manipulatorKey];
//         if (!(oldValue is array))
//         {
//             oldValue = makeArray(index + 1, false);
//         }
//         else if (size(oldValue) < index + 1)
//         {
//             oldValue = resize(oldValue, index + 1, false);
//         }
//         oldValue[index] = !oldValue[index];
//         definition[MANIPULATOR_PARAMETER][manipulatorKey] = oldValue;
//         return definition;
//     };
