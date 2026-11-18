"""Tables for Robot bearing and Robot bushing: FRCDesignLib's bearings and bushings, and WCP's bearing hats, which
they declare for FRCDesignApp's Auto Assembly (see `fs_cli/library.py`).

Read from the snapshot in `vendor/frcdesign-bearings.json`, which `uv run python
featurescripts/released/bearing/bearingTables.py fetch` refreshes: each size and vendor the library indexes, named as it
names them. A size's name gives its sizes (`<bore> x <OD> OD x <width> WD`): its outside diameter, which the bore is cut
to, and its width and inner diameter, which its stand-in is drawn at. A hex, rounded hex, or square bore is drawn as a
circle at its nominal size, since the stand-in only shows where the part goes.
"""

from __future__ import annotations

import json
import pathlib
import re
import sys

from fs_cli import library
from fs_cli.tables import Node, Table, Value, inch, mm, string

SNAPSHOT = pathlib.Path(__file__).parent / "vendor" / "frcdesign-bearings.json"
BEARINGS = {
    "Flanged radial": "Flanged Radial Bearing",
    "Radial": "Radial Bearing",
    "X-contact": "X-Contact Bearing",
    "Needle roller": "Needle Roller Bearing",
}
BUSHING = "Flanged Bushing"
HAT = "Bearing Retention Hat (WCP)"

if __name__ == "__main__" and sys.argv[1:] == ["fetch"]:
    library.fetch(SNAPSHOT, [*BEARINGS.values(), BUSHING, HAT])
    sys.exit()

PARTS = library.load(SNAPSHOT)

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


def bearing_node() -> Node:
    return Node(
        "type",
        [
            Value(label, next=size_node(name, "Configuration" if label == "Needle roller" else "Dimensions"))
            for label, name in BEARINGS.items()
        ],
        display_name="Type",
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
    Table("bearingTable", bearing_node()),
    Table("bushingTable", Node("bushing", [Value("Flanged", next=size_node(BUSHING, "Dimensions"))], display_name="Type")),
    Table("wcpHatTable", hat_node()),
    Table("screwStudioTable", SCREW_STUDIO),
]
