"""Robot gear's icon: a thin spur gear of eight teeth with a hex bore all the way through it, as Robot spacer's is. `fs gen`
writes it to robotGearIcon.svg (see docs/icons.md).
"""

import math

from shapely.geometry import Polygon
from shapely.ops import unary_union

from fs_cli.icons import Icon, circle, round_corners

TIP = 8.0  # its tips' radius
ROOT = 5.6  # its roots'
TEETH = 8
THICKNESS = 1.6
FLATS = 6.6  # across its bore's flats
CENTER = 20 - TIP - 0.8  # its face's center, as low and right as its outline allows


def hexagon(center: tuple[float, float], across_flats: float) -> Polygon:
    radius = across_flats / math.sqrt(3)
    x, y = center
    return Polygon([(x + radius * math.cos(math.radians(60 * i)), y + radius * math.sin(math.radians(60 * i))) for i in range(6)])


def gear(center: tuple[float, float]) -> Polygon:
    """Trapezoid teeth on its root circle, each 0.64 of the pitch wide at its root and 0.4 at its tip, slightly rounded."""
    x, y = center
    pitch = 2 * math.pi / TEETH
    points = []
    for tooth in range(TEETH):
        middle = (tooth + 0.5) * pitch
        for fraction, radius in ((-0.32, ROOT), (-0.2, TIP), (0.2, TIP), (0.32, ROOT)):
            angle = middle + fraction * pitch
            points.append((x + radius * math.cos(angle), y + radius * math.sin(angle)))
    return round_corners(unary_union([Polygon(points), circle(center, ROOT)]), 0.25)


center = (CENTER, CENTER)
ICON = Icon().extrusion(gear(center), THICKNESS).through(hexagon(center, FLATS), depth=THICKNESS, see_through=True, corners=0.35)
