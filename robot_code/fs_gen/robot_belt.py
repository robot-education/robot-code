from library import *

studio = Studio("robotBeltUi.gen.fs", "backend")

belt_type = (
    ENUM_FACTORY.add_enum("BeltType", parent=studio)
    .add_value("GT2")
    .add_value("HTD")
    .add_value("RT25")
    .make()
)

gt2_pitch = (
    ENUM_FACTORY.add_enum("Gt2BeltPitch", parent=studio, value_type=LookupEnumValue)
    .add_value(
        "_2MM",
        lookup_value=millimeter(2),
    )
    .add_value("_3MM", lookup_value=millimeter(3))
    .make()
)

htd_pitch = (
    ENUM_FACTORY.add_enum("Gt2BeltPitch", parent=studio, value_type=LookupEnumValue)
    .add_value(
        "_3MM",
        lookup_value=millimeter(3),
    )
    .add_value("_5MM", lookup_value=millimeter(5))
    .make()
)

htd_width = (
    ENUM_FACTORY.add_enum("HtdBeltWidth", parent=studio, value_type=LookupEnumValue)
    .add_value(
        "_9MM",
        lookup_value=millimeter(9),
    )
    .add_value("_15MM", lookup_value=millimeter(15))
    .make()
)

studio.add(
    belt_type_predicate := UiPredicate("beltType").add(
        labeled_enum_parameter(belt_type),
        IfBlock(belt_type["GT2"])
        .add(
            labeled_enum_parameter(gt2_pitch, user_name="Belt pitch", default="_3MM"),
        )
        .else_if(belt_type["HTD"])
        .add(
            labeled_enum_parameter(htd_pitch, user_name="Belt pitch", default="_5MM"),
            labeled_enum_parameter(htd_width, user_name="Belt width", default="_15MM"),
        ),
    ),
    UiPredicate("belt").add(),
)
