import dataclasses
import enum as std_enum
from typing_extensions import override

from library.base import ctxt, node
from library.core import std, map

# https://cad.onshape.com/documents/12312312345abcabcabcdeff/w/a855e4161c814f2e9ab3698a/e/87b09e244a234eb791b47826


class LengthBound(std_enum.StrEnum):
    @override
    def _generate_next_value_(name, start, count, last_values) -> str:
        return name

    LENGTH_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a positive or negative length."""
    NONNEGATIVE_LENGTH_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a length strictly greater than 0."""
    NONNEGATIVE_ZERO_INCLUSIVE_LENGTH_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a length greater than or equal to 0."""
    NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a length greater than or equal to 0, with UI defaults of 0.0 for all units."""
    ZERO_DEFAULT_LENGTH_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a positive or negative length, with UI defaults of 0.0 for all units."""
    BLEND_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for fillets and chamfers, with smaller defaults than `NONNEGATIVE_LENGTH_BOUNDS` (`0.2 * inch`, etc.)."""
    SHELL_OFFSET_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for a shell or offset thickness, with smaller defaults than `NONNEGATIVE_LENGTH_BOUNDS`. (`0.1 * inch`, etc.)."""
    ZERO_INCLUSIVE_OFFSET_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for an offset thickness, for a length greater than or equal to 0, with defaults greater than `NONNEGATIVE_ZERO_INCLUSIVE_LENGTH_BOUNDS`."""
    PLANE_SIZE_BOUNDS = std_enum.auto()
    """A `LengthBoundSpec` for the size of a construction plane."""


class AngleBound(std_enum.StrEnum):
    @override
    def _generate_next_value_(name, start, count, last_values) -> str:
        return name

    ANGLE_360_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between 0 and 360 degrees, defaulting to 30 degrees."""
    ANGLE_360_REVERSE_DEFAULT_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between 0 and 360 degrees, defaulting to 330 degrees."""
    ANGLE_360_ZERO_DEFAULT_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between 0 and 360 degrees, defaulting to 0 degrees."""
    ANGLE_360_FULL_DEFAULT_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between 0 and 360 degrees, defaulting to 360 degrees."""
    ANGLE_360_90_DEFAULT_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between 0 and 360 degrees, defaulting to 90 degrees."""
    ANGLE_STRICT_180_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle strictly less than 180 degrees."""
    ANGLE_STRICT_90_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle strictly less than 90 degrees."""
    ANGLE_180_MINUS_180_BOUNDS = std_enum.auto()
    """An `AngleBoundSpec` for an angle between -180 and 180 degrees, defaulting to 0 degrees."""


class CountBound(std_enum.StrEnum):
    @override
    def _generate_next_value_(name, start, count, last_values) -> str:
        return name

    POSITIVE_COUNT_BOUNDS = std_enum.auto()
    """An `IntegerBoundSpec` for an integer strictly greater than zero, defaulting to 2."""
    PRIMARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the primary direction of linear pattern."""
    SECONDARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the secondary direction of a linear pattern."""
    CIRCULAR_PATTERN_BOUNDS = std_enum.auto()
    """bounds for a circular pattern."""
    CURVE_PATTERN_BOUNDS = std_enum.auto()
    """bounds for a curve pattern."""
    GRID_BOUNDS = std_enum.auto()
    """The bounds for the density of an isocurve grid."""


class RealBound(std_enum.StrEnum):
    @override
    def _generate_next_value_(name, start, count, last_values) -> str:
        return name

    SCALE_BOUNDS = std_enum.auto()

    POSITIVE_COUNT_BOUNDS = std_enum.auto()
    """An `IntegerBoundSpec` for an integer strictly greater than zero, defaulting to 2."""
    PRIMARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the primary direction of linear pattern."""
    SECONDARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the secondary direction of a linear pattern."""
    CIRCULAR_PATTERN_BOUNDS = std_enum.auto()
    """bounds for a circular pattern."""
    CURVE_PATTERN_BOUNDS = std_enum.auto()
    """bounds for a curve pattern."""
    GRID_BOUNDS = std_enum.auto()
    """The bounds for the density of an isocurve grid."""


@dataclasses.dataclass
class BoundSpec(node.TopStatement):
    name: str
    bounds: dict[str, str]
    type: str

    @override
    def build_top(self, context: ctxt.Context) -> str:
        bound_map = dict(("(" + key + ")", value) for key, value in self.bounds.items())
        body = map.Map(
            bound_map, quote_keys=False, inline=False, type=self.type
        ).run_build(context)
        return std.Const(self.name, body).run_build_top(context)

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.name


VALUE_MIN = int(-1e5)
"""The minimum value for angle and integer parameters. Equal to -`VALUE_MAX`."""
VALUE_MAX = int(1e5)
"""The maximum value for angle and integer parameters."""
INSTANCE_COUNT_MAX: int = 2500
"""The maximum number of pattern instances."""
LENGTH_MAX: int = 500
"""The maximum length."""
ZERO_TOLERANCE: float = 1e-5
"""A minimum tolerance for an angle or length greater than zero."""


def inch_to_meter(inch: float) -> float:
    return inch * 0.0254


def millimeter_to_meter(millimeter: float) -> float:
    return millimeter / 1000


class IntegerBoundSpec(BoundSpec):
    def __init__(
        self,
        name: str,
        min: int = 0,
        default: int = 2,
        max: int = VALUE_MAX,
    ) -> None:
        bounds = {"unitless": "[{}, {}, {}]".format(min, default, max)}
        super().__init__(name, bounds, "IntegerBoundSpec")


class LengthBoundSpec(BoundSpec):
    def __init__(
        self,
        name: str,
        min: float = VALUE_MIN,
        default: float = 0,
        max: float = VALUE_MAX,
        millimeter_default: float | None = None,
        centimeter_default: float | None = None,
        inch_default: float | None = None,
        foot_default: float | None = None,
        yard_default: float | None = None,
    ) -> None:
        bounds = {"meter": "[{}, {}, {}]".format(min, default, max)}
        if millimeter_default is not None:
            bounds["millimeter"] = str(millimeter_default)
        if centimeter_default is not None:
            bounds["centimeter"] = str(centimeter_default)
        if inch_default is not None:
            bounds["inch"] = str(inch_default)
        if foot_default is not None:
            bounds["foot"] = str(foot_default)
        if yard_default is not None:
            bounds["yard"] = str(yard_default)
        super().__init__(name, bounds, "LengthBoundSpec")


class RealBoundSpec(BoundSpec):
    def __init__(
        self,
        name: str,
        min: float = ZERO_TOLERANCE,
        default: int = 1,
        max: int = VALUE_MAX,
    ) -> None:
        bounds = {"unitless": "[{}, {}, {}]".format(min, default, max)}
        super().__init__(name, bounds, "RealBoundSpec")
