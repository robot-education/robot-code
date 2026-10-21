"""The fasteners FRCDesign's Quick fastener inserts: screws, washers, and nuts for FRC and FTC. Run `uv run fs gen`
after editing.

This is the source of three things, which must agree:

- quickFastenerTables.gen.fs: the enums and dimensions the screw, washer, and nut features build from.
- The configurations of the Part Studios exposing those features, one per program and kind (`STUDIOS`), which
  `fs configure-studio` writes: a List input per feature parameter, Length showing only the lengths the chosen Size
  comes in. A screw's and a nut's Washer input moves only its mate connector, onto the washer it sits on, so each
  part is fastened to the hole's own edge with one mate connector and no offset.
- quickFastener.gen.json: the same catalog for FRCDesign, which picks a fastener for each hole and inserts its studio
  with a configuration of these option ids. FRCDesign stores those configurations in documents, so never rename or
  remove a released option id (as with enums stored in documents); add new ones.

Dimensions are the nominal ones of each standard (ASME B18.3 and B18.6 for inch screws, ISO 4762, 7380, and 10642 for
metric ones; SAE and ISO 7089/7093 washers; hex and nylon-insert lock nuts), close enough to check fit and clearance,
not to make the parts. Clearance holes are std's (`ANSI_V2ClearanceHoleTable`, `ISO_V2ClearanceHoleTable`), and tap
drills are std's 75% engagement ones.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from fractions import Fraction

from fs_cli.configured_studios import ConfiguredStudio, Input, Option
from fs_cli.gen import Constant
from fs_cli.tables import Enum, inch, mm, string

INCH = 0.0254
MILLIMETER = 0.001


@dataclasses.dataclass
class Size:
    id: str
    name: str
    program: str
    metric: bool
    # As in std's hole tables
    hole_table_size: str
    pitch: str
    major_diameter: float
    tap_drill_diameter: float
    close_clearance_diameter: float
    free_clearance_diameter: float
    # The screw lengths it comes in, shortest first
    lengths: list[float]

    def length(self, value: float) -> str:
        return mm(value) if self.metric else inch(value)

    def meters(self, value: float) -> float:
        return round(value * (MILLIMETER if self.metric else INCH), 9)


@dataclasses.dataclass
class Head:
    diameter: float
    height: float
    # Across the flats of its hex socket
    socket: float
    # A flat head's length includes its head, which sits in a countersink of this angle
    countersink_angle: float | None = None


@dataclasses.dataclass
class Washer:
    inner_diameter: float
    outer_diameter: float
    thickness: float


@dataclasses.dataclass
class Nut:
    across_flats: float
    height: float


def lengths(*values: float | str) -> list[float]:
    return [float(Fraction(value)) if isinstance(value, str) else float(value) for value in values]


INCH_LENGTHS = ["1/4", "3/8", "1/2", "5/8", "3/4", "7/8", 1, "5/4", "3/2", "7/4", 2, "9/4", "5/2", "11/4", 3, "7/2", 4]

SIZES = [
    Size("NO_6_32", "#6-32", "frc", False, "#6", "32 tpi", 0.138, 0.1065, 0.144, 0.1495,
         lengths("1/4", "3/8", "1/2", "5/8", "3/4", 1, "5/4", "3/2")),
    Size("NO_8_32", "#8-32", "frc", False, "#8", "32 tpi", 0.164, 0.136, 0.1695, 0.177,
         lengths("1/4", "3/8", "1/2", "5/8", "3/4", 1, "5/4", "3/2", "7/4", 2)),
    Size("NO_10_32", "#10-32", "frc", False, "#10", "32 tpi", 0.19, 0.159, 0.196, 0.201, lengths(*INCH_LENGTHS)),
    Size("QUARTER_20", "1/4-20", "frc", False, "1/4", "20 tpi", 0.25, 0.201, 0.257, 0.266,
         lengths(*INCH_LENGTHS[1:])),
    Size("M3", "M3", "ftc", True, "M3", "0.5 mm", 3, 2.5, 3.2, 3.4,
         lengths(4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 30, 35, 40)),
    Size("M4", "M4", "ftc", True, "M4", "0.7 mm", 4, 3.3, 4.3, 4.5,
         lengths(6, 8, 10, 12, 14, 16, 18, 20, 25, 30, 35, 40, 45, 50)),
]
SIZES_BY_ID = {size.id: size for size in SIZES}

FastenerSize = Enum("FastenerSize", [size.id for size in SIZES], {size.id: size.name for size in SIZES})
ScrewHead = Enum(
    "ScrewHead", ["SOCKET", "BUTTON", "FLAT"], {"SOCKET": "Socket head", "BUTTON": "Button head", "FLAT": "Flat head"}
)
WasherType = Enum("WasherType", ["FLAT", "WIDE"], {"FLAT": "Flat", "WIDE": "Wide"})
NutType = Enum("NutType", ["NYLOCK", "HEX"], {"NYLOCK": "Nylock", "HEX": "Hex"})
SeatWasher = Enum("SeatWasher", ["NONE", "FLAT", "WIDE"], {"NONE": "None", "FLAT": "Flat washer", "WIDE": "Wide washer"})

# Inch heads: B18.3 socket and button heads, and B18.3 82 degree flat heads
HEADS = {
    "NO_6_32": {"SOCKET": Head(0.226, 0.138, 7 / 64), "BUTTON": Head(0.262, 0.074, 5 / 64), "FLAT": Head(0.307, 0.097, 5 / 64, 82)},
    "NO_8_32": {"SOCKET": Head(0.27, 0.164, 9 / 64), "BUTTON": Head(0.312, 0.087, 3 / 32), "FLAT": Head(0.359, 0.115, 3 / 32, 82)},
    "NO_10_32": {"SOCKET": Head(0.312, 0.19, 5 / 32), "BUTTON": Head(0.375, 0.102, 1 / 8), "FLAT": Head(0.411, 0.132, 1 / 8, 82)},
    "QUARTER_20": {"SOCKET": Head(0.375, 0.25, 3 / 16), "BUTTON": Head(0.437, 0.132, 5 / 32), "FLAT": Head(0.531, 0.153, 5 / 32, 82)},
    "M3": {"SOCKET": Head(5.5, 3, 2.5), "BUTTON": Head(5.7, 1.65, 2), "FLAT": Head(6.72, 1.86, 2, 90)},
    "M4": {"SOCKET": Head(7, 4, 3), "BUTTON": Head(7.6, 2.2, 2.5), "FLAT": Head(8.96, 2.48, 2.5, 90)},
}

# SAE flat and fender washers; ISO 7089 and 7093
WASHERS = {
    "NO_6_32": {"FLAT": Washer(0.156, 0.312, 0.032), "WIDE": Washer(0.156, 0.5, 0.04)},
    "NO_8_32": {"FLAT": Washer(0.188, 0.375, 0.049), "WIDE": Washer(0.188, 0.625, 0.04)},
    "NO_10_32": {"FLAT": Washer(0.219, 0.5, 0.049), "WIDE": Washer(0.219, 0.75, 0.05)},
    "QUARTER_20": {"FLAT": Washer(0.281, 0.625, 0.065), "WIDE": Washer(0.281, 1, 0.063)},
    "M3": {"FLAT": Washer(3.2, 7, 0.5), "WIDE": Washer(3.2, 9, 0.8)},
    "M4": {"FLAT": Washer(4.3, 9, 0.8), "WIDE": Washer(4.3, 12, 1)},
}

# Machine screw hex nuts and nylon-insert lock nuts; ISO 4032 and 10511
NUTS = {
    "NO_6_32": {"NYLOCK": Nut(0.3125, 0.172), "HEX": Nut(0.3125, 0.109)},
    "NO_8_32": {"NYLOCK": Nut(0.34375, 0.203), "HEX": Nut(0.34375, 0.125)},
    "NO_10_32": {"NYLOCK": Nut(0.375, 0.236), "HEX": Nut(0.375, 0.125)},
    "QUARTER_20": {"NYLOCK": Nut(0.4375, 0.312), "HEX": Nut(0.4375, 0.219)},
    "M3": {"NYLOCK": Nut(5.5, 4), "HEX": Nut(5.5, 2.4)},
    "M4": {"NYLOCK": Nut(7, 5), "HEX": Nut(7, 3.2)},
}

PROGRAMS = {"frc": "FRC", "ftc": "FTC"}
DEFAULT_SIZES = {"frc": "NO_10_32", "ftc": "M4"}
DEFAULT_LENGTHS = {"frc": 0.5, "ftc": 10}


def length_id(size: Size, length: float) -> str:
    """e.g. IN_0_375, or MM_10."""
    if size.metric:
        return "MM_" + _number(length).replace(".", "_")
    return "IN_" + _number(length).replace(".", "_")


def length_name(size: Size, length: float) -> str:
    """e.g. 3/8 in., 1-1/4 in., or 10 mm."""
    if size.metric:
        return f"{_number(length)} mm"
    fraction = Fraction(length).limit_denominator(64)
    whole, part = divmod(fraction, 1)
    if part == 0:
        return f"{whole} in."
    if whole == 0:
        return f"{part} in."
    return f"{whole}-{part} in."


def _number(value: float) -> str:
    return f"{value:g}" if value < 1e6 else repr(value)


def _program_sizes(program: str) -> list[Size]:
    return [size for size in SIZES if size.program == program]


def _size_input(program: str) -> Input:
    return Input(
        "size",
        "Size",
        "size",
        [Option(size.id, size.name, size.id) for size in _program_sizes(program)],
        enum_name="FastenerSize",
        default=DEFAULT_SIZES[program],
    )


def _enum_input(id: str, name: str, enum: Enum, parameter: str | None = None) -> Input:
    assert enum.names is not None
    options = [Option(value, enum.names[value], value) for value in enum.values]
    return Input(id, name, parameter or id, options, enum_name=enum.name)


def _length_input(program: str) -> Input:
    sizes = _program_sizes(program)
    options: dict[str, Option] = {}
    for size in sizes:
        for length in size.lengths:
            id = length_id(size, length)
            if id not in options:
                options[id] = Option(id, length_name(size, length), _expression(size, length), [])
            options[id].shown_with.append(size.id)  # type: ignore[union-attr]
    ordered = sorted(options.values(), key=lambda option: _meters(option.value))
    for option in ordered:
        if len(option.shown_with or []) == len(sizes):
            option.shown_with = None
    default = length_id(sizes[0], DEFAULT_LENGTHS[program])
    return Input("length", "Length", "length", ordered, kind="length", default=default, depends_on="size")


def _expression(size: Size, length: float) -> str:
    return f"{_number(length)} {'mm' if size.metric else 'in'}"


def _meters(expression: str) -> float:
    value, unit = expression.split(" ")
    return float(value) * (MILLIMETER if unit == "mm" else INCH)


def studios(program: str) -> dict[str, ConfiguredStudio]:
    return {
        "screw": ConfiguredStudio(
            f"{program}Screw",
            "quickFastenerScrew",
            [
                _size_input(program),
                _enum_input("head", "Head", ScrewHead),
                _length_input(program),
                _enum_input("washer", "Washer", SeatWasher),
            ],
        ),
        "washer": ConfiguredStudio(
            f"{program}Washer", "quickFastenerWasher", [_size_input(program), _enum_input("type", "Type", WasherType, "washerType")]
        ),
        "nut": ConfiguredStudio(
            f"{program}Nut",
            "quickFastenerNut",
            [_size_input(program), _enum_input("type", "Type", NutType, "nutType"), _enum_input("washer", "Washer", SeatWasher)],
        ),
    }


STUDIOS = {program: studios(program) for program in PROGRAMS}

# Where each studio is: its document and tab, and the version FRCDesign inserts. Fill one in once its Part Studio is
# made and configured (`fs configure-studio`), and the version after each release of it.
LOCATIONS: dict[str, dict[str, dict[str, str] | None]] = {
    program: {"screw": None, "washer": None, "nut": None} for program in PROGRAMS
}


# FeatureScript


def _size_map(render) -> str:
    entries = [f"            {FastenerSize[size.id]} : {render(size)}" for size in SIZES]
    return "{\n" + ",\n".join(entries) + "\n        }"


def _fields(fields: dict[str, str]) -> str:
    return "{ " + ", ".join(f"{string(key)} : {value}" for key, value in fields.items()) + " }"


def _size_fields(size: Size) -> str:
    return _fields(
        {
            "name": string(size.name),
            "metric": "true" if size.metric else "false",
            "size": string(size.hole_table_size),
            "pitch": string(size.pitch),
            "majorDiameter": size.length(size.major_diameter),
            "tapDrillDiameter": size.length(size.tap_drill_diameter),
            "closeClearanceDiameter": size.length(size.close_clearance_diameter),
            "freeClearanceDiameter": size.length(size.free_clearance_diameter),
            "lengths": "[" + ", ".join(size.length(length) for length in size.lengths) + "]",
        }
    )


def _by_type(enum: Enum, table: dict[str, dict[str, object]], render) -> object:
    def render_size(size: Size) -> str:
        entries = [f"{enum[key]} : {render(size, value)}" for key, value in table[size.id].items()]
        return "{\n                " + ",\n                ".join(entries) + "\n            }"

    return render_size


def _head_fields(size: Size, head: Head) -> str:
    fields = {
        "headDiameter": size.length(head.diameter),
        "headHeight": size.length(head.height),
        "socketSize": size.length(head.socket),
    }
    if head.countersink_angle is not None:
        fields["countersinkAngle"] = f"{_number(head.countersink_angle)} * degree"
    return _fields(fields)


def _washer_fields(size: Size, washer: Washer) -> str:
    return _fields(
        {
            "innerDiameter": size.length(washer.inner_diameter),
            "outerDiameter": size.length(washer.outer_diameter),
            "thickness": size.length(washer.thickness),
        }
    )


def _nut_fields(size: Size, nut: Nut) -> str:
    return _fields({"acrossFlats": size.length(nut.across_flats), "height": size.length(nut.height)})


CONTENTS = [
    FastenerSize,
    ScrewHead,
    WasherType,
    NutType,
    SeatWasher,
    Constant("FASTENER_SIZES", _size_map(_size_fields)),
    Constant("SCREW_HEADS", _size_map(_by_type(ScrewHead, HEADS, _head_fields))),
    Constant("WASHERS", _size_map(_by_type(WasherType, WASHERS, _washer_fields))),
    Constant("NUTS", _size_map(_by_type(NutType, NUTS, _nut_fields))),
]


# FRCDesign's manifest

HOLE_SCAN = pathlib.Path(__file__).with_name("quickFastenerHoleScan.fs")


def hole_scan_lambda(code: str) -> str:
    """The body of `quickFastenerHoleScan` as the lambda Onshape's FeatureScript evaluation takes: only std's
    common.fs is in scope there, so the function uses nothing else, and keeps its helpers inside it."""
    match = re.search(
        r"export function quickFastenerHoleScan\(context is Context, queries is map\) returns map\n\{\n(.*?)\n\}\n",
        code,
        re.DOTALL,
    )
    if match is None:
        raise ValueError(f"{HOLE_SCAN.name} has no quickFastenerHoleScan(context is Context, queries is map)")
    return "function(context is Context, queries is map)\n{\n" + match.group(1) + "\n}"


def _round(value: float) -> float:
    return round(value, 9)


def manifest() -> dict:
    return {
        "source": "featurescripts/quickFastener/quickFastenerTables.py",
        "programs": [
            {
                "id": program,
                "name": name,
                "sizes": [size.id for size in _program_sizes(program)],
                "studios": {
                    kind: {**studio.manifest(), "location": LOCATIONS[program][kind]}
                    for kind, studio in STUDIOS[program].items()
                },
            }
            for program, name in PROGRAMS.items()
        ],
        "sizes": [
            {
                "id": size.id,
                "name": size.name,
                "program": size.program,
                "majorDiameter": size.meters(size.major_diameter),
                "pitch": _round(1 / float(size.pitch.split(" ")[0]) * INCH)
                if not size.metric
                else _round(float(size.pitch.split(" ")[0]) * MILLIMETER),
                "tapDrillDiameter": size.meters(size.tap_drill_diameter),
                "closeClearanceDiameter": size.meters(size.close_clearance_diameter),
                "freeClearanceDiameter": size.meters(size.free_clearance_diameter),
                "lengths": [{"id": length_id(size, length), "length": size.meters(length)} for length in size.lengths],
            }
            for size in SIZES
        ],
        "heads": [
            {
                "id": head,
                "name": ScrewHead.names[head],  # type: ignore[index]
                "sizes": {
                    size.id: {
                        "diameter": size.meters(HEADS[size.id][head].diameter),
                        "height": size.meters(HEADS[size.id][head].height),
                        "countersunk": HEADS[size.id][head].countersink_angle is not None,
                    }
                    for size in SIZES
                },
            }
            for head in ScrewHead.values
        ],
        "washers": [
            {
                "id": type,
                "name": WasherType.names[type],  # type: ignore[index]
                "sizes": {
                    size.id: {
                        "innerDiameter": size.meters(WASHERS[size.id][type].inner_diameter),
                        "outerDiameter": size.meters(WASHERS[size.id][type].outer_diameter),
                        "thickness": size.meters(WASHERS[size.id][type].thickness),
                    }
                    for size in SIZES
                },
            }
            for type in WasherType.values
        ],
        "nuts": [
            {
                "id": type,
                "name": NutType.names[type],  # type: ignore[index]
                "sizes": {
                    size.id: {
                        "acrossFlats": size.meters(NUTS[size.id][type].across_flats),
                        "height": size.meters(NUTS[size.id][type].height),
                    }
                    for size in SIZES
                },
            }
            for type in NutType.values
        ],
        "holeScan": hole_scan_lambda(HOLE_SCAN.read_text()),
    }


EXPORTS = {"quickFastener.gen.json": json.dumps(manifest(), indent=2) + "\n"}
