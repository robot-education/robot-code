"""Access to the generated Onshape standard library indexes in fs_lsp/data.

Regenerate them with `uv run python -m fs_lsp.tools.update_stdlib`.
"""

from __future__ import annotations

import dataclasses
import functools
import json
import pathlib
from typing import Literal

DATA_DIR = pathlib.Path(__file__).parent / "data"
SYMBOLS_PATH = DATA_DIR / "stdlib_symbols.json"
METADATA_PATH = DATA_DIR / "stdlib_metadata.json"

StdlibKind = Literal[
    "function",
    "predicate",
    "type",
    "enum",
    "enumMember",
    "constant",
    "unit",
    "unknown",
]

BUILT_IN_TYPES = frozenset(
    [
        "undefined",
        "boolean",
        "number",
        "string",
        "array",
        "map",
        "box",
        "builtin",
        "function",
    ]
)


@dataclasses.dataclass(frozen=True, slots=True)
class StdlibSymbol:
    name: str
    kind: StdlibKind
    module: str | None = None
    signature: str | None = None
    parent: str | None = None
    # Its doc comment (see fsdoc)
    doc: str | None = None


@dataclasses.dataclass(slots=True)
class EnumMember:
    name: str
    module: str | None = None
    doc: str | None = None


@dataclasses.dataclass(slots=True)
class FeatureField:
    """A key of a feature's definition map."""

    name: str
    source: Literal["docblock", "precondition", "predicate"]
    type: str | None = None
    label: str | None = None
    description: str | None = None
    required: bool | None = None
    predicate: str | None = None
    nestedPath: list[str] | None = None
    enumType: str | None = None
    defaultValue: str | None = None
    condition: str | None = None


@dataclasses.dataclass(slots=True)
class FeatureMetadata:
    name: str
    fields: list[FeatureField]
    module: str | None = None
    signature: str | None = None
    description: str | None = None


class StdlibIndex:
    def __init__(
        self,
        symbols: list[StdlibSymbol],
        enums: dict[str, list[EnumMember]],
        features: dict[str, FeatureMetadata],
    ) -> None:
        self.symbols = symbols
        self.by_name: dict[str, list[StdlibSymbol]] = {}
        self.enum_names: set[str] = set()
        for symbol in symbols:
            self.by_name.setdefault(symbol.name, []).append(symbol)
            if symbol.kind == "enum":
                self.enum_names.add(symbol.name)
        self.enums = enums
        self.features = features

    def lookup(self, name: str) -> list[StdlibSymbol]:
        return self.by_name.get(name, [])

    def choose(self, name: str, next_value: str | None) -> StdlibSymbol | None:
        """Picks the most plausible symbol named name, preferring callables before a "("."""
        return choose_symbol(self.lookup(name), next_value)


def choose_symbol(
    candidates: list[StdlibSymbol], next_value: str | None
) -> StdlibSymbol | None:
    if not candidates:
        return None
    callable_kinds = ("function", "predicate")
    if next_value == "(":
        preferred = (c for c in candidates if c.kind in callable_kinds)
    else:
        preferred = (c for c in candidates if c.kind not in callable_kinds)
    return next(preferred, candidates[0])


def built_in_type(name: str) -> StdlibSymbol | None:
    return StdlibSymbol(name, "type") if name in BUILT_IN_TYPES else None


@functools.cache
def stdlib() -> StdlibIndex:
    raw_symbols = json.loads(SYMBOLS_PATH.read_text())
    raw_metadata = json.loads(METADATA_PATH.read_text())
    symbols = [StdlibSymbol(**symbol) for symbol in raw_symbols]
    enums = {
        entry["name"]: [EnumMember(**member) for member in entry["members"]]
        for entry in raw_metadata["enums"]
    }
    features = {
        entry["name"]: FeatureMetadata(
            name=entry["name"],
            fields=[FeatureField(**field) for field in entry["fields"]],
            module=entry.get("module"),
            signature=entry.get("signature"),
            description=entry.get("description"),
        )
        for entry in raw_metadata["features"]
    }
    return StdlibIndex(symbols, enums, features)


def top_level_fields(feature: FeatureMetadata) -> list[FeatureField]:
    return [field for field in feature.fields if not field.nestedPath]


def dedupe_fields(fields: list[FeatureField]) -> list[FeatureField]:
    """Merges fields with the same name and path, keeping the first known value of each attribute."""
    merged: dict[tuple[str, str], FeatureField] = {}
    for field in fields:
        key = (".".join(field.nestedPath or []), field.name)
        existing = merged.get(key)
        if existing is None:
            merged[key] = field
            continue
        merged[key] = dataclasses.replace(
            field,
            type=existing.type if existing.type is not None else field.type,
            label=existing.label if existing.label is not None else field.label,
            description=(
                existing.description
                if existing.description is not None
                else field.description
            ),
            required=(
                existing.required if existing.required is not None else field.required
            ),
        )
    return [merged[key] for key in sorted(merged)]
