"""Signatures of functions, predicates, and features, for hovers and signature help."""

from __future__ import annotations

import dataclasses
import re

from fs_lsp.doc_comments import leading_doc_comment
from fs_lsp.fsdoc import DocComment, param_markdown, parse_doc
from fs_lsp.scanner import Token

# How many lines a declaration's parameters may span
MAX_SIGNATURE_LINES = 12


@dataclasses.dataclass
class Signature:
    """A callable's signature, like FsDoc's: `opExtrude(context is Context, id is Id, definition is map)`."""

    name: str
    # Each parameter as written, e.g. "definition is map"
    parameters: list[str]
    returns: str | None = None
    doc: DocComment | None = None

    @property
    def label(self) -> str:
        returns = f" returns {self.returns}" if self.returns else ""
        return f"{self.name}({', '.join(self.parameters)}){returns}"

    def parameter_offsets(self) -> list[tuple[int, int]]:
        """Where each parameter is in the label."""
        offsets = []
        position = len(self.name) + 1
        for parameter in self.parameters:
            offsets.append((position, position + len(parameter)))
            position += len(parameter) + 2
        return offsets

    def parameter_doc(self, index: int) -> str | None:
        if self.doc is None or index >= len(self.parameters):
            return None
        name, _, type = self.parameters[index].partition(" is ")
        item = self.doc.param(name.strip())
        return param_markdown(item, type.strip() or None) if item else None


def parse_signature(name: str, declaration: str) -> Signature | None:
    """The signature of a declaration of name, like `export function f(a is A) returns B`, or `export const f =
    function(...)`, or a feature's `defineFeature(function(context is Context, id is Id, definition is map)`."""
    start = declaration.find("(")
    if start < 0:
        return None
    if "defineFeature" in declaration:
        start = declaration.find("(", declaration.find("function"))
    depth = 0
    for index in range(start, len(declaration)):
        char = declaration[index]
        if char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
            if depth == 0:
                inside = declaration[start + 1 : index]
                returns = re.match(r"\s*returns\s+([A-Za-z_][\w.]*)", declaration[index + 1 :])
                parameters = [re.sub(r"\s+", " ", part).strip() for part in _split(inside)]
                return Signature(name, [part for part in parameters if part], returns[1] if returns else None)
    return None


def _split(text: str) -> list[str]:
    """Splits parameters at commas which aren't nested."""
    parts, depth, start = [], 0, 0
    for index, char in enumerate(text):
        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        elif char == "," and depth == 0:
            parts.append(text[start:index])
            start = index + 1
    parts.append(text[start:])
    return parts


def declaration_text(lines: list[str], line: int) -> str:
    """A declaration's text up to its body, from the line it starts on."""
    parts = []
    depth = 0
    for text in lines[line : line + MAX_SIGNATURE_LINES]:
        text = re.sub(r"//.*", "", text)
        parts.append(text.strip())
        depth += text.count("(") - text.count(")")
        if depth <= 0 and ")" in text:
            break
    return " ".join(parts)


def source_signature(source: str, name: str, line: int) -> Signature | None:
    """The signature of the declaration of name on line of source, with its doc comment."""
    lines = source.split("\n")
    signature = parse_signature(name, declaration_text(lines, line))
    if signature is not None:
        doc = leading_doc_comment(lines, line)
        signature.doc = parse_doc(doc) if doc else None
    return signature


@dataclasses.dataclass
class Call:
    """A call the cursor is in."""

    callee: Token
    # Which argument the cursor is in
    argument: int


def call_at(tokens: list[Token], offset: int) -> Call | None:
    """The innermost call whose parentheses hold offset."""
    stack: list[list] = []  # [opening token, the token before it, commas]
    previous = None
    for token in tokens:
        if token.offset >= offset:
            break
        value = token.value
        if value in ("(", "[", "{"):
            stack.append([token, previous, 0])
        elif value in (")", "]", "}"):
            if stack:
                stack.pop()
        elif value == "," and stack:
            stack[-1][2] += 1
        elif value == ";":
            # A statement ended, so any call before it did too
            stack = [entry for entry in stack if entry[0].value == "{"]
        previous = token
    for opening, before, commas in reversed(stack):
        if opening.value == "(":
            if before is not None and before.kind == "identifier":
                return Call(before, commas)
            return None
    return None
