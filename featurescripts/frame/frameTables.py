"""Frame lookup tables for robotFrame: tube, channel, angle, and extrusion. Run `uv run fs gen` after editing.

Each vendor's frames are chosen by size, then (where a size comes in more than one) by hole pattern, then by wall
thickness. A choice with only one option is kept when it says something (like the wall thickness), and left out when
it wouldn't (like goBILDA's series), so tables vary in depth. Options follow FRCDesign's (see docs/cots-research.md),
checked against the vendors' drawings and CAD (in vendor/).

Each entry is a profile `width` (along X) by `height` (along Y), with walls `wallX` thick on its sides facing X and
`wallY` thick on its sides facing Y; `open` channels have no wall on +Y, `angle` has only the walls facing -X and
-Y, and beams with a `bore` are solid around a round hole that diameter instead. Anything else (like T-slot
extrusion) has a `profile`: a SketchDataArray of its cross section, centered on the origin. `holeDiameter` is the
diameter of its ordinary holes.

Its holes are rows along its length: `xRows` go through the walls facing X, and `yRows` through the walls facing Y. A
row repeats a group of `shapes` every `pitch`, starting `start` from the end; each shape is `along` the frame and
`offset` across the face (along Y for `xRows`, X for `yRows`) from the row's position on the face's middle. Shapes are
ordinary holes, or `slot`s that long (between their ends' centers) along the frame, unless they have a `diameter` of
their own, or are MAXSpline cutouts (`maxSpline`). Each entry is a `Stock`, and is built by linearStock.fs's `buildStock`.

`tieStart` and `tieUnit` say which holes count for tying holes to the end (see linearStock.fs): the first one, and how
far apart they are. `stock` lists the lengths each is sold in, shortest first (see nutStripTables.py).
"""

import math
import pathlib

from fs_cli.sketches import Line, Point, Profile, Sketch, rotated, translated
from fs_cli.step import StepFile
from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, inch, mm, string

VENDOR = pathlib.Path(__file__).parent / "vendor"

# Placeholder appearances until vendor colors are picked: robotProperties.fs's WHITE and BLACK
WHITE = "color(230 / 255, 230 / 255, 230 / 255)"
BLACK = "color(0.3, 0.3, 0.3)"


def fs_map(values: dict[str, str]) -> str:
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in values.items()) + " }"


def shape(
    offset: str, along: str | None = None, diameter: str | None = None, slot: str | None = None, max_spline: bool = False
) -> str:
    values = {"along": along or "0 * meter", "offset": offset}
    if diameter is not None:
        values["diameter"] = diameter
    if slot is not None:
        values["slot"] = slot
    if max_spline:
        values["maxSpline"] = "true"
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
    bore: float | None = None,
    angle: bool = False,
    profile: str | None = None,
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
            **({"bore": unit(bore)} if bore is not None else {}),
            **({"angle": "true"} if angle else {}),
            **({"profile": profile} if profile is not None else {}),
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


def vendor(name: str, url: str, appearance: str | None, sizes: list[Value]) -> Value:
    """A vendor's frames, which all have `appearance`, unless it's None (as values closer to the root win)."""
    values = {"vendor": string(name), "url": string(url)}
    if appearance is not None:
        values["appearance"] = appearance
    return Value(name, values, Node("size", sizes))


# The choices after size, by the key they're stored under
LEVELS = {"pattern": "Pattern", "wall": "Wall", "finish": "Finish", "variant": "Type"}


def size(name: str, entries: list[Value], appearance: str | None = None, level: str = "wall") -> Value:
    """A size of a vendor's frames, with a choice between `entries`: of wall thickness, unless `level` says otherwise."""
    values = {"appearance": appearance} if appearance is not None else {}
    return Value(name, values, Node(level, entries, display_name=LEVELS[level]))


def pattern(name: str, walls: list[Value]) -> Value:
    """A hole pattern a size comes in, with a choice of wall thickness."""
    return Value(name, {}, Node("wall", walls, display_name="Wall"))


def only(name: str, frame: Value, appearance: str | None = None) -> Value:
    """A choice that's a frame itself, for a size which comes in only one kind, when choosing that one would say
    nothing (like goBILDA's series). Where the only option says something (a wall thickness, an angle's size), it's
    kept as a choice."""
    values = dict(frame.values)
    if appearance is not None:
        values["appearance"] = appearance
    return Value(name, values)


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
    detail: str | None = None,
    x_holes: bool = True,
    y_holes: bool = True,
    diameter: float = 0.196,
    x_rows: list[str] | None = None,
    y_rows: list[str] | None = None,
    tie_start: float = 0.5,
    tie_unit: float = 0.5,
    profile: str | None = None,
) -> Value:
    """A tube with FRC's usual grid of holes, through the walls facing X and/or Y (or `x_rows` and `y_rows`). It's
    `name` in the table, a wall thickness like "1/16 in.", and is described with `detail` (by default, its wall)."""
    size = f"{fraction(width)}x{fraction(height)}"
    return tube(
        name,
        f"{size} Tube ({vendor_name}, {detail or name + ' wall'})",
        width,
        height,
        wall_x,
        wall_y,
        diameter,
        x_rows if x_rows is not None else grid(height) if x_holes else [],
        y_rows if y_rows is not None else grid(width) if y_holes else [],
        inch(tie_start),
        inch(tie_unit),
        lengths,
        profile=profile,
    )


def fraction(value: float) -> str:
    return {0.5: "1/2"}.get(value, f"{value:g}")


def wcp(part_number: str) -> str:
    return f"https://wcproducts.com/products/{part_number.lower()}"


def wcp_tube(part_number: str, wall: str, width: float, height: float, thickness: float, sides_only: bool = False) -> Value:
    """WCP's tube with a `wall` (its name in the table) `thickness` thick, with holes on every side, or (`sides_only`)
    only through its 1 in. sides."""
    return frc_tube(
        "WCP",
        wall,
        width,
        height,
        thickness,
        thickness,
        stock((inch(47), part_number, wcp(part_number))),
        detail=f"{wall} wall" + (", 1 in. sides only" if sides_only else ""),
        y_holes=not sides_only,
    )


WCP_BENT_WALL = 2.5 / 25.4


def wcp_bent(part_number: str, name: str, kind: str, width: float, height: float, **kwargs) -> Value:
    """WCP's angle and C-channel, with holes on its usual grid (see grid) on each face."""
    return tube(
        name,
        f"{name} {kind} (WCP, 2.5 mm wall)",
        width,
        height,
        WCP_BENT_WALL,
        WCP_BENT_WALL,
        0.196,
        grid(height),
        grid(width),
        inch(0.5),
        inch(0.5),
        stock((inch(47), part_number, wcp(part_number))),
        **kwargs,
    )


# https://wcproducts.com/products/punched-tubing (drawing: Web-Rectangle Punched Tubing.pdf); raw aluminum
WCP = vendor(
    "WCP",
    "https://wcproducts.com/products/punched-tubing",
    WHITE,
    [
        size(
            "1x1",
            [
                wcp_tube("WCP-0924", "1/16 in.", 1, 1, 0.0625),
                # Sold as 0.093 in.
                wcp_tube("WCP-1586", "3/32 in.", 1, 1, 0.093),
                wcp_tube("WCP-1023", "1/8 in.", 1, 1, 0.125),
            ],
        ),
        size(
            "2x1",
            [
                pattern(
                    "Full",
                    [
                        wcp_tube("WCP-0895", "1/16 in.", 2, 1, 0.0625),
                        wcp_tube("WCP-1428", "3/32 in.", 2, 1, 0.09375),
                        wcp_tube("WCP-1025", "1/8 in.", 2, 1, 0.125),
                    ],
                ),
                pattern(
                    "1 in. sides only",
                    [
                        wcp_tube("WCP-0894", "1/16 in.", 2, 1, 0.0625, sides_only=True),
                        wcp_tube("WCP-1427", "3/32 in.", 2, 1, 0.09375, sides_only=True),
                        wcp_tube("WCP-1024", "1/8 in.", 2, 1, 0.125, sides_only=True),
                    ],
                ),
            ],
            level="pattern",
        ),
        size(
            "2x2",
            [
                wcp_tube("WCP-0926", "1/16 in.", 2, 2, 0.0625),
                # Sold as 0.093 in.
                wcp_tube("WCP-1587", "3/32 in.", 2, 2, 0.093),
            ],
        ),
        # Bent from 2.5 mm sheet (FRCDesign has 0.090 in., but WCP's pages say 2.5 mm); their bends are left sharp
        # TODO: WCP's drawings, to check that their holes are on the usual grid, centered on each face
        size(
            "Angle",
            [
                wcp_bent("WCP-0929", "1x1", "Angle", 1, 1, angle=True),
                wcp_bent("WCP-0930", "2x2", "Angle", 2, 2, angle=True),
            ],
            level="variant",
        ),
        size(
            "C-Channel",
            [
                wcp_bent("WCP-0931", "1x1x1", "C-Channel", 1, 1, open=True),
                wcp_bent("WCP-0932", "1x2x1", "C-Channel", 2, 1, open=True),
            ],
            level="variant",
        ),
    ],
)


REV_HALF_URL = "https://www.revrobotics.com/MAXTube-0.5x0.5/"
REV_1X1_URL = "https://www.revrobotics.com/MAXTube-1x1/"
REV_2X1_URL = "https://www.revrobotics.com/MAXTube-2x1/"
REV_2X2_URL = "https://www.revrobotics.com/MAXTube-2x2/"
REV_MM = 1 / 25.4


def rev_tube(part_number: str, url: str, name: str, width: float, height: float, wall: float, **kwargs) -> Value:
    """MAXTube, 47 in. long, with 5 mm holes on FRC's usual grid (unless given other rows)."""
    return frc_tube(
        "REV", name, width, height, wall, wall, stock((inch(47), part_number, url)), diameter=5 * REV_MM, **kwargs
    )


def max_pattern() -> list[str]:
    """REV's MAX Pattern (drawings: MAXTube-2x1_MAX_Pattern-DR.pdf and MAXTube-2x1-0.125in_Wall-Max_Pattern-DR.pdf):
    on a 2 in. face, a MAXSpline cutout every 2 in., starting 1.5 in. from the end, with a column of 3 holes between
    each."""
    return [
        row(inch(1.5), inch(2), [shape(inch(0), max_spline=True)]),
        row(inch(0.5), inch(2), [shape(inch(k * 0.5)) for k in (-1, 0, 1)]),
    ]


def max_pattern_tube(name: str, detail: str, numbers: list[int], **kwargs) -> Value:
    """MAXTube 2x1 with MAX Pattern on its 2 in. sides, and the usual grid on its 1 in. sides. It's sold in odd
    lengths (3, 5, 7, 15, 23, 31, and 47 in.: `numbers`' part numbers), so the pattern ends the way it starts."""
    lengths = (3, 5, 7, 15, 23, 31, 47)
    return frc_tube(
        "REV",
        name,
        2,
        1,
        0.125,
        0.125,
        stock(*[(inch(length), f"REV-21-{number}", REV_2X1_URL) for length, number in zip(lengths, numbers)]),
        detail=detail,
        diameter=5 * REV_MM,
        y_rows=max_pattern(),
        tie_unit=2,
        **kwargs,
    )


def rev_angle() -> Value:
    """REV's angle (drawing: REV-21-3207-DR.pdf): 5 mm holes every 1/2 in. on each leg, 1/2 in. from the outside of the
    other, starting 1/2 in. from the end."""
    holes = [row(inch(0.5), inch(0.5), [shape(inch(0.5 - 0.74 / 2))])]
    return tube(
        "0.74x0.74",
        "0.74x0.74 Angle (REV)",
        0.74,
        0.74,
        0.125,
        0.125,
        5 / 25.4,
        holes,
        holes,
        inch(0.5),
        inch(0.5),
        stock((inch(47), "REV-21-3207", "https://www.revrobotics.com/rev-21-3207/")),
        angle=True,
    )


REV_1IN_URL = "https://www.revrobotics.com/1in-extrusion/"
REV_15MM_URL = "https://www.revrobotics.com/15mm-extrusions/"


def extrusion(
    name: str, description: str, width: float, height: float, profile: str, appearance: str, lengths: str, unit=inch
) -> Value:
    """T-slot extrusion, which has no holes: `profile` names its cross section (see PROFILES)."""
    value = tube(
        name,
        description,
        width,
        height,
        0,
        0,
        0.196 if unit is inch else 4,
        [],
        [],
        unit(0.5 if unit is inch else 15),
        unit(0.5 if unit is inch else 15),
        lengths,
        unit=unit,
        profile=profile,
    )
    value.values["appearance"] = appearance
    return value


def step_profile(file: str, offset: Point = Point(0, 0), degrees: float = 0, plain_outside: bool = False) -> Profile:
    """The cross section of a vendor's extrusion, with its bores and pockets, from its STEP file (some trimmed to just
    that with `fs step`), rotated then moved to center it on the origin with its width along X. A `plain_outside` is
    left a rectangle, as MAXTube's is but for the shallow grooves along it to drill on, which aren't worth modeling."""
    step = StepFile(VENDOR / file)
    entities = step.profile(holes=True)
    if plain_outside:
        outside = len(step.profile())
        points = [point for entity in entities[:outside] for point in (entity.start, entity.end)]
        low = Point(min(point.x for point in points), min(point.y for point in points))
        high = Point(max(point.x for point in points), max(point.y for point in points))
        corners = [low, Point(high.x, low.y), high, Point(low.x, high.y)]
        entities = [Line(corners[i], corners[(i + 1) % 4]) for i in range(4)] + entities[outside:]
    return Profile(translated(rotated(entities, degrees), offset))


# Cross sections which aren't plain rectangles, centered on the origin
PROFILES = [
    # T-slot extrusion
    Sketch("REV_1IN_EXTRUSION", step_profile("REV-21-1000.STEP")),
    Sketch("REV_15MM_EXTRUSION", step_profile("REV-41-1017.STEP")),
    # Modeled off center
    Sketch("REV_15X30MM_EXTRUSION", step_profile("REV-41-1093.STEP", Point(7.5 / 25.4, 0))),
    # MAXTube which isn't a plain tube: hollow corners, with thinner walls between them (and the 2x1s modeled with
    # their 2 in. sides facing X)
    Sketch("REV_MAXTUBE_1X1", step_profile("REV-21-2160.STEP", plain_outside=True)),
    Sketch("REV_MAXTUBE_2X1", step_profile("REV-21-2162.STEP", degrees=90, plain_outside=True)),
    Sketch("REV_MAXTUBE_2X1_MAX", step_profile("REV-21-2163.STEP", degrees=90, plain_outside=True)),
    Sketch("REV_MAXTUBE_2X1_LIGHT", step_profile("REV-21-2161.STEP", degrees=90, plain_outside=True)),
    Sketch("REV_MAXTUBE_2X1_LIGHT_GRID", step_profile("REV-21-2289.STEP", degrees=90, plain_outside=True)),
    # REV-21-3287's is REV-21-3288's
    Sketch("REV_MAXTUBE_2X2", step_profile("REV-21-3288.STEP", plain_outside=True)),
    Sketch("REV_MAXTUBE_2X2_MAX", step_profile("REV-21-3286.STEP", plain_outside=True)),
]


# https://www.revrobotics.com/MAXTube (drawings: REV-21-xxxx-DR.pdf, and STEP files on each product page); clear
# anodized. The original profiles (REV's plain "MAXTube") have hollow corners, with 1 mm walls between them on the
# 1 in. sides of 2x1 (1/8 in. on its 2 in. sides; 1 mm on each side of Light); the grid pattern ones are plain tube,
# but for 2x2's, which also has hollow corners. Holes are 5 mm, every 1/2 in. starting 1/2 in. from the end (1/4 in. on
# 1/2x1/2), in each side's middle, three across 2 in. sides (the STEP files).
REV = vendor(
    "REV",
    "https://www.revrobotics.com/MAXTube",
    # Each size's own, since the extrusion comes in two
    None,
    [
        size(
            "1/2x1/2",
            [
                rev_tube(
                    "REV-21-3289",
                    REV_HALF_URL,
                    "1/16 in.",
                    0.5,
                    0.5,
                    0.0625,
                    x_rows=[row(inch(0.25), inch(0.5), [shape(inch(0))])],
                    y_rows=[row(inch(0.25), inch(0.5), [shape(inch(0))])],
                    tie_start=0.25,
                )
            ],
            WHITE,
        ),
        size(
            "1x1",
            [
                rev_tube("REV-21-2160", REV_1X1_URL, "Standard", 1, 1, REV_MM, detail="standard",
                         profile="REV_MAXTUBE_1X1"),
                rev_tube("REV-21-3540", REV_1X1_URL, "1/16 in.", 1, 1, 0.0625),
                rev_tube("REV-21-3543", REV_1X1_URL, "1/8 in.", 1, 1, 0.125),
            ],
            WHITE,
        ),
        size(
            "2x1",
            [
                pattern(
                    "Grid",
                    [
                        rev_tube("REV-21-2289", REV_2X1_URL, "Light", 2, 1, REV_MM, detail="light",
                                 profile="REV_MAXTUBE_2X1_LIGHT_GRID"),
                        rev_tube("REV-21-3552", REV_2X1_URL, "1/16 in.", 2, 1, 0.0625),
                        rev_tube("REV-21-3555", REV_2X1_URL, "1/8 in.", 2, 1, 0.125),
                        rev_tube("REV-21-3586", REV_2X1_URL, "3/16 in.", 2, 1, 0.1875),
                    ],
                ),
                pattern(
                    "1 in. sides only",
                    [
                        rev_tube("REV-21-2161", REV_2X1_URL, "Light", 2, 1, REV_MM, detail="light, 1 in. sides only",
                                 y_holes=False, profile="REV_MAXTUBE_2X1_LIGHT"),
                        rev_tube("REV-21-2162", REV_2X1_URL, "Standard", 2, 1, REV_MM,
                                 detail="standard, 1 in. sides only", y_holes=False, profile="REV_MAXTUBE_2X1"),
                    ],
                ),
                pattern(
                    "MAX Pattern",
                    [
                        max_pattern_tube("Standard", "standard, MAX Pattern", [2163, 2164, 2165, 2169, 2173, 2177, 2185],
                                         profile="REV_MAXTUBE_2X1_MAX"),
                        max_pattern_tube("1/8 in.", "1/8 in. wall, MAX Pattern", [3558, 3559, 3560, 3564, 3568, 3572, 3580]),
                    ],
                ),
            ],
            WHITE,
            level="pattern",
        ),
        size(
            "2x2",
            [
                # 1/8 in. walls, with hollow corners
                pattern("Grid", [rev_tube("REV-21-3288", REV_2X2_URL, "1/8 in.", 2, 2, 0.125, detail="grid",
                                          profile="REV_MAXTUBE_2X2")]),
                pattern("MAX Pattern", [rev_tube("REV-21-3286", REV_2X2_URL, "1/8 in.", 2, 2, 0.125,
                                                 detail="MAX Pattern", x_rows=max_pattern(), y_rows=max_pattern(),
                                                 tie_unit=2, profile="REV_MAXTUBE_2X2_MAX")]),
                pattern("MAX Pattern and grid", [rev_tube("REV-21-3287", REV_2X2_URL, "1/8 in.", 2, 2, 0.125,
                                                          detail="MAX Pattern and grid", y_rows=max_pattern(),
                                                          profile="REV_MAXTUBE_2X2")]),
            ],
            WHITE,
            level="pattern",
        ),
        size("Angle", [rev_angle()], WHITE, level="variant"),
        size(
            "1 in. Extrusion",
            [
                # 4 ft (REV-21-1000's STEP file)
                extrusion("Clear anodized", "1in Extrusion (REV, clear)", 1, 1, "REV_1IN_EXTRUSION", WHITE,
                          stock((inch(48), "REV-21-1000", REV_1IN_URL))),
                extrusion("Black anodized", "1in Extrusion (REV, black)", 1, 1, "REV_1IN_EXTRUSION", BLACK,
                          stock((inch(48), "REV-21-1404", REV_1IN_URL))),
            ],
            level="finish",
        ),
    ],
)


AM_URL = "https://andymark.com/products/pre-drilled-box-tube-extrusion"


def am_tube(part_number: str, wall: str, width: float, height: float, thickness: float, plain: bool = False) -> Value:
    """AndyMark's tube, with holes on every side, or none (`plain`)."""
    return frc_tube(
        "AndyMark",
        wall,
        width,
        height,
        thickness,
        thickness,
        stock((inch(47), part_number, f"https://andymark.com/{part_number}")),
        detail=f"{wall} wall" + (", no holes" if plain else ""),
        x_holes=not plain,
        y_holes=not plain,
    )


# https://andymark.com/products/pre-drilled-box-tube-extrusion (drawings: am-5177 to am-5180): the same grid as
# WCP's; or without holes (am-4203, am-4204, am-3214, am-4205); raw aluminum
ANDYMARK = vendor(
    "AndyMark",
    AM_URL,
    WHITE,
    [
        size(
            "1x1",
            [
                pattern("Full", [am_tube("am-5177", "1/16 in.", 1, 1, 0.0625), am_tube("am-5178", "1/8 in.", 1, 1, 0.125)]),
                pattern(
                    "No holes",
                    [
                        am_tube("am-4203", "1/16 in.", 1, 1, 0.0625, plain=True),
                        am_tube("am-4204", "1/8 in.", 1, 1, 0.125, plain=True),
                    ],
                ),
            ],
            level="pattern",
        ),
        size(
            "2x1",
            [
                pattern("Full", [am_tube("am-5179", "1/16 in.", 2, 1, 0.0625), am_tube("am-5180", "1/8 in.", 2, 1, 0.125)]),
                pattern(
                    "No holes",
                    [
                        am_tube("am-3214", "1/16 in.", 2, 1, 0.0625, plain=True),
                        am_tube("am-4205", "1/8 in.", 2, 1, 0.125, plain=True),
                    ],
                ),
            ],
            level="pattern",
        ),
    ],
)


TTB_URL = "https://www.thethriftybot.com/products/thrifty-box-extrusion"


def ttb_tube(sku: str, wall: str, thickness: float) -> Value:
    return frc_tube("TTB", wall, 2, 1, thickness, thickness, stock((inch(47), sku, TTB_URL)), diameter=5 / 25.4)


# https://www.thethriftybot.com/products/thrifty-box-extrusion: 5 mm holes on a 1/2 in. grid; raw aluminum
# TODO: ThriftyBot's drawings, to check that their grid starts 1/2 in. from the end like WCP's
TTB = vendor(
    "ThriftyBot",
    TTB_URL,
    WHITE,
    [
        size(
            "2x1",
            [
                ttb_tube("TTB-0291", "0.080 in.", 0.08),
                ttb_tube("TTB-0094", "1/8 in.", 0.125),
            ],
        )
    ],
)


SWYFT_URL = "https://swyftrobotics.com/structure/swyft-super-tube"


def swyft_tube(sku: str, wall: str, width: float, height: float, thickness: float, plain: bool = False) -> Value:
    """Swyft's tube, with its grid of holes, or none (`plain`). Its grid has one column of holes on each side but for
    2x1's 2 in. sides, which have two, 1/2 in. either side of the middle, and none in it."""
    columns = [shape(inch(-0.5)), shape(inch(0.5))] if width == 2 else [shape(inch(0))]
    return frc_tube(
        "Swyft",
        wall,
        width,
        height,
        thickness,
        thickness,
        stock((inch(47), sku, SWYFT_URL)),
        detail=f"{'plain' if plain else 'grid'}, {wall} wall",
        diameter=5 / 25.4,
        x_rows=[] if plain else grid(height),
        y_rows=[] if plain else [row(inch(0.5), inch(0.5), columns)],
    )


# https://swyftrobotics.com/structure/swyft-super-tube; black anodized. Its CAD (in the Google Drive folder linked from
# the page; SR-TUBE STEP files.zip): 5 mm holes every 1/2 in., starting 1/2 in. from the end (see swyft_tube). It's also
# sold with a Grid + Bearing pattern, which is left out.
SWYFT = vendor(
    "Swyft",
    SWYFT_URL,
    BLACK,
    [
        size(
            "1x1",
            [
                swyft_tube("SR-TUBE-0625-GRID-1X1-47", "1/16 in.", 1, 1, 0.0625),
                swyft_tube("SR-TUBE-125-GRID-1X1-47", "1/8 in.", 1, 1, 0.125),
            ],
        ),
        size(
            "2x1",
            [
                pattern(
                    "Grid",
                    [
                        swyft_tube("SR-TUBE-035-GRID-2X1-47", "0.035 in.", 2, 1, 0.035),
                        swyft_tube("SR-TUBE-0625-GRID-2X1-47", "1/16 in.", 2, 1, 0.0625),
                        swyft_tube("SR-TUBE-125-GRID-2X1-47", "1/8 in.", 2, 1, 0.125),
                    ],
                ),
                pattern(
                    "Plain",
                    [
                        swyft_tube("SR-TUBE-035-PLAIN-2X1-47", "0.035 in.", 2, 1, 0.035, plain=True),
                        swyft_tube("SR-TUBE-0625-PLAIN-2X1-47", "1/16 in.", 2, 1, 0.0625, plain=True),
                        swyft_tube("SR-TUBE-125-PLAIN-2X1-47", "1/8 in.", 2, 1, 0.125, plain=True),
                    ],
                ),
            ],
            level="pattern",
        ),
    ],
)


LAST_ANVIL_URL = "https://lastanvil.com/products/patterned-tube"

# https://lastanvil.com/products/patterned-tube: 7075, black anodized, with WCP's pattern (#10 holes every 1/2 in., on
# every side), and filleted corners which this leaves sharp
LAST_ANVIL = vendor(
    "Last Anvil",
    LAST_ANVIL_URL,
    BLACK,
    [
        size(
            "2x1",
            [
                frc_tube(
                    "Last Anvil",
                    "1/16 in.",
                    2,
                    1,
                    0.0625,
                    0.0625,
                    stock((inch(47), "240114", LAST_ANVIL_URL + "?variant=42303483773134")),
                )
            ],
        )
    ],
)


# goBILDA's pattern, on each 48 mm face (from goBILDA's STEP files): a 14 mm bore every 24 mm starting 24 mm from the
# end; 4 mm holes on an 8 mm grid around them; 4 mm holes at 45 degrees on a 24 mm circle around each bore, and across
# each bore on a 16 mm diamond; and short slots between bores, where neighboring bores' diamonds meet.


def gobilda_rows(face: int = 48) -> list[str]:
    """goBILDA's pattern on a 48 mm face (or a 32 mm one, which has neither the grid's outer columns nor the holes across
    each bore), as rows which don't overlap: holes on the 24 mm circle overlap the grid holes next to them, so they share
    their rows."""
    diagonal = 12 / math.sqrt(2)
    diamond = 8 * math.sqrt(2)
    lean = round(diagonal - 8, 4)
    bore = [shape(mm(0), diameter=mm(14))]
    if face == 48:
        bore += [shape(mm(round(diamond, 4))), shape(mm(round(-diamond, 4)))]
    rows = [
        # Bores, and the holes across them on the diamond
        row(mm(24), mm(24), bore),
        # Slots between bores, where neighboring bores' diamonds meet
        row(mm(12), mm(24), [shape(mm(0), slot=mm(round(24 - 2 * diamond, 4)))]),
    ]
    if face == 48:
        # The outer columns of the grid
        rows.append(row(mm(8), mm(8), [shape(mm(16)), shape(mm(-16))]))
    # The inner columns of the grid, either side of each bore, with the holes on the circle beside them
    for start, direction in ((8, 1), (16, -1)):
        shapes = []
        for side in (-1, 1):
            shapes += [shape(mm(side * 8)), shape(mm(round(side * diagonal, 4)), along=mm(direction * lean))]
        rows.append(row(mm(start), mm(24), shapes))
    return rows


def gobilda_stock(series: str, name: str, holes: list[int], length=lambda n: 48 + 24 * (n - 1)) -> str:
    """Lengths of goBILDA structure named e.g. "u-channel" in its pages' urls, each with `holes` holes. Channel is 48 mm
    long with 1 hole, and 24 mm longer for each hole after."""
    return stock(
        *[
            (
                mm(length(n)),
                f"{series}-{n:04d}-{length(n):04d}",
                f"https://www.gobilda.com/{series}-series-{name}-{n}-hole-{length(n)}mm-length/",
            )
            for n in holes
        ]
    )


GOBILDA_HOLES = [*range(1, 19), 21, 25, 29, 33, 37, 41, 45, 49]


# https://www.gobilda.com/structure/ (schematics and STEP files on each product page)
# TODO: goBILDA's appearance
GOBILDA = vendor(
    "goBILDA",
    "https://www.gobilda.com/channel/",
    WHITE,
    [
        only(
            "U-Channel",
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
                gobilda_stock("1120", "u-channel", GOBILDA_HOLES),
                unit=mm,
                open=True,
            ),
        ),
        only(
            "Low-Side U-Channel",
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
                gobilda_stock("1121", "low-side-u-channel", GOBILDA_HOLES),
                unit=mm,
                open=True,
            ),
        ),
        only(
            "Mini Low-Side U-Channel",
            tube(
                "1143 Series",
                "Mini Low-Side U-Channel (goBILDA 1143 Series)",
                32,
                12,
                2.5,
                2.5,
                4,
                # As on the 1121's sides (1143-0003-0096's STEP file)
                [row(mm(8), mm(8), [shape(mm(2))])],
                gobilda_rows(32),
                mm(24),
                mm(24),
                gobilda_stock("1143", "mini-low-side-u-channel", list(range(1, 18))),
                unit=mm,
                open=True,
            ),
        ),
        only(
            "Square Beam",
            # Solid around a 4 mm bore, with 4 mm holes through each side every 8 mm, starting 4 mm from the end
            # (1106-0007-0056's STEP file)
            # TODO: its ends are tapped M4
            tube(
                "1106 Series",
                "Square Beam (goBILDA 1106 Series)",
                8,
                8,
                2,
                2,
                4,
                [row(mm(4), mm(8), [shape(mm(0))])],
                [row(mm(4), mm(8), [shape(mm(0))])],
                mm(4),
                mm(8),
                # 8 mm long for each hole
                gobilda_stock(
                    "1106", "square-beam", [*range(2, 14), 15, 17, 19, 21, 23, 29, 33, 35, 41], length=lambda n: 8 * n
                ),
                unit=mm,
                bore=4,
            ),
        ),
    ],
)

# https://andymark.com/products/pre-drilled-box-tube-extrusion (drawing: am-5001-4700): AndyMark's Robits FTC system
ROBITS = vendor(
    "AndyMark",
    AM_URL,
    WHITE,
    [
        size(
            "1/2x1/2 (Robits)",
            [
                tube(
                    "1/16 in.",
                    "1/2x1/2 Tube (AndyMark Robits)",
                    0.5,
                    0.5,
                    0.063,
                    0.063,
                    0.201,
                    [row(inch(0.25), inch(0.5), [shape(inch(0))])],
                    [row(inch(0.25), inch(0.5), [shape(inch(0))])],
                    inch(0.25),
                    inch(0.5),
                    stock((inch(47), "am-5001-4700", "https://andymark.com/am-5001-4700")),
                )
            ],
        )
    ],
)

# https://www.revrobotics.com/15mm-extrusions/ (STEP files on the product page)
REV_FTC = vendor(
    "REV",
    REV_15MM_URL,
    # Each extrusion's own
    None,
    [
        size(
            "15mm Extrusion",
            [
                extrusion("Clear anodized", "15mm Extrusion (REV, clear)", 15, 15, "REV_15MM_EXTRUSION", WHITE,
                          stock((mm(120), "REV-41-1568-PK2", REV_15MM_URL), (mm(225), "REV-41-1431-PK2", REV_15MM_URL),
                                (mm(420), "REV-41-1432-PK4", REV_15MM_URL), (mm(1000), "REV-41-1017", REV_15MM_URL)),
                          unit=mm),
                extrusion("Black anodized", "15mm Extrusion (REV, black)", 15, 15, "REV_15MM_EXTRUSION", BLACK,
                          stock((mm(1000), "REV-41-1569", REV_15MM_URL)), unit=mm),
            ],
            level="finish",
        ),
        size(
            "15x30mm Extrusion",
            [
                extrusion("Clear anodized", "15x30mm Extrusion (REV, clear)", 30, 15, "REV_15X30MM_EXTRUSION", WHITE,
                          stock((mm(420), "REV-41-1587-PK4", REV_15MM_URL), (mm(1000), "REV-41-1093", REV_15MM_URL)),
                          unit=mm),
                extrusion("Black anodized", "15x30mm Extrusion (REV, black)", 30, 15, "REV_15X30MM_EXTRUSION", BLACK,
                          stock((mm(1000), "REV-41-1586", REV_15MM_URL)), unit=mm),
            ],
            level="finish",
        ),
    ],
)

CONTENTS = [
    Import("core/sketchData.fs"),
    *PROFILES,
    Table("frcFrameTable", Node("vendor", [WCP, REV, ANDYMARK, TTB, SWYFT, LAST_ANVIL])),
    Table("ftcFrameTable", Node("vendor", [GOBILDA, REV_FTC, ROBITS])),
]
