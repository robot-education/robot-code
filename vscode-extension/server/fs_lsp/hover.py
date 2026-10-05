"""Hover tooltips for local declarations and standard library symbols."""

from __future__ import annotations

from fs_lsp.completion import enum_members_for, escape_markdown, feature_fields_for
from fs_lsp.doc_comments import leading_doc_comment
from fs_lsp.parser import ParsedProgram
from fs_lsp.scanner import Token
from fs_lsp.stdlib import (
    EnumMember,
    FeatureField,
    StdlibSymbol,
    built_in_type,
    choose_symbol,
    stdlib,
    top_level_fields,
)
from fs_lsp.symbol_index import Declaration, SymbolIndex

MAX_ENUM_MEMBERS = 80
MAX_FEATURE_FIELDS = 24

KIND_LABELS = {"definitionProperty": "feature parameter", "enumMember": "enum member"}


def hover_markdown(
    parsed: ParsedProgram, index: SymbolIndex, offset: int
) -> tuple[str, Token] | None:
    """Returns the hover contents (as markdown) and the token being hovered."""
    token = index.token_at(offset)
    if token is None or token.offset in index.ignored_offsets:
        return None

    local = index.declaration_for_token(token)
    if local:
        return declaration_markdown(parsed, local), token

    symbol = _stdlib_symbol(parsed, index, token)
    if symbol:
        return _stdlib_markdown(symbol), token
    return None


def _code_block(code: str) -> str:
    return f"```featurescript\n{code}\n```"


def declaration_markdown(parsed: ParsedProgram, declaration: Declaration) -> str:
    lines = parsed.source.split("\n")
    sections = [
        f"**FeatureScript {KIND_LABELS.get(declaration.kind, declaration.kind)}**",
        _code_block(_local_signature(lines, declaration)),
    ]
    doc = leading_doc_comment(lines, declaration.token.line)
    if doc:
        sections.append(doc)
    if declaration.kind == "enum":
        sections.append(
            _enum_members(declaration.name, enum_members_for(parsed, declaration.name))
        )
    if declaration.kind == "feature":
        sections.append(_feature_fields(feature_fields_for(parsed, declaration.name)))
    return "\n\n".join(section for section in sections if section)


def _stdlib_markdown(symbol: StdlibSymbol) -> str:
    index = stdlib()
    feature = index.features.get(symbol.name)
    signature = symbol.signature or (
        f"{symbol.parent}.{symbol.name}" if symbol.parent else symbol.name
    )
    sections = [
        f"**FeatureScript stdlib {'feature' if feature else symbol.kind}**",
        _code_block(signature),
    ]
    if feature and feature.description:
        sections.append(feature.description)
    details = []
    if symbol.parent:
        details.append(f"Parent: {symbol.parent}")
    if symbol.module:
        details.append(f"Module: {symbol.module}")
    if details:
        sections.append("  \n".join(escape_markdown(detail) for detail in details))
    if symbol.kind == "enum":
        sections.append(_enum_members(symbol.name, index.enums.get(symbol.name, [])))
    if feature:
        sections.append(_feature_fields(top_level_fields(feature)))
    return "\n\n".join(section for section in sections if section)


def _local_signature(lines: list[str], declaration: Declaration) -> str:
    if declaration.kind == "definitionProperty":
        return f"definition.{declaration.name}"
    if declaration.kind == "enumMember" and declaration.parent:
        return f"{declaration.parent}.{declaration.name}"
    line_number = declaration.token.line
    line = lines[line_number].strip() if line_number < len(lines) else ""
    return line or declaration.name


def _stdlib_symbol(
    parsed: ParsedProgram, index: SymbolIndex, token: Token
) -> StdlibSymbol | None:
    library = stdlib()
    previous = index.previous_token(token)
    next_token = index.next_token(token)
    next_value = next_token.value if next_token else None
    if previous is not None and previous.value in (".", "?."):
        parent = index.previous_token(previous)
        if parent is not None and parent.value in library.enum_names:
            return choose_symbol(
                [
                    symbol
                    for symbol in library.lookup(token.value)
                    if symbol.parent == parent.value
                ],
                next_value,
            )
    if not parsed.imports_stdlib and not built_in_type(token.value):
        return None
    return library.choose(token.value, next_value) or built_in_type(token.value)


def _enum_members(enum_name: str, members: list[EnumMember]) -> str | None:
    if not members:
        return None
    shown = members[:MAX_ENUM_MEMBERS]
    lines = [f"**{escape_markdown(enum_name)} variants ({len(members)})**", ""]
    lines.extend(f"- `{member.name}`" for member in shown)
    if len(members) > len(shown):
        lines.append(f"- ... {len(members) - len(shown)} more")
    return "\n".join(lines)


def _feature_fields(fields: list[FeatureField]) -> str | None:
    if not fields:
        return None
    shown = fields[:MAX_FEATURE_FIELDS]
    lines = [f"**Definition fields ({len(fields)})**", ""]
    for field in shown:
        type = f": `{field.type}`" if field.type else ""
        label = f" - {escape_markdown(field.label)}" if field.label else ""
        source = f" ({escape_markdown(field.predicate)})" if field.predicate else ""
        lines.append(f"- `{field.name}`{type}{label}{source}")
    if len(fields) > len(shown):
        lines.append(f"- ... {len(fields) - len(shown)} more")
    return "\n".join(lines)
