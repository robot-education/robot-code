"""Quick rib's icon: the plate of Robot lighten's icon (../lighten/robotLightenIcon.py), with a hole in each corner and
one in its middle, and ribs (in Onshape's rib color, as its Rib icon has) from the middle one to the corners': a hub
and spokes. `fs gen` writes it to quickRibIcon.svg (see docs/icons.md).
"""

from shapely.geometry import LineString
from shapely.ops import unary_union

from fs_cli.icons import FACE, Icon, circle, rounded_rectangle

X0, X1 = 3.0, 19.0  # the plate's face
CORNER = 3.4  # its corners' radius, which the corner holes are centered on
DEPTH = 2.2  # its thickness, going back
HOLE = 1.3
RIB = 2.0  # the ribs' width
RIB_OUTLINE = 0.6

face = rounded_rectangle(X0, X0, X1, X1, CORNER)
middle = ((X0 + X1) / 2, (X0 + X1) / 2)
corners = [(x, y) for x in (X0 + CORNER, X1 - CORNER) for y in (X0 + CORNER, X1 - CORNER)]
ribs = unary_union([LineString([middle, corner]).buffer(RIB / 2, cap_style=2) for corner in corners])

ICON = Icon().extrusion(face, DEPTH).outlined(ribs, FACE, width=RIB_OUTLINE)
for hole in [middle, *corners]:
    ICON.through(circle(hole, HOLE), depth=0.6)
