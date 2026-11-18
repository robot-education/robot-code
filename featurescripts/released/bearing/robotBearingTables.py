"""Tables for Robot bearing and Robot bushing: the parts they declare for FRCDesignApp's Auto Assembly.

- Bearings are FRCDesign's bearings part set's (`featurescripts/bearings/`): every one its FRC studios offer, from its
  catalog, with its studio's configuration (its record's values) and the studio's location once the set is released.
  Before then, a bearing's product has no document, and Robot bearing declares nothing.
- Bushings and WCP's bearing hats are FRCDesignLib's own, from the snapshot in `vendor/frcdesign-bushings.json`, which
  `uv run python featurescripts/released/bearing/robotBearingTables.py fetch` refreshes (see `fs_cli/library.py`). A
  bushing's size name gives its sizes (`<bore> x <OD> OD x <width> WD`): its outside diameter, which the bore is cut
  to, and its width and inner diameter, which its Auto Assembly body is drawn at. A hex or rounded hex bore is drawn as
  a circle at its nominal size, since the body only shows where the part goes.
"""

from __future__ import annotations

import json
import pathlib
import re
import runpy
import sys

from fs_cli import library
from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, inch, mm, string

SNAPSHOT = pathlib.Path(__file__).parent / "vendor" / "frcdesign-bushings.json"
BUSHING = "Flanged Bushing"
HAT = "Bearing Retention Hat (WCP)"

if __name__ == "__main__" and sys.argv[1:] == ["fetch"]:
    library.fetch(SNAPSHOT, [BUSHING, HAT])
    sys.exit()

PARTS = library.load(SNAPSHOT)
BEARING_SET = runpy.run_path(str(pathlib.Path(__file__).parents[2] / "bearings" / "bearingTables.py"))

_SIZE = re.compile(r'^(?P<bore>.+?)(?: ID| Bore)? x (?P<od>[\d./]+(?:"|mm)(?: Hex)?) OD x (?P<width>[\d./]+(?:"|mm)) WD(?: \(.*\))?$')
_LENGTH = re.compile(r'^(?P<value>[\d.]+|\d+/\d+)(?P<unit>"|mm)$')
# A bore which isn't a plain diameter, drawn at its nominal size
_NOMINAL_BORE = re.compile(r'^(?P<size>[\d./]+")(?: Rounded)? (?:Hex|ThunderSquare)$')
_CLEARANCE_BORE = {"#10 Clearance": inch(0.19)}


def length(text: str) -> str:
    match = _LENGTH.match(text)
    if match is None:
        raise ValueError(f"Can't read the length {text}.")
    value = match["value"]
    number = float(value.split("/")[0]) / float(value.split("/")[1]) if "/" in value else float(value)
    return inch(number) if match["unit"] == '"' else mm(number)


def sizes(name: str) -> dict[str, str] | None:
    """A size's outside diameter, width and inner diameter, from its name; None for one this can't cut a bore for (a hex
    outside)."""
    match = _SIZE.match(name)
    if match is None:
        raise ValueError(f"Can't read the sizes of {name}.")
    if "Hex" in match["od"]:
        return None
    bore = match["bore"]
    nominal = _NOMINAL_BORE.match(bore)
    inner = _CLEARANCE_BORE.get(bore) or length(nominal["size"] if nominal else bore)
    return {"outerDiameter": length(match["od"]), "width": length(match["width"]), "innerDiameter": inner}


def size_node(part_name: str, size_parameter: str) -> Node:
    """A part's sizes, each with its vendors where the library has a Vendor parameter for it."""
    part = PARTS[part_name]
    by_size: dict[str, list[dict]] = {}
    for record in library.records(part):
        by_size.setdefault(record["options"][size_parameter], []).append(record)
    values = []
    for size, records in by_size.items():
        dimensions = sizes(re.sub(r" \((?:WCP|TTB|REV)\)$", "", size))
        if dimensions is None:
            continue
        if "Vendor" not in records[0]["options"]:
            values.append(Value(size, {**dimensions, "product": library.product(PARTS, part_name, records[0]["options"])}))
            continue
        vendors = [
            Value(record["options"]["Vendor"], {**dimensions, "product": library.product(PARTS, part_name, record["options"])})
            for record in records
        ]
        values.append(Value(size, next=Node("vendor", vendors, display_name="Vendor")))
    return Node("size", values, display_name="Size")


# Robot bearing's levels: what the bearing is, then who makes it
_BEARING_LEVELS = ["type", "bore", "size", "dimensions", "seal", "vendor"]
DEFAULTS = {"vendor": "GENERIC", "type": "FLANGED", "bore": "ROUNDED_HEX", "size": "IN_1_2", "dimensions": "OD_1_125_X_0_313",
            "seal": "SHIELDED"}
_ENUM_TYPES = {"vendor": "BearingVendor", "type": "BearingType", "bore": "BearingBore", "size": "BearingSize",
               "dimensions": "BearingDimensions", "seal": "BearingSeal"}


def _product(location: dict | None, configuration: dict[str, str], name: str) -> str:
    entries = ", ".join(f"{json.dumps(key)} : {json.dumps(value)}" for key, value in configuration.items())
    return (
        f'{{ "documentId" : {json.dumps((location or {}).get("documentId", ""))}, '
        f'"elementId" : {json.dumps((location or {}).get("elementId", ""))}, '
        f'"configuration" : {{ {entries} }}, "name" : {json.dumps(name)} }}'
    )


def bearing_node() -> Node:
    """The bearings part set's FRC bearings, by type, bore, size, dimensions, seal, and vendor, in its enums' orders,
    leaving out a level where a bearing has one option. Each names its enums (Robot bearing reads its sizes from the
    set's `BEARINGS`) and its product: its studio, and its record's configuration."""
    catalog = BEARING_SET["FRC"]
    locations = BEARING_SET["LOCATIONS"]
    enums = BEARING_SET["_ENUMS"]
    leaves = {}
    for studio, records in catalog.records.items():
        for key, record in records.items():
            values = dict(zip(BEARING_SET["COLUMNS"], key))
            leaves[tuple(values[level] for level in _BEARING_LEVELS)] = {
                **{
                    ("bearingType" if column == "type" else column): f"{_ENUM_TYPES[column]}.{values[column]}"
                    for column in BEARING_SET["COLUMNS"]
                },
                "product": _product(locations.get(studio), record.values, record.name),
            }

    def level(depth: int, prefix: tuple) -> Node:
        column = _BEARING_LEVELS[depth]
        name, enum, _ = enums[column]
        options = [value for value in enum.values if any(key[:depth] == prefix and key[depth] == value for key in leaves)]
        values = []
        for option in options:
            path = prefix + (option,)
            if depth + 1 == len(_BEARING_LEVELS):
                values.append(Value(enum.names[option], leaves[path]))
            else:
                values.append(Value(enum.names[option], next=collapse(depth + 1, path)))
        # The most inserted bearing's options, where they're offered (the bearings set's own defaults)
        default = DEFAULTS[column] if DEFAULTS[column] in options else None
        return Node(column, values, display_name=name, default=enum.names[default] if default else None)

    def collapse(depth: int, prefix: tuple) -> Node:
        # A level with one option is skipped: its one value stands for it
        node = level(depth, prefix)
        while len(node.entries) == 1 and node.entries[0].next is not None:
            prefix = prefix + (next(key[depth] for key in leaves if key[:depth] == prefix),)
            depth += 1
            node = level(depth, prefix)
        return node

    return level(0, ())


def hat_node() -> Node:
    """WCP's hats by their bore and centers, as `Robot bearing` names them ("1.125 in", "2 in")."""
    hats: dict[str, list[Value]] = {}
    for record in library.records(PARTS[HAT]):
        match = re.match(r'^([\d.]+)" Bore, ([\d.]+)" C-C$', record["options"]["Configuration"])
        assert match is not None, record
        hats.setdefault(f"{match[1]} in", []).append(
            Value(f"{match[2]} in", {"product": library.product(PARTS, HAT, record["options"])})
        )
    return Node("bore", [Value(bore, next=Node("centers", centers)) for bore, centers in hats.items()])


# Where FRCDesign's fasteners part set released its Screw studio, for the screws the features declare
FASTENER_STUDIOS = json.loads(
    (pathlib.Path(__file__).parents[2] / "fasteners" / "fastenerTables.studios.json").read_text()
)
SCREW_STUDIO = Node(
    "studio",
    [
        Value(
            "Screw",
            {
                "documentId": string(FASTENER_STUDIOS["documentId"]),
                "elementId": string(FASTENER_STUDIOS["elements"]["screw"]),
            },
        )
    ],
)

CONTENTS = [
    # The bearings set's enums, which its bearings' entries name
    Import("bearings/bearingTables.gen.fs"),
    Table("robotBearingTable", bearing_node()),
    Table("bushingTable", Node("bushing", [Value("Flanged", next=size_node(BUSHING, "Dimensions"))], display_name="Type")),
    Table("wcpHatTable", hat_node()),
    Table("screwStudioTable", SCREW_STUDIO),
]
