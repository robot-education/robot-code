from library import *
from library.enum import LookupEnumValue

studio = Studio("robotTubeUi.gen.fs")

wall_thickness = (
    custom_enum_factory.add_enum(
        "WallThickness", parent=studio, value_type=LookupEnumValue
    )
    .add_value("ONE_SIXTEENTH", user_name='1/16"', lookup_value="0.0625 * inch")
    .add_value("ONE_EIGHTH", user_name='1/8"', lookup_value="0.125 * inch")
    .add_custom(lookup_value=definition("customWallThickness"))
    .make()
)

custom_wall_thickness = custom_enum_predicate(wall_thickness, parent=studio)

wall_predicate = UiPredicate("wallThickness", parent=studio)
wall_predicate.add(
    EnumAnnotation(
        wall_thickness, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    ),
    IfBlock(custom_wall_thickness).add(
        LengthAnnotation(
            "customWallThickness", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS
        )
    ),
)

enum_lookup_function(
    "getWallThickness",
    wall_thickness,
    parent=studio,
    predicate_dict={"CUSTOM": custom_wall_thickness},
    return_type=Type.VALUE,
)

tube_size = (
    custom_enum_factory.add_enum("TubeSize", parent=studio)
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .make()
)

tube_type = (
    custom_enum_factory.add_enum("TubeType", parent=studio)
    .add_value("MAX_TUBE", user_name="MAXTube")
    .make()
)

max_tube_type = (
    enum_factory.add_enum("MaxTubeType", parent=studio)
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX", user_name="MAX")
    .make()
)

can_be_light = ui_test_predicate(
    "canBeLight",
    max_tube_type["NONE"]() | max_tube_type["GRID"](),
    parent=studio,
)

type_predicates = enum_predicates(tube_type, parent=studio)
size_predicates = enum_predicates(tube_size, parent=studio)

is_max_tube = ui_test_predicate(
    "isMaxTube", ~size_predicates["CUSTOM"] & type_predicates["MAX_TUBE"], parent=studio
)

tube_predicate = UiPredicate("tubeSize", parent=studio)
tube_predicate.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = IfBlock(size_predicates["CUSTOM"], parent=tube_predicate).add(
    LengthAnnotation("length", LengthBound.LENGTH_BOUNDS),
    LengthAnnotation("width", LengthBound.LENGTH_BOUNDS),
)

tube_if.or_else().add(
    EnumAnnotation(
        tube_type,
        default="CUSTOM",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL],
    )
)

inner_if = IfBlock(
    size_predicates["TWO_BY_ONE"] & type_predicates["MAX_TUBE"], parent=tube_if
).add(
    EnumAnnotation(max_tube_type, user_name="Pattern type"),
    IfBlock(can_be_light).add(
        BooleanAnnotation("isLight", user_name="Light"),
    ),
)

IfBlock(
    size_predicates["CUSTOM"] | type_predicates["CUSTOM"], parent=tube_predicate
).add(wall_predicate())

fit = (
    custom_enum_factory.add_enum("HoleFit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .make()
)

size = (
    custom_enum_factory.add_enum("HoleSize", parent=studio, value_type=LookupEnumValue)
    .add_value("NO_8", user_name="#8")
    .add_value("NO_10", user_name="#10")
    .make()
)

hole_predicate = UiPredicate("tubeHole", parent=studio)
hole_predicate.add(
    EnumAnnotation(size, default="NO_10"),
    IfBlock(size["NO_8"]() | size["NO_10"]())
    .add(EnumAnnotation(fit, default="FREE"))
    .or_else()
    .add(LengthAnnotation("holeSize", LengthBound.BLEND_BOUNDS)),
)


studio.print()
