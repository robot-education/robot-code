"""Hole tables for robotShaft and robotSpacer. Run `uv run fs gen-tables` after editing."""

from fs_cli.tables import Node, Table, Value, inch, string


def fit_node(close: str, free: str) -> Node:
    return Node(
        "fit",
        [
            Value("Close", {"holeDiameter": string(close)}),
            Value("Free", {"holeDiameter": string(free)}),
        ],
        display_name="Fastener fit",
    )


def pitch_node(pitches: dict[str, str | None], default: str | None = None) -> Node:
    """Maps each pitch to its tap drill diameter, or None to use the size's."""
    return Node(
        "pitch",
        [
            Value(name, {} if drill is None else {"tapDrillDiameter": string(drill)})
            for name, drill in pitches.items()
        ],
        display_name="Threads/inch",
        default=default,
    )


tapped_hole = Node(
    "size",
    [
        Value(
            "#8",
            {"majorDiameter": inch(0.164), "tapDrillDiameter": string("0.1360 in")},
            pitch_node({"32 tpi (UNC)": None, "36 tpi (UNF)": None}),
        ),
        Value(
            "#10",
            {"majorDiameter": inch(0.19)},
            pitch_node(
                {"24 tpi (UNC)": "0.1495 in", "32 tpi (UNF)": "0.1590 in"},
                default="32 tpi (UNF)",
            ),
        ),
        Value(
            "1/4",
            {"majorDiameter": inch(0.25)},
            pitch_node(
                {
                    "20 tpi (UNC)": "0.2010 in",
                    "28 tpi (UNF)": "0.2130 in",
                    "32 tpi (UNEF)": "0.2187 in",
                }
            ),
        ),
        Value(
            "5/16",
            {"majorDiameter": inch(5 / 16)},
            pitch_node(
                {
                    "18 tpi (UNC)": "0.2570 in",
                    "24 tpi (UNF)": "0.2720 in",
                    "32 tpi (UNEF)": "0.2812 in",
                }
            ),
        ),
    ],
    default="#10",
)

clearance_hole = Node(
    "size",
    [
        Value("#8", next=fit_node("0.1695 in", "0.177 in")),
        Value("#10", next=fit_node("0.196 in", "0.201 in")),
        Value("1/4", next=fit_node("0.257 in", "0.266 in")),
    ],
    default="#10",
)

CONTENTS = [
    Table("tappedHoleTable", tapped_hole),
    Table("clearanceHoleTable", clearance_hole),
]
