"""Tables for robotBearingHat. Run `uv run fs gen-tables` after editing."""

from fs_cli.tables import Node, Table, Value, inch, string

BORES = {"0.875 in": 0.875, "1.125 in": 1.125}
# The hat widths available for each bore, in inches
WIDTHS = {"0.875 in": [1.25, 2], "1.125 in": [1.5, 2]}


def bore_node(with_widths: bool) -> Node:
    return Node(
        "bore",
        [
            Value(
                name,
                {"boreDiameter": inch(diameter)},
                width_node(WIDTHS[name]) if with_widths else None,
            )
            for name, diameter in BORES.items()
        ],
        default="1.125 in",
    )


def width_node(widths: list[float]) -> Node:
    return Node(
        "width",
        [Value(f"{width:g} in", {"width": inch(width)}) for width in widths],
        default=f"{widths[-1]:g} in",
    )


def fit_node(values: dict[str, str] | None = None) -> Node:
    """The fit of a #10 fastener, plus any other values."""
    return Node(
        "fit",
        [
            Value("Close", {"holeDiameter": string("0.196 in"), **(values or {})}),
            Value("Free", {"holeDiameter": string("0.201 in"), **(values or {})}),
        ],
        display_name="Fastener fit",
    )


# #10 tap drill diameters, by pitch
TAP_DRILLS = {"24 tpi (UNC)": "0.1495 in", "32 tpi (UNF)": "0.1590 in"}


def thread_node(with_fit: bool) -> Node:
    """Without a fit (a COTS hat's single hole), the hole is just the tap drill diameter."""
    return Node(
        "pitch",
        [
            Value(
                name,
                {} if with_fit else {"holeDiameter": string(drill)},
                fit_node({"tapDrillDiameter": string(drill)}) if with_fit else None,
            )
            for name, drill in TAP_DRILLS.items()
        ],
        display_name="Threads/inch",
        default="32 tpi (UNF)",
    )


def hole_node(tapped: Node) -> Node:
    return Node(
        "holeType",
        [Value("Clearance", next=fit_node()), Value("Tapped", next=tapped)],
        display_name="Hole type",
    )


CONTENTS = [
    Table("bearingHatTable", bore_node(with_widths=True)),
    Table("boreTable", bore_node(with_widths=False)),
    Table("holeTable", hole_node(thread_node(with_fit=True))),
    Table("singleClearanceHoleTable", fit_node()),
    Table("singleHoleTable", hole_node(thread_node(with_fit=False))),
]
