"""Tokenizes FeatureScript source.

Token positions are tracked two ways: `offset`/`end` index into the Python string, while
`line`/`character` follow the LSP convention of UTF-16 code units so they can be sent to the
client as-is.
"""

from __future__ import annotations

import bisect
import re
from typing import Literal

TokenKind = Literal[
    "identifier",
    "atIdentifier",
    "keyword",
    "number",
    "string",
    "operator",
    "punctuation",
    "invalid",
    "eof",
]

KEYWORDS = frozenset(
    [
        "FeatureScript",
        "annotation",
        "enum",
        "export",
        "function",
        "import",
        "operator",
        "precondition",
        "predicate",
        "returns",
        "type",
        "typecheck",
        "typeconvert",
        "as",
        "is",
        "new",
        "if",
        "else",
        "break",
        "const",
        "continue",
        "for",
        "in",
        "return",
        "var",
        "while",
        "try",
        "catch",
        "throw",
        "false",
        "true",
        "undefined",
        "inf",
        "assert",
        "case",
        "default",
        "do",
        "switch",
    ]
)

ASSIGNMENT_OPERATORS = frozenset(
    ["=", "+=", "-=", "*=", "/=", "^=", "%=", "||=", "&&=", "??=", "~="]
)

# Order matters: earlier entries win when several match
MULTI_CHAR_OPERATORS = [
    "?[]",
    "??=",
    "||=",
    "&&=",
    "?.",
    "?[",
    "::",
    "=>",
    "->",
    "<=",
    ">=",
    "==",
    "!=",
    "+=",
    "-=",
    "*=",
    "/=",
    "^=",
    "%=",
    "~=",
    "??",
    "&&",
    "||",
    "++",
    "--",
]
SINGLE_CHAR_OPERATORS = "+-*/%^~<>!=?:"
PUNCTUATION = "{}()[],;."


class Token:
    __slots__ = (
        "kind",
        "value",
        "offset",
        "end",
        "line",
        "character",
        "end_line",
        "end_character",
    )

    def __init__(
        self,
        kind: TokenKind,
        value: str,
        offset: int,
        end: int,
        line: int,
        character: int,
        end_line: int,
        end_character: int,
    ) -> None:
        self.kind = kind
        self.value = value
        self.offset = offset
        self.end = end
        self.line = line
        self.character = character
        self.end_line = end_line
        self.end_character = end_character

    def __repr__(self) -> str:
        return f"Token({self.kind}, {self.value!r}, {self.line}:{self.character})"


def is_identifier_token(token: Token | None) -> bool:
    return token is not None and token.kind in ("identifier", "keyword")


_TOKEN_PATTERN = re.compile(
    r"""
    (?P<space>[ \t\r\n]+)
    | (?P<line_comment>//[^\n]*)
    | (?P<block_comment>/\*.*?(?:\*/|\Z))
    | (?P<string>"(?:[^"\\]|\\.)*(?:"|\Z)|'(?:[^'\\]|\\.)*(?:'|\Z))
    | (?P<number>(?:\d+|\.\d+)(?:\.\d+|\.)?(?:[eE][+-]?\d+)?)
    | (?P<at_identifier>@[A-Za-z_][A-Za-z0-9_]*)
    | (?P<identifier>[A-Za-z_][A-Za-z0-9_]*)
    | (?P<operator>"""
    + "|".join(re.escape(op) for op in MULTI_CHAR_OPERATORS)
    + "|["
    + re.escape(SINGLE_CHAR_OPERATORS)
    + r"""])
    | (?P<punctuation>["""
    + re.escape(PUNCTUATION)
    + r"""])
    | (?P<invalid>.)
    """,
    re.VERBOSE | re.DOTALL,
)


class LineMap:
    """Converts string offsets into (line, UTF-16 character) positions."""

    def __init__(self, source: str) -> None:
        self.source = source
        self.line_starts = [0]
        self.line_starts.extend(match.end() for match in re.finditer("\n", source))
        self.ascii = source.isascii()

    def position(self, offset: int) -> tuple[int, int]:
        line = bisect.bisect_right(self.line_starts, offset) - 1
        start = self.line_starts[line]
        if self.ascii:
            return line, offset - start
        text = self.source[start:offset]
        return line, len(text.encode("utf-16-le")) // 2

    def offset(self, line: int, character: int) -> int:
        """The inverse of position."""
        line = min(max(line, 0), len(self.line_starts) - 1)
        start = self.line_starts[line]
        if self.ascii:
            return min(start + character, len(self.source))
        units = 0
        offset = start
        while offset < len(self.source) and units < character:
            units += 2 if ord(self.source[offset]) > 0xFFFF else 1
            offset += 1
        return offset


class ScanResult:
    """
    Attributes:
        tokens: The tokens, ending with an eof token. Comments and whitespace are dropped.
        line_map: Converts offsets in the source to positions.
        unterminated: The (start, end) offsets of unterminated strings and block comments.
    """

    def __init__(
        self,
        tokens: list[Token],
        line_map: LineMap,
        unterminated: list[tuple[int, int, str]],
    ) -> None:
        self.tokens = tokens
        self.line_map = line_map
        self.unterminated = unterminated


def scan(source: str) -> ScanResult:
    line_map = LineMap(source)
    tokens: list[Token] = []
    unterminated: list[tuple[int, int, str]] = []
    for match in _TOKEN_PATTERN.finditer(source):
        group = match.lastgroup
        value = match.group()
        if group == "block_comment":
            if not value.endswith("*/") or len(value) < 4:
                unterminated.append((match.start(), match.end(), "comment"))
            continue
        if group in ("space", "line_comment"):
            continue
        if group == "string" and (
            len(value) < 2 or value[-1] != value[0] or _escaped_end(value)
        ):
            unterminated.append((match.start(), match.end(), "string"))
        if group == "at_identifier":
            kind: TokenKind = "atIdentifier"
        elif group == "identifier":
            kind = "keyword" if value in KEYWORDS else "identifier"
        elif group == "operator":
            kind = "invalid" if value in ("++", "--") else "operator"
        else:
            kind = group  # type: ignore[assignment]
        tokens.append(_make_token(line_map, kind, value, match.start(), match.end()))
    tokens.append(_make_token(line_map, "eof", "", len(source), len(source)))
    return ScanResult(tokens, line_map, unterminated)


def _escaped_end(value: str) -> bool:
    """True if a string token's final quote is escaped (and thus doesn't close it)."""
    backslashes = len(value) - 1 - len(value[:-1].rstrip("\\"))
    return backslashes % 2 == 1


def _make_token(
    line_map: LineMap, kind: TokenKind, value: str, start: int, end: int
) -> Token:
    line, character = line_map.position(start)
    if "\n" in value:
        end_line, end_character = line_map.position(end)
    else:
        end_line = line
        end_character = (
            character + end - start
            if value.isascii()
            else character + len(value.encode("utf-16-le")) // 2
        )
    return Token(kind, value, start, end, line, character, end_line, end_character)
