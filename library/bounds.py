import enum as std_enum

# https://cad.onshape.com/documents/12312312345abcabcabcdeff/w/a855e4161c814f2e9ab3698a/e/87b09e244a234eb791b47826

__all__ = ["LengthBound", "AngleBound"]


class LengthBound(std_enum.StrEnum):
    # (-inf, inf, 1)
    LENGTH_BOUNDS = "LENGTH_BOUNDS"
    # > 0
    NONNEGATIVE_LENGTH_BOUNDS = "NONNEGATIVE_LENGTH_BOUNDS"
    # >= 0
    NONNEGATIVE_ZERO_INCLUSIVE_LENGTH_BOUNDS = (
        "NONNEGATIVE_ZERO_INCLUSIVE_LENGTH_BOUNDS"
    )
    # >= 0, defaults to 0
    NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS = "NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS"
    # > 0, defaults to 0
    ZERO_DEFAULT_LENGTH_BOUNDS = "ZERO_DEFAULT_LENGTH_BOUNDS"
    # > 0, defaults to 0.2"
    BLEND_BOUNDS = "BLEND_BOUNDS"
    # A `LengthBoundSpec` for a shell or offset thickness, with smaller defaults than `NONNEGATIVE_LENGTH_BOUNDS`.
    # (`0.1 * inch`, etc.).
    SHELL_OFFSET_BOUNDS = "SHELL_OFFSET_BOUNDS"
    # A `LengthBoundSpec` for an offset thickness, for a length greater than or equal to 0, with defaults
    # greater than NONNEGATIVE_ZERO_INCLUSIVE_LENGTH_BOUNDS
    ZERO_INCLUSIVE_OFFSET_BOUNDS = "ZERO_INCLUSIVE_OFFSET_BOUNDS"
    PLANE_SIZE_BOUNDS = "PLANE_SIZE_BOUNDS"
    AREA_BOUNDS = "AREA_BOUNDS"
    VOLUME_BOUNDS = "VOLUME_BOUNDS"


class AngleBound(std_enum.StrEnum):
    ANGLE_360_BOUNDS = "ANGLE_360_BOUNDS"
    ANGLE_360_ZERO_DEFAULT_BOUNDS = "ANGLE_360_ZERO_DEFAULT_BOUNDS"
