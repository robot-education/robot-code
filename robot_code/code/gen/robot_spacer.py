from featurescript import *
from robot_code.code import profile
from robot_code.code.robot_feature import RobotFeature

DESCRIPTION = r"Generate spacers."

robot_spacer = RobotFeature("spacer", description=DESCRIPTION)

# feature_studio = robot_spacer.feature_studio
studio = robot_spacer.ui_studio

# studio.add_import("stdExtrudeUi.fs", export=True)
# studio.add_import("wallType.fs", export=True)

spacer_type = (
    EnumBuilder(
        "SpacerType",
        parent=studio,
        generate_predicates=True,
        predicate_name_template="{value}Spacer",
    )
    .add_value("HEX")
    .add_value("ROUND")
    .add_value("MAX_SPLINE", "MAXSpline")
    .build()
)
hex_spacer = spacer_type["HEX"]
round_spacer = spacer_type["ROUND"]
max_spacer = spacer_type["MAX_SPLINE"]

fit = profile.fit_enum(studio)
hex_size = profile.HexSizeBuilder(studio)
hole_size = profile.HoleSizeBuilder(studio)
get_hole_diameter = hole_size.make_lookup_function(fit)

studio.add(
    can_have_default_wall := UiTestPredicate(
        "canHaveDefaultWall",
        Parens(hex_spacer & ~hex_size.enum["CUSTOM"])
        | Parens(round_spacer & ~hole_size.enum["CUSTOM"])
        | max_spacer,
    ),
    can_have_default_snap_on_gap := UiTestPredicate(
        "canHaveDefaultSnapOnGap",
        hex_spacer & ~hex_size.enum["CUSTOM"],
    ),
    is_snap_on := UiTestPredicate(
        "isSnapOn",
        hex_spacer & definition("snapOn"),
    ),
    general := UiPredicate("general").add_with_group(
        labeled_enum_parameter(spacer_type),
        IfBlock(hex_spacer)
        .add(hex_size.predicate)
        .else_if(round_spacer)
        .add(hole_size.predicate)
        .else_if(max_spacer)
        .add(boolean_parameter("customOffset")),
        IfBlock(
            Parens(hex_spacer & ~hex_size.enum["CUSTOM"])
            | Parens(round_spacer & ~hole_size.enum["CUSTOM"])
            | Parens(max_spacer & ~definition("customOffset"))
        ).add(labeled_enum_parameter(fit)),
        IfBlock(max_spacer & definition("customOffset")).add(
            length_parameter(
                "offset", bound_spec=LengthBound.ZERO_DEFAULT_LENGTH_BOUNDS
            )
        ),
        IfBlock(can_have_default_wall).add(
            boolean_parameter("customWall"),
        ),
        IfBlock(~can_have_default_wall | definition("customWall")).add(
            ui_predicate_call("wallType")
        ),
    ),
    snap_on := UiPredicate("snapOn").add(
        IfBlock(hex_spacer).add(
            DrivenParameterGroup("snapOn", description="Use a snap on design").add(
                IfBlock(can_have_default_snap_on_gap).add(
                    boolean_parameter(
                        "customSnapOnGap",
                        ui_hints=UiHint.REMEMBER_EXPRESSION | UiHint.DISPLAY_SHORT,
                    ),
                ),
                IfBlock(
                    definition("customSnapOnGap") | ~can_have_default_snap_on_gap
                ).add(
                    length_parameter(
                        "snapOnGap",
                        bound_spec=LengthBound.BLEND_BOUNDS,
                        ui_hints=UiHint.REMEMBER_EXPRESSION,
                    ),
                ),
            )
        )
    ),
    UiPredicate("robotSpacer").add(
        query_parameter(
            "locations",
            display_name="Sketch points to place spacers",
            filter=SKETCH_VERTEX_FILTER,
        ),
        general,
        snap_on,
        ui_predicate_call("extrude"),
    ),
    get_custom_offset := Function(
        "getCustomOffset", parameters=definition_param, return_type=Type.VALUE
    ).add(
        IfBlock(definition("customOffset")).add(Return(definition("offset"))),
        Return(profile.fit_tolerance(fit)),
    ),
    get_inner_diameter := Function(
        "getInnerDiameter",
        parameters=definition_param,
        return_type=Type.VALUE,
        export=True,
    ).add(
        IfBlock(hex_spacer)
        .add(Return(hex_size.lookup_function + profile.fit_tolerance(fit)))
        .else_if(round_spacer)
        .add(Return(get_hole_diameter))
        .else_if(max_spacer)
        .add(Return(inch(1.375) + 2 * get_custom_offset))
    ),
    get_outer_diameter := Function(
        "getOuterDiameter",
        parameters=[definition_param, Parameter("innerDiameter", type=Type.VALUE)],
        return_type=Type.VALUE,
    ).add(
        IfBlock(~can_have_default_wall | definition("customWall")).add(
            Return(Call("getWallDiameter", definition(), "innerDiameter"))
        ),
        IfBlock(hex_spacer)
        .add(
            Return(Ternary(hex_size.enum["_1_2_IN"], inch(0.75), inch(0.625))),
        )
        .else_if(round_spacer)
        .add(enum_block(hole_size.enum, inch(0.3125), inch(0.375), inch(0.5)))
        .else_if(max_spacer)
        .add(Return(inch(1.625))),
    ),
    get_snap_on_gap := Function(
        "getSnapOnGap", parameters=definition_param, return_type=Type.VALUE, export=True
    ).add(
        IfBlock(~can_have_default_snap_on_gap | definition("customSnapOnGap")).add(
            Return(definition("snapOnGap")),
        ),
        Return(Ternary(hex_size.enum["_1_2_IN"], inch(0.375), inch(0.25))),
    ),
    Function(
        "getSpacerDefinition",
        parameters=definition_param,
        return_type=Type.MAP,
        export=True,
    ).add(
        # innerDiameter used in outerDiameter
        Const("innerDiameter", get_inner_diameter),
        snap_on := Const("snapOn", is_snap_on),
        custom_offset := Const("customOffset", max_spacer),
        Var(
            "spacerDefinition",
            Map(
                {
                    "spacerType": definition("spacerType"),
                    "innerDiameter": "innerDiameter",
                    "outerDiameter": get_outer_diameter,
                    "snapOn": snap_on,
                    "snapOnGap": Ternary(snap_on, get_snap_on_gap, "undefined"),
                    "customOffset": custom_offset,
                    "offset": Ternary(custom_offset, get_custom_offset, "undefined"),
                },
                inline=False,
            ),
        ),
        Return("spacerDefinition"),
    ),
)
