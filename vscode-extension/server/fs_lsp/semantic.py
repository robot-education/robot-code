"""Semantic token classification.

Combines the parser's explicit hints (declarations, map keys, ...) with contextual inference for
every other token (stdlib symbols, enum members, references to local declarations).
"""

from __future__ import annotations

import dataclasses

from fs_lsp.parser import ParsedProgram, SemanticHint, SemanticModifier, SemanticType
from fs_lsp.scanner import ASSIGNMENT_OPERATORS, Token, is_identifier_token
from fs_lsp.stdlib import StdlibSymbol, built_in_type, stdlib

TOKEN_TYPES: list[SemanticType] = [
    "namespace",
    "enum",
    "enumMember",
    "type",
    "function",
    "variable",
    "parameter",
    "property",
    "decorator",
    "keyword",
    "string",
    "number",
    "operator",
    "feature",
    "predicate",
    "annotationKey",
    "mapKey",
]

TOKEN_MODIFIERS: list[SemanticModifier] = [
    "declaration",
    "definition",
    "readonly",
    "modification",
    "documentation",
    "defaultLibrary",
]

_TYPE_INDEX = {name: index for index, name in enumerate(TOKEN_TYPES)}
_MODIFIER_BITS = {name: 1 << index for index, name in enumerate(TOKEN_MODIFIERS)}

LITERAL_KEYWORDS = frozenset(["false", "true", "undefined", "inf"])


@dataclasses.dataclass
class ImportedNames:
    """What a file can use from the files it imports (see fs_lsp.project).

    Attributes:
        names: The semantic type of each name, and whether it's readonly (a constant).
        enum_members: The members of each imported enum.
    """

    names: dict[str, tuple[SemanticType, bool]] = dataclasses.field(default_factory=dict)
    enum_members: dict[str, set[str]] = dataclasses.field(default_factory=dict)


@dataclasses.dataclass(slots=True)
class SemanticToken:
    token: Token
    type: SemanticType
    modifiers: tuple[SemanticModifier, ...]


def _unique(*modifiers: SemanticModifier) -> tuple[SemanticModifier, ...]:
    return tuple(dict.fromkeys(modifiers))


def build_semantic_tokens(
    parsed: ParsedProgram, imported: ImportedNames | None = None
) -> list[SemanticToken]:
    explicit: dict[int, SemanticHint] = {}
    for hint in parsed.hints:
        explicit[hint.token.offset] = _with_default_library(hint, parsed)

    built: list[SemanticToken] = []
    tokens = parsed.tokens
    for index, token in enumerate(tokens):
        if token.kind == "eof":
            continue
        hint = explicit.get(token.offset)
        if hint:
            built.append(_upgrade_explicit_hint(parsed, hint, index, imported or ImportedNames()))
            continue
        inferred = _infer(parsed, token, index, imported or ImportedNames())
        if inferred:
            built.append(inferred)
    return built


def encode(tokens: list[SemanticToken]) -> list[int]:
    """Encodes tokens in the LSP relative format. Multi-line tokens are skipped."""
    data: list[int] = []
    previous_line = previous_character = 0
    for entry in tokens:
        token = entry.token
        if token.line != token.end_line:
            continue
        delta_line = token.line - previous_line
        delta_character = (
            token.character - previous_character if delta_line == 0 else token.character
        )
        modifiers = 0
        for modifier in entry.modifiers:
            modifiers |= _MODIFIER_BITS[modifier]
        data.extend(
            (
                delta_line,
                delta_character,
                max(1, token.end_character - token.character),
                _TYPE_INDEX[entry.type],
                modifiers,
            )
        )
        previous_line, previous_character = token.line, token.character
    return data


def _previous(tokens: list[Token], index: int) -> Token | None:
    return tokens[index - 1] if index > 0 else None


def _next(tokens: list[Token], index: int) -> Token | None:
    next_index = index + 1
    if next_index < len(tokens) and tokens[next_index].kind != "eof":
        return tokens[next_index]
    return None


def _enum_member_token(
    parsed: ParsedProgram, token: Token, parent: str, extra: tuple
) -> SemanticToken | None:
    index = stdlib()
    if parent in parsed.enums or parent in index.enum_names:
        default_library = ("defaultLibrary",) if parent in index.enum_names else ()
        return SemanticToken(
            token, "enumMember", _unique("readonly", *default_library, *extra)
        )
    return None


def _infer(
    parsed: ParsedProgram, token: Token, index: int, imported: ImportedNames
) -> SemanticToken | None:
    kind = token.kind
    if kind == "number":
        return SemanticToken(token, "number", ())
    if kind == "string":
        return SemanticToken(token, "string", ())
    if kind == "operator":
        return SemanticToken(token, "operator", ())
    if kind == "keyword" and token.value not in LITERAL_KEYWORDS:
        return SemanticToken(token, "keyword", ())
    if not is_identifier_token(token):
        return None

    tokens = parsed.tokens
    previous = _previous(tokens, index)
    next_token = _next(tokens, index)
    next_value = next_token.value if next_token else None
    modifiers: tuple[SemanticModifier, ...] = (
        ("modification",) if next_value in ASSIGNMENT_OPERATORS else ()
    )

    if next_value == "::":
        return SemanticToken(token, "namespace", modifiers)

    if previous is not None and previous.value in (".", "?."):
        before_previous = _previous(tokens, index - 1)
        if before_previous is not None:
            member = _enum_member_token(parsed, token, before_previous.value, modifiers)
            if member:
                return member
            if token.value in imported.enum_members.get(before_previous.value, ()):
                return SemanticToken(token, "enumMember", _unique("readonly", *modifiers))
        return SemanticToken(token, "property", modifiers)

    if previous is not None and previous.value in ("is", "as", "returns"):
        return _type_token(token, modifiers, parsed)

    if token.value in parsed.parameters:
        return SemanticToken(token, "parameter", modifiers)

    variable = parsed.variables.get(token.value)
    if variable:
        readonly = ("readonly",) if variable.readonly else ()
        return SemanticToken(
            token,
            "feature" if variable.kind == "feature" else "variable",
            _unique(*readonly, *modifiers),
        )

    symbol = parsed.symbols.get(token.value)
    if symbol:
        readonly = ("readonly",) if symbol.readonly else ()
        return SemanticToken(
            token, _symbol_type(symbol.kind), _unique(*readonly, *modifiers)
        )

    if token.value in imported.names:
        type, readonly = imported.names[token.value]
        return SemanticToken(
            token, type, _unique(*(("readonly",) if readonly else ()), *modifiers)
        )

    if parsed.imports_stdlib:
        library_symbol = stdlib().choose(token.value, next_value)
    else:
        library_symbol = built_in_type(token.value)
    if library_symbol:
        return _stdlib_token(token, library_symbol, modifiers)

    if next_value == "(":
        return SemanticToken(token, "function", modifiers)
    return None


def _upgrade_explicit_hint(
    parsed: ParsedProgram, hint: SemanticHint, index: int, imported: ImportedNames
) -> SemanticToken:
    if hint.type == "property" and index >= 2:
        before_previous = parsed.tokens[index - 2]
        member = _enum_member_token(
            parsed, hint.token, before_previous.value, hint.modifiers
        )
        if member:
            return member
        if hint.token.value in imported.enum_members.get(before_previous.value, ()):
            return SemanticToken(hint.token, "enumMember", _unique("readonly", *hint.modifiers))
    return SemanticToken(hint.token, hint.type, hint.modifiers)


def _with_default_library(hint: SemanticHint, parsed: ParsedProgram) -> SemanticHint:
    if hint.type not in (
        "type",
        "predicate",
        "function",
        "enum",
        "enumMember",
        "variable",
    ):
        return hint
    name = hint.token.value
    built_in = built_in_type(name)
    if not parsed.imports_stdlib and not built_in:
        return hint
    candidates = stdlib().lookup(name)
    library_symbol = candidates[0] if candidates else built_in
    if not library_symbol:
        return hint
    readonly = (
        ("readonly",)
        if library_symbol.kind in ("enumMember", "constant", "unit")
        else ()
    )
    return SemanticHint(
        hint.token, hint.type, _unique(*hint.modifiers, "defaultLibrary", *readonly)
    )


def _type_token(
    token: Token, modifiers: tuple[SemanticModifier, ...], parsed: ParsedProgram
) -> SemanticToken:
    if parsed.imports_stdlib:
        is_library = any(
            symbol.kind == "type" for symbol in stdlib().lookup(token.value)
        )
    else:
        is_library = built_in_type(token.value) is not None
    default_library = ("defaultLibrary",) if is_library else ()
    return SemanticToken(token, "type", _unique(*modifiers, *default_library))


def _stdlib_token(
    token: Token, symbol: StdlibSymbol, modifiers: tuple[SemanticModifier, ...]
) -> SemanticToken:
    kind = symbol.kind
    if kind in ("function", "predicate", "type", "enum"):
        return SemanticToken(token, kind, _unique(*modifiers, "defaultLibrary"))
    if kind == "enumMember":
        return SemanticToken(
            token, "enumMember", _unique(*modifiers, "readonly", "defaultLibrary")
        )
    if kind in ("constant", "unit"):
        return SemanticToken(
            token, "variable", _unique(*modifiers, "readonly", "defaultLibrary")
        )
    return SemanticToken(token, "variable", _unique(*modifiers, "defaultLibrary"))


def _symbol_type(kind: str) -> SemanticType:
    if kind in (
        "feature",
        "predicate",
        "enum",
        "enumMember",
        "type",
        "function",
        "parameter",
    ):
        return kind  # type: ignore[return-value]
    return "variable"
