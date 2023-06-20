from library import *
from robot_code.robot_studio import RobotFeature
from robot_code.utils import profile

DESCRIPTION = r"Generate spacers."

robot_spacer = RobotFeature("spacer", description=DESCRIPTION)
studio = robot_spacer.ui_studio

studio.add_import("stdExtrudeUi.fs", "backend")
studio.add_import("tool.fs")


profile_type = (
    EnumFactory("ProfileType", parent=studio).add_value("HEX").add_value("ROUND").make()
)

hex_size = profile.HexSizeFactory(studio)
hole_size = profile.HoleSizeFactory(studio)

fit = profile.fit_enum(studio)

studio.add(
    selections := UiPredicate("selections").add(
        horizontal_enum_parameter(profile_type),
        IfBlock(profile_type["HEX"])
        .add(
            hex_size.predicate,
            IfBlock(hex_size.enum["_1_2_IN"]).add(
                boolean_parameter(
                    "snapOn", description="Use a snap-on design", default=True
                ),
                IfBlock(definition("snapOn")).add(
                    boolean_parameter("customGap"),
                    IfBlock(definition("customGap")).add(
                        length_parameter(
                            "gap",
                            LengthBound.BLEND_BOUNDS,
                            ui_hints=UiHint.REMEMBER_EXPRESSION | UiHint.DISPLAY_SHORT,
                        )
                    ),
                ),
            ),
        )
        .or_else(hole_size.predicate),
        IfBlock(
            Parens(profile_type["HEX"] & ~hex_size.enum["CUSTOM"])
            | Parens(profile_type["ROUND"] & ~hole_size.enum["CUSTOM"])
        ).add(labeled_enum_parameter(fit)),
        boolean_parameter("usePresetWall"),
        IfBlock(~definition("usePresetWall")).add(ui_predicate_call("outerDiameter")),
        query_parameter("selections", filter=SKETCH_VERTEX_FILTER),
    ),
    UiPredicate("robotSpacer").add(
        ui_predicate_call("booleanStepType"),
        selections,
        ui_predicate_call("extrude"),
        ui_predicate_call("booleanStepScope"),
    ),
)
