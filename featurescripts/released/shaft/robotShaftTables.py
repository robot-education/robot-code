"""Shaft and hole tables for robotShaft and robotSpacer. Run `uv run fs gen` after editing.

The shaft tables list the shafts teams buy, picked by how often they're used in FRCDesign's FRC and FTC libraries
(see docs/cots-research.md). Each is a vendor's shaft, described by its profile in robotShaftCommon.fs's terms
(`shaftType`, and `hexType` and `hexSize` or `splineType`), with its `partName` (after its length, e.g. "3 in.
Rounded Hex Shaft (WCP 1/2 in.)"), `material`, `appearance`, and `stock`: the lengths it's sold in, shortest first
(see nutStripTables.py). Most shafts are cut from stock, but some are only sold in set lengths, with machined ends
(like goBILDA's REX shafts, with e-clip grooves): those are chosen by their Length, and built as they're sold (see
`presets`).
"""

import dataclasses

from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, inch, mm, string


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

def fit_node(close: str, free: str) -> Node:
    """A fastener size's clearance holes, by fit: std's (`ANSI_V2ClearanceHoleTable`'s Close and Free)."""
    return Node(
        "fit",
        [
            Value("Free", {"holeDiameter": string(free)}),
            Value("Close", {"holeDiameter": string(close)}),
        ],
        display_name="Fastener fit",
        default="Free",
    )


# Clearance holes, by fastener size and fit
clearance_hole = Node(
    "size",
    [
        Value("#8", next=fit_node("0.1695 in", "0.177 in")),
        Value("#10", next=fit_node("0.196 in", "0.201 in")),
        Value("1/4", next=fit_node("0.257 in", "0.266 in")),
    ],
    default="#10",
)


# Shafts

HALF_INCH = "HexSize._1_2_IN"
THREE_EIGHTHS = "HexSize._3_8_IN"


def stock(*lengths: tuple[str, str, str]) -> str:
    """Lengths it's sold in: (length, part number, url)."""
    entries = ", ".join(
        f"{{ {string('length')} : {length}, {string('partNumber')} : {string(part_number)}, {string('url')} : {string(url)} }}"
        for length, part_number, url in lengths
    )
    return f"[{entries}]"


@dataclasses.dataclass
class Shaft:
    """A vendor's shaft: its type (as the vendor names it), its size (a hex's; None for a spline), its material (as
    it's told apart from the vendor's other shafts of the type and size, if they're in several), its values, and the
    lengths it's chosen from as it's sold (see `presets`), if it is."""

    type: str
    size: str | None
    material: str | None
    values: dict[str, str]
    presets: Node | None = None


@dataclasses.dataclass
class Preset:
    """A length a shaft is sold in: its name (`48 mm`, `2 in.`), its length in millimeters, its length as a
    FeatureScript expression, and its stock (one entry)."""

    name: str
    millimeters: float
    length: str
    stock: str


def presets(lengths: list[Preset], factory: dict[str, str]) -> Node:
    """A shaft's lengths as it's sold, to choose from, the nearest to 56 mm by default. Each is built as it's sold: its
    `presetLength`, and its `factory` features (see `factory`), with nothing to choose about its ends; it's a custom
    shaft that's cut to length, or has its ends modified."""
    default = min(lengths, key=lambda preset: abs(preset.millimeters - 56)).name
    return Node(
        "length",
        [Value(preset.name, {"presetLength": preset.length, "stock": preset.stock, **factory}) for preset in lengths],
        display_name="Length",
        default=default,
    )


def inch_presets(url: str, *sizes: tuple[float, str]) -> list[Preset]:
    """Lengths sold in inches, each (length, part number), all on one page."""
    return [Preset(f"{length:g} in.", length * 25.4, inch(length), stock((inch(length), part_number, url))) for length, part_number in sizes]


# Tapped #10-32 at each end (std's hole table's names), `depth` deep
def tapped_10_32(depth: str) -> tuple[bool, str, str, str]:
    return (False, "#10", "32 tpi (UNF)", depth)


def factory(hole: tuple[bool, str, str, str] | None, grooves: list[tuple[float, float, float]]) -> dict[str, str]:
    """A shaft's features as it's made: a tapped hole in each end, (metric, size, pitch, depth), with its size and pitch
    as std's hole tables name them (None for none), and e-clip grooves, each (diameter, start, end), in millimeters,
    measured from its first end."""
    values = {
        "factoryGrooves": "["
        + ", ".join(
            f"{{ {string('diameter')} : {mm(diameter)}, {string('start')} : {mm(start)}, {string('end')} : {mm(end)} }}"
            for diameter, start, end in grooves
        )
        + "]"
    }
    if hole is not None:
        metric, size, pitch, depth = hole
        values["factoryHole"] = (
            f"{{ {string('metric')} : {'true' if metric else 'false'}, {string('size')} : {string(size)}, "
            f"{string('pitch')} : {string(pitch)}, {string('depth')} : {depth} }}"
        )
    return values


def shaft(
    shaft_type: str,
    size: str | None,
    material_name: str | None,
    part_name: str,
    profile: dict[str, str],
    material: str,
    appearance: str,
    lengths: str | None,
    predrilled_hole: str | None = None,
    preset_lengths: Node | None = None,
) -> Shaft:
    """A shaft.

    Args:
        material_name: Its material's name, which is shown only where the vendor sells the type and size in several.
        predrilled_hole: The diameter of a hole through a solid spline shaft (hex shafts' come from their types).
        preset_lengths: Its lengths as it's sold, to choose from (see `presets`).
    """
    return Shaft(
        shaft_type,
        size,
        material_name,
        {
            **profile,
            "partName": string(part_name),
            "material": material,
            "appearance": appearance,
            # Preset lengths each have their own
            **({"stock": lengths} if preset_lengths is None else {}),
            **({"predrilledHoleDiameter": predrilled_hole} if predrilled_hole else {}),
        },
        preset_lengths,
    )


def hex(hex_type: str, size: str) -> dict[str, str]:
    return {"shaftType": "ShaftType.HEX", "hexType": f"HexType.{hex_type}", "hexSize": size}


def spline(spline_type: str) -> dict[str, str]:
    return {"shaftType": "ShaftType.SPLINE", "splineType": f"SplineType.{spline_type}"}


def vendor(name: str, shafts: list[Shaft]) -> Value:
    """A vendor's shafts, chosen by type, then size (for a hex), then material (only where the type and size come in
    several), in the order they're listed."""
    return Value(name, {"vendor": string(name)}, Node("type", _choices(shafts, "type"), display_name="Type"))


def _choices(shafts: list[Shaft], level: str) -> list[Value]:
    """The options at `level` (type, size, or material) for `shafts`, each leading to the next level, or a leaf."""
    groups: dict[str, list[Shaft]] = {}
    for item in shafts:
        groups.setdefault(getattr(item, level), []).append(item)
    choices = []
    for option, group in groups.items():
        following = _next_level(group, level)
        if following is None:
            assert len(group) == 1, f"{option} needs a size or material to tell its shafts apart"
            choices.append(Value(option, group[0].values, group[0].presets))
        else:
            choices.append(Value(option, {}, Node(following, _choices(group, following), display_name=following.capitalize())))
    return choices


def _next_level(group: list[Shaft], level: str) -> str | None:
    """The level after `level` for shafts which share it: size for hexes, then material, if there are several."""
    if level == "type" and group[0].size is not None:
        return "size"
    if len(group) > 1:
        return "material"
    return None


def one(length: str, part_number: str, url: str) -> str:
    """Stock sold in one length."""
    return stock((length, part_number, url))


# A #10-32 tap drill (#21), the hole through shafts which can be tapped #10-32 at their ends
TAP_10_32 = inch(0.159)

# SplineXS stub shafts are tapped #10-32 1/2 in. deep at each end, "for a 3/8 in. long bolt" (ThriftyBot's page)
SPLINE_XS_STUB_FACTORY = factory(tapped_10_32(inch(0.5)), [])


def wcp(part_number: str) -> str:
    return f"https://wcproducts.com/products/{part_number.lower()}"


def wcp_shaft(shaft_type: str, size: str, part_name: str, profile: dict[str, str], part_number: str) -> Shaft:
    return shaft(shaft_type, size, None, part_name, profile, "ALUMINUM", "BLACK", one(inch(36), part_number, wcp(part_number)))


# FRCDesign "Hex Shaft (WCP)" and "Spline - SplineXL (WCP)"; 1/2 in. rounded hex is most of their use. Its SplineXS shafts
# aren't in FRCDesign (they're from WCP's store)
WCP = vendor(
    "WCP",
    [
        wcp_shaft("Rounded hex", "1/2 in.", "Rounded Hex Shaft (WCP 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "WCP-0914"),
        wcp_shaft("Rounded hex", "3/8 in.", "Rounded Hex Shaft (WCP 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS), "WCP-0911"),
        wcp_shaft("Hex", "1/2 in.", "Hex Shaft (WCP 1/2 in.)", hex("STOCK", HALF_INCH), "WCP-0915"),
        wcp_shaft("Hex", "3/8 in.", "Hex Shaft (WCP 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "WCP-0912"),
        wcp_shaft("Hex Lite", "1/2 in.", "Hex Lite Shaft (WCP 1/2 in.)", hex("HEX_LITE", HALF_INCH), "WCP-0917"),
        wcp_shaft("Hex Lite", "3/8 in.", "Hex Lite Shaft (WCP 3/8 in.)", hex("HEX_LITE", THREE_EIGHTHS), "WCP-1418"),
        shaft("SplineXL", None, None, "SplineXL Shaft (WCP)", spline("SPLINE_XL"), "ALUMINUM", "BLACK", one(inch(47), "WCP-0918", wcp("WCP-0918"))),
        # Its page has no drawing yet; assumed to have a through hole to tap #10-32, like ThriftyBot's 7075 stock. The
        # stub's ends look tapped, which the shaft's ends can draw
        shaft("SplineXS", None, "Aluminum", "SplineXS Shaft (WCP, aluminum)", spline("SPLINE_XS"), "ALUMINUM", "BLACK",
              one(inch(36), "WCP-1379", wcp("WCP-1379")), predrilled_hole=TAP_10_32),
        shaft("SplineXS stub", None, "Steel", "SplineXS Stub Shaft (WCP, steel)", spline("SPLINE_XS"), "STEEL", "BLACK", None,
              preset_lengths=presets(inch_presets(wcp("WCP-0946"), (3, "WCP-0946")), SPLINE_XS_STUB_FACTORY)),
    ],
)


def rev(part_number: str) -> str:
    """REV's page for a part, or a search for it, as some parts' pages are only reachable from their families'."""
    if part_number in ("REV-41-3205", "REV-41-6457"):
        return f"https://www.revrobotics.com/{part_number.lower()}/"
    return f"https://www.revrobotics.com/search.php?search_query={part_number}&section=product"


# FRCDesign "Hex Shaft (REV)", "Spline - MAXSpline (REV)", and "Spline - 15t SplineXS (REV)". The SplineXS shaft is solid
# stainless steel, for cutting (REV-41-6457-DR.pdf)
REV = vendor(
    "REV",
    [
        shaft("Rounded hex", "1/2 in.", None, "Rounded Hex Shaft (REV 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "ALUMINUM", "BLACK",
              one(inch(36), "REV-21-1135", rev("REV-21-1135"))),
        shaft("UltraHex", "1/2 in.", None, "UltraHex Shaft (REV 1/2 in.)", hex("ULTRA_HEX", HALF_INCH), "ALUMINUM", "MEDIUM_GRAY",
              one(inch(72), "REV-41-3205", rev("REV-41-3205"))),
        shaft("MAXSpline", None, None, "MAXSpline Shaft (REV)", spline("MAX_SPLINE"), "ALUMINUM", "MEDIUM_GRAY",
              one(inch(47), "REV-21-2520", rev("REV-21-2520"))),
        shaft("SplineXS", None, None, "SplineXS Shaft (REV)", spline("SPLINE_XS"), "STAINLESS_STEEL", "STEEL_GRAY",
              one(mm(480), "REV-41-6457", rev("REV-41-6457"))),
    ],
)


AM_CHURRO_URL = "https://andymark.com/products/1-2-in-churro-different-lengths"
AM_CHURRO_LITE_URL = "https://andymark.com/products/3-8-in-churro-lite-different-lengths"
AM_HEX_URL = "https://andymark.com/products/0-5-in-7075-aluminum-hex-shaft-stock"
AM_STEEL_HEX_URL = "https://andymark.com/products/3-8-in-steel-hex-shaft-stock"


def lengths(url: str, *sizes: tuple[float, str]) -> str:
    """Stock sold in several lengths (in inches), each (length, part number), all on one page."""
    return stock(*[(inch(length), part_number, url) for length, part_number in sizes])


# FRCDesign "Hex Shaft (AM)": mostly 1/2 in. churro, then 1/2 in. hex. Lengths from AndyMark's store; its 1/2 in. steel
# and 3/8 in. 7075 hex (am-0856, am-3807) aren't sold anymore
ANDYMARK = vendor(
    "AndyMark",
    [
        shaft("Churro", "1/2 in.", None, "Churro Shaft (AndyMark 1/2 in.)", hex("CHURRO", HALF_INCH), "ALUMINUM", "MEDIUM_GRAY",
              lengths(AM_CHURRO_URL, (2.48, "am-3399"), (3.375, "am-2569"), (3.875, "am-3087"), (6.25, "am-5724"),
                      (11.25, "am-3398"), (12, "am-3101-1"), (17.313, "am-5218"), (17.8, "am-3101-1780"),
                      (24, "am-3101-2"), (36, "am-3101-3"), (47, "am-3101-4700"))),
        shaft("Hex", "1/2 in.", "7075", "Hex Shaft (AndyMark 1/2 in., 7075)", hex("STOCK", HALF_INCH), "ALUMINUM_7075", "MEDIUM_GRAY",
              lengths(AM_HEX_URL, (12, "am-2291-1"), (47, "am-2291-4700"))),
        shaft("Churro Lite", "3/8 in.", None, "Churro Lite Shaft (AndyMark 3/8 in.)", hex("CHURRO", THREE_EIGHTHS), "ALUMINUM",
              "MEDIUM_GRAY", lengths(AM_CHURRO_LITE_URL, (10.5, "am-5867"), (36, "am-3666-3"), (47, "am-3666-4700"))),
        shaft("Hex", "3/8 in.", "Steel", "Hex Shaft (AndyMark 3/8 in., steel)", hex("STOCK", THREE_EIGHTHS), "STEEL", "STEEL_GRAY",
              lengths(AM_STEEL_HEX_URL, (1.85, "am-2356"), (12, "am-2356-1"), (36, "am-2356-3"), (47, "am-2356-4700"))),
    ],
)

SWYFT_URL = "https://swyftrobotics.com/structure/swyft-axles"

# FRCDesign "Hex Shaft (SWYFT)": nearly all 7075
SWYFT = vendor(
    "Swyft",
    [
        shaft("Rounded hex", "1/2 in.", "7075", "Rounded Hex Shaft (Swyft 1/2 in., 7075)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM_7075", "BLACK", one(inch(36), "SR-AXLE-HEX-0.5in-36in-AL7075", SWYFT_URL)),
        shaft("Rounded hex", "1/2 in.", "6061", "Rounded Hex Shaft (Swyft 1/2 in., 6061)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM", "BLACK", one(inch(36), "SR-AXLE-HEXtoSPLINE-0.5in-36in-AL6061", SWYFT_URL)),
    ],
)


def vex(part_number: str) -> str:
    return f"https://www.vexrobotics.com/{part_number}.html"


# FRCDesign "Hex Shaft (VEX)": ThunderHex is VEX's rounded hex
VEX = vendor(
    "VEX",
    [
        shaft("ThunderHex", "1/2 in.", None, "ThunderHex Shaft (VEX 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "ALUMINUM", "BLACK",
              one(inch(36), "217-8631", vex("217-8631"))),
        shaft("ThunderHex", "3/8 in.", None, "ThunderHex Shaft (VEX 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS), "ALUMINUM", "BLACK",
              one(inch(36), "217-5837", vex("217-5837"))),
        shaft("Hex", "1/2 in.", None, "Hex Shaft (VEX 1/2 in.)", hex("STOCK", HALF_INCH), "ALUMINUM", "MEDIUM_GRAY",
              one(inch(36), "217-2753", vex("217-2753"))),
        shaft("Hex", "3/8 in.", None, "Hex Shaft (VEX 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "ALUMINUM", "MEDIUM_GRAY",
              one(inch(36), "217-2754", vex("217-2754"))),
    ],
)

def ttb(handle: str) -> str:
    return f"https://www.thethriftybot.com/products/{handle}"


TTB_SPLINE_XS_URL = ttb("pre-order-splinexs-shafts")

# FRCDesign "Hex Shaft (TTB)" and "Spline - 15t SplineXS (TTB)". Its SplineXS stub shafts are 1045 steel, tapped #10-32 at
# each end; its 7075 SplineXS stock "includes a thru hole for tapping to 10-32" (its ends aren't tapped), and its steel
# stock "does not include a thru hole"
TTB = vendor(
    "ThriftyBot",
    [
        shaft("Rounded hex", "1/2 in.", "7075", "Rounded Hex Shaft (ThriftyBot 1/2 in., 7075)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM_7075", "BLACK",
              one(inch(36), "TTB-0069", ttb("copy-of-qty-1-36-inch-long-1-2-rounded-hex-shaft-7075-aluminum"))),
        shaft("Rounded hex", "1/2 in.", "6061", "Rounded Hex Shaft (ThriftyBot 1/2 in., 6061)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM", "BLACK", one(inch(36), "TTB-0068", ttb("qty-1-36-inch-long-1-2-rounded-hex-shaft-6061-aluminum"))),
        shaft("Rounded hex", "3/8 in.", None, "Rounded Hex Shaft (ThriftyBot 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS),
              "ALUMINUM", "BLACK", one(inch(36), "TTB-0265", ttb("3-8-rounded-hex-shaft-stock-36-long"))),
        shaft("SplineXS", None, "7075", "SplineXS Shaft (ThriftyBot, 7075)", spline("SPLINE_XS"), "ALUMINUM_7075", "BLACK",
              one(inch(36), "TTB-0357", TTB_SPLINE_XS_URL), predrilled_hole=TAP_10_32),
        shaft("SplineXS", None, "Steel", "SplineXS Shaft (ThriftyBot, steel)", spline("SPLINE_XS"), "STEEL", "STEEL_GRAY",
              one(inch(36), "TTB-0366", TTB_SPLINE_XS_URL)),
        shaft("SplineXS stub", None, "Steel", "SplineXS Stub Shaft (ThriftyBot, steel)", spline("SPLINE_XS"), "STEEL",
              "STEEL_GRAY", None,
              preset_lengths=presets(inch_presets(TTB_SPLINE_XS_URL, (2, "TTB-0301"), (2.5, "TTB-0303")), SPLINE_XS_STUB_FACTORY)),
    ],
)


def gobilda(series: str, profile: str, name: str, lengths: list[float]) -> str:
    """goBILDA sells each length: SKUs like 2106-4008-0560 for 56 mm, with a page each (2102 and 2104 are numbered by
    the millimeter, not the tenth)."""
    return stock(
        *[
            (
                mm(length),
                f"{series}-{profile}-{round(length * 10):04d}" if series not in ("2102", "2104") else f"{series}-{profile}-{round(length):04d}",
                f"https://www.gobilda.com/{name}-{length:g}mm-length/",
            )
            for length in lengths
        ]
    )


def gobilda_presets(series: str, profile: str, name: str, lengths: list[float]) -> list[Preset]:
    """goBILDA's lengths (in millimeters), each its own SKU (see `gobilda`)."""
    return [Preset(f"{length:g} mm", length, mm(length), gobilda(series, profile, name, [length])) for length in lengths]


REX_8_LENGTHS = [24, 32, 40, 43, 48, 52, 54, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 168, 192, 216, 240, 264, 288,
                 312, 336, 384, 432, 624]
REX_12_LENGTHS = [32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 168, 192, 216, 240, 264, 288, 312, 336, 384,
                  432, 528, 624]
REX_12_ALUMINUM_LENGTHS = [43, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 192, 240, 288, 336, 384, 432, 528, 624,
                           1200]
REX_12_PLAIN_LENGTHS = [100, 140, 180]


def tapped_m4(depth: float) -> tuple[bool, str, str, str]:
    """Tapped M4 (coarse) at each end, `depth` millimeters deep."""
    return (True, "M4", "0.70 mm (Coarse)", mm(depth))


# From goBILDA's STEP files. 8mm REX with an e-clip: an e-clip groove 6 mm across, 3.2 to 4 mm from one end; 12mm: two,
# 9 mm across, 3 to 4 and 10 to 11 mm from one end (the e-clip goes in either). The others have no grooves, and the
# plain stainless 12mm ones no holes
REX_8_ECLIP_FACTORY = factory(tapped_m4(11), [(6, 3.2, 4)])
REX_12_ECLIP_FACTORY = factory(tapped_m4(12), [(9, 3, 4), (9, 10, 11)])
REX_12_ALUMINUM_FACTORY = factory(tapped_m4(14), [])
REX_12_PLAIN_FACTORY = factory(None, [])

# FRCDesign FTC "Custom Shaft": 8mm REX is nearly all of its use. REX is goBILDA's rounded hex: an 8 mm (or 12 mm) round
# and a 7 mm (or 11 mm) hex (the STEP files). goBILDA sells each length: its whole stainless steel catalog (with an
# e-clip and its grooves, and the 12 mm without, untapped) and its aluminum 12 mm
# (https://www.gobilda.com/stainless-steel-rex-shafting/, https://www.gobilda.com/aluminum-rex-shafting/). It only
# sells REX, so its types are its sizes, with an e-clip or without, which the precondition shows Flip ends for. It
# sells no 8mm REX shaft without one (only standoffs)
# TODO: goBILDA's D-shafts and round shafts; REV's 5mm hex. 8mm REX standoffs (aluminum) are left out: they're standoffs
GOBILDA = vendor(
    "goBILDA",
    [
        shaft("8 mm REX (e-clip)", None, None, "8mm REX Shaft with E-Clip (goBILDA)", hex("ROUNDED_HEX", "HexSize._7_MM"),
              "STAINLESS_STEEL", "STEEL_GRAY", None,
              preset_lengths=presets(gobilda_presets("2106", "4008", "8mm-rex-shaft-with-e-clip-stainless-steel", REX_8_LENGTHS),
                                     REX_8_ECLIP_FACTORY)),
        shaft("12 mm REX (e-clip)", None, None, "12mm REX Shaft with E-Clip (goBILDA)", hex("ROUNDED_HEX", "HexSize._11_MM"),
              "STAINLESS_STEEL", "STEEL_GRAY", None,
              preset_lengths=presets(gobilda_presets("2109", "4012", "12mm-rex-shaft-with-e-clip-stainless-steel", REX_12_LENGTHS),
                                     REX_12_ECLIP_FACTORY)),
        shaft("12 mm REX", None, "Stainless steel", "12mm REX Shaft (goBILDA, stainless steel)", hex("ROUNDED_HEX", "HexSize._11_MM"),
              "STAINLESS_STEEL", "STEEL_GRAY", None,
              preset_lengths=presets(gobilda_presets("2102", "0012", "12mm-rex-shaft-stainless-steel", REX_12_PLAIN_LENGTHS),
                                     REX_12_PLAIN_FACTORY)),
        shaft("12 mm REX", None, "Aluminum", "12mm REX Shaft (goBILDA, aluminum)", hex("ROUNDED_HEX", "HexSize._11_MM"),
              "ALUMINUM", "MEDIUM_GRAY", None,
              preset_lengths=presets(gobilda_presets("2104", "0012", "12mm-rex-shaft-aluminum", REX_12_ALUMINUM_LENGTHS),
                                     REX_12_ALUMINUM_FACTORY)),
    ],
)

# AndyMark's Robits FTC system: 3/8 in. steel hex, in lengths on its grid, tapped #10-32 0.7 in. deep at each end (its
# STEP files)
ROBITS = vendor(
    "AndyMark",
    [
        shaft("3/8 in. Robits hex", None, None, "Robits Hex Shaft (AndyMark 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "STEEL", "STEEL_GRAY",
              None,
              preset_lengths=presets(inch_presets("https://andymark.com/products/robits-hex-shafts",
                                                  *[(n, f"am-5003-{n * 100:04d}") for n in (2, 3, 4, 6, 8, 10, 12)]),
                                     factory(tapped_10_32(inch(0.7)), []))),
    ],
)

CONTENTS = [
    Import("released/shaft/robotShaftCommon.fs"),
    Import("core/robotProperties.fs"),
    Table("tappedHoleTable", tapped_hole),
    Table("clearanceHoleTable", clearance_hole),
    Table("frcShaftTable", Node("vendor", [WCP, REV, ANDYMARK, SWYFT, VEX, TTB])),
    Table("ftcShaftTable", Node("vendor", [GOBILDA, ROBITS])),
]
