"""FRCDesign's library fasteners, generated for FRCDesignApp: screws, washers, and nuts for FRC, and FTC's metric ones
(`FTC`, whose Part Studios ftcFastenerTables.py configures as a part set of their own). Run `uv run fs gen` after
editing.

This is the source of three things, which must agree:

- fastenerTables.gen.fs: the enums and data the screw, washer, and nut features build from.
- The configurations of the Part Studios exposing those features, one per kind (`STUDIOS`), which
  `fs parts fasteners --test` (in the backend, as `<Part> (TEST)`) and `fs parts fasteners --release` (in FRCDesignLib's
  Fasteners document)
  build: an input per feature parameter, laid out from what's made by `LookupTable`'s rules (see docs/fs-cli.md's
  "Configured Part Studios"): Length shows only the lengths the chosen vendor makes in the chosen size, head, and
  finish, and only where there's more than one.
- fasteners.gen.json: the same catalog for FRCDesign, with each studio's configuration as Onshape answers it, which
  picks a fastener for each hole and inserts its studio with a configuration of these option ids. FRCDesign stores those configurations in documents, so never rename or
  remove a released option id (as with enums stored in documents); add new ones.

What each vendor makes, and its part numbers, come from FRCDesign's own Fasteners group (vendor/frcdesign-fasteners.json,
a snapshot of its configurations from FRCDesign's public API): screws, McMaster-Carr's nuts, and McMaster-Carr's, WCP's,
and AndyMark's washers. A screw's Fully threaded shows only where a length is sold both ways, and is partly threaded
otherwise. The screws of vendors whose stores list them (`STORES`: WCP and AndyMark) come from their stores instead
(vendor/stores.json), since a store is current where the group may not be. `uv run python
featurescripts/fasteners/fastenerTables.py fetch` refreshes both. Generic offers every screw length any vendor makes,
and SAE washers, without part numbers.

Each part has one mate connector, where it seats: FRCDesign fastens a screw or nut to the top edge of the washer it
sits on, or to the hole's own edge, so any washer goes under any screw or nut of its size.

FTC's metric parts come from McMaster-Carr's tables (vendor/mcmaster-metric.json), goBILDA's and REV's stores and
models, and AndyMark's store; docs/ftc-fasteners.md has how each was measured and chosen.

Dimensions are nominal (ASME B18.3 screws, SAE washers, hex and nylon-insert lock nuts; ISO 4762, 7380 and 10642 for
FTC's): close enough to check fit and clearance, not to make the parts. Clearance holes are std's (`ANSI_V2ClearanceHoleTable`), and tap drills std's
75% engagement ones.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from fractions import Fraction

from fs_cli.configured_studios import ConfiguredStudio, Input, LookupTable, Option, read_locations
from fs_cli.gen import Code, Constant, Import
from fs_cli.tables import Enum, inch, mm, string

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT = json.loads((FOLDER / "vendor" / "frcdesign-fasteners.json").read_text())
STORES_SNAPSHOT = FOLDER / "vendor" / "stores.json"

INCH = 0.0254


# Vendors, sizes, and heads


FastenerVendor = Enum(
    "FastenerVendor",
    ["GENERIC", "MCMASTER", "GOBILDA", "REV", "WCP", "ANDYMARK"],
    {
        "GENERIC": "Generic",
        "MCMASTER": "McMaster-Carr",
        "GOBILDA": "goBILDA",
        "REV": "REV",
        "WCP": "WCP",
        "ANDYMARK": "AndyMark",
    },
)
# FRCDesign's vendor codes
VENDOR_CODES = {"MCM": "MCMASTER", "REV": "REV", "WCP": "WCP", "AM": "ANDYMARK"}


@dataclasses.dataclass
class Size:
    id: str
    name: str
    # As in std's hole tables
    hole_table_size: str
    threads_per_inch: int
    major_diameter: float
    tap_drill_diameter: float
    close_clearance_diameter: float
    free_clearance_diameter: float
    # An external thread's, as std's ANSI_ExternalThreadTable has it
    minor_diameter: float
    unit: str = "in"

    @property
    def pitch(self) -> float:
        return 1 / self.threads_per_inch

    # A standard socket head's threads run 2D plus this from its tip (ASTM A574)
    thread_allowance: float = 0.5


@dataclasses.dataclass
class MetricSize:
    """In millimeters, as std's ISO hole tables have it; FTC's."""

    id: str
    name: str
    hole_table_size: str
    pitch: float
    major_diameter: float
    tap_drill_diameter: float
    close_clearance_diameter: float
    # ISO 273's normal fit
    free_clearance_diameter: float
    minor_diameter: float
    unit: str = "mm"
    # A standard socket head's threads run 2D plus this from its tip (ISO 4762)
    thread_allowance: float = 12


SIZES = [
    Size("NO_4_40", "#4-40", "#4", 40, 0.112, 0.089, 0.116, 0.1285, 0.0814),
    Size("NO_6_32", "#6-32", "#6", 32, 0.138, 0.1065, 0.144, 0.1495, 0.1),
    Size("NO_8_32", "#8-32", "#8", 32, 0.164, 0.136, 0.1695, 0.177, 0.1259),
    Size("NO_10_32", "#10-32", "#10", 32, 0.19, 0.159, 0.196, 0.201, 0.1528),
    Size("QUARTER_20", "1/4-20", "1/4", 20, 0.25, 0.201, 0.257, 0.266, 0.1894),
]
# FTC's, from std's ISO_ExternalThreadTable, ISO_V2TappedHoleTable (coarse) and ISO_V2ClearanceHoleTable
METRIC_SIZES = [
    MetricSize("M3", "M3 x 0.5", "M3", 0.5, 3, 2.5, 3.2, 3.4, 2.459),
    MetricSize("M4", "M4 x 0.7", "M4", 0.7, 4, 3.3, 4.3, 4.5, 3.242),
]
# Imperial sizes are FRC's, metric FTC's: each part set offers only its own
IMPERIAL = {size.id for size in SIZES}
METRIC = {size.id for size in METRIC_SIZES}
SIZES_BY_NAME = {size.name: size for size in SIZES}
FastenerSize = Enum(
    "FastenerSize", [size.id for size in SIZES + METRIC_SIZES], {size.id: size.name for size in SIZES + METRIC_SIZES}
)

ScrewHead = Enum(
    "ScrewHead",
    ["SOCKET", "BUTTON", "FLAT", "HEX"],
    {"SOCKET": "Socket head", "BUTTON": "Button head", "FLAT": "Flat head", "HEX": "Hex head"},
)
HEAD_CODES = {"SHCS": "SOCKET", "BHCS": "BUTTON", "FHCS": "FLAT"}


@dataclasses.dataclass
class Head:
    # A hex head's across its corners
    diameter: float
    height: float
    # Across the flats of its hex socket; a hex head has none
    socket: float | None
    # A flat head's length includes its head, which sits in a countersink of `countersink_angle`
    countersunk: bool = False
    unit: str = "in"
    countersink_angle: int = 82
    # A hex head's
    across_flats: float | None = None


def _hex_head(across_flats: float, height: float) -> Head:
    return Head(round(across_flats * 2 / 3**0.5, 4), height, None, unit="mm", across_flats=across_flats)



HEADS = {
    "NO_4_40": {"SOCKET": Head(0.183, 0.112, 3 / 32), "BUTTON": Head(0.213, 0.059, 5 / 64), "FLAT": Head(0.255, 0.083, 1 / 16, True)},
    "NO_6_32": {"SOCKET": Head(0.226, 0.138, 7 / 64), "BUTTON": Head(0.262, 0.074, 5 / 64), "FLAT": Head(0.307, 0.097, 5 / 64, True)},
    "NO_8_32": {"SOCKET": Head(0.27, 0.164, 9 / 64), "BUTTON": Head(0.312, 0.087, 3 / 32), "FLAT": Head(0.359, 0.115, 3 / 32, True)},
    "NO_10_32": {"SOCKET": Head(0.312, 0.19, 5 / 32), "BUTTON": Head(0.375, 0.102, 1 / 8), "FLAT": Head(0.411, 0.132, 1 / 8, True)},
    "QUARTER_20": {"SOCKET": Head(0.375, 0.25, 3 / 16), "BUTTON": Head(0.437, 0.132, 5 / 32), "FLAT": Head(0.531, 0.153, 5 / 32, True)},
    # ISO 4762, 7380 and 10642, which McMaster-Carr's and goBILDA's models measure to (docs/ftc-fasteners.md); a flat
    # head sits in a 90 degree countersink. M3's hex heads are REV's hex caps, M4's goBILDA's.
    "M3": {
        "SOCKET": Head(5.5, 3, 2.5, unit="mm"),
        "BUTTON": Head(5.7, 1.65, 2, unit="mm"),
        "FLAT": Head(6, 1.7, 2, True, "mm", 90),
        "HEX": _hex_head(5.5, 2),
    },
    "M4": {
        "SOCKET": Head(7, 4, 3, unit="mm"),
        "BUTTON": Head(7.6, 2.2, 2.5, unit="mm"),
        "FLAT": Head(8, 2.3, 2.5, True, "mm", 90),
        "HEX": _hex_head(7, 2.8),
    },
}


# Finishes and materials: each part is its vendor's standard, black, unless it sells others too


@dataclasses.dataclass(frozen=True)
class Material:
    # As Onshape names it, which the part's Material property says
    name: str
    # robotProperties.fs's constant for it
    constant: str


STEEL = Material("Steel", "STEEL")
MATERIALS = {
    "Steel": STEEL,
    "Stainless Steel": Material("Stainless Steel", "STAINLESS_STEEL"),
    "Aluminum": Material("Aluminum - 6061", "ALUMINUM"),
}

FINISH_NAMES = {"BLACK": "Black", "ZINC_PLATED": "Zinc plated", "STAINLESS": "Stainless steel", "ALUMINUM": "Aluminum"}
FINISH_ORDER = list(FINISH_NAMES)
# Each kind its own, so a feature's dialog offers no finish its kind never comes in
ScrewFinish = Enum("ScrewFinish", ["BLACK", "ZINC_PLATED", "ALUMINUM", "STAINLESS"], FINISH_NAMES)
WasherFinish = Enum("WasherFinish", ["BLACK", "ZINC_PLATED", "STAINLESS"], FINISH_NAMES)
# Each finish's appearance, as robotProperties.fs's constant (FRCDesignLib's color standards); WCP's anodized aluminum
# screws' color is unpublished
APPEARANCES = {"BLACK": "BLACK", "ZINC_PLATED": "MEDIUM_GRAY", "STAINLESS": "STEEL_GRAY", "ALUMINUM": "MEDIUM_GRAY"}
# The standards' color for nuts, whatever their finish
NUT_APPEARANCE = "ZINC_COATED_STEEL"
STANDARD_FINISH = "BLACK"

# A vendor's own words for a finish, after the material: black oxide, WCP's black zinc, or AndyMark's zinc
_FINISH_WORDS = {
    "Black Oxide": "BLACK",
    "Black Zinc": "BLACK",
    "Zinc Plated": "ZINC_PLATED",
    "Zink Finish": "ZINC_PLATED",
    "Anodized": "ALUMINUM",
}


def parse_finish(text: str) -> tuple[Material, str]:
    """A part's material and finish from its record's, like `Steel, Black Oxide` or plain `Stainless Steel`."""
    material, _, finish = text.partition(", ")
    if not finish:
        return MATERIALS[material], "STAINLESS" if material == "Stainless Steel" else STANDARD_FINISH
    return MATERIALS[material], _FINISH_WORDS[finish]


@dataclasses.dataclass(frozen=True)
class Variant:
    """A screw beyond its size, head, and length: its finish, and whether it's threaded its whole length."""

    finish: str = STANDARD_FINISH
    full_thread: bool = False

    @property
    def name(self) -> str:
        words = ([] if self.finish == STANDARD_FINISH else [FINISH_NAMES[self.finish]]) + (  # type: ignore[index]
            ["Fully threaded"] if self.full_thread else []
        )
        return ", ".join(words[:1] + [word.lower() for word in words[1:]]) or "Standard"


STANDARD = Variant()


def _variant_order(variant: Variant) -> tuple:
    return (FINISH_ORDER.index(variant.finish), variant.full_thread)


# Screws: what each vendor makes, from FRCDesign's records, like `#10-32 x 1-1/4" L BHCS (Steel, Black Oxide)`


@dataclasses.dataclass(frozen=True)
class Stock:
    part_number: str | None = None
    url: str | None = None
    material: Material = STEEL
    # A screw threaded its whole length, where its vendor says so; otherwise its head's standard threading
    full_thread: bool = False


_SCREW = re.compile(r'^(#\d+-\d+|1/4-20)\s+x\s+([\d\-/\.]+)"\s*L\s+(SHCS|BHCS|FHCS)\s*\(([^)]*)\)')


def parse_inches(text: str) -> Fraction:
    """`1-1/4`, `3/8`, `2`, or `0.031`, in inches."""
    whole, dash, part = text.partition("-")
    if dash:
        return Fraction(whole) + Fraction(part)
    return Fraction(text)


def _vendor(part: dict) -> str:
    [code] = part["vendors"]
    return VENDOR_CODES[code]


# Vendors' Shopify stores, whose screws replace the group's: each store's vendor, and how its screws are titled
STORES = {"wcproducts.com": "WCP", "andymark.com": "ANDYMARK"}
_STORE_SCREWS = {
    # `#10-32 x .375" L BHCS (Steel, Black Oxide) (50-Pack)`, `#10-32 x .250" L BHCS (Aluminum, Anodized)`; not ultra
    # low profile ones, whose heads aren't standard, or a Kraken's spares, `#4-40 x 1/4" BHCS (IP8, ...)`, whose drive
    # isn't a hex socket
    "WCP": re.compile(
        r'^(?P<size>#\d+-\d+) x (?P<length>[\d./]+)"? (?:L )?(?P<head>BHCS|SHCS|FHCS) '
        r"\((?P<finish>Steel, Black Oxide|Aluminum, Anodized)\)"
    ),
    # `10-32 x 0.5 in. Button Head Cap Screw`, `1/4-20 x 2 in. Flat Head Cap Screw with Nylon Patch`
    "ANDYMARK": re.compile(
        r"^(?P<size>\d+-\d+|1/4-20) x (?P<length>[\d./]+) in\.? (?:Hex Drive )?"
        r"(?P<head>Button Head Cap Screw|Socket Head Cap Screw|Flat Head Cap Screw|Flat Head Socket Cap Screw)"
        r"(?: Black Oxide)?(?P<patch> with Nylon(?: Thread Lock)? Patch)?$"
    ),
}
_STORE_HEADS = {
    "BHCS": "BUTTON",
    "SHCS": "SOCKET",
    "FHCS": "FLAT",
    "Button Head Cap Screw": "BUTTON",
    "Socket Head Cap Screw": "SOCKET",
    "Flat Head Cap Screw": "FLAT",
    "Flat Head Socket Cap Screw": "FLAT",
}

# What each vendor makes: by vendor, size, and head, each length's variants
Screws = dict[tuple[str, str, str], dict[Fraction, dict[Variant, Stock]]]


@dataclasses.dataclass(frozen=True)
class StoreScrew:
    key: tuple[str, str, str]
    length: Fraction
    variant: Variant
    has_patch: bool
    stock: Stock


def _store_screw(product: dict) -> StoreScrew | None:
    """A store's product as a catalog screw; None for anything else: another kind of part or size, or a listing
    shipped from abroad (`Ships from Sydney - ...`) of a screw the store lists at home too."""
    vendor = STORES[product["store"]]
    match = _STORE_SCREWS[vendor].match(product["title"])
    if match is None:
        return None
    size = match["size"]
    size = size if size.startswith("#") or size == "1/4-20" else "#" + size
    if size not in SIZES_BY_NAME:
        return None
    finish_text = match.groupdict().get("finish")
    if finish_text is None:
        # AndyMark names a zinc plated screw's finish only in its url, like `.../screw-bhcs-1-4-20-x-750-zinc`
        zinc = "zinc" in product["title"].lower() or "zinc" in product["url"].lower()
        finish_text = "Steel, Zinc Plated" if zinc else "Steel, Black Oxide"
    material, finish = parse_finish(finish_text)
    return StoreScrew(
        (vendor, SIZES_BY_NAME[size].id, _STORE_HEADS[match["head"]]),
        parse_inches(match["length"]),
        Variant(finish),
        bool(match.groupdict().get("patch")),
        Stock(product["sku"], product["url"], material),
    )


def _store_screws() -> Screws:
    """The screws each store sells in a size and head the catalog has: one per length and variant, one without a
    thread lock patch where it sells both."""
    chosen: dict[tuple, StoreScrew] = {}
    for product in json.loads(STORES_SNAPSHOT.read_text())["products"]:
        screw = _store_screw(product)
        if screw is None:
            continue
        slot = (screw.key, screw.length, screw.variant)
        if slot not in chosen or chosen[slot].has_patch and not screw.has_patch:
            chosen[slot] = screw
    screws: Screws = {}
    for screw in chosen.values():
        screws.setdefault(screw.key, {}).setdefault(screw.length, {})[screw.variant] = screw.stock
    return screws


def _option_names(part: dict, record: dict) -> list[str]:
    """The names of a record's options in its group's configuration, its parameters' defaults where it has none."""
    chosen = dict(entry.split("=", 1) for entry in record["configurationKey"].split(";") if entry)
    return [
        option["name"]
        for parameter in part["parameters"]
        for option in parameter["options"]
        if option["id"] == chosen.get(parameter["id"], parameter["default"])
    ]


def _stocked_screws() -> Screws:
    stocked: Screws = {}
    for part in SNAPSHOT["parts"]:
        if " Bolt (" not in part["name"]:
            continue
        vendor = _vendor(part)
        if vendor in STORES.values():
            continue
        for record in part["records"]:
            match = _SCREW.match(record["name"])
            assert match, record["name"]
            size, length, head, finish_text = match.groups()
            material, finish = parse_finish(finish_text)
            # McMaster-Carr sells some long socket heads both partly threaded, its standard, and fully threaded
            full_thread = any("Full Thread" in name for name in _option_names(part, record))
            key = (vendor, SIZES_BY_NAME[size].id, HEAD_CODES[head])
            variants = stocked.setdefault(key, {}).setdefault(parse_inches(length), {})
            variants[Variant(finish, full_thread)] = Stock(record["partNumber"], record.get("url"), material)
    stocked.update(_store_screws())
    for variants in (variants for lengths in stocked.values() for variants in lengths.values()):
        # Where a length is only sold fully threaded, that's its standard
        for variant in [variant for variant in variants if variant.full_thread]:
            if Variant(variant.finish) not in variants:
                variants[Variant(variant.finish)] = variants.pop(variant)
    # Generic: every length anyone makes, in the standard variant alone
    for (_, size, head), lengths in list(stocked.items()):
        generic = stocked.setdefault(("GENERIC", size, head), {})
        for length in lengths:
            generic[length] = {STANDARD: Stock()}
    return {
        key: {
            length: dict(sorted(variants.items(), key=lambda item: _variant_order(item[0])))
            for length, variants in sorted(lengths.items())
        }
        for key, lengths in sorted(stocked.items(), key=_screw_order)
    }


def _screw_order(item) -> tuple:
    vendor, size, head = item[0]
    return (FastenerVendor.values.index(vendor), FastenerSize.values.index(size), ScrewHead.values.index(head))


SCREWS = _stocked_screws()


@dataclasses.dataclass(frozen=True, order=True)
class Millimeters:
    """A metric screw's length; an imperial one's is a Fraction of an inch."""

    value: int


Length = Fraction | Millimeters


# FTC's metric screws: McMaster-Carr's (vendor/mcmaster-metric.json, transcribed from its tables), goBILDA's and REV's
# (from their stores and models; see docs/ftc-fasteners.md), and AndyMark's (its store, as for FRC)

MCMASTER_METRIC = json.loads((FOLDER / "vendor" / "mcmaster-metric.json").read_text())
STAINLESS = MATERIALS["Stainless Steel"]
# A finish's material, where a vendor's table names none
_FINISH_MATERIALS = {"STAINLESS": STAINLESS}


def mcmaster_url(part_number: str) -> str:
    return f"https://www.mcmaster.com/{part_number}/"


def _mcmaster_supply(supplies: list[dict]) -> dict:
    """The supply a team orders: the small pack, imported or United States origin, cheapest by the piece; a large
    pack only where there's no small one."""
    small = [supply for supply in supplies if supply["pack"].startswith("SMALL")]
    return min(small or supplies, key=lambda supply: supply["price"] / supply["quantity"])


def _mcmaster_stock(supplies: list[dict], finish: str, full_thread: bool = False) -> Stock:
    part_number = _mcmaster_supply(supplies)["partNumber"]
    return Stock(part_number, mcmaster_url(part_number), _FINISH_MATERIALS.get(finish, STEEL), full_thread)


def _mcmaster_screws() -> Screws:
    """Where a length comes both ways, partly threaded is its standard and fully threaded a variant, as McMaster-Carr's
    FRC screws are."""
    screws: Screws = {}
    for head, sizes in MCMASTER_METRIC["screws"].items():
        for size, finishes in sizes.items():
            for finish in ScrewFinish.values:
                rows: dict[int, dict[str, list[dict]]] = {}
                for row in finishes.get(finish, {"lengths": []})["lengths"]:
                    rows.setdefault(row["length"], {})[row["threading"]] = row["supplies"]
                for length, threadings in rows.items():
                    variants = screws.setdefault(("MCMASTER", size, head), {}).setdefault(Millimeters(length), {})
                    if len(threadings) == 2:
                        variants[Variant(finish)] = _mcmaster_stock(threadings["PARTIAL"], finish)
                        variants[Variant(finish, True)] = _mcmaster_stock(threadings["FULL"], finish, True)
                    else:
                        [(threading, supplies)] = threadings.items()
                        variants[Variant(finish)] = _mcmaster_stock(supplies, finish, threading == "FULL")
    return screws


GOBILDA_STORE = "https://www.gobilda.com"
# goBILDA's are zinc-plated steel, sold by the SKU of their series, size and length, like 2800-0004-0010; threaded their
# whole length but for its 75 mm socket head
_GOBILDA_SCREWS = {
    ("M4", "SOCKET"): ("2800-0004", [5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 25, 30, 35, 40, 45, 50, 55, 60, 75],
                       "/m4-socket-head-screws"),
    ("M4", "BUTTON"): ("2802-0004", [5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 30, 35, 40, 45, 50, 55, 60],
                       "/m4-button-head-screws"),
    ("M4", "HEX"): ("2813-0004", [8, 10, 12, 16], "/m4-hex-head-screws"),
    ("M3", "SOCKET"): ("2800-0003", [6, 8, 10, 12], "/m3-socket-head-screws"),
}
# REV's are zinc-plated steel, threaded their whole length
REV_STORE = "https://www.revrobotics.com"
_REV_SCREWS = {
    ("M3", "HEX"): {
        6: "REV-41-1713", 8: "REV-41-1359", 10: "REV-41-1123", 16: "REV-41-1360", 20: "REV-41-1124", 25: "REV-41-1715",
        35: "REV-41-1125",
    },
}
_REV_SCREW_PAGES = {"HEX": "/M3-Hex-Cap-Screws/"}


def _gobilda_screws() -> Screws:
    return {
        ("GOBILDA", size, head): {
            Millimeters(length): {
                Variant("ZINC_PLATED"): Stock(f"{series}-{length:04d}", GOBILDA_STORE + page, STEEL, length <= 60)
            }
            for length in lengths
        }
        for (size, head), (series, lengths, page) in _GOBILDA_SCREWS.items()
    }


def _rev_screws() -> Screws:
    return {
        ("REV", size, head): {
            Millimeters(length): {Variant("ZINC_PLATED"): Stock(part, REV_STORE + _REV_SCREW_PAGES[head], STEEL, True)}
            for length, part in parts.items()
        }
        for (size, head), parts in _REV_SCREWS.items()
    }


# `M4-0.7 x 10 mm Socket Head Cap Screw`, `M3-0.5 x 5 mm Zinc Flat Head Cap Screw with Nylon Patch`
_METRIC_STORE_SCREW = re.compile(
    r"^(?P<size>M[34])-0\.[57] x (?P<length>\d+) mm (?:Zinc |Black Oxide )?"
    r"(?P<head>Button Head Cap Screw|Socket Head Cap Screw|Flat Head Cap Screw)"
    r"(?: Black Oxide)?(?P<patch> with (?:Nylon )?(?:Thread (?:Lock )?)?Patch)?$"
)
# AndyMark's finish where it isn't zinc plating, which its titles leave out; its M4 flat heads take a Phillips driver
_ANDYMARK_FINISHES = {"am-1251": "BLACK", "am-1441": "BLACK", "am-1379": "STAINLESS", "am-1659": "STAINLESS"}
_ANDYMARK_PHILLIPS = {"am-1788", "am-1789"}


def _metric_store_screw(product: dict) -> StoreScrew | None:
    """AndyMark's metric screws, as `_store_screw` reads its imperial ones."""
    if product["store"] != "andymark.com" or product["sku"] in _ANDYMARK_PHILLIPS:
        return None
    match = _METRIC_STORE_SCREW.match(product["title"])
    if match is None:
        return None
    finish = _ANDYMARK_FINISHES.get(product["sku"], "ZINC_PLATED")
    return StoreScrew(
        ("ANDYMARK", match["size"], _STORE_HEADS[match["head"]]),
        Millimeters(int(match["length"])),
        Variant(finish),
        bool(match["patch"]),
        Stock(product["sku"], product["url"], _FINISH_MATERIALS.get(finish, STEEL)),
    )


def _andymark_metric_screws() -> Screws:
    chosen: dict[tuple, StoreScrew] = {}
    for product in json.loads(STORES_SNAPSHOT.read_text())["products"]:
        screw = _metric_store_screw(product)
        if screw is None:
            continue
        slot = (screw.key, screw.length, screw.variant)
        if slot not in chosen or chosen[slot].has_patch and not screw.has_patch:
            chosen[slot] = screw
    screws: Screws = {}
    for screw in chosen.values():
        screws.setdefault(screw.key, {}).setdefault(screw.length, {})[screw.variant] = screw.stock
    return screws


# Finishes in the order FTC's studios list them, cheapest first (docs/ftc-fasteners.md): McMaster-Carr's stainless
# socket and button heads cost no more than its others, and its zinc-plated nuts and washers least
METRIC_SCREW_FINISHES = ["STAINLESS", "ZINC_PLATED", "BLACK"]
METRIC_FINISHES = ["ZINC_PLATED", "STAINLESS", "BLACK"]


def _metric_screws() -> Screws:
    """Generic offers every length any vendor makes, in the first finish alone."""
    stocked: Screws = {}
    for screws in (_mcmaster_screws(), _gobilda_screws(), _rev_screws(), _andymark_metric_screws()):
        stocked.update(screws)
    for (_, size, head), lengths in list(stocked.items()):
        generic = stocked.setdefault(("GENERIC", size, head), {})
        for length in lengths:
            generic[length] = {Variant(METRIC_SCREW_FINISHES[0]): Stock(material=STAINLESS)}
    order = {finish: index for index, finish in enumerate(METRIC_SCREW_FINISHES)}
    return {
        key: {
            length: dict(sorted(variants.items(), key=lambda item: (order[item[0].finish], item[0].full_thread)))
            for length, variants in sorted(lengths.items())
        }
        for key, lengths in sorted(stocked.items(), key=_screw_order)
    }


METRIC_SCREWS = _metric_screws()
SCREWS = {**SCREWS, **METRIC_SCREWS}
# Imperial lengths first, as they were before FTC's
LENGTHS = sorted({length for key, lengths in SCREWS.items() if key[1] in IMPERIAL for length in lengths}) + sorted(
    {length for key, lengths in METRIC_SCREWS.items() for length in lengths}
)


def has_full_thread_choice(variants: dict[Variant, Stock], finish: str) -> bool:
    """Whether a length is sold both partly and fully threaded in a finish, so the studio asks which."""
    return Variant(finish) in variants and Variant(finish, True) in variants


def length_id(length: Length) -> str:
    """e.g. IN_0_375 for 3/8 in, IN_1_25 for 1-1/4 in, MM_10 for 10 mm"""
    if isinstance(length, Millimeters):
        return f"MM_{length.value}"
    return "IN_" + f"{float(length):g}".replace(".", "_")


def length_meters(length: Length) -> float:
    return round(length.value / 1000 if isinstance(length, Millimeters) else float(length) * INCH, 9)


def length_name(length: Length) -> str:
    """e.g. 3/8 in, 1-1/4 in, or 10 mm"""
    if isinstance(length, Millimeters):
        return f"{length.value} mm"
    whole, part = divmod(length, 1)
    if part == 0:
        return f"{whole} in"
    if whole == 0:
        return f"{part} in"
    return f"{whole}-{part} in"


ScrewLength = Enum(
    "ScrewLength", [length_id(length) for length in LENGTHS], {length_id(length): length_name(length) for length in LENGTHS}
)


# Nuts: McMaster-Carr's alone, like `#10-32 Thin Nylon-Insert Locknut`


NutType = Enum(
    "NutType",
    ["NYLOCK", "THIN_NYLOCK", "HEX", "THIN_HEX", "SQUARE"],
    {"NYLOCK": "Nylock", "THIN_NYLOCK": "Thin nylock", "HEX": "Hex", "THIN_HEX": "Thin hex", "SQUARE": "Square"},
)
NUT_CODES = {
    "Nylon-Insert Locknut": "NYLOCK",
    "Thin Nylon-Insert Locknut": "THIN_NYLOCK",
    "Hex Nut": "HEX",
    "Thin Hex Nut": "THIN_HEX",
    "Square Nut": "SQUARE",
}


@dataclasses.dataclass
class Nut:
    # A square nut's width
    across_flats: float | Fraction
    height: float | Fraction
    # A nylock's: its hex's height, under the dome holding its nylon insert, and the insert's bore
    hex_height: float | Fraction | None = None
    insert_diameter: float | None = None
    unit: str = "in"


def _nylock(across_flats: float, height: float) -> Nut:
    """A nylock with no McMaster-Carr model measured, in the proportions of the measured ones: a dome a sixth of its
    height, and an insert 0.61 of its width across."""
    return Nut(across_flats, height, height * 5 / 6, 0.61 * across_flats)


# Measured from McMaster-Carr's models where there's a Fraction; the rest are nominal
NUT_DIMENSIONS = {
    "NO_4_40": {
        "NYLOCK": _nylock(0.25, 0.141),
        "THIN_NYLOCK": _nylock(0.25, 0.109),
        "HEX": Nut(0.25, 0.094),
        "THIN_HEX": Nut(0.25, 0.063),
    },
    "NO_6_32": {
        "NYLOCK": Nut(Fraction(5, 16), Fraction(11, 64), Fraction(55, 384), 0.183127),
        "THIN_NYLOCK": Nut(Fraction(5, 16), Fraction(1, 8), Fraction(5, 48), 0.190939),
        "HEX": Nut(Fraction(5, 16), Fraction(7, 64)),
        "THIN_HEX": Nut(Fraction(1, 4), Fraction(3, 32)),
    },
    "NO_8_32": {
        "NYLOCK": _nylock(0.34375, 0.203),
        "THIN_NYLOCK": _nylock(0.34375, 0.141),
        "HEX": Nut(0.34375, 0.125),
        "SQUARE": Nut(0.34375, 0.125),
    },
    "NO_10_32": {
        "NYLOCK": Nut(Fraction(3, 8), Fraction(15, 64), Fraction(25, 128), 0.228966),
        "THIN_NYLOCK": Nut(Fraction(3, 8), Fraction(11, 64), Fraction(17, 128), 0.228966),
        "HEX": Nut(Fraction(3, 8), Fraction(1, 8)),
        "SQUARE": Nut(Fraction(3, 8), Fraction(1, 8)),
    },
    "QUARTER_20": {
        "NYLOCK": _nylock(0.4375, 0.312),
        "THIN_NYLOCK": _nylock(0.4375, 0.203),
        "HEX": Nut(0.4375, 0.219),
        "SQUARE": Nut(Fraction(7, 16), Fraction(7, 32)),
    },
}

_NUT = re.compile(r"^(#\d+-\d+|1/4-20)\s+(.+)$")


# Nuts come in finishes, FRC's in McMaster-Carr's zinc plating alone
NutFinish = Enum("NutFinish", ["ZINC_PLATED", "STAINLESS", "BLACK"], FINISH_NAMES)
FRC_NUT_FINISH = "ZINC_PLATED"


def _stocked_nuts() -> dict[tuple[str, str, str, str], Stock]:
    stocked: dict[tuple[str, str, str, str], Stock] = {}
    for part in SNAPSHOT["parts"]:
        if not part["name"].endswith(" Nut"):
            continue
        for record in part["records"]:
            match = _NUT.match(record["name"])
            assert match, record["name"]
            size, kind = match.groups()
            key = (SIZES_BY_NAME[size].id, NUT_CODES[kind])
            stocked[("MCMASTER", *key, FRC_NUT_FINISH)] = Stock(record["partNumber"], record.get("url"))
    assert {(size, type) for _, size, type, _ in stocked} == {
        (size, type) for size, types in NUT_DIMENSIONS.items() for type in types
    }, "Every nut needs its dimensions"
    return dict(
        sorted(
            stocked.items(),
            key=lambda item: (
                FastenerVendor.values.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                NutType.values.index(item[0][2]),
            ),
        )
    )


NUTS = _stocked_nuts()


# FTC's metric nuts, in millimeters, by vendor, size, and type, since their vendors' differ: McMaster-Carr's from its
# tables and its models of 90591A255 and 94645A101, goBILDA's and REV's from theirs, and Generic's and AndyMark's as
# McMaster-Carr's and REV's of the same type (AndyMark publishes no models). An M3 nylock is REV's REV-41-1361.
_M3_NYLOCK = Nut(5.5, 4, 3.33, 3.35, "mm")
_M4_NYLOCK = Nut(7, 5, 4.17, 5.05, "mm")
METRIC_NUT_DIMENSIONS = {
    ("GENERIC", "M3", "NYLOCK"): _M3_NYLOCK,
    ("GENERIC", "M4", "NYLOCK"): _M4_NYLOCK,
    ("GENERIC", "M3", "HEX"): Nut(5.5, 2.4, unit="mm"),
    ("GENERIC", "M4", "HEX"): Nut(7, 3.2, unit="mm"),
    ("MCMASTER", "M3", "NYLOCK"): _M3_NYLOCK,
    ("MCMASTER", "M4", "NYLOCK"): _M4_NYLOCK,
    ("MCMASTER", "M3", "HEX"): Nut(5.5, 2.4, unit="mm"),
    ("MCMASTER", "M4", "HEX"): Nut(7, 3.2, unit="mm"),
    ("GOBILDA", "M4", "NYLOCK"): Nut(7, 4.85, 4.04, 4.37, "mm"),
    ("GOBILDA", "M4", "HEX"): Nut(7, 2.75, unit="mm"),
    ("GOBILDA", "M4", "SQUARE"): Nut(7, 3, unit="mm"),
    ("REV", "M3", "NYLOCK"): _M3_NYLOCK,
    ("REV", "M3", "HEX"): Nut(5.5, 2.4, unit="mm"),
    ("ANDYMARK", "M3", "NYLOCK"): _M3_NYLOCK,
    ("ANDYMARK", "M3", "HEX"): Nut(5.5, 2.4, unit="mm"),
    ("ANDYMARK", "M3", "THIN_HEX"): Nut(5.5, 1.8, unit="mm"),
    ("ANDYMARK", "M4", "NYLOCK"): _M4_NYLOCK,
}
_GOBILDA_NUTS = {("M4", "NYLOCK"): "2812-0004-0007", ("M4", "HEX"): "2811-0004-0007", ("M4", "SQUARE"): "2803-0007-0007"}
_REV_NUTS = {("M3", "NYLOCK"): "REV-41-1361", ("M3", "HEX"): "REV-41-1126"}
_ANDYMARK_NUTS = {
    ("M3", "NYLOCK"): ("am-1023", "m3-0-5-nylock-nut"),
    ("M3", "HEX"): ("am-1386", "m3-nut"),
    ("M3", "THIN_HEX"): ("am-1405", "m3-0-5-jam-nut"),
    ("M4", "NYLOCK"): ("am-1435", "m4-0-7-nylock-nut"),
}


def nut_dimensions(vendor: str, size: str, type: str) -> Nut:
    return METRIC_NUT_DIMENSIONS[(vendor, size, type)] if size in METRIC else NUT_DIMENSIONS[size][type]


def _metric_nuts() -> dict[tuple[str, str, str, str], Stock]:
    stocked: dict[tuple[str, str, str, str], Stock] = {}
    for vendor, size, type in METRIC_NUT_DIMENSIONS:
        if vendor == "GENERIC":
            stocked[(vendor, size, type, METRIC_FINISHES[0])] = Stock()
    for type, sizes in MCMASTER_METRIC["nuts"].items():
        for size, finishes in sizes.items():
            for finish in METRIC_FINISHES:
                stocked[("MCMASTER", size, type, finish)] = _mcmaster_stock(finishes[finish], finish)
    for (size, type), part in _GOBILDA_NUTS.items():
        stocked[("GOBILDA", size, type, "ZINC_PLATED")] = Stock(part, GOBILDA_STORE + "/nuts/")
    for (size, type), part in _REV_NUTS.items():
        stocked[("REV", size, type, "ZINC_PLATED")] = Stock(part, REV_STORE + "/M3-Nuts/")
    for (size, type), (part, handle) in _ANDYMARK_NUTS.items():
        stocked[("ANDYMARK", size, type, "ZINC_PLATED")] = Stock(part, f"https://andymark.com/products/{handle}")
    assert {key[:3] for key in stocked} == set(METRIC_NUT_DIMENSIONS), "Every nut needs its dimensions"
    return dict(
        sorted(
            stocked.items(),
            key=lambda item: (
                FastenerVendor.values.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                NutType.values.index(item[0][2]),
                METRIC_FINISHES.index(item[0][3]),
            ),
        )
    )


METRIC_NUTS = _metric_nuts()
NUTS = {**NUTS, **METRIC_NUTS}


# Washers, like `#10 Washer (7/16" OD, 0.03" WD, Stainless Steel, Black Oxide)` or AndyMark's
# `0.281" Washer (1" OD, 0.062" WD, Steel, Zink Finish)`; Generic's are SAE flat, and black


@dataclasses.dataclass(frozen=True)
class Washer:
    inner_diameter: float
    outer_diameter: Fraction
    thickness: str
    stock: Stock = Stock()
    unit: str = "in"

    @property
    def type(self) -> str:
        """e.g. OD_7_16_X_0_03, for 7/16 in OD and 0.03 in thick, or MM_9_X_0_9"""
        if self.unit == "mm":
            return f"MM_{self.outer_diameter:g}_X_{self.thickness}".replace(".", "_")
        return washer_type(self.outer_diameter, self.thickness)

    @property
    def type_name(self) -> str:
        """e.g. 7/16 in OD x 0.03 in, or 9 mm OD x 0.9 mm"""
        if self.unit == "mm":
            return f"{self.outer_diameter:g} mm OD x {self.thickness} mm"
        return washer_type_name(self.outer_diameter, self.thickness)


# SAE flat washers' inner diameters, which FRCDesign's records leave out
WASHER_INNER_DIAMETERS = {"NO_4_40": 0.125, "NO_6_32": 0.156, "NO_8_32": 0.188, "NO_10_32": 0.219, "QUARTER_20": 0.281}
GENERIC_WASHERS = {
    "NO_4_40": ("5/16", "0.032"),
    "NO_6_32": ("3/8", "0.049"),
    "NO_8_32": ("7/16", "0.049"),
    "NO_10_32": ("1/2", "0.049"),
    "QUARTER_20": ("5/8", "0.065"),
}
_WASHER = re.compile(r'^(#\d+|[\d/\.-]+"(?: ID)?) Washer \(([\d/\.]+)" OD, ([\d\.]+)" WD, ([^)]*)\)')
# The sizes that fit a screw; AndyMark's bores, like `0.281"`, are stated
_WASHER_SIZES = {
    "#4": "NO_4_40",
    "#6": "NO_6_32",
    "#8": "NO_8_32",
    "#10": "NO_10_32",
    '1/4" ID': "QUARTER_20",
    '0.252"': "QUARTER_20",
    '0.281"': "QUARTER_20",
}
# FRCDesign's record names am-1027 by its outer diameter; its configuration option says its bore is 0.281 in
_WASHER_NAMES = {"am-1027": '0.281" Washer (0.625" OD, 0.063" WD, Steel, Zink Finish)'}


def washer_type(outer_diameter: Fraction, thickness: str) -> str:
    """e.g. OD_7_16_X_0_03, for 7/16 in OD and 0.03 in thick."""
    return f"OD_{outer_diameter}_X_{thickness}".replace("/", "_").replace(".", "_")


def washer_type_name(outer_diameter: Fraction, thickness: str) -> str:
    """e.g. 7/16 in OD x 0.03 in"""
    return f"{length_name(outer_diameter)} OD x {thickness} in"


def _washers() -> dict[tuple[str, str, str, str], Washer]:
    """By vendor, size, type, and finish."""
    washers: dict[tuple[str, str, str, str], Washer] = {}
    for size, (outer, thickness) in GENERIC_WASHERS.items():
        od = Fraction(outer)
        washers[("GENERIC", size, washer_type(od, thickness), STANDARD_FINISH)] = Washer(
            WASHER_INNER_DIAMETERS[size], od, thickness
        )
    for part in SNAPSHOT["parts"]:
        if "Washer" not in part["name"]:
            continue
        vendor = _vendor(part)
        for record in part["records"]:
            name = _WASHER_NAMES.get(record["partNumber"], record["name"])
            match = _WASHER.match(name)
            assert match, name
            size_name, outer, thickness, finish_text = match.groups()
            if size_name not in _WASHER_SIZES:
                continue
            material, finish = parse_finish(finish_text)
            size = _WASHER_SIZES[size_name]
            inner = float(size_name[:-1]) if size_name.endswith('"') else WASHER_INNER_DIAMETERS[size]
            od = parse_inches(outer)
            washers[(vendor, size, washer_type(od, thickness), finish)] = Washer(
                inner, od, thickness, Stock(record["partNumber"], record.get("url"), material)
            )
    return dict(
        sorted(
            washers.items(),
            key=lambda item: (
                FastenerVendor.values.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                item[1].outer_diameter,
                float(item[1].thickness),
                FINISH_ORDER.index(item[0][3]),
            ),
        )
    )


WASHERS = _washers()


# FTC's metric washers, in millimeters: general purpose ones, Generic's ISO 7089 and in its first finish, McMaster-Carr's
# from its tables, goBILDA's from its models, and REV's taken as standard (it publishes no dimensions)
GENERIC_METRIC_WASHERS = {"M3": (3.2, 7, "0.5"), "M4": (4.3, 9, "0.8")}
_GOBILDA_WASHERS = {"2801-0004-0008": (4, 8, "1"), "2801-0004-0011": (4.15, 10.9, "1.5")}


def _metric_washer(size: tuple, stock: Stock = Stock()) -> Washer:
    inner, outer, thickness = size
    return Washer(inner, Fraction(str(outer)), thickness, stock, "mm")


def _metric_washers() -> dict[tuple[str, str, str, str], Washer]:
    washers: dict[tuple[str, str, str, str], Washer] = {}

    def add(vendor: str, size: str, finish: str, washer: Washer) -> None:
        washers[(vendor, size, washer.type, finish)] = washer

    for size, dimensions in GENERIC_METRIC_WASHERS.items():
        add("GENERIC", size, METRIC_FINISHES[0], _metric_washer(dimensions))
    for size, washer in MCMASTER_METRIC["washers"].items():
        dimensions = washer["dimensions"]
        shape = (dimensions["innerDiameter"], dimensions["outerDiameter"], f"{dimensions['thickness']:g}")
        for finish in METRIC_FINISHES:
            add("MCMASTER", size, finish, _metric_washer(shape, _mcmaster_stock(washer[finish], finish)))
    for part, shape in _GOBILDA_WASHERS.items():
        add("GOBILDA", "M4", "ZINC_PLATED", _metric_washer(shape, Stock(part, GOBILDA_STORE + "/washers")))
    add("REV", "M3", "STAINLESS", _metric_washer((3.2, 7, "0.5"), Stock("REV-41-2886", REV_STORE + "/rev-41-2886/", STAINLESS)))
    return dict(
        sorted(
            washers.items(),
            key=lambda item: (
                FastenerVendor.values.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                item[1].outer_diameter,
                float(item[1].thickness),
                METRIC_FINISHES.index(item[0][3]),
            ),
        )
    )


METRIC_WASHERS = _metric_washers()
# Imperial types first, as they were before FTC's
_WASHER_TYPES = {
    washer.type: washer.type_name
    for washer in sorted(
        WASHERS.values(), key=lambda washer: (washer.outer_diameter, float(washer.thickness))
    )
    + sorted(METRIC_WASHERS.values(), key=lambda washer: (washer.outer_diameter, float(washer.thickness)))
}
WASHERS = {**WASHERS, **METRIC_WASHERS}
WasherType = Enum("WasherType", list(_WASHER_TYPES), _WASHER_TYPES)


# What each part is called and sold as: the features set these, and FRCDesignApp indexes the same records


SIZES_BY_ID = {size.id: size for size in SIZES + METRIC_SIZES}
HEAD_NAMES = {"SOCKET": "Socket Head", "BUTTON": "Button Head", "FLAT": "Flat Head", "HEX": "Hex Head"}
NUT_NAMES = {"NYLOCK": "Nylock", "THIN_NYLOCK": "Thin Nylock", "HEX": "Hex", "THIN_HEX": "Thin Hex", "SQUARE": "Square"}
# The finish a metric nut's or washer's name leaves unsaid
METRIC_PLAIN_FINISH = "ZINC_PLATED"


def screw_name(size: str, head: str, length: Length, variant: Variant = STANDARD, vendor: str = "GENERIC") -> str:
    """e.g. `#10-32 x 1/2 in Button Head Screw`, or `... Socket Head Screw (Fully threaded)` beyond the standard. A
    metric one is by its size alone, and says its finish but for Generic's, which comes in one: `M4 x 10 mm Socket Head
    Screw (Stainless steel)`."""
    if size in IMPERIAL:
        name = f"{SIZES_BY_ID[size].name} x {length_name(length)} {HEAD_NAMES[head]} Screw"
        return name if variant == STANDARD else f"{name} ({variant.name})"
    name = f"{SIZES_BY_ID[size].hole_table_size} x {length_name(length)} {HEAD_NAMES[head]} Screw"
    words = ([] if vendor == "GENERIC" else [FINISH_NAMES[variant.finish]]) + (
        ["fully threaded"] if variant.full_thread else []
    )
    words = [word[:1].upper() + word[1:] if index == 0 else word for index, word in enumerate(words)]
    return f"{name} ({', '.join(words)})" if words else name


def nut_name(size: str, type: str, finish: str = FRC_NUT_FINISH) -> str:
    """e.g. `#10-32 Thin Nylock Nut`, or `M4 Nylock Nut (Stainless steel)`."""
    if size in IMPERIAL:
        return f"{SIZES_BY_ID[size].name} {NUT_NAMES[type]} Nut"
    name = f"{SIZES_BY_ID[size].hole_table_size} {NUT_NAMES[type]} Nut"
    return name if finish == METRIC_PLAIN_FINISH else f"{name} ({FINISH_NAMES[finish]})"


def washer_name(size: str, type: str, finish: str = STANDARD_FINISH) -> str:
    """e.g. `#10 Washer (1/2 in OD x 0.049 in)`, `1/4 in Washer (...)`, `#10 Washer (..., Stainless steel)`, or `M4
    Washer (9 mm OD x 0.9 mm)`."""
    hole = SIZES_BY_ID[size].hole_table_size
    plain = STANDARD_FINISH if size in IMPERIAL else METRIC_PLAIN_FINISH
    described = [WasherType.names[type]] + ([] if finish == plain else [FINISH_NAMES[finish]])  # type: ignore[index]
    return f"{hole}{' in' if hole == '1/4' else ''} Washer ({', '.join(described)})"


def vendor_name(vendor: str) -> str | None:
    """The name a part's Vendor property says; Generic parts have none."""
    return None if vendor == "GENERIC" else FastenerVendor.names[vendor]  # type: ignore[index]


@dataclasses.dataclass(frozen=True)
class Record:
    """One configuration of a studio: its inputs' options, and the part it makes."""

    values: dict[str, str]
    name: str
    vendor: str
    stock: Stock
    finish: str = STANDARD_FINISH
    # robotProperties.fs's constant, when not its finish's
    appearance: str | None = None

    def fields(self) -> dict[str, str]:
        """What the FeatureScript table holds for it, which the feature sets as the part's properties."""
        vendor = vendor_name(self.vendor)
        return {
            "name": string(self.name),
            **({"vendor": string(vendor)} if vendor else {}),
            **_stock_fields(self.stock),
            "material": self.stock.material.constant,
            "appearance": self.appearance or APPEARANCES[self.finish],
            **({"fullyThreaded": "true"} if self.stock.full_thread else {}),
        }

    def manifest(self) -> dict:
        vendor = vendor_name(self.vendor)
        return {
            "values": self.values,
            "name": self.name,
            **({"vendor": vendor} if vendor else {}),
            **_stock_json(self.stock),
            "material": self.stock.material.name,
        }


# The studios' configurations: each a lookup table of what's made (see `LookupTable`), whose inputs offer only what's
# made with the choices before them, and are hidden where that's one thing. Each part set's studios offer its own sizes
# (`Catalog`): FRC's imperial ones here, FTC's metric ones in ftcFastenerTables.py.


def _list(id: str, name: str, enum: Enum, parameter: str, default: str, order: list[str] | None = None) -> Input:
    """An input offering every value of `enum`, those in `order` first, for its table to narrow to what's made."""
    assert enum.names is not None
    values = (order or []) + [value for value in enum.values if value not in (order or [])]
    options = [Option(value, enum.names[value], value) for value in values]
    return Input(id, name, parameter, options, enum_name=enum.name, default=default)


@dataclasses.dataclass(frozen=True)
class Defaults:
    """Each studio's inputs' defaults, by its table's columns."""

    screw: tuple[str, str, str, str, str]
    nut: tuple[str, str, str, str]
    washer: tuple[str, str, str, str]


class Catalog:
    """One part set's studios, laid out from the parts made in its sizes, and their records: what FRCDesign indexes.

    Args:
        finishes: The finishes' order in each studio, cheapest first, where it isn't the enums' own.
    """

    def __init__(
        self,
        sizes: set[str],
        defaults: Defaults,
        screw_finishes: list[str] | None = None,
        finishes: list[str] | None = None,
    ):
        self.sizes = [size for size in FastenerSize.values if size in sizes]
        self.screws = {key: lengths for key, lengths in SCREWS.items() if key[1] in sizes}
        self.nuts = {key: stock for key, stock in NUTS.items() if key[1] in sizes}
        self.washers = {key: washer for key, washer in WASHERS.items() if key[1] in sizes}
        self.screw_finishes = screw_finishes
        self.finishes = finishes
        self.screw_table = LookupTable(
            ("vendor", "size", "head", "finish", "length"),
            [
                key + (variant.finish, length_id(length))
                for key, lengths in self.screws.items()
                for length, variants in lengths.items()
                for variant in variants
            ],
        )
        self.nut_table = LookupTable(("vendor", "size", "type", "finish"), list(self.nuts))
        self.washer_table = LookupTable(("vendor", "size", "type", "finish"), list(self.washers))
        self.studios = {
            # Nearly every length is in one finish, so Length's conditions are smallest split by finish first
            "screw": ConfiguredStudio(
                "screw",
                "fastenerScrew",
                self._screw_inputs(defaults.screw),
                factor_first=["finish"],
                mate_connector="mateConnector",
            ),
            "nut": ConfiguredStudio("nut", "fastenerNut", self._nut_inputs(defaults.nut), mate_connector="mateConnector"),
            "washer": ConfiguredStudio(
                "washer", "fastenerWasher", self._washer_inputs(defaults.washer), mate_connector="mateConnector"
            ),
        }
        self.records = {"screw": self._screw_records(), "nut": self._nut_records(), "washer": self._washer_records()}

    def _screw_inputs(self, defaults: tuple[str, str, str, str, str]) -> list[Input]:
        vendor, size, head, finish, length = defaults
        both = [
            self.screw_table.shown(key + (variant.finish, length_id(stocked_length)))
            for key, stocked in self.screws.items()
            for stocked_length, variants in stocked.items()
            for variant in variants
            if not variant.full_thread and has_full_thread_choice(variants, variant.finish)
        ]
        table = self.screw_table
        return [
            table.input(_list("vendor", "Vendor", FastenerVendor, "vendor", vendor)),
            table.input(_list("size", "Size", FastenerSize, "size", size)),
            table.input(_list("head", "Head", ScrewHead, "head", head)),
            table.input(_list("finish", "Finish", ScrewFinish, "finish", finish, self.screw_finishes)),
            table.input(_list("length", "Length", ScrewLength, "length", length)),
            # Hidden, and so partly threaded, wherever a length is only sold one way: a switch, since a checkbox shows
            # by one condition, and this needs several
            Input(
                "fullyThreaded",
                "Fully threaded",
                "fullyThreaded",
                [Option("false", "No", "false", both), Option("true", "Yes", "true", both)],
                kind="switch",
            ),
        ]

    def _nut_inputs(self, defaults: tuple[str, str, str, str]) -> list[Input]:
        vendor, size, type, finish = defaults
        table = self.nut_table
        return [
            table.input(_list("vendor", "Vendor", FastenerVendor, "vendor", vendor)),
            table.input(_list("size", "Size", FastenerSize, "size", size)),
            table.input(_list("type", "Type", NutType, "nutType", type)),
            table.input(_list("finish", "Finish", NutFinish, "finish", finish, self.finishes)),
        ]

    def _washer_inputs(self, defaults: tuple[str, str, str, str]) -> list[Input]:
        vendor, size, type, finish = defaults
        table = self.washer_table
        return [
            table.input(_list("vendor", "Vendor", FastenerVendor, "vendor", vendor)),
            table.input(_list("size", "Size", FastenerSize, "size", size)),
            table.input(_list("type", "Type", WasherType, "washerType", type)),
            table.input(_list("finish", "Finish", WasherFinish, "finish", finish, self.finishes)),
        ]

    def _screw_records(self) -> dict[tuple[str, str, str, str, str, bool], Record]:
        """By vendor, size, head, finish, length, and whether it's fully threaded. Fully threaded is only chosen where
        a length is sold both ways in its finish, and is false where it's hidden."""
        return {
            (vendor, size, head, variant.finish, length_id(length), variant.full_thread): Record(
                {
                    **self.screw_table.shown((vendor, size, head, variant.finish, length_id(length))),
                    **(
                        {"fullyThreaded": str(variant.full_thread).lower()}
                        if has_full_thread_choice(variants, variant.finish)
                        else {}
                    ),
                },
                screw_name(size, head, length, variant, vendor),
                vendor,
                stock,
                variant.finish,
            )
            for (vendor, size, head), lengths in self.screws.items()
            for length, variants in lengths.items()
            for variant, stock in variants.items()
        }

    def _nut_records(self) -> dict[tuple[str, str, str, str], Record]:
        """The standards' color is a nut's, whatever its finish, but a black or stainless one's own."""
        return {
            (vendor, size, type, finish): Record(
                self.nut_table.shown((vendor, size, type, finish)),
                nut_name(size, type, finish),
                vendor,
                stock,
                finish,
                appearance=NUT_APPEARANCE if finish == FRC_NUT_FINISH else None,
            )
            for (vendor, size, type, finish), stock in self.nuts.items()
        }

    def _washer_records(self) -> dict[tuple[str, str, str, str], Record]:
        return {
            (vendor, size, type, finish): Record(
                self.washer_table.shown((vendor, size, type, finish)),
                washer_name(size, type, finish),
                vendor,
                washer.stock,
                finish,
            )
            for (vendor, size, type, finish), washer in self.washers.items()
        }

    def washer_finishes(self, vendor: str, size: str, type: str) -> list[str]:
        return [finish for (v, s, t, finish) in self.washers if (v, s, t) == (vendor, size, type)]

    def nut_finishes(self, vendor: str, size: str, type: str) -> list[str]:
        return [finish for (v, s, t, finish) in self.nuts if (v, s, t) == (vendor, size, type)]


# The defaults are what's inserted most from FRCDesign's library: #10-32 x 3/8 in black button heads (from Generic, which
# names no vendor), McMaster-Carr's #10 x 7/16 in OD stainless washers, and #10-32 nylocks
FRC = Catalog(
    IMPERIAL,
    Defaults(
        ("GENERIC", "NO_10_32", "BUTTON", STANDARD_FINISH, length_id(Fraction(3, 8))),
        ("MCMASTER", "NO_10_32", "NYLOCK", FRC_NUT_FINISH),
        ("MCMASTER", "NO_10_32", "OD_7_16_X_0_03", "STAINLESS"),
    ),
)
# FTC's, from what's inserted most from ConfigLib (docs/ftc-fasteners.md): M4 socket heads, nylocks and washers, from the
# first vendor making each, in its cheapest finish; ConfigLib doesn't count lengths
FTC = Catalog(
    METRIC,
    Defaults(
        ("GENERIC", "M4", "SOCKET", METRIC_SCREW_FINISHES[0], length_id(Millimeters(10))),
        ("GENERIC", "M4", "NYLOCK", METRIC_FINISHES[0]),
        # Type shows only for goBILDA's, which makes two: its 8 mm one is its usual
        ("GENERIC", "M4", "MM_8_X_1", METRIC_FINISHES[0]),
    ),
    METRIC_SCREW_FINISHES,
    METRIC_FINISHES,
)

SCREW_TABLE, NUT_TABLE, WASHER_TABLE = FRC.screw_table, FRC.nut_table, FRC.washer_table
STUDIOS = FRC.studios
RECORDS = FRC.records
SCREW_RECORDS, NUT_RECORDS, WASHER_RECORDS = RECORDS["screw"], RECORDS["nut"], RECORDS["washer"]
# Every part either set makes, which the feature's tables hold
ALL_RECORDS = {kind: {**FRC.records[kind], **FTC.records[kind]} for kind in RECORDS}

# Where each studio was last released: `fs release` records it beside its definition, FTC's beside
# ftcFastenerTables.py
LOCATIONS = read_locations(pathlib.Path(__file__))


# FeatureScript


def _in(value: float | Fraction) -> str:
    return inch(round(float(value), 6))


def _map(entries: list[tuple[str, str]], indent: int) -> str:
    pad = " " * indent
    return "{\n" + ",\n".join(f"{pad}    {key} : {value}" for key, value in entries) + f"\n{pad}}}"


def _fields(fields: dict[str, str]) -> str:
    if not fields:
        return "{}"
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in fields.items()) + " }"


def _stock_fields(stock: Stock) -> dict[str, str]:
    return {key: string(value) for key, value in (("partNumber", stock.part_number), ("url", stock.url)) if value}


def _nested(table: dict[tuple, object], enums: list[Enum], render, indent: int = 8) -> str:
    """A map of maps, keyed by each level's enum, from a table keyed by tuples of their values."""
    groups: dict[str, dict] = {}
    for key, value in table.items():
        groups.setdefault(key[0], {})[key[1:]] = value
    entries = [
        (enums[0][first], render(rest[()]) if len(enums) == 1 else _nested(rest, enums[1:], render, indent + 4))
        for first, rest in groups.items()
    ]
    return _map(entries, indent)


def _length(value: float | Fraction | str, unit: str) -> str:
    return _in(float(value)) if unit == "in" else mm(round(float(value), 6))


def _size_fields(size: Size | MetricSize) -> str:
    if isinstance(size, Size):
        pitch = {"pitch": string(f"{size.threads_per_inch} tpi"), "threadPitch": f"inch / {size.threads_per_inch}"}
    else:
        pitch = {"pitch": string(f"{size.pitch:g} mm"), "threadPitch": mm(size.pitch)}
    return _fields(
        {
            "name": string(size.name),
            "size": string(size.hole_table_size),
            **pitch,
            "majorDiameter": _length(size.major_diameter, size.unit),
            "minorDiameter": _length(size.minor_diameter, size.unit),
            "tapDrillDiameter": _length(size.tap_drill_diameter, size.unit),
            "closeClearanceDiameter": _length(size.close_clearance_diameter, size.unit),
            "freeClearanceDiameter": _length(size.free_clearance_diameter, size.unit),
            "threadAllowance": _length(size.thread_allowance, size.unit),
            "metric": str(isinstance(size, MetricSize)).lower(),
        }
    )


def _head_fields(head: Head) -> str:
    fields = {"headDiameter": _length(head.diameter, head.unit), "headHeight": _length(head.height, head.unit)}
    if head.socket is not None:
        fields["socketSize"] = _length(head.socket, head.unit)
    if head.countersunk:
        fields["countersinkAngle"] = f"{head.countersink_angle} * degree"
    if head.across_flats is not None:
        fields["acrossFlats"] = _length(head.across_flats, head.unit)
    return _fields(fields)


def _screw_table() -> dict[tuple, dict[bool, Record]]:
    table: dict[tuple, dict[bool, Record]] = {}
    for (*key, full_thread), record in ALL_RECORDS["screw"].items():
        table.setdefault(tuple(key), {})[full_thread] = record
    return table


def _screw_fields(threads: dict[bool, Record]) -> str:
    entries = [(string("standard"), _fields(threads[False].fields()))]
    if True in threads:
        entries.append((string("fullThread"), _fields(threads[True].fields())))
    return "{ " + ", ".join(f"{key} : {value}" for key, value in entries) + " }"


def _nut_fields(item: tuple[tuple[str, str, str, str], Record]) -> str:
    (vendor, size, type, _), record = item
    nut = nut_dimensions(vendor, size, type)
    nylock = (
        {"hexHeight": _length(nut.hex_height, nut.unit), "insertDiameter": _length(nut.insert_diameter, nut.unit)}
        if nut.hex_height is not None and nut.insert_diameter is not None
        else {}
    )
    return _fields(
        {
            "acrossFlats": _length(nut.across_flats, nut.unit),
            "height": _length(nut.height, nut.unit),
            **nylock,
            **record.fields(),
        }
    )


def _washer_fields(item: tuple[Washer, Record]) -> str:
    washer, record = item
    return _fields(
        {
            "innerDiameter": _length(washer.inner_diameter, washer.unit),
            "outerDiameter": _length(washer.outer_diameter, washer.unit),
            "thickness": _length(washer.thickness, washer.unit),
            **record.fields(),
        }
    )


def _documented(doc: str, name: str, expression: str) -> Code:
    return Code(f"/** {doc} */\nexport const {name} = {expression};")


CONTENTS = [
    Import("core/robotProperties.fs"),
    FastenerVendor,
    FastenerSize,
    ScrewHead,
    ScrewLength,
    ScrewFinish,
    WasherFinish,
    NutFinish,
    NutType,
    WasherType,
    Constant("FASTENER_SIZES", _map([(FastenerSize[size.id], _size_fields(size)) for size in SIZES + METRIC_SIZES], 8)),
    Constant(
        "SCREW_HEADS",
        _nested(
            {(size, head): value for size, heads in HEADS.items() for head, value in heads.items()},
            [FastenerSize, ScrewHead],
            _head_fields,
        ),
    ),
    Constant(
        "SCREW_LENGTHS",
        _map(
            [
                (
                    ScrewLength[length_id(length)],
                    _fields(
                        {
                            "length": mm(length.value) if isinstance(length, Millimeters) else _in(length),
                            "name": string(length_name(length)),
                        }
                    ),
                )
                for length in LENGTHS
            ],
            8,
        ),
    ),
    _documented(
        "What each vendor makes, by vendor, size, head, finish, and length: each screw's properties, partly threaded"
        " (`standard`) and, where it's sold so too, fully threaded (`fullThread`).",
        "SCREWS",
        _nested(
            _screw_table(),
            [FastenerVendor, FastenerSize, ScrewHead, ScrewFinish, ScrewLength],
            _screw_fields,
            4,
        ),
    ),
    Constant(
        "NUTS",
        _nested(
            {key: (key, record) for key, record in ALL_RECORDS["nut"].items()},
            [FastenerVendor, FastenerSize, NutType, NutFinish],
            _nut_fields,
        ),
    ),
    Constant(
        "WASHERS",
        _nested(
            {key: (WASHERS[key], record) for key, record in ALL_RECORDS["washer"].items()},
            [FastenerVendor, FastenerSize, WasherType, WasherFinish],
            _washer_fields,
        ),
    ),
]


# FRCDesign's manifest


def _meters(value: float | Fraction | str, unit: str = "in") -> float:
    return round(float(value) * (INCH if unit == "in" else 0.001), 9)


def _stock_json(stock: Stock) -> dict:
    return {key: value for key, value in (("partNumber", stock.part_number), ("url", stock.url)) if value}


def manifest(catalog: Catalog = FRC, locations: dict = LOCATIONS, source: str = "fastenerTables.py") -> dict:
    """A part set's manifest, for FRCDesign: its studios and records, and its catalog for Auto Assembly."""
    vendors = {key[0] for parts in (catalog.screws, catalog.nuts, catalog.washers) for key in parts}
    sizes = [SIZES_BY_ID[size] for size in catalog.sizes]
    heads = [head for head in ScrewHead.values if any(head in HEADS[size.id] for size in sizes)]
    metric = all(size.id in METRIC for size in sizes)
    screw_finishes = (catalog.screw_finishes or []) + [
        finish for finish in ScrewFinish.values if finish not in (catalog.screw_finishes or [])
    ]
    return {
        "source": f"featurescripts/fasteners/{source}",
        "studios": {
            kind: {
                **studio.manifest(),
                "location": locations.get(kind),
                "records": [record.manifest() for record in catalog.records[kind].values()],
            }
            for kind, studio in catalog.studios.items()
        },
        "vendors": [
            {"id": id, "name": FastenerVendor.names[id]}  # type: ignore[index]
            for id in FastenerVendor.values
            if id in vendors
        ],
        "sizes": [
            {
                "id": size.id,
                "name": size.name,
                "majorDiameter": _meters(size.major_diameter, size.unit),
                "pitch": _meters(size.pitch, size.unit),
                "tapDrillDiameter": _meters(size.tap_drill_diameter, size.unit),
                "closeClearanceDiameter": _meters(size.close_clearance_diameter, size.unit),
                "freeClearanceDiameter": _meters(size.free_clearance_diameter, size.unit),
            }
            for size in sizes
        ],
        "heads": [
            {
                "id": head,
                "name": ScrewHead.names[head],  # type: ignore[index]
                "sizes": {
                    size.id: {
                        "diameter": _meters(HEADS[size.id][head].diameter, HEADS[size.id][head].unit),
                        "height": _meters(HEADS[size.id][head].height, HEADS[size.id][head].unit),
                        "countersunk": HEADS[size.id][head].countersunk,
                    }
                    for size in sizes
                    if head in HEADS[size.id]
                },
            }
            for head in heads
        ],
        "screws": [
            {
                "vendor": vendor,
                "size": size,
                "head": head,
                "finish": finish,
                "lengths": [
                    {
                        "id": length_id(length),
                        "name": length_name(length),
                        "length": length_meters(length),
                        "fullThreadChoice": has_full_thread_choice(variants, finish),
                    }
                    for length, variants in lengths.items()
                    if Variant(finish) in variants
                ],
            }
            for (vendor, size, head), lengths in catalog.screws.items()
            for finish in screw_finishes
            if any(Variant(finish) in variants for variants in lengths.values())
        ],
        "nutTypes": [{"id": id, "name": NutType.names[id]} for id in NutType.values],  # type: ignore[index]
        "nuts": [
            {
                "vendor": vendor,
                "size": size,
                "type": type,
                "acrossFlats": _meters(nut_dimensions(vendor, size, type).across_flats, nut_dimensions(vendor, size, type).unit),
                "height": _meters(nut_dimensions(vendor, size, type).height, nut_dimensions(vendor, size, type).unit),
                **_stock_json(stock),
                # FRC's come in one finish
                **({"finishes": catalog.nut_finishes(vendor, size, type)} if metric else {}),
            }
            for (vendor, size, type, finish), stock in catalog.nuts.items()
            if finish == catalog.nut_finishes(vendor, size, type)[0]
        ],
        "washers": [
            {
                "vendor": vendor,
                "size": size,
                "type": type,
                "name": WasherType.names[type],  # type: ignore[index]
                "innerDiameter": _meters(washer.inner_diameter, washer.unit),
                "outerDiameter": _meters(washer.outer_diameter, washer.unit),
                "thickness": _meters(washer.thickness, washer.unit),
                "finishes": catalog.washer_finishes(vendor, size, type),
            }
            for (vendor, size, type, finish), washer in catalog.washers.items()
            if finish == catalog.washer_finishes(vendor, size, type)[0]
        ],
    }


EXPORTS = {"fasteners.gen.json": json.dumps(manifest(), indent=2) + "\n"}


# Refreshing vendor/frcdesign-fasteners.json and vendor/stores.json, which only `fetch` does: `fs gen` runs this file
# without it, and makes no requests

LIBRARY_API = "https://app.frcdesign.org/api"
# The Fasteners group's parts whose configurations are read: standard screws, nuts, and metal washers
_LIBRARY_PARTS = re.compile(r"^(?:[#\d/-]+ Bolt \((?:MCM|REV|WCP)\)|[#\d/-]+ Nut|Metal Washer \(\w+\))$")


def _get_json(url: str):
    import urllib.request

    request = urllib.request.Request(url, headers={"User-Agent": "robot-code fastenerTables.py fetch"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def fetch_library() -> None:
    """Writes the configurations of FRCDesignLib's Fasteners group's parts this reads (`_LIBRARY_PARTS`), in the
    group's order, to vendor/frcdesign-fasteners.json, from FRCDesign's public API."""
    version = _get_json(f"{LIBRARY_API}/library-version/library/frc-design-lib")["version"]
    library = _get_json(f"{LIBRARY_API}/library-data/library/frc-design-lib?v={version}")
    # Both by id
    [group] = [group for group in library["groups"].values() if group["name"] == "Fasteners"]
    insertables = library["insertables"]
    parts = []
    for insertable_id in group["insertableOrder"]:
        insertable = insertables[insertable_id]
        if not _LIBRARY_PARTS.match(insertable["name"]):
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
        "source": f"{LIBRARY_API}/configuration/insertable/{{id}}?v={version} (frc-design-lib, Fasteners group)",
        "parts": parts,
    }
    (FOLDER / "vendor" / "frcdesign-fasteners.json").write_text(json.dumps(snapshot, indent=1) + "\n", newline="")
    print(f"Wrote {len(parts)} parts to vendor/frcdesign-fasteners.json.")


def fetch_stores() -> None:
    """Writes each store's products which are catalog screws (`_store_screw`, `_metric_store_screw`) to
    vendor/stores.json, from Shopify's product list."""
    import urllib.request

    products = []
    for store in STORES:
        for page in range(1, 100):
            url = f"https://{store}/products.json?limit=250&page={page}"
            # Stores refuse Python's own user agent
            request = urllib.request.Request(url, headers={"User-Agent": "robot-code fastenerTables.py fetch"})
            with urllib.request.urlopen(request, timeout=60) as response:
                listed = json.load(response)["products"]
            if not listed:
                break
            for product in listed:
                if not product["variants"]:
                    continue
                entry = {
                    "store": store,
                    "title": product["title"],
                    "sku": product["variants"][0]["sku"],
                    "url": f"https://{store}/products/{product['handle']}",
                }
                if _store_screw(entry) is not None or _metric_store_screw(entry) is not None:
                    products.append(entry)
    products.sort(key=lambda product: (product["store"], product["sku"], product["title"]))
    snapshot = {"source": "Each store's /products.json, by fastenerTables.py fetch", "products": products}
    STORES_SNAPSHOT.write_text(json.dumps(snapshot, indent=1) + "\n")
    print(f"Wrote {len(products)} products to {STORES_SNAPSHOT}.")


if __name__ == "__main__":
    import sys

    if sys.argv[1:] != ["fetch"]:
        sys.exit("Usage: python fastenerTables.py fetch")
    fetch_library()
    fetch_stores()
