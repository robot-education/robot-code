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


def load_declarations(project: Project, std_dir: pathlib.Path, path: pathlib.Path) -> Declarations:
    """Parses `path` and every file it imports, directly or not."""
    declarations = Declarations()
    element_ids = project.element_ids()
    seen: set[pathlib.Path] = set()
    pending = [path.resolve()]
    while pending:
        current = pending.pop(0)
        if current in seen or not current.is_file():
            continue
        seen.add(current)
        source = current.read_text(encoding="utf-8", errors="replace")
        declarations.add_source(source)
        for namespace, imported in _IMPORT.findall(source):
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
    # For lookup tables, the (label, choice) of each level
    levels: list[tuple[str, str]] = dataclasses.field(default_factory=list)
    # For arrays, how many items to show, and each one's parameters
    count: int = 0
    items: list[list] = dataclasses.field(default_factory=list)

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

    def build(self, feature: Feature) -> list:
        if feature.defaults is not None:
            defaults = self.evaluator.value(feature.defaults, {})
            if isinstance(defaults, dict):
                for key, value in defaults.items():
                    if isinstance(key, str) and value is not UNKNOWN:
                        self.definition.values[key] = value
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
        elif kind == "if":
            condition = self.evaluator.value(node.args[0], scope)
            if condition is UNKNOWN:
                self.warnings.append(f"Couldn't decide if ({describe(node.args[0])}); showing its first branch.")
                condition = True
            branch = node.args[1] if condition else node.args[2]
            if branch is not None:
                self.walk(branch, scope, items, {})
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

    def array_items(self, array: Parameter, body: Node, variable: str, scope: dict[str, Any]) -> None:
        """Walks an array parameter's loop once for each item, as each item's parameters are declared in it."""
        outer = self.definition, self.declared
        for _ in range(array.count):
            self.definition, self.declared = Definition({}), set()
            children: list = []
            self.walk(body, {**scope, variable: self.definition}, children, {})
            array.items.append(children)
        self.definition, self.declared = outer

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
        parameter = Parameter(name, "other", annotation)
        enum = self.declarations.enums.get(type_name)
        if enum is not None:
            parameter.kind = "enum"
            parameter.enum = enum
            default = annotation.get("Default")
            value = default.value if isinstance(default, EnumValue) else next(iter(enum.values), None)
            if name in self.overrides:
                value = self.overrides[name]
                if value not in enum.values:
                    raise UiError(f"{name} is a {enum.name}, which has no value {value}.")
            if name not in self.overrides and isinstance(self.definition.values.get(name), EnumValue):
                value = self.definition.values[name].value
            parameter.value = value
            self.definition.values[name] = EnumValue(enum.name, value)
        elif type_name == "boolean":
            parameter.kind = "boolean"
            value = bool(annotation.get("Default", self.definition.values.get(name, False)))
            if name in self.overrides:
                value = self.overrides[name].lower() in ("true", "1", "yes")
            parameter.value = value
            self.definition.values[name] = value
        elif type_name == "Query":
            parameter.kind = "query"
        elif type_name == "LookupTablePath":
            parameter.kind = "lookup"
            table = annotation.get("Lookup Table")
            override = self.overrides.get(name)
            choices = [part.strip() for part in override.split(">")] if override else []
            parameter.levels = lookup_levels(table, choices) if isinstance(table, dict) else []
        elif type_name == "array":
            parameter.kind = "array"
            count = self.overrides.get(name, "0")
            if not count.isdigit():
                raise UiError(f"{name} is an array, so --set it to how many items to show, not {count}.")
            parameter.count = int(count)
            self.arrays[name] = parameter
        elif type_name in ("PartStudioData",):
            parameter.kind = "reference"
        elif type_name == "string":
            parameter.kind = "string"
            parameter.value = self.overrides.get(name, annotation.get("Default", ""))
        elif type_name in ("length", "angle", "integer", "real"):
            parameter.kind = type_name
            parameter.value = self.overrides.get(name) or format_bounds(type_name, bounds)
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


def lookup_levels(table: dict, choices: list[str]) -> list[tuple[str, str]]:
    """The label and choice of each level of a lookup table, following `choices` (then defaults)."""
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
        levels.append((str(node.get("displayName", node.get("name", ""))), str(choice)))
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


# Icons from onshape_icons/, by name (see its index.html)
ICON_DIR = pathlib.Path(__file__).resolve().parents[1] / "onshape_icons"

# Buttons for enum and boolean parameters with these UI hints: (icon, whether it's drawn dark and needs inverting)
BUTTONS = {
    "OPPOSITE_DIRECTION": ("dialog/flip", True),
    "OPPOSITE_DIRECTION_CIRCULAR": ("dialog/flipCircular", True),
    "PRIMARY_AXIS": ("dialog/rotate", True),
    "MATE_CONNECTOR_AXIS_TYPE": ("dialog/rotate", True),
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

# The button beside queries which accept mate connectors, to create one
MATE_CONNECTOR_ICON = "dialog/mateConnector"

# The button beside CAN_BE_TOLERANT values, to add a tolerance
TOLERANCE_ICON = "dialog/tolerance"

# The annotation key holding the source of a query's filter
FILTER_TEXT = "__filter"


_inlined = itertools.count()


def icon(name: str, invert: bool = False) -> str:
    """An icon's SVG, inline. Its ids are made unique, since icons often reuse the same ones (like `id="a"`) and
    references to them would find another icon's on the page."""
    path = ICON_DIR / f"{name}.svg"
    if not path.is_file():
        return ""
    svg = re.sub(r"<\?xml[^>]*>", "", path.read_text())
    prefix = f"i{next(_inlined)}-"
    svg = re.sub(r'\bid="([^"]+)"', lambda match: f'id="{prefix}{match[1]}"', svg)
    svg = re.sub(r'(href="#|url\(#)([^")]+)', lambda match: f"{match[1]}{prefix}{match[2]}", svg)
    return f"<span class='icon{' invert' if invert else ''}'>{svg}</span>"


def parameter_icon(name: str, annotation: dict) -> str | None:
    """The icon a parameter is shown with instead of its label, if any."""
    chosen = annotation.get("Icon")
    if isinstance(chosen, EnumValue):
        return ICON_VALUES.get(chosen.value)
    return PARAMETER_ICONS.get(re.sub(r"V\d+$", "", name))


STYLE = """
body { margin: 0; padding: 10px; background: #1b1b1b; font: 12px Roboto, "Helvetica Neue", Arial, sans-serif; color: #dcdcdc; }
.dialog { width: 236px; background: #2b2b2b; border: 1px solid #444; border-radius: 3px; box-shadow: 0 2px 8px rgba(0,0,0,.5); }
.header { display: flex; align-items: center; padding: 5px 6px 5px 8px; background: #333; border-bottom: 1px solid #444; font-size: 13px; color: #eee; }
.header .title { flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.header .ok { width: 24px; height: 22px; display: inline-flex; align-items: center; justify-content: center; background: #2f6f2a; color: #fff; border-radius: 2px; font-size: 14px; margin-left: 6px; }
.header .cancel { width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; color: #e04b3a; font-size: 14px; margin-left: 2px; }
.body { padding: 4px 6px 6px; }
.row { display: flex; align-items: center; min-height: 24px; margin: 3px 0; gap: 6px; }
.label { flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #d0d0d0; }
.label.right { text-align: right; }
.input, .select { min-width: 92px; height: 20px; border-bottom: 1px solid #8a8a8a; padding: 0 3px; display: flex; align-items: center; justify-content: flex-end; box-sizing: border-box; color: #eee; }
.select { justify-content: space-between; gap: 6px; }
.select.wide { flex: 1; width: auto; border: none; border-bottom: 1px solid #8a8a8a; }
.select::after { content: ""; border: 4px solid transparent; border-top: 5px solid #ccc; margin-top: 4px; }
.tabs { display: flex; flex: 1; border-bottom: 1px solid #444; }
.tab { flex: 1; text-align: center; padding: 4px 2px; white-space: nowrap; color: #ddd; }
.tab.selected { background: #3d5975; color: #a9d4ff; box-shadow: inset 0 -2px #5aa9f0; }
.query { flex: 1; min-height: 34px; border: 1px solid #3b7bc4; border-radius: 2px; padding: 3px 6px; color: #8fb8e6; background: #1f2732; box-sizing: border-box; }
.query.focus { border-color: #58a3f2; background: #22344a; color: #b7d8fb; }
.check { width: 13px; height: 13px; border: 1px solid #bbb; border-radius: 2px; display: inline-flex; align-items: center; justify-content: center; font-size: 10px; color: #fff; flex: none; }
.check.on { background: #3d8ee0; border-color: #3d8ee0; }
.button { width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; color: #ddd; flex: none; }
.icon { display: inline-flex; width: 18px; height: 18px; flex: none; }
.icon svg { width: 100%; height: 100%; }
.icon.invert { filter: invert(1); }
.icon.parameter { width: 22px; height: 22px; }
.group { margin: 4px 0 2px; }
.group-header { display: flex; align-items: center; gap: 6px; padding: 4px 0 3px; color: #ddd; }
.group-header::before { content: ""; width: 6px; height: 6px; border-right: 1.5px solid #bbb; border-bottom: 1.5px solid #bbb; transform: rotate(45deg); margin: 0 3px 3px 2px; }
.collapsed .group-header::before { transform: rotate(-45deg); margin-bottom: 0; }
.group-body { margin-left: 5px; padding-left: 8px; border-left: 1px solid #555; }
.short { display: flex; gap: 6px; flex: 1; align-items: center; }
.short .input { flex: 1; min-width: 0; }
.input.read-only { color: #8c8c8c; border-bottom-style: dotted; }
.chevron { width: 5px; height: 5px; border-right: 1.5px solid #bbb; border-bottom: 1.5px solid #bbb; transform: rotate(-45deg); margin: 0 2px 0 1px; flex: none; }
.chevron.open { transform: rotate(45deg); margin-bottom: 3px; }
.array { margin: 4px 0; }
.array-header { padding: 4px 0 3px; color: #ddd; }
.array-list { border: 1px solid #484848; border-radius: 2px; }
.array-item + .array-item { border-top: 1px solid #484848; }
.item-header { display: flex; align-items: center; gap: 6px; padding: 4px 6px; background: #313131; }
.item-header .label { color: #ddd; }
.item-header .remove { color: #999; font-size: 11px; }
.item-header .grip { width: 6px; height: 10px; flex: none; background: radial-gradient(circle, #888 1px, transparent 1.2px) 0 0 / 3px 3.4px; }
.item-body { padding: 0 6px 2px 18px; }
.array-add { display: flex; align-items: center; gap: 5px; padding: 5px 2px 2px; color: #79b4f0; }
.array-add::before { content: "+"; font-size: 15px; line-height: 12px; }
.slider { margin: 8px 6px 2px; height: 2px; background: #666; position: relative; }
.slider::after { content: ""; position: absolute; left: 52%; top: -5px; width: 10px; height: 10px; border-radius: 50%; background: #2b2b2b; border: 1.5px solid #ccc; }
"""


class Renderer:
    def __init__(self) -> None:
        self.focused = False

    def page(self, title: str, items: list) -> str:
        body = self.items(items)
        return (
            "<!doctype html><html><head><meta charset='utf-8'><style>"
            + STYLE
            + "</style></head><body><div class='dialog'><div class='header'><span class='title'>"
            + html.escape(title)
            + "</span><span class='ok'>&#x2714;</span><span class='cancel'>&#x2716;</span></div><div class='body'>"
            + body
            + "<div class='slider'></div></div></div></body></html>"
        )

    def items(self, items: list) -> str:
        rows: list[list[str]] = []
        driving = {
            item.annotation.get("Driving Parameter")
            for item in items
            if isinstance(item, Group) and item.annotation.get("Driving Parameter")
        }
        driving_values = {
            item.name: item.value for item in items if isinstance(item, Parameter) and item.name in driving
        }
        for item in items:
            if isinstance(item, Group):
                rows.append([self.group(item, driving_values)])
                continue
            if item.kind == "array":
                rows.append([self.array(item)])
                continue
            hints = item.hints
            if "ALWAYS_HIDDEN" in hints or item.name in driving:
                continue
            button = next((hint for hint in BUTTONS if hint in hints), None)
            if button and (item.kind in ("boolean", "enum")):
                control = f"<span class='button' title='{html.escape(item.label)}'>{icon(*BUTTONS[button])}</span>"
                if rows and "FIRST_IN_ROW" not in hints and not rows[-1][0].startswith("<div"):
                    rows[-1].append(control)
                else:
                    rows.append([control])
                continue
            if "DISPLAY_SHORT" in hints and rows and rows[-1][-1].startswith("<span class='short'>") and "FIRST_IN_ROW" not in hints:
                # Joins the row, which already has a label
                rows[-1].append(self.short(item, labeled=False))
                continue
            if "DISPLAY_SHORT" in hints:
                rows.append([self.short(item)])
                continue
            rows.extend([row] for row in self.parameter(item))
        return "".join(
            row[0] if row[0].startswith("<div") else "<div class='row'>" + "".join(row) + "</div>"
            for row in rows
        )

    def group(self, group: Group, driving_values: dict[str, Any]) -> str:
        driving = group.annotation.get("Driving Parameter")
        check = ""
        collapsed = group.annotation.get("Collapsed By Default") is True
        if driving:
            on = bool(driving_values.get(driving))
            check = _check(driving, on)
            # Its contents are only shown when it's checked
            collapsed = collapsed or not on
        return (
            f"<div class='group{' collapsed' if collapsed else ''}'><div class='group-header'>"
            + check
            + html.escape(group.name)
            + "</div><div class='group-body'>"
            + ("" if collapsed else self.items(group.children))
            + "</div></div>"
        )

    def array(self, array: Parameter) -> str:
        """An array parameter: its items, each under a header labeled by its "Item label template", and a button to
        add one."""
        item_name = str(array.annotation.get("Item name", "item"))
        template = array.annotation.get("Item label template")
        items = []
        for index, children in enumerate(array.items):
            values = {child.name: _text(child.value) for child in children if isinstance(child, Parameter)}
            label = f"{item_name.capitalize()} {index + 1}"
            if isinstance(template, str):
                label = re.sub(r"#(\w+)", lambda match: values.get(match[1], match[0]), template)
            items.append(
                "<div class='array-item'><div class='item-header'><span class='grip'></span><span class='chevron open'>"
                f"</span><span class='label'>{html.escape(label)}</span><span class='remove'>&#x2716;</span></div>"
                f"<div class='item-body'>{self.items(children)}</div></div>"
            )
        listed = f"<div class='array-list'>{''.join(items)}</div>" if items else ""
        return (
            f"<div class='array'><div class='array-header'>{html.escape(array.label)}</div>{listed}"
            f"<div class='array-add'>Add {html.escape(item_name)}</div></div>"
        )

    def short(self, item: Parameter, labeled: bool = True) -> str:
        """A parameter displayed short, sharing its row."""
        label = f"<span class='label'>{html.escape(item.label)}</span>" if labeled else ""
        if item.kind == "boolean":
            return f"<span class='short'>{_check(item.name, bool(item.value))}{label}</span>"
        return f"<span class='short'>{label}<span class='input'>{html.escape(_text(item.value or ''))}</span></span>"

    def parameter(self, item: Parameter) -> list[str]:
        label = f"<span class='label'>{html.escape(item.label)}</span>"
        chosen = parameter_icon(item.name, item.annotation)
        if chosen is not None:
            # In place of its label, which shows when it's hovered
            label = f"<span class='label' title='{html.escape(item.label)}'>{icon(chosen)}</span>"
        if item.kind == "enum" and item.enum is not None:
            names = item.enum.values
            if "HORIZONTAL_ENUM" in item.hints:
                tabs = "".join(
                    f"<span class='tab{' selected' if value == item.value else ''}'{_setting(item.name, value)}>"
                    f"{html.escape(name)}</span>"
                    for value, name in names.items()
                )
                return [f"<span class='tabs'>{tabs}</span>"]
            selected = html.escape(names.get(item.value, item.value or ""))
            options = _options(item.name, names)
            if "SHOW_LABEL" in item.hints:
                return [label.replace("class='label'", "class='label right'") + f"<span class='select'{options}>{selected}</span>"]
            return [f"<span class='select wide'{options}>{selected}</span>"]
        if item.kind == "boolean":
            return [_check(item.name, bool(item.value)) + label]
        if item.kind in ("query", "reference"):
            focus = not self.focused
            self.focused = True
            query = f"<span class='query{' focus' if focus else ''}'>{html.escape(item.label)}</span>"
            if "MATE_CONNECTOR" in str(item.annotation.get(FILTER_TEXT, "")):
                query += f"<span class='button' title='Create mate connector'>{icon(MATE_CONNECTOR_ICON)}</span>"
            return [query]
        if item.kind == "lookup":
            return [
                f"<span class='label right'>{html.escape(level)}</span><span class='select'>{html.escape(choice)}</span>"
                for level, choice in item.levels
            ]
        if item.kind in ("length", "angle", "integer", "real", "string"):
            read_only = " read-only" if "READ_ONLY" in item.hints else ""
            row = label + f"<span class='input{read_only}'>{html.escape(_text(item.value or ''))}</span>"
            if "CAN_BE_TOLERANT" in item.hints:
                # Expands to show the tolerance, once one's added with the button
                row = "<span class='chevron'></span>" + row
                row += f"<span class='button' title='Add tolerance'>{icon(TOLERANCE_ICON)}</span>"
            return [row]
        return [label]


def _setting(name: str, value: Any) -> str:
    """Attributes saying which parameter a control sets (with `fs ui --set`), and to what, for the VS Code extension's
    preview to change it when it's clicked."""
    return f" data-name='{html.escape(name)}' data-value='{html.escape(_text(value))}'"


def _options(name: str, names: dict) -> str:
    """Attributes listing an enum's values (as `value=Name` lines), for the preview to offer them."""
    listed = "\n".join(f"{value}={label}" for value, label in names.items())
    return f" data-name='{html.escape(name)}' data-options='{html.escape(listed)}'"


def _check(name: str, on: bool) -> str:
    """A checkbox, which sets its parameter to the opposite when clicked in the preview."""
    return (
        f"<span class='check{' on' if on else ''}'{_setting(name, 'false' if on else 'true')}>"
        f"{'&#x2714;' if on else ''}</span>"
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
                "<script>document.title = document.querySelector('.dialog').getBoundingClientRect().bottom + 12;</script></body>",
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
            [chromium, *flags, f"--window-size=326,{height}", f"--screenshot={output.resolve()}", source.as_uri()],
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
) -> tuple[str, list[str]]:
    """Returns the HTML of a feature's dialog, and any warnings."""
    declarations = load_declarations(project, std_dir, path)
    own = Declarations()
    own.add_source(path.read_text(encoding="utf-8", errors="replace"))
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
    unused = set(overrides) - builder.declared
    warnings = builder.warnings + [f"{name} isn't shown, so --set {name} did nothing." for name in sorted(unused)]
    return Renderer().page(str(title), items), warnings
