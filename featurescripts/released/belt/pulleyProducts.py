"""FRCDesignLib's pulleys, for Robot pulley's Library pulley (see docs/auto-assembly.md). Run `uv run fs gen` after
editing.

Each comes from FRCDesign's Pulleys & Belt Accessories group (vendor/frcdesign-pulleys.json, a snapshot of its studios'
configurations from FRCDesign's public API, which `uv run python featurescripts/released/belt/pulleyProducts.py fetch`
refreshes): one per configuration with a part number, read from its record's name, like
`15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`. A configuration's option ids are what FRCDesign inserts it
with, and don't always match their names (a studio's options are often renamed copies), so its record is what says
what it is. Idlers and round belt pulleys are left out.

`pulleyProductTable` chooses a vendor and a bore, and `PULLEY_PRODUCTS` has each one's pulleys, by belt type, teeth,
and width, which Robot pulley matches to each pulley it builds.
"""

from __future__ import annotations

import json
import pathlib
import re

from fs_cli import frcdesign_library
from fs_cli.frcdesign_library import BORE_NAMES
from fs_cli.gen import Constant, Import
from fs_cli.tables import Table, inch, mm, number

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-pulleys.json"
# Until `fetch` writes it, none
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text()) if SNAPSHOT_PATH.exists() else {"parts": []}

BELT_TYPES = {"HTD 5mm": "BeltType._5_MM_HTD", "GT2 3mm": "BeltType._3_MM_GT2", "RT25": "BeltType.RT25"}

# `15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`, and `..., Lightened)`
RECORD = re.compile(
    r"^(?P<teeth>\d+)t [^(]*Pulley \((?P<type>[^,]+), (?P<width>[\d./]+)(?P<unit>mm|\") Wide, (?P<bore>.+?) Bore\b"
)


def _width(match: re.Match) -> str:
    value = match["width"]
    if "/" in value:
        numerator, denominator = value.split("/")
        width = float(numerator) / float(denominator)
    else:
        width = float(value)
    return mm(width) if match["unit"] == "mm" else inch(width)


def _products() -> dict[str, dict[str, list[str]]]:
    """Each vendor's pulleys, by bore, as FeatureScript maps; of the same belt type, teeth, and width, the first."""
    found: dict[str, dict[str, list[str]]] = {}
    seen = set()
    for product in frcdesign_library.products(SNAPSHOT):
        match = RECORD.match(product.name)
        if match is None or match["type"] not in BELT_TYPES or match["bore"] not in BORE_NAMES:
            continue
        bore = BORE_NAMES[match["bore"]]
        width = _width(match)
        key = (product.vendor, bore, match["type"], match["teeth"], width)
        if key in seen:
            continue
        seen.add(key)
        fields = {"beltType": BELT_TYPES[match["type"]], "teeth": number(int(match["teeth"])), "width": width}
        found.setdefault(product.vendor, {}).setdefault(bore, []).append(frcdesign_library.entry(product, bore, fields))
    return found


PRODUCTS = _products()

CONTENTS = [
    Import("released/belt/robotBeltTables.gen.fs"),
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
