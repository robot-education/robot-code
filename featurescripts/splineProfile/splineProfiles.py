"""The SplineXL (WCP), MAXSpline (REV), and SplineXS shaft profiles. Run `uv run fs gen` after editing.

Each spline has six teeth. The inside profile is the bore of a tube shaft: the outside profile
offset inward by the tube's wall. Offsetting keeps every arc's center, shrinking convex arcs and
growing concave fillets.
"""

import math

from fs_cli.gen import Import
from fs_cli.sketches import MM, ORIGIN, Arc, Entity, FitSpline, Line, Point, Profile, Sketch, SketchMap, polar

TEETH = 6
PITCH = 360 / TEETH
TUBE_WALL = 1 / 16


def law_of_cosines(a: float, b: float, c: float) -> float:
    """The angle (in degrees) opposite side c of a triangle with sides a, b, and c."""
    return math.degrees(math.acos((a * a + b * b - c * c) / (2 * a * b)))


def tangent_point(center_a: Point, radius_a: float, center_b: Point) -> Point:
    """Where a circle about center_a touches a circle about center_b it's tangent to."""
    direction = center_b - center_a
    return center_a + direction * (radius_a / direction.length)


def spline_xl(offset: float) -> list[Entity]:
    """Rounded teeth: each is an arc, joined to the root circle by a fillet on each side."""
    root, tooth_radius, tooth_center, fillet = 0.562, 0.171375, 0.515625, 0.08
    fillet_center = root + fillet
    # The fillets are tangent to the tooth arc
    fillet_angle = law_of_cosines(tooth_center, fillet_center, tooth_radius + fillet)
    teeth: list[Entity] = []
    roots: list[Entity] = []
    for tooth in range(TEETH):
        angle = tooth * PITCH
        center = polar(tooth_center, angle)
        flanks = []
        for side in (1, -1):
            fillet_at = polar(fillet_center, angle + side * fillet_angle)
            flank = tangent_point(center, tooth_radius - offset, fillet_at)
            flanks.append(flank)
            on_root = polar(root - offset, angle + side * fillet_angle)
            teeth.append(Arc.between(fillet_at, on_root, flank))
        tip = polar(tooth_center + tooth_radius - offset, angle)
        teeth.insert(-1, Arc(flanks[0], tip, flanks[1]))
        roots.append(
            Arc.around(ORIGIN, root - offset, angle + fillet_angle, angle + PITCH - fillet_angle)
        )
    return teeth + roots


def max_spline(offset: float) -> list[Entity]:
    """Flat-topped teeth: a tip arc, then a tip fillet and a root fillet down to the root circle."""
    root, tip = 14.275 * MM, 17.45 * MM
    tip_fillet, root_fillet = 2.0375 * MM, 2.0625 * MM
    # Half the angle the tip arc spans, measured from the original sketch
    tip_angle = 7.403381433
    tip_fillet_center = tip - tip_fillet
    root_fillet_center = root + root_fillet
    # The tip and root fillets are tangent to each other
    root_angle = tip_angle + law_of_cosines(
        tip_fillet_center, root_fillet_center, tip_fillet + root_fillet
    )
    entities: list[Entity] = []
    for tooth in range(TEETH):
        angle = tooth * PITCH
        entities.append(Arc.around(ORIGIN, tip - offset, angle - tip_angle, angle + tip_angle))
        for side in (1, -1):
            tip_center = polar(tip_fillet_center, angle + side * tip_angle)
            root_center = polar(root_fillet_center, angle + side * root_angle)
            between = tangent_point(tip_center, tip_fillet - offset, root_center)
            entities.append(Arc.between(tip_center, polar(tip - offset, angle + side * tip_angle), between))
            entities.append(Arc.between(root_center, between, polar(root - offset, angle + side * root_angle)))
        entities.append(
            Arc.around(ORIGIN, root - offset, angle + root_angle, angle + PITCH - root_angle)
        )
    return entities


def max_spline_outside() -> list[Entity]:
    # A short line from the root circle to one root fillet, captured with the original sketch
    # (probably construction geometry). It splits that fillet, but removing it would renumber
    # every entity after it (see Profile).
    stub = Line(polar(14.275 * MM, 75), polar(14.275 * MM + 0.0504313012, 75))
    return [*max_spline(0), stub]


def involute(angle: float) -> float:
    """The involute function, tan(a) - a, of an angle in radians."""
    return math.tan(angle) - angle


def internal_spline(
    teeth: int,
    module: float,
    pressure_angle: float,
    major_diameter: float,
    minor_diameter: float,
    space_angle: float = 0,
) -> list[Entity]:
    """The hole an involute spline shaft fits in (an ISO 4156 internal spline, with flat roots and sharp corners).

    Lengths are in millimeters; the result is in inches.

    Args:
        pressure_angle: In degrees.
        space_angle: The angle of the center of one of the spaces the shaft's teeth fit in.
    """
    pitch_radius = module * teeth / 2
    base_radius = pitch_radius * math.cos(math.radians(pressure_angle))
    # The basic space width at the pitch circle is half the circular pitch
    pitch_half_angle = (math.pi * module / 2) / (2 * pitch_radius)
    inner, outer = minor_diameter / 2, major_diameter / 2

    def half_angle(radius: float) -> float:
        """Half the angle a space spans at a radius, in degrees."""
        return math.degrees(
            pitch_half_angle
            + involute(math.radians(pressure_angle))
            - involute(math.acos(base_radius / radius))
        )

    def flank(center: float, side: int) -> FitSpline:
        radii = [inner + (outer - inner) * i / 8 for i in range(9)]
        return FitSpline(tuple(polar(r * MM, center + side * half_angle(r)) for r in radii))

    entities: list[Entity] = []
    for space in range(teeth):
        center = space_angle + space * 360 / teeth
        before, after = flank(center, -1), flank(center, 1)
        next_center = center + 360 / teeth
        entities += [
            before,
            Arc.around(ORIGIN, outer * MM, center - half_angle(outer), center + half_angle(outer)),
            FitSpline(after.points[::-1]),
            Arc.around(ORIGIN, inner * MM, center + half_angle(inner), next_center - half_angle(inner)),
        ]
    # Join the arcs to the flanks exactly, so the profile closes
    for index in range(1, len(entities), 2):
        arc = entities[index]
        assert isinstance(arc, Arc)
        entities[index] = Arc(entities[index - 1].end, arc.mid, entities[(index + 1) % len(entities)].start)
    return entities


def spline_xs_hole() -> list[Entity]:
    """SplineXS: a 15 tooth, 0.5 module, 30 degree ISO 4156 spline (e.g. the Kraken X60's shaft).

    ISO 4156 flat root internal spline: major diameter m(z + 1.5), minor diameter m(z - 1). A space
    is centered on the x axis, matching TTB-0356 (vendor/), where it lines up with one of the
    print adapter's reliefs. That also makes the profile symmetric about the x axis, so mirroring
    the sketch (e.g. when a feature is flipped) doesn't change it.
    """
    teeth, module = 15, 0.5
    return internal_spline(teeth, module, 30, module * (teeth + 1.5), module * (teeth - 1))


# The order the profiles were originally captured in (see Profile)
SPLINE_XL_OUTSIDE_ORDER = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 0, 1, 2, 18, 23, 22, 21, 20, 19]
SPLINE_XL_INSIDE_ORDER = [7, 8, 19, 3, 4, 5, 18, 0, 1, 2, 23, 15, 16, 17, 22, 12, 13, 14, 21, 9, 10, 11, 20, 6]
MAX_SPLINE_OUTSIDE_ORDER = [8, 36, 7, 6, 9, 10, 11, 16, 14, 13, 15, 17, 12, 22, 20, 19, 21, 23, 18, 28, 26, 25, 27, 29, 24, 34, 32, 31, 33, 35, 30, 4, 2, 1, 3, 5, 0]
MAX_SPLINE_INSIDE_ORDER = [29, 26, 25, 24, 27, 28, 23, 20, 19, 18, 21, 22, 17, 14, 13, 12, 15, 16, 11, 8, 7, 6, 9, 10, 5, 2, 1, 0, 3, 4, 35, 32, 31, 30, 33, 34]

CONTENTS = [
    Import("core/sketchData.fs"),
    Import("core/profileSide.fs"),
    SketchMap(
        "SPLINE_XL",
        {
            "ProfileSide.OUTSIDE": Profile(spline_xl(0), SPLINE_XL_OUTSIDE_ORDER),
            "ProfileSide.INSIDE": Profile(spline_xl(TUBE_WALL), SPLINE_XL_INSIDE_ORDER),
        },
    ),
    SketchMap(
        "MAX_SPLINE",
        {
            "ProfileSide.OUTSIDE": Profile(max_spline_outside(), MAX_SPLINE_OUTSIDE_ORDER),
            "ProfileSide.INSIDE": Profile(max_spline(TUBE_WALL), MAX_SPLINE_INSIDE_ORDER),
        },
    ),
    # The outside of a MAXSpline without the stub line, for features with no released versions
    # to keep compatible with (e.g. MAXTube holes)
    Sketch("MAX_SPLINE_HOLE", Profile(max_spline(0))),
    # The hole for a SplineXS shaft, e.g. a SplineXS bore through a print
    Sketch("SPLINE_XS_HOLE", Profile(spline_xs_hole())),
]
