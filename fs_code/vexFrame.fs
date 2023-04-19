FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");
// export import(path : "17d4441aee17c4a912b0b6a3", version : "c6c0ffdea5e1387d0134e39a");

import(path : "8862d241df6ece50b11a2d5b", version : "5485d4ff0ac5eec8b450b708");

export enum VexFrameType
{
    annotation { "Name" : "1x1" }
    ONE_BY_ONE,
    annotation { "Name" : "1x2x1" }
    ONE_BY_TWO_BY_ONE,
    annotation { "Name" : "1x3x1" }
    ONE_BY_THREE_BY_ONE,
    annotation { "Name" : "1x5x1" }
    ONE_BY_FIVE_BY_ONE
}

export predicate vexFramePredicate(definition is map)
{
    annotation { "Name" : "VEX frame type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"], "Default" : "ONE_BY_TWO_BY_ONE" }
    definition.vexFrameType is VexFrameType;
}

// export function executeVexFrame(context is Context, id is Id, definition is map)
// {
//   //  verifyNonemptyQuery(context, definition, "edge", "Select an edge to use.");

//     const ownerPlane = evOwnerSketchPlane(context, { "entity" : definition.edge });
//     const tangentLine = evEdgeTangentLine(context, {
//                 "edge" : definition.edge,
//                 "parameter" : 0.5
//             });
//     definition.location = coordSystem(tangentLine.origin, cross(ownerPlane.normal, tangentLine.direction), tangentLine.direction);
//     definition.points = vexFramePoints(definition.vexFrameType);
    
//     const pointTransformResult = computePointTransform(definition);
//     addPointDeriveManipulator(context, id, pointTransformResult);

//     createVexFrame(context, id, definition);

//     opTransform(context, id + "transform", {
//                 "bodies" : qCreatedBy(id, EntityType.BODY),
//                 "transform" : pointTransformResult.transform
//             });
// }

// function createVexFrame(context is Context, id is Id, definition is map)
// {
//     const vexFrameMap = VEX_TUBE_MAP[definition.vexFrameType];

//     const length = evLength(context, { "entities" : definition.edge });
//     const numHoles = round(length / (0.5 * inch));

//     if (!tolerantEquals(length - (numHoles * 0.5 * inch), 0 * meter))
//     {
//         reportFeatureInfo(context, id, "The edge length is not a multiple of a 1/2 inch.");
//         addDebugEntities(context, definition.edge, DebugColor.RED);
//     }

//     createVexFrameBody(context, id + "frameBody", definition, length, vexFrameMap);
//     const frame = qCreatedBy(id + "frameBody", EntityType.BODY);

//     const frameAttribute = frameAttribute(id, definition.bodyType, length, 0.177 / 2 * inch, 1 / 16 * inch);
//     setFrameAttribute(context, frame, frameAttribute);

//     const frameHoleAttribute = frameHoleAttribute(frameAttribute);
//     const frameInfo = frameInfo(mergeMaps(vexFrameMap.info, {
//                     "frontHasOpposite" : false,
//                     "rightWidth" : 0.546 * inch,
//                     "length" : length
//                 }));

//     const framePatterns = getVexFramePatterns(definition.vexFrameType, frameInfo, frameHoleAttribute, numHoles, vexFrameMap);

//     addFramePatterns(context, id + "framePattern", {
//                 "frameInfo" : frameInfo,
//                 "framePatterns" : framePatterns
//             });

//     opBoolean(context, id + "boolean", {
//                 "tools" : qCreatedBy(id + "framePattern", EntityType.BODY)->qBodyType(BodyType.SOLID),
//                 "targets" : qFrame(frameAttribute),
//                 "operationType" : BooleanOperationType.SUBTRACTION
//             });

//     opCreateCompositePart(context, id + "compositePart", {
//                 "bodies" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.NO),
//                 "closed" : true
//             });
// }

// function createVexFrameBody(context is Context, id is Id, definition is map, length is ValueWithUnits, vexFrameMap is map)
// {
//     // createSketchDataArray(context, id + "sketchData", {
//     //             "plane" : XY_PLANE,
//     //             "sketchDataArray" : vexFrameMap[definition.bodyType == ToolBodyType.SOLID ? "solid" : "surface"]
//     //         });

//     const query = isSolid(definition) ?
//         qCreatedBy(id + "sketchData", EntityType.FACE) :
//         qCreatedBy(id + "sketchData", EntityType.BODY)->qBodyType(BodyType.WIRE)->qOwnedByBody(EntityType.EDGE);

//     opExtrude(context, id + "extrude", {
//                 "entities" : query,
//                 "direction" : XY_PLANE.normal,
//                 "endBound" : BoundingType.BLIND,
//                 "endDepth" : length / 2,
//                 "startBound" : BoundingType.BLIND,
//                 "startDepth" : length / 2
//             });
//     opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(id + "sketchData", EntityType.BODY) });
// }

// function getVexFramePatterns(vexFrameType is VexFrameType, frameInfo is FrameInfo, frameHoleAttribute is FrameHoleAttribute, numHoles is number, vexFrameMap is map)
// {
//     const rightLocations = getLocations(vector(0.25, 0.25) * inch, numHoles, 0.5 * inch);
//     var patterns = [framePattern({
//                 "frameFace" : FrameFace.RIGHT,
//                 "frameHoleAttribute" : frameHoleAttribute,
//                 "locations" : rightLocations
//             }),
//         framePattern({
//                 "frameFace" : FrameFace.FRONT,
//                 "frameHoleAttribute" : frameHoleAttribute,
//                 "locations" : rightLocations
//             })
//     ];

//     if (vexFrameType == VexFrameType.ONE_BY_ONE || vexFrameType == VexFrameType.ONE_BY_TWO_BY_ONE)
//     {
//         patterns = append(patterns, framePattern({
//                         "frameFace" : FrameFace.FRONT,
//                         "frameHoleAttribute" : frameHoleAttribute,
//                         "locations" : getLocations(vector(0.5, 0.5) * inch, numHoles, 0.5 * inch)
//                     }));
//     }
//     if (vexFrameType != VexFrameType.ONE_BY_ONE)
//     {
//         patterns = addVexFrameNormalPatterns(patterns, vexFrameType, frameHoleAttribute, numHoles, vexFrameMap.yOffsets);
//     }
//     return patterns;
// }

// function addVexFrameNormalPatterns(patterns is array, vexFrameType is VexFrameType, frameHoleAttribute is FrameHoleAttribute, numHoles is number, yOffsets is array) returns array
// {
//     for (var yOffset in yOffsets)
//     {
//         patterns = append(patterns, framePattern({
//                         "frameFace" : FrameFace.FRONT,
//                         "frameHoleAttribute" : frameHoleAttribute,
//                         "locations" : getLocations(vector(0.25 * inch, yOffset), numHoles, 0.5 * inch)
//                     }));
//     }
//     return patterns;
// }

// function getLocations(start is Vector, count is number, increment is ValueWithUnits) returns array
// {
//     var locations = [];
//     for (var i = 0; i < count; i += 1)
//     {
//         locations = append(locations, start);
//         start[0] += increment;
//     }
//     return locations;
// }

// function vexFramePoints(vexFrameType is VexFrameType) returns array
// {
//     const xPoints = VEX_TUBE_POINTS_MAP[vexFrameType];
//     var points = makeArray(size(xPoints) * 6 + 3);

//     var j = 3;
//     for (var i, y in Y_POINTS)
//     {
//         points[i] = vector(0, y, 0) * inch;
//         for (var x in xPoints)
//         {
//             points[j] = vector(x, y, 0) * inch;
//             points[j + 1] = vector(-x, y, 0) * inch;
//             j += 2;
//         }
//     }
//     return points;
// }

// const Y_POINTS = [0.023, -0.273, 0.273];

// const VEX_TUBE_POINTS_MAP = {
//         VexFrameType.ONE_BY_ONE : [
//             0.25
//         ],
//         VexFrameType.ONE_BY_TWO_BY_ONE : [
//             0.5, 0.25
//         ],
//         VexFrameType.ONE_BY_THREE_BY_ONE : [
//             0.75, 0.5
//         ],
//         VexFrameType.ONE_BY_FIVE_BY_ONE : [
//             1.25, 1, 0.5
//         ]
//     };

// export const VEX_TUBE_MAP = {
//         VexFrameType.ONE_BY_ONE : {
//             "solid" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(4.7625, 6.9342) * millimeter, "end" : vector(4.7625, -4.5529) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-6.35, -6.9342) * millimeter, "end" : vector(4.3688, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-6.35, -6.9342) * millimeter, "end" : vector(-6.35, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(3.9687, -5.3467) * millimeter, "end" : vector(-6.35, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(6.35, -4.953) * millimeter, "end" : vector(6.35, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(6.35, 6.9342) * millimeter, "end" : vector(4.7625, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(3.9688, -5.3467) * millimeter, "mid" : vector(4.53, -5.1142) * millimeter, "end" : vector(4.7625, -4.5529) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(4.3688, -6.9342) * millimeter, "mid" : vector(5.7697, -6.3539) * millimeter, "end" : vector(6.35, -4.953) * millimeter },
//                 ] as SketchDataArray,
//             "surface" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(6.35, 6.9342) * millimeter, "end" : vector(6.35, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-6.35, -6.9342) * millimeter, "end" : vector(6.35, -6.9342) * millimeter },
//                     // { "operation" : SketchOperation.LINE, "start" : vector(4.7625, 6.9342) * millimeter, "end" : vector(4.7625, -4.5529) * millimeter },
//                     // { "operation" : SketchOperation.ARC, "start" : vector(3.9688, -5.3467) * millimeter, "mid" : vector(4.53, -5.1142) * millimeter, "end" : vector(4.7625, -4.5529) * millimeter },
//                     // { "operation" : SketchOperation.LINE, "start" : vector(3.9687, -5.3467) * millimeter, "end" : vector(-6.35, -5.3467) * millimeter },
//                 ] as SketchDataArray,
//             "info" : { "frontWidth" : 0.5 * inch, "rightHasOpposite" : false },
//         },
//         VexFrameType.ONE_BY_TWO_BY_ONE : {

//             "solid" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-12.7, 6.9342) * millimeter, "end" : vector(-12.7, -4.953) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-10.7188, -6.9342) * millimeter, "end" : vector(10.7188, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(12.7, -4.953) * millimeter, "end" : vector(12.7, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-12.7, 6.9342) * millimeter, "end" : vector(-11.1125, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-11.1125, 6.9342) * millimeter, "end" : vector(-11.1125, -4.5529) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-10.3187, -5.3467) * millimeter, "end" : vector(10.3188, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(11.1125, -4.5529) * millimeter, "end" : vector(11.1125, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(11.1125, 6.9342) * millimeter, "end" : vector(12.7, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-12.7, -4.953) * millimeter, "mid" : vector(-12.1197, -6.3539) * millimeter, "end" : vector(-10.7188, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(10.7188, -6.9342) * millimeter, "mid" : vector(12.1197, -6.3539) * millimeter, "end" : vector(12.7, -4.953) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-11.1125, -4.5529) * millimeter, "mid" : vector(-10.88, -5.1142) * millimeter, "end" : vector(-10.3187, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(10.3188, -5.3467) * millimeter, "mid" : vector(10.88, -5.1142) * millimeter, "end" : vector(11.1125, -4.5529) * millimeter },
//                 ] as SketchDataArray,
//             "surface" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-12.7, 6.9342) * millimeter, "end" : vector(-12.7, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-12.7, -6.9342) * millimeter, "end" : vector(12.7, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(12.7, -6.9342) * millimeter, "end" : vector(12.7, 6.9342) * millimeter },
//                 ] as SketchDataArray,
//             "info" : { "frontWidth" : 1 * inch },
//             "yOffsets" : [0.75 * inch],

//         },
//         VexFrameType.ONE_BY_THREE_BY_ONE : {
//             "solid" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-19.05, 6.9342) * millimeter, "end" : vector(-19.05, -4.953) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-17.0688, -6.9342) * millimeter, "end" : vector(17.0688, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(19.05, -4.953) * millimeter, "end" : vector(19.05, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-19.05, 6.9342) * millimeter, "end" : vector(-17.4625, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-17.4625, 6.9342) * millimeter, "end" : vector(-17.4625, -4.5529) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-16.6688, -5.3467) * millimeter, "end" : vector(16.6688, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(17.4625, -4.5529) * millimeter, "end" : vector(17.4625, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(17.4625, 6.9342) * millimeter, "end" : vector(19.05, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-19.05, -4.953) * millimeter, "mid" : vector(-18.4697, -6.3539) * millimeter, "end" : vector(-17.0688, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(17.0688, -6.9342) * millimeter, "mid" : vector(18.4697, -6.3539) * millimeter, "end" : vector(19.05, -4.953) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-17.4625, -4.5529) * millimeter, "mid" : vector(-17.23, -5.1142) * millimeter, "end" : vector(-16.6688, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(16.6688, -5.3467) * millimeter, "mid" : vector(17.23, -5.1142) * millimeter, "end" : vector(17.4625, -4.5529) * millimeter },
//                 ] as SketchDataArray,
//             "surface" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-19.05, 6.9342) * millimeter, "end" : vector(-19.05, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-19.05, -6.9342) * millimeter, "end" : vector(19.05, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(19.05, -6.9342) * millimeter, "end" : vector(19.05, 6.9342) * millimeter },
//                 ] as SketchDataArray,
//             "info" : { "frontWidth" : 1.5 * inch },
//             "yOffsets" : [0.75 * inch, 1.25 * inch],
//         },
//         VexFrameType.ONE_BY_FIVE_BY_ONE : {
//             "solid" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-31.75, 6.9342) * millimeter, "end" : vector(-31.75, -4.953) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-29.7688, -6.9342) * millimeter, "end" : vector(29.7688, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(31.75, -4.953) * millimeter, "end" : vector(31.75, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-31.75, 6.9342) * millimeter, "end" : vector(-30.1625, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-30.1625, 6.9342) * millimeter, "end" : vector(-30.1625, -4.553) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-29.3687, -5.3467) * millimeter, "end" : vector(29.3687, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(30.1625, -4.5529) * millimeter, "end" : vector(30.1625, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(30.1625, 6.9342) * millimeter, "end" : vector(31.75, 6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-31.75, -4.953) * millimeter, "mid" : vector(-31.1697, -6.3539) * millimeter, "end" : vector(-29.7688, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(29.7688, -6.9342) * millimeter, "mid" : vector(31.1697, -6.3539) * millimeter, "end" : vector(31.75, -4.953) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(-30.1625, -4.553) * millimeter, "mid" : vector(-29.93, -5.1142) * millimeter, "end" : vector(-29.3687, -5.3467) * millimeter },
//                     { "operation" : SketchOperation.ARC, "start" : vector(29.3687, -5.3467) * millimeter, "mid" : vector(29.93, -5.1142) * millimeter, "end" : vector(30.1625, -4.5529) * millimeter },
//                 ] as SketchDataArray,
//             "surface" : [
//                     { "operation" : SketchOperation.LINE, "start" : vector(-31.75, 6.9342) * millimeter, "end" : vector(-31.75, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(-31.75, -6.9342) * millimeter, "end" : vector(31.75, -6.9342) * millimeter },
//                     { "operation" : SketchOperation.LINE, "start" : vector(31.75, -6.9342) * millimeter, "end" : vector(31.75, 6.9342) * millimeter },
//                 ] as SketchDataArray,
//             "info" : { "frontWidth" : 2.5 * inch },
//             "yOffsets" : [0.75 * inch, 1.25 * inch, 1.75 * inch, 2.25 * inch],
//         }
//     };
