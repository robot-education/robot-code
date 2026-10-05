"""The 3D print adapters robotPrintAdapter cuts pockets for: the list of them, and their profiles.
Run `uv run fs gen` after editing.

Most adapters are a ring of tabs: a tab has parallel sides, an outer arc, and filleted corners,
and neighboring tabs are joined by a round relief which both sides are tangent to. Vendor
drawings and models are in vendor/.
"""

import math
import pathlib

from fs_cli.gen import Constant, Import
from fs_cli.sketches import MM, ORIGIN, Arc, Entity, Line, Profile, Sketch, polar
from fs_cli.step import StepFile
from fs_cli.tables import Enum, Node, Table, Value, string

VENDOR = pathlib.Path(__file__).parent / "vendor"


def adapter(
    tabs: int,
    outer_radius: float,
    fillet: float,
    relief_radius: float,
    first_tab: float = 0,
    half_width: float | None = None,
    relief_center: float | None = None,
) -> list[Entity]:
    """The profile of an adapter, given either the tabs' half width or the reliefs' center radius.

    Args:
        first_tab: The angle of the first tab's axis.
    """
    pitch = 360 / tabs
    sine = math.sin(math.radians(pitch / 2))
    if half_width is None:
        assert relief_center is not None
        half_width = relief_center * sine - relief_radius
    else:
        relief_center = (half_width + relief_radius) / sine
    # Fillets are tangent to the outer arc and a side
    along = math.sqrt((outer_radius - fillet) ** 2 - (half_width - fillet) ** 2)

    entities: list[Entity] = []
    for tab in range(tabs):
        angle = first_tab + tab * pitch
        axis, normal = polar(1, angle), polar(1, angle + 90)
        corners = []
        for side in (-1, 1):
            center = axis * along + normal * (side * (half_width - fillet))
            on_side = center + normal * (side * fillet)
            on_outer = center * (outer_radius / (outer_radius - fillet))
            corners.append((center, on_side, on_outer))
        relief_before = polar(relief_center, angle - pitch / 2)
        relief_after = polar(relief_center, angle + pitch / 2)
        (low, low_side, low_outer), (high, high_side, high_outer) = corners
        next_normal = polar(1, angle + pitch + 90)
        entities += [
            Line(relief_before + normal * relief_radius, low_side),
            Arc.between(low, low_side, low_outer),
            Arc.between(ORIGIN, low_outer, high_outer),
            Arc.between(high, high_outer, high_side),
            Line(high_side, relief_after - normal * relief_radius),
            Arc.between(
                relief_after,
                relief_after - normal * relief_radius,
                relief_after + next_normal * relief_radius,
            ),
        ]
    return entities


WCP_HEX_ADAPTER = adapter(
    12, outer_radius=0.5, fillet=0.75 * MM, relief_radius=1.2 * MM, relief_center=10 * MM
)
WCP_SPLINE_ADAPTER = adapter(
    12,
    outer_radius=5 / 16,
    fillet=0.5 * MM,
    # Measured from the original sketch
    relief_radius=0.0330070348,
    first_tab=15,
    half_width=1 / 32,
)
TTB_HEX_INSERT = adapter(12, outer_radius=0.5, fillet=0.02, relief_radius=0.05, half_width=1 / 16)


def andymark_insert(diameter: float, relief_radius: float) -> list[Entity]:
    """AndyMark's 3D print inserts (am-5654 to am-5657): 12 tabs, 0.09 wide with R0.02 corners.

    The drawings don't dimension where the reliefs are, so they're tangent to the tabs like the
    other adapters.
    """
    return adapter(12, outer_radius=diameter / 2, fillet=0.02, relief_radius=relief_radius, half_width=0.045)


ANDYMARK_HEX_INSERT = andymark_insert(1, relief_radius=0.07)  # am-5654
# am-5655 (3/8" hex), am-5656 (8mm keyed), and am-5657 (Kraken spline) share a profile
ANDYMARK_SMALL_INSERT = andymark_insert(0.75, relief_radius=0.04)

# Ten notches around a circle; the notches are splines in Swyft's model
SWYFT_HEX_ADAPTER = StepFile(VENDOR / "SR-HEXto3DPRINT-01_v1.step").profile()


def starting_at(entities: list[Entity], first: int) -> list[int]:
    """An order going around the profile from entities[first]."""
    return [(first + index) % len(entities) for index in range(len(entities))]


# Where the profiles originally started (see Profile)
WCP_HEX_ADAPTER_ORDER = starting_at(WCP_HEX_ADAPTER, 30)
WCP_SPLINE_ADAPTER_ORDER = starting_at(WCP_SPLINE_ADAPTER, 32)
TTB_HEX_INSERT_ORDER = starting_at(TTB_HEX_INSERT, 18)

# Every adapter: its value in the PrintAdapter enum, its vendor, and its name, sorted by vendor
# and then part number. The enum's values are stored in documents, so never rename them.
ADAPTERS = [
    ("ANDYMARK_HEX_INSERT", "AndyMark", '1/2" Hex Insert (am-5654)'),
    ("ANDYMARK_3_8_HEX_INSERT", "AndyMark", '3/8" Hex Insert (am-5655)'),
    ("ANDYMARK_8MM_KEYED_INSERT", "AndyMark", "8mm Keyed Insert (am-5656)"),
    ("ANDYMARK_KRAKEN_INSERT", "AndyMark", "Kraken Spline Insert (am-5657)"),
    ("SWYFT_HEX_ADAPTER", "Swyft", '1/2" Hex Adapter (SR-HEXto3DPRINT-01)'),
    ("TTB_HEX_INSERT", "TTB", '1/2" Hex Insert (TTB-0034)'),
    ("TTB_SPLINE_INSERT", "TTB", "SplineXS Insert (TTB-0356)"),
    ("WCP_SPLINE_ADAPTER", "WCP", "SplineXS Adapter (WCP-1021)"),
    ("WCP_HEX_ADAPTER", "WCP", '1/2" Hex Adapter (WCP-1121)'),
]
VENDORS = list(dict.fromkeys(vendor for _, vendor, _ in ADAPTERS))

PrintAdapter = Enum(
    "PrintAdapter",
    [value for value, _, _ in ADAPTERS],
    {value: f"{vendor} {name}" for value, vendor, name in ADAPTERS},
)

# The adapter parameter: a vendor, then one of its adapters
adapter_table = Node(
    "vendor",
    [
        Value(
            vendor,
            next=Node(
                "adapter",
                [
                    Value(name, {"adapter": PrintAdapter[value]})
                    for value, adapter_vendor, name in ADAPTERS
                    if adapter_vendor == vendor
                ],
            ),
        )
        for vendor in VENDORS
    ],
)

# The table path of each adapter, for features made before the table (which only stored the enum)
paths = "".join(
    f"        {PrintAdapter[value]} : {{ {string('vendor')} : {string(vendor)}, {string('adapter')} : {string(name)} }},\n"
    for value, vendor, name in ADAPTERS
)

CONTENTS = [
    Import("core/sketchData.fs"),
    PrintAdapter,
    Table("PRINT_ADAPTER_TABLE", adapter_table),
    Constant("PRINT_ADAPTER_PATHS", "{\n" + paths + "    }"),
    Sketch("WCP_HEX_ADAPTER_PROFILE", Profile(WCP_HEX_ADAPTER, WCP_HEX_ADAPTER_ORDER)),
    Sketch("WCP_SPLINE_ADAPTER_PROFILE", Profile(WCP_SPLINE_ADAPTER, WCP_SPLINE_ADAPTER_ORDER)),
    Sketch("TTB_HEX_INSERT_PROFILE", Profile(TTB_HEX_INSERT, TTB_HEX_INSERT_ORDER)),
    Sketch("ANDYMARK_HEX_INSERT_PROFILE", Profile(ANDYMARK_HEX_INSERT)),
    Sketch("ANDYMARK_SMALL_INSERT_PROFILE", Profile(ANDYMARK_SMALL_INSERT)),
    Sketch("SWYFT_HEX_ADAPTER_PROFILE", Profile(SWYFT_HEX_ADAPTER)),
]
