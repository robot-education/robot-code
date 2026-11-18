"""FRCDesign's library bushings, generated for FRCDesignApp: flanged bushings, from FRCDesign's hand-made Flanged Bushing
(its records in `vendor/frcdesign-bushings.json`, which `uv run python featurescripts/bushings/bushingTables.py fetch`
refreshes), with each flange and step as its vendor's STEP file or store gives it (`FLANGES`, sources in `vendor/`).

A bushing whose flange no source gives isn't offered: WCP's and VEX's, which publish no models, and Generic's of their
sizes. Generic offers every bushing a vendor does. The studio asks Vendor, Material, Bore, Size, and Dimensions (outer
diameter by width), each shown only where more than one is made with the choices before it, as the bearings' does
(`featurescripts/bearings/`).
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import sys

from fs_cli import library
from fs_cli.configured_studios import ConfiguredStudio, Input, LookupTable, Option, read_locations
from fs_cli.gen import Code, Import
from fs_cli.tables import Enum, inch, mm, string

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT = FOLDER / "vendor" / "frcdesign-bushings.json"
LIBRARY_PART = "Flanged Bushing"

if __name__ == "__main__" and sys.argv[1:] == ["fetch"]:
    library.fetch(SNAPSHOT, [LIBRARY_PART])
    sys.exit()

INCH = 0.0254

BushingVendor = Enum("BushingVendor", ["GENERIC", "REV", "THRIFTYBOT"], {"GENERIC": "Generic", "REV": "REV", "THRIFTYBOT": "ThriftyBot"})
_VENDORS = {"Generic": "GENERIC", "REV Robotics": "REV", "The Thrifty Bot": "THRIFTYBOT"}
_VENDOR_NAMES = {"REV": "REV Robotics", "THRIFTYBOT": "The Thrifty Bot"}

BushingMaterial = Enum("BushingMaterial", ["BRONZE", "NYLON"], {"BRONZE": "Bronze", "NYLON": "Nylon"})
_MATERIALS = {"BRONZE": ("BRONZE", "BRASS_YELLOW"), "NYLON": ("NYLON", "BLACK")}

BushingBore = Enum("BushingBore", ["ROUNDED_HEX", "ROUND"], {"ROUNDED_HEX": "Rounded hex", "ROUND": "Round"})

# Nominal sizes, in inches: across a hex's flats, or a round bore's diameter
SIZES = {"IN_1_2": ("1/2 in", 0.5), "IN_3_8": ("3/8 in", 0.375), "IN_1_4": ("1/4 in", 0.25), "IN_3_4": ("3/4 in", 0.75),
         "IN_7_8": ("7/8 in", 0.875), "IN_1": ("1 in", 1.0)}
# Rounded hex corners, as the bearings and Robot shaft round them
ROUNDED_DIAMETERS = {"IN_1_2": mm(13.75), "IN_3_8": mm(10.25)}

# FRCDesign's Dimensions options name a bushing's bore, size, outer diameter, width, and any step or material
_DIMENSIONS = re.compile(
    r'^(?:(?P<hex>[\d/]+)" Rounded Hex ID|(?P<round>[\d.]+)" ID) x (?P<od>[\d.]+)" OD x (?P<width>[\d.]+)" WD'
    r'(?: \((?:(?P<step>[\d.]+)" OD Step|(?P<flange>[\d.]+)" OD Flange|(?P<material>Bronze|Nylon))\))?$'
)
_ROUND_SIZES = {0.25: "IN_1_4", 0.375: "IN_3_8", 0.5: "IN_1_2", 0.75: "IN_3_4", 0.875: "IN_7_8", 1.0: "IN_1"}
_HEX_SIZES = {"1/2": "IN_1_2", "3/8": "IN_3_8"}


@dataclasses.dataclass(frozen=True)
class Flange:
    """A bushing's flange (and a step under it, where it has one), in inches."""

    diameter: float
    thickness: float
    step_diameter: float | None = None
    step_height: float | None = None


# By (material, bore, size, outer diameter, width), from each source in vendor/; Generic's of the same are the same
FLANGES = {
    # TTB-0266.stp
    ("BRONZE", "ROUNDED_HEX", "IN_3_8", 0.625, 0.375): Flange(0.75, 0.0825),
    # TTB-0267.stp
    ("BRONZE", "ROUNDED_HEX", "IN_1_2", 0.75, 0.333): Flange(0.875, 0.083),
    # TTB-0004, the Thrifty Bushing: "0.375 in overall width / 0.125 in thick flange, 0.75 in body outer diameter,
    # 0.875 in flange outer diameter" (its store's description)
    ("NYLON", "ROUNDED_HEX", "IN_1_2", 0.75, 0.375): Flange(0.875, 0.125),
    # TTB-0268.stp, TTB-0269.stp, TTB-0270.stp
    ("BRONZE", "ROUND", "IN_3_4", 1.125, 0.333): Flange(1.225, 0.083),
    ("BRONZE", "ROUND", "IN_7_8", 1.125, 0.333): Flange(1.225, 0.083),
    ("BRONZE", "ROUND", "IN_1", 1.125, 0.333): Flange(1.225, 0.083),
    # REV-21-2396.STEP: a 1.125 in step 0.125 in tall under its flange
    ("BRONZE", "ROUND", "IN_3_4", 1.0, 0.563): Flange(1.225, 0.0625, 1.125, 0.125),
}


@dataclasses.dataclass(frozen=True)
class Bushing:
    material: str
    bore: str
    size: str
    outer_diameter: float
    width: float
    flange: Flange

    @property
    def dimensions(self) -> str:
        return dimensions_id(self.outer_diameter, self.width, self.flange)


def _decimal(value: float) -> str:
    return f"{value:g}"


def dimensions_id(outer: float, width: float, flange: Flange) -> str:
    step = "_STEP" if flange.step_diameter else ""
    return f"OD_{_decimal(outer).replace('.', '_')}_X_{_decimal(width).replace('.', '_')}{step}"


def dimensions_name(outer: float, width: float, flange: Flange) -> str:
    step = f" ({_decimal(flange.step_diameter)} in step)" if flange.step_diameter else ""
    return f"{_decimal(outer)} in OD x {_decimal(width)} in{step}"


def parse(option: str, record_name: str) -> tuple[str, str, str, float, float] | None:
    """A Dimensions option's material, bore, size, outer diameter, and width; None for one with a hex outside."""
    match = _DIMENSIONS.match(option)
    if match is None:
        if "Hex OD" in option:
            return None
        raise ValueError(f"Can't read {option}.")
    material = (match["material"] or ("Nylon" if "Nylon" in record_name else "Bronze")).upper()
    if match["hex"]:
        bore, size = "ROUNDED_HEX", _HEX_SIZES[match["hex"]]
    else:
        bore, size = "ROUND", _ROUND_SIZES[float(match["round"])]
    return material, bore, size, float(match["od"]), float(match["width"])


Key = tuple[str, str, str, str, str]


@dataclasses.dataclass(frozen=True)
class Stock:
    part_number: str | None


def _catalog() -> tuple[dict[Key, tuple[Bushing, Stock]], list[str]]:
    """Every bushing FRCDesign's records name whose flange is known, by (vendor, material, bore, size, dimensions); and
    the records left out for want of one."""
    part = library.load(SNAPSHOT)[LIBRARY_PART]
    found: dict[Key, tuple[Bushing, Stock]] = {}
    unmeasured = []
    for record in library.records(part):
        vendor = _VENDORS.get(record["options"]["Vendor"])
        parsed = parse(record["options"]["Dimensions"], record["name"])
        if parsed is None:
            continue
        flange = FLANGES.get(parsed)
        if vendor is None or flange is None:
            unmeasured.append(f"{record['options']['Vendor']}: {record['options']['Dimensions']}")
            continue
        bushing = Bushing(*parsed, flange)
        found[(vendor, bushing.material, bushing.bore, bushing.size, bushing.dimensions)] = (bushing, Stock(record["partNumber"]))
    return found, unmeasured


BUSHINGS, UNMEASURED = _catalog()
# The sizes offered, 1/2 in and 3/8 in first, as the bearings' are
BushingSize = Enum(
    "BushingSize",
    [id for id in SIZES if any(key[3] == id for key in BUSHINGS)],
    {id: name for id, (name, _) in SIZES.items()},
)
_DIMENSION_VALUES = sorted({(b.outer_diameter, b.width, b.flange) for b, _ in BUSHINGS.values()}, key=lambda d: (d[0], d[1]))
BushingDimensions = Enum(
    "BushingDimensions",
    [dimensions_id(*d) for d in _DIMENSION_VALUES],
    {dimensions_id(*d): dimensions_name(*d) for d in _DIMENSION_VALUES},
)


def bushing_name(bushing: Bushing) -> str:
    bore = "Rounded Hex" if bushing.bore == "ROUNDED_HEX" else "Round"
    material = "Nylon" if bushing.material == "NYLON" else "Bronze"
    return f"{SIZES[bushing.size][0]} {bore} Flanged {material} Bushing ({dimensions_name(bushing.outer_diameter, bushing.width, bushing.flange)})"


@dataclasses.dataclass(frozen=True)
class Record:
    """One configuration of the studio: its inputs' options, and the part it makes."""

    values: dict[str, str]
    name: str
    vendor: str
    stock: Stock
    bushing: Bushing

    def manifest(self) -> dict:
        vendor = _VENDOR_NAMES.get(self.vendor)
        return {
            "values": self.values,
            "name": self.name,
            **({"vendor": vendor} if vendor else {}),
            **({"partNumber": self.stock.part_number} if self.stock.part_number else {}),
            "material": "Nylon 6/6" if self.bushing.material == "NYLON" else "Bronze - SAE 841",
        }


COLUMNS = ("vendor", "material", "bore", "size", "dimensions")
_ENUMS = {
    "vendor": ("Vendor", BushingVendor, "vendor"),
    "material": ("Material", BushingMaterial, "material"),
    "bore": ("Bore", BushingBore, "bore"),
    "size": ("Size", BushingSize, "size"),
    "dimensions": ("Dimensions", BushingDimensions, "dimensions"),
}
SEAT_MATE_CONNECTOR = "mateConnector"
# The most used: FRCDesign's 1/2 in rounded hex bronze bushing, whose one size Dimensions (shown only for the 3/4 in
# round bushings, which come in two) resolves to
DEFAULTS = ("GENERIC", "BRONZE", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_333")

TABLE = LookupTable(COLUMNS, list(BUSHINGS))


def _input(id: str, default: str) -> Input:
    name, enum, parameter = _ENUMS[id]
    assert enum.names is not None
    options = [Option(value, enum.names[value], value) for value in enum.values]
    return TABLE.input(Input(id, name, parameter, options, enum_name=enum.name, default=default))


STUDIOS = {
    "bushing": ConfiguredStudio(
        "bushing",
        "libraryBushing",
        [_input(id, default) for id, default in zip(COLUMNS, DEFAULTS)],
        tab="Bushing",
        mate_connector=SEAT_MATE_CONNECTOR,
        top_mate_connector="topMateConnector",
        bottom_mate_connector="bottomMateConnector",
    )
}
RECORDS = {
    "bushing": {
        key: Record(TABLE.shown(key), bushing_name(bushing), key[0], stock, bushing) for key, (bushing, stock) in BUSHINGS.items()
    }
}

# Where the studio was last released: `fs parts bushings --release` records it beside this file
LOCATIONS = read_locations(pathlib.Path(__file__))


# FeatureScript


def _fields(fields: dict[str, str]) -> str:
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in fields.items()) + " }"


def _nested(table: dict[tuple, str], enums: list[Enum], indent: int = 0) -> str:
    groups: dict[str, dict] = {}
    for key, value in table.items():
        groups.setdefault(key[0], {})[key[1:]] = value
    pad = " " * indent
    entries = [
        f"{pad}    {enums[0][first]} : " + (rest[()] if len(enums) == 1 else _nested(rest, enums[1:], indent + 4))
        for first, rest in groups.items()
    ]
    return "{\n" + ",\n".join(entries) + f"\n{pad}}}"


def _bushing_fields(key: Key, bushing: Bushing, stock: Stock) -> str:
    flange = bushing.flange
    material, appearance = _MATERIALS[bushing.material]
    vendor = _VENDOR_NAMES.get(key[0])
    fields = {
        "outerDiameter": inch(bushing.outer_diameter),
        "width": inch(bushing.width),
        "flangeDiameter": inch(flange.diameter),
        "flangeThickness": inch(flange.thickness),
        **({"stepDiameter": inch(flange.step_diameter), "stepHeight": inch(flange.step_height)} if flange.step_diameter else {}),
        "name": string(bushing_name(bushing)),
        **({"vendor": string(vendor)} if vendor else {}),
        **({"partNumber": string(stock.part_number)} if stock.part_number else {}),
        "material": material,
        "appearance": appearance,
    }
    return _fields(fields)


def _bore_fields(bore: str, size: str) -> str:
    value = inch(SIZES[size][1])
    if bore == "ROUND":
        return _fields({"diameter": value})
    return _fields({"acrossFlats": value, "diameter": ROUNDED_DIAMETERS[size]})


_BORE_SIZES = sorted({(b.bore, b.size) for b, _ in BUSHINGS.values()}, key=lambda k: (BushingBore.values.index(k[0]), list(SIZES).index(k[1])))

CONTENTS = [
    Import("core/robotProperties.fs"),
    BushingVendor,
    BushingMaterial,
    BushingBore,
    BushingSize,
    BushingDimensions,
    Code(
        "/** Each bore's shape: a round bore's `diameter`, or a rounded hex's `acrossFlats` and the `diameter` its corners"
        " are rounded to. */\nexport const BUSHING_BORES = "
        + _nested({key: _bore_fields(*key) for key in _BORE_SIZES}, [BushingBore, BushingSize])
        + ";"
    ),
    Code(
        "/** What each vendor makes, by vendor, material, bore, size, and dimensions: each bushing's sizes, its flange"
        " (and step), and its properties. */\nexport const BUSHINGS = "
        + _nested({key: _bushing_fields(key, *value) for key, value in BUSHINGS.items()},
                  [BushingVendor, BushingMaterial, BushingBore, BushingSize, BushingDimensions])
        + ";"
    ),
]


# FRCDesign's manifest


def manifest() -> dict:
    """The part set's manifest, for FRCDesign: its studio and records, and its catalog."""
    return {
        "source": "featurescripts/bushings/bushingTables.py",
        "studios": {
            name: {
                **studio.manifest(),
                "location": LOCATIONS.get(name),
                "records": [record.manifest() for record in RECORDS[name].values()],
            }
            for name, studio in STUDIOS.items()
        },
        "vendors": [{"id": id, "name": BushingVendor.names[id]} for id in BushingVendor.values],  # type: ignore[index]
        "bushings": [
            {
                "vendor": vendor,
                "material": material,
                "bore": bore,
                "size": size,
                "boreSize": round(SIZES[size][1] * INCH, 9),
                "dimensions": dimensions,
                "outerDiameter": round(bushing.outer_diameter * INCH, 9),
                "width": round(bushing.width * INCH, 9),
                "flangeDiameter": round(bushing.flange.diameter * INCH, 9),
                **({"partNumber": stock.part_number} if stock.part_number else {}),
            }
            for (vendor, material, bore, size, dimensions), (bushing, stock) in BUSHINGS.items()
        ],
    }


EXPORTS = {"bushings.gen.json": json.dumps(manifest(), indent=2) + "\n"}
