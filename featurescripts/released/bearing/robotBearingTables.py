"""Tables for Robot bearing, Robot bearing retention and Robot bushing: the parts they declare for FRCDesignApp's Auto Assembly.

- Bearings and bushings are FRCDesign's bearings and bushings part sets' (`featurescripts/bearings/`,
  `featurescripts/bushings/`): every one their FRC studios offer, from their catalogs, by what the part is and then who
  makes it, by vendor first, each with its studio's configuration (its record's values) and the studio's location once the set is
  released. Before then, a part's product has no document, and the feature declares nothing.
- Bearing hats are FRCDesign's bearing hats part set's (`featurescripts/bearingHats/`): WCP's retention plates, by the
  bearing's outer diameter and their centers, each with its studio's configuration and its sizes.
- Screws and nuts are the fasteners part set's Screw and Nut studios, where they're released.
"""

from __future__ import annotations

import json
import pathlib
import runpy

from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, inch, string

_FEATURESCRIPTS = pathlib.Path(__file__).parents[2]
BEARING_SET = runpy.run_path(str(_FEATURESCRIPTS / "bearings" / "bearingTables.py"))
BUSHING_SET = runpy.run_path(str(_FEATURESCRIPTS / "bushings" / "bushingTables.py"))
HAT_SET = runpy.run_path(str(_FEATURESCRIPTS / "bearingHats" / "hatTables.py"))

def _product(location: dict | None, configuration: dict[str, str], name: str) -> str:
    entries = ", ".join(f"{json.dumps(key)} : {json.dumps(value)}" for key, value in configuration.items())
    return (
        f'{{ "documentId" : {json.dumps((location or {}).get("documentId", ""))}, '
        f'"elementId" : {json.dumps((location or {}).get("elementId", ""))}, '
        f'"configuration" : {{ {entries} }}, "name" : {json.dumps(name)} }}'
    )


def part_set_node(
    records: dict[str, dict], locations: dict, columns: tuple, enums: dict, levels: list[str], defaults: dict[str, str]
) -> Node:
    """A part set's parts, by `levels` (its columns, in the order the feature asks them), in its enums' orders, leaving
    out a level where a part has one option. Each names its enums by its parameter's name (the feature reads its sizes
    from the set's tables) and its product: its studio, and its record's configuration."""
    leaves = {}
    for studio, studio_records in records.items():
        for key, record in studio_records.items():
            values = dict(zip(columns, key))
            leaves[tuple(values[level] for level in levels)] = {
                **{enums[column][2]: enums[column][1][values[column]] for column in columns},
                "product": _product(locations.get(studio), record.values, record.name),
            }

    def level(depth: int, prefix: tuple) -> Node:
        column = levels[depth]
        name, enum, _ = enums[column]
        options = [value for value in enum.values if any(key[:depth] == prefix and key[depth] == value for key in leaves)]
        values = []
        for option in options:
            path = prefix + (option,)
            if depth + 1 == len(levels):
                values.append(Value(enum.names[option], leaves[path]))
            else:
                values.append(Value(enum.names[option], next=collapse(depth + 1, path)))
        # The most inserted part's options, where they're offered (the set's own defaults)
        default = defaults[column] if defaults[column] in options else None
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


def bearing_node() -> Node:
    """The bearings set's FRC bearings, by vendor, type, bore, size, dimensions, and seal."""
    columns = BEARING_SET["COLUMNS"]
    defaults = dict(zip(columns, ("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")))
    return part_set_node(
        BEARING_SET["FRC"].records, BEARING_SET["LOCATIONS"], columns, BEARING_SET["_ENUMS"],
        ["vendor", "type", "bore", "size", "dimensions", "seal"], defaults,
    )


def bushing_node() -> Node:
    """The bushings set's bushings, by vendor, bore, size, material, and dimensions."""
    columns = BUSHING_SET["COLUMNS"]
    defaults = {"bore": "ROUNDED_HEX", "size": "IN_1_2", "material": "BRONZE", "dimensions": "OD_0_75_X_0_313", "vendor": "GENERIC"}
    return part_set_node(
        BUSHING_SET["RECORDS"], BUSHING_SET["LOCATIONS"], columns, BUSHING_SET["_ENUMS"],
        ["vendor", "bore", "size", "material", "dimensions"], defaults,
    )


def hat_node() -> Node:
    """The hats set's hats by the bearing's outer diameter and their centers ("1.125 in", "2 in"), for Robot bearing
    retention: each its product, the bearing's outer diameter, its centers, and the sizes a screw through it needs."""
    location = HAT_SET["LOCATIONS"].get("bearingHat")
    shape = HAT_SET["SHAPE"]
    hats: dict[str, list[Value]] = {}
    for record in HAT_SET["RECORDS"]["bearingHat"].values():
        hat = record.hat
        hats.setdefault(f"{hat.outer_diameter:g} in", []).append(
            Value(
                f"{HAT_SET['_CENTERS'][hat.centers]:g} in",
                {
                    "product": _product(location, record.values, hat.name),
                    "outerDiameter": inch(hat.outer_diameter),
                    "centers": inch(HAT_SET["_CENTERS"][hat.centers]),
                    "thickness": inch(shape.thickness),
                    "holeDiameter": inch(shape.hole_diameter),
                },
            )
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
            name,
            {
                "documentId": string(FASTENER_STUDIOS["documentId"]),
                "elementId": string(FASTENER_STUDIOS["elements"][element]),
            },
        )
        for name, element in (("Screw", "screw"), ("Nut", "nut"))
    ],
)

CONTENTS = [
    # The sets' enums, which their parts' entries name
    Import("bearings/bearingTables.gen.fs"),
    Table("robotBearingTable", bearing_node()),
    Import("bushings/bushingTables.gen.fs"),
    Table("bushingTable", bushing_node()),
    Table("hatTable", hat_node()),
    Table("screwStudioTable", SCREW_STUDIO),
]
