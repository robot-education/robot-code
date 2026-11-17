"""FRCDesignLib's pulleys, for Robot pulley's Library pulley (see docs/auto-assembly.md). Run `uv run fs gen` after
editing.

Each comes from FRCDesign's Pulleys & Belt Accessories group (vendor/frcdesign-pulleys.json, a snapshot of its studios'
configurations from FRCDesign's public API, which `uv run python featurescripts/released/belt/pulleyProducts.py fetch`
refreshes): one per configuration with a part number, read from its record's name, like
`15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`. A configuration's option ids are what FRCDesign inserts it
with, and don't always match their names (a studio's options are often renamed copies), so its record is what says
what it is. Idlers and round belt pulleys are left out.

`PULLEY_PRODUCTS` has each vendor's pulleys, by bore. `libraryPulleyTable` chooses one: a vendor, a bore, a belt type,
and a pulley (its teeth, and its width where it comes in several), each level left out where it has one option, and
each level's default its studio's own (its first record's). In Belt mode, where the belt has set each pulley's type
and teeth, `pulleyProductTable` chooses only a vendor and a bore, and Robot pulley matches each pulley to one.
"""

from __future__ import annotations

import json
import pathlib
import re

from fs_cli import frcdesign_library
from fs_cli.frcdesign_library import BORE_NAMES
from fs_cli.gen import Constant, Import
from fs_cli.tables import Node, Table, Value, inch, mm, number

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-pulleys.json"
# Until `fetch` writes it, none
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text()) if SNAPSHOT_PATH.exists() else {"parts": []}

BELT_TYPES = {"HTD 5mm": "BeltType._5_MM_HTD", "GT2 3mm": "BeltType._3_MM_GT2", "RT25": "BeltType.RT25"}
# As the belt tables name them, the most common first
BELT_TYPE_NAMES = {"HTD 5mm": "5mm HTD", "GT2 3mm": "3mm GT2", "RT25": "RT25"}

# `15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`, and `..., Lightened)`
RECORD = re.compile(
    r"^(?P<teeth>\d+)t [^(]*Pulley \((?P<type>[^,]+), (?P<width>[\d./]+)(?P<unit>mm|\") Wide, (?P<bore>.+?) Bore\b"
)


def _width(match: re.Match) -> tuple[str, str, float]:
    """A pulley's width: as an expression, as the belt tables name widths (`9 mm`, `1/2 in.`), and in mm."""
    value = match["width"]
    if "/" in value:
        numerator, denominator = value.split("/")
        width = float(numerator) / float(denominator)
    else:
        width = float(value)
    if match["unit"] == "mm":
        return mm(width), f"{number(width)} mm", width
    return inch(width), f"{value} in.", width * 25.4


# Each vendor's pulleys' belt types, teeth, and widths (named, and in mm), by bore, as PULLEY_PRODUCTS has them
CHOICES: dict[str, dict[str, list[tuple[str, int, str, float]]]] = {}


def _products() -> dict[str, dict[str, list[str]]]:
    """Each vendor's pulleys, by bore, as FeatureScript maps; of the same belt type, teeth, and width, the first."""
    found: dict[str, dict[str, list[str]]] = {}
    seen = set()
    for product in frcdesign_library.products(SNAPSHOT):
        match = RECORD.match(product.name)
        if match is None or match["type"] not in BELT_TYPES or match["bore"] not in BORE_NAMES:
            continue
        bore = BORE_NAMES[match["bore"]]
        width, width_name, width_mm = _width(match)
        key = (product.vendor, bore, match["type"], match["teeth"], width)
        if key in seen:
            continue
        seen.add(key)
        fields = {"beltType": BELT_TYPES[match["type"]], "teeth": number(int(match["teeth"])), "width": width}
        found.setdefault(product.vendor, {}).setdefault(bore, []).append(frcdesign_library.entry(product, bore, fields))
        CHOICES.setdefault(product.vendor, {}).setdefault(bore, []).append(
            (BELT_TYPE_NAMES[match["type"]], int(match["teeth"]), width_name, width_mm)
        )
    return found


PRODUCTS = _products()


def _pulleys(vendor: str, bore: str) -> Node:
    """A vendor's pulleys with a bore: by belt type (where it has several), then teeth and width, each giving its
    `index` in PULLEY_PRODUCTS."""
    choices = CHOICES[vendor][bore]

    def of_type(belt_type: str) -> Node:
        indexes = [index for index, choice in enumerate(choices) if choice[0] == belt_type]
        several_widths = len({choices[index][2] for index in indexes}) > 1

        def name(index: int) -> str:
            _, teeth, width, _ = choices[index]
            return f"{teeth}T {width}" if several_widths else f"{teeth}T"

        ordered = sorted(indexes, key=lambda index: (choices[index][1], choices[index][3]))
        return Node(
            "pulley", [Value(name(index), {"index": number(index)}) for index in ordered], default=name(indexes[0])
        )

    belt_types = [name for name in BELT_TYPE_NAMES.values() if any(choice[0] == name for choice in choices)]
    if len(belt_types) == 1:
        return of_type(belt_types[0])
    return Node(
        "beltType", [Value(belt_type, next=of_type(belt_type)) for belt_type in belt_types], display_name="Belt type"
    )


CONTENTS = [
    Import("released/belt/robotBeltTables.gen.fs"),
    # A pulley, in Manual
    Table("libraryPulleyTable", frcdesign_library.vendor_bore_table(PRODUCTS, _pulleys)),
    # A vendor and bore, in Belt mode
    Table("pulleyProductTable", frcdesign_library.vendor_bore_table(PRODUCTS)),
    # Each vendor's pulleys, by bore: each with its beltType, teeth, width (its own, not its belt's), and what
    # `frcdesign_library.entry` gives
    Constant("PULLEY_PRODUCTS", frcdesign_library.vendor_bore_map(PRODUCTS)),
]


def _keep(name: str) -> bool:
    return "Pulley" in name and "Custom" not in name


if __name__ == "__main__":
    import sys

    if sys.argv[1:] != ["fetch"]:
        sys.exit("Usage: python pulleyProducts.py fetch")
    frcdesign_library.fetch(SNAPSHOT_PATH, ["Pulleys & Belt Accessories"], _keep)
