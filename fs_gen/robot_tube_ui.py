from library import *

studio = Studio("robotTubeUi.gen.fs")

wall_thickness = (
    studio.register(Enum("WallThickness"))
    .add_value("ONE_SIXTEENTH", user_name='1/16"')
    .add_value("ONE_EIGHTH", user_name='1/8"')
    .add_value("CUSTOM")
)

custom_wall_thickness = studio.register(
    UiTestPredicate("isCustomWallThickness", equal(wall_thickness.CUSTOM))
).call()

wall_thickness_predicate = studio.register(UiPredicate("wallThickness"))
wall_thickness_predicate.add(
    EnumAnnotation(
        wall_thickness, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    )
)
wall_thickness_predicate.register(If(custom_wall_thickness)).add(
    LengthAnnotation("wallThickness", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS)
)

tube_size = (
    studio.register(Enum("TubeSize"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .add_custom_value()
)

tube_type = (
    studio.register(Enum("TubeType"))
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_custom_value()
)

max_tube_type = (
    studio.register(Enum("MaxTubeType"))
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX", user_name="MAX")
)


can_be_light = (
    studio.register(
        UiTestPredicate(
            "canBeLight", equal(max_tube_type.NONE) | equal(max_tube_type.GRID)
        )
    )
).call()

type_predicates = studio.register(EnumPredicates(tube_type, prepend="isTubeType"))
size_predicates = studio.register(EnumPredicates(tube_size, prepend="isTubeSize"))

studio.register(UiTestPredicate("isMaxTube", ~size_predicates[2] & type_predicates[0]))


tube_predicate = studio.register(UiPredicate("tubeSize"))
tube_predicate.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = tube_predicate.register(If(size_predicates[2]))
tube_if.add(LengthAnnotation("length", LengthBound.LENGTH_BOUNDS))
tube_if.add(LengthAnnotation("width", LengthBound.LENGTH_BOUNDS))

tube_if = tube_if.or_else()
tube_if.add(
    EnumAnnotation(
        tube_type,
        default="CUSTOM",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL],
    )
)

inner_if = tube_if.register(If(size_predicates[1] & type_predicates[0])).add(
    EnumAnnotation(max_tube_type, user_name="Pattern type"),
)

inner_if.register(If(can_be_light)).add(
    BooleanAnnotation("isLight", user_name="Light"),
)

wall_thickness_if = tube_predicate.register(If(size_predicates[2] | type_predicates[1]))
wall_thickness_if.add(wall_thickness_predicate.call())

fit = studio.register(Enum("HoleFit")).add_value("CLOSE").add_value("FREE")

size = (
    studio.register(Enum("HoleSize"))
    .add_value("NO_8", user_name="#8")
    .add_value("NO_10", user_name="#10")
    .add_custom_value()
)

hole_predicate = studio.register(UiPredicate("tubeHole"))
# hole_predicate.register(If())


studio.print()
