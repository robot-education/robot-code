"""FRCDesign's library bearing hats, generated for FRCDesignApp: WCP's bearing retention plates, one for each of
FRCDesign's hand-made Bearing Retention Hat (WCP)'s records (in `vendor/frcdesign-hats.json`, which `uv run python
featurescripts/bearingHats/hatTables.py fetch` refreshes).

The studio asks Vendor, then Size (the bearing's outer diameter it retains) and Centers (its mounting holes' centers),
each shown only where more than one is made with the choices before it (`LookupTable`).

WCP publishes none of a plate's dimensions, so `SHAPE` is an estimate to confirm against FRCDesignLib's model: Robot
bearing's standard hat's outline, as thick as WCP-0328's listed 0.012 lb of 6061 makes it.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import sys

from fs_cli import library
from fs_cli.configured_studios import ConfiguredStudio, Input, LookupTable, Option, read_locations
from fs_cli.gen import Code
from fs_cli.tables import Enum, inch, string

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT = FOLDER / "vendor" / "frcdesign-hats.json"
LIBRARY_PART = "Bearing Retention Hat (WCP)"

if __name__ == "__main__" and sys.argv[1:] == ["fetch"]:
    library.fetch(SNAPSHOT, [LIBRARY_PART])
    sys.exit()

INCH = 0.0254

BearingHatVendor = Enum("BearingHatVendor", ["WCP"], {"WCP": "WCP"})
_VENDOR_NAMES = {"WCP": "West Coast Products"}
# By the bearing's outer diameter it retains
BearingHatSize = Enum("BearingHatSize", ["OD_1_125", "OD_0_875"], {"OD_1_125": "1.125 in", "OD_0_875": "0.875 in"})
_SIZES = {"OD_1_125": 1.125, "OD_0_875": 0.875}
BearingHatCenters = Enum(
    "BearingHatCenters", ["IN_2", "IN_1_5", "IN_1_25"], {"IN_2": "2 in", "IN_1_5": "1.5 in", "IN_1_25": "1.25 in"}
)
_CENTERS = {"IN_2": 2.0, "IN_1_5": 1.5, "IN_1_25": 1.25}


@dataclasses.dataclass(frozen=True)
class Shape:
    """A plate's dimensions, in inches. Estimates: WCP publishes none (see the module's docstring)."""

    thickness: float
    # Past the bearing's outer diameter, all round: its outline's radius about the bore
    rim: float
    # Its mounting holes' lobes
    lobe_radius: float
    # How far it reaches over the bearing's outer race, all round: its hole is the outer diameter less twice this
    overlap: float
    # #10 free clearance
    hole_diameter: float


SHAPE = Shape(thickness=0.09, rim=0.125, lobe_radius=0.25, overlap=0.0625, hole_diameter=0.201)

# FRCDesign's Configuration options: `1.125" Bore, 2" C-C`
_OPTION = re.compile(r'^([\d.]+)" Bore, ([\d.]+)" C-C$')


@dataclasses.dataclass(frozen=True)
class Hat:
    size: str
    centers: str
    part_number: str | None

    @property
    def outer_diameter(self) -> float:
        return _SIZES[self.size]

    @property
    def name(self) -> str:
        return f'{_SIZES[self.size]:g}" OD Bearing Retention Plate ({_CENTERS[self.centers]:g}" C-C)'


Key = tuple[str, str, str]


def _catalog() -> dict[Key, Hat]:
    """Every plate FRCDesign's records name, by (vendor, size, centers)."""
    by_value = {
        **{value: size for size, value in _SIZES.items()},
        **{value: centers for centers, value in _CENTERS.items()},
    }
    found: dict[Key, Hat] = {}
    for record in library.records(library.load(SNAPSHOT)[LIBRARY_PART]):
        match = _OPTION.match(record["options"]["Configuration"])
        assert match is not None, record
        size, centers = by_value[float(match[1])], by_value[float(match[2])]
        found[("WCP", size, centers)] = Hat(size, centers, record["partNumber"])
    return found


HATS = _catalog()


@dataclasses.dataclass(frozen=True)
class Record:
    """One configuration of the studio: its inputs' options, and the part it makes."""

    values: dict[str, str]
    hat: Hat

    def manifest(self) -> dict:
        return {
            "values": self.values,
            "name": self.hat.name,
            "vendor": _VENDOR_NAMES["WCP"],
            **({"partNumber": self.hat.part_number} if self.hat.part_number else {}),
            "material": "Aluminum - 6061",
        }


COLUMNS = ("vendor", "size", "centers")
_ENUMS = {
    "vendor": ("Vendor", BearingHatVendor),
    "size": ("Size", BearingHatSize),
    "centers": ("Centers", BearingHatCenters),
}
SEAT_MATE_CONNECTOR = "mateConnector"
# The most used: the 1.125 in plate at 2 in centers
DEFAULTS = ("WCP", "OD_1_125", "IN_2")

TABLE = LookupTable(COLUMNS, list(HATS))


def _input(id: str, default: str) -> Input:
    name, enum = _ENUMS[id]
    assert enum.names is not None
    options = [Option(value, enum.names[value], value) for value in enum.values]
    return TABLE.input(Input(id, name, id, options, enum_name=enum.name, default=default))


STUDIOS = {
    "bearingHat": ConfiguredStudio(
        "bearingHat",
        "libraryBearingHat",
        [_input(id, default) for id, default in zip(COLUMNS, DEFAULTS)],
        tab="Bearing Hat",
        mate_connector=SEAT_MATE_CONNECTOR,
        top_mate_connector="topMateConnector",
    )
}
RECORDS = {"bearingHat": {key: Record(TABLE.shown(key), hat) for key, hat in HATS.items()}}

# Where the studio was last released: `fs parts bearingHats --release` records it beside this file
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


def _hat_fields(hat: Hat) -> str:
    fields = {
        "outerDiameter": inch(hat.outer_diameter),
        "centers": inch(_CENTERS[hat.centers]),
        "thickness": inch(SHAPE.thickness),
        "rim": inch(SHAPE.rim),
        "lobeRadius": inch(SHAPE.lobe_radius),
        "holeDiameter": inch(SHAPE.hole_diameter),
        "innerDiameter": inch(hat.outer_diameter - 2 * SHAPE.overlap),
        "name": string(hat.name),
        "vendor": string(_VENDOR_NAMES["WCP"]),
        **({"partNumber": string(hat.part_number)} if hat.part_number else {}),
    }
    return _fields(fields)


CONTENTS = [
    BearingHatVendor,
    BearingHatSize,
    BearingHatCenters,
    Code(
        "/** What each vendor makes, by vendor, size and centers: each plate's sizes and properties. The sizes are"
        " estimates (see hatTables.py). */\nexport const BEARING_HATS = "
        + _nested({key: _hat_fields(hat) for key, hat in HATS.items()}, [BearingHatVendor, BearingHatSize, BearingHatCenters])
        + ";"
    ),
]


# FRCDesign's manifest


def manifest() -> dict:
    """The part set's manifest, for FRCDesign: its studio and records, and its catalog."""
    return {
        "source": "featurescripts/bearingHats/hatTables.py",
        "studios": {
            name: {
                **studio.manifest(),
                "location": LOCATIONS.get(name),
                "records": [record.manifest() for record in RECORDS[name].values()],
            }
            for name, studio in STUDIOS.items()
        },
        "vendors": [{"id": id, "name": BearingHatVendor.names[id]} for id in BearingHatVendor.values],  # type: ignore[index]
        "hats": [
            {
                "vendor": vendor,
                "size": size,
                "centers": centers,
                "outerDiameter": round(hat.outer_diameter * INCH, 9),
                "centerDistance": round(_CENTERS[centers] * INCH, 9),
                "thickness": round(SHAPE.thickness * INCH, 9),
                **({"partNumber": hat.part_number} if hat.part_number else {}),
            }
            for (vendor, size, centers), hat in HATS.items()
        ],
    }


EXPORTS = {"bearingHats.gen.json": json.dumps(manifest(), indent=2) + "\n"}
