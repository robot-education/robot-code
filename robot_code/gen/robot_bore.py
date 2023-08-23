from library import *
from library.core.func import Return
from library.core.func import Return
from robot_code import robot_studio
from robot_code.utils import profile

DESCRIPTION = """Create a variety of bores suitable for use in robotics."""
robot_studio = robot_studio.RobotFeature("bore", description=DESCRIPTION)
studio = robot_studio.ui_studio

end_style = (
    EnumFactory("EndStyle", parent=studio)
    .add_value("BLIND")
    .add_value("UP_TO_VERTEX")
    .add_value("THROUGH_ALL")
    .make()
)

bore_type = (
    EnumFactory(
        "BoreType",
        parent=studio,
        generate_predicates=True,
    )
    .add_value("HEX")
    .add_value("ROUND")
    .add_value("MAX_SPLINE", "MAXSpline")
    .add_value("FALCON_SPLINE")
    .add_value("GEAR")
    .add_value("INSERT")
    .make()
)

bore_extend_type = (
    EnumFactory(
        "BoreExtendType",
        parent=studio,
    )
    .add_value("HEX")
    .add_value("ROUND")
    .add_value("MAX_SPLINE")
    .add_value("FALCON_SPLINE")
    .make()
)

gear_pitch_type = (
    EnumFactory("GearPitchType", parent=studio)
    .add_value("DIAMETRICAL_PITCH")
    .add_value("CIRCULAR_PITCH")
    .add_value("MODULE")
    .make()
)

insert_type = (
    EnumFactory("InsertType", parent=studio)
    .add_value("HEX", "1/2 in. hex")
    .add_value("FALCON_SPLINE")
    .make()
)

hex_size = profile.HexSizeFactory(studio)

fit = (
    EnumFactory("Fit", parent=studio)
    .add_value("NONE")
    .add_value("CLOSE")
    .add_value("FREE")
    .add_custom()
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
        enum_parameter(end_style, display_name="Termination", default="BLIND"),
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
                display_name="Up to vertex or mate connector",
                filter=VERTEX_FILTER,
            )
        ),
    ),
    bore_predicate := UiPredicate("bore").add_with_group(
        enum_parameter(bore_type),
        bore_end_predicate,
        IfBlock(bore_type["HEX"])
        .add(hex_size.predicate)
        .else_if(bore_type["ROUND"])
        .add(length_parameter("diameter"))
        .else_if(bore_type["GEAR"])
        .add(
            labeled_enum_parameter(gear_pitch_type, display_name="Pitch type"),
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
    ),
    extend_predicate := UiPredicate("extendBore").add(
        IfBlock(end_style["BLIND"] | end_style["UP_TO_VERTEX"]).add(
            DrivenParameterGroup("extendBore").add(
                IfBlock(bore_type["INSERT"]).add(
                    boolean_parameter("overrideBore"),
                ),
                IfBlock(definition("overrideBore") | ~bore_type["INSERT"]).add(
                    enum_parameter(bore_extend_type, display_name="Bore type"),
                    IfBlock(bore_extend_type["HEX"])
                    .add(
                        enum_parameter(hex_size.enum, "innerHexSize", "Hex size"),
                        IfBlock(
                            hex_size.enum["CUSTOM"](parameter_name="innerHexSize")
                        ).add(length_parameter("extendWidth", "Width")),
                    )
                    .else_if(bore_extend_type["ROUND"])
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
                        display_name="Up to vertex or mate connector",
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
    selections_predicate := UiPredicate("selections").add_with_group(
        # mimic hole feature
        query_parameter(
            "locations",
            display_name="Sketch points to place bores",
            filter=SKETCH_VERTEX_FILTER,
        ),
        query_parameter(
            "scope",
            display_name="Merge scope",
            filter=ModifiableEntityOnly.YES & EntityType.BODY & BodyType.SOLID,
        ),
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
        "getBoreDefinition", parameters=definition_param, return_type=Type.MAP
    ).add(
        IfBlock(bore_type["HEX"])
        .add(Return(Map({"width": hex_size.lookup_function})))
        .else_if(bore_type["ROUND"])
        .add(Return(definition_map("diameter"))),
    ),
)
