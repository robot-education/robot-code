"""Robot shaft's icon: a long rounded hex shaft (like REX), going back from bottom right to top left, with a tapped
hole in its end. `fs gen` writes it to robotShaftIcon.svg (see docs/icons.md).
"""

import math

from shapely.geometry import Polygon

from fs_cli.icons import Icon, circle

FLATS = 7.0  # across its hex's flats
DIAMETER = FLATS * 1.12  # its rounding's diameter
CENTER = 19.5 - DIAMETER / 2 - 0.65  # its end's center, as low and right as its outline allows
DEPTH = 18.7 - DIAMETER - 0.3  # its length, going back: as long as fits


def hexagon(center: tuple[float, float], across_flats: float) -> Polygon:
    radius = across_flats / math.sqrt(3)
    x, y = center
    return Polygon([(x + radius * math.cos(math.radians(60 * i)), y + radius * math.sin(math.radians(60 * i))) for i in range(6)])


end = hexagon((CENTER, CENTER), FLATS).intersection(circle((CENTER, CENTER), DIAMETER / 2))
ICON = Icon().extrusion(end, DEPTH).through(circle((CENTER, CENTER), FLATS * 0.22), depth=0.6)
