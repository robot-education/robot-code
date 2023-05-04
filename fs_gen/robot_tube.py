from library import *


studio = Studio("robotTubeUi.gen.fs", "backend")

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
    .add_value("MAX", user_name="MAX")
    .make()
)

max_tube_profile = (
    enum_factory.add_enum("MaxTubeProfileType", parent=studio, annotate=False)
    .add_value("ONE_BY_ONE")
    .add_value("TWO_BY_ONE")
    .add_value("TWO_BY_ONE_LIGHT")
    .make()
)

# predicates
custom_wall_thickness = custom_enum_predicate(wall_thickness, parent=studio)


type_predicates = enum_predicates(tube_type, parent=studio)
size_predicates = enum_predicates(tube_size, parent=studio)


is_max_tube = TestPredicate(
    "isMaxTube",
    ~tube_size["CUSTOM"] & tube_type["MAX_TUBE"],
    parent=studio,
)

can_be_light = TestPredicate(
    "canBeLight",
    max_pattern_type["NONE"] | max_pattern_type["GRID"],
    parent=studio,
)

# True for any tube without preset holes.
has_predrilled_holes = TestPredicate(
    "hasPredrilledHoles",
    ~Parens(is_max_tube & Parens(~max_pattern_type["NONE"] | tube_size["ONE_BY_ONE"])),
    parent=studio,
)

is_hole_size_set = TestPredicate(
    "isHoleSizeSet", hole_size["NO_8"] | hole_size["NO_10"], parent=studio
)


# ui predicates
hole_predicate = UiPredicate("tubeHole", parent=studio).add(
    DrivenGroupAnnotation(
        parameter_name="hasHoles",
        user_name="Holes",
        default=True,
        drive_group_test=has_predrilled_holes,
    ).add(
        IfBlock(has_predrilled_holes)
        .add(
            EnumAnnotation(
                hole_size,
                user_name="Size",
                default="NO_10",
                ui_hints=show_label_hint,
            ),
            IfBlock(is_hole_size_set).add(
                EnumAnnotation(
                    fit,
                    user_name="Fit",
                    ui_hints=show_label_hint,
                )
            ),
        )
        .or_else()
        .add(BooleanAnnotation("overrideHoleDiameter")),
        IfBlock(
            Parens(has_predrilled_holes & ~is_hole_size_set)
            | Parens(~has_predrilled_holes & definition("overrideHoleDiameter"))
        ).add(LengthAnnotation("holeDiameter", LengthBound.BLEND_BOUNDS)),
    )
)

wall_predicate = UiPredicate("wallThickness", parent=studio).add(
    EnumAnnotation(
        wall_thickness,
        ui_hints=show_label_hint,
    ),
    IfBlock(custom_wall_thickness).add(
        LengthAnnotation(
            "customWallThickness",
            LengthBound.SHELL_OFFSET_BOUNDS,
            user_name="Wall thickness",
        )
    ),
)


tube_size_predicate = UiPredicate("tubeSize", parent=studio).add(
    EnumAnnotation(
        tube_size,
        user_name="Size",
        default="TWO_BY_ONE",
        ui_hints=show_label_hint,
    ),
    IfBlock(size_predicates["CUSTOM"])
    .add(
        LengthAnnotation("length", LengthBound.LENGTH_BOUNDS),
        LengthAnnotation("width", LengthBound.LENGTH_BOUNDS),
    )
    .or_else()
    .add(
        EnumAnnotation(
            tube_type,
            default="CUSTOM",
            user_name="Type",
            ui_hints=show_label_hint,
        ),
        IfBlock(is_max_tube & size_predicates["TWO_BY_ONE"]).add(
            EnumAnnotation(
                max_pattern_type,
                user_name="Pattern type",
                default="GRID",
                ui_hints=show_label_hint,
            ),
            IfBlock(can_be_light).add(
                BooleanAnnotation("isLight", user_name="Light"),
            ),
        ),
    ),
    IfBlock(size_predicates["CUSTOM"] | type_predicates["CUSTOM"]).add(wall_predicate),
)

tube_face_predicate = UiPredicate("tubeFace", parent=studio).add(
    GroupAnnotation("First face").add(), GroupAnnotation("Second face").add()
)

tube_predicate = UiPredicate("tube", parent=studio).add(
    GroupAnnotation("Tube").add(
        tube_size_predicate,
    ),
    hole_predicate,
    tube_face_predicate,
)


# # lookup functions
get_hole_diameter = Function(
    "getHoleDiameter",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
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

Function(
    "getMinHoleDiameter",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.VALUE,
).add(Return(millimeter(5)))

get_tube_size = Function(
    "getTubeSize", parent=studio, arguments=definition_arg, return_type=Type.MAP
).add(
    IfBlock(size_predicates["TWO_BY_ONE"])
    .add(Return(Map({"length": inch(2), "width": inch(1)})))
    .else_if(size_predicates["ONE_BY_ONE"])
    .add(Return(Map({"length": inch(1), "width": inch(1)}))),
    Return(definition_map("length", "width")),
)

get_wall_thickness = enum_lookup_function(
    "getWallThickness",
    wall_thickness,
    parent=studio,
    predicate_dict={"CUSTOM": custom_wall_thickness},
    return_type=Type.VALUE,
)

get_max_tube_profile_type = Function(
    "getMaxTubeProfileType",
    parent=studio,
    arguments=definition_arg,
    return_type="MaxTubeProfileType",
).add(
    IfBlock(size_predicates["TWO_BY_ONE"]).add(
        IfBlock(can_be_light & definition("is_light")).add(
            Return("MaxTubeProfileType.TWO_BY_ONE")
        ),
        Return("MaxTubeProfileType.TWO_BY_ONE_LIGHT"),
    ),
    Return("MaxTubeProfileType.ONE_BY_ONE"),
)

get_max_tube_definition = Function(
    "getMaxTubeDefinition",
    parent=studio,
    arguments=definition_arg,
    return_type=Type.MAP,
).add(
    Return(
        Map(
            {
                "patternType": definition("maxTubePatternType"),
                "light": can_be_light & definition("isLight"),
                "maxTubeProfileType": get_max_tube_profile_type,
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
        merge_maps(
            get_tube_size,
            Map(
                {
                    "wallThickness": get_wall_thickness,
                    "hole_diameter": get_hole_diameter,
                },
                inline=False,
            ),
        ),
    ),
    # IfBlock(is_max_tube).add(
    #     Assign(tube_def, merge_maps(tube_def, get_max_tube_definition))
    # ),
    # Return(tube_def),
)
