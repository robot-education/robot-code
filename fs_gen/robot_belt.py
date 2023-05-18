from library import *

studio = Studio("robotBeltUi.gen.fs", "backend")

belt_type = (
    enum_factory.add_enum("BeltType", parent=studio)
    .add_value("GT2")
    .add_value("HTD")
    .add_value("RT25")
    .make()
)

gt2_pitch = (
    enum_factory.add_enum("Gt2BeltPitch", parent=studio, value_type=LookupEnumValue)
    .add_value(
        "_2MM",
        lookup_value=millimeter(2),
    )
    .add_value("_3MM", lookup_value=millimeter(3))
    .make()
)

htd_width = (
    enum_factory.add_enum("HtdBeltWidth", parent=studio, value_type=LookupEnumValue)
    .add_value(
        "_9MM",
        lookup_value=millimeter(9),
    )
    .add_value("_15MM", lookup_value=millimeter(15))
    .make()
)

studio.add(
    belt_type_predicate := UiPredicate("beltType").add(
        LabeledEnumParameter(belt_type),
        IfBlock(belt_type["GT2"])
        .add(
            LabeledEnumParameter(gt2_pitch, user_name="Belt pitch", default="_3MM"),
        )
        .else_if(belt_type["HTD"])
        .add(
            LabeledEnumParameter(htd_width, user_name="Belt width", default="_15MM"),
        ),
    ),
    UiPredicate("belt").add(
    
    ),
)
