from library import *

studio = Studio("robotTubeUiGen.fs")

tube_type = (
    studio.add(Enum("TubeSupplier"))
    .add_value("MAX_TUBE", user_name="MAXTube")
    .add_value("VERSA_FRAME", user_name="VersaFrame")
    .add_value("CUSTOM")
)

tube_size = (
    studio.add(Enum("TubeSize"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("ONE_BY_TWO", user_name="1x2")
    .add_value("CUSTOM")
)

size_predicates = [
    studio.add(make_enum_test_predicate(value, name_prepend="isTube")).call()
    for value in tube_size
]

tube_ui = studio.add(UiPredicate("robotTube"))

tube_ui.add(
    EnumAnnotation(
        tube_size,
        default="ONE_BY_TWO",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)

tube_type = tube_ui.add(
    EnumAnnotation(
        tube_type, ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.SHOW_LABEL]
    )
)

tube_if = tube_ui.add(If(size_predicates[0]))



tube_if = tube_ui.add(If(size_predicates[1]))

tube_if = tube_ui.add(If(size_predicates[2]))


studio.print()
