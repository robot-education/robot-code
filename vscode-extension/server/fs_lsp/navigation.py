"""Document symbols (Outline, breadcrumbs, sticky scroll) and folding ranges."""

from __future__ import annotations

import bisect

from lsprotocol import types as lsp

from fs_lsp.parser import AstNode, ParsedProgram
from fs_lsp.scanner import Token

DOCUMENT_SYMBOL_NODE_TYPES = {
    "FeatureDeclaration": (lsp.SymbolKind.Function, "FeatureScript feature"),
    "FunctionDeclaration": (lsp.SymbolKind.Function, ""),
    "PredicateDeclaration": (lsp.SymbolKind.Function, "predicate"),
    "OperatorDeclaration": (lsp.SymbolKind.Operator, "operator overload"),
    "EnumDeclaration": (lsp.SymbolKind.Enum, "enum"),
    "TypeDeclaration": (lsp.SymbolKind.Struct, "type"),
    "TopLevelConst": (lsp.SymbolKind.Constant, "const"),
}

REGION_NODE_TYPES = frozenset(
    [
        "FeatureDeclaration",
        "FunctionDeclaration",
        "PredicateDeclaration",
        "OperatorDeclaration",
        "EnumDeclaration",
        "TopLevelConst",
    ]
)
FOLDING_NODE_TYPES = REGION_NODE_TYPES | {
    "PreconditionBlock",
    "Block",
    "MapLiteral",
    "AnnotationMap",
}


class TokenLookup:
    """Fast lookups of tokens by offset."""

    def __init__(self, parsed: ParsedProgram) -> None:
        self.tokens = [token for token in parsed.tokens if token.kind != "eof"]
        self.ends = [token.end for token in self.tokens]
        self.offsets = [token.offset for token in self.tokens]

    def last_ending_by(self, offset: int) -> Token | None:
        """The last token ending at or before offset."""
        index = bisect.bisect_right(self.ends, offset) - 1
        return self.tokens[index] if index >= 0 else None

    def find_in(self, node: AstNode, value: str) -> Token | None:
        """The first token inside node with the given value."""
        index = bisect.bisect_left(self.offsets, node.start)
        while index < len(self.tokens) and self.tokens[index].end <= node.end:
            if self.tokens[index].value == value:
                return self.tokens[index]
            index += 1
        return None


def token_range(start: Token, end: Token | None = None) -> lsp.Range:
    end = end or start
    return lsp.Range(
        lsp.Position(start.line, start.character),
        lsp.Position(end.end_line, end.end_character),
    )


def document_symbols(parsed: ParsedProgram) -> list[lsp.DocumentSymbol]:
    lookup = TokenLookup(parsed)
    symbols: list[lsp.DocumentSymbol] = []
    seen: set[tuple] = set()
    symbol_tokens = {
        (symbol.name, symbol.token.offset): symbol.token
        for symbol in parsed.symbols.values()
    }
    for node in parsed.nodes:
        if node.type not in DOCUMENT_SYMBOL_NODE_TYPES or not node.name:
            continue
        end = lookup.last_ending_by(node.end)
        if end is None:
            continue
        kind, detail = DOCUMENT_SYMBOL_NODE_TYPES[node.type]
        selection = _selection_token(parsed, lookup, node, symbol_tokens)
        symbol = lsp.DocumentSymbol(
            name=(
                f"operator{node.name}"
                if node.type == "OperatorDeclaration"
                else node.name
            ),
            detail=detail,
            kind=kind,
            range=token_range(node.token, end),
            selection_range=token_range(selection),
            children=[],
        )
        if node.type == "EnumDeclaration":
            symbol.children = [
                lsp.DocumentSymbol(
                    name=member.name or "",
                    detail="enum member",
                    kind=lsp.SymbolKind.EnumMember,
                    range=token_range(member.token),
                    selection_range=token_range(member.token),
                )
                for member in parsed.nodes
                if member.type == "EnumMember"
                and member.name
                and node.start <= member.start
                and member.end <= node.end
            ]
        key = (symbol.name, symbol.range.start.line, symbol.range.end.line, kind)
        if key not in seen:
            seen.add(key)
            symbols.append(symbol)
    return symbols


def _selection_token(
    parsed: ParsedProgram,
    lookup: TokenLookup,
    node: AstNode,
    symbol_tokens: dict[tuple[str, int], Token],
) -> Token:
    assert node.name
    if node.type != "OperatorDeclaration":
        for (name, offset), token in symbol_tokens.items():
            if name == node.name and node.start <= offset and token.end <= node.end:
                return token
    return lookup.find_in(node, node.name) or node.token


def folding_ranges(parsed: ParsedProgram) -> list[lsp.FoldingRange]:
    lookup = TokenLookup(parsed)
    ranges: list[lsp.FoldingRange] = []
    seen: set[tuple] = set()
    for node in parsed.nodes:
        if node.type not in FOLDING_NODE_TYPES:
            continue
        end = lookup.last_ending_by(node.end)
        if end is None or end.line <= node.token.line:
            continue
        key = (node.token.line, end.line, node.type)
        if key in seen:
            continue
        seen.add(key)
        ranges.append(
            lsp.FoldingRange(
                start_line=node.token.line,
                end_line=end.line,
                kind=(
                    lsp.FoldingRangeKind.Region
                    if node.type in REGION_NODE_TYPES
                    else None
                ),
            )
        )
    ranges.sort(key=lambda r: (r.start_line, -r.end_line))
    return ranges
