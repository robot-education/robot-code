from library import *

studio = Studio("robotPulleyUi.gen.fs", "backend")

pulley_fit = (
    custom_enum_factory.add_enum("Fit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .make()
)

flange_size = (
    custom_enum_factory.add_enum("FlangeWidthType", parent=studio)
    .add_value("STANDARD")
    .add_value("LARGE")
    .add_value("CUSTOM")
    .make()
)

flange_width_type = (
    enum_factory.add_enum("FlangeWidthType", parent=studio)
    .add_value("FLANGE_WIDTH")
    .add_value("PULLEY_WIDTH")
    .make()
)

flange_diameter_type = (
    enum_factory.add_enum("FlangeDiameterType", parent=studio)
    .add_value("OFFSET")
    .add_value("OUTER_DIAMETER")
    .make()
)

bore_type = (
    enum_factory.add_enum("BoreType", parent=studio)
    .add_value("HEX")
    .add_value("CIRCLE")
    .add_value("FALCON_SPLINE")
    .add_value("INSERT")
    .make()
)

insert_type = (
    enum_factory.add_enum("InsertType", parent=studio)
    .add_value("HEX", user_name="1/2 in. hex")
    .add_value("SPLINE", user_name="Falcon spline")
    .make()
)

studio.add(
    general := UiPredicate("general").add(
        GroupParameter("general").add(
            BooleanParameter("customWidth"),
            IfBlock(definition("customWidth")).add(
                LengthParameter(
                    "width", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
                )
            ),
        ),
    ),
    bore := UiPredicate("bore").add(
        DrivenGroupParameter(parameter_name="bore", user_name="Bore").add(
            EnumParameter(bore_type),
            IfBlock(bore_type["INSERT"]).add(
                EnumParameter(insert_type, ui_hints=SHOW_LABEL_HINT),
                BooleanParameter("bothSides"),
                IfBlock(~definition("insertBothSides")).add(
                    BooleanFlipParameter("oppositeSide")
                ),
            ),
        )
    ),
)
#     custom_flange := UiPredicate("customFlange").add(
#         EnumParameter(flange_width_type),
#         IfBlock(flange_width_type["FLANGE_WIDTH"])
#         .add(
#             LengthParameter(
#                 "flangeWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
#             )
#         )
#         .or_else()
#         .add(
#             LengthParameter(
#                 "pulleyAndFlangeWidth",
#                 bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
#                 user_name="Total pulley width",
#             )
#         ),
#         EnumParameter(flange_diameter_type, ui_hints=SHOW_LABEL_HINT),
#         IfBlock(flange_diameter_type["OFFSET"])
#         .add(LengthParameter("flangeOffset", bound_spec=LengthBound.BLEND_BOUNDS))
#         .or_else()
#         .add(
#             LengthParameter(
#                 "flangeDiameter",
#                 bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
#             )
#         ),
#     ),
#     flange := UiPredicate("flange").add(
#         DrivenGroupParameter(parameter_name="flanges", user_name="Flanges").add(
#             EnumParameter(flange_size),
#             IfBlock(flange_size["CUSTOM"]).add(custom_flange),
#         )
#     ),
#     engrave_tooth_count := UiPredicate("engraveToothCount").add(
#         DrivenGroupParameter(
#             parameter_name="engraveToothCount", user_name="Engrave tooth count"
#         ).add(LengthParameter("engravingDepth", bound_spec=LengthBound.BLEND_BOUNDS))
#     ),
#     UiPredicate("pulley").add(general, flange, bore, engrave_tooth_count),
# )
