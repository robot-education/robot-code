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

# Built-in type names, which Onshape's parser treats as their own token, so they can't name anything
RESERVED_TYPE_NAMES = frozenset(["boolean", "number", "string", "array", "map", "box", "builtin"])

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

    previous = {
        token.offset: parsed.tokens[index - 1].value if index else None
        for index, token in enumerate(parsed.tokens)
    }
    for hint in parsed.hints:
        if (
            "declaration" in hint.modifiers
            and hint.token.value in RESERVED_TYPE_NAMES
            # A type, e.g. in an arrow function's parameters: `(name is string) => ...`
            and previous.get(hint.token.offset) not in ("is", "returns", "as")
        ):
            error(
                hint.token,
                f"'{hint.token.value}' is a built-in type name, which FeatureScript reserves; rename it.",
            )

    stack: list[Token] = []
    for token in parsed.tokens:
        if token.kind == "invalid":
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
    for unclosed in stack:
        error(
            unclosed,
            f"'{unclosed.value}' is never closed (expected '{OPENERS[unclosed.value]}').",
        )
    return results
