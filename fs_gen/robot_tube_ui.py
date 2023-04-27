from library import *

studio = Studio("robotTubeUi.gen.fs")

wall_thickness = (
    lookup_enum_factory.add_enum("WallThickness", parent=studio)
    .add_value("ONE_SIXTEENTH", user_name='1/16"', lookup_value="0.0625 * inch")
    .add_value("ONE_EIGHTH", user_name='1/8"', lookup_value="0.125 * inch")
    .add_custom(lookup_value="definition.customWallThickness")
    .make()
)

custom_wall_thickness = custom_enum_predicate(wall_thickness, parent=studio)

wall_predicate = UiPredicate("wallThickness", parent=studio)
wall_predicate.add(
    EnumAnnotation(
        wall_thickness, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    )
)
If(custom_wall_thickness, parent=wall_predicate).add(
    LengthAnnotation("customWallThickness", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS)
)

lookup_function(
    wall_thickness, parent=studio, predicate_dict={"CUSTOM": custom_wall_thickness}
)

tube_size = (
    enum_factory.add_enum("TubeSize", parent=studio)
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .add_custom()
    .make()
)

tube_type = (
    enum_factory.add_enum("TubeType", parent=studio)
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_custom()
    .make()
)

max_tube_type = (
    enum_factory.add_enum("MaxTubeType", parent=studio)
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX", user_name="MAX")
    .make()
)

can_be_light = UiTestPredicate(
    "canBeLight",
    max_tube_type["NONE"]() | max_tube_type["GRID"](),
    parent=studio,
)()

type_predicates = enum_predicates(tube_type, parent=studio)
size_predicates = enum_predicates(tube_size, parent=studio)

is_max_tube = UiTestPredicate(
    "isMaxTube", ~size_predicates["CUSTOM"] & type_predicates["MAX_TUBE"], parent=studio
)()

tube_predicate = UiPredicate("tubeSize", parent=studio)
tube_predicate.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = If(size_predicates["CUSTOM"], parent=tube_predicate)
tube_if.add(LengthAnnotation("length", LengthBound.LENGTH_BOUNDS))
tube_if.add(LengthAnnotation("width", LengthBound.LENGTH_BOUNDS))

tube_if.or_else().add(
    EnumAnnotation(
        tube_type,
        default="CUSTOM",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL],
    )
)

inner_if = If(
    size_predicates["TWO_BY_ONE"] & type_predicates["MAX_TUBE"], parent=tube_if
).add(EnumAnnotation(max_tube_type, user_name="Pattern type"))

If(can_be_light, parent=inner_if).add(
    BooleanAnnotation("isLight", user_name="Light"),
)

If(size_predicates["CUSTOM"] | type_predicates["CUSTOM"], parent=tube_predicate).add(
    wall_predicate()
)

fit = (
    enum_factory.add_enum("HoleFit", parent=studio)
    .add_value("CLOSE")
    .add_value("FREE")
    .make()
)

size = (
    enum_factory.add_enum("HoleSize", parent=studio)
    .add_value("NO_8", user_name="#8")
    .add_value("NO_10", user_name="#10")
    .add_custom()
    .make()
)

hole_predicate = UiPredicate("tubeHole", parent=studio)
# hole_predicate.register(If())


studio.print()
