from library import *

studio = Studio("robotTubeUiGen.fs")

tube_type = (
    studio.register(Enum("TubeType"))
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_value("VERSA_FRAME", user_name="VersaFrame")
    .add_value("CUSTOM")
)

max_tube_type = (
    studio.register(Enum("MaxTubePatternType"))
    .add_value("NONE")
    .add_value("GRID")
    .add_value("MAX")
)

tube_size = (
    studio.register(Enum("TubeSize"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("TWO_BY_ONE", user_name="2x1")
    .add_value("CUSTOM")
)

size_predicates = [
    studio.register(make_enum_test_predicate(value, name_prepend="isTube")).call()
    for value in tube_size
]

tube_ui = studio.register(UiPredicate("robotTube"))
tube_ui.add(
    EnumAnnotation(
        tube_size,
        default="TWO_BY_ONE",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_if = tube_ui.register(If(~size_predicates[2]))
tube_if.add(
    EnumAnnotation(
        tube_type, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    ),
)

inner_if = tube_if.register(If(equal(tube_type.MAX_TUBE)))
inner_if.add(
    EnumAnnotation(max_tube_type, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE])
    # BooleanAnnotation isLight
)

inner_if.else_if(equal(tube_type.VERSA_FRAME))

tube_if.or_else()


studio.print()
