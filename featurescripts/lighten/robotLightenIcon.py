"""Robot lighten's icon: the plate icon (../plate/Plate_v2.svg) lightened. A hole in each corner, centered on the
corner's round, and one pocket between them, leaving a wall of one thickness along the edges and around the holes,
rounded where they meet. `fs gen` writes it to robotLightenIcon.svg (see docs/icons.md).
"""

from fs_cli.icons import (
    OUTLINE_WIDTH,
    Icon,
    circle,
    offset,
    round_corners,
    rounded_rectangle,
)

X0, X1 = 3.0, 19.0  # the plate's face
CORNER = 3.4  # its corners' radius, which the holes are centered on
DEPTH = 2.2  # its thickness, going back
WALL = 0.85
FILLET = 1.4  # where the walls around the holes meet the walls along the edges

face = rounded_rectangle(X0, X0, X1, X1, CORNER)
inside = offset(face, -OUTLINE_WIDTH / 2)
# The holes, as big as a wall between them and the face's edge allows
radius = CORNER - OUTLINE_WIDTH / 2 - WALL
holes = [circle((x, y), radius) for x in (X0 + CORNER, X1 - CORNER) for y in (X0 + CORNER, X1 - CORNER)]
pocket = offset(inside, -WALL)
for hole in holes:
    pocket = pocket.difference(offset(hole, WALL))

ICON = Icon().extrusion(face, DEPTH).through(round_corners(pocket, FILLET))
for hole in holes:
    ICON.through(hole, depth=0.6)
