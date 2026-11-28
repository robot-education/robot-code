FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/hole.fs", version : "2960.0");

/**
 * Emulating std's hole: its parameters, with its names and ids, and what they do, for features that cut holes of their
 * own.
 */

export predicate holeDiameterPredicate(definition is map)
{
    annotation { "Name" : "Hole diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
    isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
}

/**
 * Creates the parameter `scope`.
 */
export predicate holeMergeScopePredicate(definition is map)
{
    annotation { "Name" : "Merge scope",
                "Filter" : (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES && AllowMeshGeometry.YES) }
    definition.scope is Query;
}

/**
 * Std's hole's Start plane options (`HoleStartStyle`), but for Start from part.
 */
export enum HoleStartPlane
{
    annotation { "Name" : "Start from sketch plane" }
    SKETCH,
    annotation { "Name" : "Start from selected plane" }
    PLANE
}

/**
 * Std's hole's Start plane, with its names and ids: `startStyle` (a `HoleStartPlane`), and `startBoundEntity` to start
 * from a selected plane (see `holeStartPlane`).
 */
export predicate holeStartPredicate(definition is map)
{
    annotation { "Name" : "Start plane", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.startStyle is HoleStartPlane;

    if (definition.startStyle == HoleStartPlane.PLANE)
    {
        annotation { "Name" : "Hole start plane or mate connector", "Filter" : (EntityType.FACE && GeometryType.PLANE) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
        definition.startBoundEntity is Query;
    }
}

/**
 * Where a hole at `plane` starts, as std's hole's Start plane puts it: `plane`, or where its axis meets
 * `startBoundEntity`, which must be parallel to it.
 */
export function holeStartPlane(context is Context, definition is map, plane is Plane) returns Plane
{
    if (definition.startStyle != HoleStartPlane.PLANE)
    {
        return plane;
    }
    if (isQueryEmpty(context, definition.startBoundEntity))
    {
        throw regenError(ErrorStringEnum.HOLE_NO_START_BOUND, ["startBoundEntity"]);
    }
    const start = evPlane(context, { "face" : definition.startBoundEntity });
    if (!parallelVectors(plane.normal, start.normal))
    {
        throw regenError(ErrorStringEnum.HOLE_START_BOUND_INVALID, ["startBoundEntity"], definition.startBoundEntity);
    }
    plane.origin = intersection(start, line(plane.origin, plane.normal)).intersection;
    return plane;
}

/**
 * A `HoleStartPlane` as std's hole's own, for its heuristics.
 */
export function holeStartStyle(startStyle is HoleStartPlane) returns HoleStartStyle
{
    return startStyle == HoleStartPlane.PLANE ? HoleStartStyle.PLANE : HoleStartStyle.SKETCH;
}

// export predicate holeEndStylePredicate(definition is map)
// {
//     annotation { "Name" : "Termination", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
//     definition.endStyleV2 is HoleEndStyleV2;

//     // annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
//     // definition.oppositeDirection is boolean;

//     if (definition.endStyleV2 == HoleEndStyleV2.UP_TO_ENTITY || definition.endStyleV2 == HoleEndStyleV2.UP_TO_NEXT)
//     {
//         if (definition.endStyleV2 == HoleEndStyleV2.UP_TO_ENTITY)
//         {
//             annotation { "Name" : "Up to entity or mate connector",
//                         "Filter" : (EntityType.FACE && SketchObject.NO && AllowMeshGeometry.YES) || QueryFilterCompound.ALLOWS_VERTEX,
//                         "MaxNumberOfPicks" : 1 }
//             definition.endBoundEntity is Query;
//         }

//         annotation { "Name" : "Offset from tip", "Column Name" : "Has offset", "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW"] }
//         definition.offset is boolean;

//         if (definition.offset)
//         {
//             annotation { "Name" : "Offset from tip", "UIHint" : UIHint.DISPLAY_SHORT }
//             isLength(definition.offsetDistance, ZERO_INCLUSIVE_OFFSET_BOUNDS);

//             annotation { "Name" : "Opposite direction", "Column Name" : "Offset opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
//             definition.oppositeOffsetDirection is boolean;
//         }
//     }
// }

export function sketchHoleLocations(context is Context, id is Id, plane is Plane, holeLocations is array) returns Query
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    for (var i, location in holeLocations)
    {
        skPoint(sketch, "point" ~ i, { "position" : location });
    }
    skSolve(sketch);
    return qCreatedBy(id + "sketch", EntityType.VERTEX);
}

/**
 * Creates a sketch point at the center of `plane`.
 */
export function sketchHoleLocation(context is Context, id is Id, plane is Plane) returns Query
{
    return sketchHoleLocations(context, id, plane, [zeroVector(2) * meter]);
}

export const HOLE_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.25,
            (foot) : 0.02,
            (yard) : 0.007
        } as LengthBoundSpec;

export const HOLE_DEPTH_BOUNDS =
{
            (meter) : [1e-5, 0.012, 500],
            (centimeter) : 1.2,
            (millimeter) : 12.0,
            (inch) : 0.5,
            (foot) : 0.04,
            (yard) : 0.014
        } as LengthBoundSpec;
