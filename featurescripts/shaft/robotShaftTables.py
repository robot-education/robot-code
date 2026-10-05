"""Shaft and hole tables for robotShaft and robotSpacer. Run `uv run fs gen` after editing.

The shaft tables list the shafts teams buy, picked by how often they're used in FRCDesign's FRC and FTC libraries
(see docs/cots-research.md). Each is a vendor's shaft, described by its profile in robotShaftCommon.fs's terms
(`shaftType`, and `hexType` and `hexSize` or `splineType`), with its `partName` (after its length, e.g. "3 in.
Rounded Hex Shaft (WCP 1/2 in.)"), `material`, `appearance`, and `stock`: the lengths it's sold in, shortest first
(see nutStripTables.py). Most shafts are cut from stock, but some are only sold in set lengths, with machined ends
(like goBILDA's REX shafts, with e-clip grooves); `fixedLengths` warns when those aren't one of them.
"""

from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, inch, mm, string


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


def shaft(
    name: str,
    part_name: str,
    profile: dict[str, str],
    material: str,
    appearance: str,
    lengths: str,
    fixed_lengths: bool = False,
    predrilled_hole: str | None = None,
) -> Value:
    """A shaft.

    Args:
        fixed_lengths: Whether it's only sold in its stock's lengths, rather than cut from them.
        predrilled_hole: The diameter of a hole through a solid spline shaft (hex shafts' come from their types).
    """
    return Value(
        name,
        {
            **profile,
            "partName": string(part_name),
            "material": material,
            "appearance": appearance,
            "stock": lengths,
            **({"fixedLengths": "true"} if fixed_lengths else {}),
            **({"predrilledHoleDiameter": predrilled_hole} if predrilled_hole else {}),
        },
    )


def hex(hex_type: str, size: str) -> dict[str, str]:
    return {"shaftType": "ShaftType.HEX", "hexType": f"HexType.{hex_type}", "hexSize": size}


def spline(spline_type: str) -> dict[str, str]:
    return {"shaftType": "ShaftType.SPLINE", "splineType": f"SplineType.{spline_type}"}


def vendor(name: str, shafts: list[Value]) -> Value:
    return Value(name, {"vendor": string(name)}, Node("shaft", shafts, display_name="Shaft"))


def one(length: str, part_number: str, url: str) -> str:
    """Stock sold in one length."""
    return stock((length, part_number, url))


def wcp(part_number: str) -> str:
    return f"https://wcproducts.com/products/{part_number.lower()}"


def wcp_shaft(name: str, part_name: str, profile: dict[str, str], part_number: str) -> Value:
    return shaft(name, part_name, profile, "ALUMINUM", "BLACK", one(inch(36), part_number, wcp(part_number)))


# FRCDesign "Hex Shaft (WCP)" and "Spline - SplineXL (WCP)"; 1/2 in. rounded hex is most of their use. Its SplineXS shafts
# aren't in FRCDesign (they're from WCP's store)
WCP = vendor(
    "WCP",
    [
        wcp_shaft("1/2 in. Rounded Hex", "Rounded Hex Shaft (WCP 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "WCP-0914"),
        wcp_shaft("3/8 in. Rounded Hex", "Rounded Hex Shaft (WCP 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS), "WCP-0911"),
        wcp_shaft("1/2 in. Hex", "Hex Shaft (WCP 1/2 in.)", hex("STOCK", HALF_INCH), "WCP-0915"),
        wcp_shaft("3/8 in. Hex", "Hex Shaft (WCP 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "WCP-0912"),
        wcp_shaft("1/2 in. Hex Lite", "Hex Lite Shaft (WCP 1/2 in.)", hex("HEX_LITE", HALF_INCH), "WCP-0917"),
        wcp_shaft("3/8 in. Hex Lite", "Hex Lite Shaft (WCP 3/8 in.)", hex("HEX_LITE", THREE_EIGHTHS), "WCP-1418"),
        shaft("SplineXL", "SplineXL Shaft (WCP)", spline("SPLINE_XL"), "ALUMINUM", "BLACK", one(inch(47), "WCP-0918", wcp("WCP-0918"))),
        # TODO: check whether the SplineXS stock has a through hole (its page has no drawing yet). The stub's ends look
        # tapped, which the shaft's ends can draw
        shaft("SplineXS (aluminum)", "SplineXS Shaft (WCP, aluminum)", spline("SPLINE_XS"), "ALUMINUM", "BLACK",
              one(inch(36), "WCP-1379", wcp("WCP-1379"))),
        shaft("SplineXS Stub (steel)", "SplineXS Stub Shaft (WCP, steel)", spline("SPLINE_XS"), "STEEL", "BLACK",
              one(inch(3), "WCP-0946", wcp("WCP-0946")), fixed_lengths=True),
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
        shaft("1/2 in. Rounded Hex", "Rounded Hex Shaft (REV 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "ALUMINUM", "BLACK",
              one(inch(36), "REV-21-1135", rev("REV-21-1135"))),
        shaft("1/2 in. UltraHex", "UltraHex Shaft (REV 1/2 in.)", hex("ULTRA_HEX", HALF_INCH), "ALUMINUM", "WHITE",
              one(inch(72), "REV-41-3205", rev("REV-41-3205"))),
        shaft("MAXSpline", "MAXSpline Shaft (REV)", spline("MAX_SPLINE"), "ALUMINUM", "WHITE",
              one(inch(47), "REV-21-2520", rev("REV-21-2520"))),
        shaft("SplineXS", "SplineXS Shaft (REV)", spline("SPLINE_XS"), "STAINLESS_STEEL", "WHITE",
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
        shaft("1/2 in. Churro", "Churro Shaft (AndyMark 1/2 in.)", hex("CHURRO", HALF_INCH), "ALUMINUM", "WHITE",
              lengths(AM_CHURRO_URL, (2.48, "am-3399"), (3.375, "am-2569"), (3.875, "am-3087"), (6.25, "am-5724"),
                      (11.25, "am-3398"), (12, "am-3101-1"), (17.313, "am-5218"), (17.8, "am-3101-1780"),
                      (24, "am-3101-2"), (36, "am-3101-3"), (47, "am-3101-4700"))),
        shaft("1/2 in. Hex (7075)", "Hex Shaft (AndyMark 1/2 in., 7075)", hex("STOCK", HALF_INCH), "ALUMINUM_7075", "WHITE",
              lengths(AM_HEX_URL, (12, "am-2291-1"), (47, "am-2291-4700"))),
        shaft("3/8 in. Churro Lite", "Churro Lite Shaft (AndyMark 3/8 in.)", hex("CHURRO", THREE_EIGHTHS), "ALUMINUM",
              "WHITE", lengths(AM_CHURRO_LITE_URL, (10.5, "am-5867"), (36, "am-3666-3"), (47, "am-3666-4700"))),
        shaft("3/8 in. Hex (steel)", "Hex Shaft (AndyMark 3/8 in., steel)", hex("STOCK", THREE_EIGHTHS), "STEEL", "DARK_GRAY",
              lengths(AM_STEEL_HEX_URL, (1.85, "am-2356"), (12, "am-2356-1"), (36, "am-2356-3"), (47, "am-2356-4700"))),
    ],
)

SWYFT_URL = "https://swyftrobotics.com/structure/swyft-axles"

# FRCDesign "Hex Shaft (SWYFT)": nearly all 7075
SWYFT = vendor(
    "Swyft",
    [
        shaft("1/2 in. Rounded Hex (7075)", "Rounded Hex Shaft (Swyft 1/2 in., 7075)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM_7075", "BLACK", one(inch(36), "SR-AXLE-HEX-0.5in-36in-AL7075", SWYFT_URL)),
        shaft("1/2 in. Rounded Hex (6061)", "Rounded Hex Shaft (Swyft 1/2 in., 6061)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM", "BLACK", one(inch(36), "SR-AXLE-HEXtoSPLINE-0.5in-36in-AL6061", SWYFT_URL)),
    ],
)


def vex(part_number: str) -> str:
    return f"https://www.vexrobotics.com/{part_number}.html"


# FRCDesign "Hex Shaft (VEX)": ThunderHex is VEX's rounded hex
VEX = vendor(
    "VEX",
    [
        shaft("1/2 in. ThunderHex", "ThunderHex Shaft (VEX 1/2 in.)", hex("ROUNDED_HEX", HALF_INCH), "ALUMINUM", "BLACK",
              one(inch(36), "217-8631", vex("217-8631"))),
        shaft("3/8 in. ThunderHex", "ThunderHex Shaft (VEX 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS), "ALUMINUM", "BLACK",
              one(inch(36), "217-5837", vex("217-5837"))),
        shaft("1/2 in. Hex", "Hex Shaft (VEX 1/2 in.)", hex("STOCK", HALF_INCH), "ALUMINUM", "WHITE",
              one(inch(36), "217-2753", vex("217-2753"))),
        shaft("3/8 in. Hex", "Hex Shaft (VEX 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "ALUMINUM", "WHITE",
              one(inch(36), "217-2754", vex("217-2754"))),
    ],
)

def ttb(handle: str) -> str:
    return f"https://www.thethriftybot.com/products/{handle}"


TTB_SPLINE_XS_URL = ttb("pre-order-splinexs-shafts")

# A #10-32 tap drill (#21), the hole through shafts which can be tapped #10-32 at their ends
TAP_10_32 = inch(0.159)

# FRCDesign "Hex Shaft (TTB)" and "Spline - 15t SplineXS (TTB)". Its SplineXS stub shafts are 1045 steel, tapped #10-32 at
# each end (draw them with the shaft's ends); its 7075 SplineXS stock has a through hole to tap #10-32, and its steel
# stock doesn't
TTB = vendor(
    "ThriftyBot",
    [
        shaft("1/2 in. Rounded Hex (7075)", "Rounded Hex Shaft (ThriftyBot 1/2 in., 7075)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM_7075", "BLACK",
              one(inch(36), "TTB-0069", ttb("copy-of-qty-1-36-inch-long-1-2-rounded-hex-shaft-7075-aluminum"))),
        shaft("1/2 in. Rounded Hex (6061)", "Rounded Hex Shaft (ThriftyBot 1/2 in., 6061)", hex("ROUNDED_HEX", HALF_INCH),
              "ALUMINUM", "BLACK", one(inch(36), "TTB-0068", ttb("qty-1-36-inch-long-1-2-rounded-hex-shaft-6061-aluminum"))),
        shaft("3/8 in. Rounded Hex", "Rounded Hex Shaft (ThriftyBot 3/8 in.)", hex("ROUNDED_HEX", THREE_EIGHTHS),
              "ALUMINUM", "BLACK", one(inch(36), "TTB-0265", ttb("3-8-rounded-hex-shaft-stock-36-long"))),
        shaft("SplineXS Stub (steel)", "SplineXS Stub Shaft (ThriftyBot, steel)", spline("SPLINE_XS"), "STEEL",
              "DARK_GRAY", lengths(TTB_SPLINE_XS_URL, (2, "TTB-0301"), (2.5, "TTB-0303")), fixed_lengths=True),
        shaft("SplineXS (7075)", "SplineXS Shaft (ThriftyBot, 7075)", spline("SPLINE_XS"), "ALUMINUM_7075", "BLACK",
              one(inch(36), "TTB-0357", TTB_SPLINE_XS_URL), predrilled_hole=TAP_10_32),
        shaft("SplineXS (steel)", "SplineXS Shaft (ThriftyBot, steel)", spline("SPLINE_XS"), "STEEL", "DARK_GRAY",
              one(inch(36), "TTB-0366", TTB_SPLINE_XS_URL)),
    ],
)


def gobilda(series: str, profile: str, name: str, lengths: list[float]) -> str:
    """goBILDA sells each length: SKUs like 2106-4008-0560 for 56 mm, with a page each."""
    return stock(
        *[
            (
                mm(length),
                f"{series}-{profile}-{round(length * 10):04d}" if series != "2104" else f"{series}-{profile}-{round(length):04d}",
                f"https://www.gobilda.com/{name}-{length:g}mm-length/",
            )
            for length in lengths
        ]
    )


REX_8_LENGTHS = [24, 32, 40, 43, 48, 52, 54, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 168, 192, 216, 240, 264, 288,
                 312, 336, 384, 432, 624]
REX_12_LENGTHS = [32, 40, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 168, 192, 216, 240, 264, 288, 312, 336, 384,
                  432, 528, 624]
REX_12_ALUMINUM_LENGTHS = [43, 48, 56, 64, 72, 80, 88, 96, 104, 112, 120, 144, 192, 240, 288, 336, 384, 432, 528, 624,
                           1200]

# FRCDesign FTC "Custom Shaft": 8mm REX is nearly all of its use. REX is goBILDA's rounded hex: an 8 mm (or 12 mm) round
# and a 7 mm (or 11 mm) hex (the STEP files); its ends are tapped M4. The stainless steel ones have e-clip grooves, so
# they're only sold in set lengths; the aluminum one (up to 1200 mm) can be cut
# TODO: goBILDA's D-shafts and round shafts, and 8mm REX in aluminum; REV's 5mm hex
GOBILDA = vendor(
    "goBILDA",
    [
        shaft("8mm REX (stainless steel)", "8mm REX Shaft (goBILDA, stainless steel)", hex("ROUNDED_HEX", "HexSize._7_MM"),
              "STAINLESS_STEEL", "WHITE",
              gobilda("2106", "4008", "8mm-rex-shaft-with-e-clip-stainless-steel", REX_8_LENGTHS), fixed_lengths=True),
        shaft("12mm REX (stainless steel)", "12mm REX Shaft (goBILDA, stainless steel)", hex("ROUNDED_HEX", "HexSize._11_MM"),
              "STAINLESS_STEEL", "WHITE",
              gobilda("2109", "4012", "12mm-rex-shaft-with-e-clip-stainless-steel", REX_12_LENGTHS), fixed_lengths=True),
        shaft("12mm REX (aluminum)", "12mm REX Shaft (goBILDA, aluminum)", hex("ROUNDED_HEX", "HexSize._11_MM"),
              "ALUMINUM", "WHITE",
              gobilda("2104", "0012", "12mm-rex-shaft-aluminum", REX_12_ALUMINUM_LENGTHS)),
    ],
)

# AndyMark's Robits FTC system: 3/8 in. steel hex, tapped #10-32 at each end, in lengths on its grid
ROBITS = vendor(
    "AndyMark",
    [
        shaft("Robits 3/8 in. Hex", "Robits Hex Shaft (AndyMark 3/8 in.)", hex("STOCK", THREE_EIGHTHS), "STEEL", "DARK_GRAY",
              lengths("https://andymark.com/products/robits-hex-shafts", *[(n, f"am-5003-{n * 100:04d}") for n in (2, 3, 4, 6, 8, 10, 12)]),
              fixed_lengths=True),
    ],
)

CONTENTS = [
    Import("shaft/robotShaftCommon.fs"),
    Import("core/robotProperties.fs"),
    Table("tappedHoleTable", tapped_hole),
    Table("clearanceHoleTable", clearance_hole),
    Table("frcShaftTable", Node("vendor", [WCP, REV, ANDYMARK, SWYFT, VEX, TTB])),
    Table("ftcShaftTable", Node("vendor", [GOBILDA, ROBITS])),
]
