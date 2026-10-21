"""FRCDesign's library fasteners, generated for FRCDesignApp: screws, washers, and nuts for FRC. Run `uv run fs gen` after
editing.

This is the source of three things, which must agree:

- fastenerTables.gen.fs: the enums and data the screw, washer, and nut features build from.
- The configurations of the Part Studios exposing those features, one per kind (`STUDIOS`), which
  `fs configure-studio` writes: an input per feature parameter, each List's options shown only where they're made
  (Length shows only the lengths the chosen vendor makes in the chosen size and head).
- fasteners.gen.json: the same catalog for FRCDesign, with each studio's configuration as Onshape answers it, which
  picks a fastener for each hole and inserts its studio with a configuration of these option ids. FRCDesign stores those configurations in documents, so never rename or
  remove a released option id (as with enums stored in documents); add new ones.

What each vendor makes, and its part numbers, come from FRCDesign's own Fasteners group (vendor/frcdesign-fasteners.json,
a snapshot of its configurations): black oxide screws, McMaster-Carr's nuts, and black washers. The screws of vendors
whose stores list them (`STORES`: WCP and AndyMark) come from their stores instead (vendor/stores.json, which
`uv run python featurescripts/fasteners/fastenerTables.py fetch` refreshes), since a store is current where
the group may not be. Generic offers every length any vendor makes, without part numbers.

Each part has one mate connector, where it seats: FRCDesign fastens a screw or nut to the top edge of the washer it
sits on, or to the hole's own edge, so any washer goes under any screw or nut of its size.

Dimensions are nominal (ASME B18.3 screws, SAE washers, hex and nylon-insert lock nuts): close enough to check fit
and clearance, not to make the parts. Clearance holes are std's (`ANSI_V2ClearanceHoleTable`), and tap drills std's
75% engagement ones.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from fractions import Fraction

from fs_cli.configured_studios import ConfiguredStudio, Input, Option
from fs_cli.gen import Code, Constant
from fs_cli.tables import Enum, inch, string

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT = json.loads((FOLDER / "vendor" / "frcdesign-fasteners.json").read_text())
STORES_SNAPSHOT = FOLDER / "vendor" / "stores.json"
HOLE_SCAN = FOLDER / "fastenerHoleScan.fs"

INCH = 0.0254


# Vendors, sizes, and heads


FastenerVendor = Enum(
    "FastenerVendor",
    ["GENERIC", "MCMASTER", "REV", "WCP", "ANDYMARK"],
    {"GENERIC": "Generic", "MCMASTER": "McMaster-Carr", "REV": "REV", "WCP": "WCP", "ANDYMARK": "AndyMark"},
)
# FRCDesign's vendor codes
VENDOR_CODES = {"MCM": "MCMASTER", "REV": "REV", "WCP": "WCP"}


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


SIZES = [
    Size("NO_4_40", "#4-40", "#4", 40, 0.112, 0.089, 0.116, 0.1285),
    Size("NO_6_32", "#6-32", "#6", 32, 0.138, 0.1065, 0.144, 0.1495),
    Size("NO_8_32", "#8-32", "#8", 32, 0.164, 0.136, 0.1695, 0.177),
    Size("NO_10_32", "#10-32", "#10", 32, 0.19, 0.159, 0.196, 0.201),
    Size("QUARTER_20", "1/4-20", "1/4", 20, 0.25, 0.201, 0.257, 0.266),
]
SIZES_BY_NAME = {size.name: size for size in SIZES}
FastenerSize = Enum("FastenerSize", [size.id for size in SIZES], {size.id: size.name for size in SIZES})

ScrewHead = Enum(
    "ScrewHead", ["SOCKET", "BUTTON", "FLAT"], {"SOCKET": "Socket head", "BUTTON": "Button head", "FLAT": "Flat head"}
)
HEAD_CODES = {"SHCS": "SOCKET", "BHCS": "BUTTON", "FHCS": "FLAT"}


@dataclasses.dataclass
class Head:
    diameter: float
    height: float
    # Across the flats of its hex socket
    socket: float
    # A flat head's length includes its head, which sits in an 82 degree countersink
    countersunk: bool = False


HEADS = {
    "NO_4_40": {"SOCKET": Head(0.183, 0.112, 3 / 32), "BUTTON": Head(0.213, 0.059, 5 / 64), "FLAT": Head(0.255, 0.083, 1 / 16, True)},
    "NO_6_32": {"SOCKET": Head(0.226, 0.138, 7 / 64), "BUTTON": Head(0.262, 0.074, 5 / 64), "FLAT": Head(0.307, 0.097, 5 / 64, True)},
    "NO_8_32": {"SOCKET": Head(0.27, 0.164, 9 / 64), "BUTTON": Head(0.312, 0.087, 3 / 32), "FLAT": Head(0.359, 0.115, 3 / 32, True)},
    "NO_10_32": {"SOCKET": Head(0.312, 0.19, 5 / 32), "BUTTON": Head(0.375, 0.102, 1 / 8), "FLAT": Head(0.411, 0.132, 1 / 8, True)},
    "QUARTER_20": {"SOCKET": Head(0.375, 0.25, 3 / 16), "BUTTON": Head(0.437, 0.132, 5 / 32), "FLAT": Head(0.531, 0.153, 5 / 32, True)},
}
COUNTERSINK_ANGLE = 82


# Screws: what each vendor makes, from FRCDesign's records, like `#10-32 x 1-1/4" L BHCS (Steel, Black Oxide)`


@dataclasses.dataclass(frozen=True)
class Stock:
    part_number: str | None = None
    url: str | None = None


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
    # `#10-32 x .375" L BHCS (Steel, Black Oxide) (50-Pack)`, or a Kraken's spares, `#4-40 x 1/4" BHCS (IP8, ...)`; not
    # aluminum or ultra low profile ones, whose heads aren't standard
    "WCP": re.compile(r'^(#\d+-\d+) x ([\d./]+)"? (?:L )?(BHCS|SHCS|FHCS) \((?:Steel, Black Oxide|IP\d+, [^)]*)\)'),
    # `10-32 x 0.5 in. Button Head Cap Screw`, `1/4-20 x 2 in. Flat Head Cap Screw with Nylon Patch`
    "ANDYMARK": re.compile(
        r"^(\d+-\d+|1/4-20) x ([\d./]+) in\.? (?:Hex Drive )?"
        r"(Button Head Cap Screw|Socket Head Cap Screw|Flat Head Cap Screw|Flat Head Socket Cap Screw)"
        r"(?: Black Oxide)?( with Nylon(?: Thread Lock)? Patch)?$"
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


def _store_screws() -> dict[tuple[str, str, str], dict[Fraction, Stock]]:
    """The screws each store sells in a size and head the catalog has: one per length, one without a thread lock patch
    where it sells both."""
    stocked: dict[tuple[str, str, str], dict[Fraction, tuple[bool, Stock]]] = {}
    for product in json.loads(STORES_SNAPSHOT.read_text())["products"]:
        vendor = STORES[product["store"]]
        match = _STORE_SCREWS[vendor].match(product["title"])
        if match is None:
            continue
        size, length, head, *patched = match.groups()
        size = size if size.startswith("#") or size == "1/4-20" else "#" + size
        if size not in SIZES_BY_NAME:
            continue
        key = (vendor, SIZES_BY_NAME[size].id, _STORE_HEADS[head])
        has_patch = bool(patched and patched[0])
        lengths = stocked.setdefault(key, {})
        inches = parse_inches(length)
        if inches not in lengths or lengths[inches][0] and not has_patch:
            lengths[inches] = (has_patch, Stock(product["sku"], product["url"]))
    return {key: {length: stock for length, (_, stock) in lengths.items()} for key, lengths in stocked.items()}


def _stocked_screws() -> dict[tuple[str, str, str], dict[Fraction, Stock]]:
    stocked: dict[tuple[str, str, str], dict[Fraction, Stock]] = {}
    for part in SNAPSHOT["parts"]:
        if " Bolt (" not in part["name"]:
            continue
        vendor = _vendor(part)
        if vendor in STORES.values():
            continue
        for record in part["records"]:
            match = _SCREW.match(record["name"])
            assert match, record["name"]
            size, length, head, finish = match.groups()
            if "Black Oxide" not in finish:
                continue
            key = (vendor, SIZES_BY_NAME[size].id, HEAD_CODES[head])
            stocked.setdefault(key, {})[parse_inches(length)] = Stock(record["partNumber"], record.get("url"))
    stocked.update(_store_screws())
    # Generic: every length anyone makes
    for (_, size, head), lengths in list(stocked.items()):
        generic = stocked.setdefault(("GENERIC", size, head), {})
        for length in lengths:
            generic[length] = Stock()
    return {key: dict(sorted(lengths.items())) for key, lengths in sorted(stocked.items(), key=_screw_order)}


def _screw_order(item) -> tuple:
    vendor, size, head = item[0]
    return (FastenerVendor.values.index(vendor), FastenerSize.values.index(size), ScrewHead.values.index(head))


SCREWS = _stocked_screws()
LENGTHS = sorted({length for lengths in SCREWS.values() for length in lengths})


def length_id(length: Fraction) -> str:
    """e.g. IN_0_375 for 3/8 in., IN_1_25 for 1-1/4 in."""
    return "IN_" + f"{float(length):g}".replace(".", "_")


def length_name(length: Fraction) -> str:
    """e.g. 3/8 in., or 1-1/4 in."""
    whole, part = divmod(length, 1)
    if part == 0:
        return f"{whole} in."
    if whole == 0:
        return f"{part} in."
    return f"{whole}-{part} in."


ScrewLength = Enum(
    "ScrewLength", [length_id(length) for length in LENGTHS], {length_id(length): length_name(length) for length in LENGTHS}
)


# Nuts: McMaster-Carr's, like `#10-32 Thin Nylon-Insert Locknut`; Generic makes the same ones


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
NUT_VENDORS = ["GENERIC", "MCMASTER"]


@dataclasses.dataclass
class Nut:
    # A square nut's width
    across_flats: float
    height: float


NUT_DIMENSIONS = {
    "NO_4_40": {"NYLOCK": Nut(0.25, 0.141), "THIN_NYLOCK": Nut(0.25, 0.109), "HEX": Nut(0.25, 0.094), "THIN_HEX": Nut(0.25, 0.063)},
    "NO_6_32": {"NYLOCK": Nut(0.3125, 0.172), "THIN_NYLOCK": Nut(0.3125, 0.125), "HEX": Nut(0.3125, 0.109), "THIN_HEX": Nut(0.3125, 0.078)},
    "NO_8_32": {"NYLOCK": Nut(0.34375, 0.203), "THIN_NYLOCK": Nut(0.34375, 0.141), "HEX": Nut(0.34375, 0.125), "SQUARE": Nut(0.34375, 0.125)},
    "NO_10_32": {"NYLOCK": Nut(0.375, 0.236), "THIN_NYLOCK": Nut(0.375, 0.156), "HEX": Nut(0.375, 0.125), "SQUARE": Nut(0.375, 0.125)},
    "QUARTER_20": {"NYLOCK": Nut(0.4375, 0.312), "THIN_NYLOCK": Nut(0.4375, 0.203), "HEX": Nut(0.4375, 0.219), "SQUARE": Nut(0.4375, 0.219)},
}

_NUT = re.compile(r"^(#\d+-\d+|1/4-20)\s+(.+)$")


def _stocked_nuts() -> dict[tuple[str, str, str], Stock]:
    stocked: dict[tuple[str, str, str], Stock] = {}
    for part in SNAPSHOT["parts"]:
        if not part["name"].endswith(" Nut"):
            continue
        for record in part["records"]:
            match = _NUT.match(record["name"])
            assert match, record["name"]
            size, kind = match.groups()
            key = (SIZES_BY_NAME[size].id, NUT_CODES[kind])
            stocked[("MCMASTER", *key)] = Stock(record["partNumber"], record.get("url"))
            stocked[("GENERIC", *key)] = Stock()
    assert {(size, type) for _, size, type in stocked} == {
        (size, type) for size, types in NUT_DIMENSIONS.items() for type in types
    }, "Every nut needs its dimensions"
    return dict(
        sorted(
            stocked.items(),
            key=lambda item: (
                NUT_VENDORS.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                NutType.values.index(item[0][2]),
            ),
        )
    )


NUTS = _stocked_nuts()


# Washers: black ones, like `#10 Washer (7/16" OD, 0.03" WD, Stainless Steel, Black Oxide)`; Generic's are SAE flat


@dataclasses.dataclass(frozen=True)
class Washer:
    inner_diameter: float
    outer_diameter: Fraction
    thickness: str
    stock: Stock = Stock()


# SAE flat washers' inner diameters, which FRCDesign's records leave out
WASHER_INNER_DIAMETERS = {"NO_4_40": 0.125, "NO_6_32": 0.156, "NO_8_32": 0.188, "NO_10_32": 0.219, "QUARTER_20": 0.281}
GENERIC_WASHERS = {
    "NO_4_40": ("5/16", "0.032"),
    "NO_6_32": ("3/8", "0.049"),
    "NO_8_32": ("7/16", "0.049"),
    "NO_10_32": ("1/2", "0.049"),
    "QUARTER_20": ("5/8", "0.065"),
}
WASHER_VENDORS = ["GENERIC", "MCMASTER", "WCP"]
_WASHER = re.compile(r'^(#\d+|[\d/\.]+" ID) Washer \(([\d/\.]+)" OD, ([\d\.]+)" WD, ([^)]*)\)')
_WASHER_SIZES = {"#4": "NO_4_40", "#6": "NO_6_32", "#8": "NO_8_32", "#10": "NO_10_32", '1/4" ID': "QUARTER_20"}


def washer_type(outer_diameter: Fraction, thickness: str) -> str:
    """e.g. OD_7_16_X_0_03, for 7/16 in. OD and 0.03 in. thick."""
    return f"OD_{outer_diameter}_X_{thickness}".replace("/", "_").replace(".", "_")


def washer_type_name(outer_diameter: Fraction, thickness: str) -> str:
    """e.g. 7/16 in. OD x 0.03 in."""
    return f"{length_name(outer_diameter)} OD x {thickness} in."


def _washers() -> dict[tuple[str, str, str], Washer]:
    washers: dict[tuple[str, str, str], Washer] = {}
    for size, (outer, thickness) in GENERIC_WASHERS.items():
        od = Fraction(outer)
        washers[("GENERIC", size, washer_type(od, thickness))] = Washer(WASHER_INNER_DIAMETERS[size], od, thickness)
    for part in SNAPSHOT["parts"]:
        if "Washer" not in part["name"]:
            continue
        vendor = _vendor(part)
        for record in part["records"]:
            match = _WASHER.match(record["name"])
            assert match, record["name"]
            size_name, outer, thickness, finish = match.groups()
            if size_name not in _WASHER_SIZES or "Black" not in finish:
                continue
            size = _WASHER_SIZES[size_name]
            od = parse_inches(outer)
            washers[(vendor, size, washer_type(od, thickness))] = Washer(
                WASHER_INNER_DIAMETERS[size], od, thickness, Stock(record["partNumber"], record.get("url"))
            )
    return dict(
        sorted(
            washers.items(),
            key=lambda item: (
                WASHER_VENDORS.index(item[0][0]),
                FastenerSize.values.index(item[0][1]),
                item[1].outer_diameter,
                float(item[1].thickness),
            ),
        )
    )


WASHERS = _washers()
_WASHER_TYPES = sorted(
    {(washer.outer_diameter, float(washer.thickness), washer.thickness) for washer in WASHERS.values()}
)
WasherType = Enum(
    "WasherType",
    [washer_type(od, thickness) for od, _, thickness in _WASHER_TYPES],
    {washer_type(od, thickness): washer_type_name(od, thickness) for od, _, thickness in _WASHER_TYPES},
)


def _thickest_washer(vendor: str, size: str) -> str:
    """The type of a vendor's thickest washer in a size: the washer studio's default."""
    made = [(key, washer) for key, washer in WASHERS.items() if key[0] == vendor and key[1] == size]
    (_, _, type), _ = max(made, key=lambda item: (float(item[1].thickness), -item[1].outer_diameter))
    return type


# What each part is called and sold as: the features set these, and FRCDesignApp indexes the same records


SIZES_BY_ID = {size.id: size for size in SIZES}
MATERIAL = "Steel"
HEAD_NAMES = {"SOCKET": "Socket Head", "BUTTON": "Button Head", "FLAT": "Flat Head"}
NUT_NAMES = {"NYLOCK": "Nylock", "THIN_NYLOCK": "Thin Nylock", "HEX": "Hex", "THIN_HEX": "Thin Hex", "SQUARE": "Square"}


def screw_name(size: str, head: str, length: Fraction) -> str:
    """e.g. `#10-32 x 1/2 in. Button Head Screw`."""
    return f"{SIZES_BY_ID[size].name} x {length_name(length)} {HEAD_NAMES[head]} Screw"


def nut_name(size: str, type: str) -> str:
    """e.g. `#10-32 Thin Nylock Nut`."""
    return f"{SIZES_BY_ID[size].name} {NUT_NAMES[type]} Nut"


def washer_name(size: str, type: str) -> str:
    """e.g. `#10 Washer (1/2 in. OD x 0.049 in.)`, or `1/4 in. Washer (...)`."""
    hole = SIZES_BY_ID[size].hole_table_size
    return f"{hole}{' in.' if hole == '1/4' else ''} Washer ({WasherType.names[type]})"  # type: ignore[index]


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

    def fields(self) -> dict[str, str]:
        """What the FeatureScript table holds for it, which the feature sets as the part's properties."""
        vendor = vendor_name(self.vendor)
        return {"name": string(self.name), **({"vendor": string(vendor)} if vendor else {}), **_stock_fields(self.stock)}

    def manifest(self) -> dict:
        vendor = vendor_name(self.vendor)
        return {
            "values": self.values,
            "name": self.name,
            **({"vendor": vendor} if vendor else {}),
            **_stock_json(self.stock),
            "material": MATERIAL,
        }


def _screw_records() -> dict[tuple[str, str, str, str], Record]:
    return {
        (vendor, size, head, length_id(length)): Record(
            {"vendor": vendor, "size": size, "head": head, "length": length_id(length)},
            screw_name(size, head, length),
            vendor,
            stock,
        )
        for (vendor, size, head), lengths in SCREWS.items()
        for length, stock in lengths.items()
    }


def _nut_records() -> dict[tuple[str, str, str], Record]:
    return {
        (vendor, size, type): Record({"vendor": vendor, "size": size, "type": type}, nut_name(size, type), vendor, stock)
        for (vendor, size, type), stock in NUTS.items()
    }


def _washer_records() -> dict[tuple[str, str, str], Record]:
    return {
        (vendor, size, type): Record(
            {"vendor": vendor, "size": size, "type": type}, washer_name(size, type), vendor, washer.stock
        )
        for (vendor, size, type), washer in WASHERS.items()
    }


SCREW_RECORDS = _screw_records()
NUT_RECORDS = _nut_records()
WASHER_RECORDS = _washer_records()
RECORDS = {"screw": SCREW_RECORDS, "nut": NUT_RECORDS, "washer": WASHER_RECORDS}


# The studios' configurations


def _shown(keys, names: tuple[str, ...], value_index: int):
    """For each value of position `value_index` in keys, the combinations of the earlier inputs `names` (the key's
    other positions before it) it's made in; None when it's made in every one."""
    combinations: dict[str, set[tuple]] = {}
    for key in keys:
        combinations.setdefault(key[value_index], set()).add(tuple(zip(names, key[:value_index])))
    every = set().union(*combinations.values())

    def shown_when(value: str):
        mine = combinations[value]
        return None if mine == every else [dict(combination) for combination in sorted(mine)]

    return shown_when


def _list(id, name, enum: Enum, parameter, values, shown_when=lambda value: None, default=None) -> Input:
    assert enum.names is not None
    options = [Option(value, enum.names[value], value, shown_when(value)) for value in values]
    return Input(id, name, parameter, options, enum_name=enum.name, default=default)


def _used(enum: Enum, keys, index: int) -> list[str]:
    present = {key[index] for key in keys}
    return [value for value in enum.values if value in present]


def _screw_inputs() -> list[Input]:
    keys = list(SCREWS)
    lengths = [key + (length_id(length),) for key, stocked in SCREWS.items() for length in stocked]
    return [
        _list("vendor", "Vendor", FastenerVendor, "vendor", _used(FastenerVendor, keys, 0)),
        _list("size", "Size", FastenerSize, "size", _used(FastenerSize, keys, 1), _shown(keys, ("vendor",), 1), "NO_10_32"),
        _list("head", "Head", ScrewHead, "head", _used(ScrewHead, keys, 2), _shown(keys, ("vendor", "size"), 2), "BUTTON"),
        _list(
            "length",
            "Length",
            ScrewLength,
            "length",
            ScrewLength.values,
            _shown(lengths, ("vendor", "size", "head"), 3),
            length_id(Fraction(1, 2)),
        ),
    ]


def _nut_inputs() -> list[Input]:
    keys = list(NUTS)
    return [
        _list("vendor", "Vendor", FastenerVendor, "vendor", NUT_VENDORS),
        _list("size", "Size", FastenerSize, "size", _used(FastenerSize, keys, 1), _shown(keys, ("vendor",), 1), "NO_10_32"),
        _list("type", "Type", NutType, "nutType", _used(NutType, keys, 2), _shown(keys, ("vendor", "size"), 2)),
    ]


def _washer_inputs() -> list[Input]:
    keys = list(WASHERS)
    return [
        _list("vendor", "Vendor", FastenerVendor, "vendor", WASHER_VENDORS),
        _list("size", "Size", FastenerSize, "size", _used(FastenerSize, keys, 1), _shown(keys, ("vendor",), 1), "NO_10_32"),
        _list(
            "type",
            "Type",
            WasherType,
            "washerType",
            WasherType.values,
            _shown(keys, ("vendor", "size"), 2),
            _thickest_washer("GENERIC", "NO_10_32"),
        ),
    ]


STUDIOS = {
    "screw": ConfiguredStudio("screw", "fastenerScrew", _screw_inputs()),
    "nut": ConfiguredStudio("nut", "fastenerNut", _nut_inputs()),
    "washer": ConfiguredStudio("washer", "fastenerWasher", _washer_inputs()),
}

# Where each studio is: its document and tab, and the version FRCDesign inserts (`documentId`, `versionId`,
# `elementId`). Fill one in once its Part Studio is made and configured (`fs configure-studio`).
LOCATIONS: dict[str, dict[str, str] | None] = {"screw": None, "washer": None, "nut": None}


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


def _size_fields(size: Size) -> str:
    return _fields(
        {
            "name": string(size.name),
            "size": string(size.hole_table_size),
            "pitch": string(f"{size.threads_per_inch} tpi"),
            "majorDiameter": _in(size.major_diameter),
            "tapDrillDiameter": _in(size.tap_drill_diameter),
            "closeClearanceDiameter": _in(size.close_clearance_diameter),
            "freeClearanceDiameter": _in(size.free_clearance_diameter),
        }
    )


def _head_fields(head: Head) -> str:
    fields = {"headDiameter": _in(head.diameter), "headHeight": _in(head.height), "socketSize": _in(head.socket)}
    if head.countersunk:
        fields["countersinkAngle"] = f"{COUNTERSINK_ANGLE} * degree"
    return _fields(fields)


def _nut_fields(record: Record) -> str:
    nut = NUT_DIMENSIONS[record.values["size"]][record.values["type"]]
    return _fields({"acrossFlats": _in(nut.across_flats), "height": _in(nut.height), **record.fields()})


def _washer_fields(item: tuple[Washer, Record]) -> str:
    washer, record = item
    return _fields(
        {
            "innerDiameter": _in(washer.inner_diameter),
            "outerDiameter": _in(washer.outer_diameter),
            "thickness": _in(float(washer.thickness)),
            **record.fields(),
        }
    )


def _documented(doc: str, name: str, expression: str) -> Code:
    return Code(f"/** {doc} */\nexport const {name} = {expression};")


CONTENTS = [
    FastenerVendor,
    FastenerSize,
    ScrewHead,
    ScrewLength,
    NutType,
    WasherType,
    Constant("FASTENER_SIZES", _map([(FastenerSize[size.id], _size_fields(size)) for size in SIZES], 8)),
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
                (ScrewLength[length_id(length)], _fields({"length": _in(length), "name": string(length_name(length))}))
                for length in LENGTHS
            ],
            8,
        ),
    ),
    _documented(
        "What each vendor makes, by vendor, size, head, and length: each screw's part number and url, where it has them.",
        "SCREWS",
        _nested(
            SCREW_RECORDS,
            [FastenerVendor, FastenerSize, ScrewHead, ScrewLength],
            lambda record: _fields(record.fields()),
            4,
        ),
    ),
    Constant(
        "NUTS",
        _nested(
            NUT_RECORDS,
            [FastenerVendor, FastenerSize, NutType],
            _nut_fields,
        ),
    ),
    Constant(
        "WASHERS",
        _nested({key: (WASHERS[key], record) for key, record in WASHER_RECORDS.items()}, [FastenerVendor, FastenerSize, WasherType], _washer_fields),
    ),
]


# FRCDesign's manifest


def hole_scan_lambda(code: str) -> str:
    """The body of `fastenerHoleScan` as the lambda Onshape's FeatureScript evaluation takes: only std's
    common.fs is in scope there, so the function uses nothing else, and keeps its helpers inside it."""
    match = re.search(
        r"export function fastenerHoleScan\(context is Context, queries is map\) returns map\n\{\n(.*?)\n\}\n",
        code,
        re.DOTALL,
    )
    if match is None:
        raise ValueError(f"{HOLE_SCAN.name} has no fastenerHoleScan(context is Context, queries is map)")
    return "function(context is Context, queries is map)\n{\n" + match.group(1) + "\n}"


def _meters(inches: float | Fraction | str) -> float:
    return round(float(inches) * INCH, 9)


def _stock_json(stock: Stock) -> dict:
    return {key: value for key, value in (("partNumber", stock.part_number), ("url", stock.url)) if value}


def manifest() -> dict:
    return {
        "source": "featurescripts/fasteners/fastenerTables.py",
        "studios": {
            kind: {
                **studio.manifest(),
                "location": LOCATIONS[kind],
                "records": [record.manifest() for record in RECORDS[kind].values()],
            }
            for kind, studio in STUDIOS.items()
        },
        "vendors": [{"id": id, "name": FastenerVendor.names[id]} for id in FastenerVendor.values],  # type: ignore[index]
        "sizes": [
            {
                "id": size.id,
                "name": size.name,
                "majorDiameter": _meters(size.major_diameter),
                "pitch": _meters(1 / size.threads_per_inch),
                "tapDrillDiameter": _meters(size.tap_drill_diameter),
                "closeClearanceDiameter": _meters(size.close_clearance_diameter),
                "freeClearanceDiameter": _meters(size.free_clearance_diameter),
            }
            for size in SIZES
        ],
        "heads": [
            {
                "id": head,
                "name": ScrewHead.names[head],  # type: ignore[index]
                "sizes": {
                    size: {
                        "diameter": _meters(heads[head].diameter),
                        "height": _meters(heads[head].height),
                        "countersunk": heads[head].countersunk,
                    }
                    for size, heads in HEADS.items()
                },
            }
            for head in ScrewHead.values
        ],
        "screws": [
            {
                "vendor": vendor,
                "size": size,
                "head": head,
                "lengths": [
                    {
                        "id": length_id(length),
                        "name": length_name(length),
                        "length": _meters(length),
                        **_stock_json(stock),
                    }
                    for length, stock in lengths.items()
                ],
            }
            for (vendor, size, head), lengths in SCREWS.items()
        ],
        "nutTypes": [{"id": id, "name": NutType.names[id]} for id in NutType.values],  # type: ignore[index]
        "nuts": [
            {
                "vendor": vendor,
                "size": size,
                "type": type,
                "acrossFlats": _meters(NUT_DIMENSIONS[size][type].across_flats),
                "height": _meters(NUT_DIMENSIONS[size][type].height),
                **_stock_json(stock),
            }
            for (vendor, size, type), stock in NUTS.items()
        ],
        "washers": [
            {
                "vendor": vendor,
                "size": size,
                "type": type,
                "name": WasherType.names[type],  # type: ignore[index]
                "innerDiameter": _meters(washer.inner_diameter),
                "outerDiameter": _meters(washer.outer_diameter),
                "thickness": _meters(washer.thickness),
                **_stock_json(washer.stock),
            }
            for (vendor, size, type), washer in WASHERS.items()
        ],
        "holeScan": hole_scan_lambda(HOLE_SCAN.read_text()),
    }


EXPORTS = {"fasteners.gen.json": json.dumps(manifest(), indent=2) + "\n"}


# Refreshing vendor/stores.json, which only `fetch` does: `fs gen` runs this file without it, and makes no requests

# Words in a screw's title, which keep the snapshot to what might be one
_SCREW_WORDS = re.compile(r"BHCS|SHCS|FHCS|Cap Screw")


def fetch_stores() -> None:
    """Writes each store's products whose titles might be screws to vendor/stores.json, from Shopify's product list."""
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
                if _SCREW_WORDS.search(product["title"]) and product["variants"]:
                    products.append(
                        {
                            "store": store,
                            "title": product["title"],
                            "sku": product["variants"][0]["sku"],
                            "url": f"https://{store}/products/{product['handle']}",
                        }
                    )
    products.sort(key=lambda product: (product["store"], product["sku"], product["title"]))
    snapshot = {"source": "Each store's /products.json, by fastenerTables.py fetch", "products": products}
    STORES_SNAPSHOT.write_text(json.dumps(snapshot, indent=1) + "\n")
    print(f"Wrote {len(products)} products to {STORES_SNAPSHOT}.")


if __name__ == "__main__":
    import sys

    if sys.argv[1:] != ["fetch"]:
        sys.exit("Usage: python fastenerTables.py fetch")
    fetch_stores()
