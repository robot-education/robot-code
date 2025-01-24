import dataclasses
import enum as std_enum
from typing import override

from featurescript.base import ctxt, node, expr, user_error
from featurescript.core import std, map

# https://cad.onshape.com/documents/12312312345abcabcabcdeff/w/a855e4161c814f2e9ab3698a/e/87b09e244a234eb791b47826


class BoundEnum(std_enum.StrEnum):
    @staticmethod
    def _generate_next_value_(name, start, count, last_values):
        return name


class LengthBound(BoundEnum):
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


class AngleBound(BoundEnum):
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


class IntegerBound(BoundEnum):
    POSITIVE_COUNT_BOUNDS = std_enum.auto()
    """An `IntegerBoundSpec` for an integer strictly greater than zero, defaulting to 2."""
    PRIMARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the primary direction of linear pattern. Defaults to 2, with a max of `INSTANCE_COUNT_MAX`."""
    SECONDARY_PATTERN_BOUNDS = std_enum.auto()
    """bounds for the secondary direction of a linear pattern. Defaults to 1."""
    CIRCULAR_PATTERN_BOUNDS = std_enum.auto()
    """bounds for a circular pattern. Defaults to 4."""
    # CURVE_PATTERN is identical to PRIMARY_PATTERN
    # CURVE_PATTERN_BOUNDS = std_enum.auto()
    # """bounds for a curve pattern."""
    GRID_BOUNDS = std_enum.auto()
    """The bounds for the density of an isocurve grid. Defaults to 10. Ranges from 1 to 50."""


class RealBound(BoundEnum):
    SCALE_BOUNDS = std_enum.auto()
    """A `RealBoundSpec` for the positive or negative scale factor on a transform, defaulting to `1`."""
    POSITIVE_REAL_BOUNDS = std_enum.auto()
    """A `RealBoundSpec` for a number greater than or equal to zero, defaulting to 1."""
    FILLET_RHO_BOUNDS = std_enum.auto()
    """A `RealBoundSpec` for a value greater than or equal to zero and strictly less than 1."""
    EDGE_PARAMETER_BOUNDS = std_enum.auto()
    """A `RealBoundSpec` for a normalized parameter along an edge's length, with 0 being the start of the edge and 1 the end.
    Defaults to 0.5, i.e. the midpoint of an open edge"""
    CLAMP_MAGNITUDE_REAL_BOUNDS = std_enum.auto()
    """Ranges from -inf to inf. Defaults to 1."""


@dataclasses.dataclass
class BoundSpec(node.ParentNode, expr.Expression):
    name: str
    bounds: dict[str, str]
    type: str

    @override
    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP:
            bound_map = dict(
                ("(" + key + ")", value) for key, value in self.bounds.items()
            )
            body = map.Map(
                bound_map, quote_keys=False, inline=False, type=self.type
            ).run_build(context, scope=ctxt.Scope.EXPRESSION)
            return std.Const(self.name, body).run_build(context, scope=ctxt.Scope.TOP)
        elif context.scope == ctxt.Scope.EXPRESSION:
            return self.name
        return user_error.expected_scope(ctxt.Scope.TOP, ctxt.Scope.EXPRESSION)


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
