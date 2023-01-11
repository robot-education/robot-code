FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export import(path : "onshape/std/frame.fs", version : "1930.0");
import(path : "onshape/std/frameAttributes.fs", version : "1930.0");

export import(path : "onshape/std/frameUtils.fs", version : "1930.0");

// in `extendFrames` we pad our frame extrusion length to help avoid non-manifold cases in boolean operations
const EXTEND_FRAMES_PAD_LENGTH = .1 * millimeter;

export const TUBE_PLANE_ATTRIBUTE = "tubePlaneAttribute";

export predicate framePredicate(definition is map)
{
    annotation {
                "Name" : "Selections",
                "Description" : "Faces, edges, and vertices that define sweep paths",
                "Filter" : ((EntityType.FACE && ConstructionObject.NO) || (EntityType.EDGE) || (EntityType.VERTEX && AllowEdgePoint.NO) || (EntityType.BODY && BodyType.WIRE))
            }
    definition.selections is Query;

    annotation { "Name" : "Merge tangent segments", "Default" : true }
    definition.mergeTangentSegments is boolean;

    annotation { "Name" : "Angle" }
    isAngle(definition.angle, ANGLE_360_ZERO_DEFAULT_BOUNDS);

    annotation { "Name" : "Mirror across Y axis", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.mirrorProfile is boolean;

    annotation { "Name" : "Default corner type", "UIHint" : UIHint.SHOW_LABEL, "Default" : "BUTT" }
    definition.defaultCornerType is FrameCornerType;

    if (definition.defaultCornerType == FrameCornerType.BUTT || definition.defaultCornerType == FrameCornerType.COPED_BUTT)
    {
        annotation {
                    "Name" : "Flip corner",
                    "UIHint" : UIHint.OPPOSITE_DIRECTION
                }
        definition.defaultButtFlip is boolean;
    }

    annotation {
                "Name" : "Corner overrides",
                "Item name" : "vertex",
                "Driven query" : "vertex",
                "Item label template" : "#vertex [#cornerType]"
            }
    definition.cornerOverrides is array;
    for (var corner in definition.cornerOverrides)
    {
        annotation {
                    "Name" : "Vertex",
                    "Filter" : EntityType.VERTEX && ConstructionObject.NO,
                    "MaxNumberOfPicks" : 1,
                    "UIHint" : UIHint.ALWAYS_HIDDEN
                }
        corner.vertex is Query;

        annotation { "Name" : "Corner type", "UIHint" : UIHint.SHOW_LABEL, "Default" : "BUTT" }
        corner.cornerType is FrameCornerType;

        if (corner.cornerType == FrameCornerType.BUTT || corner.cornerType == FrameCornerType.COPED_BUTT)
        {
            annotation { "Name" : "Flip corner", "UIHint" : UIHint.OPPOSITE_DIRECTION }
            corner.cornerButtFlip is boolean;
        }
    }

    annotation { "Name" : "Limit frame ends", "Default" : false }
    definition.trim is boolean;

    if (definition.trim)
    {
        annotation {
                    "Group Name" : "Trimming",
                    "Collapsed By Default" : false,
                    "Driving Parameter" : "trim"
                }
        {
            annotation {
                        "Name" : "Faces to trim to",
                        "Description" : "Planes and planar faces to use as trim tools",
                        "Filter" : (EntityType.FACE && GeometryType.PLANE)
                    }
            definition.trimPlanes is Query;

            annotation {
                        "Name" : "Parts to trim to",
                        "Description" : "Parts to use as trim tools",
                        "Filter" : EntityType.BODY && BodyType.SOLID
                    }
            definition.trimBodies is Query;
        }
    }
    annotation { "Name" : "Index", "UIHint" : UIHint.ALWAYS_HIDDEN }
    isInteger(definition.index, FRAME_NINE_POINT_COUNT);
}


// /** @internal */
// export function frameEditLogicFunction(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
// {
//     if (oldDefinition.cornerOverrides != undefined)
//     {
//         definition = handleNewCornerOverride(context, oldDefinition, definition);
//     }
//     return definition;
// }

// /** @internal */
// export function frameManipulators(context is Context, definition is map, newManipulators is map) returns map
// {
//     try silent
//     {
//         var newAngle is ValueWithUnits = newManipulators["angleManipulator"].angle;
//         definition.angle = newAngle;
//     }
//     try silent
//     {
//         definition.index = newManipulators["points"].index;
//     }
//     for (var i = 0; i < size(definition.cornerOverrides); i += 1)
//     {
//         if (newManipulators["flip" ~ i] != undefined)
//         {
//             definition.cornerOverrides[i].cornerButtFlip = newManipulators["flip" ~ i].flipped;
//         }
//     }
//     return definition;
// }

// /** @internal */
// export function setFrameAttributes(context is Context, frame is Query, profileData is map, frameData is map)
// {
//     setFrameProfileAttribute(context, frame, profileData.profileAttribute);
//     setFrameTopologyAttribute(context, frameData.sweptFaces, frameTopologyAttributeForSwept(FrameTopologyType.SWEPT_FACE));
//     if (!isQueryEmpty(context, frameData.sweptEdges))
//     {
//         setFrameTopologyAttribute(context, frameData.sweptEdges, frameTopologyAttributeForSwept(FrameTopologyType.SWEPT_EDGE));
//     }
//     setFrameTopologyAttribute(context, frameData.startFace, frameTopologyAttributeForCapFace(true, false, false));
//     setFrameTopologyAttribute(context, frameData.endFace, frameTopologyAttributeForCapFace(false, false, false));
// }

// /** @internal */
// export function setFrameTerminusAttributes(context is Context, startFace is Query, endFace is Query)
// {
//     setFrameTopologyAttribute(context, startFace, frameTopologyAttributeForCapFace(true, true, false));
//     setFrameTopologyAttribute(context, endFace, frameTopologyAttributeForCapFace(false, true, false));
// }

// function handleNewCornerOverride(context is Context, oldDefinition is map, definition is map) returns map
// {
//     // on joint override creation, set the starting value to the global default
//     const newOverrideSize = size(definition.cornerOverrides);
//     const oldOverrideSize = size(oldDefinition.cornerOverrides);
//     if (newOverrideSize == oldOverrideSize + 1)
//     {
//         var newOverride = last(definition.cornerOverrides);
//         newOverride.cornerType = definition.defaultCornerType;
//         if (isButtCorner(newOverride.cornerType))
//         {
//             newOverride.cornerButtFlip = definition.defaultButtFlip;
//         }
//         definition.cornerOverrides[newOverrideSize - 1] = newOverride;
//     }
//     return definition;
// }

const FRAME_TRANSFORM_ATTRIBUTE_NAME = "frameTransform";

/**
 * Sets an attribute specifying the location of a frame.
 * The location is specified via a `CoordSystem` with its `zAxis` along the direction of the frame and its `xAxis` with the
 * standard orientation. The origin is not specified relative to the frame but is presumed to lie between the frame's start
 * and end face.
 */
function setFrameTransformAttributes(context is Context, pathSweepData is array)
{
    var currTransform = identityTransform();
    for (var sweepData in pathSweepData)
    {
        println(sweepData.transform);
        currTransform = currTransform * sweepData.transform;
        setAttribute(context, {
                    "entities" : qCreatedBy(sweepData.id, EntityType.BODY),
                    "name" : FRAME_TRANSFORM_ATTRIBUTE_NAME,
                    "attribute" : sweepData.transform * WORLD_COORD_SYSTEM
                });
    }
}

export function getFrameTransformAttribute(context is Context, frame is Query)
{
    return getAttribute(context, {
                "entity" : frame,
                "name" : FRAME_TRANSFORM_ATTRIBUTE_NAME
            });
}
