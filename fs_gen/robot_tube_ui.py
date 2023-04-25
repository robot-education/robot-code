from library import *

studio = Studio("robotTubeUi.gen.fs")

tube_size = (
    studio.register(Enum("TubeSize"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .add_value("CUSTOM")
)

tube_type = (
    studio.register(Enum("TubeType"))
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_value("CUSTOM")
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

type_predicates = [
    studio.register(
        UiTestPredicate(predicate_name(value, prepend="isTubeType"), equal(value))
    ).call()
    for value in tube_type
]
size_predicates = [
    studio.register(
        UiTestPredicate(predicate_name(value, prepend="isTubeSize"), equal(value))
    ).call()
    for value in tube_size
]

# studio.register(
#     UiTestPredicate("isMaxTube", size_predicates[0])
# )


tube_ui = studio.register(UiPredicate("robotTube"))
tube_ui.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = tube_ui.register(If(size_predicates[2]))
tube_if.add(LengthAnnotation("length", bound_spec=LengthBound.LENGTH_BOUNDS))
tube_if.add(LengthAnnotation("width", bound_spec=LengthBound.LENGTH_BOUNDS))

# inner_if = inner_if.else_if(equal(tube_type.CUSTOM))

tube_if = tube_if.or_else()
tube_if.add(
    EnumAnnotation(
        tube_type,
        default="CUSTOM",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL],
    ),
)

inner_if = tube_if.register(If(size_predicates[1] & type_predicates[0])).add(
    EnumAnnotation(max_tube_type, user_name="Pattern type"),
)

inner_if.register(If(can_be_light)).add(
    BooleanAnnotation("isLight", user_name="Light"),
)


tube_ui.register(If(size_predicates[2] | type_predicates[1])).add(
    LengthAnnotation("wallThickness", bound_spec=LengthBound.SHELL_OFFSET_BOUNDS)
)

studio.print()
