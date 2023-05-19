from library import *

studio = Studio("robotBoreUi.gen.fs", "backend")


inner_bore_type = (
    EnumFactory()
    .add_enum("InnerBoreType", parent=studio)
    .add_value("HEX")
    .add_value("CIRCLE")
    .add_value("MAX_SPLINE")
    .make()
)

outer_bore_type = (
    EnumFactory()
    .add_enum("OuterBoreType", parent=studio)
    .add_value("FALCON_SPLINE")
    .add_value("GEAR")
    .add_value("INSERT")
    .make()
)

insert_type = (
    EnumFactory()
    .add_enum("InsertType", parent=studio)
    .add_value("HEX", user_name="1/2 in. hex")
    .add_value("SPLINE", user_name="Falcon spline")
    .make()
)

studio.add(
    inner_bore := UiPredicate("innerBore").add(
        DrivenGroupParameter("hasBore", user_name="Bore").add(
            LabeledEnumParameter(inner_bore_type),
            IfBlock(inner_bore_type["HEX"])
            .add(
                LengthParameter(
                    "width", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
                )
            )
            .else_if(inner_bore_type["CIRCLE"])
            .add(
                LengthParameter(
                    "diameter", bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS
                )
            )
            .add(
                # RealParameter("scale")
            ),
        )
    ),
)
