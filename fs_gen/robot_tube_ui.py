from library import *

studio = Studio("robotTubeUi.gen.fs")

wall_thickness = (
    studio.register(CustomEnum("WallThickness"))
    .add_value("ONE_SIXTEENTH", user_name='1/16"')
    .add_value("ONE_EIGHTH", user_name='1/8"')
    .add_custom()
)

custom_wall_thickness = studio.register(custom_predicate(wall_thickness)).call()

wall_predicate = studio.register(UiPredicate("wallThickness"))
wall_predicate.add(
    EnumAnnotation(
        wall_thickness, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    )
)
wall_predicate.register(If(custom_wall_thickness)).add(
    LengthAnnotation("wallThickness", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS)
)

tube_size = (
    studio.register(CustomEnum("TubeSize"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .add_custom()
)

tube_type = (
    studio.register(CustomEnum("TubeType"))
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_custom()
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

type_predicates = studio.register(EnumPredicates(tube_type))
size_predicates = studio.register(EnumPredicates(tube_size))

is_max_tube = studio.register(
    UiTestPredicate(
        "isMaxTube", ~size_predicates["CUSTOM"] & type_predicates["MAX_TUBE"]
    )
).call()


tube_predicate = studio.register(UiPredicate("tubeSize"))
tube_predicate.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = tube_predicate.register(If(size_predicates["CUSTOM"]))
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

inner_if = tube_if.register(
    If(size_predicates["TWO_BY_ONE"] & type_predicates["MAX_TUBE"])
).add(
    EnumAnnotation(max_tube_type, user_name="Pattern type"),
)

inner_if.register(If(can_be_light)).add(
    BooleanAnnotation("isLight", user_name="Light"),
)

wall_if = tube_predicate.register(
    If(size_predicates["CUSTOM"] | type_predicates["CUSTOM"])
)
wall_if.add(wall_predicate.call())

fit = studio.register(Enum("HoleFit")).add_value("CLOSE").add_value("FREE")

size = (
    studio.register(CustomEnum("HoleSize"))
    .add_value("NO_8", user_name="#8")
    .add_value("NO_10", user_name="#10")
    .add_custom()
)

hole_predicate = studio.register(UiPredicate("tubeHole"))
# hole_predicate.register(If())


studio.print()
