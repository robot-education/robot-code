FeatureScript 1948;
// import(path : "onshape/std/common.fs", version : "1948.0");

// export operator*(keepout is Keepout, num is number)
// precondition
// {
//     num != 0;
// }
// {
//     keepout.firstCorner *= num;
//     keepout.secondCorner *= num;
//     return keepout;

// }

// export operator*(num is number, keepout is Keepout)
// precondition
// {
//     num != 0;
// }
// {
//     return keepout * num;
// }

// export operator/(keepout is Keepout, num is number)
// {
//     return keepout * (1 / num);
// }

// // 5 / keepout is not defined

// export operator+(keepout is Keepout, vector is Vector)
// precondition
// {
//     is2dPoint(vector);
// }
// {
//     keepout.firstCorner += vector;
//     keepout.secondCorner += vector;
//     return keepout;
// }

// export operator+(vector is Vector, keepout is Keepout)
// precondition
// {
//     is2dPoint(vector);
// }
// {
//     return keepout + vector;
// }

// export operator-(keepout is Keepout, vector is Vector)
// precondition
// {
//     is2dPoint(vector);
// }
// {
//     return keepout + (-vector);
// }

// export operator-(vector is Vector, keepout is Keepout)
// precondition
// {
//     is2dPoint(vector);
// }
// {
//     return keepout + (-vector);
// }

// /**
//  * Defines a rectangular axis-aligned region other features should not be allowed to enter.
//  * @type {{
//  *      @param firstCorner {Vector} :
//  *              A 2D `Vector` representing the min corner (bottom left) of the keepout region.
//  *      @param secondCorner {Vector} :
//  *              A 2D `Vector` representing the max corner (upper right) of the keepout region.
//  * }}
//  */
// export type Keepout typecheck canBeKeepout;

// export predicate canBeKeepout(value)
// {
//     value is map;
//     is2dPoint(value.firstCorner);
//     is2dPoint(value.secondCorner);

//     value.firstCorner[0] - TOLERANCE.zeroLength * meter <= value.secondCorner[0];
//     value.firstCorner[1] - TOLERANCE.zeroLength * meter <= value.secondCorner[1];
// }

// /**
//  * API: keepout
//  * A constructor for a `Keepout`.
//  * The `firstCorner` should be the bottom left corner. The `secondCorner` should be the top right.
//  */
// export function keepout(firstCorner is Vector, secondCorner is Vector) returns Keepout
// precondition
// {
//     canBeKeepout({ "firstCorner" : firstCorner, "secondCorner" : secondCorner });
// }
// {
//     return { "firstCorner" : firstCorner, "secondCorner" : secondCorner } as Keepout;
// }

// /**
//  * A constructor for a `Keepout`.
//  * @param definition {{
//  *          @field location {Vector} : @optional
//  *                   A 2D `Vector` defining the center of the keepout region.
//  *          @field height {ValueWithUnits} :
//  *                  The height of the keepout region.
//  *          @field width {ValueWithUnits} : @optional
//  *                  The `width` of the keepout region.
//  *                  @autocomplete `width`
//  *          @field leftWidth {ValueWithUnits} : @requiredif {`width` == `undefined`}
//  *                  The `leftWidth` of the keepout region. May be specified as an alternative to `width`.
//  *                  Should be negative to be left of center.
//  *          @field rightWidth {ValueWithUnits} : @requiredif {`width` == `undefined`}
//  *                  The `rightWidth` of the keepout region. May be specified as an alternative to `width`.
//  *                  Should be positive to be right of center.
//  *          @field scale {Number} : @optional
//  *                  A number to scale the `Keepout` by.
//  * }}
//  */
// export function keepout(definition is map) returns Keepout
// precondition
// {
//     is2dPoint(definition.location) || definition.location is undefined;
//     isLength(definition.height);

//     isLength(definition.width) || definition.width is undefined;
//     if (definition.width is undefined)
//     {
//         isLength(definition.leftWidth);
//         isLength(definition.rightWidth);
//     }
//     definition.scale is number || definition.scale is undefined;
// }
// {
//     definition = mergeMaps({ "location" : zeroVector(2) * meter, "scale" : 1 }, definition);
//     if (definition.width != undefined)
//     {
//         definition.leftWidth = -definition.width / 2;
//         definition.rightWidth = definition.width / 2;
//     }

//     const firstCorner = definition.location + vector(definition.leftWidth, -definition.height / 2) * definition.scale;
//     const secondCorner = definition.location + vector(definition.rightWidth, definition.height / 2) * definition.scale;
//     return keepout(firstCorner, secondCorner);
// }

// /**
//  * Merges two keepouts into a single, larger keepout containing both of them.
//  */
// export function mergeKeepouts(keepout1 is Keepout, keepout2 is Keepout) returns Keepout
// {
//     const firstCorner = vector(min(keepout1.firstCorner[0], keepout2.firstCorner[0]), min(keepout1.firstCorner[1], keepout2.firstCorner[1]));
//     const secondCorner = vector(max(keepout1.secondCorner[0], keepout2.secondCorner[0]), max(keepout1.secondCorner[1], keepout2.secondCorner[1]));
//     return keepout(firstCorner, secondCorner);
// }

// /**
//  * Returns `true` if a point violates a specified `Keepout` region.
//  * @param keepout : @autocomplete `definition.keepout`
//  * @param point : @autocomplete `definition.start`
//  */
// export function pointIsWithinKeepout(keepout is Keepout, point is Vector)
// {
//     return point[0] > keepout.firstCorner[0] && point[0] < keepout.secondCorner[0] &&
//         point[1] > keepout.firstCorner[1] && point[1] < keepout.secondCorner[1];
// }

// export function keepoutHeight(keepout is Keepout) returns ValueWithUnits
// {
//     return keepout.secondCorner[1] - keepout.firstCorner[1];
// }

// export function keepoutWidth(keepout is Keepout) returns ValueWithUnits
// {
//     return keepout.secondCorner[0] - keepout.firstCorner[0];
// }

// export function keepoutCenter(keepout is Keepout) returns Vector
// {
//     return (keepout.firstCorner + keepout.secondCorner) / 2;
// }

// /**
//  * Returns the top left corner of a `Keepout`.
//  */
// export function keepoutTopLeft(keepout is Keepout) returns Vector
// {
//     return vector(keepout.firstCorner[0], keepout.secondCorner[1]);
// }

// /**
//  * Returns the bottom right corner of a `Keepout`.
//  */
// export function keepoutBottomRight(keepout is Keepout) returns Vector
// {
//     return vector(keepout.secondCorner[0], keepout.firstCorner[1]);
// }

// /**
//  * Creates a rectangle along the borders of a Keepout.
//  * @param value {{
//  *          @field keepout {Keepout} :
//  *                  The keepout region to draw along.
//  * }}
//  */
// export function skKeepoutBoundary(sketch is Sketch, textId is string, value is map)
// {
//     skRectangle(sketch, textId, value.keepout);
// }

// /**
//  * Creates a sketch line while enforcing a given `Keepout` region.
//  * Notably does not handle situations where the line passes entirely through the keepout region; at least one endpoint must lie within it.
//  * @param textId : @autocomplete `"skKeepoutLine1"`
//  * @param value {{
//  *      @field keepout {Keepout} : @autocomplete `keepout`
//  *              The keepout region to enforce.
//  *      @field start {Vector} : @autocomplete `vector(0 * meter, 0 * meter)`
//  *      @field end {Vector} : @autocomplete `vector(0 * meter, 0 * meter)`
//  * }}
//  */
// export function skKeepoutLineSegment(sketch is Sketch, textId is string, value is map)
// {
//     if (pointIsWithinKeepout(value.keepout, value.start) && pointIsWithinKeepout(value.keepout, value.end))
//     {
//         return;
//     }

//     if (pointIsWithinKeepout(value.keepout, value.start))
//     {
//         value.start = lineKeepoutIntersection(value.keepout, value.start, value.end);
//     }

//     if (pointIsWithinKeepout(value.keepout, value.end))
//     {
//         value.end = lineKeepoutIntersection(value.keepout, value.end, value.start);
//     }

//     skLineSegment(sketch, textId, value);
// }

// function lineKeepoutIntersection(keepout is Keepout, point1 is Vector, point2 is Vector)
// {
//     const minX = keepout.firstCorner[0];
//     const maxX = keepout.secondCorner[0];
//     const minY = keepout.firstCorner[1];
//     const maxY = keepout.secondCorner[1];

//     var intersection;
//     if (point2[0] < minX) // If the second point of the segment is at left/bottom-left/top-left of the AABB
//     {
//         if (point2[1] > minY && point2[1] < maxY)
//         {
//             return lineSegmentIntersection(point1, point2, vector(minX, minY), vector(minX, maxY));
//         } // If it is at the left
//         else if (point2[1] < minY) // If it is at the bottom-left
//         {
//             intersection = lineSegmentIntersection(point1, point2, vector(minX, minY), vector(maxX, minY));
//             if (intersection == undefined)
//                 intersection = lineSegmentIntersection(point1, point2, vector(minX, minY), vector(minX, maxY));
//             return intersection;
//         }
//         else // If point2[1] > maxY, i.e. if it is at the top-left
//         {
//             intersection = lineSegmentIntersection(point1, point2, vector(minX, maxY), vector(maxX, maxY));
//             if (intersection == undefined)
//                 intersection = lineSegmentIntersection(point1, point2, vector(minX, minY), vector(minX, maxY));
//             return intersection;
//         }
//     }
//     else if (point2[0] > maxX) //If the second point of the segment is at right/bottom-right/top-right of the AABB
//     {
//         if (point2[1] > minY && point2[1] < maxY)
//         {
//             return lineSegmentIntersection(point1, point2, vector(maxX, minY), vector(maxX, maxY));
//         } // If it is at the right
//         else if (point2[1] < minY) //If it is at the bottom-right
//         {
//             intersection = lineSegmentIntersection(point1, point2, vector(minX, minY), vector(maxX, minY));
//             if (intersection == undefined)
//                 intersection = lineSegmentIntersection(point1, point2, vector(maxX, minY), vector(maxX, maxY));
//             return intersection;
//         }
//         else // If point2[1] > maxY, i.e. if it is at the top-left
//         {
//             intersection = lineSegmentIntersection(point1, point2, vector(minX, maxY), vector(maxX, maxY));
//             if (intersection == undefined)
//                 intersection = lineSegmentIntersection(point1, point2, vector(maxX, minY), vector(maxX, maxY));
//             return intersection;
//         }
//     }
//     else // If the second point of the segment is at top/bottom of the AABB
//     {
//         if (point2[1] < minY)
//             return lineSegmentIntersection(point1, point2, vector(minX, minY), vector(maxX, minY)); //If it is at the bottom
//         if (point2[1] > maxY)
//             return lineSegmentIntersection(point1, point2, vector(minX, maxY), vector(maxX, maxY)); //If it is at the top
//     }
//     return undefined;
// }

// /**
//  * Creates a sketch arc. If the arc's endpoint falls within the specified `Keepout` region, the arc is automatically adjusted to comply.
//  * @param textId {string} : @autocomplete `keepoutArc`
//  * @param value {{
//  *          @field keepout {Keepout} : @autocomplete `keepout`
//  *          @field start {Vector} : @autocomplete `start`
//  *          @field mid {Vector} : @autocomplete `mid`
//  *          @field end {Vector} : @autocomplete `end`
//  *          @field center {Vector} : @autocomplete `centerPoint`
//  * }}
//  */
// export function skKeepoutArc(sketch is Sketch, textId is string, value is map)
// {
//     if (pointIsWithinKeepout(value.keepout, value.start) && pointIsWithinKeepout(value.keepout, value.end))
//     {
//         return; // arc is completely inside keepout
//     }

//     const intersectionPoints = arcKeepoutIntersectionPoints(value.keepout, value.start, value.mid, value.end, value.center);

//     if (size(intersectionPoints) == 2)
//     {
//         var closestPoint;
//         var furthestPoint;
//         if (squaredNorm(value.start - intersectionPoints[0]) > squaredNorm(value.start - intersectionPoints[1]))
//         {
//             closestPoint = intersectionPoints[1];
//             furthestPoint = intersectionPoints[0];
//         }
//         else
//         {
//             closestPoint = intersectionPoints[0];
//             furthestPoint = intersectionPoints[1];
//         }

//         skArc(sketch, textId ~ 0, {
//                     "start" : value.start,
//                     "mid" : arcMidPoint(value.start, closestPoint, value.center),
//                     "end" : closestPoint
//                 });

//         skArc(sketch, textId ~ 1, {
//                     "start" : furthestPoint,
//                     "mid" : arcMidPoint(furthestPoint, value.end, value.center),
//                     "end" : value.end
//                 });
//     }
//     else if (size(intersectionPoints) == 1)
//     {
//         if (pointIsWithinKeepout(value.keepout, value.start))
//         {
//             // start is invalid; draw from intersectionPoints[0] to end
//             value.start = intersectionPoints[0];
//         }
//         else // end is invalid; draw from start to end
//         {
//             value.end = intersectionPoints[0];
//         }
//         value.mid = arcMidPoint(value.start, value.end, value.center);
//         skArc(sketch, textId, value);
//     }
//     else
//     {
//         skArc(sketch, textId, value);
//     }
// }

// function arcKeepoutIntersectionPoints(keepout is Keepout, startPoint is Vector, midPoint is Vector, endPoint is Vector, centerPoint is Vector)
// {
//     const radius = norm(startPoint - centerPoint);
//     const topLeft = vector(keepout.firstCorner[0], keepout.secondCorner[1]);
//     const bottomRight = vector(keepout.secondCorner[0], keepout.firstCorner[1]);
//     var points = [];

//     points = concatenateArrays([points, lineSegmentCircleIntersection(centerPoint, radius, topLeft, keepout.secondCorner)]);
//     points = concatenateArrays([points, lineSegmentCircleIntersection(centerPoint, radius, keepout.firstCorner, bottomRight)]);
//     points = concatenateArrays([points, lineSegmentCircleIntersection(centerPoint, radius, topLeft, keepout.firstCorner)]);
//     points = concatenateArrays([points, lineSegmentCircleIntersection(centerPoint, radius, keepout.secondCorner, bottomRight)]);

//     var resultPoints = [];
//     for (var point in points)
//     {
//         // only one point should be on the arc
//         if (isOnArc(startPoint, midPoint, endPoint, centerPoint, point))
//         {
//             resultPoints = append(resultPoints, point);
//         }
//     }
//     return resultPoints;
// }

// /**
//  * Returns the intersection points between a circle and a line. Note that the line segment should be in proper order.
//  */
// function lineSegmentCircleIntersection(centerPoint is Vector, radius is ValueWithUnits, startPoint is Vector, endPoint is Vector)
// {
//     var points = [];
//     var intersectionPoints = lineCircleIntersection(centerPoint, radius, startPoint, endPoint);

//     for (var point in intersectionPoints)
//     {
//         if (tolerantEquals(norm(endPoint - startPoint), norm(point - startPoint) + norm(endPoint - point)))
//         {
//             points = append(points, point);
//         }
//     }
//     return points;
// }

// /**
//  * Returns an array of intersections between a line segment and a circle, or `[]` if none exists.
//  */
// export function lineCircleIntersection(centerPoint is Vector, radius is ValueWithUnits, point1 is Vector, point2 is Vector) returns array
// {
//     const dx = point2[0] - point1[0];
//     const dy = point2[1] - point1[1];

//     const A = dx * dx + dy * dy;
//     const B = 2 * (dx * (point1[0] - centerPoint[0]) + dy * (point1[1] - centerPoint[1]));
//     const C = (point1[0] - centerPoint[0]) * (point1[0] - centerPoint[0]) +
//         (point1[1] - centerPoint[1]) * (point1[1] - centerPoint[1]) -
//         radius * radius;

//     const det = B * B - 4 * A * C;

//     if ((A <= TOLERANCE.zeroLength * meter ^ 2) || (det < 0 * meter ^ 4))
//     {
//         // // No solutions
//         return [];
//     }
//     else if (det == 0)
//     {
//         // One solution
//         const t = -B / (2 * A);
//         return [vector(point1[0] + t * dx, point1[1] + t * dy)];
//     }
//     else
//     {
//         // Two solutions
//         var t = ((-B + sqrt(det)) / (2 * A));
//         var intersection1 = vector(point1[0] + t * dx, point1[1] + t * dy);
//         t = ((-B - sqrt(det)) / (2 * A));
//         var intersection2 = vector(point1[0] + t * dx, point1[1] + t * dy);
//         return [intersection1, intersection2];
//     }
// }

// /**
//  * Returns a `Vector` representing the intersection betweeen two line segments. Note that the line segments must actually touch.
//  */
// function lineSegmentIntersection(point1 is Vector, point2 is Vector, point3 is Vector, point4 is Vector) returns Vector
// {
//     const s1_x = point2[0] - point1[0];
//     const s1_y = point2[1] - point1[1];
//     const s2_x = point4[0] - point3[0];
//     const s2_y = point4[1] - point3[1];

//     const s = (-s1_y * (point1[0] - point3[0]) + s1_x * (point1[1] - point3[1])) / (-s2_x * s1_y + s1_x * s2_y);
//     const t = (s2_x * (point1[1] - point3[1]) - s2_y * (point1[0] - point3[0])) / (-s2_x * s1_y + s1_x * s2_y);

//     if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
//     {
//         return vector(point1[0] + (t * s1_x), point1[1] + (t * s1_y));
//     }

//     return undefined;
// }