"""Belt tables for robotBelt and robotPulley. Run `uv run fs gen` after editing."""

from fs_cli.tables import Enum, Node, Table, Value, inch, mm

BeltType = Enum("BeltType", ["_2_MM_GT2", "_3_MM_GT2", "_3_MM_HTD", "_5_MM_HTD", "RT25"])


def teeth_node(teeth: list[int]) -> Node:
    return Node("teeth", [Value(f"{count}T", {"beltTeeth": str(count)}) for count in teeth])


def supplier_node(suppliers: dict[str, list[int]]) -> Node:
    """Maps each supplier to the tooth counts of the belts it sells. "Custom" is always added."""
    entries = [Value(name, next=teeth_node(teeth)) for name, teeth in suppliers.items()]
    # Custom needs no value, since it can be determined from the path
    return Node("supplier", [*entries, Value("Custom")], display_name="Belt supplier")


def width_node(widths_mm: list[int]) -> Node:
    return Node(
        "width",
        [Value(f"{width} mm", {"beltWidth": mm(width)}) for width in widths_mm],
        display_name="Belt width",
    )


belt_type = Node(
    "beltType",
    [
        Value("2mm GT2", {"beltType": BeltType["_2_MM_GT2"], "beltWidth": mm(6)}),
        Value("3mm GT2", {"beltType": BeltType["_3_MM_GT2"], "beltWidth": mm(9)}),
        Value("3mm HTD", {"beltType": BeltType["_3_MM_HTD"], "beltWidth": mm(15)}),
        Value("5mm HTD", {"beltType": BeltType["_5_MM_HTD"]}, width_node([9, 15, 18])),
        Value("RT25", {"beltType": BeltType["RT25"], "beltWidth": inch(0.5)}),
    ],
    display_name="Belt type",
    default="5mm HTD",
)

belt = belt_type.with_next(
    {
        "2mm GT2": supplier_node(
            # vendor/goBILDA-2mm-GT2-belts.md
            {
                "goBILDA": [
                    44, 68, 92, 108, 116, 132, 140, 156, 164, 180, 188, 204, 209, 212, 228, 233, 236,
                    252, 257, 276, 300, 324,
                ]
            }
        ),
        "3mm GT2": supplier_node(
            {
                "WCP": [
                    45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120, 125, 130,
                    140, 150, 160, 170, 180, 190, 200,
                ],
                "REV": [55, 85, 105, 120, 145, 184, 210, 270],
            }
        ),
        "3mm HTD": supplier_node(
            {"goBILDA": [68, 84, 100, 116, 132, 148, 164, 180, 212, 228, 244, 260, 276, 292]}
        ),
        "5mm HTD": width_node([9, 15, 18]).with_next(
            {
                "9 mm": supplier_node(
                    {
                        "WCP": [
                            30, 35, 40, 43, 45, 50, 53, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100,
                            104, 105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170,
                            175, 180, 185, 190, 195, 200, 210, 220, 225, 230, 240, 250, 260, 270, 280,
                            290, 300,
                        ],
                        "AndyMark": [
                            30, 35, 40, 45, 48, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 91, 93, 95, 100,
                            105, 106, 110, 115, 120, 121, 125, 130, 135, 136, 140, 145, 150, 152, 160,
                            167, 170, 180, 190, 200, 225, 250,
                        ],
                        "The Thrifty Bot": [
                            35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115, 120,
                            125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195,
                            200, 210, 220, 230, 240, 250, 260, 270, 280, 290, 300,
                        ],
                        "goBILDA": [
                            43, 45, 49, 53, 55, 59, 63, 64, 68, 72, 74, 76, 78, 82, 84, 85, 89, 92, 94,
                            97, 101, 104, 107, 112,
                        ],
                    }
                ),
                "15 mm": supplier_node(
                    {
                        "WCP": [
                            30, 35, 40, 45, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100, 104, 105,
                            110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180,
                            185, 190, 195, 200, 210, 220, 225, 230, 240, 250, 260, 270, 280, 290, 300,
                        ],
                        "AndyMark": [
                            30, 35, 40, 45, 50, 55, 60, 64, 65, 70, 75, 78, 80, 85, 90, 95, 100, 104,
                            105, 107, 110, 115, 117, 120, 125, 130, 131, 135, 140, 145, 150, 151, 160,
                            170, 180, 190, 200, 210, 220, 225, 230, 240, 250,
                        ],
                        "The Thrifty Bot": [
                            35, 40, 45, 50, 55, 60, 64, 65, 70, 75, 80, 85, 90, 95, 100, 105, 110, 115,
                            120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190,
                            195, 200, 210, 220, 230, 240, 250, 260, 270, 280, 290, 300,
                        ],
                    }
                ),
                "18 mm": supplier_node({}),
            }
        ),
        "RT25": supplier_node(
            {
                "REV": [
                    32, 36, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 128, 136, 144, 152, 160,
                    168, 176, 184, 192, 200, 208, 216,
                ]
            }
        ),
    }
)

# Both widths of double sided belt come in the same lengths from the same suppliers
DOUBLE_BELT_TEETH = [100, 125, 150, 175, 200, 250, 300]
double_belt_suppliers = supplier_node(
    {"WCP": DOUBLE_BELT_TEETH, "The Thrifty Bot": DOUBLE_BELT_TEETH}
)

double_belt = Node(
    "beltType",
    [
        Value(
            "5mm HTD",
            {"beltType": BeltType["_5_MM_HTD"], "pitch": mm(5)},
            width_node([9, 15]).with_next(double_belt_suppliers),
        )
    ],
    display_name="Belt type",
)

CONTENTS = [
    BeltType,
    Table("beltTable", belt),
    Table("doubleBeltTable", double_belt),
    Table("beltTypeTable", belt_type),
]
