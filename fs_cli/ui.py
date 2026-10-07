"""Renders a feature's dialog the way Onshape shows it, approximately, from its precondition.

The precondition is evaluated rather than run: parameters take their defaults (or values given with
`fs ui --set`), predicates are inlined, and `if`s are decided from the parameters' values, the same
way Onshape decides which parameters to show. Editing logic doesn't run, so values it would set
aren't shown.

The dialog is written as HTML and screenshotted with headless Chromium. Its styling is a rough
approximation of Onshape's, to be refined against screenshots of the real thing.
"""

from __future__ import annotations

import dataclasses
import html
import itertools
import os
import pathlib
import re
import shutil
import subprocess
import tempfile
from typing import Any, Callable, Iterable

from fs_lsp.project import Project
from fs_lsp.scanner import Token, scan

STD_PREFIX = "onshape/std/"


class UiError(Exception):
    pass


# Syntax


@dataclasses.dataclass
class Node:
    kind: str
    args: tuple = ()
    token: Token | None = None


class Parser:
    """Parses the parts of FeatureScript a precondition uses: statements and expressions."""

    BINARY = [
        ("||",),
        ("&&",),
        ("==", "!="),
        ("<", ">", "<=", ">="),
        ("is", "as"),
        ("+", "-", "~"),
        ("*", "/", "%"),
        ("^",),
    ]

    def __init__(self, tokens: list[Token], position: int = 0) -> None:
        self.tokens = tokens
        self.position = position

    def peek(self, distance: int = 0) -> Token:
        return self.tokens[min(self.position + distance, len(self.tokens) - 1)]

    def check(self, value: str) -> bool:
        return self.peek().value == value and self.peek().kind != "string"

    def advance(self) -> Token:
        token = self.peek()
        self.position += 1
        return token

    def expect(self, value: str) -> Token:
        if not self.check(value):
            token = self.peek()
            raise UiError(f"Expected {value!r} at line {token.line + 1}, found {token.value!r}.")
        return self.advance()

    def accept(self, value: str) -> bool:
        if self.check(value):
            self.advance()
            return True
        return False

    def skip_balanced(self) -> None:
        """Skips a bracketed group starting at the current token."""
        pairs = {"(": ")", "[": "]", "{": "}"}
        stack = [pairs[self.advance().value]]
        while stack and self.peek().kind != "eof":
            token = self.advance()
            if token.kind in ("string",):
                continue
            if token.value in pairs:
                stack.append(pairs[token.value])
            elif token.value == stack[-1]:
                stack.pop()

    # Statements

    def block(self) -> Node:
        self.expect("{")
        statements = []
        while not self.check("}") and self.peek().kind != "eof":
            statements.append(self.statement())
        self.expect("}")
        return Node("block", tuple(statements))

    def statement(self) -> Node:
        token = self.peek()
        if self.check("annotation"):
            self.advance()
            annotation = self.expression()
            return Node("annotated", (annotation, self.statement()), token)
        if self.check("{"):
            return self.block()
        if self.check("if"):
            self.advance()
            self.expect("(")
            condition = self.expression()
            self.expect(")")
            then = self.statement()
            otherwise = self.statement() if self.accept("else") else None
            return Node("if", (condition, then, otherwise), token)
        if self.check("for") and self.peek(1).value == "(" and self.peek(2).value == "var" and self.peek(4).value == "in":
            # for (var item in definition.items), as array parameters' items are declared
            self.advance()
            self.expect("(")
            self.advance()
            variable = self.advance().value
            self.expect("in")
            iterable = self.expression()
            self.expect(")")
            return Node("loop", (self.statement(), variable, iterable), token)
        if self.check("for") or self.check("while"):
            self.advance()
            self.skip_balanced()
            return Node("loop", (self.statement(), None, None), token)
        if self.check("var") or self.check("const"):
            self.advance()
            name = self.advance().value
            if self.accept("is"):
                self.expression()
            value = self.expression() if self.accept("=") else None
            self.accept(";")
            return Node("declare", (name, value), token)
        if self.check("return"):
            self.advance()
            value = None if self.check(";") else self.expression()
            self.accept(";")
            return Node("return", (value,), token)
        if self.check(";"):
            self.advance()
            return Node("block", ())
        expression = self.expression()
        self.accept(";")
        return Node("expression", (expression,), token)

    # Expressions

    def expression(self) -> Node:
        condition = self.binary(0)
        if self.accept("?"):
            then = self.expression()
            self.expect(":")
            return Node("ternary", (condition, then, self.expression()))
        return condition

    def binary(self, level: int) -> Node:
        if level == len(self.BINARY):
            return self.unary()
        left = self.binary(level + 1)
        while self.peek().kind != "string" and self.peek().value in self.BINARY[level]:
            operator = self.advance().value
            if operator in ("is", "as"):
                left = Node(operator, (left, self.type_name()))
            else:
                left = Node("binary", (operator, left, self.binary(level + 1)))
        return left

    def type_name(self) -> str:
        name = self.advance().value
        while self.check("::"):
            self.advance()
            name = self.advance().value
        return name

    def unary(self) -> Node:
        if self.peek().kind != "string" and self.peek().value in ("!", "-", "+"):
            operator = self.advance().value
            return Node("unary", (operator, self.unary()))
        return self.postfix(self.primary())

    def postfix(self, node: Node) -> Node:
        while True:
            if self.check("."):
                self.advance()
                node = Node("member", (node, self.advance().value))
            elif self.check("["):
                self.advance()
                key = self.expression()
                self.expect("]")
                node = Node("index", (node, key))
            elif self.check("("):
                node = Node("call", (node, tuple(self.arguments())))
            elif self.check("->"):
                self.advance()
                callee = self.primary()
                node = Node("call", (callee, (node, *self.arguments())))
            else:
                return node

    def arguments(self) -> list[Node]:
        self.expect("(")
        arguments = []
        while not self.check(")") and self.peek().kind != "eof":
            arguments.append(self.expression())
            if not self.accept(","):
                break
        self.expect(")")
        return arguments

    def primary(self) -> Node:
        token = self.peek()
        if token.kind == "string":
            self.advance()
            return Node("literal", (_string_value(token.value),), token)
        if token.kind == "number":
            self.advance()
            return Node("literal", (float(token.value),), token)
        if self.check("("):
            self.advance()
            node = self.expression()
            self.expect(")")
            return node
        if self.check("["):
            self.advance()
            items = []
            while not self.check("]") and self.peek().kind != "eof":
                items.append(self.expression())
                if not self.accept(","):
                    break
            self.expect("]")
            return Node("array", tuple(items), token)
        if self.check("{"):
            return self.map_literal()
        if self.check("function"):
            self.advance()
            while not self.check("{"):
                self.advance()
            self.skip_balanced()
            return Node("unknown", (), token)
        if token.kind in ("identifier", "keyword"):
            self.advance()
            if self.check("::"):
                self.advance()
                return Node("name", (self.advance().value,), token)
            return Node("name", (token.value,), token)
        raise UiError(f"Unexpected {token.value!r} at line {token.line + 1}.")

    def map_literal(self) -> Node:
        token = self.expect("{")
        entries = []
        while not self.check("}") and self.peek().kind != "eof":
            if self.peek().kind in ("identifier", "keyword") and self.peek(1).value == ":":
                key = Node("literal", (self.advance().value,))
            else:
                key = self.binary(0)
            self.expect(":")
            entries.append((key, self.expression()))
            if not self.accept(","):
                break
        self.expect("}")
        return Node("map", tuple(entries), token)


def _string_value(text: str) -> str:
    body = text[1:-1] if len(text) >= 2 else ""
    return re.sub(r"\\(.)", r"\1", body)


# Declarations


@dataclasses.dataclass
class EnumType:
    name: str
    # value -> name shown in the UI; hidden values are left out
    values: dict[str, str]


@dataclasses.dataclass(frozen=True)
class EnumValue:
    type: str
    value: str


@dataclasses.dataclass(frozen=True)
class Unit:
    name: str


@dataclasses.dataclass
class Predicate:
    name: str
    parameters: list[str]
    body: Node


@dataclasses.dataclass
class Feature:
    name: str
    annotation: Node | None
    precondition: Node
    defaults: Node | None


UNITS = {"meter", "centimeter", "millimeter", "inch", "foot", "yard", "degree", "radian", "unitless"}


class Declarations:
    """The predicates, enums, constants and features of a set of files, parsed lazily.

    The first file added to declare a name wins, so files should be added nearest first (as
    `load_declarations` does), the way a file's own declarations hide those of files it imports.
    """

    def __init__(self) -> None:
        self.predicates: dict[str, Predicate] = {}
        self.enums: dict[str, EnumType] = {}
        self.constants: dict[str, tuple[list[Token], int]] = {}
        self.features: dict[str, Feature] = {}

    def merge(self, other: Declarations) -> None:
        """Adds another file's declarations, after this one's (so this one's win)."""
        for name in ("predicates", "enums", "constants", "features"):
            mine: dict = getattr(self, name)
            for key, value in getattr(other, name).items():
                mine.setdefault(key, value)

    def add_source(self, source: str) -> None:
        tokens = scan(source).tokens
        parser = Parser(tokens)
        annotation = None
        while parser.peek().kind != "eof":
            token = parser.peek()
            if token.value in ("export", "import") or token.kind == "string":
                parser.advance()
                continue
            if token.value == "annotation":
                parser.advance()
                annotation = parser.map_literal() if parser.check("{") else None
                continue
            try:
                if token.value == "predicate":
                    self._predicate(parser)
                elif token.value == "enum":
                    self._enum(parser)
                elif token.value == "const":
                    self._constant(parser, tokens, annotation)
                elif token.value in ("(", "[", "{"):
                    parser.skip_balanced()
                else:
                    parser.advance()
            except UiError:
                # Skip what can't be parsed; it's rarely needed for a dialog
                pass
            annotation = None

    def _predicate(self, parser: Parser) -> None:
        parser.advance()
        name = parser.advance().value
        parser.expect("(")
        parameters = []
        while not parser.check(")"):
            parameters.append(parser.advance().value)
            while not parser.check(",") and not parser.check(")"):
                parser.advance()
            parser.accept(",")
        parser.expect(")")
        self.predicates.setdefault(name, Predicate(name, parameters, parser.block()))

    def _enum(self, parser: Parser) -> None:
        parser.advance()
        name = parser.advance().value
        parser.expect("{")
        values: dict[str, str] = {}
        annotation: dict = {}
        while not parser.check("}") and parser.peek().kind != "eof":
            if parser.accept("annotation"):
                annotation = _literal_map(parser.map_literal())
                continue
            value = parser.advance().value
            if value != ",":
                if not annotation.get("Hidden"):
                    values[value] = annotation.get("Name", value)
                annotation = {}
        parser.expect("}")
        self.enums.setdefault(name, EnumType(name, values))

    def _constant(self, parser: Parser, tokens: list[Token], annotation: Node | None) -> None:
        parser.advance()
        name = parser.advance().value
        if parser.accept("is"):
            parser.type_name()
        parser.expect("=")
        start = parser.position
        if parser.check("defineFeature"):
            self.features.setdefault(name, self._feature(parser, name, annotation))
            return
        self.constants.setdefault(name, (tokens, start))
        # Skip to the end of the constant
        while not parser.check(";") and parser.peek().kind != "eof":
            if parser.peek().value in ("(", "[", "{"):
                parser.skip_balanced()
            else:
                parser.advance()

    def _feature(self, parser: Parser, name: str, annotation: Node | None) -> Feature:
        parser.expect("defineFeature")
        parser.expect("(")
        parser.expect("function")
        parser.skip_balanced()  # The parameters
        precondition = Node("block", ())
        if parser.accept("precondition"):
            precondition = parser.block()
        parser.skip_balanced()  # The body
        defaults = None
        if parser.accept(",") and parser.check("{"):
            defaults = parser.map_literal()
        return Feature(name, annotation, precondition, defaults)


def _literal_map(node: Node) -> dict:
    """Evaluates a map of literals (and enum values), e.g. an enum value's annotation."""
    return Evaluator(Declarations()).value(node, {}) or {}


_IMPORT = re.compile(r'(\w+\s*::\s*)?\bimport\s*\(\s*path\s*:\s*"([^"]+)"')


@dataclasses.dataclass
class ParsedFile:
    declarations: Declarations
    # (namespace, path) of each import
    imports: list[tuple[str, str]]


# Each file's parsed declarations, with the modification time and size (or the source) they were parsed from. Parsing
# the std library takes seconds, so the language server keeps these between renders.
_parsed_files: dict[pathlib.Path, tuple[object, ParsedFile]] = {}


def parse_file(path: pathlib.Path, source: str | None = None) -> ParsedFile | None:
    """A file's declarations and imports (from `source`, its unsaved contents, if given), cached."""
    if source is None:
        try:
            stat = path.stat()
        except OSError:
            return None
        key: object = (stat.st_mtime_ns, stat.st_size)
    else:
        key = ("source", source)
    cached = _parsed_files.get(path)
    if cached is not None and cached[0] == key:
        return cached[1]
    if source is None:
        source = path.read_text(encoding="utf-8", errors="replace")
    declarations = Declarations()
    declarations.add_source(source)
    parsed = ParsedFile(declarations, _IMPORT.findall(source))
    _parsed_files[path] = (key, parsed)
    return parsed


def load_declarations(
    project: Project, std_dir: pathlib.Path, path: pathlib.Path, sources: dict[pathlib.Path, str] | None = None
) -> Declarations:
    """Parses `path` and every file it imports, directly or not.

    Args:
        sources: Unsaved contents of files, by resolved path.
    """
    sources = sources or {}
    declarations = Declarations()
    element_ids = project.element_ids()
    seen: set[pathlib.Path] = set()
    pending = [path.resolve()]
    while pending:
        current = pending.pop(0).resolve()
        if current in seen:
            continue
        seen.add(current)
        parsed = parse_file(current, sources.get(current))
        if parsed is None:
            continue
        declarations.merge(parsed.declarations)
        for namespace, imported in parsed.imports:
            if namespace:
                continue
            if imported.startswith(STD_PREFIX):
                pending.append(std_dir / imported[len(STD_PREFIX) :])
            elif imported in element_ids:
                pending.append(element_ids[imported])
            elif imported.endswith(".fs"):
                pending.append(project.code_dir / imported)
    return declarations


# Evaluation


class Definition:
    """The `definition` map: each parameter's current value."""

    def __init__(self, values: dict[str, Any]) -> None:
        self.values = values


UNKNOWN = object()


class Evaluator:
    def __init__(self, declarations: Declarations) -> None:
        self.declarations = declarations
        self.constant_values: dict[str, Any] = {}

    def value(self, node: Node | None, scope: dict[str, Any]) -> Any:
        if node is None:
            return None
        kind = node.kind
        if kind == "literal":
            return node.args[0]
        if kind == "name":
            return self.name(node.args[0], scope)
        if kind == "map":
            result = {}
            for key, value in node.args:
                key_value = self.value(key, scope)
                if key_value is UNKNOWN:
                    continue
                result[key_value] = self.value(value, scope)
            return result
        if kind == "array":
            return [self.value(item, scope) for item in node.args]
        if kind == "member":
            target = self.value(node.args[0], scope)
            name = node.args[1]
            if isinstance(target, Definition):
                return target.values.get(name)
            if isinstance(target, EnumType):
                return EnumValue(target.name, name)
            if isinstance(target, dict):
                return target.get(name)
            return UNKNOWN
        if kind == "index":
            target = self.value(node.args[0], scope)
            key = self.value(node.args[1], scope)
            if isinstance(target, Definition):
                return target.values.get(key)
            if isinstance(target, dict):
                return target.get(key)
            if isinstance(target, list) and isinstance(key, float) and 0 <= key < len(target):
                return target[int(key)]
            return UNKNOWN
        if kind == "unary":
            operator, operand = node.args
            value = self.value(operand, scope)
            if operator == "!":
                return UNKNOWN if value is UNKNOWN else not value
            if operator == "-" and isinstance(value, float):
                return -value
            return value if operator == "+" else UNKNOWN
        if kind == "binary":
            return self.binary(*node.args, scope=scope)
        if kind == "ternary":
            condition = self.value(node.args[0], scope)
            if condition is UNKNOWN:
                return UNKNOWN
            return self.value(node.args[1] if condition else node.args[2], scope)
        if kind == "is":
            return UNKNOWN
        if kind == "as":
            return self.value(node.args[0], scope)
        if kind == "call":
            callee, arguments = node.args
            if callee.kind == "name" and callee.args[0] in self.declarations.predicates:
                return self.predicate_value(callee.args[0], arguments, scope)
            return UNKNOWN
        return UNKNOWN

    def name(self, name: str, scope: dict[str, Any]) -> Any:
        if name in scope:
            return scope[name]
        if name == "true":
            return True
        if name == "false":
            return False
        if name == "undefined":
            return None
        if name in UNITS:
            return Unit(name)
        if name in self.declarations.enums:
            return self.declarations.enums[name]
        if name in ("UIHint",):
            return EnumType(name, {})
        if name in self.declarations.constants:
            if name not in self.constant_values:
                self.constant_values[name] = UNKNOWN  # Guards against cycles
                tokens, start = self.declarations.constants[name]
                try:
                    self.constant_values[name] = self.value(Parser(tokens, start).expression(), {})
                except UiError:
                    pass
            return self.constant_values[name]
        if name[:1].isupper() and name.isidentifier():
            # An enum from a file that wasn't loaded, e.g. UIHint
            return EnumType(name, {})
        return UNKNOWN

    def binary(self, operator: str, left: Node, right: Node, scope: dict[str, Any]) -> Any:
        if operator in ("&&", "||"):
            first = self.value(left, scope)
            if first is UNKNOWN:
                return UNKNOWN
            if operator == "&&" and not first:
                return False
            if operator == "||" and first:
                return True
            return self.value(right, scope)
        a, b = self.value(left, scope), self.value(right, scope)
        if a is UNKNOWN or b is UNKNOWN:
            return UNKNOWN
        if operator == "==":
            return a == b
        if operator == "!=":
            return a != b
        if operator == "~":
            return f"{_text(a)}{_text(b)}"
        if isinstance(a, float) and isinstance(b, float):
            return {
                "+": lambda: a + b,
                "-": lambda: a - b,
                "*": lambda: a * b,
                "/": lambda: a / b if b else UNKNOWN,
                "%": lambda: a % b if b else UNKNOWN,
                "^": lambda: a**b,
                "<": lambda: a < b,
                ">": lambda: a > b,
                "<=": lambda: a <= b,
                ">=": lambda: a >= b,
            }[operator]()
        return UNKNOWN

    def predicate_value(self, name: str, arguments: Iterable[Node], scope: dict[str, Any]) -> Any:
        """The value of a predicate used as a condition: whether every statement in it holds."""
        predicate = self.declarations.predicates[name]
        inner = dict(zip(predicate.parameters, (self.value(a, scope) for a in arguments)))
        return self.statements_hold(predicate.body, inner)

    def statements_hold(self, node: Node, scope: dict[str, Any]) -> Any:
        if node.kind == "block":
            result: Any = True
            for statement in node.args:
                held = self.statements_hold(statement, scope)
                if held is False:
                    return False
                if held is UNKNOWN:
                    result = UNKNOWN
            return result
        if node.kind == "expression":
            return self.value(node.args[0], scope)
        if node.kind == "annotated":
            return self.statements_hold(node.args[1], scope)
        if node.kind == "if":
            condition = self.value(node.args[0], scope)
            if condition is UNKNOWN:
                return UNKNOWN
            branch = node.args[1] if condition else node.args[2]
            return True if branch is None else self.statements_hold(branch, scope)
        return True


def _text(value: Any) -> str:
    if isinstance(value, float):
        return f"{value:g}"
    return str(value)


# The dialog


@dataclasses.dataclass
class Parameter:
    name: str
    kind: str  # enum, boolean, query, length, angle, integer, real, lookup, string, reference, other
    annotation: dict
    value: Any = None
    enum: EnumType | None = None
    # For lookup tables, the (label, choice, and choices) of each level
    levels: list[tuple[str, str, list[str]]] = dataclasses.field(default_factory=list)
    # For arrays, how many items to show, and each one's parameters
    count: int = 0
    items: list[list] = dataclasses.field(default_factory=list)
    # What sets it with `fs ui --set`: its name, or for an array item's parameter, like `items.0.length`
    key: str = ""

    def __post_init__(self) -> None:
        self.key = self.key or self.name

    @property
    def label(self) -> str:
        return str(self.annotation.get("Name", self.name))

    @property
    def hints(self) -> set[str]:
        hints = self.annotation.get("UIHint", [])
        if not isinstance(hints, list):
            hints = [hints]
        return {hint.value if isinstance(hint, EnumValue) else str(hint) for hint in hints}


@dataclasses.dataclass
class Group:
    name: str
    annotation: dict
    children: list = dataclasses.field(default_factory=list)


VALUE_CHECKS = {
    "isLength": "length",
    "isAngle": "angle",
    "isInteger": "integer",
    "isReal": "real",
    "isAnything": "other",
}


class DialogBuilder:
    """Walks a precondition, collecting the parameters (and groups) Onshape would show."""

    def __init__(self, declarations: Declarations, overrides: dict[str, str]) -> None:
        self.declarations = declarations
        self.evaluator = Evaluator(declarations)
        self.overrides = overrides
        self.definition = Definition({})
        self.warnings: list[str] = []
        self.declared: set[str] = set()
        self.arrays: dict[str, Parameter] = {}
        # The prefix of the keys (see Parameter.key) of the parameters being declared, inside an array's item
        self.prefix = ""
        # The keys of the parameters shown, and of every parameter, to warn about settings which do nothing
        self.keys: set[str] = set()
        self.all_keys: set[str] = set()
        # Whether to walk both branches of every if, to declare every parameter (see build)
        self.every_branch = False

    def build(self, feature: Feature) -> list:
        if feature.defaults is not None:
            defaults = self.evaluator.value(feature.defaults, {})
            if isinstance(defaults, dict):
                for key, value in defaults.items():
                    if isinstance(key, str) and value is not UNKNOWN:
                        self.definition.values[key] = value
        # The definition is a map with a key for every parameter, which has a value (its default, until it's set) even
        # where it isn't shown. So first every parameter is declared, down every branch, for conditions which read
        # ones declared after them or in other branches; then the dialog is walked as Onshape shows it.
        warnings = list(self.warnings)
        self.every_branch = True
        self.walk(feature.precondition, {"definition": self.definition}, [], {})
        self.every_branch = False
        self.warnings, self.declared, self.arrays = warnings, set(), {}
        self.all_keys, self.keys = self.keys, set()
        items: list = []
        self.walk(feature.precondition, {"definition": self.definition}, items, {})
        return items

    def walk(self, node: Node, scope: dict[str, Any], items: list, annotation: dict) -> None:
        kind = node.kind
        if kind == "block":
            for statement in node.args:
                self.walk(statement, scope, items, {})
        elif kind == "annotated":
            value = self.evaluator.value(node.args[0], scope)
            annotation = value if isinstance(value, dict) else {}
            if node.args[0].kind == "map":
                for key, entry in node.args[0].args:
                    if key.kind == "literal" and key.args[0] == "Filter":
                        # Usually too complex to evaluate, but its text says what it accepts
                        annotation = {**annotation, FILTER_TEXT: describe(entry)}
            inner = node.args[1]
            if "Group Name" in annotation and inner.kind == "block":
                group = Group(str(annotation["Group Name"]), annotation)
                items.append(group)
                self.walk(inner, scope, group.children, {})
            else:
                self.walk(inner, scope, items, annotation)
        elif kind == "if" and self.every_branch:
            for branch in node.args[1:]:
                if branch is not None:
                    self.walk(branch, scope, [], {})
        elif kind == "if":
            condition = self.evaluator.value(node.args[0], scope)
            if condition is UNKNOWN:
                self.warnings.append(f"Couldn't decide if ({describe(node.args[0])}); showing its first branch.")
                condition = True
            branch = node.args[1] if condition else node.args[2]
            if branch is not None:
                self.walk(branch, scope, items, {})
            if not condition:
                # Onshape still shows the header of a group driven by a parameter which is off
                items.extend(self.driving_groups(node.args[1], scope))
        elif kind == "declare":
            name, value = node.args
            scope[name] = self.evaluator.value(value, scope)
        elif kind == "expression":
            self.expression(node.args[0], scope, items, annotation)
        elif kind == "loop":
            body, variable, iterable = node.args
            name = self.parameter_name(iterable, scope) if iterable is not None else None
            if name in self.arrays:
                self.array_items(self.arrays[name], body, variable, scope)

    def driving_groups(self, node: Node, scope: dict[str, Any]) -> list[Group]:
        """The groups with driving parameters directly in a block (or statement), empty."""
        statements = node.args if node.kind == "block" else (node,)
        groups = []
        for statement in statements:
            if statement.kind == "annotated" and statement.args[1].kind == "block":
                annotation = self.evaluator.value(statement.args[0], scope)
                if isinstance(annotation, dict) and "Group Name" in annotation and annotation.get("Driving Parameter"):
                    groups.append(Group(str(annotation["Group Name"]), annotation))
        return groups

    def array_items(self, array: Parameter, body: Node, variable: str, scope: dict[str, Any]) -> None:
        """Walks an array parameter's loop once for each item, as each item's parameters are declared in it."""
        outer = self.definition, self.declared, self.prefix
        for index in range(array.count):
            self.definition, self.declared = Definition({}), set()
            self.prefix = f"{array.key}.{index}."
            children: list = []
            self.walk(body, {**scope, variable: self.definition}, children, {})
            array.items.append(children)
        self.definition, self.declared, self.prefix = outer

    def expression(self, node: Node, scope: dict[str, Any], items: list, annotation: dict) -> None:
        if node.kind == "is":
            target, type_name = node.args
            name = self.parameter_name(target, scope)
            if name is not None:
                self.declare(name, type_name, annotation, items)
            return
        if node.kind != "call":
            return
        callee, arguments = node.args
        if callee.kind != "name":
            return
        function = callee.args[0]
        if function in VALUE_CHECKS and arguments:
            name = self.parameter_name(arguments[0], scope)
            if name is not None:
                bounds = self.evaluator.value(arguments[1], scope) if len(arguments) > 1 else None
                self.declare(name, VALUE_CHECKS[function], annotation, items, bounds)
            return
        predicate = self.declarations.predicates.get(function)
        if predicate is not None:
            inner = dict(zip(predicate.parameters, (self.evaluator.value(a, scope) for a in arguments)))
            self.walk(predicate.body, inner, items, {})

    def parameter_name(self, node: Node, scope: dict[str, Any]) -> str | None:
        if node.kind == "member":
            target, name = node.args
        elif node.kind == "index":
            target = node.args[0]
            name = self.evaluator.value(node.args[1], scope)
        else:
            return None
        if isinstance(self.evaluator.value(target, scope), Definition) and isinstance(name, str):
            return name
        return None

    def declare(self, name: str, type_name: str, annotation: dict, items: list, bounds: Any = None) -> None:
        if name in self.declared:
            return
        self.declared.add(name)
        key = self.prefix + name
        self.keys.add(key)
        parameter = Parameter(name, "other", annotation, key=key)
        enum = self.declarations.enums.get(type_name)
        if enum is not None:
            parameter.kind = "enum"
            parameter.enum = enum
            default = annotation.get("Default")
            value = default.value if isinstance(default, EnumValue) else next(iter(enum.values), None)
            if key in self.overrides:
                value = self.overrides[key]
                if value not in enum.values:
                    raise UiError(f"{name} is a {enum.name}, which has no value {value}.")
            if key not in self.overrides and isinstance(self.definition.values.get(name), EnumValue):
                value = self.definition.values[name].value
            parameter.value = value
            self.definition.values[name] = EnumValue(enum.name, value)
        elif type_name == "boolean":
            parameter.kind = "boolean"
            value = bool(annotation.get("Default", self.definition.values.get(name, False)))
            if key in self.overrides:
                value = self.overrides[key].lower() in ("true", "1", "yes")
            parameter.value = value
            self.definition.values[name] = value
        elif type_name == "Query":
            parameter.kind = "query"
        elif type_name == "LookupTablePath":
            parameter.kind = "lookup"
            table = annotation.get("Lookup Table")
            override = self.overrides.get(key)
            choices = [part.strip() for part in override.split(">")] if override else []
            parameter.levels = lookup_levels(table, choices) if isinstance(table, dict) else []
        elif type_name == "array":
            parameter.kind = "array"
            count = self.overrides.get(key, "0")
            if not count.isdigit():
                raise UiError(f"{name} is an array, so --set it to how many items to show, not {count}.")
            parameter.count = int(count)
            self.arrays[name] = parameter
        elif type_name in ("PartStudioData",):
            parameter.kind = "reference"
        elif type_name == "string":
            parameter.kind = "string"
            parameter.value = self.overrides.get(key, annotation.get("Default", ""))
        elif type_name in ("length", "angle", "integer", "real"):
            parameter.kind = type_name
            parameter.value = self.overrides.get(key) or format_bounds(type_name, bounds)
        items.append(parameter)


def describe(node: Node) -> str:
    """Roughly the code an expression was parsed from, for messages."""
    kind, args = node.kind, node.args
    if kind == "literal":
        return repr(args[0]) if isinstance(args[0], str) else _text(args[0])
    if kind == "name":
        return args[0]
    if kind == "member":
        return f"{describe(args[0])}.{args[1]}"
    if kind == "index":
        return f"{describe(args[0])}[{describe(args[1])}]"
    if kind == "call":
        return f"{describe(args[0])}({', '.join(describe(a) for a in args[1])})"
    if kind == "unary":
        return f"{args[0]}{describe(args[1])}"
    if kind == "binary":
        return f"{describe(args[1])} {args[0]} {describe(args[2])}"
    if kind == "group":
        return f"({describe(args[0])})"
    return "..."


def lookup_levels(table: dict, choices: list[str]) -> list[tuple[str, str, list[str]]]:
    """The label, choice, and choices of each level of a lookup table, following `choices` (then defaults)."""
    levels = []
    node: Any = table
    while isinstance(node, dict) and isinstance(node.get("entries"), dict):
        entries = node["entries"]
        if not entries:
            break
        index = len(levels)
        choice = choices[index] if index < len(choices) else node.get("default")
        if choice not in entries:
            if index < len(choices):
                raise UiError(f"The lookup table has no {choices[index]!r} at level {index + 1}; choose one of {list(entries)}.")
            choice = next(iter(entries))
        levels.append((str(node.get("displayName", node.get("name", ""))), str(choice), [str(entry) for entry in entries]))
        node = entries[choice]
    return levels


DISPLAY_UNITS = {"length": ("inch", "in"), "angle": ("degree", "deg")}


def format_bounds(kind: str, bounds: Any) -> str:
    if not isinstance(bounds, dict):
        return ""
    if kind in DISPLAY_UNITS:
        unit, suffix = DISPLAY_UNITS[kind]
        value = bounds.get(Unit(unit))
        if isinstance(value, list) and len(value) == 3:
            value = value[1]
        return f"{value:g} {suffix}" if isinstance(value, float) else ""
    value = bounds.get(Unit("unitless"))
    if isinstance(value, list) and len(value) == 3:
        return f"{value[1]:g}"
    return ""


# Rendering
#
# The dialog is written with Onshape's own markup (its elements and classes, from a saved Onshape page; see
# fs_cli/onshape_ui/extract.py), and styled with Onshape's own styles, so it looks the same. Controls carry
# `data-set` (the parameter, or for an array's items `items.0.name`) and `data-value` attributes saying what clicking
# them sets, for the VS Code extension's preview to change them (see vscode-extension/src/uiPreview.ts).


ONSHAPE_UI = pathlib.Path(__file__).resolve().parent / "onshape_ui"

# Icons from onshape_icons/, by name (see its index.html), for icons Onshape's page doesn't define
ICON_DIR = pathlib.Path(__file__).resolve().parents[1] / "onshape_icons"

# Icons Onshape's page defines (see onshape_ui/icons.svg), by name
SPRITE_ICONS = {
    "hole/diameter": "hole-diameter",
    "hole/depth": "hole-depth",
    "hole/tapDrillDiameter": "hole-tap-diameter",
}

# Buttons for enum and boolean parameters with these UI hints, by the icon Onshape shows
BUTTONS = {
    "OPPOSITE_DIRECTION": "flip-direction-opposite",
    "OPPOSITE_DIRECTION_CIRCULAR": "flip-rotation-opposite",
    "PRIMARY_AXIS": "flip-direction-opposite",
    "MATE_CONNECTOR_AXIS_TYPE": "realign-mate-dialog",
}

# Parameters Onshape shows with an icon instead of a label, by id (std's versioned ids, like holeDiameterV2, too)
PARAMETER_ICONS = {
    "holeDiameter": "hole/diameter",
    "holeDepth": "hole/depth",
    "cBoreDiameter": "hole/counterboreDiameter",
    "cBoreDepth": "hole/counterboreDepth",
    "cSinkDiameter": "hole/countersinkDiameter",
    "cSinkAngle": "hole/countersinkAngle",
    "tapDrillDiameter": "hole/tapDrillDiameter",
    "tappedDepth": "hole/tappedDepth",
}

# Icons chosen with a parameter's "Icon" annotation, by std's Icon enum value
ICON_VALUES = {
    "HOLE_DIAMETER": "hole/diameter",
    "HOLE_DEPTH": "hole/depth",
    "HOLE_DRILL_ANGLE": "hole/drillAngle",
    "HOLE_COUNTERBORE_DIAMETER": "hole/counterboreDiameter",
    "HOLE_COUNTERBORE_DEPTH": "hole/counterboreDepth",
    "HOLE_COUNTERSINK_DIAMETER": "hole/countersinkDiameter",
    "HOLE_COUNTERSINK_ANGLE": "hole/countersinkAngle",
    "HOLE_TAP_DIAMETER": "hole/tapDrillDiameter",
    "HOLE_TAPPED_DEPTH": "hole/tappedDepth",
    "HOLE_TAP_CLEARANCE": "hole/tapClearance",
}

# The annotation key holding the source of a query's filter
FILTER_TEXT = "__filter"

# Styles of our own, on top of Onshape's: the page around the dialog, and open dropdowns (which Onshape's page didn't
# have open)
PAGE_STYLE = """
html, body { margin: 0; }
body { padding: 10px; background: var(--os-graphics-background, #1d1d1d); }
html[data-os-theme="light"] body { background: #e8e8e8; }
#feature-dialog { position: relative; inset: auto; width: 278px; pointer-events: auto; }
#feature-dialog .ns-parameter-list { margin: 0; }
#feature-dialog [data-set], #feature-dialog [data-toggle], #feature-dialog .os-select-toggle { cursor: pointer; }
#feature-dialog .os-select-dropdown.open { display: block; opacity: 1; position: absolute; z-index: 1000; max-height: 300px; overflow-y: auto; }
#feature-dialog .os-select-choices-row { padding: 2px 10px; white-space: nowrap; color: var(--os-text-primary); }
#feature-dialog .os-select-choices-row:hover, #feature-dialog .os-select-choices-row.active { background: var(--os-select-active-background, var(--os-accent-quaternary)); }
#feature-dialog .os-svg-icon.expanded { transform: rotate(90deg); }
#feature-dialog .os-parameter-icon image { width: 100%; height: 100%; }
"""

_inlined = itertools.count()


def icon(name: str) -> str:
    """An icon's SVG, inline. Its ids are made unique, since icons often reuse the same ones (like `id="a"`) and
    references to them would find another icon's on the page."""
    path = ICON_DIR / f"{name}.svg"
    if not path.is_file():
        return ""
    svg = re.sub(r"<\?xml[^>]*>", "", path.read_text())
    prefix = f"i{next(_inlined)}-"
    svg = re.sub(r'\bid="([^"]+)"', lambda match: f'id="{prefix}{match[1]}"', svg)
    return re.sub(r'(href="#|url\(#)([^")]+)', lambda match: f"{match[1]}{prefix}{match[2]}", svg)


def sprite(name: str, attributes: str = "") -> str:
    """An icon from Onshape's page (onshape_ui/icons.svg), by its symbol's name without `svg-icon-`."""
    return f"<svg{attributes}><use href='#svg-icon-{name}'></use></svg>"


def parameter_icon(name: str, annotation: dict) -> str | None:
    """The icon a parameter is shown with instead of its label, if any."""
    chosen = annotation.get("Icon")
    if isinstance(chosen, EnumValue):
        return ICON_VALUES.get(chosen.value)
    return PARAMETER_ICONS.get(re.sub(r"V\d+$", "", name))


def _attribute(text: Any) -> str:
    return html.escape(_text(text), quote=True)


def _setting(key: str, value: Any) -> str:
    """Attributes saying what clicking a control sets."""
    return f" data-set='{_attribute(key)}' data-value='{_attribute(value)}'"


def _is_button(item: Parameter) -> bool:
    """Whether a parameter is shown as an icon button (like a flip button), in the right column."""
    return item.kind in ("boolean", "enum") and any(hint in BUTTONS for hint in item.hints)


def _allows_mate_connectors(filter: str) -> bool:
    """Whether a query's filter accepts mate connectors, as `BodyType.MATE_CONNECTOR` and some compound filters do."""
    return any(name in filter for name in ("MATE_CONNECTOR", "ALLOWS_AXIS", "ALLOWS_PLANE", "ALLOWS_VERTEX"))


def _driving_parameters(items: list) -> dict[str, Parameter]:
    """The parameters among `items` which drive groups among them (shown in the groups' headers), by name."""
    driving = {item.annotation.get("Driving Parameter") for item in items if isinstance(item, Group)}
    return {item.name: item for item in items if isinstance(item, Parameter) and item.name in driving}


def _is_checkbox(item: Parameter) -> bool:
    return item.kind == "boolean" and not _is_button(item)


class Renderer:
    """Writes a dialog's parameters with Onshape's markup."""

    def __init__(self, theme: str = "dark") -> None:
        self.theme = theme
        self.focused = False

    def page(self, title: str, items: list) -> str:
        style = (ONSHAPE_UI / "dialog.css").read_text() + PAGE_STYLE
        return (
            f"<!doctype html><html data-os-theme='{self.theme}'><head><meta charset='utf-8'><style>{style}</style></head>"
            f"<body>{(ONSHAPE_UI / 'icons.svg').read_text()}"
            "<div id='feature-dialog' class='ns-dialog-panel feature-dialog feature-dialog-resize has-resized'><div class='feature-dialog-main ns-dialog-frame'>"
            "<div class='ns-dialog-header'><div class='ns-drag-area'><div class='ns-dialog-title-container'>"
            f"<span class='ns-dialog-title'>{html.escape(title)}</span></div></div>"
            "<div class='ns-dialog-button-ok button-ok'><osc-svg-icon class='osc-svg-vertical-align' style='display: inline-flex;'>"
            f"{sprite('ok-button', ' height=24 width=28')}</osc-svg-icon></div>"
            "<div class='ns-dialog-button-cancel backbone-cancel'><osc-svg-icon class='osc-svg-vertical-align' style='display: inline-flex;'>"
            f"{sprite('cancel-button', ' height=24 width=28')}</osc-svg-icon></div></div>"
            "<div class='ns-dialog-fixed-content'><ul class='ns-parameter-list'><os-parameter-list-view>"
            + self.items(items)
            + "</os-parameter-list-view></ul>"
            "<div class='ns-preview-control'><div><div class='clearfix'></div><div class='ns-preview-slider-container'>"
            "<div class='ns-preview-alpha-slider active noUi-target'><div class='noUi-base noUi-background noUi-horizontal'>"
            "<div class='noUi-origin noUi-origin-lower' style='left: 70%;'><div class='noUi-handle noUi-handle-lower'></div></div>"
            "</div></div></div><div class='clearfix'></div></div></div><div class='clearfix'></div>"
            "</div></div></div></body></html>"
        )

    def items(self, items: list) -> str:
        """A list of parameters and groups: runs of parameters are each a parameter group, as are groups."""
        parts = []
        run: list[Parameter] = []
        driving_parameters = _driving_parameters(items)
        driving = set(driving_parameters)

        def flush() -> None:
            if run:
                parts.append(f"<os-parameter-group>{self.parameters(run)}</os-parameter-group>")
                run.clear()

        for item in items:
            if isinstance(item, Group):
                flush()
                parts.append(f"<os-parameter-group>{self.group(item, driving_parameters)}</os-parameter-group>")
            elif item.name not in driving and "ALWAYS_HIDDEN" not in item.hints:
                run.append(item)
        flush()
        return "".join(parts)

    def parameters(self, items: list[Parameter]) -> str:
        return "".join(self.list_item(item, items[index + 1] if index + 1 < len(items) else None) for index, item in enumerate(items))

    def group(self, group: Group, driving_parameters: dict[str, Parameter]) -> str:
        """A collapsible group, with its driving parameter (a checkbox) in its header, if it has one."""
        driving = driving_parameters.get(group.annotation.get("Driving Parameter"))
        enabled = driving is None or bool(driving.value)
        expanded = enabled and group.annotation.get("Collapsed By Default") is not True
        expander = (
            f"<div class='node-expander-wrapper'><node-expander class='os-param-group-expander{'' if enabled else ' node-expander-disabled'}'"
            f"{' data-toggle' if enabled else ''}>"
            f"<div class='os-center-content'>{sprite('collapsed', f' class=\"os-svg-icon{' expanded' if expanded else ''}\"')}</div>"
            "</node-expander></div>"
        )
        if driving is None:
            name = f"<span class='os-param-group-name'>{html.escape(group.name)}</span>"
        else:
            name = (
                f"<div class='os-param-group-driving-parameter os-parameter-list-item' data-parameter-id='{_attribute(driving.name)}'>"
                f"{self.boolean(driving)}</div>"
            )
        nested_driving = _driving_parameters(group.children)
        rows = self.subgroup_rows(
            [
                item
                for item in group.children
                if isinstance(item, Group) or (item.name not in nested_driving and "ALWAYS_HIDDEN" not in item.hints)
            ],
            nested_driving,
        )
        return (
            f"<div class='os-param-group-collapsible-container' data-group='{_attribute(group.name)}'>"
            f"<div class='os-param-group-header' data-driving-parameter-id='{_attribute(driving.name if driving else '')}'>"
            f"{expander}{name}</div>"
            f"<div class='os-param-group-collapsible-contents{'' if expanded else ' ng-hide'}'>{rows}</div></div>"
        )

    def subgroup_rows(self, items: list, driving_parameters: dict[str, Parameter]) -> str:
        """A group's parameters and nested groups, in rows of the indent line beside them: as Onshape splits them,
        each checkbox (and the short parameters beside it) and each nested group is a row of its own, and the last
        row ends the line."""
        rows: list[list] = []
        for item in items:
            previous = rows[-1][0] if rows else None
            if isinstance(item, Group) or previous is None or isinstance(previous, Group):
                rows.append([item])
                continue
            joins_checkbox = (
                _is_checkbox(previous) and "DISPLAY_SHORT" in item.hints and "FIRST_IN_ROW" not in item.hints
            )
            if joins_checkbox or (not _is_checkbox(item) and not _is_checkbox(previous)):
                rows[-1].append(item)
            else:
                rows.append([item])
        return "".join(
            "<div class='os-param-subgroup-row'>"
            f"<div class='os-param-group-indent {'node-indent-line-end' if index == len(rows) - 1 else 'node-indent-line'}'></div>"
            "<os-parameter-group>"
            + (self.group(row[0], driving_parameters) if isinstance(row[0], Group) else self.parameters(row))
            + "</os-parameter-group></div>"
            for index, row in enumerate(rows)
        )

    def list_item(self, item: Parameter, following: Parameter | None) -> str:
        """A parameter in a list, with the classes which lay it out (as Onshape's do)."""
        classes = ["os-parameter-list-item"]
        hints = item.hints
        fills_both = item.kind in ("query", "reference", "lookup", "array") or (
            item.kind == "enum" and "HORIZONTAL_ENUM" in hints
        )
        if _is_button(item):
            classes.append("os-param-fits-in-right-column")
        elif "DISPLAY_SHORT" in hints:
            classes.append("os-param-display-short")
        else:
            if not fills_both or (following is not None and _is_button(following)):
                classes.append("os-param-fill-first-column")
            if fills_both:
                classes.append("os-param-fill-both-columns")
        if item.kind in ("query", "reference", "array"):
            classes.append("os-param-requires-margin")
        return (
            f"<div class='{' '.join(classes)}' data-parameter-id='{_attribute(item.name)}'"
            f"{' title=' + chr(39) + _attribute(item.annotation['Description']) + chr(39) if item.annotation.get('Description') else ''}>"
            f"{self.parameter(item)}</div>"
        )

    def parameter(self, item: Parameter) -> str:
        if item.kind == "enum" and item.enum is not None:
            return self.enum(item)
        if item.kind == "boolean":
            return self.boolean(item)
        if item.kind in ("query", "reference"):
            return self.query(item)
        if item.kind == "lookup":
            return self.lookup(item)
        if item.kind == "array":
            return self.array(item)
        if item.kind == "string":
            return (
                "<osx-string-parameter data-parameter-type='os-string-parameter'>"
                f"<span class='os-param-wrapper os-param-container' data-parameter-id='{_attribute(item.name)}'>"
                f"<label class='os-param-label'>{html.escape(item.label)}</label>"
                f"<input type='text' class='os-param-text os-param-form-item' value='{_attribute(item.value or '')}' data-set='{_attribute(item.key)}'>"
                "</span></osx-string-parameter>"
            )
        if item.kind in ("length", "angle", "integer", "real"):
            return self.quantity(item)
        return f"<span class='os-param-wrapper os-param-container'><label class='os-param-label'>{html.escape(item.label)}</label></span>"

    def label(self, item: Parameter) -> str:
        chosen = parameter_icon(item.name, item.annotation)
        if chosen is None:
            return f"<label class='os-param-label'><span>{html.escape(item.label)}</span></label>"
        if chosen in SPRITE_ICONS:
            graphic = sprite(SPRITE_ICONS[chosen], " class='os-parameter-icon os-svg-icon'")
        else:
            graphic = f"<span class='os-parameter-icon os-svg-icon'>{icon(chosen)}</span>"
        return f"<label class='ns-parameter-label icon-label' title='{_attribute(item.label)}'>{graphic}</label>"

    def quantity(self, item: Parameter) -> str:
        hints = item.hints
        read_only = "READ_ONLY" in hints
        tolerant = "CAN_BE_TOLERANT" in hints
        expander = (
            f"<node-expander class='node-expander-disabled pe-none'><div class='os-center-content'>{sprite('collapsed', ' class=os-svg-icon')}</div></node-expander>"
            if tolerant
            else ""
        )
        # Short parameters share a row, without labels
        label = "" if "DISPLAY_SHORT" in hints else self.label(item)
        toggle = (
            f"<button class='parameter-state-toggle is-button pe-auto'>{sprite('hole-tolerance-precision', ' class=os-svg-icon')}</button>"
            if tolerant
            else ""
        )
        return (
            "<os-quantity-parameter data-parameter-type='os-quantity-parameter'>"
            f"<div class='os-param-wrapper os-param-container{' os-param-readonly' if read_only else ''}' data-parameter-id='{_attribute(item.name)}'>"
            f"{expander}{label}<div class='quantity-autocomplete-holder dropdown'>"
            f"<input class='os-param-number dropdown-source os-param-form-item' type='text' value='{_attribute(item.value or '')}'"
            f"{' readonly' if read_only else f' data-set={chr(39)}{_attribute(item.key)}{chr(39)}'}>"
            f"</div>{toggle}</div></os-quantity-parameter>"
        )

    def boolean(self, item: Parameter) -> str:
        on = bool(item.value)
        toggle = _setting(item.key, "false" if on else "true")
        button = next((BUTTONS[hint] for hint in BUTTONS if hint in item.hints), None)
        if button is not None:
            return (
                "<osx-boolean-parameter data-parameter-type='os-boolean-parameter'>"
                f"<div class='os-param-wrapper os-param-container' data-parameter-id='{_attribute(item.name)}'>"
                f"<div class='os-inline-icon-button-wrapper'><osc-svg-icon class='os-param-icon-button os-inline-icon-button'"
                f" style='display: inline-flex;' title='{_attribute(item.label)}'{toggle}>{sprite(button, ' height=100% width=100%')}"
                "</osc-svg-icon></div></div></osx-boolean-parameter>"
            )
        return (
            "<osx-boolean-parameter data-parameter-type='os-boolean-parameter'>"
            f"<div class='os-param-wrapper os-param-container' data-parameter-id='{_attribute(item.name)}'>"
            f"<label class='os-param-checkbox'{toggle}>"
            f"<input type='checkbox' class='os-param-checkbox-input' data-parameter-value='{'true' if on else 'false'}'{' checked' if on else ''}>"
            f"<span class='os-checkbox-indicator'></span><span class='os-param-checkbox-label'>{html.escape(item.label)}</span>"
            "</label></div></osx-boolean-parameter>"
        )

    def enum(self, item: Parameter) -> str:
        assert item.enum is not None
        names = item.enum.values
        hints = item.hints
        if "MATE_CONNECTOR_AXIS_TYPE" in hints:
            # A button which steps through the values
            values = list(names)
            following = values[(values.index(item.value) + 1) % len(values)] if item.value in values else values[0]
            return (
                "<os-enum-parameter data-parameter-type='os-enum-parameter'>"
                f"<div class='os-param-container os-row' data-parameter-id='{_attribute(item.name)}' title='{_attribute(item.label)}'"
                f"{_setting(item.key, following)}>{sprite(BUTTONS['MATE_CONNECTOR_AXIS_TYPE'], ' class=' + chr(34) + 'os-param-icon-button ns-dialog-button-rotatexy os-svg-icon' + chr(34))}"
                "</div></os-enum-parameter>"
            )
        if "HORIZONTAL_ENUM" in hints:
            options = "".join(
                f"<div class='option{' os-active' if value == item.value else ''} os-param-form-item' data-enum-param-value='{_attribute(value)}'"
                f"{_setting(item.key, value)}><span>{html.escape(name)}</span></div>"
                for value, name in names.items()
            )
            return (
                "<os-enum-parameter data-parameter-type='os-enum-parameter'>"
                f"<span class='os-param-wrapper os-param-container' data-parameter-id='{_attribute(item.name)}'>"
                f"<div class='os-param-tabs'>{options}</div></span></os-enum-parameter>"
            )
        label = self.label(item) if "SHOW_LABEL" in hints else ""
        options = [(name, _setting(item.key, value)) for value, name in names.items()]
        select = self.select(item.name, names.get(item.value, item.value or ""), options, "select-input ")
        return (
            "<os-enum-parameter data-parameter-type='os-enum-parameter'><span class='os-param-wrapper os-param-select'>"
            f"{label}<div class='select-container-wrapper'>{select}</div></span></os-enum-parameter>"
        )

    def select(self, name: str, selected: str, options: list[tuple[str, str]], classes: str = "") -> str:
        """A dropdown, showing `selected`, with `options` (each its text and the attributes setting it)."""
        rows = "".join(f"<div class='os-select-choices-row'{setting}>{html.escape(text)}</div>" for text, setting in options)
        return (
            f"<div class='{classes}os-select-container os-select-bootstrap dropdown' data-parameter-id='{_attribute(name)}'>"
            "<div class='os-select-match'><span class='btn btn-secondary form-control os-select-toggle' style='outline: 0;'>"
            f"<span class='os-select-match-text float-start'><span>{html.escape(selected)}</span></span><i class='caret float-end'></i>"
            "</span></div>"
            "<span class='os-spinner-small os-spinner-spinning ng-hide'></span>"
            "<input type='search' class='form-control os-select-search ng-hide'>"
            f"<ul class='os-select-choices os-select-choices-content os-select-dropdown dropdown-menu ng-hide'>"
            f"<li class='os-select-choices-group'>{rows}</li></ul>"
            # Onshape's hidden parts, which affect the layout
            "<div class='os-select-no-choice'></div><os-select-single></os-select-single>"
            "<input class='os-select-focusser os-select-offscreen' type='text' tabindex='-1'></div>"
        )

    def lookup(self, item: Parameter) -> str:
        """A lookup table: a dropdown for each level. Choosing a value keeps the levels above it, and takes the
        defaults below."""
        rows = []
        for index, (level, choice, choices) in enumerate(item.levels):
            above = [chosen for _, chosen, _ in item.levels[:index]]
            options = [(option, _setting(item.key, " > ".join([*above, option]))) for option in choices]
            rows.append(
                "<tr class='os-param-lookup-table-selector-container'>"
                f"<td class='os-param-table-label'><span>{html.escape(level)}</span></td>"
                f"<td class='os-param-table-value'><div class='os-param-lookup-table-selector'>{self.select(level, choice, options)}</div></td></tr>"
            )
        return (
            "<os-lookup-table-parameter data-parameter-type='os-lookup-table-parameter'>"
            f"<table class='os-param-lookup-table' data-parameter-id='{_attribute(item.name)}'><tbody>{''.join(rows)}</tbody></table>"
            "</os-lookup-table-parameter>"
        )

    def query(self, item: Parameter) -> str:
        focus = not self.focused
        self.focused = True
        side = (
            "<osc-svg-icon class='query-side-button' style='display: inline-flex;' title='Create mate connector'>"
            f"{sprite('mate-connector-button', ' height=20 width=20')}</osc-svg-icon>"
            if _allows_mate_connectors(str(item.annotation.get(FILTER_TEXT, "")))
            else ""
        )
        return (
            "<os-query-list-parameter data-parameter-type='os-query-list-parameter'>"
            f"<div class='os-param-wrapper os-param-query-list-container os-param-container' data-parameter-id='{_attribute(item.name)}'>"
            "<div class='os-row os-grow'>"
            f"<div class='os-param-query-list os-param-selection-list os-grow os-param-query-list-resize{' os-param-query-list-focus' if focus else ''}'>"
            "<div class='slimScrollDiv' style='position: relative; overflow: hidden; width: auto;'>"
            "<div class='os-param-query-list-scroll-box' style='overflow: hidden; width: calc(100% + 20px); padding-right: 20px; height: auto; min-height: auto;'>"
            f"<div class='os-param-query-list-header'><label class='os-param-query-list-label os-grow'>{html.escape(item.label)}</label></div>"
            "<ul class='os-param-list-container'></ul></div></div></div>"
            f"{side}</div></div></os-query-list-parameter>"
        )

    def array(self, array: Parameter) -> str:
        """An array parameter: its items, each under a header labeled by its "Item label template", and a button to
        add one."""
        item_name = str(array.annotation.get("Item name", "item"))
        template = array.annotation.get("Item label template")
        entries = []
        for index, children in enumerate(array.items):
            values = {child.name: _text(child.value) for child in children if isinstance(child, Parameter)}
            label = f"{item_name.capitalize()} {index + 1}"
            if isinstance(template, str):
                label = re.sub(r"#(\w+)", lambda match: values.get(match[1], match[0]), template)
            entries.append(
                f"<li class='os-param-array-item' data-parent-parameter-id='{_attribute(array.name)}'>"
                "<div class='os-param-selection-list-entry'>"
                f"<node-expander class='os-param-array-item-expander hidden-on-init visible' data-toggle>"
                f"<div class='os-center-content'>{sprite('collapsed', ' class=' + chr(34) + 'os-svg-icon expanded' + chr(34))}</div></node-expander>"
                f"<span class='os-param-query-list-entry-text os-param-array-item-title'>{html.escape(label)}</span>"
                f"<span class='os-param-selection-list-entry-delete' data-remove='{_attribute(array.key)}' data-index='{index}'>&times;</span></div>"
                f"<div class='os-param-array-item-contents'><os-parameter-list-view>{self.items(children)}</os-parameter-list-view></div></li>"
            )
        return (
            "<os-array-parameter data-parameter-type='os-array-parameter'>"
            f"<div class='os-param-array os-param-container os-param-query-list-resize os-param-selection-list' data-parameter-id='{_attribute(array.name)}'>"
            "<div class='os-param-query-list-scroll-box os-param-array-list' style='overflow: hidden; width: calc(100% + 20px); padding-right: 20px; height: auto; min-height: auto;'>"
            "<div class='os-param-query-list-header'>"
            f"<label class='os-param-query-list-label os-grow os-param-query-list-small-label'>{html.escape(array.label)}</label>"
            f"<button class='os-param-query-list-reorder-button'>{sprite('reorder-items-button', ' class=os-svg-icon')}</button>"
            f"<button class='os-param-array-clear-button'{_setting(array.key, 0)}><small>CLEAR</small></button></div>"
            f"<ul class='os-param-list-container'>{''.join(entries)}</ul>"
            f"<button class='btn btn-secondary os-param-button os-param-array-add-button os-no-shrink'{_setting(array.key, array.count + 1)}>"
            f"Add {html.escape(item_name)}</button></div></div></os-array-parameter>"
        )


# Screenshots


def find_chromium() -> tuple[str, bool]:
    """The browser to screenshot with, and whether it's a headless shell.

    Headless shells (like Playwright's) size the page to exactly the window; full Chromium leaves
    room for its UI, so its screenshots get extra height.
    """
    if os.environ.get("CHROMIUM"):
        path = os.environ["CHROMIUM"]
        return path, "headless_shell" in path
    for shell in sorted(pathlib.Path("/opt/pw-browsers").glob("chromium_headless_shell-*/chrome-linux/headless_shell")):
        return str(shell), True
    for candidate in ("/opt/pw-browsers/chromium", *map(shutil.which, ("chromium", "chromium-browser", "google-chrome"))):
        if candidate and pathlib.Path(candidate).exists():
            return candidate, False
    raise UiError("Couldn't find Chromium to take the screenshot with; set CHROMIUM to its path.")


def screenshot(page: str, output: pathlib.Path, run: Callable = subprocess.run) -> None:
    """Saves a screenshot of an HTML page, cropped to its height."""
    chromium, exact = find_chromium()
    flags = ["--headless", "--no-sandbox", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=2"]
    with tempfile.TemporaryDirectory() as directory:
        measured = pathlib.Path(directory) / "measure.html"
        measured.write_text(
            page.replace(
                "</body>",
                "<script>document.title = document.querySelector('#feature-dialog').getBoundingClientRect().bottom + 10;</script></body>",
            )
        )
        dom = run(
            [chromium, *flags, "--dump-dom", measured.as_uri()],
            capture_output=True,
            text=True,
            timeout=120,
        ).stdout
        match = re.search(r"<title>([\d.]+)</title>", dom)
        height = int(float(match.group(1))) + 1 if match else 800
        if not exact:
            height += 90
        source = pathlib.Path(directory) / "dialog.html"
        source.write_text(page)
        run(
            [chromium, *flags, f"--window-size=298,{height}", f"--screenshot={output.resolve()}", source.as_uri()],
            capture_output=True,
            timeout=120,
        )
    if not output.is_file():
        raise UiError("Chromium didn't save the screenshot.")


def render_feature(
    project: Project,
    std_dir: pathlib.Path,
    path: pathlib.Path,
    feature_name: str | None,
    overrides: dict[str, str],
    theme: str = "dark",
    sources: dict[pathlib.Path, str] | None = None,
) -> tuple[str, list[str]]:
    """Returns the HTML of a feature's dialog (in Onshape's `dark` or `light` theme), and any warnings.

    Args:
        sources: Unsaved contents of files, by resolved path.
    """
    declarations = load_declarations(project, std_dir, path, sources)
    own = parse_file(path.resolve(), (sources or {}).get(path.resolve()))
    if own is None:
        raise UiError(f"Couldn't read {path}.")
    own = own.declarations
    if not own.features:
        raise UiError(f"{path.name} doesn't define a feature.")
    if feature_name is None:
        if len(own.features) > 1:
            raise UiError(f"{path.name} defines {', '.join(own.features)}; choose one with --feature.")
        feature_name = next(iter(own.features))
    if feature_name not in own.features:
        raise UiError(f"{path.name} doesn't define {feature_name}.")
    feature = declarations.features.get(feature_name, own.features[feature_name])
    builder = DialogBuilder(declarations, overrides)
    items = builder.build(feature)
    annotation = builder.evaluator.value(feature.annotation, {}) if feature.annotation else {}
    title = annotation.get("Feature Type Name", feature_name) if isinstance(annotation, dict) else feature_name
    # As Onshape names a new feature
    title = f"{title} 1"
    unused = set(overrides) - builder.keys
    warnings = builder.warnings + [
        f"{name} isn't shown, so --set {name} did nothing."
        if name in builder.all_keys
        else f"{name} isn't a parameter, so --set {name} did nothing."
        for name in sorted(unused)
    ]
    return Renderer(theme).page(str(title), items), warnings
