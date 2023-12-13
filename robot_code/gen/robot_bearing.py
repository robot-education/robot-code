from featurescript import *
from robot_code.robot_studio import RobotFeature

robot_bearing = RobotFeature("bearing")
studio = robot_bearing.ui_studio

studio.add_import("stdHoleCommon.fs", export=True)

bearing_type = (
    EnumBuilder("BearingType", studio).add_value("FLANGED").add_value("RADIAL").build()
)
flanged = bearing_type["FLANGED"]
radial = bearing_type["RADIAL"]

bore_type = EnumBuilder("BoreType", studio).add_value("HEX").add_value("ROUND").build()
hex = bore_type["HEX"]
round = bore_type["ROUND"]

bearing_hole_style = (
    EnumBuilder("BearingHoleStyle", studio, value_type=LookupEnumValue)
    .add_value("SIMPLE")
    .add_value("COUNTER_BORE")
    .build()
)

bearing_hole_end_style = (
    EnumBuilder("BearingHoleEndStyle", studio, value_type=LookupEnumValue)
    .add_value("BLIND")
    .add_value("UP_TO_NEXT")
    .add_value("UP_TO_ENTITY")
    .add_value("THROUGH")
    .build()
)

# hex_bearing_type = (
#     EnumBuilder("HexBearingType", studio)
#     .add_value("0.5 in.")
#     .add_value("0.375 in.")
#     .build()
# )

# radial_round_bearing_type = (
#     EnumBuilder("RadialRoundBearingType", studio)
#     .add_value("0.5 in.")
#     .add_value("0.375 in.")
#     .add_value("0.25 in.")
#     .add_value("0.1875 in.")
#     .build()
# )

# flanged_round_bearing_type = (
#     EnumBuilder("FlangedRoundBearingType", studio)
#     .add_value("0.75 in.")
#     .add_value("0.5 in.")
#     .add_value("0.1875 in.")
#     .build()
# )

studio.add(
    hole_style := EnumBuilder("BearingHoleStyle")
    .add_value("SIMPLE")
    .add_value("COUNTERBORE")
    .build(),
    UiPredicate("bearingHoleTop").add(
        ui_predicate_call("holePreselectionPredicate"),
        labeled_enum_parameter(bearing_hole_style, display_name="Style"),
        labeled_enum_parameter(bearing_hole_end_style, display_name="Termination"),
        boolean_flip_parameter(),
        ui_predicate_call("holeEndBound"),
    ),
    UiPredicate("robotBearing").add(
        horizontal_enum_parameter(bore_type),
        labeled_enum_parameter(bearing_type),
        IfBlock(flanged).add(),
        ui_predicate_call("holeLocation"),
        ui_predicate_call("holeBooleanScope"),
    ),
)
