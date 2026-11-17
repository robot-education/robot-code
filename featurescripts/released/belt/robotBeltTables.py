"""Belt tables for robotBelt and robotPulley. Run `uv run fs gen` after editing."""

import dataclasses

from fs_cli.tables import Enum, Node, Table, Value, inch, mm

BeltType = Enum("BeltType", ["_2_MM_GT2", "_3_MM_GT2", "_3_MM_HTD", "_5_MM_HTD", "RT25"])

# A custom belt's type: any but REV's RT25 (a closed set), the most common first. Stored in documents: never rename or
# remove one.
CustomBeltType = Enum(
    "CustomBeltType",
    ["_5_MM_HTD", "_3_MM_GT2", "_3_MM_HTD", "_2_MM_GT2"],
    names={"_5_MM_HTD": "5mm HTD", "_3_MM_GT2": "3mm GT2", "_3_MM_HTD": "3mm HTD", "_2_MM_GT2": "2mm GT2"},
)


def teeth_node(teeth: list[int]) -> Node:
    return Node("teeth", [Value(f"{count}T", {"beltTeeth": str(count)}) for count in teeth])


# A belt of a width no supplier sells, or a custom one: its width is the feature's Belt width
CUSTOM_WIDTH = "Custom"

# A belt of any type, width, and teeth, given by the feature's own parameters: first, but not the default
CUSTOM_SUPPLIER = "Custom"

# What the custom options show: the feature's custom belt parameters (its Belt type, width, and teeth, or an open belt's
# or pulley's Belt width), which `showsCustomBelt` (generated) says a path chose
CUSTOM_BELT = "customBelt"


def width_node(widths: list[tuple[str, str]], extra: list[Value] = (), custom: bool = True) -> Node:
    """Each width, by its name and its expression, then `extra` widths, then Custom (which has no beltWidth), unless
    `custom` is false."""
    return Node(
        "width",
        [*(Value(name, {"beltWidth": width}) for name, width in widths), *extra, *([Value(CUSTOM_WIDTH, shows=CUSTOM_BELT)] if custom else [])],
        display_name="Belt width",
    )


def mm_widths(*widths_mm: int) -> list[tuple[str, str]]:
    return [(f"{width} mm", mm(width)) for width in widths_mm]


# WCP's rubber coated belts' backs are 4 mm outside their pitch line (their drawing,
# Web-Rubber Coated HTD Timing Belts), where a 5mm HTD belt's is 1.16 mm
RUBBER_COATED_15_MM = Value(
    "15 mm rubber coated", {"beltWidth": mm(15), "backThickness": mm(4), "rubberCoated": "true"}
)

WIDTHS = {
    "2mm GT2": mm_widths(6),
    "3mm GT2": mm_widths(9),
    "3mm HTD": mm_widths(15),
    "5mm HTD": mm_widths(9, 15, 18),
    "RT25": [("1/2 in.", inch(0.5))],
}

BELT_TYPES = {
    "2mm GT2": BeltType["_2_MM_GT2"],
    "3mm GT2": BeltType["_3_MM_GT2"],
    "3mm HTD": BeltType["_3_MM_HTD"],
    "5mm HTD": BeltType["_5_MM_HTD"],
    "RT25": BeltType["RT25"],
}

# The belts each supplier sells, for FRC or FTC: by belt type, then width, their tooth counts. The most common first, as
# the first of each is the default.
FRC_SUPPLIERS: dict[str, dict[str, dict[str, list[int]]]] = {
    "WCP": {
        "5mm HTD": {
            "9 mm": [
                30, 35, 40, 43, 44, 45, 50, 53, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100, 104, 105, 110, 115, 120,
                125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 210, 220, 225, 230,
                240, 250, 260, 270, 280, 290, 300,
            ],
            "15 mm": [
                30, 35, 40, 44, 45, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100, 104, 105, 110, 115, 120, 125, 130,
                135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 210, 220, 225, 230, 240, 250,
                260, 270, 280, 290, 300,
            ],
            # docs.wcproducts.com's belts and pulleys
            RUBBER_COATED_15_MM.name: list(range(30, 301, 10)),
        },
        "3mm GT2": {
            "9 mm": [
                45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 140, 150, 160, 170,
                180, 190, 200,
            ],
        },
    },
    "The Thrifty Bot": {
        "5mm HTD": {
            "9 mm": [
                35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 135, 140, 145,
                150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 210, 220, 230, 240, 250, 260, 270, 280, 290,
                300,
            ],
            "15 mm": [
                35, 40, 45, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130, 135, 140,
                145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 210, 220, 230, 240, 250, 260, 270, 280,
                290, 300,
            ],
        },
    },
    "AndyMark": {
        "5mm HTD": {
            "9 mm": [
                30, 35, 40, 45, 48, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 91, 93, 95, 100, 105, 106, 110, 115, 120,
                121, 125, 130, 135, 136, 140, 145, 150, 152, 160, 167, 170, 180, 190, 200, 225, 250,
            ],
            "15 mm": [
                30, 35, 40, 45, 50, 55, 60, 64, 65, 70, 75, 78, 80, 85, 90, 95, 100, 104, 105, 107, 110, 115, 117, 120,
                125, 130, 131, 135, 140, 145, 150, 151, 160, 170, 180, 190, 200, 210, 220, 225, 230, 240, 250,
            ],
        },
    },
    # REV's ION system
    "REV": {
        "RT25": {
            "1/2 in.": [
                32, 36, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192,
                200, 208, 216,
            ],
        },
    },
}

FTC_SUPPLIERS: dict[str, dict[str, dict[str, list[int]]]] = {
    "goBILDA": {
        # vendor/goBILDA-2mm-GT2-belts.md
        "2mm GT2": {
            "6 mm": [
                44, 68, 92, 108, 116, 132, 140, 156, 164, 180, 188, 204, 209, 212, 228, 233, 236, 252, 257, 276, 300,
                324,
            ],
        },
        "3mm HTD": {"15 mm": [68, 84, 100, 116, 132, 148, 164, 180, 212, 228, 244, 260, 276, 292]},
        "5mm HTD": {
            "9 mm": [43, 45, 49, 53, 55, 59, 63, 64, 68, 72, 74, 76, 78, 82, 84, 85, 89, 92, 94, 97, 101, 104, 107, 112],
        },
    },
    # REV's FTC system
    "REV": {"3mm GT2": {"9 mm": [55, 85, 105, 120, 145, 184, 210, 270]}},
}


def width_value(belt_type: str, width: str) -> Value:
    if width == RUBBER_COATED_15_MM.name:
        return RUBBER_COATED_15_MM
    return Value(width, {"beltWidth": dict(WIDTHS[belt_type])[width]})


def supplier_belts(belts: dict[str, dict[str, list[int]]]) -> Node:
    """A supplier's belt types, each its widths (or, if it has one, just its teeth), each its tooth counts."""
    entries = []
    for belt_type, widths in belts.items():
        values = {"beltType": BELT_TYPES[belt_type]}
        if len(widths) == 1:
            ((width, teeth),) = widths.items()
            # One width: no choice to make
            entries.append(Value(belt_type, {**values, **width_value(belt_type, width).values}, teeth_node(teeth)))
        else:
            entries.append(Value(belt_type, values, Node(
                "width",
                [Value(width_value(belt_type, width).name, width_value(belt_type, width).values, teeth_node(teeth)) for width, teeth in widths.items()],
                display_name="Belt width",
            )))
    return Node("beltType", entries, display_name="Belt type")


def supplier_table(suppliers: dict[str, dict[str, dict[str, list[int]]]]) -> Node:
    """Custom first (its belt's the feature's Custom belt parameters), then each supplier's belts; the first supplier
    by default."""
    return Node(
        "supplier",
        [Value(CUSTOM_SUPPLIER, shows=CUSTOM_BELT), *(Value(supplier, next=supplier_belts(belts)) for supplier, belts in suppliers.items())],
        display_name="Belt supplier",
        # Custom only if there's no supplier
        default=next(iter(suppliers), CUSTOM_SUPPLIER),
    )


frc_belt = supplier_table(FRC_SUPPLIERS)
ftc_belt = supplier_table(FTC_SUPPLIERS)


def without_teeth(node: Node) -> Node:
    """A table's sizes: its levels down to its teeth, which a simple belt using the closest belt chooses for itself
    (from the full table's teeth for its path)."""
    entries = [
        dataclasses.replace(entry, next=None if entry.next is None or entry.next.name == "teeth" else without_teeth(entry.next))
        for entry in node.entries
    ]
    return dataclasses.replace(node, entries=entries)


frc_belt_size = without_teeth(frc_belt)
ftc_belt_size = without_teeth(ftc_belt)

# Both widths of double sided belt come in the same lengths from the same suppliers (only FRC's)
DOUBLE_BELT_TEETH = [100, 125, 150, 175, 200, 250, 300]
frc_double_belt = supplier_table(
    {supplier: {"5mm HTD": {"9 mm": DOUBLE_BELT_TEETH, "15 mm": DOUBLE_BELT_TEETH}} for supplier in ["WCP", "The Thrifty Bot"]}
)
# No FTC supplier sells them: only custom ones
ftc_double_belt = supplier_table({})


# What an open belt or a pulley's for (no supplier: open belts are cut to length)
belt_type = Node(
    "beltType",
    [
        Value("2mm GT2", {"beltType": BeltType["_2_MM_GT2"]}, width_node(WIDTHS["2mm GT2"])),
        Value("3mm GT2", {"beltType": BeltType["_3_MM_GT2"]}, width_node(WIDTHS["3mm GT2"])),
        Value("3mm HTD", {"beltType": BeltType["_3_MM_HTD"]}, width_node(WIDTHS["3mm HTD"])),
        Value("5mm HTD", {"beltType": BeltType["_5_MM_HTD"]}, width_node(WIDTHS["5mm HTD"])),
        # REV's: a closed set, so no custom belts, and one width
        Value("RT25", {"beltType": BeltType["RT25"], "beltWidth": inch(0.5)}),
    ],
    display_name="Belt type",
    default="5mm HTD",
)

CONTENTS = [
    BeltType,
    CustomBeltType,
    Table("frcBeltTable", frc_belt),
    Table("ftcBeltTable", ftc_belt),
    Table("frcBeltSizeTable", frc_belt_size),
    Table("ftcBeltSizeTable", ftc_belt_size),
    Table("frcDoubleBeltTable", frc_double_belt),
    Table("ftcDoubleBeltTable", ftc_double_belt),
    Table("beltTypeTable", belt_type),
]
