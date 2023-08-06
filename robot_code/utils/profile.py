"""Metaprogramming classes used to generate configurable code."""
from library import *


class HexSizeFactory:
    def __init__(self, studio: Studio, width_parameter: str = "hexWidth") -> None:
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
                    display_name="Width",
                    bound_spec=LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
                )
            ),
        )

    def _register_lookup_function(self) -> Function:
        return lookup_enum_function(
            "getHexWidth", self.enum, return_type=Type.VALUE, parent=self.studio
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

        self.predicate = UiPredicate("holeSize", parent=self.studio).add(
            labeled_enum_parameter(self.enum, default="_NO_10"),
            IfBlock(self.enum["CUSTOM"]).add(
                length_parameter("holeDiameter", bound_spec=LengthBound.BLEND_BOUNDS)
            ),
        )

    def make_lookup_function(self, fit_enum: Enum) -> Function:
        self.studio.add(
            function := Function(
                "getHoleSize",
                parameters=definition_param,
                return_type=Type.VALUE,
            ).add(
                IfBlock(~self.enum["CUSTOM"]).add(
                    Return(MapAccess("HOLE_SIZES", self.enum, fit_enum)),
                ),
                Return(definition("holeDiameter")),
            ),
            Const(
                "HOLE_SIZES",
                enum_map(
                    self.enum,
                    enum_map(fit_enum, inch(0.1695), inch(0.177)),
                    enum_map(fit_enum, inch(0.196), inch(0.201)),
                    enum_map(fit_enum, inch(0.257), inch(0.266)),
                ),
            ),
        )
        return function


def fit_enum(parent: Studio | None = None, custom: bool = False) -> Enum:
    factory = EnumFactory("Fit", parent=parent).add_value("CLOSE").add_value("FREE")
    if custom:
        factory.add_value("CUSTOM")
    return factory.make()


def fit_tolerance(fit: Enum) -> Ternary:
    return Ternary(fit["CLOSE"], inch(0.008), inch(0.016))
