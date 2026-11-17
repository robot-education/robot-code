"""FRCDesignLib's sprockets, for Robot sprocket's Library sprocket (see docs/auto-assembly.md). Run `uv run fs gen`
after editing.

Each comes from FRCDesign's Sprockets & Chain Accessories group (vendor/frcdesign-sprockets.json, a snapshot of its
studios' configurations from FRCDesign's public API, which `uv run python featurescripts/chain/sprocketProducts.py
fetch` refreshes): one per configuration with a part number, read from its record's name, like
`16t Aluminum Sprocket (#25, 1/2" Hex Bore, Double Hub)`, as Robot pulley's are (see
released/belt/pulleyProducts.py). Double sprockets, for two chains, are left out, as Robot sprocket makes one.

`SPROCKET_PRODUCTS` has each vendor's sprockets, by bore. `librarySprocketTable` chooses one: a vendor, a bore, a
chain, and its teeth, each level left out where it has one option, and each level's default its studio's own. In Chain
mode, where the chain has set each sprocket's chain type and teeth, `sprocketProductTable` chooses only a vendor and a
bore, and Robot sprocket matches each sprocket to one.
"""

from __future__ import annotations

import json
import pathlib
import re

from fs_cli import frcdesign_library
from fs_cli.frcdesign_library import BORE_NAMES
from fs_cli.gen import Constant, Import
from fs_cli.tables import Node, Table, Value, number

FOLDER = pathlib.Path(__file__).parent
SNAPSHOT_PATH = FOLDER / "vendor" / "frcdesign-sprockets.json"
# Until `fetch` writes it, none
SNAPSHOT = json.loads(SNAPSHOT_PATH.read_text()) if SNAPSHOT_PATH.exists() else {"parts": []}

CHAIN_TYPES = {"#25": "ChainType.ANSI_25", "#35": "ChainType.ANSI_35"}

# `16t Aluminum Sprocket (#25, 1/2" Hex Bore, Double Hub)`, `32t Aluminum Plate Sprocket (#25, 1.125" Round Bore)`
RECORD = re.compile(r"^(?P<teeth>\d+)t (?P<kind>[^(]*)Sprocket \((?P<chain>#\d+), (?P<bore>.+?) Bore\b")


# Each vendor's sprockets' chains and teeth, by bore, as SPROCKET_PRODUCTS has them
CHOICES: dict[str, dict[str, list[tuple[str, int]]]] = {}


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
        CHOICES.setdefault(product.vendor, {}).setdefault(bore, []).append((match["chain"], int(match["teeth"])))
    return found


PRODUCTS = _products()


def _sprockets(vendor: str, bore: str) -> Node:
    """A vendor's sprockets with a bore: by chain (where it has several), then teeth, each giving its `index` in
    SPROCKET_PRODUCTS."""
    choices = CHOICES[vendor][bore]

    def of_chain(chain: str) -> Node:
        indexes = [index for index, choice in enumerate(choices) if choice[0] == chain]
        ordered = sorted(indexes, key=lambda index: choices[index][1])
        return Node(
            "teeth",
            [Value(f"{choices[index][1]}T", {"index": number(index)}) for index in ordered],
            default=f"{choices[indexes[0]][1]}T",
        )

    chains = [chain for chain in CHAIN_TYPES if any(choice[0] == chain for choice in choices)]
    if len(chains) == 1:
        return of_chain(chains[0])
    return Node("chainType", [Value(chain, next=of_chain(chain)) for chain in chains], display_name="Chain type")


CONTENTS = [
    Import("chain/chainCommon.fs"),
    # A sprocket, in Manual
    Table("librarySprocketTable", frcdesign_library.vendor_bore_table(PRODUCTS, _sprockets)),
    # A vendor and bore, in Chain mode
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
