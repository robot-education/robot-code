from library import *
from robot_code.robot_studio import RobotFeature

robot_bearing = RobotFeature("bearing")
studio = robot_bearing.ui_studio
# feature_studio = robot_bearing.feature_studio

studio.add_import("stdHoleCommon.fs", export=True)

bearing_type = (
    EnumFactory("BearingType", studio).add_value("FLANGED").add_value("RADIAL").make()
)
flanged = bearing_type["FLANGED"]
radial = bearing_type["RADIAL"]

bore_type = EnumFactory("BoreType", studio).add_value("HEX").add_value("ROUND").make()
hex = bore_type["HEX"]
round = bore_type["ROUND"]

# hex_bearing_type = (
#     EnumFactory("HexBearingType", studio)
#     .add_value("0.5 in.")
#     .add_value("0.375 in.")
#     .make()
# )

# radial_round_bearing_type = (
#     EnumFactory("RadialRoundBearingType", studio)
#     .add_value("0.5 in.")
#     .add_value("0.375 in.")
#     .add_value("0.25 in.")
#     .add_value("0.1875 in.")
#     .make()
# )

# flanged_round_bearing_type = (
#     EnumFactory("FlangedRoundBearingType", studio)
#     .add_value("0.75 in.")
#     .add_value("0.5 in.")
#     .add_value("0.1875 in.")
#     .make()
# )

studio.add(
    hole_style := EnumFactory("BearingHoleStyle")
    .add_value("SIMPLE")
    .add_value("COUNTERBORE")
    .make(),
    UiPredicate("robotBearing").add(
        horizontal_enum_parameter(bore_type),
        labeled_enum_parameter(bearing_type),
        IfBlock(flanged).add(),
        ui_predicate_call("holeLocation"),
    ),
)
