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
        DrivenGroupParameter("hasSelections", user_name="Selections").add(
            BooleanParameter("ahh")
        )
    ),
    general := UiPredicate("pulleyGeneral").add(
        GroupParameter("General").add(
            BooleanParameter("customWidth"),
            IfBlock(definition("customWidth")).add(
                LengthParameter(
                    "width",
                    user_name="Pulley width",
                    bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                )
            ),
            BooleanParameter("offsetProfile"),
            IfBlock(definition("offsetProfile")).add(
                LengthParameter(
                    "offsetDistance",
                    bound_spec=LengthBound.ZERO_INCLUSIVE_OFFSET_BOUNDS,
                ),
                BooleanFlipParameter("oppositeDirection"),
            ),
        ),
    ),
    custom_flange := UiPredicate("pulleyCustomFlange").add(
        LabeledEnumParameter(flange_width_control),
        IfBlock(flange_width_control["FLANGE_WIDTH"])
        .add(
            LengthParameter(
                "flangeWidth", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
            )
        )
        .or_else()
        .add(
            LengthParameter(
                "pulleyAndFlangeWidth",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                user_name="Total pulley width",
            )
        ),
        LabeledEnumParameter(flange_diameter_control),
        IfBlock(flange_diameter_control["OFFSET"])
        .add(LengthParameter("flangeOffset", bound_spec=LengthBound.BLEND_BOUNDS))
        .or_else()
        .add(
            LengthParameter(
                "flangeDiameter",
                bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
            )
        ),
    ),
    flange := UiPredicate("pulleyFlange").add(
        DrivenGroupParameter("hasFlanges", user_name="Flanges").add(
            LabeledEnumParameter(flange_type),
            IfBlock(flange_type["CUSTOM"]).add(custom_flange),
        )
    ),
    engrave_tooth_count := UiPredicate("pulleyEngraveToothCount").add(
        DrivenGroupParameter("engraveToothCount", user_name="Engrave tooth count").add(
            LengthParameter("engravingDepth", bound_spec=LengthBound.BLEND_BOUNDS)
        )
    ),
    UiPredicate("pulley").add(selections, general, flange, engrave_tooth_count),
)
