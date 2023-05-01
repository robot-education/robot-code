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

max_tube_type = (
    enum_factory.add_enum("MaxTubeType", parent=studio)
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX", user_name="MAX")
    .make()
)


# predicates
custom_wall_thickness = custom_enum_predicate(wall_thickness, parent=studio)


type_predicates = enum_predicates(tube_type, parent=studio)
size_predicates = enum_predicates(tube_size, parent=studio)


is_max_tube = ui_test_predicate(
    "isMaxTube",
    ~tube_size["CUSTOM"]() & tube_type["MAX_TUBE"](),
    parent=studio,
)

can_be_light = ui_test_predicate(
    "canBeLight",
    max_tube_type["NONE"] | max_tube_type["GRID"],
    parent=studio,
)

has_min_hole_diameter = ui_test_predicate(
    "hasMinHoleDiameter",
    tube_type["MAX_TUBE"]
    & Parens(
        tube_size["ONE_BY_ONE"]
        | Parens(
            tube_size["TWO_BY_ONE"]
            & Parens(max_tube_type["GRID"] | max_tube_type["MAX"])
        )
    ),
    parent=studio,
)

can_be_preset_diameter = ui_test_predicate(
    "canBePresetDiameter",
    ~Parens(
        Parens(tube_size["ONE_BY_ONE"] | tube_size["TWO_BY_ONE"])
        & tube_type["MAX_TUBE"]
        & Parens(~max_tube_type["NONE"] | tube_size["ONE_BY_ONE"])
    ),
    parent=studio,
)

# ui predicates
hole_predicate = UiPredicate("tubeHole", parent=studio).add(
    DrivenGroupAnnotation(
        parameter_name="hasHoles", user_name="Holes", default=True
    ).add(
        IfBlock(can_be_preset_diameter).add(
            EnumAnnotation(
                hole_size,
                user_name="Size",
                default="NO_10",
                ui_hints=show_label_hint,
            )
        ),
        IfBlock(
            can_be_preset_diameter & Parens(hole_size["NO_8"] | hole_size["NO_10"])
        ).add(
            EnumAnnotation(
                fit,
                user_name="Fit",
                ui_hints=show_label_hint,
            )
        ),
        IfBlock(~can_be_preset_diameter).add(BooleanAnnotation("overrideHoleDiameter")),
        IfBlock(
            Parens(
                can_be_preset_diameter & Parens(hole_size["NO_8"] | hole_size["NO_10"])
            )
            | Parens(~can_be_preset_diameter & definition("overrideHoleDiameter"))
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
                max_tube_type,
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

tube_predicate = UiPredicate("tube", parent=studio).add(
    GroupAnnotation("Tube").add(
        tube_size_predicate,
    ),
    hole_predicate(),
)

# # lookup functions
# get_hole_diameter = Function(
#     "getHoleDiameter",
#     parent=studio,
#     arguments=definition_arg,
#     return_type=Type.VALUE,
# ).add(
#     IfBlock(
#         can_be_preset_diameter & Parens(hole_size["NO_8"]() | hole_size["NO_10"]())
#     ).add(
#         Return(map_access("HOLE_SIZES", definition("holeSize"), definition("holeFit")))
#     ),
#     Return(definition("holeDiameter")),
# )


# Const(
#     "HOLE_SIZES",
#     enum_map(
#         hole_size,
#         enum_map(fit, inch(0.1695), inch(0.177)),
#         enum_map(fit, inch(0.196), inch(0.201)),
#     ),
#     parent=studio,
# )

# Function(
#     "getMinHoleDiameter",
#     parent=studio,
#     arguments=definition_arg,
#     return_type=Type.VALUE,
# ).add(Return(millimeter(5)))

# get_tube_size = Function(
#     "getTubeSize", parent=studio, arguments=definition_arg, return_type=Type.MAP
# ).add(
#     IfBlock(size_predicates["TWO_BY_ONE"])
#     .add(Return(Map({"length": inch(2), "width": inch(1)})))
#     .else_if(size_predicates["ONE_BY_ONE"])
#     .add(Return(Map({"length": inch(1), "width": inch(1)}))),
#     Return(definition_map("length", "width")),
# )

# get_wall_thickness = enum_lookup_function(
#     "getWallThickness",
#     wall_thickness,
#     parent=studio,
#     predicate_dict={"CUSTOM": custom_wall_thickness},
#     return_type=Type.VALUE,
# )

# get_max_tube_definition = Function(
#     "getMaxTubeDefinition",
#     parent=studio,
#     arguments=definition_arg,
#     return_type=Type.MAP,
# ).add(
#     Return(
#         Map(
#             {
#                 "patternType": definition("maxTubePatternType"),
#                 "light": can_be_light & definition("isLight"),
#             },
#             inline=False,
#         )
#     )
# )

# Function(
#     "getTubeDefinition", parent=studio, arguments=definition_arg, return_type=Type.MAP
# ).add(
#     Return(
#         merge_maps(
#             get_tube_size(),
#             Map(
#                 {
#                     "wallThickness": get_wall_thickness(),
#                     "hole_diameter": get_hole_diameter(),
#                 },
#                 inline=False,
#             ),
#         )
#     )
# )
