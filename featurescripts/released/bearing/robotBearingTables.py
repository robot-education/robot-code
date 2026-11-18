"""Tables for Robot bearing and Robot bushing: the parts they declare for FRCDesignApp's Auto Assembly.

- Bearings and bushings are FRCDesign's bearings and bushings part sets' (`featurescripts/bearings/`,
  `featurescripts/bushings/`): every one their FRC studios offer, from their catalogs, by what the part is and then who
  makes it, each with its studio's configuration (its record's values) and the studio's location once the set is
  released. Before then, a part's product has no document, and the feature declares nothing.
- WCP's bearing hats are FRCDesignLib's own, from the snapshot in `vendor/frcdesign-hats.json`, which `uv run python
  featurescripts/released/bearing/robotBearingTables.py fetch` refreshes (see `fs_cli/library.py`).
"""

from __future__ import annotations

import json
import pathlib
import re
import runpy
import sys

from fs_cli import library
from fs_cli.gen import Import
from fs_cli.tables import Node, Table, Value, string

SNAPSHOT = pathlib.Path(__file__).parent / "vendor" / "frcdesign-hats.json"
HAT = "Bearing Retention Hat (WCP)"

if __name__ == "__main__" and sys.argv[1:] == ["fetch"]:
    library.fetch(SNAPSHOT, [HAT])
    sys.exit()

PARTS = library.load(SNAPSHOT)
_FEATURESCRIPTS = pathlib.Path(__file__).parents[2]
BEARING_SET = runpy.run_path(str(_FEATURESCRIPTS / "bearings" / "bearingTables.py"))
BUSHING_SET = runpy.run_path(str(_FEATURESCRIPTS / "bushings" / "bushingTables.py"))

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
    """The bearings set's FRC bearings, by type, bore, size, dimensions, seal, and vendor."""
    columns = BEARING_SET["COLUMNS"]
    defaults = dict(zip(columns, ("GENERIC", "FLANGED", "ROUNDED_HEX", "IN_1_2", "OD_1_125_X_0_313", "SHIELDED")))
    return part_set_node(
        BEARING_SET["FRC"].records, BEARING_SET["LOCATIONS"], columns, BEARING_SET["_ENUMS"],
        ["type", "bore", "size", "dimensions", "seal", "vendor"], defaults,
    )


def bushing_node() -> Node:
    """The bushings set's bushings, by bore, size, material, dimensions, and vendor."""
    columns = BUSHING_SET["COLUMNS"]
    defaults = dict(zip(columns, ("GENERIC", "BRONZE", "ROUNDED_HEX", "IN_1_2", "OD_0_75_X_0_333")))
    return part_set_node(
        BUSHING_SET["RECORDS"], BUSHING_SET["LOCATIONS"], columns, BUSHING_SET["_ENUMS"],
        ["bore", "size", "material", "dimensions", "vendor"], defaults,
    )


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
    # The sets' enums, which their parts' entries name
    Import("bearings/bearingTables.gen.fs"),
    Table("robotBearingTable", bearing_node()),
    Import("bushings/bushingTables.gen.fs"),
    Table("bushingTable", bushing_node()),
    Table("wcpHatTable", hat_node()),
    Table("screwStudioTable", SCREW_STUDIO),
]
