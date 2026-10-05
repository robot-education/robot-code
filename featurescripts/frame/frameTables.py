"""Tube and channel lookup tables for robotFrame. Run `uv run fs gen` after editing.

Each entry is a profile `width` (along X) by `height` (along Y), with walls `wallX` thick on its sides facing X and
`wallY` thick on its sides facing Y; `open` channels have no wall on +Y. Its holes are rows along its length:
`xRows` go through the walls facing X (each `offset` along Y from the profile's middle), and `yRows` through the walls
facing Y (offset along X). A row's first hole is `start` from the end, and its holes are `pitch` apart; `slot` makes
them slots that long (between their ends' centers) along the tube.

`tieStart` and `tieUnit` say which holes count for tying holes to the end (see linearStock.fs): the first one, and how
far apart they are. `stock` lists the lengths each is sold in, shortest first (see nutStripTables.py).
"""

import math

from fs_cli.tables import Node, Table, Value, inch, mm, string

# Placeholder appearances until vendor colors are picked: robotProperties.fs's WHITE and BLACK
WHITE = "color(230 / 255, 230 / 255, 230 / 255)"
BLACK = "color(0.3, 0.3, 0.3)"


def row(offset: str, start: str, pitch: str, diameter: str, slot: str | None = None) -> str:
    values = {"offset": offset, "start": start, "pitch": pitch, "diameter": diameter}
    if slot is not None:
        values["slot"] = slot
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in values.items()) + " }"


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
            "xRows": array(x_rows),
            "yRows": array(y_rows),
            "tieStart": tie_start,
            "tieUnit": tie_unit,
            "stock": lengths,
        },
    )


def grid(face: float, pitch: float = 0.5, diameter: float = 0.196, start: float = 0.5) -> list[str]:
    """Rows of holes `pitch` apart across a face `face` wide, centered, as on most FRC tube."""
    count = math.floor((face / 2 - pitch / 2) / pitch + 1e-9)
    return [row(inch(k * pitch), inch(start), inch(pitch), inch(diameter)) for k in range(-count, count + 1)]


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
    wall_name: str | None = None,
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
        grid(height, diameter=diameter) if x_holes else [],
        grid(width, diameter=diameter) if y_holes else [],
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


def gobilda_rows(center: float = 0) -> list[str]:
    diagonal = 12 / math.sqrt(2)
    diamond = 8 * math.sqrt(2)
    rows = [
        row(mm(center), mm(24), mm(24), mm(14)),
        row(mm(center), mm(12), mm(24), mm(4), slot=mm(round(24 - 2 * diamond, 4))),
    ]
    for side in (-1, 1):
        rows += [
            row(mm(center + side * 16), mm(8), mm(8), mm(4)),
            row(mm(center + side * 8), mm(8), mm(24), mm(4)),
            row(mm(center + side * 8), mm(16), mm(24), mm(4)),
            row(mm(round(center + side * diagonal, 4)), mm(round(diagonal, 4)), mm(24), mm(4)),
            row(mm(round(center + side * diagonal, 4)), mm(round(24 - diagonal, 4)), mm(24), mm(4)),
            row(mm(round(center + side * diamond, 4)), mm(24), mm(24), mm(4)),
        ]
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
                    # The sides have one row of holes, 8 mm from the base's outside
                    [row(mm(2), mm(8), mm(8), mm(4))],
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
