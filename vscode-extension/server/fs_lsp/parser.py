"""A tolerant FeatureScript parser.

The parser never fails: it recovers from incomplete code while the user is typing and records just
enough structure (declarations, scopes, maps vs. blocks, ...) to power highlighting and navigation.

Ported from gatrall/featurescript-language-support (MIT).
"""

from __future__ import annotations

import dataclasses
import re
from typing import Literal

from fs_lsp.scanner import (
    ASSIGNMENT_OPERATORS,
    LineMap,
    ScanResult,
    Token,
    TokenKind,
    is_identifier_token,
    scan,
)

NodeType = Literal[
    "Program",
    "VersionDirective",
    "ImportDeclaration",
    "NamespacedImportDeclaration",
    "AnnotationStatement",
    "AnnotationMap",
    "TopLevelConst",
    "FeatureDeclaration",
    "FunctionDeclaration",
    "FunctionExpression",
    "ArrowFunction",
    "PredicateDeclaration",
    "OperatorDeclaration",
    "EnumDeclaration",
    "EnumMember",
    "TypeDeclaration",
    "PreconditionBlock",
    "Block",
    "MapLiteral",
    "ArrayLiteral",
    "FunctionCall",
    "MemberAccess",
    "SafeMemberAccess",
    "IndexAccess",
    "SafeIndexAccess",
    "BoxAccess",
    "SafeBoxAccess",
    "NamespaceAccess",
    "TypeCheck",
    "TypeConversion",
]

SemanticType = Literal[
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

SemanticModifier = Literal[
    "declaration",
    "definition",
    "readonly",
    "modification",
    "documentation",
    "defaultLibrary",
]

SymbolKind = Literal[
    "feature",
    "function",
    "predicate",
    "enum",
    "enumMember",
    "type",
    "variable",
    "parameter",
    "namespace",
]


@dataclasses.dataclass(slots=True)
class AstNode:
    type: NodeType
    start: int
    end: int
    token: Token
    name: str | None = None


@dataclasses.dataclass(slots=True)
class SemanticHint:
    token: Token
    type: SemanticType
    modifiers: tuple[SemanticModifier, ...]


@dataclasses.dataclass(slots=True)
class SymbolInfo:
    name: str
    kind: SymbolKind
    token: Token
    readonly: bool | None = None
    parent: str | None = None


@dataclasses.dataclass
class ParsedProgram:
    source: str
    start: int
    end: int
    tokens: list[Token]
    line_map: LineMap
    unterminated: list[tuple[int, int, str]]
    nodes: list[AstNode]
    hints: list[SemanticHint]
    symbols: dict[str, SymbolInfo]
    variables: dict[str, SymbolInfo]
    parameters: dict[str, SymbolInfo]
    enums: set[str]
    enum_members: dict[str, list[str]]
    imports_stdlib: bool


EXPRESSION_STOPS = frozenset([";", ",", "}", ")"])

STRUCTURAL_KEYWORDS = frozenset(
    [
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
        "if",
        "else",
        "for",
        "while",
        "try",
        "catch",
        "return",
        "throw",
        "const",
        "var",
    ]
)

STDLIB_IMPORT = re.compile(r"onshape/std/(?:geometry|common)\.fs")


def parse(source: str) -> ParsedProgram:
    return Parser(source, scan(source)).parse()


class Parser:
    def __init__(self, source: str, scanned: ScanResult) -> None:
        self.source = source
        self.scanned = scanned
        self.tokens = scanned.tokens
        self.current = 0
        self.nodes: list[AstNode] = []
        self.hints: list[SemanticHint] = []
        self.symbols: dict[str, SymbolInfo] = {}
        self.variables: dict[str, SymbolInfo] = {}
        self.parameters: dict[str, SymbolInfo] = {}
        self.enums: set[str] = set()
        self.enum_members: dict[str, list[str]] = {}
        self.imports_stdlib = False

    def parse(self) -> ParsedProgram:
        start = self.peek()
        while not self.is_at_end():
            self.parse_top_level()
        end = self.previous() or start
        return ParsedProgram(
            source=self.source,
            start=start.offset,
            end=end.end,
            tokens=self.tokens,
            line_map=self.scanned.line_map,
            unterminated=self.scanned.unterminated,
            nodes=self.nodes,
            hints=self.hints,
            symbols=self.symbols,
            variables=self.variables,
            parameters=self.parameters,
            enums=self.enums,
            enum_members=self.enum_members,
            imports_stdlib=self.imports_stdlib,
        )

    # Top level

    def parse_top_level(self) -> None:
        if self.match_value("FeatureScript"):
            start = self.previous_required()
            if self.check_kind("number"):
                self.advance()
            self.consume_optional(";")
            self.add_node("VersionDirective", start, self.previous_required())
            return

        if self.check_value("annotation"):
            self.parse_annotation()
            return

        exported = self.match_value("export")
        if (
            self.check_identifier_like()
            and self.look_value(1) == "::"
            and self.look_value(2) == "import"
        ):
            self.parse_import(self.advance())
            return

        if self.check_value("import"):
            self.parse_import()
            return

        value = self.peek().value
        if value == "const":
            self.parse_variable_declaration(True, True)
        elif value == "var":
            self.parse_variable_declaration(False, True)
        elif value == "function":
            self.parse_function_declaration()
        elif value == "predicate":
            self.parse_predicate_declaration()
        elif value == "operator":
            self.parse_operator_declaration()
        elif value == "enum":
            self.parse_enum_declaration()
        elif value == "type":
            self.parse_type_declaration()
        elif exported:
            self.synchronize_top_level()
        else:
            self.parse_statement()

    def parse_import(self, namespace: Token | None = None) -> None:
        start = namespace or self.peek()
        if namespace:
            self.add_hint(namespace, "namespace")
            self.add_node(
                "NamespaceAccess", namespace, self.look(1) or namespace, namespace.value
            )
            self.consume_optional("::")
        self.consume_optional("import")
        while not self.is_at_end() and not self.check_value(";"):
            token = self.advance()
            if token.kind == "string" and STDLIB_IMPORT.search(token.value):
                self.imports_stdlib = True
        self.consume_optional(";")
        self.add_node(
            "NamespacedImportDeclaration" if namespace else "ImportDeclaration",
            start,
            self.previous_required(),
            namespace.value if namespace else None,
        )

    def parse_annotation(self) -> None:
        start = self.advance()
        self.add_hint(start, "decorator")
        if self.check_value("{"):
            self.parse_brace(True, "AnnotationMap")
        self.add_node("AnnotationStatement", start, self.previous_required())

    def parse_enum_declaration(self) -> None:
        start = self.advance()
        name = self.consume_identifier()
        if name:
            self.declare(name, "enum")
            self.enums.add(name.value)
            self.add_hint(name, "enum", "declaration")
        if self.check_value("{"):
            self.advance()
            while not self.is_at_end() and not self.check_value("}"):
                if self.check_value("annotation"):
                    self.parse_annotation()
                    continue
                if self.check_identifier_like():
                    member = self.advance()
                    parent = name.value if name else None
                    if parent:
                        members = self.enum_members.setdefault(parent, [])
                        if member.value not in members:
                            members.append(member.value)
                    self.declare(member, "enumMember", True, parent)
                    self.add_hint(member, "enumMember", "declaration", "readonly")
                    self.add_node("EnumMember", member, member, member.value)
                    self.consume_optional(",")
                    continue
                self.advance()
            self.consume_optional("}")
        self.add_node(
            "EnumDeclaration",
            start,
            self.previous_required(),
            name.value if name else None,
        )

    def parse_type_declaration(self) -> None:
        start = self.advance()
        name = self.consume_identifier()
        if name:
            self.declare(name, "type")
            self.add_hint(name, "type", "declaration")
        while not self.is_at_end() and not self.check_value(";"):
            if self.match_value("typecheck", "typeconvert"):
                target = self.consume_identifier()
                if target:
                    self.add_hint(target, "predicate")
                continue
            self.parse_expression_token()
        self.consume_optional(";")
        self.add_node(
            "TypeDeclaration",
            start,
            self.previous_required(),
            name.value if name else None,
        )

    def parse_function_declaration(self) -> None:
        start = self.advance()
        name = self.consume_identifier()
        if name:
            self.declare(name, "function")
            self.add_hint(name, "function", "declaration")
        self.parse_function_tail()
        self.add_node(
            "FunctionDeclaration",
            start,
            self.previous_required(),
            name.value if name else None,
        )

    def parse_predicate_declaration(self) -> None:
        start = self.advance()
        name = self.consume_identifier()
        if name:
            self.declare(name, "predicate")
            self.add_hint(name, "predicate", "declaration")
        self.parse_function_tail()
        self.add_node(
            "PredicateDeclaration",
            start,
            self.previous_required(),
            name.value if name else None,
        )

    def parse_operator_declaration(self) -> None:
        start = self.advance()
        operator = self.advance()
        if operator.kind != "eof":
            self.add_hint(operator, "function", "declaration")
        self.parse_function_tail()
        self.add_node(
            "OperatorDeclaration", start, self.previous_required(), operator.value
        )

    def parse_function_tail(self) -> None:
        if self.check_value("("):
            self.parse_parameter_list()
        if self.match_value("returns"):
            type_token = self.consume_identifier()
            if type_token:
                self.add_hint(type_token, "type")
        if self.check_value("precondition"):
            precondition = self.advance()
            if self.check_value("{"):
                self.parse_brace(False, "PreconditionBlock")
            self.add_node("PreconditionBlock", precondition, self.previous_required())
        if self.check_value("{"):
            self.parse_brace(False, "Block")

    def parse_function_expression(self) -> None:
        start = self.advance()
        if self.check_identifier_like() and self.look_value(1) == "(":
            name = self.advance()
            self.declare(name, "function")
            self.add_hint(name, "function", "declaration")
        self.parse_function_tail()
        self.add_node("FunctionExpression", start, self.previous_required())

    def parse_variable_declaration(self, readonly: bool, top_level: bool) -> None:
        start = self.advance()
        name = self.consume_identifier()
        is_feature = False
        if name and self.check_value("="):
            index = self.current + 1
            while index < len(self.tokens) and self.tokens[index].value == "(":
                index += 1
            is_feature = (
                index < len(self.tokens) and self.tokens[index].value == "defineFeature"
            )
        if name:
            kind: SymbolKind = "feature" if is_feature else "variable"
            self.declare(name, kind, readonly)
            self.variables[name.value] = SymbolInfo(name.value, kind, name, readonly)
            modifiers = ("declaration", "readonly") if readonly else ("declaration",)
            self.add_hint(name, "feature" if is_feature else "variable", *modifiers)
        if self.match_value("="):
            self.parse_expression_until({";"})
        self.consume_optional(";")
        node_type: NodeType = (
            "FeatureDeclaration"
            if is_feature
            else "TopLevelConst" if top_level and readonly else "Block"
        )
        self.add_node(
            node_type, start, self.previous_required(), name.value if name else None
        )

    # Statements and expressions

    def parse_statement(self) -> None:
        if self.is_at_end():
            return
        if self.check_value("annotation"):
            self.parse_annotation()
            return
        if self.check_value("precondition"):
            start = self.advance()
            if self.check_value("{"):
                self.parse_brace(False, "PreconditionBlock")
            self.add_node("PreconditionBlock", start, self.previous_required())
            return
        if self.check_value("const"):
            self.parse_variable_declaration(True, False)
            return
        if self.check_value("var"):
            self.parse_variable_declaration(False, False)
            return
        if self.check_value("{"):
            self.parse_brace(False, "Block")
            return
        self.parse_expression_until({";", "}"})
        self.consume_optional(";")

    def parse_expression_until(self, stops: set[str] | frozenset[str]) -> None:
        while not self.is_at_end() and self.peek().value not in stops:
            self.parse_expression_token(stops)

    def parse_expression_token(
        self, stops: set[str] | frozenset[str] = EXPRESSION_STOPS
    ) -> None:
        token = self.peek()
        value = token.value
        if value == "function":
            self.parse_function_expression()
        elif value == "{":
            self.parse_brace(False)
        elif value == "(":
            self.parse_paren(stops)
        elif value == "[":
            self.parse_bracket()
        elif value == "?[":
            start = self.advance()
            self.parse_expression_until({"]"})
            self.consume_optional("]")
            self.add_node("SafeIndexAccess", start, self.previous_required())
        elif value == "?[]":
            start = self.advance()
            self.add_node("SafeBoxAccess", start, start)
        elif value in (".", "?."):
            start = self.advance()
            prop = self.consume_identifier()
            if prop:
                if self.peek().value in ASSIGNMENT_OPERATORS:
                    self.add_hint(prop, "property", "modification")
                else:
                    self.add_hint(prop, "property")
            self.add_node(
                "SafeMemberAccess" if start.value == "?." else "MemberAccess",
                start,
                prop or start,
                prop.value if prop else None,
            )
        elif value == "::":
            start = self.advance()
            self.add_node("NamespaceAccess", start, start)
        elif value in ("is", "as"):
            start = self.advance()
            type_token = self.consume_identifier()
            if type_token:
                self.add_hint(type_token, "type")
            self.add_node(
                "TypeCheck" if start.value == "is" else "TypeConversion",
                start,
                type_token or start,
            )
        elif self.check_identifier_like() and self.look_value(1) == "::":
            namespace = self.advance()
            self.add_hint(namespace, "namespace")
            self.consume_optional("::")
            self.add_node(
                "NamespaceAccess", namespace, self.previous_required(), namespace.value
            )
        elif self.check_identifier_like() and self.look_value(1) == "(":
            call = self.advance()
            self.add_node("FunctionCall", call, call, call.value)
        elif self.check_identifier_like() and self.look_value(1) == "=>":
            parameter = self.advance()
            self.declare_parameter(parameter)
            arrow = self.advance()
            self.add_node("ArrowFunction", parameter, arrow)
        else:
            self.advance()

    def parse_paren(self, stops: set[str] | frozenset[str]) -> None:
        open_token = self.advance()
        content_start = self.current
        self.parse_expression_until({")"})
        self.consume_optional(")")
        close_index = self.current - 1
        if self.check_value("=>"):
            for token in self.tokens[content_start:close_index]:
                if token.kind == "identifier":
                    self.declare_parameter(token)
            arrow = self.advance()
            self.add_node("ArrowFunction", open_token, arrow)
            self.parse_expression_until(stops)

    def parse_bracket(self) -> None:
        previous = self.previous()
        start = self.advance()
        access = is_identifier_token(previous)
        if self.check_value("]"):
            self.advance()
            self.add_node(
                "BoxAccess" if access else "ArrayLiteral",
                start,
                self.previous_required(),
            )
            return
        self.parse_expression_until({"]"})
        self.consume_optional("]")
        self.add_node(
            "IndexAccess" if access else "ArrayLiteral", start, self.previous_required()
        )

    def parse_parameter_list(self) -> None:
        self.consume_optional("(")
        while not self.is_at_end() and not self.check_value(")"):
            if self.check_identifier_like():
                parameter = self.advance()
                self.declare_parameter(parameter)
                if self.match_value("is"):
                    type_token = self.consume_identifier()
                    if type_token:
                        self.add_hint(type_token, "type")
                continue
            self.advance()
        self.consume_optional(")")

    def parse_brace(
        self,
        annotation_context: bool,
        forced_type: NodeType | None = None,
    ) -> None:
        start = self.advance()
        is_map = (
            annotation_context
            or forced_type == "AnnotationMap"
            or (forced_type is None and self.looks_like_map())
        )
        if is_map:
            while not self.is_at_end() and not self.check_value("}"):
                before = self.current
                self.parse_map_entry(annotation_context)
                self.consume_optional(",")
                if self.current == before:
                    # A stray ":" or similar; skip it so we can't loop forever
                    self.advance()
            self.consume_optional("}")
            self.add_node(
                (
                    "AnnotationMap"
                    if annotation_context or forced_type == "AnnotationMap"
                    else "MapLiteral"
                ),
                start,
                self.previous_required(),
            )
            return
        while not self.is_at_end() and not self.check_value("}"):
            before = self.current
            self.parse_statement()
            if self.current == before:
                self.advance()
        self.consume_optional("}")
        self.add_node(forced_type or "Block", start, self.previous_required())

    def parse_map_entry(self, annotation_context: bool) -> None:
        key = self.peek()
        key_type: SemanticType = "annotationKey" if annotation_context else "mapKey"
        if key.kind == "string":
            self.add_hint(key, key_type)
            self.advance()
        elif self.check_identifier_like() and self.look_value(1) == ":":
            self.add_hint(key, key_type)
            self.advance()
        elif key.value == "(":
            self.parse_paren({":", ",", "}"})
        else:
            self.parse_expression_until({":", ",", "}"})
        if self.match_value(":"):
            self.parse_expression_until({",", "}"})

    def looks_like_map(self) -> bool:
        paren = bracket = brace = 0
        tokens = self.tokens
        for index in range(self.current, len(tokens)):
            token = tokens[index]
            value = token.value
            if token.kind == "eof":
                return False
            if value == "{":
                brace += 1
            elif value == "}":
                if brace == 0:
                    return False
                brace -= 1
            elif value == "(":
                paren += 1
            elif value == ")":
                paren -= 1
            elif value == "[":
                bracket += 1
            elif value == "]":
                bracket -= 1
            elif paren == 0 and bracket == 0 and brace == 0:
                if value == ":":
                    return True
                if value == ";":
                    return False
        return False

    # Bookkeeping

    def declare(
        self,
        token: Token,
        kind: SymbolKind,
        readonly: bool | None = None,
        parent: str | None = None,
    ) -> None:
        key = f"{parent}.{token.value}" if parent else token.value
        self.symbols[key] = SymbolInfo(token.value, kind, token, readonly, parent)

    def declare_parameter(self, token: Token) -> None:
        self.parameters[token.value] = SymbolInfo(token.value, "parameter", token)
        self.add_hint(token, "parameter", "declaration")

    def add_hint(
        self, token: Token, type: SemanticType, *modifiers: SemanticModifier
    ) -> None:
        self.hints.append(SemanticHint(token, type, modifiers))

    def add_node(
        self, type: NodeType, start: Token, end: Token, name: str | None = None
    ) -> None:
        self.nodes.append(AstNode(type, start.offset, end.end, start, name))

    # Token helpers

    def consume_identifier(self) -> Token | None:
        if self.check_identifier_like():
            return self.advance()
        return None

    def consume_optional(self, value: str) -> bool:
        if self.check_value(value):
            self.advance()
            return True
        return False

    def match_value(self, *values: str) -> bool:
        if self.peek().value in values:
            self.advance()
            return True
        return False

    def synchronize_top_level(self) -> None:
        while (
            not self.is_at_end()
            and not self.check_value(";")
            and not self.check_value("}")
        ):
            self.advance()
        self.consume_optional(";")

    def check_value(self, value: str) -> bool:
        return self.peek().value == value

    def check_kind(self, kind: TokenKind) -> bool:
        return self.peek().kind == kind

    def check_identifier_like(self) -> bool:
        token = self.peek()
        return token.kind == "identifier" or (
            token.kind == "keyword" and token.value not in STRUCTURAL_KEYWORDS
        )

    def advance(self) -> Token:
        if not self.is_at_end():
            self.current += 1
        return self.tokens[self.current - 1] if self.current > 0 else self.tokens[0]

    def peek(self) -> Token:
        return self.tokens[self.current]

    def look(self, distance: int) -> Token | None:
        index = self.current + distance
        return self.tokens[index] if index < len(self.tokens) else None

    def look_value(self, distance: int) -> str | None:
        token = self.look(distance)
        return token.value if token else None

    def previous(self) -> Token | None:
        return self.tokens[self.current - 1] if self.current > 0 else None

    def previous_required(self) -> Token:
        return self.previous() or self.peek()

    def is_at_end(self) -> bool:
        return self.peek().kind == "eof"
