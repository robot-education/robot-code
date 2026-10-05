"""Resolves identifiers to declarations within a single file (Go to Definition / Find References)."""

from __future__ import annotations

import bisect
import dataclasses
from typing import Iterable, Literal

from fs_lsp.parser import AstNode, ParsedProgram, SemanticHint
from fs_lsp.scanner import Token, is_identifier_token

IndexedKind = Literal[
    "feature",
    "function",
    "predicate",
    "operator",
    "enum",
    "enumMember",
    "type",
    "variable",
    "parameter",
    "definitionProperty",
]

FILE_SCOPED_NODE_TYPES = frozenset(
    [
        "FeatureDeclaration",
        "FunctionDeclaration",
        "PredicateDeclaration",
        "OperatorDeclaration",
        "EnumDeclaration",
        "TypeDeclaration",
        "TopLevelConst",
    ]
)

LOCAL_SCOPE_NODE_TYPES = frozenset(
    [
        "FeatureDeclaration",
        "FunctionDeclaration",
        "PredicateDeclaration",
        "OperatorDeclaration",
        "FunctionExpression",
        "PreconditionBlock",
        "Block",
    ]
)

FUNCTION_NODE_TYPES = frozenset(
    [
        "FunctionDeclaration",
        "PredicateDeclaration",
        "OperatorDeclaration",
        "FunctionExpression",
        "FeatureDeclaration",
    ]
)

FUNCTION_LIKE_NODE_TYPES = frozenset(
    [
        "FunctionExpression",
        "FunctionDeclaration",
        "PredicateDeclaration",
        "OperatorDeclaration",
    ]
)

DECLARATION_KINDS: dict[str, IndexedKind] = {
    "feature": "feature",
    "function": "function",
    "predicate": "predicate",
    "enum": "enum",
    "enumMember": "enumMember",
    "type": "type",
    "variable": "variable",
    "parameter": "parameter",
}


@dataclasses.dataclass(slots=True)
class Declaration:
    key: str
    name: str
    kind: IndexedKind
    token: Token
    scope_start: int
    scope_end: int
    visible_from: int
    parent: str | None = None


@dataclasses.dataclass(slots=True)
class Reference:
    token: Token
    declaration: Declaration


class EnclosingNodes:
    """Finds the smallest node of a given set of types containing a range.

    Nodes produced by the (recursive descent) parser nest properly, so the nodes form a tree. The
    smallest enclosing node is found by locating the last node starting at or before the range and
    walking up its ancestors.
    """

    def __init__(self, nodes: Iterable[AstNode]) -> None:
        self.nodes = sorted(nodes, key=lambda node: (node.start, -node.end))
        self.starts = [node.start for node in self.nodes]
        self.parents: list[int] = []
        stack: list[int] = []
        for index, node in enumerate(self.nodes):
            while stack and self.nodes[stack[-1]].end < node.end:
                stack.pop()
            self.parents.append(stack[-1] if stack else -1)
            stack.append(index)

    def find(self, start: int, end: int) -> AstNode | None:
        index = bisect.bisect_right(self.starts, start) - 1
        while index >= 0:
            node = self.nodes[index]
            if node.start <= start and end <= node.end:
                return node
            index = self.parents[index]
        return None


class SymbolIndex:
    def __init__(self, parsed: ParsedProgram) -> None:
        self.parsed = parsed
        self.tokens = [token for token in parsed.tokens if token.kind != "eof"]
        self.token_offsets = [token.offset for token in self.tokens]
        self.token_index_by_offset = {
            token.offset: index for index, token in enumerate(self.tokens)
        }
        self.ignored_offsets = {
            hint.token.offset
            for hint in parsed.hints
            if hint.type in ("mapKey", "annotationKey")
        }
        self._enclosing: dict[frozenset[str], EnclosingNodes] = {}
        # The end of each local `const`/`var` statement, by the offset of its keyword
        self._statement_ends = {
            node.start: node.end
            for node in parsed.nodes
            if node.type == "Block" and node.name
        }
        self._file_scoped: dict[str, list[AstNode]] = {}
        for node in parsed.nodes:
            if node.type in FILE_SCOPED_NODE_TYPES and node.name:
                self._file_scoped.setdefault(node.name, []).append(node)

        self.declarations = self._declaration_hints()
        self.declarations.extend(self._definition_property_declarations())
        self.declarations.sort(key=lambda declaration: declaration.token.offset)

        self.declaration_by_offset: dict[int, Declaration] = {}
        self.declarations_by_name: dict[str, list[Declaration]] = {}
        for declaration in self.declarations:
            self.declaration_by_offset.setdefault(declaration.token.offset, declaration)
            self.declarations_by_name.setdefault(declaration.name, []).append(
                declaration
            )

        self.references: list[Reference] = []
        self.references_by_key: dict[str, list[Reference]] = {}
        for token in self.tokens:
            declaration = self.declaration_for_token(token)
            if declaration:
                reference = Reference(token, declaration)
                self.references.append(reference)
                self.references_by_key.setdefault(declaration.key, []).append(reference)

    # Queries

    def token_at(self, offset: int) -> Token | None:
        """Returns the token containing offset, or ending at it."""
        index = bisect.bisect_right(self.token_offsets, offset) - 1
        if index >= 0:
            token = self.tokens[index]
            if token.offset <= offset < token.end:
                return token
            if offset == token.end and offset > token.offset:
                return token
        return None

    def definition_at(self, offset: int) -> Declaration | None:
        token = self.token_at(offset)
        return self.declaration_for_token(token) if token else None

    def references_at(self, offset: int, include_declaration: bool) -> list[Reference]:
        declaration = self.definition_at(offset)
        if not declaration:
            return []
        references = self.references_by_key.get(declaration.key, [])
        if include_declaration:
            return list(references)
        return [
            reference
            for reference in references
            if reference.token.offset != declaration.token.offset
        ]

    def previous_token(self, token: Token) -> Token | None:
        index = self.token_index_by_offset.get(token.offset)
        return self.tokens[index - 1] if index else None

    def next_token(self, token: Token) -> Token | None:
        index = self.token_index_by_offset.get(token.offset)
        if index is None or index + 1 >= len(self.tokens):
            return None
        return self.tokens[index + 1]

    def enclosing(self, token: Token, types: frozenset[str]) -> AstNode | None:
        finder = self._enclosing.get(types)
        if finder is None:
            finder = EnclosingNodes(
                node
                for node in self.parsed.nodes
                if node.type in types and not (node.type == "Block" and node.name)
            )
            self._enclosing[types] = finder
        return finder.find(token.offset, token.end)

    # Resolution

    def declaration_for_token(self, token: Token) -> Declaration | None:
        direct = self.declaration_by_offset.get(token.offset)
        if direct:
            return direct
        if token.offset in self.ignored_offsets or not is_identifier_token(token):
            return None
        previous = self.previous_token(token)
        if previous is not None and previous.value in (".", "?."):
            before_previous = self.previous_token(previous)
            if before_previous is None:
                return None
            if before_previous.value == "definition":
                return self._choose(
                    token,
                    lambda d: d.kind == "definitionProperty"
                    and d.scope_start <= token.offset <= d.scope_end,
                )
            return self._choose(
                token,
                lambda d: d.kind == "enumMember"
                and d.parent == before_previous.value
                and d.scope_start <= token.offset <= d.scope_end,
            )
        next_token = self.next_token(token)
        if next_token is not None and next_token.value == "::":
            return None
        return self._choose(
            token,
            lambda d: d.kind not in ("enumMember", "operator", "definitionProperty")
            and d.visible_from <= token.offset <= d.scope_end,
        )

    def _choose(self, token: Token, predicate) -> Declaration | None:
        candidates = [
            declaration
            for declaration in self.declarations_by_name.get(token.value, [])
            if predicate(declaration)
        ]
        if not candidates:
            return None
        # Prefer the innermost scope, then the most recent declaration
        return min(
            candidates,
            key=lambda d: (d.scope_end - d.scope_start, -d.visible_from),
        )

    # Building declarations

    def _declaration_hints(self) -> list[Declaration]:
        declarations = []
        seen: set[int] = set()
        for hint in self.parsed.hints:
            offset = hint.token.offset
            if (
                "declaration" not in hint.modifiers
                or offset in self.ignored_offsets
                or offset in seen
            ):
                continue
            kind = self._declaration_kind(hint)
            if not kind:
                continue
            parent = None
            if kind == "enumMember":
                enum_node = self.enclosing(hint.token, frozenset(["EnumDeclaration"]))
                parent = enum_node.name if enum_node else None
            scope_start, scope_end, visible_from = self._scope(hint.token, kind)
            declarations.append(
                Declaration(
                    key=_declaration_key(kind, hint.token.value, scope_start, parent),
                    name=hint.token.value,
                    kind=kind,
                    token=hint.token,
                    scope_start=scope_start,
                    scope_end=scope_end,
                    visible_from=visible_from,
                    parent=parent,
                )
            )
            seen.add(offset)
        return declarations

    def _declaration_kind(self, hint: SemanticHint) -> IndexedKind | None:
        if hint.type == "function":
            previous = self.previous_token(hint.token)
            if previous is not None and previous.value == "operator":
                return "operator"
        return DECLARATION_KINDS.get(hint.type)

    def _definition_property_declarations(self) -> list[Declaration]:
        """Treats `definition.foo is ...` in a feature's precondition as declaring foo."""
        declarations = []
        seen: set[tuple[int, str]] = set()
        parsed = self.parsed
        for index, token in enumerate(self.tokens):
            if (
                index < 2
                or token.offset in self.ignored_offsets
                or not is_identifier_token(token)
                or self.tokens[index - 1].value not in (".", "?.")
                or self.tokens[index - 2].value != "definition"
                or index + 1 >= len(self.tokens)
                or self.tokens[index + 1].value != "is"
            ):
                continue
            scope_node = self.enclosing(
                token, frozenset(["FeatureDeclaration"])
            ) or self.enclosing(token, FUNCTION_LIKE_NODE_TYPES)
            scope_start = scope_node.start if scope_node else parsed.start
            scope_end = scope_node.end if scope_node else parsed.end
            if (scope_start, token.value) in seen:
                continue
            seen.add((scope_start, token.value))
            declarations.append(
                Declaration(
                    key=_declaration_key(
                        "definitionProperty", token.value, scope_start
                    ),
                    name=token.value,
                    kind="definitionProperty",
                    token=token,
                    scope_start=scope_start,
                    scope_end=scope_end,
                    visible_from=scope_start,
                    parent="definition",
                )
            )
        return declarations

    def _scope(self, token: Token, kind: IndexedKind) -> tuple[int, int, int]:
        parsed = self.parsed
        if kind == "parameter":
            node = self.enclosing(token, FUNCTION_NODE_TYPES)
            start = node.start if node else parsed.start
            end = node.end if node else parsed.end
            return start, end, start
        if self._is_file_scoped(token, kind):
            return parsed.start, parsed.end, parsed.start
        node = self.enclosing(token, LOCAL_SCOPE_NODE_TYPES)
        visible_from = token.offset
        keyword = self.previous_token(token)
        if kind == "variable" and keyword is not None and keyword.value in ("const", "var"):
            # A variable isn't visible in its own initializer: `const f = f(x);` calls the function f
            visible_from = self._statement_ends.get(keyword.offset, visible_from)
        return (
            node.start if node else token.offset,
            node.end if node else parsed.end,
            visible_from,
        )

    def _is_file_scoped(self, token: Token, kind: IndexedKind) -> bool:
        if kind in ("enumMember", "operator"):
            return True
        return any(
            node.start <= token.offset and token.end <= node.end
            for node in self._file_scoped.get(token.value, [])
        )


def _declaration_key(
    kind: IndexedKind, name: str, scope_start: int, parent: str | None = None
) -> str:
    if kind == "enumMember":
        return f"enumMember:{parent or ''}.{name}"
    if kind == "definitionProperty":
        return f"definition:{scope_start}.{name}"
    if kind in ("parameter", "variable"):
        return f"{kind}:{scope_start}.{name}"
    return f"{kind}:{name}"
