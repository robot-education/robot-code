"""FRCDesignLib's parts, from FRCDesign's public API, for features declaring library parts (see docs/auto-assembly.md).

A definition keeps a snapshot of the groups it reads (`fetch`, run by its own `fetch` command, never by `fs gen`), and
reads each studio's products from it (`products`): one per configuration with a part number, with the whole
configuration to insert it with, as FRCDesign stores a selection (parameter id to option id).
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from typing import Callable, Iterator

from fs_cli import tables

API = "https://app.frcdesign.org/api"
LIBRARY = "frc-design-lib"

# The vendors part numbers start with, for studios several vendors sell (like Pinion Pulley - 550 Bore)
PART_NUMBER_VENDORS = {"WCP": "WCP", "TTB": "TTB", "REV": "REV", "am": "AM", "217": "VEX"}

# Library parts' configuration parameters which pick a lighter model: products are inserted in full
SIMPLIFIED_PARAMETERS = {"Simplified Model"}


def _get_json(url: str):
    import urllib.request

    # FRCDesign refuses Python's own user agent
    request = urllib.request.Request(url, headers={"User-Agent": "robot-code library fetch"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def fetch(path: pathlib.Path, groups: list[str], keep: Callable[[str], bool]) -> None:
    """Writes the configurations of the insertables in `groups` whose names `keep` takes, in each group's order, to
    `path`."""
    version = _get_json(f"{API}/library-version/library/{LIBRARY}")["version"]
    library = _get_json(f"{API}/library-data/library/{LIBRARY}?v={version}")
    insertables = library["insertables"]
    parts = []
    for group in library["groups"].values():
        if group["name"] not in groups:
            continue
        for insertable_id in group["insertableOrder"]:
            insertable = insertables[insertable_id]
            if not keep(insertable["name"]) or not insertable["isConfigurable"]:
                continue
            configuration = _get_json(f"{API}/configuration/insertable/{insertable_id}?v={version}")
            parts.append(
                {
                    "insertableId": insertable_id,
                    "group": group["name"],
                    "name": insertable["name"],
                    "vendors": insertable["vendors"],
                    "elementType": insertable["elementType"],
                    "documentId": insertable["documentId"],
                    "elementId": insertable["elementId"],
                    "parameters": [
                        {key: parameter[key] for key in ("id", "name", "type", "default") if key in parameter}
                        | ({"options": parameter["options"]} if "options" in parameter else {})
                        for parameter in configuration["parameters"]
                    ],
                    "records": [
                        {"partNumber": record["partNumber"], "name": record["name"], "values": record["values"]}
                        for record in configuration["records"]
                        if record.get("partNumber")
                    ],
                }
            )
    snapshot = {"source": f"{API}/configuration/insertable/{{id}}?v={version} ({LIBRARY}: {', '.join(groups)})", "parts": parts}
    path.write_text(json.dumps(snapshot, indent=1) + "\n", newline="")
    print(f"Wrote {len(parts)} studios to {path.as_posix()}.")


@dataclasses.dataclass(frozen=True)
class Product:
    """A library part, in one configuration.

    Attributes:
        name: Its record's name, as FRCDesign shows it, like `15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`.
        configuration: Its studio's every parameter's value, by id: its record's, else the parameter's default, but a
            simplification parameter's off.
    """

    vendor: str
    part_number: str
    name: str
    studio: str
    document_id: str
    element_id: str
    configuration: dict[str, str]


def _vendor(part: dict, part_number: str) -> str | None:
    if len(part["vendors"]) == 1:
        return part["vendors"][0]
    prefix = re.split(r"[-\s]", part_number, maxsplit=1)[0]
    return PART_NUMBER_VENDORS.get(prefix)


def products(snapshot: dict) -> Iterator[Product]:
    """Each studio's products, in its order, once each (a configuration differing only in a simplification parameter is
    the same product)."""
    for part in snapshot["parts"]:
        seen = set()
        for record in part["records"]:
            configuration = {}
            for parameter in part["parameters"]:
                if parameter["name"] in SIMPLIFIED_PARAMETERS:
                    configuration[parameter["id"]] = "false"
                else:
                    configuration[parameter["id"]] = record["values"].get(parameter["id"], parameter["default"])
            key = json.dumps(configuration, sort_keys=True)
            vendor = _vendor(part, record["partNumber"])
            if key in seen or vendor is None:
                continue
            seen.add(key)
            yield Product(
                vendor,
                record["partNumber"],
                record["name"],
                part["name"],
                part["documentId"],
                part["elementId"],
                configuration,
            )


# Tables of products, by vendor and bore

# Vendors' names as robot-code's tables have them, the most common first
VENDORS = {"WCP": "WCP", "TTB": "The Thrifty Bot", "REV": "REV", "AM": "AndyMark", "VEX": "VEX", "LAI": "LAI"}

# Bores by the names records give them (the same bore, named differently, is one), the most common first, with a
# stand-in's hole, a circle the shaft fits in (a hex's across its corners), as a FeatureScript length, or none for a
# motor's pinion
_COS_30 = 3**0.5 / 2
BORES: dict[str, tuple[list[str], str | None]] = {
    '1/2" Hex': (['1/2" Hex', '1/2" Rounded Hex'], tables.inch(round(0.5 / _COS_30, 4))),
    "8mm SplineXS": (["8mm SplineXS"], tables.mm(8)),
    "Falcon Spline": (["Falcon Spline"], tables.mm(8)),
    "8mm CIM/NEO": (["8mm Keyed", "8mm CIM/NEO"], tables.mm(8)),
    '3/8" Hex': (['3/8" Hex'], tables.inch(round(0.375 / _COS_30, 4))),
    "MAXSpline": (["MAXSpline"], tables.inch(1.375)),
    "SplineXL": (["SplineXL"], tables.inch(1.375)),
    '1.125" Round': (['1.125" Round', '1.125" Bearing'], tables.inch(1.125)),
    '0.958" Round': (['0.958" Round'], tables.inch(0.958)),
    "550": (["550"], None),
    "775": (["775"], None),
}
BORE_NAMES = {name: bore for bore, (names, _) in BORES.items() for name in names}


def entry(product: Product, bore: str, fields: dict[str, str]) -> str:
    """A product as a FeatureScript map: `fields`, then its name, partNumber, documentId, elementId, configuration (its
    selection), and boreDiameter (a stand-in's hole, but a motor pinion's)."""
    configuration = ", ".join(
        f"{tables.string(id)} : {tables.string(option)}" for id, option in product.configuration.items()
    )
    fields = {
        **fields,
        "name": tables.string(product.name),
        "partNumber": tables.string(product.part_number),
        "documentId": tables.string(product.document_id),
        "elementId": tables.string(product.element_id),
        "configuration": f"{{ {configuration} }}",
    }
    if BORES[bore][1] is not None:
        fields["boreDiameter"] = BORES[bore][1]
    return "{ " + ", ".join(f"{tables.string(name)} : {value}" for name, value in fields.items()) + " }"


def vendor_bore_table(
    found: dict[str, dict[str, list[str]]], then: Callable[[str, str], tables.Node | None] = lambda vendor, bore: None
) -> tables.Node:
    """A lookup table choosing a vendor, then a bore (unless it has one), of `found` (entries by vendor and bore), then
    `then`'s levels for them, if any: its leaves give `vendor` (its key in `found`), `vendorName`, and `bore`."""

    def values(vendor: str, bore: str) -> dict[str, str]:
        return {
            "vendor": tables.string(vendor),
            "vendorName": tables.string(VENDORS[vendor]),
            "bore": tables.string(bore),
        }

    entries = []
    for vendor in VENDORS:
        if vendor not in found:
            continue
        bores = [bore for bore in BORES if bore in found[vendor]]
        if len(bores) == 1:
            entries.append(tables.Value(VENDORS[vendor], values(vendor, bores[0]), next=then(vendor, bores[0])))
        else:
            bore_node = tables.Node("bore", [tables.Value(bore, values(vendor, bore), next=then(vendor, bore)) for bore in bores])
            entries.append(tables.Value(VENDORS[vendor], next=bore_node))
    return tables.Node("vendor", entries)


def vendor_bore_map(found: dict[str, dict[str, list[str]]]) -> str:
    """`found` (entries by vendor and bore) as a FeatureScript map of maps of arrays."""
    vendors = []
    for vendor in VENDORS:
        if vendor not in found:
            continue
        bores = []
        for bore in BORES:
            if bore in found[vendor]:
                entries = ",\n                ".join(found[vendor][bore])
                bores.append(f"            {tables.string(bore)} : [\n                {entries}\n            ]")
        vendors.append(f"        {tables.string(vendor)} : {{\n" + ",\n".join(bores) + "\n        }")
    return "{\n" + ",\n".join(vendors) + "\n    }"
