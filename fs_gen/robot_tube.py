from library import *
from library.ui.parameter import BooleanParameter, LengthParameter


studio = Studio("tubeUi.gen.fs", "backend")

# enums
wall_thickness = (
    custom_enum_factory.add_enum(
        "WallThickness", parent=studio, value_type=LookupEnumValue
    )
    .add_value("ONE_SIXTEENTH", user_name='1/16\\"', lookup_value=inch(0.0625))
    .add_value("ONE_EIGHTH", user_name='1/8\\"', lookup_value=inch(0.125))
    .add_custom(lookup_value=definition("customWallThickness"))
    .make()
)

fit = (
    enum_factory.add_enum("HoleFit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .make()
)

hole_size = (
    custom_enum_factory.add_enum("HoleSize", parent=studio)
    .add_value("NO_8", user_name="#8")
    .add_value("NO_10", user_name="#10")
    .make()
)

tube_size = (
    custom_enum_factory.add_enum("TubeSize", parent=studio)
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .make()
)

tube_type = (
    custom_enum_factory.add_enum("TubeType", parent=studio)
    .add_value("MAX_TUBE", user_name="MAXTube")
    .make()
)

max_pattern_type = (
    enum_factory.add_enum("MaxTubePatternType", parent=studio)
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX")
    .make()
)

max_tube_profile = (
    enum_factory.add_enum("MaxTubeProfileType", parent=studio, annotate=False)
    .add_value("ONE_BY_ONE")
    .add_value("TWO_BY_ONE")
    .add_value("TWO_BY_ONE_LIGHT")
    .make()
)

tube_face = (
    enum_factory.add_enum("TubeFace", parent=studio, annotate=False)
    .add_value("FIRST")
    .add_value("SECOND")
    .make()
)

two_inch_face = (
    enum_factory.add_enum(
        "TwoInchFaceHoleCount", parent=studio, value_type=LookupEnumValue
    )
    .add_value("THREE", user_name="Three holes", lookup_value="3")
    .add_value("TWO", user_name="Two holes", lookup_value="2")
    .add_value("ONE", user_name="One hole", lookup_value="1")
    .add_value("NONE", lookup_value="0")
    .make()
)

one_inch_face = (
    enum_factory.add_enum(
        "OneInchFaceHoleCount", parent=studio, value_type=LookupEnumValue
    )
    .add_value("TWO", user_name="Two holes", lookup_value="2")
    .add_value("ONE", user_name="One hole", lookup_value="1")
    .add_value("NONE", lookup_value="0")
    .make()
)


# predicates
custom_wall_thickness = custom_enum_predicate(wall_thickness, parent=studio)

type_predicates = enum_predicates(tube_type, parent=studio)
size_predicates = enum_predicates(tube_size, parent=studio)

studio.add(
    is_max_tube := UiTestPredicate(
        "isMaxTube",
        ~tube_size["CUSTOM"] & tube_type["MAX_TUBE"],
    ),
    can_be_light := UiTestPredicate(
        "canBeLight",
        max_pattern_type["NONE"] | max_pattern_type["GRID"],
    ),
    # True for any tube without preset holes.
    has_predrilled_holes := UiTestPredicate(
        "hasPredrilledHoles",
        is_max_tube,
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
        size_predicates["TWO_BY_ONE"]
        & Parens(
            type_predicates["CUSTOM"]
            | Parens(type_predicates["MAX_TUBE"] & max_pattern_type["NONE"])
        ),
    ),
    can_have_one_inch_face := UiTestPredicate(
        "canHaveOneInchFace",
        type_predicates["CUSTOM"]
        & Parens(size_predicates["ONE_BY_ONE"] | size_predicates["TWO_BY_ONE"]),
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
        EnumParameter(
            two_inch_face, user_name='2\\" face hole count', ui_hints=SHOW_LABEL_HINT
        ),
        IfBlock(two_inch_face["TWO"] | two_inch_face["THREE"]).add(
            LengthParameter(
                "twoInchFaceSpacing",
                bound_spec=two_inch_spacing_bounds,
                user_name='2\\" face spacing',
            )
        ),
    ),
    IfBlock(can_have_one_inch_face).add(
        EnumParameter(
            one_inch_face, user_name='1\\" face hole count', ui_hints=SHOW_LABEL_HINT
        ),
        IfBlock(one_inch_face["TWO"]).add(
            LengthParameter(
                "oneInchFaceSpacing",
                bound_spec=one_inch_spacing_bounds,
                user_name='1\\" face spacing',
            )
        ),
    ),
    IfBlock(size_predicates["CUSTOM"]).add(
        CountParameter("firstFaceCount", user_name="First face hole count"),
        LengthParameter(
            "firstFaceSpacing",
            bound_spec=spacing_bounds,
        ),
        CountParameter("secondFaceCount", user_name="Second face hole count"),
        LengthParameter(
            "secondFaceSpacing",
            bound_spec=spacing_bounds,
        ),
    ),
    IfBlock(~has_predrilled_holes).add(
        LengthParameter("distance", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS)
    ),
)

hole_predicate = UiPredicate("tubeHole", parent=studio).add(
    IfBlock(~has_predrilled_holes)
    .add(
        EnumParameter(
            hole_size,
            default="NO_10",
            ui_hints=SHOW_LABEL_HINT,
        ),
        IfBlock(is_hole_size_set).add(
            EnumParameter(
                fit,
                ui_hints=SHOW_LABEL_HINT,
            )
        ),
    )
    .or_else()
    .add(BooleanParameter("overrideHoleDiameter")),
    IfBlock(
        Parens(~has_predrilled_holes & ~is_hole_size_set)
        | Parens(has_predrilled_holes & definition("overrideHoleDiameter"))
    ).add(LengthParameter("holeDiameter", bound_spec=LengthBound.BLEND_BOUNDS)),
    BooleanParameter("delayInstantiation"),
)

wall_predicate = UiPredicate("wallThickness", parent=studio).add(
    EnumParameter(
        wall_thickness,
        ui_hints=SHOW_LABEL_HINT,
    ),
    IfBlock(custom_wall_thickness).add(
        LengthParameter(
            "customWallThickness",
            bound_spec=LengthBound.SHELL_OFFSET_BOUNDS,
            user_name="Wall thickness",
        )
    ),
)


tube_size_predicate = UiPredicate("tubeSize", parent=studio).add(
    EnumParameter(
        tube_size,
        user_name="Size",
        default="TWO_BY_ONE",
        ui_hints=SHOW_LABEL_HINT,
    ),
    IfBlock(size_predicates["CUSTOM"])
    .add(
        LengthParameter(
            "firstFaceWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
        ),
        LengthParameter(
            "secondFaceWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
        ),
    )
    .or_else()
    .add(
        EnumParameter(
            tube_type,
            default="CUSTOM",
            user_name="Type",
            ui_hints=SHOW_LABEL_HINT,
        ),
        IfBlock(is_max_tube).add(
            IfBlock(size_predicates["TWO_BY_ONE"]).add(
                EnumParameter(
                    max_pattern_type,
                    user_name="Pattern type",
                    default="GRID",
                    ui_hints=SHOW_LABEL_HINT,
                ),
                IfBlock(can_be_light).add(
                    BooleanParameter("isLight", user_name="Light"),
                ),
            ),
            BooleanParameter(
                "hasScribeLines", user_name="Draw scribe lines", default=True
            ),
        ),
    ),
    IfBlock(size_predicates["CUSTOM"] | type_predicates["CUSTOM"]).add(wall_predicate),
)


tube_predicate = UiPredicate("tube", parent=studio).add(
    GroupParameter("Tube").add(
        tube_size_predicate,
    ),
    DrivenGroupParameter(
        parameter_name="hasHoles",
        user_name="Holes",
        default=True,
        drive_group_test=~has_predrilled_holes,
    ).add(tube_face_predicate, hole_predicate),
)


# # lookup functions
get_hole_diameter = Function(
    "getHoleDiameter",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
    export=False,
).add(
    IfBlock(has_predrilled_holes & Parens(hole_size["NO_8"] | hole_size["NO_10"])).add(
        Return(MapAccess("HOLE_SIZES", definition("holeSize"), definition("holeFit")))
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

get_min_hole_diameter = Function(
    "getMinHoleDiameter",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
    export=False,
).add(Return(millimeter(5)))

get_one_inch_face_hole_count = enum_lookup_function(
    "getOneInchFaceHoleCount",
    one_inch_face,
    parent=studio,
    return_type=Type.NUMBER,
    export=False,
)

get_first_face_width = Function(
    "getFirstFaceWidth",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
    export=False,
).add(
    IfBlock(size_predicates["TWO_BY_ONE"])
    .add(Return(inch(2)))
    .else_if(size_predicates["ONE_BY_ONE"])
    .add(Return(inch(1))),
    Return(definition("firstFaceWidth")),
)

get_two_inch_face_hole_count = enum_lookup_function(
    "getTwoInchFaceHoleCount",
    two_inch_face,
    parent=studio,
    return_type=Type.NUMBER,
    export=False,
)

get_second_face_width = Function(
    "getSecondFaceWidth",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.MAP,
    export=False,
).add(
    IfBlock(size_predicates["TWO_BY_ONE"] | size_predicates["ONE_BY_ONE"]).add(
        Return(inch(1))
    ),
    Return(definition("secondFaceWidth")),
)

tube_face_def = "tubeFaceDefinition"
get_first_face_pattern_definition = Function(
    "getFirstFacePatternDefinition",
    parent=studio,
    arguments=definition_arg,
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
                    "count": get_two_inch_face_hole_count,
                    "spacing": definition("twoInchFaceSpacing"),
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
                    "count": get_one_inch_face_hole_count,
                    "spacing": definition("oneInchFaceSpacing"),
                }
            ),
        )
    )
    .else_if(size_predicates["CUSTOM"])
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "count": definition("firstFaceCount"),
                    "spacing": definition("firstFaceSpacing"),
                }
            ),
        )
    )
    .else_if(is_max_tube)
    .add(
        IfBlock(size_predicates["ONE_BY_ONE"])
        .add(
            Assign(
                tube_face_def,
                Map({"count": 1}),
            )
        )
        .else_if(max_pattern_type["GRID"] | max_pattern_type["MAX"])
        .add(
            Assign(
                tube_face_def,
                Map({"count": 3, "spacing": inch(0.5)}),
            )
        )
        .else_if(max_pattern_type["NONE"])
        # set hole count to 0
        .add(Assign(tube_face_def, Map({"count": 0})))
    ),
    # Return(tube_face_def)
    Return(merge_maps(tube_face_def, Map({"width": get_first_face_width}))),
)

get_second_face_pattern_definition = Function(
    "getSecondFacePatternDefinition",
    parent=studio,
    arguments=definition_arg,
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
                    "count": get_one_inch_face_hole_count,
                    "spacing": definition("oneInchFaceSpacing"),
                }
            ),
        )
    )
    .else_if(size_predicates["CUSTOM"])
    .add(
        Assign(
            tube_face_def,
            Map(
                {
                    "count": definition("secondFaceCount"),
                    "spacing": definition("secondFaceSpacing"),
                }
            ),
        )
    )
    .else_if(is_max_tube)
    .add(
        Assign(
            tube_face_def,
            Map({"count": 1}),
        )
    ),
    # Return(tube_face_def)
    Return(merge_maps(tube_face_def, Map({"width": get_second_face_width}))),
)

get_hole_distance = Function(
    "getHoleDistance",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
    export=False,
).add(
    IfBlock(has_predrilled_holes).add(Return(inch(0.5))), Return(definition("distance"))
)

get_wall_thickness = enum_lookup_function(
    "getWallThickness",
    wall_thickness,
    parent=studio,
    predicate_dict={"CUSTOM": custom_wall_thickness},
    return_type=Type.VALUE,
    export=False,
)

get_max_tube_profile_type = Function(
    "getMaxTubeProfileType",
    parent=studio,
    arguments=definition_arg,
    return_type="MaxTubeProfileType",
    export=False,
).add(
    IfBlock(size_predicates["TWO_BY_ONE"]).add(
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
    arguments=definition_arg,
    return_type=Type.MAP,
    export=False,
).add(
    Return(
        Map(
            {
                "maxTubePatternType": definition("maxTubePatternType"),
                "hasScribeLines": definition("hasScribeLines"),
                "maxTubeProfileType": get_max_tube_profile_type,
                "minHoleDiameter": get_min_hole_diameter,
            },
            inline=False,
        )
    )
)

tube_def = "tubeDefinition"
Function(
    "getTubeDefinition", parent=studio, arguments=definition_arg, return_type=Type.MAP
).add(
    Var(
        tube_def,
        Map(
            {
                "wallThickness": get_wall_thickness,
                "hasHoles": has_holes,
                "isMaxTube": is_max_tube,
                "firstFaceWidth": get_first_face_width,
                "secondFaceWidth": get_second_face_width,
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
                        '"holeDiameter"': get_hole_diameter,
                        '"distance"': get_hole_distance,
                        "(TubeFace.FIRST)": get_first_face_pattern_definition,
                        "(TubeFace.SECOND)": get_second_face_pattern_definition,
                    },
                    quote_keys=False,
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
