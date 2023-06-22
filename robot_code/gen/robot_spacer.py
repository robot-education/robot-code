from library import *
from robot_code.robot_studio import RobotFeature
from robot_code.utils import profile

DESCRIPTION = r"Generate spacers."

robot_spacer = RobotFeature("spacer", description=DESCRIPTION)

feature_studio = robot_spacer.feature_studio
studio = robot_spacer.ui_studio

studio.add_import("tool.fs")

studio.add_import("stdExtrudeUi.fs", "backend")
studio.add_import("wallType.fs", "backend")

profile_type = (
    EnumFactory("ProfileType", parent=studio).add_value("HEX").add_value("ROUND").make()
)

hex_size = profile.HexSizeFactory(studio)
hole_size = profile.HoleSizeFactory(studio)

fit = profile.fit_enum(studio)

studio.add(
    selections := UiPredicate("general").add_with_group(
        IfBlock(profile_type["HEX"])
        .add(
            hex_size.predicate,
            IfBlock(hex_size.enum["_1_2_IN"] | hex_size.enum["_3_8_IN"]).add(
                boolean_parameter(
                    "snapOn", description="Use a snap on design", default=True
                ),
                IfBlock(definition("snapOn")).add(
                    boolean_parameter(
                        "useStandardGap",
                        ui_hints=UiHint.REMEMBER_EXPRESSION | UiHint.DISPLAY_SHORT,
                    ),
                    IfBlock(~definition("useStandardGap")).add(
                        length_parameter(
                            "gap",
                            bound_spec=LengthBound.BLEND_BOUNDS,
                            ui_hints=UiHint.REMEMBER_EXPRESSION,
                        ),
                    ),
                ),
            ),
        )
        .or_else(hole_size.predicate),
        IfBlock(
            Parens(profile_type["HEX"] & ~hex_size.enum["CUSTOM"])
            | Parens(profile_type["ROUND"] & ~hole_size.enum["CUSTOM"])
        ).add(labeled_enum_parameter(fit)),
        boolean_parameter("useStandardWall", default=True),
        IfBlock(~definition("useStandardWall")).add(ui_predicate_call("wallType")),
    ),
    UiPredicate("robotSpacer").add(
        horizontal_enum_parameter(profile_type),
        ui_predicate_call("booleanStepType"),
        query_parameter(
            "selections",
            user_name="Sketch points to place spacers",
            filter=SKETCH_VERTEX_FILTER,
        ),
        ui_predicate_call("extrude"),
        selections,
        ui_predicate_call("booleanStepScope"),
    ),
)
