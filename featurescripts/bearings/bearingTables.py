"""FRCDesign's library bearings, generated for FRCDesignApp: flanged and radial ball bearings, by vendor, type, bore,
size, dimensions, and seal, FRC's (`FRC`) and FTC's (`FTC`, whose Part Studio ftcBearingTables.py configures as a part
set of its own). Run `uv run fs gen` after editing.

This is the source of three things, which must agree:

- bearingTables.gen.fs: the enums and data the bearing feature builds from.
- The configuration of the Part Studio exposing it (`STUDIOS`), built by `fs build bearings` and released by
  `fs release bearings`: an input per feature parameter, laid out by `LookupTable` from what's made, so each
  input offers only what the vendor makes with the choices before it, and shows only where that's more than one.
- bearings.gen.json: the same catalog for FRCDesign, which inserts the studio with a configuration of these option
  ids. FRCDesign stores those configurations in documents, so never rename or remove a released option id; add new
  ones.

What each vendor makes, and its part numbers, come from FRCDesign's own Flanged Radial Bearing and Radial Bearing
(vendor/frcdesign-bearings.json, a snapshot of their configurations from FRCDesign's public API), by each record's
Dimensions option, which names its bore, outer diameter, width, and seal, but for parts their vendors no longer sell
(`DISCONTINUED`). McMaster-Carr's come from vendor/mcmaster-bearings.json, transcribed by hand. Generic offers every
bearing any vendor makes, with the bearing's designation (FR8ZZ) as its part number where FRCDesign gives one.

FTC's are goBILDA's (the only bearings in ConfigLib), REV's from its FTC catalog, and AndyMark's its store tags for FTC,
from their stores and STEP files (vendor/, measured for their flanges and bores).

Flanges come from REV's drawings, AndyMark's listed specifications, and goBILDA's STEP files (vendor/), or else the
designation's standard dimensions. A bore is its shaft's nominal size: 13.75 mm and
10.25 mm across a rounded hex's corners, as Robot shaft rounds its shafts.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from fractions import Fraction
from typing import Callable

from fs_cli.configured_studios import ConfiguredStudio, Input, LookupTable, Option, read_locations
from fs_cli.gen import Code, Constant, Import
from fs_cli.tables import Enum, inch, mm, string

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-bearings.json"
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text())

INCH = 0.0254


# Vendors, by how often FRCDesign's bearings are inserted from each

BearingVendor = Enum(
    "BearingVendor",
    ["GENERIC", "WCP", "ANDYMARK", "REV", "THRIFTYBOT", "SWYFT", "SDS", "MCMASTER", "GOBILDA"],
    {
        "GENERIC": "Generic",
        "WCP": "WCP",
        "ANDYMARK": "AndyMark",
        "REV": "REV",
        "THRIFTYBOT": "ThriftyBot",
        "SWYFT": "Swyft",
        "SDS": "SDS",
        "MCMASTER": "McMaster-Carr",
        "GOBILDA": "goBILDA",
    },
)
# FRCDesign's names for them, its Vendor options'
_VENDOR_NAMES = {
    "Generic": "GENERIC",
    "West Coast Products": "WCP",
    "AndyMark": "ANDYMARK",
    "REV Robotics": "REV",
    # VEX no longer sells bearings, so its records are left out
    "VEX Robotics": "VEX",
    "The Thrifty Bot": "THRIFTYBOT",
    "SWYFT Robotics": "SWYFT",
    "Swerve Drive Specialties": "SDS",
}

BearingType = Enum("BearingType", ["FLANGED", "RADIAL"], {"FLANGED": "Flanged", "RADIAL": "Radial"})

BearingBore = Enum(
    "BearingBore",
    ["HEX", "ROUNDED_HEX", "ROUND", "REX"],
    {"HEX": "Hex", "ROUNDED_HEX": "Rounded hex", "ROUND": "Round", "REX": "REX"},
)

BearingSeal = Enum("BearingSeal", ["SHIELDED", "SEALED", "OPEN"], {"SHIELDED": "Shielded", "SEALED": "Sealed", "OPEN": "Open"})
# Each bearing's material, as robotProperties.fs's constant and as Onshape names it: steel, but some of McMaster-Carr's
# 440C stainless
MATERIALS = {"STEEL": "Steel", "STAINLESS_STEEL": "Stainless Steel"}



# Sizes: a bore's nominal size, 1/2 in and 3/8 in first, then the rest from smallest


@dataclasses.dataclass(frozen=True)
class Size:
    id: str
    name: str
    # Across a hex's or square's flats, or a round bore's diameter, in its unit
    value: float
    unit: str = "in"

    @property
    def meters(self) -> float:
        return round(self.value * (INCH if self.unit == "in" else 0.001), 9)


SIZES = [
    Size("IN_1_2", "1/2 in", 0.5),
    Size("IN_3_8", "3/8 in", 0.375),
    Size("IN_3_16", "3/16 in", 0.1875),
    # REV's #10 clearance bearing (4.8 mm) and R166ZZEE (0.190 in)
    Size("NO_10", "#10 (0.19 in)", 0.19),
    Size("IN_1_4", "1/4 in", 0.25),
    Size("IN_5_8", "5/8 in", 0.625),
    Size("IN_3_4", "3/4 in", 0.75),
    *(Size(f"MM_{value}", f"{value} mm", value, "mm") for value in (4, 5, 6, 7, 8, 9, 10, 12, 15, 17, 25, 30, 32, 35, 80, 120)),
]
SIZES_BY_ID = {size.id: size for size in SIZES}
BearingSize = Enum("BearingSize", [size.id for size in SIZES], {size.id: size.name for size in SIZES})

# The diameter a rounded hex's corners are rounded to, as Robot shaft rounds its shafts
ROUNDED_DIAMETERS = {("ROUNDED_HEX", "IN_1_2"): mm(13.75), ("ROUNDED_HEX", "IN_3_8"): mm(10.25)}
# A bore across its flats where it isn't its size: goBILDA's 8 mm REX bearing's is a 7 mm hex, its corners unrounded
# (1611-0514-4008's STEP file)
BORE_FLATS = {("REX", "MM_8"): mm(7)}


# Dimensions: an outer diameter and width, like `1.125 in OD x 0.313 in`


@dataclasses.dataclass(frozen=True)
class Dimensions:
    outer_diameter: str
    width: str
    unit: str = "in"
    # R166ZZEE's inner race is `width` wide, its outer race this
    outer_width: str | None = None

    @property
    def id(self) -> str:
        """e.g. OD_1_125_X_0_313, MM_16_X_5, or OD_0_375_X_0_156_EXTENDED"""
        prefix = "MM" if self.unit == "mm" else "OD"
        id = f"{prefix}_{self.outer_diameter}_X_{self.width}".replace(".", "_")
        return id + "_EXTENDED" if self.outer_width else id

    @property
    def name(self) -> str:
        """e.g. 1.125 in OD x 0.313 in, or 0.375 in OD x 0.156 in (extended inner race)"""
        name = f"{self.outer_diameter} {self.unit} OD x {self.width} {self.unit}"
        return name + " (extended inner race)" if self.outer_width else name

    def meters(self, value: str) -> float:
        return round(float(value) * (INCH if self.unit == "in" else 0.001), 9)


# FRCDesign's Dimensions options, like `1/2" Rounded Hex ID x 1.125" OD x 0.313" WD`, `8mm ID x 0.875" OD x 0.281" WD`,
# or `0.375" ID x 0.875" OD x 0.219" WD (Unsealed)`
_DIMENSIONS = re.compile(
    r'^(?P<bore>.+?) (?:ID|Bore) x (?P<od>[\d.]+)(?P<od_unit>"|mm) OD x (?P<width>[\d.]+)(?P<width_unit>"|mm) WD'
    r"(?: \((?P<note>Sealed|Unsealed|Extended Race)\))?$"
)
_BORES = {
    '1/2" Rounded Hex': ("ROUNDED_HEX", "IN_1_2"),
    '3/8" Rounded Hex': ("ROUNDED_HEX", "IN_3_8"),
    '1/2" Hex': ("HEX", "IN_1_2"),
    '3/8" Hex': ("HEX", "IN_3_8"),
    "#10 Clearance": ("ROUND", "NO_10"),
    '0.188"': ("ROUND", "IN_3_16"),
    '0.190"': ("ROUND", "NO_10"),
    '0.250"': ("ROUND", "IN_1_4"),
    '0.375"': ("ROUND", "IN_3_8"),
    '0.500"': ("ROUND", "IN_1_2"),
    '0.625"': ("ROUND", "IN_5_8"),
    '0.750"': ("ROUND", "IN_3_4"),
}
# R166ZZEE's outer race, which FRCDesign leaves out (its standard R166's width)
_EXTENDED_OUTER_WIDTH = "0.125"
# FRCDesign's widths which aren't the vendor's: F126ZZ is 4 mm wide (am-3377)
_WIDTH_FIXES = {("MM_6", "12", "8"): "4"}


def _decimal(text: str) -> str:
    """`0.500` as 0.5, `1` as 1, `6.3` as 6.3"""
    return f"{float(text):g}"


@dataclasses.dataclass(frozen=True)
class Bearing:
    """What a configuration makes, but its vendor: a bearing as FRCDesign's Dimensions option names it."""

    type: str
    bore: str
    size: str
    dimensions: Dimensions
    seal: str

    @property
    def key(self) -> tuple[str, str, str, str, str]:
        return (self.type, self.bore, self.size, self.dimensions.id, self.seal)


def parse_dimensions(type: str, text: str) -> Bearing:
    match = _DIMENSIONS.match(text)
    assert match, text
    metric = re.fullmatch(r"(\d+)mm", match["bore"])
    bore, size = ("ROUND", f"MM_{metric[1]}") if metric else _BORES[match["bore"]]
    unit = "mm" if match["od_unit"] == "mm" else "in"
    outer, width = _decimal(match["od"]), _decimal(match["width"])
    assert (match["width_unit"] == "mm") == (unit == "mm"), text
    width = _WIDTH_FIXES.get((size, outer, width), width)
    note = match["note"]
    extended = _EXTENDED_OUTER_WIDTH if note == "Extended Race" else None
    seal = {"Sealed": "SEALED", "Unsealed": "OPEN"}.get(note or "", "SHIELDED")
    return Bearing(type, bore, size, Dimensions(outer, width, unit, extended), seal)


# Flanges, by size and dimensions: (diameter, thickness), in the dimensions' unit
FLANGES: dict[tuple[str, str], tuple[str, str]] = {
    # REV-21-1916-DR.pdf (1/2 in rounded hex), and WCP-0785's and Swyft's BRG-0001 STEP files; AndyMark lists 1.225 to
    # 1.25 in x 0.061 to 0.062 in for its own
    **{(size, "OD_1_125_X_0_313"): ("1.225", "0.063") for size in ("IN_1_2", "IN_3_8")},
    # REV-21-3197-DR.pdf
    **{(size, "OD_1_X_0_281"): ("1.06", "0.06") for size in ("IN_1_2", "IN_3_8")},
    # REV-21-3199-DR.pdf (#10 clearance), and Swyft's BRG-0002 STEP file; AndyMark lists 0.967 to 0.975 in x 0.06 to
    # 0.065 in (am-4463, am-4489)
    **{(size, "OD_0_875_X_0_281"): ("0.97", "0.06") for size in ("IN_1_2", "IN_3_8", "IN_1_4", "NO_10", "MM_8")},
    ("IN_3_8", "OD_0_875_X_0_219"): ("0.97", "0.062"),  # am-0027 (FR6)
    ("IN_3_16", "OD_0_5_X_0_196"): ("0.565", "0.042"),  # am-4459 (FR3ZZ)
    ("IN_1_4", "OD_0_5_X_0_188"): ("0.565", "0.042"),  # FR188ZZ's standard
    ("IN_1_4", "OD_0_625_X_0_196"): ("0.69", "0.042"),  # am-4461 (FR4ZZ)
    ("MM_5", "MM_11_X_4"): ("12.5", "0.8"),  # MF115ZZ's standard
    ("MM_5", "MM_13_X_4"): ("15", "1"),  # am-5469
    ("MM_5", "MM_16_X_5"): ("18", "1"),  # F625ZZ's standard
    ("MM_5", "MM_19_X_6"): ("22", "1.5"),  # F635ZZ's standard
    ("MM_6", "MM_12_X_4"): ("13.5", "0.8"),  # 1601-0412-0006's STEP file
    ("MM_6", "MM_14_X_5"): ("15", "1"),  # 1611-0514-0006's STEP file
    **{(size, "MM_14_X_5"): ("16", "1") for size in ("MM_4", "MM_5")},  # 1601-0014-0004's and -0005's STEP files
    ("MM_6", "MM_17_X_6"): ("19", "1"),  # am-4455
    # 1611-0514-0008's and -4008's STEP files, and Swyft's BRG-8mm-14mm-01
    ("MM_8", "MM_14_X_5"): ("15", "1"),
    ("MM_8", "MM_16_X_6_3"): ("18", "1.3"),  # TTB-0006's STEP file
    ("MM_8", "MM_16_X_5"): ("18", "1.1"),  # W688AZZ's standard
    ("MM_8", "MM_19_X_6"): ("22", "1.5"),  # am-0931
    ("MM_8", "MM_22_X_7"): ("25", "1.5"),  # am-4457
    ("MM_9", "MM_17_X_5"): ("19", "1.1"),  # F689ZZ's standard
    ("MM_12", "MM_24_X_6"): ("26.5", "1.5"),  # F6901ZZ's standard
    ("MM_12", "MM_32_X_10"): ("34", "2.3"),  # 1601-1032-0012's STEP file
    ("MM_32", "MM_39_X_5"): ("41", "1"),  # 1601-0039-0032's STEP file
    ("MM_15", "MM_21_X_4"): ("22.5", "0.8"),  # am-5536
    ("MM_30", "MM_42_X_7"): ("45", "1.5"),  # WCP-0299's STEP file
    ("MM_35", "MM_47_X_7"): ("50", "1.5"),  # WCP-1471's STEP file; REV-29-3295's too
}

# Flanges as each vendor's model has them, by vendor, size, and dimensions, where they differ from `FLANGES`
# (vendor/models: AndyMark's and REV's STEP files)
VENDOR_FLANGES: dict[tuple[str, str, str], tuple[str, str]] = {
    # AndyMark's 1.125 in OD flanges are 1.225 in x 0.06 in (am-0030, am-0573, am-0692, am-2986), FR82RS's 1.25 in
    **{("ANDYMARK", size, "OD_1_125_X_0_313"): ("1.225", "0.06") for size in ("IN_1_2", "IN_3_8")},
    ("ANDYMARK", "IN_3_8", "OD_0_875_X_0_281"): ("0.975", "0.062"),  # am-0028; am-4463's and am-4489's are within 0.01 in
    ("ANDYMARK", "IN_3_8", "OD_0_875_X_0_219"): ("0.972", "0.06"),  # am-0027
    ("ANDYMARK", "MM_6", "MM_12_X_4"): ("13.6", "0.8"),  # am-3377
    ("ANDYMARK", "MM_8", "MM_19_X_6"): ("22.1", "1.5"),  # am-0931
    ("ANDYMARK", "MM_8", "MM_22_X_7"): ("25", "1.5"),  # am-4457
    ("REV", "IN_1_2", "OD_1_X_0_281"): ("1.0625", "0.062"),  # REV-21-3197
    ("REV", "NO_10", "OD_0_875_X_0_281"): ("0.969", "0.0625"),  # REV-21-3199
    ("REV", "MM_8", "MM_12_X_3_5"): ("13.6", "0.8"),  # REV-49-1559
}


def flange(vendor: str, size: str, dimensions: str) -> tuple[str, str]:
    """A flanged bearing's flange: its diameter and thickness, in its dimensions' unit."""
    own = VENDOR_FLANGES.get((vendor, size, dimensions))
    if own is not None:
        return own
    if (size, dimensions) in FLANGES:
        return FLANGES[(size, dimensions)]
    # A bearing only a vendor's own model or table gives, which Generic offers too, takes that vendor's flange
    return next(found for (_, *key), found in VENDOR_FLANGES.items() if tuple(key) == (size, dimensions))


# What each vendor makes, from FRCDesign's records


@dataclasses.dataclass(frozen=True)
class Stock:
    part_number: str | None = None
    url: str | None = None
    # A key of MATERIALS
    material: str = "STEEL"


def _option_names(part: dict, record: dict) -> dict[str, str]:
    """A record's options in its part's configuration, by parameter name, the defaults where it has none."""
    chosen = dict(entry.split("=", 1) for entry in record["configurationKey"].split(";") if entry)
    return {
        parameter["name"]: option["name"]
        for parameter in part["parameters"]
        for option in parameter["options"]
        if option["id"] == chosen.get(parameter["id"], parameter["default"])
    }


def _designation(part_number: str | None) -> str | None:
    """A Generic record's part number where it's a designation, like FR8ZZ-HexHD; FRCDesign gives some a name"""
    return part_number if part_number and re.fullmatch(r"[A-Za-z0-9-]+", part_number) else None


_TYPES = {"Flanged Radial Bearing": "FLANGED", "Radial Bearing": "RADIAL"}

# By vendor, then what's made
Bearings = dict[tuple[str, str, str, str, str, str], tuple[Bearing, Stock]]


def _order(item: tuple[tuple, tuple[Bearing, Stock]]) -> tuple:
    (vendor, type, bore, size, _, seal), (bearing, _) = item
    dimensions = bearing.dimensions
    return (
        BearingVendor.values.index(vendor),
        BearingType.values.index(type),
        BearingBore.values.index(bore),
        BearingSize.values.index(size),
        dimensions.meters(dimensions.outer_diameter),
        dimensions.meters(dimensions.width),
        BearingSeal.values.index(seal),
    )


# Parts FRCDesign's records offer which their vendors no longer sell: AndyMark's store tags these Discontinued, and WCP's
# lists neither of its own (checked 2026-11-16). VEX sells no bearings at all.
DISCONTINUED = {
    ("ANDYMARK", part)
    for part in (
        "am-0027", "am-0029", "am-0031", "am-0936", "am-2095", "am-4149", "am-4455", "am-4459", "am-4462", "am-4464",
        "am-4466", "am-4467", "am-4468", "am-4469",
    )
} | {("WCP", "WCP-0782"), ("WCP", "217-5830")}
DISCONTINUED_VENDORS = {"VEX"}


def _frc_vendor_bearings() -> tuple[Bearings, dict[tuple, tuple[Bearing, str]]]:
    """FRCDesign's vendors' records but the discontinued, and the standard bearings FRCDesign's Generic records name, by
    their designations. A Generic record with no designation is a vendor's own bearing (a rounded hex, say), Generic's
    only while a vendor sells it."""
    stocked: Bearings = {}
    standard: dict[tuple, tuple[Bearing, str]] = {}
    for part in SNAPSHOT["parts"]:
        type = _TYPES[part["name"]]
        for record in part["records"]:
            options = _option_names(part, record)
            vendor = _VENDOR_NAMES[options["Vendor"]]
            part_number = record.get("partNumber")
            if vendor in DISCONTINUED_VENDORS or (vendor, part_number) in DISCONTINUED:
                continue
            if vendor == "GENERIC":
                designation = _designation(part_number)
                if designation is not None:
                    bearing = parse_dimensions(type, options["Dimensions"])
                    standard[bearing.key] = (bearing, designation)
                continue
            bearing = parse_dimensions(type, options["Dimensions"])
            stocked[(vendor, *bearing.key)] = (bearing, Stock(part_number, record.get("url")))
    return stocked, standard


FRC_VENDOR_BEARINGS, STANDARD_BEARINGS = _frc_vendor_bearings()


# FTC's: goBILDA's, by type, bore, size, outer diameter and width (mm), and seal, each its SKU and page. Its titles name
# only its 12 mm non-flanged bearing sealed; the rest are taken as shielded
GOBILDA_STORE = "https://www.gobilda.com"
_GOBILDA = [
    ("FLANGED", "ROUND", "MM_4", "14", "5", "SHIELDED", "1601-0014-0004", "/4mm-id-flanged-ball-bearing-14mm-od-5mm-thickness-2-pack/"),
    ("FLANGED", "ROUND", "MM_5", "14", "5", "SHIELDED", "1601-0014-0005", "/5mm-id-flanged-ball-bearing-14mm-od-5mm-thickness-2-pack/"),
    ("FLANGED", "ROUND", "MM_6", "12", "4", "SHIELDED", "1601-0412-0006", "/6mm-id-flanged-ball-bearing-12mm-od-4mm-thickness-2-pack/"),
    ("FLANGED", "ROUND", "MM_6", "14", "5", "SHIELDED", "1611-0514-0006", "/1611-series-flanged-ball-bearing-6mm-id-x-14mm-od-5mm-thickness-2-pack/"),
    ("FLANGED", "ROUND", "MM_8", "14", "5", "SHIELDED", "1611-0514-0008", "/1611-series-flanged-ball-bearing-8mm-id-x-14mm-od-5mm-thickness-2-pack/"),
    ("FLANGED", "REX", "MM_8", "14", "5", "SHIELDED", "1611-0514-4008", "/1611-series-flanged-ball-bearing-8mm-rex-id-x-14mm-od-5mm-thickness-2-pack/"),
    ("FLANGED", "ROUND", "MM_12", "32", "10", "SHIELDED", "1601-1032-0012", "/12mm-id-flanged-ball-bearing-32mm-od-10mm-thickness/"),
    ("FLANGED", "ROUND", "MM_32", "39", "5", "SHIELDED", "1601-0039-0032", "/32mm-id-flanged-ball-bearing-39mm-od-5mm-thickness/"),
    ("RADIAL", "ROUND", "MM_4", "10", "4", "SHIELDED", "1600-0410-0004", "/4mm-id-non-flanged-ball-bearing-10mm-od-4mm-thickness-2-pack/"),
    ("RADIAL", "ROUND", "MM_4", "16", "4", "SHIELDED", "1600-0416-0004", "/4mm-id-non-flanged-ball-bearing-16mm-od-4mm-thickness-2-pack/"),
    ("RADIAL", "ROUND", "MM_6", "16", "4", "SHIELDED", "1600-0416-0006", "/6mm-id-non-flanged-ball-bearing-16mm-od-4mm-thickness-2-pack/"),
    ("RADIAL", "ROUND", "MM_8", "22", "7", "SHIELDED", "1600-0722-0008", "/1600-series-non-flanged-ball-bearing-8mm-id-x-22mm-od-7mm-thickness-2-pack/"),
    ("RADIAL", "ROUND", "MM_12", "32", "10", "SEALED", "1626-1032-0012", "/12mm-id-non-flanged-ball-bearing-sealed-32mm-od-10mm-thickness-2-pack/"),
    ("RADIAL", "ROUND", "MM_120", "136", "8", "SHIELDED", "1600-0001-0120", "/120mm-non-flanged-ball-bearing-136mm-od-8mm-thickness/"),
]


# REV's for FTC, from its FTC catalog's Bearings, Shafts, & Spacers: its flanged ball bearing. Its Through Bore Bearings
# are acetal bushings, its pulley bearings V-grooved, and its end cap bearings caps, so none is a bearing here.
REV_STORE = "https://www.revrobotics.com"
_REV_FTC = [
    ("FLANGED", "ROUND", "MM_8", "12", "3.5", "SHIELDED", "REV-49-1559-PK10", "/rev-49-1559-pk10/"),
]
# AndyMark's for FTC: those its store tags for FTC (All FTC Parts, and its Robits line's bearings), but its V-grooved
# pulley bearing (am-4836)
ANDYMARK_STORE = "https://andymark.com/products"
_ANDYMARK_FTC = [
    ("FLANGED", "ROUND", "MM_5", "11", "4", "SHIELDED", "am-5606", "/5-mm-id-11-mm-od-shielded-flanged-bearing-mf115zz"),
    ("FLANGED", "ROUND", "MM_6", "12", "4", "SHIELDED", "am-3377", "/6-mm-id-12-mm-od-shielded-flanged-bearing-f126zz"),
    ("FLANGED", "ROUND", "MM_15", "21", "4", "SHIELDED", "am-5536", "/15-mm-id-21-mm-od-shielded-flanged-bearing-f6702zz"),
    ("RADIAL", "ROUND", "MM_5", "13", "4", "SHIELDED", "am-4724", "/5-mm-id-13-mm-od-shielded-bearing-695zz"),
]
_FTC_STORES = {"GOBILDA": (_GOBILDA, GOBILDA_STORE), "REV": (_REV_FTC, REV_STORE), "ANDYMARK": (_ANDYMARK_FTC, ANDYMARK_STORE)}
METRIC = {size.id for size in SIZES if size.unit == "mm"}


def _ftc_store_bearings() -> Bearings:
    """FTC's vendors' metric bearings, and AndyMark's 3/8 in hex Robits bearing (am-4489), which FRC's catalog has too."""
    bearings: Bearings = {}
    for vendor, (parts, store) in _FTC_STORES.items():
        for type, bore, size, outer, width, seal, sku, page in parts:
            bearing = Bearing(type, bore, size, Dimensions(outer, width, "mm"), seal)
            bearings[(vendor, *bearing.key)] = (bearing, Stock(sku, store + page))
    robits = ("ANDYMARK", "FLANGED", "HEX", "IN_3_8", "OD_0_875_X_0_281", "SEALED")
    bearings[robits] = FRC_VENDOR_BEARINGS[robits]
    return bearings


FTC_STORE_BEARINGS = _ftc_store_bearings()


# McMaster-Carr's, from vendor/mcmaster-bearings.json, the rows transcribed so far: a row joins once it has its part
# number, and a flanged one its flange, which its own table gives
MCMASTER_SNAPSHOT = json.loads((FOLDER / "vendor" / "mcmaster-bearings.json").read_text())


def mcmaster_url(part_number: str) -> str:
    return f"https://www.mcmaster.com/{part_number}/"


# A bearing costing more, which no other vendor sells, is a specialty bearing teams don't buy: McMaster-Carr's tables are
# full of them
MCMASTER_MAX_PRICE = 20


def _mcmaster_bearings(sold: set[tuple]) -> Bearings:
    """McMaster-Carr's bearings: of each, its cheapest row, and none over `MCMASTER_MAX_PRICE` that's not in `sold`,
    the bearings other vendors sell."""
    bearings: Bearings = {}
    prices: dict[tuple, float] = {}
    for row in MCMASTER_SNAPSHOT["bearings"]:
        part = row["partNumber"]
        dimensions = Dimensions(row["outerDiameter"], row["width"], row["unit"])
        bearing = Bearing(row["type"], row["bore"], row["size"], dimensions, row["seal"])
        if row["type"] == "FLANGED":
            assert row["flangeDiameter"] and row["flangeThickness"], f"{part} needs its flange"
        key = ("MCMASTER", *bearing.key)
        # Where it sells a bearing more than one way (steel or stainless, or another ABEC rating), the cheapest
        if row["price"] > MCMASTER_MAX_PRICE and bearing.key not in sold or key in bearings and row["price"] >= prices[key]:
            continue
        prices[key] = row["price"]
        bearings[key] = (bearing, Stock(part, mcmaster_url(part), row["material"]))
    return bearings


MCMASTER_BEARINGS = _mcmaster_bearings({key[1:] for key in {**FRC_VENDOR_BEARINGS, **FTC_STORE_BEARINGS}})
VENDOR_FLANGES.update(
    {
        ("MCMASTER", row["size"], Dimensions(row["outerDiameter"], row["width"], row["unit"]).id): (
            row["flangeDiameter"],
            row["flangeThickness"],
        )
        for row in MCMASTER_SNAPSHOT["bearings"]
        if row["type"] == "FLANGED"
        and ("MCMASTER", row["type"], row["bore"], row["size"], Dimensions(row["outerDiameter"], row["width"], row["unit"]).id, row["seal"])
        in MCMASTER_BEARINGS
    }
)


def _frc_bearings() -> Bearings:
    """FRCDesign's vendors', McMaster-Carr's, and Generic's: every standard bearing FRCDesign's Generic records name,
    and every bearing any vendor makes."""
    stocked: Bearings = {**FRC_VENDOR_BEARINGS, **MCMASTER_BEARINGS}
    designations = {key: designation for key, (_, designation) in STANDARD_BEARINGS.items()}
    made = {key[1:]: bearing for key, (bearing, _) in stocked.items()} | {key: bearing for key, (bearing, _) in STANDARD_BEARINGS.items()}
    for key, bearing in made.items():
        stocked[("GENERIC", *key)] = (bearing, Stock(designations.get(key)))
    return dict(sorted(stocked.items(), key=_order))


def _ftc_bearings() -> Bearings:
    """FTC's vendors', and McMaster-Carr's metric ones."""
    bearings = {**FTC_STORE_BEARINGS, **{key: value for key, value in MCMASTER_BEARINGS.items() if key[3] in METRIC}}
    return dict(sorted(bearings.items(), key=_order))


FRC_BEARINGS = _frc_bearings()
FTC_BEARINGS = _ftc_bearings()
# Every bearing either set makes, which the feature's tables hold
BEARINGS = {**FRC_BEARINGS, **FTC_BEARINGS}
_ALL_DIMENSIONS = sorted(
    {bearing.dimensions for bearing, _ in BEARINGS.values()},
    key=lambda dimensions: (
        dimensions.unit == "mm",
        dimensions.meters(dimensions.outer_diameter),
        dimensions.meters(dimensions.width),
        dimensions.outer_width is not None,
    ),
)
DIMENSIONS = {dimensions.id: dimensions for dimensions in _ALL_DIMENSIONS}
BearingDimensions = Enum("BearingDimensions", list(DIMENSIONS), {id: dimensions.name for id, dimensions in DIMENSIONS.items()})


# What each part is called and sold as: the feature sets these, and FRCDesignApp indexes the same records

_BORE_WORDS = {"HEX": "Hex", "ROUNDED_HEX": "Rounded Hex", "ROUND": "Round", "REX": "REX"}
_TYPE_WORDS = {"FLANGED": "Flanged Bearing", "RADIAL": "Radial Bearing"}
_SIZE_WORDS = {"NO_10": "0.19 in"}


def bearing_name(bearing: Bearing) -> str:
    """e.g. `1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)`, with any seal but shields after its
    dimensions: `3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)`."""
    size = _SIZE_WORDS.get(bearing.size, SIZES_BY_ID[bearing.size].name)
    described = [bearing.dimensions.name] + ([] if bearing.seal == "SHIELDED" else [BearingSeal.names[bearing.seal]])  # type: ignore[index]
    return f"{size} {_BORE_WORDS[bearing.bore]} {_TYPE_WORDS[bearing.type]} ({', '.join(described)})"


def vendor_name(vendor: str) -> str | None:
    """The name a part's Vendor property says; Generic parts have none."""
    return None if vendor == "GENERIC" else BearingVendor.names[vendor]  # type: ignore[index]


@dataclasses.dataclass(frozen=True)
class Record:
    """One configuration of the studio: its inputs' options, and the part it makes."""

    values: dict[str, str]
    name: str
    vendor: str
    stock: Stock
    bearing: Bearing

    def fields(self) -> dict[str, str]:
        vendor = vendor_name(self.vendor)
        return {
            "name": string(self.name),
            **({"vendor": string(vendor)} if vendor else {}),
            **{key: string(value) for key, value in _stock(self.stock).items()},
            "material": self.stock.material,
            # Every bearing's races in one steel grey, as WCP's models have them
            "appearance": "STEEL_GRAY",
        }

    def manifest(self) -> dict:
        vendor = vendor_name(self.vendor)
        return {
            "values": self.values,
            "name": self.name,
            **({"vendor": vendor} if vendor else {}),
            **_stock(self.stock),
            "material": MATERIALS[self.stock.material],
            # A radial bearing's top is where it seats: one connector, which its studio's top connector stands for
            **({"topMateConnector": SEAT_MATE_CONNECTOR} if self.bearing.type == "RADIAL" else {}),
        }


def _stock(stock: Stock) -> dict[str, str]:
    return {key: value for key, value in (("partNumber", stock.part_number), ("url", stock.url)) if value}


# Each set's studio: a lookup table of what's made (see `LookupTable`), each input offering what's made with the choices
# before it, and hidden where that's one thing

COLUMNS = ("vendor", "type", "bore", "size", "dimensions", "seal")
_ENUMS = {
    "vendor": ("Vendor", BearingVendor, "vendor"),
    "type": ("Type", BearingType, "bearingType"),
    "bore": ("Bore", BearingBore, "bore"),
    "size": ("Size", BearingSize, "size"),
    "dimensions": ("Dimensions", BearingDimensions, "dimensions"),
    "seal": ("Seal", BearingSeal, "seal"),
}


SEAT_MATE_CONNECTOR = "mateConnector"


# Bearings teams use, by type, bore, size, and dimensions: every size inserted from FRCDesign's library 150 times or more
# in a year (`fs cots`, to 2026-11-16, its vendors' uses summed), which is 98% of its flanged bearings' uses and 90% of
# its radial bearings'; and the flanged bearings which clear a #10 screw, 5 mm and REV's #10 clearance. Each with its
# uses; the rest are specialty bearings, in a studio of their own, so they don't crowd these lists.
STANDARD = {
    ("FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313"),  # 43,043
    ("FLANGED", "HEX", "IN_1_2", "OD_1_125_X_0_313"),  # 29,558
    ("FLANGED", "ROUNDED_HEX", "IN_3_8", "OD_0_875_X_0_281"),  # 3,706
    ("FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_X_0_281"),  # 1,543
    ("FLANGED", "HEX", "IN_3_8", "OD_1_125_X_0_313"),  # 1,243
    ("FLANGED", "HEX", "IN_3_8", "OD_0_875_X_0_281"),  # 1,229
    ("FLANGED", "ROUND", "IN_3_8", "OD_0_875_X_0_281"),  # 945
    ("FLANGED", "ROUND", "IN_1_4", "OD_0_5_X_0_188"),  # 716
    ("FLANGED", "ROUND", "IN_1_2", "OD_1_125_X_0_313"),  # 695
    ("FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_0_875_X_0_281"),  # 522
    ("FLANGED", "ROUND", "MM_35", "MM_47_X_7"),  # 585
    ("FLANGED", "ROUND", "MM_8", "MM_16_X_5"),  # 368
    ("FLANGED", "ROUND", "IN_1_4", "OD_0_625_X_0_196"),  # 260
    ("FLANGED", "ROUND", "IN_3_16", "OD_0_5_X_0_196"),  # 221
    ("FLANGED", "ROUND", "IN_3_8", "OD_1_125_X_0_313"),  # 197
    ("FLANGED", "ROUND", "MM_8", "MM_14_X_5"),  # 173
    ("FLANGED", "ROUND", "NO_10", "OD_0_875_X_0_281"),  # 116, #10 clearance
    ("FLANGED", "ROUND", "MM_5", "MM_11_X_4"),  # 108, #10 clearance
    ("FLANGED", "ROUND", "MM_5", "MM_13_X_4"),  # 80, #10 clearance
    ("RADIAL", "ROUND", "IN_3_8", "OD_0_875_X_0_281"),  # 2,736
    ("RADIAL", "ROUND", "IN_1_4", "OD_0_5_X_0_188"),  # 1,557
    ("RADIAL", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313"),  # 1,132
    ("RADIAL", "ROUND", "IN_1_4", "OD_0_75_X_0_281"),  # 922
    ("RADIAL", "HEX", "IN_1_2", "OD_1_125_X_0_313"),  # 832
    ("RADIAL", "ROUND", "IN_1_2", "OD_1_125_X_0_313"),  # 753
    ("RADIAL", "ROUND", "IN_1_4", "OD_0_625_X_0_196"),  # 643
    ("RADIAL", "ROUND", "IN_3_16", "OD_0_5_X_0_196"),  # 607
    ("RADIAL", "ROUND", "IN_1_4", "OD_0_375_X_0_125"),  # 505
    ("RADIAL", "ROUND", "IN_1_2", "OD_0_875_X_0_281"),  # 419
    ("RADIAL", "ROUND", "NO_10", "OD_0_375_X_0_156_EXTENDED"),  # 286
    ("RADIAL", "HEX", "IN_3_8", "OD_0_875_X_0_281"),  # 262
    ("RADIAL", "ROUND", "MM_35", "MM_47_X_7"),  # 205
    ("RADIAL", "ROUND", "MM_8", "MM_22_X_7"),  # 200
    ("RADIAL", "ROUND", "MM_5", "MM_13_X_4"),  # 194
    ("RADIAL", "ROUND", "MM_8", "MM_16_X_5"),  # 187
    ("RADIAL", "ROUND", "IN_3_8", "OD_1_125_X_0_375"),  # 150
}


def is_standard(key: tuple) -> bool:
    """Whether a bearing, by its key (vendor, type, bore, size, dimensions, seal), is a standard one: of any vendor, and
    any seal."""
    return key[1:5] in STANDARD


@dataclasses.dataclass(frozen=True)
class StudioSpec:
    """A Part Studio of a part set: its name in the definition, its tab's, which of the set's bearings it offers, and
    its inputs' defaults."""

    name: str
    tab: str
    offers: Callable[[tuple], bool]
    defaults: tuple[str, str, str, str, str, str]


class Catalog:
    """One part set's studios, each laid out from the bearings it offers, and their records: what FRCDesign indexes. Each
    bearing is in one studio.

    Args:
        vendors: The vendors' order in its studios, most used first, where it isn't the enum's own.
    """

    def __init__(self, bearings: Bearings, specs: list[StudioSpec], vendors: list[str] | None = None):
        self.bearings = bearings
        self.vendors = vendors or []
        self.tables: dict[str, LookupTable] = {}
        self.studios: dict[str, ConfiguredStudio] = {}
        self.records: dict[str, dict[tuple, Record]] = {}
        for spec in specs:
            offered = {key: value for key, value in bearings.items() if spec.offers(key)}
            table = LookupTable(COLUMNS, list(offered))
            self.tables[spec.name] = table
            self.studios[spec.name] = ConfiguredStudio(
                spec.name,
                "libraryBearing",
                [self._list(table, id, *_ENUMS[id], default) for id, default in zip(COLUMNS, spec.defaults)],
                tab=spec.tab,
                mate_connector=SEAT_MATE_CONNECTOR,
                top_mate_connector="topMateConnector",
                bottom_mate_connector="bottomMateConnector",
            )
            self.records[spec.name] = {
                key: Record(table.shown(key), bearing_name(bearing), key[0], stock, bearing)
                for key, (bearing, stock) in offered.items()
            }
        studied = [key for records in self.records.values() for key in records]
        assert sorted(studied) == sorted(bearings), "Each bearing is in exactly one studio"

    def _list(self, table: LookupTable, id: str, name: str, enum: Enum, parameter: str, default: str) -> Input:
        assert enum.names is not None
        order = self.vendors if enum is BearingVendor else []
        options = [Option(value, enum.names[value], value) for value in order + [value for value in enum.values if value not in order]]
        return table.input(Input(id, name, parameter, options, enum_name=enum.name, default=default))


# The defaults are what's inserted most from each library: FRCDesign's Generic 1/2 in rounded hex flanged bearing,
# 1.125 in OD, and of its specialty bearings, the 8 x 22 x 7 mm flanged one; and ConfigLib's goBILDA 8 mm round flanged
# bearing, 14 mm OD. ConfigLib has goBILDA's alone, so FTC's vendors are goBILDA's first. FTC's are few enough for one
# studio.
FRC = Catalog(
    FRC_BEARINGS,
    [
        StudioSpec("bearing", "Bearing", is_standard, ("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")),
        StudioSpec(
            "specialtyBearing",
            "Specialty Bearing",
            lambda key: not is_standard(key),
            ("GENERIC", "FLANGED", "ROUND", "MM_8", "MM_22_X_7", "SHIELDED"),
        ),
    ],
)
FTC = Catalog(
    FTC_BEARINGS,
    [StudioSpec("bearing", "Bearing", lambda key: True, ("GOBILDA", "FLANGED", "ROUND", "MM_8", "MM_14_X_5", "SHIELDED"))],
    ["GOBILDA", "REV", "ANDYMARK", "MCMASTER"],
)

STUDIOS = FRC.studios
RECORDS = FRC.records
# Every part either set makes, by its key, which the feature's tables hold
ALL_RECORDS = {key: record for catalog in (FRC, FTC) for records in catalog.records.values() for key, record in records.items()}

# Where the studio was last released: `fs release bearings` records it beside this file, FTC's beside
# ftcBearingTables.py
LOCATIONS = read_locations(pathlib.Path(__file__))


# FeatureScript


def _length(value: str | float, unit: str) -> str:
    return inch(round(float(value), 6)) if unit == "in" else mm(round(float(value), 6))


def _map(entries: list[tuple[str, str]], indent: int) -> str:
    pad = " " * indent
    return "{\n" + ",\n".join(f"{pad}    {key} : {value}" for key, value in entries) + f"\n{pad}}}"


def _fields(fields: dict[str, str]) -> str:
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in fields.items()) + " }"


def _nested(table: dict[tuple, object], enums: list[Enum], render, indent: int = 4) -> str:
    """A map of maps, keyed by each level's enum, from a table keyed by tuples of their values."""
    groups: dict[str, dict] = {}
    for key, value in table.items():
        groups.setdefault(key[0], {})[key[1:]] = value
    entries = [
        (enums[0][first], render(rest[()]) if len(enums) == 1 else _nested(rest, enums[1:], render, indent + 4))
        for first, rest in groups.items()
    ]
    return _map(entries, indent)


def _bore_fields(bore: str, size: str) -> str:
    spec = SIZES_BY_ID[size]
    value = BORE_FLATS.get((bore, size), _length(spec.value, spec.unit))
    fields = {"diameter": value} if bore == "ROUND" else {"acrossFlats": value}
    if (bore, size) in ROUNDED_DIAMETERS:
        fields["diameter"] = ROUNDED_DIAMETERS[(bore, size)]
    return _fields(fields)


def _bearing_fields(item: tuple[Bearing, Record]) -> str:
    bearing, record = item
    dimensions = bearing.dimensions
    unit = dimensions.unit
    fields = {
        "outerDiameter": _length(dimensions.outer_diameter, unit),
        "width": _length(dimensions.width, unit),
    }
    if dimensions.outer_width:
        fields["outerWidth"] = _length(dimensions.outer_width, unit)
    if bearing.type != "RADIAL":
        diameter, thickness = flange(record.vendor, bearing.size, dimensions.id)
        fields["flangeDiameter"] = _length(diameter, unit)
        fields["flangeThickness"] = _length(thickness, unit)
    return _fields({**fields, **record.fields()})


_BORE_SIZES = {(bearing.bore, bearing.size) for bearing, _ in BEARINGS.values()}

CONTENTS = [
    Import("core/robotProperties.fs"),
    BearingVendor,
    BearingType,
    BearingBore,
    BearingSize,
    BearingDimensions,
    BearingSeal,
    Code(
        "/** Each bore's shape: a round bore's `diameter`, or a hex's `acrossFlats`, with its corners rounded to"
        " `diameter` where they are. */\nexport const BEARING_BORES = "
        + _nested(
            {key: key for key in sorted(_BORE_SIZES, key=lambda key: (BearingBore.values.index(key[0]), BearingSize.values.index(key[1])))},
            [BearingBore, BearingSize],
            lambda key: _bore_fields(*key),
            0,
        )
        + ";"
    ),
    Code(
        "/** What each vendor makes, by vendor, type, bore, size, dimensions, and seal: each bearing's dimensions, a"
        " flanged one's flange, and its properties. */\nexport const BEARINGS = "
        + _nested(
            {key: (bearing, ALL_RECORDS[key]) for key, (bearing, _) in BEARINGS.items()},
            [BearingVendor, BearingType, BearingBore, BearingSize, BearingDimensions, BearingSeal],
            _bearing_fields,
            0,
        )
        + ";"
    ),
]


# FRCDesign's manifest


def manifest(catalog: Catalog = FRC, locations: dict = LOCATIONS, source: str = "bearingTables.py") -> dict:
    """A part set's manifest, for FRCDesign: its studio and records, and its catalog."""
    return {
        "source": f"featurescripts/bearings/{source}",
        "studios": {
            name: {
                **studio.manifest(),
                "location": locations.get(name),
                "records": [record.manifest() for record in catalog.records[name].values()],
            }
            for name, studio in catalog.studios.items()
        },
        "vendors": [
            {"id": id, "name": BearingVendor.names[id]}  # type: ignore[index]
            for id in BearingVendor.values
            if any(key[0] == id for key in catalog.bearings)
        ],
        "bearings": [
            {
                "vendor": vendor,
                "type": type,
                "bore": bore,
                "size": size,
                "boreSize": SIZES_BY_ID[size].meters,
                "dimensions": dimensions,
                "outerDiameter": bearing.dimensions.meters(bearing.dimensions.outer_diameter),
                "width": bearing.dimensions.meters(bearing.dimensions.width),
                **(
                    {"flangeDiameter": bearing.dimensions.meters(flange(vendor, size, dimensions)[0])}
                    if type != "RADIAL"
                    else {}
                ),
                "seal": seal,
                **_stock(stock),
            }
            for (vendor, type, bore, size, dimensions, seal), (bearing, stock) in catalog.bearings.items()
        ],
    }


EXPORTS = {"bearings.gen.json": json.dumps(manifest(), indent=2) + "\n"}


# Refreshing vendor/frcdesign-bearings.json, which only `fetch` does: `fs gen` runs this file without it, and makes no
# requests

LIBRARY_API = "https://app.frcdesign.org/api"
LIBRARY_PARTS = list(_TYPES)


def _get_json(url: str):
    import urllib.request

    request = urllib.request.Request(url, headers={"User-Agent": "robot-code bearingTables.py fetch"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def fetch_library() -> None:
    """Writes the configurations of FRCDesignLib's Flanged Radial Bearing and Radial Bearing to
    vendor/frcdesign-bearings.json, from FRCDesign's public API."""
    version = _get_json(f"{LIBRARY_API}/library-version/library/frc-design-lib")["version"]
    library = _get_json(f"{LIBRARY_API}/library-data/library/frc-design-lib?v={version}")
    [group] = [group for group in library["groups"].values() if group["name"] == "Bearings & Bushings"]
    insertables = library["insertables"]
    parts = []
    for insertable_id in group["insertableOrder"]:
        insertable = insertables[insertable_id]
        if insertable["name"] not in LIBRARY_PARTS:
            continue
        configuration = _get_json(f"{LIBRARY_API}/configuration/insertable/{insertable_id}?v={version}")
        parts.append(
            {
                "insertableId": insertable_id,
                "name": insertable["name"],
                "vendors": insertable["vendors"],
                "documentId": insertable["documentId"],
                "versionId": insertable["versionId"],
                "elementId": insertable["elementId"],
                "parameters": configuration["parameters"],
                "records": configuration["records"],
            }
        )
    snapshot = {
        "source": f"{LIBRARY_API}/configuration/insertable/{{id}}?v={version} (frc-design-lib, Bearings & Bushings group)",
        "parts": parts,
    }
    SNAPSHOT_PATH.write_text(json.dumps(snapshot, indent=1) + "\n", newline="")
    print(f"Wrote {len(parts)} parts to vendor/frcdesign-bearings.json.")


if __name__ == "__main__":
    import sys

    if sys.argv[1:] != ["fetch"]:
        sys.exit("Usage: python bearingTables.py fetch")
    fetch_library()
