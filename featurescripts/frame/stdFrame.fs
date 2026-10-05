FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/frame.fs", version : "2960.0");


export predicate frameSelectionPredicate(definition is map)
{
    annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
    {
        annotation {
                    "Name" : "Edge to use",
                    "Description" : "An edge to place the frame",
                    "MaxNumberOfPicks" : 1,
                    "Filter" : (EntityType.EDGE && GeometryType.LINE) || (EntityType.BODY && BodyType.WIRE && GeometryType.LINE)
                }
        definition.edgeToUse is Query;

        annotation { "Name" : "Angle" }
        isAngle(definition.angle, ANGLE_360_ZERO_DEFAULT_BOUNDS);

        annotation {
                    "Name" : "Mirror across Y axis",
                    "UIHint" : UIHint.OPPOSITE_DIRECTION
                }
        definition.mirrorProfile is boolean;
    }

    annotation { "Name" : "Index", "UIHint" : UIHint.ALWAYS_HIDDEN }
    isInteger(definition.index, FRAME_NINE_POINT_COUNT); // for points manipulator
}
