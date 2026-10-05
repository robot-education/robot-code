"""Nut strip lookup tables for robotNutStrip. Run `uv run fs gen` after editing.

Each nut strip has two rows of tapped through holes along its length, one through its width (the X row) and one
through its height (the Y row). Each row's first hole is `xHoleStart` or `yHoleStart` from the strip's start, and its
holes are `spacing` apart; holes stop as close to the strip's end as the closest row starts to its start. Strips with
`centerHole` also have a tapped through hole down their length.

`stock` lists the lengths each nut strip is sold in, shortest first, with their part numbers and pages. A strip is
given the part number of the shortest stock it can be cut from; strips longer than any stock link to the vendor's
`url` instead.
"""

import dataclasses

from fs_cli.tables import Node, Table, Value, inch, mm, string

# Placeholder appearances until vendor colors are picked: robotProperties.fs's WHITE, BLACK, and DARK_GRAY
WHITE = "color(230 / 255, 230 / 255, 230 / 255)"
BLACK = "color(0.3, 0.3, 0.3)"
DARK_GRAY = "color(135 / 255, 135 / 255, 135 / 255)"


def thread(name: str, size: str, pitch: str, major_diameter: str, tap_drill_diameter: str) -> Value:
    """A thread, with size and pitch as in the std hole tables."""
    return Value(
        name,
        {
            "threadName": string(name),
            "size": string(size),
            "pitch": string(pitch),
            "majorDiameter": major_diameter,
            "tapDrillDiameter": tap_drill_diameter,
        },
    )


NUMBER_8_32 = thread("#8-32", "#8", "32 tpi", inch(0.164), inch(0.136))
NUMBER_10_32 = thread("#10-32", "#10", "32 tpi", inch(0.19), inch(0.159))
M3 = thread("M3 x 0.5", "M3", "0.5 mm", mm(3), mm(2.5))


def with_center_hole(thread: Value) -> Value:
    return dataclasses.replace(thread, values={**thread.values, "centerHole": "true"})


def with_stock(thread: Value, *lengths: tuple[str, str, str]) -> Value:
    """A thread sold in (length, part number, url) lengths."""
    lengths = sorted(lengths, key=lambda length: _inches(length[0]))
    stock = ", ".join(
        f"{{ {string('length')} : {length}, {string('partNumber')} : {string(part_number)}, {string('url')} : {string(url)} }}"
        for length, part_number, url in lengths
    )
    return dataclasses.replace(thread, values={**thread.values, "stock": f"[{stock}]"})


def _inches(length: str) -> float:
    value, unit = length.split(" * ")
    return float(value) / (25.4 if unit == "millimeter" else 1)


def wcp(part_number: str) -> str:
    return f"https://wcproducts.com/products/{part_number.lower()}"


def size(
    name: str,
    width: str,
    spacing: str,
    x_hole_start: str,
    y_hole_start: str,
    threads: list[Value],
    default_thread: str | None = None,
) -> Value:
    """A square nut strip `width` across."""
    return Value(
        name,
        {
            "sizeName": string(name),
            "width": width,
            "height": width,
            "spacing": spacing,
            "xHoleStart": x_hole_start,
            "yHoleStart": y_hole_start,
        },
        Node("thread", threads, default=default_thread),
    )


def vendor(name: str, url: str, sizes: list[Value], appearance: str, default_size: str | None = None) -> Value:
    return Value(
        name,
        {"vendor": string(name), "url": string(url), "appearance": appearance},
        Node("size", sizes, default=default_size),
    )


# https://wcproducts.com/products/nut-strips
# The rows alternate: one starts 1/4 in. from the end, the other 1/2 in. (drawing: Web-Aluminum Nut Strip.pdf)
# Raw aluminum
# Part numbers from the drawing; each has its own page
WCP = vendor(
    "WCP",
    "https://wcproducts.com/products/nut-strips",
    [
        size(
            "1/2 in.",
            inch(0.5),
            inch(0.5),
            inch(0.25),
            inch(0.5),
            [
                with_stock(
                    NUMBER_10_32,
                    (inch(1.25), "WCP-2495", wcp("WCP-2495")),
                    (inch(3), "WCP-1553", wcp("WCP-1553")),
                    (inch(6), "WCP-0336", wcp("WCP-0336")),
                ),
                with_stock(NUMBER_8_32, (inch(6), "WCP-0335", wcp("WCP-0335"))),
            ],
        ),
        size(
            "3/8 in.",
            inch(0.375),
            inch(0.5),
            inch(0.25),
            inch(0.5),
            [
                with_stock(
                    NUMBER_10_32,
                    (inch(3), "WCP-1554", wcp("WCP-1554")),
                    (inch(6), "WCP-1555", wcp("WCP-1555")),
                )
            ],
        ),
    ],
    appearance=WHITE,
)

# https://lastanvil.com/products/nut-strip
# The rows alternate like WCP's (both STEP files); the #10-32 strip also has a hole down its center
# Raw aluminum (the #10-32 strip is sandblasted, the #8-32 strip unfinished)
# SKUs from the product page's variants
LAST_ANVIL_URL = "https://lastanvil.com/products/nut-strip"
LAST_ANVIL = vendor(
    "Last Anvil",
    LAST_ANVIL_URL,
    [
        size(
            "1/2 in.",
            inch(0.5),
            inch(0.5),
            inch(0.25),
            inch(0.5),
            [
                with_stock(
                    with_center_hole(NUMBER_10_32),
                    (inch(6), "240111", LAST_ANVIL_URL + "?variant=42303475417294"),
                ),
                with_stock(NUMBER_8_32, (inch(6), "240349", LAST_ANVIL_URL + "?variant=42833788764366")),
            ],
        )
    ],
    appearance=WHITE,
)

# https://www.revrobotics.com/3-8in-nut-strips/
# The rows line up (their holes cross), starting 0.24 in. from the end (REV-21-3420's STEP file)
# Black anodized
# REV's parts don't have their own pages (and are sold in 2-packs)
REV_FRC_URL = "https://www.revrobotics.com/3-8in-nut-strips/"
REV_FRC = vendor(
    "REV",
    REV_FRC_URL,
    [
        size(
            "3/8 in.",
            inch(0.375),
            inch(0.5),
            inch(0.24),
            inch(0.24),
            [
                with_stock(
                    NUMBER_10_32,
                    # 1.48 in. to 6.48 in., with 3 to 13 holes in each row (drawing: 0.375in-Nut-Strip-DR.pdf)
                    *[(inch(1.48 + i), f"REV-21-{3420 + 2 * i}-PK2", REV_FRC_URL) for i in range(6)],
                )
            ],
        )
    ],
    appearance=BLACK,
)

# https://www.revrobotics.com/m3-nut-strips/
# The rows alternate every 8 mm: one starts 8 mm from the end, the other 16 mm (REV-41-1731's STEP file), and a
# tapped hole runs down the center
# Matte gray in REV's photos
REV_FTC_URL = "https://www.revrobotics.com/m3-nut-strips/"
REV_FTC = vendor(
    "REV",
    REV_FTC_URL,
    [
        size(
            "8 mm",
            mm(8),
            mm(16),
            mm(8),
            mm(16),
            [
                with_stock(
                    with_center_hole(M3),
                    (mm(32), "REV-41-1731", REV_FTC_URL),
                    (mm(40), "REV-41-1732", REV_FTC_URL),
                    (mm(56), "REV-41-1733", REV_FTC_URL),
                    (mm(136), "REV-41-1738", REV_FTC_URL),
                )
            ],
        )
    ],
    appearance=DARK_GRAY,
)

CONTENTS = [
    Table("frcNutStripTable", Node("vendor", [WCP, REV_FRC, LAST_ANVIL])),
    Table("ftcNutStripTable", Node("vendor", [REV_FTC])),
]
