from library import *
from robot_code import robot_studio

DESCRIPTION = """Create a variety of bores suitable for use in robotics."""
robot_studio = robot_studio.RobotFeature("bore")
feature_studio = robot_studio.make_feature_studio(DESCRIPTION)

studio = robot_studio.make_ui_studio(feature_studio)

end_style = (
    ENUM_FACTORY.add_enum("EndStyle", parent=studio)
    .add_value("BLIND")
    .add_value("UP_TO_VERTEX")
    .add_value("THROUGH_ALL")
    .make()
)

bore_type = (
    ENUM_FACTORY.add_enum(
        "BoreType",
        parent=studio,
        generate_predicates=True,
    )
    .add_value("HEX")
    .add_value("CIRCLE")
    .add_value("MAX_SPLINE", "MAXSpline")
    .add_value("FALCON_SPLINE")
    .add_value("GEAR")
    .add_value("INSERT")
    .make()
)

bore_extend_type = (
    ENUM_FACTORY.add_enum(
        "BoreExtendType",
        parent=studio,
    )
    .add_value("HEX")
    .add_value("CIRCLE")
    .add_value("MAX_SPLINE")
    .add_value("FALCON_SPLINE")
    .make()
)

gear_pitch_type = (
    ENUM_FACTORY.add_enum("GearPitchType", parent=studio)
    .add_value("DIAMETRICAL_PITCH")
    .add_value("CIRCULAR_PITCH")
    .add_value("MODULE")
    .make()
)

insert_type = (
    ENUM_FACTORY.add_enum("InsertType", parent=studio)
    .add_value("HEX", "1/2 in. hex")
    .add_value("FALCON_SPLINE")
    .make()
)

hex_size = (
    CUSTOM_ENUM_FACTORY.add_enum("HexSize", parent=studio, value_type=LookupEnumValue)
    .add_value("_1_2_IN", "1/2 in.", lookup_value=inch(1 / 2))
    .add_value("_3_8_IN", "3/8 in.", lookup_value=inch(3 / 8))
    .add_custom(lookup_value=definition("width"))
    .make()
)

get_hex_size = enum_lookup_function(
    "getHexSize", hex_size, return_type=Type.VALUE, parent=studio
)

fit = (
    ENUM_FACTORY.add_enum("Fit", parent=studio)
    .add_value("NONE")
    .add_value("CLOSE")
    .add_value("FREE")
    .add_value("CUSTOM")
    .make()
)

studio.add(
    # has_preset_depth := UiTestPredicate(
    #     "hasPresetDepth",
    #     bore_type["INSERT"] & ~definition("customDepth"),
    # ),
    teeth_bounds := IntegerBoundSpec("TEETH_BOUNDS", min=1, default=18),
    dp_pitch_bounds := RealBoundSpec("DIAMETRICAL_PITCH_BOUNDS", default=20),
    cp_pitch_bounds := RealBoundSpec("CIRCULAR_PITCH_BOUNDS", default=18),
)

# predicates
studio.add(
    bore_end_predicate := UiPredicate("boreEnd").add(
        enum_parameter(end_style, user_name="Termination", default="BLIND"),
        boolean_flip_parameter(),
        IfBlock(end_style["BLIND"] & bore_type["INSERT"]).add(
            boolean_parameter("overrideDepth")
        ),
        IfBlock(
            end_style["BLIND"]
            & Parens(
                Parens(bore_type["INSERT"] & definition("overrideDepth"))
                | ~bore_type["INSERT"]
            )
        )
        .add(length_parameter("depth"))
        .else_if(end_style["UP_TO_VERTEX"])
        .add(
            query_parameter(
                "upToEntity",
                user_name="Up to vertex or mate connector",
                filter=VERTEX_FILTER,
            )
        ),
    ),
    bore_predicate := UiPredicate("bore").add(
        ParameterGroup("Bore").add(
            enum_parameter(bore_type),
            bore_end_predicate,
            IfBlock(bore_type["HEX"])
            .add(
                enum_parameter(hex_size),
                IfBlock(hex_size["CUSTOM"]).add(length_parameter("width")),
            )
            .else_if(bore_type["CIRCLE"])
            .add(length_parameter("diameter"))
            .else_if(bore_type["GEAR"])
            .add(
                labeled_enum_parameter(gear_pitch_type, user_name="Pitch type"),
                IfBlock(gear_pitch_type["DIAMETRICAL_PITCH"])
                .add(real_parameter("diametricalPitch", bound_spec=dp_pitch_bounds))
                .else_if(gear_pitch_type["CIRCULAR_PITCH"])
                .add(real_parameter("circularPitch", bound_spec=cp_pitch_bounds))
                .or_else()
                .add(real_parameter("module", bound_spec=dp_pitch_bounds)),
                integer_parameter("teeth", bound_spec=teeth_bounds),
            )
            .else_if(bore_type["INSERT"])
            .add(
                enum_parameter(insert_type),
            ),
            labeled_enum_parameter(fit),
        )
    ),
    extend_predicate := UiPredicate("extendBore").add(
        IfBlock(end_style["BLIND"] | end_style["UP_TO_VERTEX"]).add(
            DrivenParameterGroup("extendBore").add(
                IfBlock(bore_type["INSERT"]).add(
                    boolean_parameter("overrideBore"),
                ),
                IfBlock(definition("overrideBore") | ~bore_type["INSERT"]).add(
                    enum_parameter(bore_extend_type, user_name="Bore type"),
                    IfBlock(bore_extend_type["HEX"])
                    .add(
                        enum_parameter(hex_size, "innerHexSize", "Hex size"),
                        IfBlock(hex_size["CUSTOM"](parameter_name="innerHexSize")).add(
                            length_parameter("extendWidth", "Width")
                        ),
                    )
                    .else_if(bore_extend_type["CIRCLE"])
                    .add(length_parameter("extendDiameter", "Diameter")),
                ),
                boolean_parameter("overrideFit"),
                IfBlock(definition("overrideFit")).add(
                    labeled_enum_parameter(fit, "innerFit", "Fit"),
                ),
                enum_parameter(end_style, "innerEndStyle", default="THROUGH_ALL"),
                IfBlock(end_style["BLIND"](parameter_name="innerEndStyle"))
                .add(length_parameter("innerDepth", "Depth"))
                .else_if(end_style["UP_TO_VERTEX"])
                .add(
                    query_parameter(
                        "innerUpToEntity",
                        user_name="Up to vertex or mate connector",
                        filter=VERTEX_FILTER,
                    )
                ),
            )
        )
    ),
    chamfer_predicate := UiPredicate("chamfer").add(
        DrivenParameterGroup("chamfer").add(
            length_parameter("distance", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS)
        )
    ),
    selections_predicate := UiPredicate("selections").add(
        ParameterGroup("Selections").add(
            # mimic hole feature
            query_parameter(
                "locations",
                user_name="Sketch points to place bores",
                filter=SKETCH_VERTEX_FILTER,
            ),
            query_parameter(
                "scope",
                user_name="Merge scope",
                filter=ModifiableEntityOnly.YES & EntityType.BODY & BodyType.SOLID,
            ),
        )
    ),
    UiPredicate("robotBore").add(
        bore_predicate,
        extend_predicate,
        selections_predicate,
        boolean_parameter("finish", "Finish bore", default=True),
    ),
)

# functions
studio.add(
    get_bore_definition := Function(
        "getBoreDefinition", arguments=definition_arg, return_type=Type.MAP
    ).add(
        IfBlock(bore_type["HEX"])
        .add(Return(Map({"width": get_hex_size})))
        .else_if(bore_type["CIRCLE"])
        .add(Return(definition_map("diameter"))),
    ),
)
