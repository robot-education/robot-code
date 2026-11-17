"""FRCDesignLib's sprockets, for Robot sprocket's Library sprocket (see docs/auto-assembly.md). Run `uv run fs gen`
after editing.

Each comes from FRCDesign's Sprockets & Chain Accessories group (vendor/frcdesign-sprockets.json, a snapshot of its
studios' configurations from FRCDesign's public API, which `uv run python featurescripts/chain/sprocketProducts.py
fetch` refreshes): one per configuration with a part number, read from its record's name, like
`16t Aluminum Sprocket (#25, 1/2" Hex Bore, Double Hub)`, as Robot pulley's are (see
released/belt/pulleyProducts.py). Double sprockets, for two chains, are left out, as Robot sprocket makes one.

`sprocketProductTable` chooses a vendor and a bore, and `SPROCKET_PRODUCTS` has each one's sprockets, by chain type
and teeth, which Robot sprocket matches to each sprocket it builds.
"""

from __future__ import annotations

import json
import pathlib
import re

from fs_cli import frcdesign_library
from fs_cli.frcdesign_library import BORE_NAMES
from fs_cli.gen import Constant, Import
from fs_cli.tables import Table, number

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-sprockets.json"
# Until `fetch` writes it, none
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text()) if SNAPSHOT_PATH.exists() else {"parts": []}

CHAIN_TYPES = {"#25": "ChainType.ANSI_25", "#35": "ChainType.ANSI_35"}

# `16t Aluminum Sprocket (#25, 1/2" Hex Bore, Double Hub)`, `32t Aluminum Plate Sprocket (#25, 1.125" Round Bore)`
RECORD = re.compile(r"^(?P<teeth>\d+)t (?P<kind>[^(]*)Sprocket \((?P<chain>#\d+), (?P<bore>.+?) Bore\b")


def _products() -> dict[str, dict[str, list[str]]]:
    """Each vendor's sprockets, by bore, as FeatureScript maps; of the same chain type and teeth, the first."""
    found: dict[str, dict[str, list[str]]] = {}
    seen = set()
    for product in frcdesign_library.products(SNAPSHOT):
        match = RECORD.match(product.name)
        if (
            match is None
            or "Double" in match["kind"]
            or match["chain"] not in CHAIN_TYPES
            or match["bore"] not in BORE_NAMES
        ):
            continue
        bore = BORE_NAMES[match["bore"]]
        key = (product.vendor, bore, match["chain"], match["teeth"])
        if key in seen:
            continue
        seen.add(key)
        fields = {"chainType": CHAIN_TYPES[match["chain"]], "teeth": number(int(match["teeth"]))}
        found.setdefault(product.vendor, {}).setdefault(bore, []).append(frcdesign_library.entry(product, bore, fields))
    return found


PRODUCTS = _products()

CONTENTS = [
    Import("chain/chainCommon.fs"),
    Table("sprocketProductTable", frcdesign_library.vendor_bore_table(PRODUCTS)),
    # Each vendor's sprockets, by bore: each with its chainType, teeth, and what `frcdesign_library.entry` gives
    Constant("SPROCKET_PRODUCTS", frcdesign_library.vendor_bore_map(PRODUCTS)),
]


def _keep(name: str) -> bool:
    return "Sprocket" in name and "Custom" not in name and "Idler" not in name and "Tensioner" not in name


if __name__ == "__main__":
    import sys

    if sys.argv[1:] != ["fetch"]:
        sys.exit("Usage: python sprocketProducts.py fetch")
    frcdesign_library.fetch(SNAPSHOT_PATH, ["Sprockets & Chain Accessories"], _keep)
