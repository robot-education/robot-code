from library import *

studio = Studio("robotTubeUiGen.fs")

tube_type = (
    studio.add(Enum("TubeType"))
    .add_value("ONE_BY_ONE", user_name="1x1")
    .add_value("ONE_BY_TWO", user_name="1x2")
    .add_value("Custom")
)

for value in tube_type:
    studio.add(make_enum_test_predicate(value, name_append="isTube"))

tube_ui = studio.add(UiPredicate("robotTube"))

tube_ui.add(
    EnumAnnotation(
        tube_type,
        default="ONE_BY_TWO",
        ui_hints=[UiHint.REMEMBER_PREVIOUS_VALUE, UiHint.HORIZONTAL_ENUM],
    )
)


# comp_enum = studio.add(Enum("Competition", "FRC", "VEX", generate_names=False))

# comp_enum_tests = {}
# for value in comp_enum:
#     name = "is" + value.camel_case(capitalize=True)
#     pred = studio.add(UiTestPredicate(name))
#     pred.add(equal("competition", value))
#     comp_enum_tests[name] = pred.call()


# pred = studio.add(UiPredicate("competition"))
# pred.add(EnumAnnotation(comp_enum))

# frame_if = pred.add(If(comp_enum_tests["isFrc"]))


studio.print()
