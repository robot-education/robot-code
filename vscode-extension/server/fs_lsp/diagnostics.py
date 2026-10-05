"""Cheap, high-confidence syntax diagnostics.

The parser is deliberately tolerant, so this only reports problems which are unambiguous from the
token stream: unbalanced brackets, unterminated strings/comments, and characters or operators
FeatureScript doesn't have. Real compile errors still come from Onshape (see `fs push`).
"""

from __future__ import annotations

from lsprotocol import types as lsp

from fs_lsp.parser import ParsedProgram
from fs_lsp.scanner import Token

SOURCE = "featurescript"

OPENERS = {"(": ")", "[": "]", "{": "}", "?[": "]"}
CLOSERS = {")", "]", "}"}


def diagnostics(parsed: ParsedProgram) -> list[lsp.Diagnostic]:
    results = []
    line_map = parsed.line_map

    def offsets_range(start: int, end: int) -> lsp.Range:
        return lsp.Range(
            lsp.Position(*line_map.position(start)),
            lsp.Position(*line_map.position(end)),
        )

    def error(token: Token, message: str) -> None:
        results.append(
            lsp.Diagnostic(
                range=offsets_range(token.offset, token.end),
                message=message,
                severity=lsp.DiagnosticSeverity.Error,
                source=SOURCE,
            )
        )

    for start, end, kind in parsed.unterminated:
        results.append(
            lsp.Diagnostic(
                range=offsets_range(start, min(end, start + 2)),
                message=f"Unterminated {kind}.",
                severity=lsp.DiagnosticSeverity.Error,
                source=SOURCE,
            )
        )

    stack: list[Token] = []
    previous: Token | None = None
    for token in parsed.tokens:
        if is_version_placeholder(token, previous):
            pass
        elif token.kind == "invalid":
            if token.value in ("++", "--"):
                error(
                    token,
                    f"FeatureScript has no {token.value} operator; use {token.value[0]}= 1 instead.",
                )
            else:
                error(token, f"Unexpected character {token.value!r}.")
        elif token.kind in ("operator", "punctuation"):
            if token.value in OPENERS:
                stack.append(token)
            elif token.value in CLOSERS:
                if stack and OPENERS[stack[-1].value] == token.value:
                    stack.pop()
                elif any(OPENERS[opener.value] == token.value for opener in stack):
                    # Recover by closing everything opened since the matching bracket
                    while OPENERS[stack[-1].value] != token.value:
                        unclosed = stack.pop()
                        error(
                            unclosed,
                            f"'{unclosed.value}' is never closed (expected '{OPENERS[unclosed.value]}').",
                        )
                    stack.pop()
                else:
                    error(token, f"Unmatched '{token.value}'.")
        previous = token
    for unclosed in stack:
        error(
            unclosed,
            f"'{unclosed.value}' is never closed (expected '{OPENERS[unclosed.value]}').",
        )
    return results


def is_version_placeholder(token: Token, previous: Token | None) -> bool:
    """The checked-in std (std/) replaces version numbers with ✨, e.g. `FeatureScript ✨;`."""
    return (
        token.value == "✨"
        and previous is not None
        and previous.value == "FeatureScript"
    )
