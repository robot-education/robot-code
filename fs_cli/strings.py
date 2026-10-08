"""The strings a FeatureScript shows its users, for auditing their wording (`fs strings`).

Found in the source, not by running it: annotations' names and descriptions, errors, warnings and info messages, the
messages of empty selections, and lookup tables' level names. A string built with `~` is shown as written, e.g.
`"The " ~ name ~ " has no length."`.
"""

from __future__ import annotations

import dataclasses
import re
from collections.abc import Iterable

from fs_lsp.scanner import Token, scan

# Annotation keys whose values are shown, by what they're called in the output
ANNOTATION_KEYS = {
    '"Feature Type Name"': "feature name",
    '"Feature Type Description"': "feature description",
    '"Name"': "name",
    '"Description"': "description",
    '"Group Name"': "group",
    '"Item name"': "array item name",
    '"Item label template"': "array item label",
    '"Column Name"': "column name",
}

# Calls whose arguments are shown, by the argument's index, and what they're called in the output
CALLS = {
    "regenError": (0, "error"),
    "reportFeatureWarning": (2, "warning"),
    "reportFeatureInfo": (2, "info"),
    "reportFeatureError": (2, "error"),
    "verifyNonemptyQuery": (3, "empty selection"),
}

# Lookup table levels' names (in generated tables)
TABLE_KEYS = {'"displayName"': "lookup table level"}


@dataclasses.dataclass
class UserString:
    line: int  # 1-based
    kind: str
    text: str


def user_strings(source: str) -> list[UserString]:
    """The user-facing strings in a file's source, in order. Lookup table level names are listed once each."""
    tokens = [token for token in scan(source).tokens if token.kind != "eof"]
    found = []
    levels = set()
    for index, token in enumerate(tokens):
        following = tokens[index + 1] if index + 1 < len(tokens) else None
        if following is None:
            continue
        if token.kind == "string" and token.value in TABLE_KEYS and following.value == ":":
            text = _expression(source, tokens, index + 2, (",", "}"))
            if text not in levels:
                levels.add(text)
                found.append(UserString(token.line + 1, TABLE_KEYS[token.value], text))
        elif token.kind == "string" and token.value in ANNOTATION_KEYS and following.value == ":":
            kind = ANNOTATION_KEYS[token.value]
            if kind == "name" and _annotates_enum_value(tokens, index):
                kind = "enum value"
            found.append(UserString(token.line + 1, kind, _expression(source, tokens, index + 2, (",", "}"))))
        elif token.kind == "identifier" and token.value in CALLS and following.value == "(":
            # Not where it's declared
            if index > 0 and tokens[index - 1].value == "function":
                continue
            argument, kind = CALLS[token.value]
            arguments = _arguments(source, tokens, index + 1)
            if argument < len(arguments) and arguments[argument]:
                found.append(UserString(token.line + 1, kind, arguments[argument]))
    return found


def _annotates_enum_value(tokens: list[Token], key: int) -> bool:
    """Whether the annotation map with the key at `tokens[key]` is on an enum's value (a name followed by `,` or `}`)."""
    depth = 0
    for position in range(key, len(tokens)):
        token = tokens[position]
        if token.kind == "string":
            continue
        if token.value in ("(", "[", "{"):
            depth += 1
        elif token.value in (")", "]", "}"):
            if depth == 0:
                after = tokens[position + 1 : position + 3]
                return len(after) == 2 and after[0].kind == "identifier" and after[1].value in (",", "}")
            depth -= 1
    return False


def _expression(source: str, tokens: list[Token], start: int, ends: Iterable[str]) -> str:
    """The source of the expression starting at `tokens[start]`, up to one of `ends` outside brackets."""
    depth = 0
    position = start
    while position < len(tokens):
        token = tokens[position]
        if token.kind != "string":
            if token.value in ("(", "[", "{"):
                depth += 1
            elif token.value in (")", "]", "}"):
                if depth == 0:
                    break
                depth -= 1
            elif depth == 0 and token.value in ends:
                break
        position += 1
    if position == start:
        return ""
    return _one_line(source[tokens[start].offset : tokens[position - 1].end])


def _arguments(source: str, tokens: list[Token], open_paren: int) -> list[str]:
    """The source of each argument of the call whose `(` is `tokens[open_paren]`."""
    arguments = []
    position = open_paren + 1
    while position < len(tokens) and tokens[position].value != ")":
        start = position
        depth = 0
        while position < len(tokens):
            token = tokens[position]
            if token.kind != "string":
                if token.value in ("(", "[", "{"):
                    depth += 1
                elif token.value in (")", "]", "}"):
                    if depth == 0:
                        break
                    depth -= 1
                elif depth == 0 and token.value == ",":
                    break
            position += 1
        arguments.append(_one_line(source[tokens[start].offset : tokens[position - 1].end]) if position > start else "")
        if position < len(tokens) and tokens[position].value == ",":
            position += 1
    return arguments


def _one_line(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()
