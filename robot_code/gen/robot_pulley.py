from featurescript import *

studio = Studio("robotPulleyUi.gen.fs", "backend")

pulley_fit = (
    EnumBuilder("Fit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .add_custom()
    .build()
)

flange_type = (
    EnumBuilder("FlangeType", parent=studio)
    .add_value("STANDARD")
    .add_value("LARGE")
    .add_custom()
    .build()
)

flange_width_control = (
    EnumBuilder("widthControl", parent=studio)
    .add_value("FLANGE_WIDTH")
    .add_value("PULLEY_WIDTH")
    .build()
)

flange_diameter_control = (
    EnumBuilder("FlangeDiameterControl", parent=studio)
    .add_value("OFFSET")
    .add_value("OUTER_DIAMETER")
    .build()
)

studio.add(
    selections := UiPredicate("pulleySelections").add(
        DrivenParameterGroup("hasSelections", display_name="Selections").add(
            boolean_parameter("ahh")
        )
    ),
    general := UiPredicate("pulleyGeneral").add(
        ParameterGroup("General").add(
            boolean_parameter("customWidth"),
            IfBlock(definition("customWidth")).add(
                length_parameter(
                    "width",
                    display_name="Pulley width",
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
                display_name="Total pulley width",
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
        DrivenParameterGroup("hasFlanges", display_name="Flanges").add(
            labeled_enum_parameter(flange_type),
            IfBlock(flange_type["CUSTOM"]).add(custom_flange),
        )
    ),
    engrave_tooth_count := UiPredicate("pulleyEngraveToothCount").add(
        DrivenParameterGroup(
            "engraveToothCount", display_name="Engrave tooth count"
        ).add(length_parameter("engravingDepth", bound_spec=LengthBound.BLEND_BOUNDS))
    ),
    UiPredicate("pulley").add(selections, general, flange, engrave_tooth_count),
)
