"""Nut strip lookup tables for robotNutStrip. Run `uv run fs gen` after editing.

Each nut strip has two rows of tapped through holes along its length, one through its width (the X row) and one
through its height (the Y row). Each row's first hole is `xHoleStart` or `yHoleStart` from the strip's start, and its
holes are `spacing` apart; holes stop as close to the strip's end as the closest row starts to its start. Strips with
`centerHole` also have a tapped through hole down their length.
"""

import dataclasses

from fs_cli.tables import Node, Table, Value, inch, mm, string

# robotProperties.fs's BLACK
BLACK = "color(0.3, 0.3, 0.3)"


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


def vendor(name: str, sizes: list[Value], appearance: str | None = None, default_size: str | None = None) -> Value:
    values = {"vendor": string(name)}
    if appearance is not None:
        values["appearance"] = appearance
    return Value(name, values, Node("size", sizes, default=default_size))


# https://wcproducts.com/products/nut-strips
# The rows alternate: one starts 1/4 in. from the end, the other 1/2 in. (drawing: Web-Aluminum Nut Strip.pdf)
# TODO: WCP's appearance
WCP = vendor(
    "WCP",
    [
        size("1/2 in.", inch(0.5), inch(0.5), inch(0.25), inch(0.5), [NUMBER_10_32, NUMBER_8_32]),
        size("3/8 in.", inch(0.375), inch(0.5), inch(0.25), inch(0.5), [NUMBER_10_32]),
    ],
)

# https://lastanvil.com/products/nut-strip
# The rows alternate like WCP's (both STEP files); the #10-32 strip also has a hole down its center
# TODO: Last Anvil's appearance (the #10-32 strip is sandblasted, the #8-32 strip unfinished)
LAST_ANVIL = vendor(
    "Last Anvil",
    [size("1/2 in.", inch(0.5), inch(0.5), inch(0.25), inch(0.5), [with_center_hole(NUMBER_10_32), NUMBER_8_32])],
)

# https://www.revrobotics.com/3-8in-nut-strips/
# The rows line up (their holes cross), starting 0.24 in. from the end (REV-21-3420's STEP file)
REV_FRC = vendor(
    "REV",
    [size("3/8 in.", inch(0.375), inch(0.5), inch(0.24), inch(0.24), [NUMBER_10_32])],
    appearance=BLACK,
)

# https://www.revrobotics.com/m3-nut-strips/
# The rows alternate every 8 mm: one starts 8 mm from the end, the other 16 mm (REV-41-1731's STEP file)
# TODO: REV's M3 nut strips also have tapped holes in their ends
REV_FTC = vendor(
    "REV",
    [size("8 mm", mm(8), mm(16), mm(8), mm(16), [M3])],
)

CONTENTS = [
    Table("frcNutStripTable", Node("vendor", [WCP, REV_FRC, LAST_ANVIL])),
    Table("ftcNutStripTable", Node("vendor", [REV_FTC])),
]
