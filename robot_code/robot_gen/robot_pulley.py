from library import *

studio = Studio("robotPulleyUi.gen.fs", "backend")

pulley_fit = (
    custom_enum_factory.add_enum("Fit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .make()
)

flange_type = (
    custom_enum_factory.add_enum("FlangeType", parent=studio)
    .add_value("STANDARD")
    .add_value("LARGE")
    .make()
)

flange_width_control = (
    enum_factory.add_enum("widthControl", parent=studio)
    .add_value("FLANGE_WIDTH")
    .add_value("PULLEY_WIDTH")
    .make()
)

flange_diameter_control = (
    enum_factory.add_enum("FlangeDiameterControl", parent=studio)
    .add_value("OFFSET")
    .add_value("OUTER_DIAMETER")
    .make()
)


studio.add(
    selections := UiPredicate("pulleySelections").add(
        DrivenParameterGroup("hasSelections", user_name="Selections").add(
            boolean_parameter("ahh")
        )
    ),
    general := UiPredicate("pulleyGeneral").add(
        ParameterGroup("General").add(
            boolean_parameter("customWidth"),
            IfBlock(definition("customWidth")).add(
                length_parameter(
                    "width",
                    user_name="Pulley width",
                    bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                )
            ),
            boolean_parameter("offsetProfile"),
            IfBlock(definition("offsetProfile")).add(
                length_parameter(
                    "offsetDistance",
                    bound_spec=LengthBound.ZERO_INCLUSIVE_OFFSET_BOUNDS,
                ),
                boolean_flip_parameter("oppositeDirection"),
            ),
        ),
    ),
    custom_flange := UiPredicate("pulleyCustomFlange").add(
        labeled_enum_parameter(flange_width_control),
        IfBlock(flange_width_control["FLANGE_WIDTH"])
        .add(
            length_parameter(
                "flangeWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
            )
        )
        .or_else()
        .add(
            length_parameter(
                "pulleyAndFlangeWidth",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                user_name="Total pulley width",
            )
        ),
        labeled_enum_parameter(flange_diameter_control),
        IfBlock(flange_diameter_control["OFFSET"])
        .add(length_parameter("flangeOffset", bound_spec=LengthBound.BLEND_BOUNDS))
        .or_else()
        .add(
            length_parameter(
                "flangeDiameter",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
            )
        ),
    ),
    flange := UiPredicate("pulleyFlange").add(
        DrivenParameterGroup("hasFlanges", user_name="Flanges").add(
            labeled_enum_parameter(flange_type),
            IfBlock(flange_type["CUSTOM"]).add(custom_flange),
        )
    ),
    engrave_tooth_count := UiPredicate("pulleyEngraveToothCount").add(
        DrivenParameterGroup("engraveToothCount", user_name="Engrave tooth count").add(
            length_parameter("engravingDepth", bound_spec=LengthBound.BLEND_BOUNDS)
        )
    ),
    UiPredicate("pulley").add(selections, general, flange, engrave_tooth_count),
)
