"""Regenerates the language server's stdlib indexes (fs_lsp/data/stdlib_symbols.json and
stdlib_metadata.json) from the Onshape std library checked in at std/.

    uv run python -m fs_lsp.tools.update_stdlib

`fs pull-std` updates std/ and runs this automatically.

Ported from gatrall/featurescript-language-support's scripts/update-stdlib-symbols.ts (MIT).
"""

from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import re

from fs_lsp.doc_comments import leading_doc_comment
from fs_lsp.stdlib import METADATA_PATH, SYMBOLS_PATH

UNIT_NAMES = frozenset(
    [
        "unitless",
        "meter",
        "centimeter",
        "millimeter",
        "micrometer",
        "nanometer",
        "inch",
        "foot",
        "yard",
        "degree",
        "radian",
        "second",
        "minute",
        "hour",
        "kilogram",
        "gram",
        "newton",
        "pound",
        "pascal",
        "psi",
    ]
)

VALIDATOR_TYPES = {
    "isLength": "ValueWithUnits",
    "isAngle": "ValueWithUnits",
    "isInteger": "number",
    "isReal": "number",
    "isRealInRange": "number",
    "isNonNegativeInteger": "number",
    "isPositiveInteger": "number",
}

NON_ENUM_TYPES = frozenset(
    [
        "Context",
        "Id",
        "Query",
        "Vector",
        "Transform",
        "Line",
        "Plane",
        "ValueWithUnits",
        "LengthBoundSpec",
        "PartStudioData",
        "boolean",
        "number",
        "string",
        "array",
        "map",
        "box",
        "function",
    ]
)

MANUAL_BUILTINS = [
    {"name": name, "kind": "type"}
    for name in [
        "undefined",
        "boolean",
        "number",
        "string",
        "array",
        "map",
        "box",
        "builtin",
        "function",
        "Context",
        "Id",
        "Query",
        "Vector",
        "Transform",
        "Line",
        "Plane",
        "ValueWithUnits",
        "LengthBoundSpec",
    ]
]

# Symbols the tests rely on, in case they're ever missing from the std
MANUAL_REQUIRED_SYMBOLS = [
    {"name": "defineFeature", "kind": "function", "module": "feature.fs"},
    {"name": "opExtrude", "kind": "function", "module": "extrude.fs"},
    {"name": "evOwnerSketchPlane", "kind": "function", "module": "evaluate.fs"},
    {"name": "qCreatedBy", "kind": "function", "module": "query.fs"},
    {"name": "isLength", "kind": "predicate", "module": "valueBounds.fs"},
    {"name": "PI", "kind": "constant", "module": "math.fs"},
    {"name": "LENGTH_BOUNDS", "kind": "constant", "module": "valueBounds.fs"},
    {"name": "inch", "kind": "unit", "module": "units.fs"},
    {"name": "meter", "kind": "unit", "module": "units.fs"},
    {"name": "EntityType", "kind": "enum", "module": "entitytype.gen.fs"},
    {
        "name": "EDGE",
        "kind": "enumMember",
        "module": "entitytype.gen.fs",
        "parent": "EntityType",
    },
    {"name": "BodyType", "kind": "enum", "module": "bodytype.gen.fs"},
    {"name": "BoundingType", "kind": "enum", "module": "boundingtype.gen.fs"},
    {
        "name": "THROUGH_ALL",
        "kind": "enumMember",
        "module": "boundingtype.gen.fs",
        "parent": "BoundingType",
    },
]

IDENTIFIER = r"[A-Za-z_][A-Za-z0-9_]*"
ENUM_TYPE_NAME = re.compile(r"^[A-Z][A-Za-z0-9_]*$")


@dataclasses.dataclass
class SourceFile:
    module: str
    text: str


# Symbols


def read_signature(lines: list[str], start: int) -> str:
    parts = []
    depth = 0
    for line in lines[start : start + 12]:
        before_body = line.split("{", 1)[0]
        parts.append(before_body.strip())
        depth += before_body.count("(") - before_body.count(")")
        if depth <= 0 and ")" in before_body:
            break
    return re.sub(r"\s+", " ", " ".join(parts)).strip()


def const_kind(name: str, module: str, line: str) -> str:
    if name == "defineFeature" or re.search(
        r"=\s*(?:function\b|defineFeature\s*\()", line
    ):
        return "function"
    if name in UNIT_NAMES:
        return "unit"
    if module.endswith("units.fs") and re.fullmatch(r"[a-z][A-Za-z0-9_]*", name):
        return "unit"
    return "constant"


def extract_symbols(text: str, module: str) -> list[dict]:
    symbols: list[dict] = []
    lines = re.split(r"\r?\n", text)
    index = 0
    while index < len(lines):
        line = lines[index]
        if match := re.match(rf"^\s*export\s+function\s+({IDENTIFIER})\s*\(", line):
            symbols.append(
                _symbol(match[1], "function", module, read_signature(lines, index), index)
            )
        elif match := re.match(rf"^\s*export\s+predicate\s+({IDENTIFIER})\s*\(", line):
            symbols.append(
                _symbol(match[1], "predicate", module, read_signature(lines, index), index)
            )
        elif match := re.match(rf"^\s*export\s+type\s+({IDENTIFIER})\b", line):
            symbols.append(_symbol(match[1], "type", module, line.strip(), index))
        elif match := re.match(rf"^\s*export\s+const\s+({IDENTIFIER})\b", line):
            name = match[1]
            symbols.append(
                _symbol(name, const_kind(name, module, line), module, line.strip(), index)
            )
        elif match := re.match(rf"^\s*export\s+enum\s+({IDENTIFIER})\b", line):
            parent = match[1]
            symbols.append(_symbol(parent, "enum", module, line.strip(), index))
            body = index + 1
            while body < len(lines) and not re.match(r"^\s*\{", lines[body]):
                body += 1
            body += 1
            while body < len(lines) and not re.match(r"^\s*\}", lines[body]):
                member = re.match(r"^\s*([A-Z][A-Z0-9_]*)\s*,?", lines[body])
                if member and member[1] != "annotation":
                    symbols.append(
                        {
                            "name": member[1],
                            "kind": "enumMember",
                            "module": module,
                            "parent": parent,
                        }
                    )
                body += 1
        index += 1
    for symbol in symbols:
        if symbol["kind"] != "enumMember" and "line" in symbol:
            if doc := leading_doc_comment(lines, symbol["line"]):
                symbol["doc"] = doc
        symbol.pop("line", None)
    return symbols


def _symbol(name: str, kind: str, module: str, signature: str, line: int | None = None) -> dict:
    symbol = {"name": name, "kind": kind, "module": module, "signature": signature}
    if line is not None:
        # Where its doc comment is found, then dropped
        symbol["line"] = line
    return symbol


def dedupe_symbols(symbols: list[dict]) -> list[dict]:
    merged: dict[tuple, dict] = {}
    for symbol in symbols:
        key = (symbol.get("parent", ""), symbol["name"], symbol["kind"])
        existing = merged.get(key)
        if (
            existing is None
            or ("signature" not in existing and "signature" in symbol)
            or ("module" not in existing and "module" in symbol)
        ):
            merged[key] = symbol
    return [merged[key] for key in sorted(merged)]


# Metadata


def extract_metadata(files: list[SourceFile], symbols: list[dict]) -> dict:
    enums: dict[str, dict] = {}
    for symbol in symbols:
        if symbol["kind"] == "enum":
            enums[symbol["name"]] = _with_module(
                {"name": symbol["name"]}, symbol.get("module")
            ) | {"members": []}
    for symbol in symbols:
        if symbol["kind"] != "enumMember" or "parent" not in symbol:
            continue
        parent = enums.setdefault(
            symbol["parent"],
            _with_module({"name": symbol["parent"]}, symbol.get("module"))
            | {"members": []},
        )
        if not any(member["name"] == symbol["name"] for member in parent["members"]):
            parent["members"].append(
                _with_module({"name": symbol["name"]}, symbol.get("module"))
            )

    predicate_fields: dict[str, list[dict]] = {}
    for file in files:
        for name, parameter, body in extract_predicate_blocks(file.text):
            predicate_fields[name] = dedupe_fields(
                extract_fields_from_block(body, [parameter], source="precondition")
            )

    features = []
    for file in files:
        for feature in extract_feature_blocks(file.text, file.module):
            doc_fields = (
                extract_fields_from_doc_comment(feature["doc"])
                if feature.get("doc")
                else []
            )
            precondition = feature.get("precondition", "")
            fields = dedupe_fields(
                doc_fields
                + extract_fields_from_block(
                    precondition, ["definition"], source="precondition"
                )
                + inline_predicate_fields(precondition, predicate_fields)
            )
            entry = {
                "name": feature["name"],
                "module": file.module,
                "signature": feature["signature"],
            }
            if feature.get("description"):
                entry["description"] = feature["description"]
            entry["fields"] = fields
            features.append(entry)

    # The std modules each module re-exports with `export import`
    exports = {}
    for file in files:
        reexported = sorted(set(STD_EXPORT_IMPORT.findall(file.text)))
        if reexported:
            exports[file.module] = reexported

    return {
        "enums": [
            entry | {"members": sorted(entry["members"], key=lambda m: m["name"])}
            for entry in sorted(enums.values(), key=lambda e: e["name"])
        ],
        "features": sorted(features, key=lambda f: f["name"]),
        "exports": dict(sorted(exports.items())),
    }


STD_EXPORT_IMPORT = re.compile(r'\bexport\s+import\s*\(\s*path\s*:\s*"onshape/std/([^"]+)"')


def _with_module(entry: dict, module: str | None) -> dict:
    if module:
        entry["module"] = module
    return entry


def extract_fields_from_doc_comment(comment: str) -> list[dict]:
    fields = []
    current: dict | None = None
    for raw_line in clean_block_comment(comment).splitlines():
        line = raw_line.strip()
        match = re.match(
            rf"^@field\s+({IDENTIFIER})\s+\{{([^}}]+)\}}\s*:?\s*(.*)$", line
        )
        if match:
            if current:
                fields.append(clean_field(current))
            rest = match[3] or ""
            current = {
                "name": match[1],
                "type": match[2].strip(),
                "source": "docblock",
                "required": not re.search(r"@optional\b", rest),
            }
            if (
                ENUM_TYPE_NAME.match(current["type"])
                and current["type"] not in NON_ENUM_TYPES
            ):
                current["enumType"] = current["type"]
            if condition := re.search(r"@requiredif\s+\{([^}]+)\}", rest):
                current["condition"] = condition[1].replace("`", "").strip()
            if description := clean_doc_field_description(rest):
                current["description"] = description
            continue
        if not current or line.startswith("@field") or line.startswith("@param"):
            continue
        if description := clean_doc_field_description(line):
            current["description"] = " ".join(
                part for part in [current.get("description"), description] if part
            )
    if current:
        fields.append(clean_field(current))
    return dedupe_fields(fields)


def extract_predicate_blocks(text: str) -> list[tuple[str, str, str]]:
    predicates = []
    pattern = re.compile(
        rf"\b(?:export\s+)?predicate\s+({IDENTIFIER})\s*\(\s*({IDENTIFIER})\b"
    )
    for match in pattern.finditer(text):
        body_open = text.find("{", match.start())
        body_close = find_matching_brace(text, body_open) if body_open >= 0 else -1
        if body_open >= 0 and body_close > body_open:
            predicates.append((match[1], match[2], text[body_open + 1 : body_close]))
    return predicates


def extract_feature_blocks(text: str, module: str) -> list[dict]:
    features = []
    lines = re.split(r"\r?\n", text)
    pattern = re.compile(
        rf"export\s+const\s+({IDENTIFIER})\s*=\s*defineFeature\s*\(\s*function\s*\("
    )
    for match in pattern.finditer(text):
        start_line = text.count("\n", 0, match.start())
        feature = {
            "name": match[1],
            "module": module,
            "signature": read_signature(lines, start_line),
        }
        if doc := leading_doc_block(text, match.start()):
            feature["doc"] = doc
            feature["description"] = doc_summary(doc)
        if precondition := extract_precondition_block(text, match.start()):
            feature["precondition"] = precondition
        features.append(feature)
    return features


def extract_precondition_block(text: str, start: int) -> str | None:
    precondition = text.find("precondition", start)
    if precondition < 0:
        return None
    body_open = text.find("{", precondition)
    body_close = find_matching_brace(text, body_open) if body_open >= 0 else -1
    if body_open < 0 or body_close <= body_open:
        return None
    return text[body_open + 1 : body_close]


def extract_fields_from_block(
    block: str,
    root_names: list[str],
    source: str,
    predicate: str | None = None,
    nested_path: list[str] | None = None,
) -> list[dict]:
    if not block or not root_names:
        return []
    roots = "|".join(re.escape(name) for name in root_names)
    options = {"source": source, "predicate": predicate, "nested_path": nested_path}
    fields = []
    for match in re.finditer(
        rf"\b(?:{roots})\.({IDENTIFIER})\s+is\s+({IDENTIFIER})", block
    ):
        fields.append(
            field_from_match(block, match.start(), match[1], match[2], **options)
        )
    for match in re.finditer(
        rf"\b({IDENTIFIER})\s*\(\s*(?:{roots})\.({IDENTIFIER})\s*,", block
    ):
        if match[1] in VALIDATOR_TYPES:
            fields.append(
                field_from_match(
                    block, match.start(), match[2], VALIDATOR_TYPES[match[1]], **options
                )
            )
    if "definition" in root_names:
        fields.extend(extract_nested_loop_fields(block, source, predicate))
    return dedupe_fields(fields)


def extract_nested_loop_fields(
    block: str, source: str, predicate: str | None
) -> list[dict]:
    fields = []
    pattern = rf"for\s*\(\s*var\s+({IDENTIFIER})\s+in\s+definition\.({IDENTIFIER})\s*\)"
    for match in re.finditer(pattern, block):
        body_open = block.find("{", match.start())
        body_close = find_matching_brace(block, body_open) if body_open >= 0 else -1
        if body_open < 0 or body_close <= body_open:
            continue
        fields.extend(
            extract_fields_from_block(
                block[body_open + 1 : body_close],
                [match[1]],
                source=source,
                predicate=predicate,
                nested_path=[match[2]],
            )
        )
    return fields


def inline_predicate_fields(
    block: str, predicate_fields: dict[str, list[dict]]
) -> list[dict]:
    fields = []
    seen = set()
    for match in re.finditer(rf"\b({IDENTIFIER})\s*\(\s*definition\s*\)", block):
        predicate = match[1]
        if predicate in seen:
            continue
        seen.add(predicate)
        for field in predicate_fields.get(predicate, []):
            fields.append(
                clean_field(field | {"source": "predicate", "predicate": predicate})
            )
    return fields


def field_from_match(
    block: str,
    index: int,
    name: str,
    type: str | None,
    source: str,
    predicate: str | None = None,
    nested_path: list[str] | None = None,
) -> dict:
    field: dict = {"name": name, "source": source}
    if type:
        field["type"] = type
    field |= annotation_before(block, index)
    if predicate:
        field["predicate"] = predicate
    if nested_path:
        field["nestedPath"] = nested_path
    if type and ENUM_TYPE_NAME.match(type) and type not in NON_ENUM_TYPES:
        field["enumType"] = type
    return clean_field(field)


def annotation_before(block: str, offset: int) -> dict:
    window_start = max(0, offset - 1200)
    annotation = block.rfind("annotation", window_start, offset)
    if annotation < 0:
        return {}
    open_brace = block.find("{", annotation)
    if open_brace < 0 or open_brace > offset:
        return {}
    close_brace = find_matching_brace(block, open_brace)
    if close_brace < open_brace or close_brace > offset:
        return {}
    if re.search(
        r"\b(?:definition|isLength|isAngle|isInteger|isReal|annotation)\b",
        block[close_brace + 1 : offset],
    ):
        return {}
    body = block[open_brace + 1 : close_brace]
    result = {}
    if match := re.search(r"[\"']Name[\"']\s*:\s*[\"']([^\"']+)[\"']", body):
        result["label"] = match[1]
    if match := re.search(r"[\"']Description[\"']\s*:\s*[\"']([^\"']+)[\"']", body):
        result["description"] = match[1]
    if match := re.search(r"[\"']Default[\"']\s*:\s*([^,}\n]+)", body):
        result["defaultValue"] = match[1].strip()
    return result


def clean_block_comment(comment: str) -> str:
    comment = re.sub(r"^/\*\*?", "", comment)
    comment = re.sub(r"\*/$", "", comment)
    return "\n".join(
        re.sub(r"^\s*\*\s?", "", line).rstrip() for line in re.split(r"\r?\n", comment)
    ).strip()


def doc_summary(comment: str) -> str | None:
    paragraph = []
    for line in clean_block_comment(comment).splitlines():
        line = line.strip()
        if not line:
            continue
        if line.startswith("@"):
            break
        paragraph.append(line)
    return re.sub(r"\s+", " ", " ".join(paragraph)).strip() or None


def clean_doc_field_description(text: str) -> str:
    text = re.sub(r"@optional\b", "", text)
    text = re.sub(r"@requiredif\s+\{[^}]+\}", "", text)
    text = re.sub(r"@ex\b", "", text)
    text = text.replace("`", "")
    text = re.sub(r"\s+", " ", text).strip()
    return re.sub(r"^:\s*", "", text)


FIELD_ORDER = [
    "name",
    "source",
    "type",
    "label",
    "description",
    "required",
    "predicate",
    "nestedPath",
    "enumType",
    "defaultValue",
    "condition",
]


def clean_field(field: dict) -> dict:
    cleaned = {}
    for key in FIELD_ORDER:
        value = field.get(key)
        if key == "description" and value:
            value = re.sub(r"\s+", " ", value).strip()
        if key == "required" and value is not None:
            cleaned[key] = value
        elif value:
            cleaned[key] = value
    return cleaned


def dedupe_fields(fields: list[dict]) -> list[dict]:
    merged: dict[tuple[str, str], dict] = {}
    for field in fields:
        key = (".".join(field.get("nestedPath", [])), field["name"])
        existing = merged.get(key)
        if existing is None:
            merged[key] = clean_field(field)
            continue
        combined = existing | field
        for attribute in ("description", "label", "type", "required"):
            if existing.get(attribute) is not None:
                combined[attribute] = existing[attribute]
        merged[key] = clean_field(combined)
    return [merged[key] for key in sorted(merged)]


def leading_doc_block(text: str, offset: int) -> str | None:
    start = text.rfind("/**", 0, offset)
    if start < 0:
        return None
    end = text.find("*/", start)
    if end < 0 or end + 2 > offset:
        return None
    if re.search(
        r"\bexport\s+(?:const|function|predicate|enum|type)\b", text[end + 2 : offset]
    ):
        return None
    return text[start : end + 2]


def find_matching_brace(text: str, open_offset: int) -> int:
    depth = 0
    quote = None
    index = open_offset
    while index < len(text):
        char = text[index]
        if quote:
            if char == "\\":
                index += 1
            elif char == quote:
                quote = None
        elif char in "\"'":
            quote = char
        elif text.startswith("//", index):
            newline = text.find("\n", index + 2)
            index = len(text) if newline < 0 else newline
        elif text.startswith("/*", index):
            close = text.find("*/", index + 2)
            index = len(text) if close < 0 else close + 1
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return index
        index += 1
    return -1


# Sources


REPO_ROOT = pathlib.Path(__file__).resolve().parents[4]
STD_DIR = REPO_ROOT / "std"


def read_source_dir(root: pathlib.Path) -> list[SourceFile]:
    return [
        SourceFile(path.relative_to(root).as_posix(), path.read_text())
        for path in sorted(root.rglob("*.fs"))
    ]


def generate(files: list[SourceFile]) -> tuple[list[dict], dict]:
    symbols = [*MANUAL_BUILTINS, *MANUAL_REQUIRED_SYMBOLS]
    for file in files:
        symbols.extend(extract_symbols(file.text, file.module))
    symbols = dedupe_symbols(symbols)
    return symbols, extract_metadata(files, symbols)


def regenerate(std_dir: pathlib.Path = STD_DIR) -> str:
    """Regenerates the indexes from std_dir. Returns a summary of what was written."""
    files = read_source_dir(std_dir)
    if not files:
        raise ValueError(f"No std .fs files were found in {std_dir}.")
    symbols, metadata = generate(files)
    SYMBOLS_PATH.write_text(json.dumps(symbols, indent=2, ensure_ascii=False) + "\n")
    METADATA_PATH.write_text(json.dumps(metadata, indent=2, ensure_ascii=False) + "\n")
    return f"{len(symbols)} symbols, {len(metadata['enums'])} enums, and {len(metadata['features'])} features"


def main() -> None:
    argparse.ArgumentParser(description=__doc__.split("\n\n")[0]).parse_args()
    print(f"Wrote {regenerate()}.")


if __name__ == "__main__":
    main()
