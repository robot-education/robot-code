from library import *
from library.core.func import Call, Return, ui_predicate_call
from library.core.func import Return
from robot_code.robot_studio import RobotFeature
from robot_code.utils import profile

DESCRIPTION = r"Generate spacers."

robot_spacer = RobotFeature("spacer", description=DESCRIPTION)

# feature_studio = robot_spacer.feature_studio
studio = robot_spacer.ui_studio

studio.add_import("tool.fs")

studio.add_import("stdExtrudeUi.fs", "backend")
studio.add_import("wallType.fs", "backend")

spacer_type = (
    EnumFactory("SpacerType", parent=studio).add_value("HEX").add_value("ROUND").make()
)
hex_spacer = spacer_type["HEX"]
round_spacer = spacer_type["ROUND"]

hex_size = profile.HexSizeFactory(studio)
hole_size = profile.HoleSizeFactory(studio)
fit = profile.fit_enum(studio)

studio.add(
    can_have_default_wall := UiTestPredicate(
        "canHaveDefaultWall", hex_spacer & ~hex_size.enum["CUSTOM"]
    ),
    spacer := UiPredicate("spacer").add_with_group(
        labeled_enum_parameter(spacer_type),
        IfBlock(spacer_type["HEX"])
        .add(hex_size.predicate)
        .or_else(hole_size.predicate),
        IfBlock(
            Parens(hex_spacer & ~hex_size.enum["CUSTOM"])
            | Parens(round_spacer & ~hole_size.enum["CUSTOM"])
        ).add(labeled_enum_parameter(fit)),
        IfBlock(
            spacer_type["HEX"]
            & Parens(hex_size.enum["_1_2_IN"] | hex_size.enum["_3_8_IN"])
        ).add(
            boolean_parameter(
                "snapOn", description="Use a snap on design", default=False
            ),
            IfBlock(definition("snapOn")).add(
                boolean_parameter(
                    "customSnapOnGap",
                    ui_hints=UiHint.REMEMBER_EXPRESSION | UiHint.DISPLAY_SHORT,
                ),
                IfBlock(definition("customSnapOnGap")).add(
                    length_parameter(
                        "snapOnGap",
                        bound_spec=LengthBound.BLEND_BOUNDS,
                        ui_hints=UiHint.REMEMBER_EXPRESSION,
                    ),
                ),
            ),
        ),
        IfBlock(can_have_default_wall).add(
            boolean_parameter("customWall"),
        ),
        IfBlock(~can_have_default_wall | definition("customWall")).add(
            ui_predicate_call("wallType")
        ),
    ),
    UiPredicate("robotSpacer").add(
        ui_predicate_call("booleanStepType"),
        query_parameter(
            "locations",
            user_name="Sketch points to place spacers",
            filter=SKETCH_VERTEX_FILTER,
        ),
        spacer,
        ui_predicate_call("extrude"),
        ui_predicate_call("booleanStepScope"),
    ),
    get_inner_diameter := Function(
        "getInnerDiameter", parameters=definition_param, return_type=Type.VALUE
    ).add(
        IfBlock(spacer_type["HEX"]).add(Return(hex_size.lookup_function)),
    ),
    get_outer_diameter := Function(
        "getOuterDiameter",
        parameters=[definition_param, Parameter("innerDiameter", type=Type.VALUE)],
        return_type=Type.VALUE,
    ).add(
        IfBlock(~can_have_default_wall | definition("customWall")).add(
            Return(Call("getWallDiameter", definition(), "innerDiameter"))
        ),
        Return(Ternary(hex_size.enum["_1_2_IN"], inch(0.75), inch(0.625))),
    ),
    get_snap_on_gap := Function(
        "getSnapOnGap", parameters=definition_param, return_type=Type.VALUE
    ).add(
        IfBlock(definition("customSnapOnGap")).add(
            Return(Ternary(hex_size.enum["_1_2_IN"], inch(0.375), inch(0.25))),
        ),
        Return(definition("snapOnGap")),
    ),
    Function(
        "getSpacerDefinition",
        parameters=definition_param,
        return_type=Type.MAP,
    ).add(
        Const("innerDiameter", get_inner_diameter),
        Var(
            "spacerDefinition",
            Map(
                {
                    "spacerType": definition("spacerType"),
                    "innerDiameter": "innerDiameter",
                    "outerDiameter": get_outer_diameter,
                },
                inline=False,
            ),
        ),
        IfBlock(hex_spacer).add(
            Assign(
                "spacerDefinition",
                merge_maps(
                    Map(
                        {
                            "snapOn": definition("snapOn"),
                            "snapOnGap": Ternary(
                                definition("snapOn"), get_snap_on_gap, "undefined"
                            ),
                        },
                        inline=False,
                    ),
                    "spacerDefinition",
                ),
            ),
        ),
        Return("spacerDefinition"),
    ),
)
