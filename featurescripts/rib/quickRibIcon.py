"""Quick rib's icon: a hub and spokes, as it sketches them: white circles (the holes it's given) joined by ribs, in
Onshape's rib color (as its Rib icon has), all in one outline. The circles are big and the outline thin, so they still
read at 20 px on Onshape's dark theme. `fs gen` writes it to quickRibIcon.svg (see docs/icons.md).
"""

from shapely.geometry import LineString
from shapely.ops import unary_union

from fs_cli.icons import FACE, OUTLINE, WHITE, Icon, circle, offset

HUB = (10.0, 10.0)
SPOKES = [(4.0, 4.0), (16.0, 4.0), (4.0, 16.0), (16.0, 16.0)]
HUB_RADIUS = 3.6
SPOKE_RADIUS = 3.0
RIB = 3.2  # the ribs' width
LINE = 0.8  # outlines: thinner than Onshape's usual 1.3, which fills the circles in at 20 px

ribs = unary_union([LineString([HUB, spoke]).buffer(RIB / 2, cap_style=2) for spoke in SPOKES])
discs = [circle(HUB, HUB_RADIUS), *(circle(spoke, SPOKE_RADIUS) for spoke in SPOKES)]

ICON = Icon().fill(offset(unary_union([ribs, *discs]), LINE / 2), OUTLINE).fill(offset(ribs, -LINE / 2), FACE)
for disc in discs:
    ICON.fill(offset(disc, -LINE / 2), OUTLINE).fill(offset(disc, -LINE), WHITE)
