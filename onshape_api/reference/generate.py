"""Regenerates the committed reference copy of the Onshape API definition.

    uv run python -m onshape_api.reference.generate [--spec openapi.json]

Downloads Onshape's OpenAPI definition (the one behind the Glassworks API explorer) and writes
openapi.json next to this file, trimmed to just the operations listed in OPERATIONS and the schemas
they reach.

Nothing imports the output. The types the code actually uses are hand-written in
onshape_api/types.py, which only covers the fields we read. This file exists to read and to diff
against: regenerating it surfaces upstream API changes (including fields we don't use yet) to
fold into types.py by hand.

To use a new endpoint or field:
    1. Add its operation to OPERATIONS and run this script.
    2. Read the operation's response schema in openapi.json to see the real shape.
    3. Hand-write the subset you need in onshape_api/types.py.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import urllib.request

SPEC_URL = "https://cad.onshape.com/api/openapi"
OUTPUT = pathlib.Path(__file__).parent / "openapi.json"

# (method, path) of every operation onshape_api calls
OPERATIONS = [
    ("get", "/documents/d/{did}/{wvm}/{wvmid}/contents"),
    ("get", "/documents/d/{did}/{wvm}/{wvmid}/elements"),
    ("get", "/documents/d/{did}/versions"),
    ("post", "/documents/d/{did}/versions"),
    ("post", "/featurestudios/d/{did}/w/{wid}"),
    ("get", "/featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}"),
    ("post", "/featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}"),
    ("get", "/featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}/featurespecs"),
]

# Polymorphic base schemas whose subtypes (from their discriminator mapping) should be kept.
# Subtypes extend their base with allOf, so they're otherwise unreachable.
POLYMORPHIC_BASES = [
    "BTGroupOrElementReference-2205",
]

# Fields trimmed because they pull in large schema trees we don't use. A field re-declared on a
# schema's allOf bases is trimmed there too.
OMIT = {
    "BTDocumentElementInfo": ["thumbnailInfo", "applicationTarget"],
    "BTVersionInfo": ["creator", "lastModifier", "thumbnail"],
    "BTFeatureSpecsResponse-664": ["featureSpecs"],
}

# Schemas whose full (huge) definition isn't worth keeping; replaced by a stub.
STUBS = {
    "BTFeatureSpec-129": "A feature's specification. We only read featureTypeName.",
}


def load_spec(path: pathlib.Path | None) -> dict:
    if path:
        return json.loads(path.read_text())
    with urllib.request.urlopen(SPEC_URL) as response:
        return json.load(response)


def trim(spec: dict) -> dict:
    schemas = spec["components"]["schemas"]
    for name, fields in OMIT.items():
        schema = schemas.get(name)
        if not schema:
            continue
        for bag in [schema, *schema.get("allOf", [])]:
            for field in fields:
                bag.get("properties", {}).pop(field, None)

    paths: dict[str, dict] = {}
    for method, path in OPERATIONS:
        operation = spec["paths"][path][method]
        paths.setdefault(path, {})[method] = operation

    kept: dict[str, dict] = {}

    def visit(node: object) -> None:
        if isinstance(node, dict):
            ref = node.get("$ref")
            if isinstance(ref, str) and ref.startswith("#/components/schemas/"):
                keep(ref.split("/")[-1])
            for value in node.values():
                visit(value)
        elif isinstance(node, list):
            for value in node:
                visit(value)

    def keep(name: str) -> None:
        if name in kept or name not in schemas:
            return
        if name in STUBS:
            kept[name] = {"type": "object", "description": STUBS[name]}
            return
        kept[name] = schemas[name]
        visit(schemas[name])
        if name in POLYMORPHIC_BASES:
            for subtype in (
                schemas[name].get("discriminator", {}).get("mapping", {}).values()
            ):
                keep(subtype.split("/")[-1])

    visit(paths)
    if "featureSpecs" in OMIT.get("BTFeatureSpecsResponse-664", []):
        # Keep a stub for featureSpecs, which is all we read
        kept["BTFeatureSpecsResponse-664"].setdefault("properties", {})[
            "featureSpecs"
        ] = {
            "type": "array",
            "items": {"$ref": "#/components/schemas/BTFeatureSpec-129"},
        }
        keep("BTFeatureSpec-129")

    return {
        "openapi": spec["openapi"],
        "info": {
            "title": spec["info"]["title"],
            "version": spec["info"]["version"],
            "description": f"Trimmed from {SPEC_URL} by onshape_api/reference/generate.py. Do not edit.",
        },
        "paths": {path: paths[path] for path in sorted(paths)},
        "components": {"schemas": {name: kept[name] for name in sorted(kept)}},
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument(
        "--spec",
        type=pathlib.Path,
        help="read the definition from a file instead of downloading it",
    )
    args = parser.parse_args()
    trimmed = trim(load_spec(args.spec))
    OUTPUT.write_text(json.dumps(trimmed, indent=2, sort_keys=False) + "\n")
    print(
        f"Wrote {len(trimmed['paths'])} paths and {len(trimmed['components']['schemas'])} schemas to {OUTPUT}."
    )


if __name__ == "__main__":
    main()
