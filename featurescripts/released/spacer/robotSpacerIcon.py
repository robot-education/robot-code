"""Robot spacer's icon: a thin round spacer with a hex bore all the way through it, its corners drawn along its wall.
`fs gen` writes it to robotSpacerIcon.svg (see docs/icons.md).
"""

import math

from shapely.geometry import Polygon

from fs_cli.icons import Icon, circle

RADIUS = 7.8
THICKNESS = 1.5
FLATS = 9.4  # across its bore's flats
CENTER = 20 - RADIUS - 1.0  # its face's center, as low and right as its outline allows


def hexagon(center: tuple[float, float], across_flats: float) -> Polygon:
    radius = across_flats / math.sqrt(3)
    x, y = center
    return Polygon([(x + radius * math.cos(math.radians(60 * i)), y + radius * math.sin(math.radians(60 * i))) for i in range(6)])


center = (CENTER, CENTER)
ICON = Icon().extrusion(circle(center, RADIUS), THICKNESS).through(hexagon(center, FLATS), depth=THICKNESS, see_through=True, corners=0.35)
