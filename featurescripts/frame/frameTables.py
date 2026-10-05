"""Tube and channel lookup tables for robotFrame. Run `uv run fs gen` after editing.

Each entry is a profile `width` (along X) by `height` (along Y), with walls `wallX` thick on its sides facing X and
`wallY` thick on its sides facing Y; `open` channels have no wall on +Y. `holeDiameter` is the diameter of its
ordinary holes.

Its holes are rows along its length: `xRows` go through the walls facing X, and `yRows` through the walls facing Y. A
row repeats a group of `shapes` every `pitch`, starting `start` from the end; each shape is `along` the tube and
`offset` across the face (along Y for `xRows`, X for `yRows`) from the row's position on the face's middle. Shapes are
ordinary holes, or `slot`s that long (between their ends' centers) along the tube, unless they have a `diameter` of
their own. Each row is cut once and face patterned (see robotFrame.fs), so its shapes mustn't overlap other rows'.

`tieStart` and `tieUnit` say which holes count for tying holes to the end (see linearStock.fs): the first one, and how
far apart they are. `stock` lists the lengths each is sold in, shortest first (see nutStripTables.py).
"""

import math

from fs_cli.tables import Node, Table, Value, inch, mm, string

# Placeholder appearances until vendor colors are picked: robotProperties.fs's WHITE and BLACK
WHITE = "color(230 / 255, 230 / 255, 230 / 255)"
BLACK = "color(0.3, 0.3, 0.3)"


def fs_map(values: dict[str, str]) -> str:
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in values.items()) + " }"


def shape(offset: str, along: str | None = None, diameter: str | None = None, slot: str | None = None) -> str:
    values = {"along": along or "0 * meter", "offset": offset}
    if diameter is not None:
        values["diameter"] = diameter
    if slot is not None:
        values["slot"] = slot
    return fs_map(values)


def row(start: str, pitch: str, shapes: list[str]) -> str:
    return fs_map({"start": start, "pitch": pitch, "shapes": array(shapes)})


def array(items: list[str]) -> str:
    return "[" + ", ".join(items) + "]"


def stock(*lengths: tuple[str, str, str]) -> str:
    """Lengths it's sold in: (length, part number, url)."""
    return array(
        [
            f"{{ {string('length')} : {length}, {string('partNumber')} : {string(part_number)}, {string('url')} : {string(url)} }}"
            for length, part_number, url in lengths
        ]
    )


def tube(
    name: str,
    description: str,
    width: float,
    height: float,
    wall_x: float,
    wall_y: float,
    hole_diameter: float,
    x_rows: list[str],
    y_rows: list[str],
    tie_start: str,
    tie_unit: str,
    lengths: str,
    unit=inch,
    open: bool = False,
) -> Value:
    """A tube (or channel), with `description` for its part name, e.g. `2x1 Tube (WCP, 1/16 in. wall)`."""
    return Value(
        name,
        {
            "partName": string(description),
            "width": unit(width),
            "height": unit(height),
            "wallX": unit(wall_x),
            "wallY": unit(wall_y),
            "open": "true" if open else "false",
            "holeDiameter": unit(hole_diameter),
            "xRows": array(x_rows),
            "yRows": array(y_rows),
            "tieStart": tie_start,
            "tieUnit": tie_unit,
            "stock": lengths,
        },
    )


def grid(face: float, pitch: float = 0.5, start: float = 0.5) -> list[str]:
    """A row of holes `pitch` apart, in columns `pitch` apart centered across a face `face` wide, as on most FRC
    tube."""
    count = math.floor((face / 2 - pitch / 2) / pitch + 1e-9)
    return [row(inch(start), inch(pitch), [shape(inch(k * pitch)) for k in range(-count, count + 1)])]


def vendor(name: str, url: str, appearance: str, sizes: list[Value]) -> Value:
    return Value(name, {"vendor": string(name), "url": string(url), "appearance": appearance}, Node("size", sizes))


def size(name: str, variants: list[Value]) -> Value:
    return Value(name, next=Node("variant", variants, display_name="Type"))


# FRC tube: #10 clearance holes 1/2 in. apart, starting 1/2 in. from the end; one row on a 1 in. face, three on a 2 in.
# face (WCP's and REV's drawings)


def frc_tube(
    vendor_name: str,
    name: str,
    width: float,
    height: float,
    wall_x: float,
    wall_y: float,
    lengths: str,
    x_holes: bool = True,
    y_holes: bool = True,
    diameter: float = 0.196,
) -> Value:
    """A tube with FRC's usual grid of holes, through the walls facing X and/or Y."""
    size = f"{width:g}x{height:g}"
    return tube(
        name,
        f"{size} Tube ({vendor_name}, {name[:1].lower() + name[1:]})",
        width,
        height,
        wall_x,
        wall_y,
        diameter,
        grid(height) if x_holes else [],
        grid(width) if y_holes else [],
        inch(0.5),
        inch(0.5),
        lengths,
    )


def wcp(part_number: str) -> str:
    return f"https://wcproducts.com/products/{part_number.lower()}"


def wcp_tube(part_number: str, *args, **kwargs) -> Value:
    return frc_tube("WCP", *args, lengths=stock((inch(47), part_number, wcp(part_number))), **kwargs)


# https://wcproducts.com/products/punched-tubing (drawing: Web-Rectangle Punched Tubing.pdf); raw aluminum
WCP = vendor(
    "WCP",
    "https://wcproducts.com/products/punched-tubing",
    WHITE,
    [
        size(
            "1x1",
            [
                wcp_tube("WCP-0924", "1/16 in. wall", 1, 1, 0.0625, 0.0625),
                wcp_tube("WCP-1023", "1/8 in. wall", 1, 1, 0.125, 0.125),
            ],
        ),
        size(
            "2x1",
            [
                wcp_tube("WCP-0895", "1/16 in. wall", 2, 1, 0.0625, 0.0625),
                wcp_tube("WCP-0894", "1/16 in. wall, 1 in. sides only", 2, 1, 0.0625, 0.0625, y_holes=False),
                wcp_tube("WCP-1428", "3/32 in. wall", 2, 1, 0.09375, 0.09375),
                wcp_tube("WCP-1427", "3/32 in. wall, 1 in. sides only", 2, 1, 0.09375, 0.09375, y_holes=False),
                wcp_tube("WCP-1025", "1/8 in. wall", 2, 1, 0.125, 0.125),
                wcp_tube("WCP-1024", "1/8 in. wall, 1 in. sides only", 2, 1, 0.125, 0.125, y_holes=False),
            ],
        ),
        size("2x2", [wcp_tube("WCP-0926", "1/16 in. wall", 2, 2, 0.0625, 0.0625)]),
    ],
)


REV_1X1_URL = "https://www.revrobotics.com/MAXTube-1x1/"
REV_2X1_URL = "https://www.revrobotics.com/MAXTube-2x1"


def rev_tube(part_number: str, url: str, *args, **kwargs) -> Value:
    return frc_tube("REV", *args, lengths=stock((inch(47), part_number, url)), diameter=5 / 25.4, **kwargs)


# https://www.revrobotics.com/MAXTube (drawings: REV-21-xxxx-DR.pdf); clear anodized
# TODO: the 1 mm wall MAXTube profiles have #10 nut grooves, which these leave out; and MAX Pattern tube
REV = vendor(
    "REV",
    "https://www.revrobotics.com/MAXTube",
    WHITE,
    [
        size(
            "1x1",
            [
                rev_tube("REV-21-3540", REV_1X1_URL, "Grid, 1/16 in. wall", 1, 1, 0.0625, 0.0625),
                rev_tube("REV-21-3543", REV_1X1_URL, "Grid, 1/8 in. wall", 1, 1, 0.125, 0.125),
                rev_tube("REV-21-2160", REV_1X1_URL, "Grid, 1 mm wall", 1, 1, 1 / 25.4, 1 / 25.4),
            ],
        ),
        size(
            "2x1",
            [
                rev_tube("REV-21-3552", REV_2X1_URL, "Grid, 1/16 in. wall", 2, 1, 0.0625, 0.0625),
                rev_tube("REV-21-3555", REV_2X1_URL, "Grid, 1/8 in. wall", 2, 1, 0.125, 0.125),
                rev_tube("REV-21-3586", REV_2X1_URL, "Grid, 3/16 in. wall", 2, 1, 0.1875, 0.1875),
                rev_tube("REV-21-2289", REV_2X1_URL, "Light grid, 1 mm wall", 2, 1, 1 / 25.4, 1 / 25.4),
                rev_tube("REV-21-2161", REV_2X1_URL, "Light, 1 mm wall", 2, 1, 1 / 25.4, 1 / 25.4, y_holes=False),
                # 1/8 in. on the 2 in. sides, 1 mm on the 1 in. sides
                rev_tube("REV-21-2162", REV_2X1_URL, "Standard", 2, 1, 1 / 25.4, 0.125, y_holes=False),
            ],
        ),
    ],
)


SWYFT_URL = "https://swyftrobotics.com/products/swyft-super-tube"


def swyft_tube(sku: str, *args, **kwargs) -> Value:
    return frc_tube("Swyft", *args, lengths=stock((inch(47), sku, SWYFT_URL)), **kwargs)


# https://swyftrobotics.com/products/swyft-super-tube; black anodized
# TODO: Swyft's drawings (their CAD is only on Google Drive) to check that their grid is like WCP's, and Grid+Bearing
SWYFT = vendor(
    "Swyft",
    SWYFT_URL,
    BLACK,
    [
        size(
            "1x1",
            [
                swyft_tube("SR-TUBE-0625-GRID-1X1-47", "Grid, 1/16 in. wall", 1, 1, 0.0625, 0.0625),
                swyft_tube("SR-TUBE-125-GRID-1X1-47", "Grid, 1/8 in. wall", 1, 1, 0.125, 0.125),
            ],
        ),
        size(
            "2x1",
            [
                swyft_tube("SR-TUBE-035-GRID-2X1-47", "Grid, 0.035 in. wall", 2, 1, 0.035, 0.035),
                swyft_tube("SR-TUBE-0625-GRID-2X1-47", "Grid, 1/16 in. wall", 2, 1, 0.0625, 0.0625),
                swyft_tube("SR-TUBE-125-GRID-2X1-47", "Grid, 1/8 in. wall", 2, 1, 0.125, 0.125),
                swyft_tube("SR-TUBE-035-PLAIN-2X1-47", "Plain, 0.035 in. wall", 2, 1, 0.035, 0.035, x_holes=False, y_holes=False),
                swyft_tube("SR-TUBE-0625-PLAIN-2X1-47", "Plain, 1/16 in. wall", 2, 1, 0.0625, 0.0625, x_holes=False, y_holes=False),
                swyft_tube("SR-TUBE-125-PLAIN-2X1-47", "Plain, 1/8 in. wall", 2, 1, 0.125, 0.125, x_holes=False, y_holes=False),
            ],
        ),
    ],
)


# goBILDA's pattern, on each 48 mm face (from goBILDA's STEP files): a 14 mm bore every 24 mm starting 24 mm from the
# end; 4 mm holes on an 8 mm grid around them; 4 mm holes at 45 degrees on a 24 mm circle around each bore, and across
# each bore on a 16 mm diamond; and short slots between bores, where neighboring bores' diamonds meet.


def gobilda_rows() -> list[str]:
    """goBILDA's pattern on a 48 mm face, as rows which don't overlap: holes on the 24 mm circle overlap the grid holes
    next to them, so they share their rows."""
    diagonal = 12 / math.sqrt(2)
    diamond = 8 * math.sqrt(2)
    lean = round(diagonal - 8, 4)
    rows = [
        # Bores, and the holes across them on the diamond
        row(mm(24), mm(24), [shape(mm(0), diameter=mm(14)), shape(mm(round(diamond, 4))), shape(mm(round(-diamond, 4)))]),
        # Slots between bores, where neighboring bores' diamonds meet
        row(mm(12), mm(24), [shape(mm(0), slot=mm(round(24 - 2 * diamond, 4)))]),
        # The outer columns of the grid
        row(mm(8), mm(8), [shape(mm(16)), shape(mm(-16))]),
    ]
    # The inner columns of the grid, either side of each bore, with the holes on the circle beside them
    for start, direction in ((8, 1), (16, -1)):
        shapes = []
        for side in (-1, 1):
            shapes += [shape(mm(side * 8)), shape(mm(round(side * diagonal, 4)), along=mm(direction * lean))]
        rows.append(row(mm(start), mm(24), shapes))
    return rows


def gobilda_stock(series: str, holes: list[int], url: str) -> str:
    """goBILDA channel is 48 mm long with 1 hole, and 24 mm longer for each hole after."""
    return stock(*[(mm(48 + 24 * (n - 1)), f"{series}-{n:04d}-{48 + 24 * (n - 1):04d}", url) for n in holes])


GOBILDA_HOLES = [*range(1, 19), 21, 25, 29, 33, 37, 41, 45, 49]
U_CHANNEL_URL = "https://www.gobilda.com/1120-series-u-channel/"
LOW_SIDE_URL = "https://www.gobilda.com/1121-series-low-side-u-channel/"

# https://www.gobilda.com/structure/ (schematics and STEP files on each product page)
# TODO: goBILDA's appearance
GOBILDA = vendor(
    "goBILDA",
    "https://www.gobilda.com/channel/",
    WHITE,
    [
        size(
            "U-Channel",
            [
                tube(
                    "1120 Series",
                    "U-Channel (goBILDA 1120 Series)",
                    48,
                    48,
                    2.5,
                    2.5,
                    4,
                    gobilda_rows(),
                    gobilda_rows(),
                    mm(24),
                    mm(24),
                    gobilda_stock("1120", GOBILDA_HOLES, U_CHANNEL_URL),
                    unit=mm,
                    open=True,
                )
            ],
        ),
        size(
            "Low-Side U-Channel",
            [
                tube(
                    "1121 Series",
                    "Low-Side U-Channel (goBILDA 1121 Series)",
                    48,
                    12,
                    2.5,
                    2.5,
                    4,
                    # The sides have one row of holes, 8 mm from the base's outside
                    [row(mm(8), mm(8), [shape(mm(2))])],
                    gobilda_rows(),
                    mm(24),
                    mm(24),
                    gobilda_stock("1121", GOBILDA_HOLES, LOW_SIDE_URL),
                    unit=mm,
                    open=True,
                )
            ],
        ),
    ],
)

CONTENTS = [
    Table("frcFrameTable", Node("vendor", [WCP, REV, SWYFT])),
    Table("ftcFrameTable", Node("vendor", [GOBILDA])),
]
