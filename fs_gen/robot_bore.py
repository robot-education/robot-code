from library import *

studio = Studio("robotBoreUi.gen.fs", "backend")

end_style = (
    enum_factory.add_enum("EndStyle", parent=studio)
    .add_value("THROUGH")
    .add_value("BLIND")
    .add_value("UP_TO_VERTEX")
    .make()
)

bore_creation_type = (
    enum_factory.add_enum("BoreCreationType", parent=studio)
    .add_value("OUTER_BORE")
    .add_value("INNER_BORE")
    .add_value("BOTH")
    .make()
)

studio.add(
    has_outer_bore := UiTestPredicate(
        "hasOuterBore", any(bore_creation_type, "OUTER_BORE", "BOTH")
    ),
    has_inner_bore := UiTestPredicate(
        "hasInnerBore", any(bore_creation_type, "INNER_BORE", "BOTH")
    ),
)

outer_bore_type = (
    enum_factory.add_enum(
        "OuterBoreType",
        parent=studio,
        generate_predicates=True,
        predicate_name_template="isOuterBore{value}",
    )
    .add_value("MAX_SPLINE", user_name="MAX spline")
    .add_value("GEAR")
    .add_value("INSERT")
    .make()
)

gear_pitch_type = (
    enum_factory.add_enum("GearPitchType", parent=studio)
    .add_value("DIAMETRICAL_PITCH")
    .add_value("CIRCULAR_PITCH")
    .add_value("MODULE")
    .make()
)

insert_type = (
    enum_factory.add_enum("InsertType", parent=studio)
    .add_value("HEX", user_name="1/2 in. hex")
    .add_value("FALCON_SPLINE", user_name="Falcon spline")
    .make()
)

studio.add(
    can_extend_insert := UiTestPredicate(
        "canExtendInsert", bore_creation_type["BOTH"] & outer_bore_type["INSERT"]
    )
)

inner_bore_type = (
    enum_factory.add_enum(
        "InnerBoreType",
        parent=studio,
        generate_predicates=True,
        predicate_name_template="isInnerBore{value}",
    )
    .add_value("HEX")
    .add_value("CIRCLE")
    .add_value("FALCON_SPLINE")
    .make()
)

hex_size = (
    custom_enum_factory.add_enum("HexSize", parent=studio, value_type=LookupEnumValue)
    .add_value("_1_2_IN", user_name="1/2 in.", lookup_value=inch(1 / 2))
    .add_value("_3_8_IN", user_name="3/8 in.", lookup_value=inch(3 / 8))
    .add_custom(lookup_value=definition("width"))
    .make()
)

fit = (
    enum_factory.add_enum("fit", parent=studio)
    .add_value("NOMINAL")
    .add_value("CLOSE")
    .add_value("FREE")
    .add_value("CUSTOM")
    .make()
)

studio.add(
    teeth_bounds := IntegerBoundSpec("TEETH_BOUNDS", min=1, default=18),
    dp_pitch_bounds := RealBoundSpec("DIAMETRICAL_PITCH_BOUNDS", default=20),
    cp_pitch_bounds := RealBoundSpec("CIRCULAR_PITCH_BOUNDS", default=18),
)

# predicates
studio.add(
    outer_bore := UiPredicate("outerBore").add(
        GroupParameter("Outer bore").add(
            EnumParameter(outer_bore_type),
            IfBlock(outer_bore_type["GEAR"])
            .add(
                LabeledEnumParameter(gear_pitch_type, user_name="Pitch type"),
                IfBlock(gear_pitch_type["DIAMETRICAL_PITCH"])
                .add(RealParameter("diametricalPitch", bound_spec=dp_pitch_bounds))
                .else_if(gear_pitch_type["CIRCULAR_PITCH"])
                .add(RealParameter("circularPitch", bound_spec=cp_pitch_bounds))
                .or_else()
                .add(),
                IntegerParameter("teeth", bound_spec=teeth_bounds),
            )
            .else_if(outer_bore_type["INSERT"])
            .add(
                LabeledEnumParameter(insert_type),
            ),
            IfBlock(any(outer_bore_type, "GEAR", "MAX_SPLINE")).add(
                BooleanParameter("throughAll"),
                LengthParameter(
                    "outerBoreDepth",
                    bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                    user_name="Depth",
                ),
            ),
            LabeledEnumParameter(fit, parameter_name="outerFit", user_name="Fit"),
        )
    ),
    inner_bore := UiPredicate("innerBore").add(
        GroupParameter("Inner bore").add(
            IfBlock(can_extend_insert).add(BooleanParameter("extendInsert")),
            IfBlock(
                Parens(can_extend_insert & ~definition("extendInsert"))
                | ~can_extend_insert
            ).add(
                EnumParameter(inner_bore_type),
                IfBlock(inner_bore_type["HEX"])
                .add(
                    LabeledEnumParameter(hex_size),
                    IfBlock(hex_size["CUSTOM"]).add(
                        LengthParameter(
                            "width", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
                        )
                    ),
                )
                .else_if(inner_bore_type["CIRCLE"])
                .add(
                    LengthParameter(
                        "diameter", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
                    )
                ),
            ),
            EnumParameter(fit, parameter_name="innerFit"),
        )
    ),
    selections := UiPredicate("selections").add(
        # mimic hole feature
        GroupParameter("Selections").add(
            EnumParameter(end_style, user_name="Termination")
        )
    ),
    UiPredicate("bore").add(
        HorizontalEnumParameter(bore_creation_type),
        IfBlock(has_outer_bore).add(outer_bore),
        IfBlock(has_inner_bore).add(inner_bore),
        selections,
    ),
)

# functions
studio.add(
    get_hex_size := EnumLookupFunction("getHexSize", hex_size, return_type=Type.VALUE),
    get_inner_bore_definition := Function(
        "getInnerBoreDefinition", arguments=definition_arg, return_type=Type.MAP
    ).add(
        IfBlock(inner_bore_type["HEX"])
        .add(Return(Map({"width": get_hex_size})))
        .else_if(inner_bore_type["CIRCLE"])
        .add(Return(definition_map("diameter"))),
        Function(
            "getBoreDefinition", arguments=definition_arg, return_type=Type.MAP
        ).add(),
    ),
)
