"""The 3D print adapters robotPrintAdapter cuts pockets for: the list of them, and their profiles.
Run `uv run fs gen` after editing; adding an adapter only takes an entry in ADAPTERS.

Most adapters are a ring of tabs: a tab has parallel sides, an outer arc, and filleted corners,
and neighboring tabs are joined by a round relief which both sides are tangent to. Vendor
drawings and models are in vendor/.
"""

import dataclasses
import math
import pathlib
from typing import Callable

from fs_cli.gen import Code, Constant, Import
from fs_cli.sketches import MM, ORIGIN, Arc, Entity, Line, Profile, Sketch, polar
from fs_cli.step import StepFile
from fs_cli.tables import Enum, inch, mm, number, string

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

# Bores cut through the print for an adapter's shaft (see robotPrintAdapter's sketchBoreProfile): a hex
# (across flats, with a corner at vertex_angle to match the adapter), or a clearance circle, which
# SplineXS adapters can replace with a SplineXS profile.


def hex_bore(across_flats: float, vertex_angle: float) -> str:
    return f"{{ {string('hexSize')} : {inch(across_flats)}, {string('vertexAngle')} : {number(vertex_angle)} * degree }}"


def circle_bore(diameter_mm: float) -> str:
    return f"{{ {string('diameter')} : {mm(diameter_mm)} }}"


# SplineXS shafts are 8mm across
SPLINE_XS_BORE = f"{{ {string('diameter')} : {mm(8.5)}, {string('splineXs')} : true }}"


@dataclasses.dataclass
class Adapter:
    """A 3D print adapter.

    Attributes:
        vendor: A key of VENDORS.
        value: The adapter's value in its vendor's enum. Enum values are stored in documents, so never
            rename them.
        name: The name shown for the adapter.
        profile: The generated sketch of its outline.
        bore: The bore cut through the print for its shaft (hex_bore, circle_bore, or SPLINE_XS_BORE).
        depth: Its thickness, in inches.
        boss: How tall the boss on one side of it is, in inches, if it has one.
    """

    vendor: str
    value: str
    name: str
    profile: str
    bore: str
    depth: float = 0.25
    boss: float | None = None

    @property
    def splines(self) -> bool:
        return self.bore == SPLINE_XS_BORE


# Every adapter, sorted by vendor and then part number
ADAPTERS = [
    # AndyMark's inserts have a 0.03" boss (see vendor/am-5654), so the toothed part is 0.22" thick
    Adapter("AndyMark", "HEX_INSERT", '1/2" Hex Insert (am-5654)', "ANDYMARK_HEX_INSERT_PROFILE", hex_bore(0.5, 90), boss=0.03),
    Adapter("AndyMark", "HEX_3_8_INSERT", '3/8" Hex Insert (am-5655)', "ANDYMARK_SMALL_INSERT_PROFILE", hex_bore(0.375, 90), boss=0.03),
    # The bore clears the key, which reaches 4.9mm from the center
    Adapter("AndyMark", "KEYED_8MM_INSERT", "8mm Keyed Insert (am-5656)", "ANDYMARK_SMALL_INSERT_PROFILE", circle_bore(10), boss=0.03),
    Adapter("AndyMark", "KRAKEN_INSERT", "Kraken Spline Insert (am-5657)", "ANDYMARK_SMALL_INSERT_PROFILE", SPLINE_XS_BORE, boss=0.03),
    Adapter("Swyft", "HEX_ADAPTER", '1/2" Hex Adapter (SR-HEXto3DPRINT-01)', "SWYFT_HEX_ADAPTER_PROFILE", hex_bore(0.5, 90)),
    Adapter("TTB", "HEX_INSERT", '1/2" Hex Insert (TTB-0034)', "TTB_HEX_INSERT_PROFILE", hex_bore(0.5, 0)),
    # The same shape as WCP's SplineXS adapter
    Adapter("TTB", "SPLINE_INSERT", "SplineXS Insert (TTB-0356)", "WCP_SPLINE_ADAPTER_PROFILE", SPLINE_XS_BORE),
    Adapter("WCP", "SPLINE_ADAPTER", "SplineXS Adapter (WCP-1021)", "WCP_SPLINE_ADAPTER_PROFILE", SPLINE_XS_BORE),
    Adapter("WCP", "HEX_ADAPTER", '1/2" Hex Adapter (WCP-1121)', "WCP_HEX_ADAPTER_PROFILE", hex_bore(0.5, 0)),
]
DEFAULT_VENDOR = "TTB"

# Each vendor: its value in the PrintAdapterVendor enum, its adapter enum, and that enum's parameter
VENDORS = {
    "AndyMark": ("ANDYMARK", "AndyMarkAdapter", "andyMarkAdapter"),
    "Swyft": ("SWYFT", "SwyftAdapter", "swyftAdapter"),
    "TTB": ("TTB", "TtbAdapter", "ttbAdapter"),
    "WCP": ("WCP", "WcpAdapter", "wcpAdapter"),
}

PrintAdapterVendor = Enum(
    "PrintAdapterVendor",
    [value for value, _, _ in VENDORS.values()],
    {value: vendor for vendor, (value, _, _) in VENDORS.items()},
)
vendor_enums = {
    vendor: Enum(
        enum,
        [adapter.value for adapter in ADAPTERS if adapter.vendor == vendor],
        {adapter.value: adapter.name for adapter in ADAPTERS if adapter.vendor == vendor},
    )
    for vendor, (_, enum, _) in VENDORS.items()
}


def vendor_is(vendor: str) -> str:
    return f"definition.adapterVendor == {PrintAdapterVendor[VENDORS[vendor][0]]}"


def ui_predicate(name: str, description: str, chosen: Callable[[Adapter], bool]) -> str:
    """A predicate which is true when the chosen adapter is one of those chosen() picks.

    Preconditions show parameters based on these, rather than on hidden parameters set by editing
    logic (see docs/featurescript-style.md).
    """
    terms = []
    for vendor, (_, _, parameter) in VENDORS.items():
        adapters = [adapter for adapter in ADAPTERS if adapter.vendor == vendor]
        picked = [adapter for adapter in adapters if chosen(adapter)]
        if len(picked) == len(adapters):
            terms.append(vendor_is(vendor))
        elif picked:
            options = " || ".join(
                f"definition.{parameter} == {vendor_enums[vendor][adapter.value]}" for adapter in picked
            )
            terms.append(f"({vendor_is(vendor)} && ({options}))" if len(picked) > 1 else f"({vendor_is(vendor)} && {options})")
    condition = " ||\n        ".join(terms) or "false"
    return f"""/**
 * {description}
 */
export predicate {name}(definition is map)
{{
    {condition};
}}"""


branches = "\n    else ".join(
    f"if ({vendor_is(vendor)})\n"
    f"    {{\n"
    f'        annotation {{ "Name" : "Adapter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }}\n'
    f"        definition.{parameter} is {enum};\n"
    f"    }}"
    for vendor, (_, enum, parameter) in VENDORS.items()
)
selection_predicate = f"""/**
 * The vendor and adapter parameters. Use getPrintAdapter to get the chosen adapter.
 */
export predicate printAdapterSelectionPredicate(definition is map)
{{
    annotation {{ "Name" : "Vendor", "Default" : {PrintAdapterVendor[VENDORS[DEFAULT_VENDOR][0]]}, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }}
    definition.adapterVendor is PrintAdapterVendor;

    {branches}
}}"""


def adapter_data(adapter: Adapter) -> str:
    entries = [
        (string("profile"), adapter.profile),
        (string("depth"), inch(adapter.depth)),
        (string("bore"), adapter.bore),
    ]
    if adapter.boss is not None:
        entries.append((string("boss"), inch(adapter.boss)))
    return "{ " + ", ".join(f"{key} : {value}" for key, value in entries) + " }"


adapters_by_vendor = ",\n".join(
    f"        {PrintAdapterVendor[value]} : {{\n"
    + ",\n".join(
        f"            {vendor_enums[vendor][adapter.value]} : {adapter_data(adapter)}"
        for adapter in ADAPTERS
        if adapter.vendor == vendor
    )
    + "\n        }"
    for vendor, (value, _, _) in VENDORS.items()
)
parameters_by_vendor = ",\n".join(
    f"        {PrintAdapterVendor[value]} : {string(parameter)}" for value, _, parameter in VENDORS.values()
)
get_print_adapter = """/**
 * The adapter chosen by printAdapterSelectionPredicate's parameters: its `profile`, its thickness (`depth`), how
 * tall the boss on one side of it is (`boss`, if it has one), and the `bore` cut through the print for its shaft.
 *
 * A bore is a hex (`hexSize` across flats, with a corner at `vertexAngle`, timed to match the adapter), or a
 * clearance circle (`diameter`), which SplineXS adapters (`splineXs`) can replace with a SplineXS profile.
 */
export function getPrintAdapter(definition is map) returns map
{
    return PRINT_ADAPTERS[definition.adapterVendor][definition[PRINT_ADAPTER_PARAMETERS[definition.adapterVendor]]];
}"""

CONTENTS = [
    Import("core/sketchData.fs"),
    PrintAdapterVendor,
    *vendor_enums.values(),
    Code(selection_predicate),
    Code(
        ui_predicate(
            "printAdapterHasBoss",
            "Whether the chosen adapter has a boss on one side.",
            lambda adapter: adapter.boss is not None,
        )
    ),
    Code(
        ui_predicate(
            "printAdapterHasSplineXsBore",
            "Whether the chosen adapter fits a SplineXS shaft.",
            lambda adapter: adapter.splines,
        )
    ),
    Sketch("WCP_HEX_ADAPTER_PROFILE", Profile(WCP_HEX_ADAPTER, WCP_HEX_ADAPTER_ORDER)),
    Sketch("WCP_SPLINE_ADAPTER_PROFILE", Profile(WCP_SPLINE_ADAPTER, WCP_SPLINE_ADAPTER_ORDER)),
    Sketch("TTB_HEX_INSERT_PROFILE", Profile(TTB_HEX_INSERT, TTB_HEX_INSERT_ORDER)),
    Sketch("ANDYMARK_HEX_INSERT_PROFILE", Profile(ANDYMARK_HEX_INSERT)),
    Sketch("ANDYMARK_SMALL_INSERT_PROFILE", Profile(ANDYMARK_SMALL_INSERT)),
    Sketch("SWYFT_HEX_ADAPTER_PROFILE", Profile(SWYFT_HEX_ADAPTER)),
    # Each vendor's adapter parameter
    Constant("PRINT_ADAPTER_PARAMETERS", "{\n" + parameters_by_vendor + "\n    }"),
    # Every adapter, by vendor and then value
    Constant("PRINT_ADAPTERS", "{\n" + adapters_by_vendor + "\n    }"),
    Code(get_print_adapter),
]
