"""Metaprogramming classes used to generate configurable code."""
from library import *


class HexSizeFactory:
    def __init__(self, studio: Studio, width_parameter: str = "width") -> None:
        self.studio = studio
        self.width_parameter = width_parameter
        self.enum = (
            EnumFactory("HexSize", parent=self.studio, value_type=LookupEnumValue)
            .add_value("_1_2_IN", "1/2 in.", lookup_value=inch(1 / 2))
            .add_value("_3_8_IN", "3/8 in.", lookup_value=inch(3 / 8))
            .add_custom(lookup_value=definition(self.width_parameter))
            .make()
        )
        self.predicate = self._register_predicate()
        self.lookup_function = self._register_lookup_function()

    def _register_predicate(self) -> UiPredicate:
        return UiPredicate("hexSize", parent=self.studio).add(
            labeled_enum_parameter(self.enum),
            IfBlock(self.enum["CUSTOM"]).add(
                length_parameter(
                    self.width_parameter,
                    user_name="Width",
                    bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                )
            ),
        )

    def _register_lookup_function(self) -> Function:
        return enum_lookup_function(
            "getHexSize", self.enum, return_type=Type.VALUE, parent=self.studio
        )


class HoleSizeFactory:
    def __init__(self, studio: Studio) -> None:
        self.studio = studio
        self.enum = (
            EnumFactory("HoleSize", parent=studio)
            .add_value("_NO_8", "#8")
            .add_value("_NO_10", "#10")
            .add_value("_1_4_20", "1/4-20")
            .add_custom()
            .make()
        )

        # self.hole_diameter =
        self.predicate = UiPredicate("holeSize", parent=self.studio).add(
            labeled_enum_parameter(self.enum, default="_NO_10"),
            IfBlock(self.enum["CUSTOM"]).add(
                length_parameter("holeDiameter", bound_spec=LengthBound.BLEND_BOUNDS)
            ),
        )

    def register_function(self, fit_enum: Enum) -> Function:
        return Function(
            "getHoleSize",
            parent=self.studio,
            arguments=definition_arg,
            return_type=Type.VALUE,
        ).add(IfBlock(~self.enum["CUSTOM"]).add(), Return(definition("holeDiameter")))


def fit_enum(parent: Studio | None = None) -> Enum:
    return EnumFactory("Fit", parent=parent).add_value("CLOSE").add_value("FREE").make()


# labeled_enum_parameter(
#     hole_size,
#     default="NO_10",
# ),
# IfBlock(is_hole_size_set).add(labeled_enum_parameter(fit)),

# is_hole_size_set := UiTestPredicate(
#     "isHoleSizeSet", hole_size["NO_8"] | hole_size["NO_10"]
# ),

# hole_size = (
#     EnumFactory("HoleSize", parent=studio)
#     .add_value("NO_8", "#8")
#     .add_value("NO_10", "#10")
#     .add_custom()
#     .make()
# )
