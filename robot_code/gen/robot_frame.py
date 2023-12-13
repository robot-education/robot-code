from featurescript import *
from featurescript.core.func import Call, Return
from featurescript.core.func import Return
from robot_code.robot_studio import RobotFeature

DESCRIPTION = r"Generate robot frame members."

robot_frame = RobotFeature("frame", description=DESCRIPTION)
studio = robot_frame.ui_studio

studio.add_import("stdFrameUi.fs", export=True)

# enums
wall_thickness = (
    EnumBuilder("WallThickness", parent=studio, value_type=LookupEnumValue)
    .add_value("ONE_SIXTEENTH", "1/16 in.", lookup_value=inch(0.0625))
    .add_value("ONE_EIGHTH", "1/8 in.", lookup_value=inch(0.125))
    .add_custom(lookup_value=definition("customWallThickness"))
    .build()
)

fit = EnumBuilder("HoleFit", parent=studio).add_value("CLOSE").add_value("FREE").build()

hole_size = (
    EnumBuilder("HoleSize", parent=studio)
    .add_value("NO_8", "#8")
    .add_value("NO_10", "#10")
    .add_custom()
    .build()
)

# pattern_method = (
#     enum_factory.add_enum("PatternMethod", parent=studio)
#     .add_value("END")
#     .add_value("CENTER_HOLE")
#     .add_value("CENTER_GAP")
#     .build()
# )
# pattern_method_predicates = enum_predicates(pattern_method, parent=studio)

tube_size = (
    EnumBuilder("TubeSize", parent=studio, generate_predicates=True)
    .add_value("ONE_BY_ONE", "1x1")
    .add_value("TWO_BY_ONE", "2x1")
    .add_custom()
    .build()
)

tube_type = (
    EnumBuilder("TubeType", parent=studio, generate_predicates=True)
    .add_value("MAX_TUBE", "MAXTube")
    .add_custom()
    .build()
)

max_pattern_type = (
    EnumBuilder("MaxTubePatternType", parent=studio)
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX")
    .build()
)

max_tube_profile = (
    EnumBuilder("MaxTubeProfileType", parent=studio, annotate=False)
    .add_value("ONE_BY_ONE")
    .add_value("TWO_BY_ONE")
    .add_value("TWO_BY_ONE_LIGHT")
    .build()
)

tube_face = (
    EnumBuilder("TubeFace", parent=studio, annotate=False)
    .add_value("FIRST")
    .add_value("SECOND")
    .build()
)

two_inch_face = (
    EnumBuilder("TwoInchFaceHoleCount", parent=studio, value_type=LookupEnumValue)
    .add_value("FOUR", display_name="Four holes", lookup_value=4)
    .add_value("THREE", display_name="Three holes", lookup_value=3)
    .add_value("TWO", display_name="Two holes", lookup_value=2)
    .add_value("ONE", display_name="One hole", lookup_value=1)
    .add_value("NONE", lookup_value=0)
    .build()
)

one_inch_face = (
    EnumBuilder("OneInchFaceHoleCount", parent=studio, value_type=LookupEnumValue)
    .add_value("TWO", display_name="Two holes", lookup_value=2)
    .add_value("ONE", display_name="One hole", lookup_value=1)
    .add_value("NONE", lookup_value=0)
    .build()
)

studio.add(
    is_max_tube := UiTestPredicate(
        "isMaxTube",
        ~tube_size["CUSTOM"] & tube_type["MAX_TUBE"],
    ),
    can_be_light := UiTestPredicate(
        "canBeLight",
        max_pattern_type["NONE"] | max_pattern_type["GRID"],
    ),
    is_max_pattern_max_tube := UiTestPredicate(
        "isMaxPatternMaxTube",
        tube_size["TWO_BY_ONE"] & tube_type["MAX_TUBE"] & max_pattern_type["MAX"],
    ),
    is_no_pattern_max_tube := UiTestPredicate(
        "isNoPatternMaxTube",
        tube_size["TWO_BY_ONE"] & tube_type["MAX_TUBE"] & max_pattern_type["NONE"],
    ),
    # True for any tube without preset holes.
    has_predrilled_holes := UiTestPredicate(
        "hasPredrilledHoles",
        is_max_tube,
    ),
    has_custom_holes := UiTestPredicate(
        "hasCustomHoles", tube_type["CUSTOM"] | is_no_pattern_max_tube
    ),
    is_hole_size_set := UiTestPredicate(
        "isHoleSizeSet", hole_size["NO_8"] | hole_size["NO_10"]
    ),
    has_holes := UiTestPredicate(
        "hasHoles",
        has_predrilled_holes | definition("hasHoles"),
    ),
    can_have_two_inch_face := UiTestPredicate(
        "canHaveTwoInchFace",
        is_no_pattern_max_tube | Parens(tube_type["CUSTOM"] & tube_size["TWO_BY_ONE"]),
    ),
    can_have_one_inch_face := UiTestPredicate(
        "canHaveOneInchFace",
        tube_type["CUSTOM"] & Parens(tube_size["ONE_BY_ONE"] | tube_size["TWO_BY_ONE"]),
    ),
)

# ui predicates
studio.add(
    two_inch_spacing_bounds := LengthBoundSpec(
        "TWO_INCH_SPACING_BOUNDS",
        min=ZERO_TOLERANCE,
        default=inch_to_meter(1),
        inch_default=0.5,
    ),
    one_inch_spacing_bounds := LengthBoundSpec(
        "ONE_INCH_SPACING_BOUNDS",
        min=ZERO_TOLERANCE,
        default=inch_to_meter(0.5),
        inch_default=0.5,
    ),
    spacing_bounds := LengthBoundSpec(
        "SPACING_BOUNDS",
        min=ZERO_TOLERANCE,
        default=inch_to_meter(0.5),
        millimeter_default=5,
        inch_default=0.5,
    ),
)

tube_face_predicate = UiPredicate("tubeFace", parent=studio).add(
    IfBlock(can_have_two_inch_face).add(
        labeled_enum_parameter(two_inch_face, display_name="2 in. face hole count"),
        IfBlock(any(two_inch_face, "TWO", "THREE", "FOUR")).add(
            length_parameter(
                "twoInchFaceSpacing",
                bound_spec=two_inch_spacing_bounds,
                display_name="2 in. face spacing",
            )
        ),
    ),
    IfBlock(can_have_one_inch_face).add(
        labeled_enum_parameter(one_inch_face, display_name="1 in. face hole count"),
        IfBlock(one_inch_face["TWO"]).add(
            length_parameter(
                "oneInchFaceSpacing",
                bound_spec=one_inch_spacing_bounds,
                display_name="1 in. face spacing",
            )
        ),
    ),
    IfBlock(tube_size["CUSTOM"]).add(
        integer_parameter("firstFaceCount", display_name="First face hole count"),
        length_parameter(
            "firstFaceSpacing",
            bound_spec=spacing_bounds,
        ),
        integer_parameter("secondFaceCount", display_name="Second face hole count"),
        length_parameter(
            "secondFaceSpacing",
            bound_spec=spacing_bounds,
        ),
    ),
    IfBlock(has_custom_holes).add(
        length_parameter("distance", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS),
    ),
    # enum_parameter(pattern_method, display_name="Start location", ui_hints=SHOW_LABEL_HINT),
    # IfBlock(pattern_method_predicates["END"]).add(
    boolean_parameter("flipStart", display_name="Flip pattern start", ui_hints=None),
    IfBlock(has_custom_holes).add(
        boolean_parameter("customStartOffset"),
        IfBlock(definition("customStartOffset")).add(
            length_parameter(
                "startOffset",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
            )
        ),
        boolean_parameter("customEndOffset"),
        IfBlock(definition("customEndOffset")).add(
            length_parameter(
                "endOffset",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
            )
        ),
    )
    # CountParameter("Holes to tie to end", bound_spec=)
)

hole_predicate = UiPredicate("tubeHoleDiameter", parent=studio).add(
    IfBlock(~has_predrilled_holes)
    .add(
        labeled_enum_parameter(
            hole_size,
            default="NO_10",
        ),
        IfBlock(is_hole_size_set).add(labeled_enum_parameter(fit)),
    )
    .or_else()
    .add(boolean_parameter("overrideHoleDiameter")),
    IfBlock(
        Parens(~has_predrilled_holes & ~is_hole_size_set)
        | Parens(has_predrilled_holes & definition("overrideHoleDiameter"))
    ).add(length_parameter("holeDiameter", bound_spec=LengthBound.BLEND_BOUNDS)),
    boolean_parameter("finish", default=True),
)

wall_predicate = UiPredicate("wallThickness", parent=studio).add(
    labeled_enum_parameter(wall_thickness),
    IfBlock(wall_thickness["CUSTOM"]).add(
        length_parameter(
            "customWallThickness",
            bound_spec=LengthBound.SHELL_OFFSET_BOUNDS,
            display_name="Wall thickness",
        )
    ),
)

tube_size_predicate = UiPredicate("tubeSize", parent=studio).add(
    labeled_enum_parameter(tube_size, display_name="Size", default="TWO_BY_ONE"),
    IfBlock(tube_size["CUSTOM"])
    .add(
        length_parameter(
            "firstFaceWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
        ),
        length_parameter(
            "secondFaceWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
        ),
    )
    .or_else()
    .add(
        labeled_enum_parameter(tube_type, default="CUSTOM", display_name="Type"),
        IfBlock(is_max_tube).add(
            IfBlock(tube_size["TWO_BY_ONE"]).add(
                labeled_enum_parameter(
                    max_pattern_type, display_name="Pattern type", default="GRID"
                ),
                IfBlock(can_be_light).add(
                    boolean_parameter("isLight", display_name="Light"),
                ),
            ),
            boolean_parameter(
                "hasScribeLines", display_name="Draw scribe lines", default=True
            ),
        ),
    ),
    IfBlock(tube_size["CUSTOM"] | tube_type["CUSTOM"]).add(wall_predicate),
)


studio.add(
    tube_predicate := UiPredicate("tube").add(
        ParameterGroup("Tube").add(
            tube_size_predicate,
        ),
        DrivenParameterGroup(
            "hasHoles",
            display_name="Holes",
            default=True,
            test=~has_predrilled_holes,
        ).add(tube_face_predicate, hole_predicate),
    ),
    UiPredicate("robotFrame").add(
        tube_predicate, Call("frameSelectionPredicate", "definition")
    ),
)


# # lookup functions
get_hole_diameter = Function(
    "getHoleDiameter",
    parent=studio,
    parameters=definition_param,
    return_type=Type.VALUE,
    export=False,
).add(
    IfBlock(has_predrilled_holes).add(
        IfBlock(Parens(hole_size["NO_8"] | hole_size["NO_10"]))
        .add(
            Return(
                MapAccess("HOLE_SIZES", definition("holeSize"), definition("holeFit"))
            )
        )
        .else_if(~definition("overrideHoleDiameter"))
        .add(IfBlock(is_max_tube).add(Return(millimeter(5))))
    ),
    Return(definition("holeDiameter")),
)

Const(
    "HOLE_SIZES",
    enum_map(
        hole_size,
        enum_map(fit, inch(0.1695), inch(0.177)),
        enum_map(fit, inch(0.196), inch(0.201)),
    ),
    parent=studio,
)

# get_min_hole_diameter = Function(
#     "getMinHoleDiameter",
#     parent=studio,
#     parameters=definition_param,
#     return_type=Type.VALUE,
#     export=False,
#     # Although its technically 5mm, we fudge to allow close fit holes
# ).add(Return(inch(0.196)))

get_one_inch_face_hole_count = lookup_enum_function(
    "getOneInchFaceHoleCount",
    one_inch_face,
    parent=studio,
    return_type=Type.NUMBER,
)

get_first_face_width = Function(
    "getFirstFaceWidth",
    parent=studio,
    parameters=definition_param,
    return_type=Type.VALUE,
    export=False,
).add(
    IfBlock(tube_size["TWO_BY_ONE"])
    .add(Return(inch(2)))
    .else_if(tube_size["ONE_BY_ONE"])
    .add(Return(inch(1))),
    Return(definition("firstFaceWidth")),
)

get_two_inch_face_hole_count = lookup_enum_function(
    "getTwoInchFaceHoleCount",
    two_inch_face,
    parent=studio,
    return_type=Type.NUMBER,
)

get_second_face_width = Function(
    "getSecondFaceWidth",
    parent=studio,
    parameters=definition_param,
    return_type=Type.MAP,
    export=False,
).add(
    IfBlock(tube_size["TWO_BY_ONE"] | tube_size["ONE_BY_ONE"]).add(Return(inch(1))),
    Return(definition("secondFaceWidth")),
)

one_inch_pattern = Const(
    "ONE_INCH_PATTERN",
    Map(
        {
            "distance": inch(0.5),
            "startOffset": inch(0.5),
            "endOffset": inch(0.5),
            "patternCount": 1,
            # "isPredrilled": True,
        },
        inline=False,
    ),
    parent=studio,
)

two_inch_max_grid_pattern = Const(
    "TWO_INCH_MAX_GRID_PATTERN",
    Map(
        {
            "distance": inch(0.5),
            "startOffset": inch(0.5),
            "endOffset": inch(0.5),
            "patternCount": 3,
            "patternSpacing": inch(0.5),
            # "isPredrilled": True,
        },
        inline=False,
    ),
    parent=studio,
)

two_inch_max_pattern = Const(
    "TWO_INCH_MAX_PATTERN",
    Map(
        {
            "distance": inch(2),
            "startOffset": inch(0.5),
            "endOffset": inch(0.5),
            "patternCount": 3,
            "patternSpacing": inch(0.5),
            # "isPredrilled": True,
        },
        inline=False,
    ),
    parent=studio,
)

Const(
    "MAX_PATTERN",
    Map(
        {
            "distance": inch(2),
            "startOffset": inch(1.5),
            "endOffset": inch(1),
            "patternCount": 1,
            "patternSpacing": meter(0),
            "holeDepth": inch(1),
            "width": inch(2)
            # "isPredrilled": True,
        },
        inline=False,
    ),
    parent=studio,
    export=True,
)

tube_face_def = "tubeFaceDefinition"
get_first_face_pattern_definition = Function(
    "getFirstFacePatternDefinition",
    parent=studio,
    parameters=definition_param,
    return_type=Type.MAP,
    export=False,
).add(
    Var(tube_face_def, "{}"),
    IfBlock(can_have_two_inch_face)
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "patternCount": get_two_inch_face_hole_count,
                    "patternSpacing": definition("twoInchFaceSpacing"),
                }
            ),
        )
    )
    .else_if(can_have_one_inch_face)
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "patternCount": get_one_inch_face_hole_count,
                    "patternSpacing": definition("oneInchFaceSpacing"),
                }
            ),
        )
    )
    .else_if(tube_size["CUSTOM"])
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "patternCount": definition("firstFaceCount"),
                    "patternSpacing": definition("firstFaceSpacing"),
                }
            ),
        ),
    )
    # max tube, not none (as that's caught by canHaveTwoInchFace)
    .else_if(is_max_tube).add(
        IfBlock(tube_size["ONE_BY_ONE"])
        .add(Assign(tube_face_def, one_inch_pattern))
        # two by one
        .else_if(max_pattern_type["GRID"])
        .add(Assign(tube_face_def, two_inch_max_grid_pattern))
        .else_if(max_pattern_type["MAX"])
        .add(Assign(tube_face_def, two_inch_max_pattern)),
    ),
    Return(
        merge_maps(
            Map(
                {
                    "width": get_first_face_width,
                    "holeDepth": get_second_face_width,
                    "holeDiameter": get_hole_diameter,
                    "distance": definition("distance"),
                    "startOffset": Ternary(
                        definition("customStartOffset"),
                        definition("startOffset"),
                        inch(0.5),
                    ),
                    "endOffset": Ternary(
                        definition("customEndOffset"),
                        definition("endOffset"),
                        inch(0.5),
                    ),
                    "patternSpacing": inch(0),
                    # "isPredrilled": False,
                },
                inline=False,
            ),
            tube_face_def,
        )
    ),
)

get_second_face_pattern_definition = Function(
    "getSecondFacePatternDefinition",
    parent=studio,
    parameters=definition_param,
    return_type=Type.MAP,
    export=False,
).add(
    Var(tube_face_def, "{}"),
    IfBlock(can_have_one_inch_face)
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "patternCount": get_one_inch_face_hole_count,
                    "patternSpacing": definition("oneInchFaceSpacing"),
                }
            ),
        )
    )
    .else_if(tube_size["CUSTOM"])
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "patternCount": definition("secondFaceCount"),
                    "patternSpacing": definition("secondFaceSpacing"),
                }
            ),
        )
    )
    .else_if(is_max_tube)
    .add(Assign(tube_face_def, one_inch_pattern)),
    Return(
        merge_maps(
            Map(
                {
                    "width": get_second_face_width,
                    "holeDepth": get_first_face_width,
                    "holeDiameter": get_hole_diameter,
                    "patternSpacing": inch(0),
                    "distance": definition("distance"),
                    "startOffset": Ternary(
                        definition("customStartOffset"),
                        definition("startOffset"),
                        inch(0.5),
                    ),
                    "endOffset": Ternary(
                        definition("customEndOffset"),
                        definition("endOffset"),
                        inch(0.5),
                    ),
                    # "isPredrilled": False,
                },
                inline=False,
            ),
            tube_face_def,
        )
    ),
)

get_wall_thickness = lookup_enum_function(
    "getWallThickness",
    wall_thickness,
    parent=studio,
    return_type=Type.VALUE,
)

get_max_tube_profile_type = Function(
    "getMaxTubeProfileType",
    parent=studio,
    parameters=definition_param,
    return_type="MaxTubeProfileType",
    export=False,
).add(
    IfBlock(tube_size["TWO_BY_ONE"]).add(
        IfBlock(can_be_light & definition("isLight")).add(
            Return("MaxTubeProfileType.TWO_BY_ONE_LIGHT"),
        ),
        Return("MaxTubeProfileType.TWO_BY_ONE"),
    ),
    Return("MaxTubeProfileType.ONE_BY_ONE"),
)

get_max_tube_definition = Function(
    "getMaxTubeDefinition",
    parent=studio,
    parameters=definition_param,
    return_type=Type.MAP,
    export=False,
).add(
    Return(
        Map(
            {
                "maxTubePatternType": definition("maxTubePatternType"),
                "hasScribeLines": definition("hasScribeLines"),
                "maxTubeProfileType": get_max_tube_profile_type,
            },
            inline=False,
        )
    )
)

tube_def = "tubeDefinition"
Function(
    "getTubeDefinition",
    parent=studio,
    parameters=definition_param,
    return_type=Type.MAP,
    export=True,
).add(
    Var(
        tube_def,
        Map(
            {
                "wallThickness": get_wall_thickness,
                "hasHoles": has_holes,
                "isMaxTube": is_max_tube,
            },
            inline=False,
        ),
    ),
    IfBlock(definition("hasHoles", tube_def)).add(
        Assign(
            tube_def,
            merge_maps(
                tube_def,
                Map(
                    {
                        "flipStart": definition("flipStart"),
                        "(TubeFace.FIRST)": get_first_face_pattern_definition,
                        "(TubeFace.SECOND)": get_second_face_pattern_definition,
                    },
                    quote_keys=False,
                    excluded_keys=["(TubeFace.FIRST)", "(TubeFace.SECOND)"],
                    inline=False,
                ),
            ),
        )
    ),
    IfBlock(definition("isMaxTube", tube_def)).add(
        Assign(tube_def, merge_maps(tube_def, get_max_tube_definition))
    ),
    Return(tube_def),
)
