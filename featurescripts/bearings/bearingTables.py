"""FRCDesign's library bearings, generated for FRCDesignApp: flanged and radial ball bearings, by vendor, type, bore,
size, dimensions, and seal. Run `uv run fs gen` after editing.

This is the source of three things, which must agree:

- bearingTables.gen.fs: the enums and data the bearing feature builds from.
- The configuration of the Part Studio exposing it (`STUDIOS`), built by `fs parts bearings --test` and released by
  `fs parts bearings --release`: an input per feature parameter, laid out by `LookupTable` from what's made, so each
  input offers only what the vendor makes with the choices before it, and shows only where that's more than one.
- bearings.gen.json: the same catalog for FRCDesign, which inserts the studio with a configuration of these option
  ids. FRCDesign stores those configurations in documents, so never rename or remove a released option id; add new
  ones.

What each vendor makes, and its part numbers, come from FRCDesign's own Flanged Radial Bearing and Radial Bearing
(vendor/frcdesign-bearings.json, a snapshot of their configurations from FRCDesign's public API), by each record's
Dimensions option, which names its bore, outer diameter, width, and seal. Generic offers every bearing any vendor
makes, with the bearing's designation (FR8ZZ) as its part number where FRCDesign gives one.

Flanges come from REV's drawings and AndyMark's listed specifications (vendor/), or else the designation's standard
dimensions, marked where they're assumed. A bore is its shaft's nominal size: 13.75 mm and 10.25 mm across a rounded
hex's corners, as Robot shaft rounds its shafts, and 0.302 in across a ThunderSquare's.
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
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-bearings.json"
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text())

INCH = 0.0254


# Vendors, by how often FRCDesign's bearings are inserted from each

BearingVendor = Enum(
    "BearingVendor",
    ["GENERIC", "WCP", "ANDYMARK", "REV", "VEX", "THRIFTYBOT", "SWYFT", "SDS"],
    {
        "GENERIC": "Generic",
        "WCP": "WCP",
        "ANDYMARK": "AndyMark",
        "REV": "REV",
        "VEX": "VEX",
        "THRIFTYBOT": "ThriftyBot",
        "SWYFT": "Swyft",
        "SDS": "SDS",
    },
)
# FRCDesign's names for them, its Vendor options'
_VENDOR_NAMES = {
    "Generic": "GENERIC",
    "West Coast Products": "WCP",
    "AndyMark": "ANDYMARK",
    "REV Robotics": "REV",
    "VEX Robotics": "VEX",
    "The Thrifty Bot": "THRIFTYBOT",
    "SWYFT Robotics": "SWYFT",
    "Swerve Drive Specialties": "SDS",
}

BearingType = Enum("BearingType", ["FLANGED", "RADIAL"], {"FLANGED": "Flanged", "RADIAL": "Radial"})

BearingBore = Enum(
    "BearingBore",
    ["HEX", "ROUNDED_HEX", "ROUND", "THUNDERSQUARE"],
    {"HEX": "Hex", "ROUNDED_HEX": "Rounded hex", "ROUND": "Round", "THUNDERSQUARE": "ThunderSquare"},
)

BearingSeal = Enum("BearingSeal", ["SHIELDED", "SEALED", "OPEN"], {"SHIELDED": "Shielded", "SEALED": "Sealed", "OPEN": "Open"})


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
    *(Size(f"MM_{value}", f"{value} mm", value, "mm") for value in (5, 6, 8, 9, 10, 12, 15, 17, 25, 30, 35, 80)),
]
SIZES_BY_ID = {size.id: size for size in SIZES}
BearingSize = Enum("BearingSize", [size.id for size in SIZES], {size.id: size.name for size in SIZES})

# The diameter a bore's corners are rounded to: Robot shaft's rounded hex shafts', and VEX's ThunderSquare's 0.302 in
ROUNDED_DIAMETERS = {("ROUNDED_HEX", "IN_1_2"): mm(13.75), ("ROUNDED_HEX", "IN_3_8"): mm(10.25), ("THUNDERSQUARE", "IN_1_4"): inch(0.302)}


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
    '1/4" ThunderSquare': ("THUNDERSQUARE", "IN_1_4"),
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
    # REV-21-1916-DR.pdf (1/2 in rounded hex); AndyMark lists 1.225 to 1.25 in x 0.061 to 0.062 in for its own
    **{(size, "OD_1_125_X_0_313"): ("1.225", "0.063") for size in ("IN_1_2", "IN_3_8")},
    # REV-21-3197-DR.pdf
    **{(size, "OD_1_X_0_281"): ("1.06", "0.06") for size in ("IN_1_2", "IN_3_8")},
    # REV-21-3199-DR.pdf (#10 clearance); AndyMark lists 0.967 to 0.975 in x 0.06 to 0.065 in (am-4463, am-4489)
    **{(size, "OD_0_875_X_0_281"): ("0.97", "0.06") for size in ("IN_1_2", "IN_3_8", "IN_1_4", "NO_10", "MM_8")},
    ("IN_3_8", "OD_0_875_X_0_219"): ("0.97", "0.062"),  # am-0027 (FR6)
    ("IN_3_16", "OD_0_5_X_0_196"): ("0.565", "0.042"),  # am-4459 (FR3ZZ)
    ("IN_1_4", "OD_0_5_X_0_188"): ("0.565", "0.042"),  # FR188ZZ's standard
    ("IN_1_4", "OD_0_625_X_0_196"): ("0.69", "0.042"),  # am-4461 (FR4ZZ)
    # TODO: assumed; WCP and VEX publish no flange for WCP-0782 or 217-2789
    ("IN_3_4", "OD_1_375_X_0_438"): ("1.5", "0.063"),
    ("MM_5", "MM_11_X_4"): ("12.5", "0.8"),  # MF115ZZ's standard
    ("MM_5", "MM_13_X_4"): ("15", "1"),  # am-5469
    ("MM_5", "MM_16_X_5"): ("18", "1"),  # F625ZZ's standard
    ("MM_5", "MM_19_X_6"): ("22", "1.5"),  # F635ZZ's standard
    ("MM_6", "MM_12_X_4"): ("13.6", "0.8"),  # F126ZZ's standard
    ("MM_6", "MM_17_X_6"): ("19", "1"),  # am-4455
    # TODO: assumed, F688's; Swyft publishes none for BRG-8mm-14mm-01, nor ThriftyBot for TTB-0006
    ("MM_8", "MM_14_X_5"): ("15.6", "0.8"),
    ("MM_8", "MM_16_X_6_3"): ("18", "1.1"),
    ("MM_8", "MM_16_X_5"): ("18", "1.1"),  # W688AZZ's standard
    ("MM_8", "MM_19_X_6"): ("22", "1.5"),  # am-0931
    ("MM_8", "MM_22_X_7"): ("25", "1.5"),  # am-4457
    ("MM_9", "MM_17_X_5"): ("19", "1.1"),  # F689ZZ's standard
    ("MM_12", "MM_24_X_6"): ("26.5", "1.5"),  # F6901ZZ's standard
    ("MM_15", "MM_21_X_4"): ("22.5", "0.8"),  # am-5536
    # TODO: assumed; F6806ZZ's and F6807ZZ's flanges vary by maker
    ("MM_30", "MM_42_X_7"): ("45", "1.5"),
    ("MM_35", "MM_47_X_7"): ("50", "1.5"),
}


# What each vendor makes, from FRCDesign's records


@dataclasses.dataclass(frozen=True)
class Stock:
    part_number: str | None = None
    url: str | None = None


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


def _bearings() -> Bearings:
    stocked: Bearings = {}
    designations: dict[tuple, str] = {}
    for part in SNAPSHOT["parts"]:
        type = _TYPES[part["name"]]
        for record in part["records"]:
            options = _option_names(part, record)
            bearing = parse_dimensions(type, options["Dimensions"])
            vendor = _VENDOR_NAMES[options["Vendor"]]
            if vendor == "GENERIC":
                designation = _designation(record.get("partNumber"))
                if designation:
                    designations[bearing.key] = designation
                stocked[("GENERIC", *bearing.key)] = (bearing, Stock())
                continue
            stocked[(vendor, *bearing.key)] = (bearing, Stock(record.get("partNumber"), record.get("url")))
    # Generic: every bearing anyone makes, by its designation
    for (_, *key), (bearing, _) in list(stocked.items()):
        stocked[("GENERIC", *key)] = (bearing, Stock(designations.get(tuple(key))))
    return dict(sorted(stocked.items(), key=_order))


BEARINGS = _bearings()
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

_BORE_WORDS = {"HEX": "Hex", "ROUNDED_HEX": "Rounded Hex", "ROUND": "Round", "THUNDERSQUARE": "ThunderSquare"}
_TYPE_WORDS = {"FLANGED": "Flanged", "RADIAL": "Radial"}
_SIZE_WORDS = {"NO_10": "0.19 in"}


def bearing_name(bearing: Bearing) -> str:
    """e.g. `1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)`, with any seal but shields after its
    dimensions: `3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)`."""
    size = _SIZE_WORDS.get(bearing.size, SIZES_BY_ID[bearing.size].name)
    described = [bearing.dimensions.name] + ([] if bearing.seal == "SHIELDED" else [BearingSeal.names[bearing.seal]])  # type: ignore[index]
    return f"{size} {_BORE_WORDS[bearing.bore]} {_TYPE_WORDS[bearing.type]} Bearing ({', '.join(described)})"


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

    def fields(self) -> dict[str, str]:
        vendor = vendor_name(self.vendor)
        return {
            "name": string(self.name),
            **({"vendor": string(vendor)} if vendor else {}),
            **{key: string(value) for key, value in _stock(self.stock).items()},
        }

    def manifest(self) -> dict:
        vendor = vendor_name(self.vendor)
        return {"values": self.values, "name": self.name, **({"vendor": vendor} if vendor else {}), **_stock(self.stock), "material": "Steel"}


def _stock(stock: Stock) -> dict[str, str]:
    return {key: value for key, value in (("partNumber", stock.part_number), ("url", stock.url)) if value}


# The studio's configuration: a lookup table of what's made (see `LookupTable`)

COLUMNS = ("vendor", "type", "bore", "size", "dimensions", "seal")
TABLE = LookupTable(COLUMNS, list(BEARINGS))


def _list(id: str, name: str, enum: Enum, parameter: str, default: str) -> Input:
    assert enum.names is not None
    options = [Option(value, enum.names[value], value) for value in enum.values]
    return TABLE.input(Input(id, name, parameter, options, enum_name=enum.name, default=default))


# The defaults are what's inserted most from FRCDesign's library: Generic's 1/2 in rounded hex flanged bearing, 1.125 in
# OD, and shielded
DEFAULTS = ("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")
_ENUMS = {
    "vendor": ("Vendor", BearingVendor, "vendor"),
    "type": ("Type", BearingType, "bearingType"),
    "bore": ("Bore", BearingBore, "bore"),
    "size": ("Size", BearingSize, "size"),
    "dimensions": ("Dimensions", BearingDimensions, "dimensions"),
    "seal": ("Seal", BearingSeal, "seal"),
}
STUDIOS = {
    "bearing": ConfiguredStudio(
        "bearing",
        "libraryBearing",
        [_list(id, *_ENUMS[id], default) for id, default in zip(COLUMNS, DEFAULTS)],
        mate_connector="mateConnector",
    )
}
RECORDS = {
    "bearing": {
        key: Record(TABLE.shown(key), bearing_name(bearing), key[0], stock) for key, (bearing, stock) in BEARINGS.items()
    }
}

# Where the studio was last released: `fs parts bearings --release` records it beside this file
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
    value = _length(spec.value, spec.unit)
    fields = {"diameter": value} if bore == "ROUND" else {"acrossFlats": value, "sides": "4" if bore == "THUNDERSQUARE" else "6"}
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
    if bearing.type == "FLANGED":
        diameter, thickness = FLANGES[(bearing.size, dimensions.id)]
        fields["flangeDiameter"] = _length(diameter, unit)
        fields["flangeThickness"] = _length(thickness, unit)
    return _fields({**fields, **record.fields()})


_BORE_SIZES = {(bearing.bore, bearing.size) for bearing, _ in BEARINGS.values()}

CONTENTS = [
    BearingVendor,
    BearingType,
    BearingBore,
    BearingSize,
    BearingDimensions,
    BearingSeal,
    Code(
        "/** Each bore's shape: a round bore's `diameter`, or a hex's or square's `sides` and `acrossFlats`, with its"
        " corners rounded to `diameter` where they are. */\nexport const BEARING_BORES = "
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
            {key: (bearing, RECORDS["bearing"][key]) for key, (bearing, _) in BEARINGS.items()},
            [BearingVendor, BearingType, BearingBore, BearingSize, BearingDimensions, BearingSeal],
            _bearing_fields,
            0,
        )
        + ";"
    ),
]


# FRCDesign's manifest


def manifest() -> dict:
    """The part set's manifest, for FRCDesign: its studio and records, and its catalog."""
    return {
        "source": "featurescripts/bearings/bearingTables.py",
        "studios": {
            name: {
                **studio.manifest(),
                "location": LOCATIONS.get(name),
                "records": [record.manifest() for record in RECORDS[name].values()],
            }
            for name, studio in STUDIOS.items()
        },
        "vendors": [
            {"id": id, "name": BearingVendor.names[id]}  # type: ignore[index]
            for id in BearingVendor.values
            if any(key[0] == id for key in BEARINGS)
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
                    {"flangeDiameter": bearing.dimensions.meters(FLANGES[(size, dimensions)][0])}
                    if type == "FLANGED"
                    else {}
                ),
                "seal": seal,
                **_stock(stock),
            }
            for (vendor, type, bore, size, dimensions, seal), (bearing, stock) in BEARINGS.items()
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
