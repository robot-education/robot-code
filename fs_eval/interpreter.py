"""Runs FeatureScript: loads modules (and what they import, std included), and compiles their syntax trees into
Python closures, which run with FeatureScript's semantics (see `fs_eval.values`).

std's own FeatureScript runs as is: units, vectors, transforms, and so on are std's code, on top of the built-ins
(`@size`, `@sqrt`, ...) in `fs_eval.builtins`. Built-ins which model geometry (operations, evaluations) aren't
available; sketches are recorded rather than solved (see `fs_eval.builtins`).
"""

from __future__ import annotations

import hashlib
import json
import pathlib
import pickle
import re
import sys

from fs_eval import ast
from fs_eval.parser import parse, parse_expression
from fs_eval.values import (
    Box,
    Builtin,
    Context,
    EnumType,
    EnumValue,
    FSArray,
    FSMap,
    FSType,
    Tagged,
    equal,
    format_number,
    key_of,
    tag_of,
    type_name,
    untag,
)

STD_PREFIX = "onshape/std/"
ELEMENT_ID = re.compile(r"[0-9a-f]{24}")
BUILTIN_TYPES = frozenset(["number", "string", "boolean", "array", "map", "function", "undefined", "box", "builtin"])
CACHE_VERSION = 1


class FSError(Exception):
    """A FeatureScript error: a thrown value (`throw regenError(...)`), or an error of the language's own (like a
    failed precondition), whose value is its message. `trace` is where it happened, innermost last."""

    def __init__(self, value, trace: list[str] | None = None):
        super().__init__()
        self.value = value
        self.trace = trace or []

    @property
    def message(self) -> str:
        value = self.value
        if isinstance(value, FSMap):
            custom = value.get_str("customMessage")
            if custom is not None:
                return untag(custom)
            message = value.get_str("message")
            if isinstance(message, EnumValue):
                return message.name
        if isinstance(value, str):
            return value
        return to_display(value)

    def __str__(self) -> str:
        where = "".join(f"\n  at {frame}" for frame in reversed(self.trace))
        return self.message + where


def error(message: str, at=None) -> FSError:
    return FSError(message, [f"{at[0]}:{at[1]}"] if at else [])


def to_display(value) -> str:
    """A value as `~` makes it a string."""
    kind = type(value)
    if kind is str:
        return value
    if kind is float:
        return format_number(value)
    if kind is bool:
        return "true" if value else "false"
    if value is None:
        return "undefined"
    if kind is Tagged:
        return to_display(value.value)
    if kind is EnumValue:
        return value.name
    if kind is FSArray:
        return "[" + ",".join(f" {to_display(item)} " for item in value.items) + "]"
    if kind is FSMap:
        if value.tag is not None and value.tag.name == "ValueWithUnits":
            text = to_display(value.get_str("value"))
            for unit, exponent in value.get_str("unit").sorted_items():
                text += " " + unit + ("" if exponent == 1 else "^" + to_display(exponent))
            return text
        return "{" + ",".join(f" {to_display(k)} : {to_display(v)} " for k, v in value.sorted_items()) + "}"
    if kind is Box:
        return "box(" + to_display(value.value) + ")"
    return repr(value)


# Control flow signals, returned by compiled statements
class _Signal:
    __slots__ = ("name",)

    def __init__(self, name):
        self.name = name


BREAK = _Signal("break")
CONTINUE = _Signal("continue")
# A predicate's statement was false
FALSE = _Signal("false")


class Return:
    __slots__ = ("value",)

    def __init__(self, value):
        self.value = value


_MISSING = object()


class Scope:
    __slots__ = ("vars", "consts", "parent")

    def __init__(self, parent: Scope | None = None):
        self.vars: dict = {}
        self.consts: set | None = None
        self.parent = parent

    def lookup(self, name: str):
        scope = self
        while scope is not None:
            value = scope.vars.get(name, _MISSING)
            if value is not _MISSING:
                return value
            scope = scope.parent
        return _MISSING

    def declare(self, name: str, value, const: bool):
        self.vars[name] = value
        if const:
            if self.consts is None:
                self.consts = set()
            self.consts.add(name)

    def assign(self, name: str, value, at) -> None:
        scope = self
        while scope is not None:
            if name in scope.vars:
                if scope.consts is not None and name in scope.consts:
                    raise error(f"Can't assign to the constant {name}", at)
                scope.vars[name] = value
                return
            scope = scope.parent
        raise error(f"{name} isn't declared", at)


# Functions


class Function:
    """A function value: something `call` can call."""

    name = "function"


class DeclFunction(Function):
    """A function, predicate, or operator declared at the top level of a module."""

    __slots__ = ("decl", "module", "_compiled")

    def __init__(self, decl: ast.FunctionDecl, module: Module):
        self.decl = decl
        self.module = module
        self._compiled = None

    @property
    def name(self) -> str:
        return ("operator" if self.decl.kind == "operator" else "") + self.decl.name

    @property
    def params(self) -> list[ast.Param]:
        return self.decl.params

    def compiled(self):
        if self._compiled is None:
            compiler = Compiler(self.module)
            self._compiled = compiler.function(self.decl.kind == "predicate", self.decl.params, self.decl.returns,
                                               self.decl.precondition, self.decl.body, False, self.name, self.decl.at)
        return self._compiled

    def __repr__(self) -> str:
        return f"<{self.decl.kind} {self.decl.name} ({self.decl.at[0]}:{self.decl.at[1]})>"


class Overloads(Function):
    """A name's functions (overloads), from wherever they're visible: calls pick one by their arguments' types."""

    __slots__ = ("name", "functions")

    def __init__(self, name: str, functions: list[DeclFunction]):
        self.name = name
        self.functions = functions

    def __repr__(self) -> str:
        return f"<function {self.name}>"


class Closure(Function):
    __slots__ = ("node", "scope", "module", "_compiled")

    def __init__(self, node: ast.Lambda, scope: Scope, module: Module, compiled):
        self.node = node
        self.scope = scope
        self.module = module
        self._compiled = compiled

    name = "function expression"

    def __repr__(self) -> str:
        return f"<function expression ({self.node.at[0]}:{self.node.at[1]})>"


class PythonFunction(Function):
    """A function written in Python, as a value (like a test's helper)."""

    __slots__ = ("name", "function")

    def __init__(self, name: str, function):
        self.name = name
        self.function = function


# Modules


class ConstSymbol:
    """A top-level constant, evaluated when it's first used."""

    __slots__ = ("decl", "module", "state", "value")

    def __init__(self, decl: ast.ConstDecl, module: Module):
        self.decl = decl
        self.module = module
        self.state = 0  # 0: not evaluated, 1: evaluating, 2: evaluated
        self.value = None

    def get(self):
        if self.state == 2:
            return self.value
        if self.state == 1:
            raise error(f"{self.decl.name} depends on itself", self.decl.at)
        self.state = 1
        try:
            compiler = Compiler(self.module)
            value = compiler.expression(self.decl.value)(Scope())
            if self.decl.type is not None:
                value = compiler.check_type(value, self.decl.type, f"the constant {self.decl.name}", self.decl.at)
        except BaseException:
            self.state = 0
            raise
        self.value = value
        self.state = 2
        return value


class Namespace:
    """`Name::import(...)`: a module's exports, or an image's (its `BLOB_DATA`)."""

    def __init__(self, module: Module | None, path: str):
        self.module = module
        self.path = path

    def resolve(self, name: str, at):
        if self.module is not None:
            symbol = self.module.exported_symbol(name)
            if symbol is None:
                raise error(f"{name} isn't exported by {self.path}", at)
            return symbol_value(symbol)
        if name == "BLOB_DATA":
            return FSMap.of([("blob", self.path)])
        raise error(f"{name} isn't defined by the image {self.path}", at)


def symbol_value(symbol):
    if type(symbol) is ConstSymbol:
        return symbol.get()
    return symbol


class Module:
    def __init__(self, interpreter: Interpreter, path: pathlib.Path, label: str, program: ast.Program):
        self.interpreter = interpreter
        self.path = path
        self.label = label
        self.program = program
        # Own declarations: name -> list of symbols (functions), or a symbol
        self.functions: dict[str, list[DeclFunction]] = {}
        self.symbols: dict[str, object] = {}
        self.exported: set[str] = set()
        self.imports: list[tuple[Module, bool]] = []
        self.namespaces: dict[str, Namespace] = {}
        self._visible: dict[str, object] = {}
        self._exports: dict[str, list] = {}

    def declare(self) -> None:
        """Declares the module's top-level names, and loads what it imports."""
        for decl in self.program.declarations:
            kind = type(decl)
            if kind is ast.Import:
                self.interpreter.load_import(self, decl)
                continue
            if kind is ast.FunctionDecl:
                function = DeclFunction(decl, self)
                if decl.kind == "operator":
                    self.interpreter.operators.setdefault((decl.name, len(decl.params)), []).append(function)
                    continue
                self.functions.setdefault(decl.name, []).append(function)
            elif kind is ast.ConstDecl:
                self.symbols[decl.name] = ConstSymbol(decl, self)
            elif kind is ast.EnumDecl:
                self.symbols[decl.name] = EnumType(decl.name, self, decl.members)
            elif kind is ast.TypeDecl:
                self.symbols[decl.name] = FSType(decl.name, self, decl.typecheck)
            if decl.exported:
                self.exported.add(decl.name)

    def own_symbols(self, name: str, exported_only: bool) -> list:
        if exported_only and name not in self.exported:
            return []
        if name in self.functions:
            return list(self.functions[name])
        symbol = self.symbols.get(name)
        return [symbol] if symbol is not None else []

    def exported_symbols(self, name: str) -> list:
        """What this module exports by name: its own exported declarations, and what it re-exports."""
        cached = self._exports.get(name)
        if cached is not None:
            return cached
        self._exports[name] = []  # Guards against import cycles
        result = self.own_symbols(name, True)
        for module, exported in self.imports:
            if exported:
                result.extend(module.exported_symbols(name))
        self._exports[name] = result
        return result

    def exported_symbol(self, name: str):
        return _combine(name, self.exported_symbols(name))

    def resolve(self, name: str):
        """What a name means in this module: its own declaration, or one it imports (or `_MISSING`)."""
        cached = self._visible.get(name, _MISSING)
        if cached is not _MISSING:
            return cached
        symbols = self.own_symbols(name, False)
        for module, _exported in self.imports:
            symbols.extend(module.exported_symbols(name))
        result = _combine(name, symbols)
        if result is None:
            result = _MISSING
        self._visible[name] = result
        return result


def _combine(name: str, symbols: list):
    if not symbols:
        return None
    functions = [s for s in symbols if type(s) is DeclFunction]
    if functions:
        unique = []
        seen = set()
        for function in functions:
            if id(function) not in seen:
                seen.add(id(function))
                unique.append(function)
        return Overloads(name, unique)
    return symbols[0]


class Interpreter:
    def __init__(self, std_dir: pathlib.Path, code_dir: pathlib.Path | None = None,
                 studios_path: pathlib.Path | None = None, cache_dir: pathlib.Path | None = None):
        from fs_eval.builtins import BUILTINS

        self.std_dir = std_dir
        self.code_dir = code_dir
        self.studios = {}
        if studios_path is not None and studios_path.exists():
            self.studios = json.loads(studios_path.read_text()).get("studios", {})
        self.cache_dir = cache_dir
        self.modules: dict[pathlib.Path, Module] = {}
        self.operators: dict[tuple[str, int], list[DeclFunction]] = {}
        self.builtins = BUILTINS
        self.output: list[str] = []
        self.trace: list[str] = []
        sys.setrecursionlimit(max(sys.getrecursionlimit(), 100000))

    # Loading

    def load(self, path: pathlib.Path) -> Module:
        path = path.resolve()
        module = self.modules.get(path)
        if module is not None:
            return module
        label = self._label(path)
        program = self._parse(path, label)
        module = Module(self, path, label, program)
        self.modules[path] = module
        module.declare()
        return module

    def load_source(self, source: str, label: str, path: pathlib.Path) -> Module:
        """Loads a module from source (not cached), as if it were at `path` (which its imports are relative to)."""
        module = Module(self, path, label, parse(source, label))
        module.declare()
        return module

    def _label(self, path: pathlib.Path) -> str:
        for base in (self.std_dir, self.code_dir):
            if base is not None:
                try:
                    return path.relative_to(base.resolve()).as_posix()
                except ValueError:
                    pass
        return path.name

    def _parse(self, path: pathlib.Path, label: str) -> ast.Program:
        source = path.read_text()
        if self.cache_dir is None:
            return parse(source, label)
        digest = hashlib.sha1(f"{CACHE_VERSION}:{label}:{source}".encode()).hexdigest()
        cached = self.cache_dir / f"{digest}.pickle"
        try:
            return pickle.loads(cached.read_bytes())
        except (OSError, pickle.PickleError, EOFError, AttributeError):
            pass
        program = parse(source, label)
        try:
            self.cache_dir.mkdir(parents=True, exist_ok=True)
            cached.write_bytes(pickle.dumps(program, protocol=pickle.HIGHEST_PROTOCOL))
        except OSError:
            pass
        return program

    def resolve_import(self, path: str) -> pathlib.Path | None:
        if path.startswith(STD_PREFIX):
            return self.std_dir / path.removeprefix(STD_PREFIX)
        if ELEMENT_ID.fullmatch(path):
            file = self.studios.get(path)
            return self.code_dir / file if file and self.code_dir is not None else None
        if self.code_dir is not None:
            return self.code_dir / path
        return None

    def load_import(self, module: Module, decl: ast.Import) -> None:
        resolved = self.resolve_import(decl.path)
        if resolved is not None and not resolved.exists() and not decl.path.startswith(STD_PREFIX):
            # A file outside the code folder (like a test) can import files beside it
            beside = module.path.parent / decl.path
            if beside.exists():
                resolved = beside
        if decl.namespace is not None:
            imported = self.load(resolved) if resolved is not None and resolved.suffix == ".fs" and resolved.exists() else None
            module.namespaces[decl.namespace] = Namespace(imported, decl.path)
            return
        if resolved is None or not resolved.exists():
            raise error(f"Can't find the import {decl.path}", decl.at)
        module.imports.append((self.load(resolved), decl.exported))

    # Running

    def call(self, function, args: list, at=None):
        """Calls a function value with `args` (FeatureScript values)."""
        kind = type(function)
        if kind is Overloads:
            chosen = self.choose_overload(function, args, at)
            return self._invoke(chosen.compiled(), args, chosen.name, at)
        if kind is Closure:
            return self._invoke(function._compiled, args, "function expression", at, function.scope)
        if kind is DeclFunction:
            return self._invoke(function.compiled(), args, function.name, at)
        if kind is PythonFunction:
            return function.function(*args)
        if kind is Tagged:
            return self.call(function.value, args, at)
        raise error(f"{to_display(function)} isn't a function", at)

    def _invoke(self, compiled, args: list, name: str, at, scope: Scope | None = None):
        self.trace.append(f"{name} ({at[0]}:{at[1]})" if at else name)
        try:
            return compiled(args, scope, at)
        finally:
            self.trace.pop()

    def choose_overload(self, overloads: Overloads, args: list, at) -> DeclFunction:
        best = None
        best_score = -1
        count = len(args)
        for function in overloads.functions:
            params = function.decl.params
            if len(params) != count:
                continue
            score = 0
            for param, arg in zip(params, args):
                if param.type is None:
                    continue
                match = self.type_matches(arg, param.type, function.module)
                if match == 0:
                    score = -1
                    break
                score += match
            if score > best_score:
                best = function
                best_score = score
        if best is None:
            described = ", ".join(_describe(arg) for arg in args)
            raise error(f"No overload of {overloads.name} takes ({described})", at)
        return best

    def resolve_type(self, type_ref: ast.TypeRef, module: Module):
        """A builtin type's name, or an `FSType`."""
        if type_ref.namespace is None and type_ref.name in BUILTIN_TYPES:
            return type_ref.name
        if type_ref.namespace is not None:
            namespace = module.namespaces.get(type_ref.namespace)
            if namespace is None:
                raise error(f"{type_ref.namespace} isn't a namespace", type_ref.at)
            resolved = namespace.resolve(type_ref.name, type_ref.at)
        else:
            resolved = module.resolve(type_ref.name)
        if not isinstance(resolved, FSType):
            raise error(f"{type_ref.name} isn't a type", type_ref.at)
        return resolved

    def type_matches(self, value, type_ref: ast.TypeRef, module: Module) -> int:
        """0 if `value` isn't of the type; otherwise how specific a match it is (1 for a builtin type, 2 for a
        declared one), to choose between overloads."""
        resolved = _type_cache.get(id(type_ref))
        if resolved is None:
            resolved = self.resolve_type(type_ref, module)
            _type_cache[id(type_ref)] = resolved
            _type_cache_refs.append(type_ref)
        return self.is_type(value, resolved)

    def is_type(self, value, resolved) -> int:
        if type(resolved) is str:
            if resolved == "undefined":
                return 1 if value is None else 0
            if resolved == "function":
                return 1 if isinstance(untag(value), Function) else 0
            if resolved == "builtin":
                return 1 if isinstance(untag(value), Builtin) else 0
            return 1 if type_name(value) == resolved else 0
        return 2 if tag_of(value) is resolved else 0

    def cast(self, value, type_ref: ast.TypeRef, module: Module, at):
        """`value as Type`."""
        resolved = _type_cache.get(id(type_ref))
        if resolved is None:
            resolved = self.resolve_type(type_ref, module)
            _type_cache[id(type_ref)] = resolved
            _type_cache_refs.append(type_ref)
        if type(resolved) is str:
            plain = untag(value)
            if type(plain) in (FSMap, FSArray):
                plain = type(plain)(plain.entries if type(plain) is FSMap else plain.items, None)
            if self.is_type(plain, resolved) == 0 and type_name(plain) != resolved:
                raise error(f"{_describe(value)} can't be cast to {resolved}", at)
            return plain
        if isinstance(resolved, EnumType):
            if type(value) is EnumValue and value.enum is resolved:
                return value
            if type(untag(value)) is str and untag(value) in resolved.members:
                return resolved.members[untag(value)]
            raise error(f"{_describe(value)} isn't a {resolved.name}", at)
        typecheck = resolved.typecheck
        if not isinstance(typecheck, Function):
            typecheck = Compiler(resolved.module).expression(typecheck)(Scope())
            resolved.typecheck = typecheck
        plain = untag(value)
        if self.call(typecheck, [plain], at) is not True:
            raise error(f"{_describe(value)} fails {resolved.name}'s typecheck", at)
        kind = type(plain)
        if kind is FSMap:
            return FSMap(plain.entries, resolved)
        if kind is FSArray:
            return FSArray(plain.items, resolved)
        if isinstance(plain, Builtin):
            plain.tag = resolved
            return plain
        return Tagged(plain, resolved)

    # Operators

    def operator(self, op: str, args: list, at):
        """Calls an overloaded operator (like std's `+` for `ValueWithUnits`)."""
        overloads = self.operators.get((op, len(args)))
        if overloads:
            best = None
            best_score = -1
            for function in overloads:
                score = 0
                for param, arg in zip(function.decl.params, args):
                    if param.type is None:
                        continue
                    match = self.type_matches(arg, param.type, function.module)
                    if match == 0:
                        score = -1
                        break
                    score += match
                if score > best_score:
                    best = function
                    best_score = score
            if best is not None:
                return self._invoke(best.compiled(), args, f"operator{op}", at)
        described = f" {op} ".join(_describe(arg) for arg in args) if len(args) == 2 else op + _describe(args[0])
        raise error(f"Can't evaluate {described}", at)

    def binary(self, op: str, a, b, at):
        ta = type(a)
        if ta is float and type(b) is float:
            if op == "+":
                return a + b
            if op == "-":
                return a - b
            if op == "*":
                return a * b
            if op == "/":
                if b == 0:
                    raise error("Division by zero", at)
                return a / b
            if op == "<":
                return a < b
            if op == ">":
                return a > b
            if op == "<=":
                return a <= b
            if op == ">=":
                return a >= b
            if op == "%":
                if b == 0:
                    raise error("Division by zero", at)
                import math

                return math.fmod(a, b)
            if op == "^":
                try:
                    result = a ** b
                except (OverflowError, ZeroDivisionError):
                    raise error(f"Can't evaluate {format_number(a)} ^ {format_number(b)}", at)
                if type(result) is complex:
                    raise error(f"Can't evaluate {format_number(a)} ^ {format_number(b)}", at)
                return float(result)
            if op == "~":
                return format_number(a) + format_number(b)
        if op == "==":
            return equal(a, b)
        if op == "!=":
            return not equal(a, b)
        if op == "~":
            return to_display(a) + to_display(b)
        if ta is str and type(b) is str and op in ("<", ">", "<=", ">="):
            return {"<": a < b, ">": a > b, "<=": a <= b, ">=": a >= b}[op]
        # Overloads: `>`, `<=`, and `>=` are in terms of `<`
        if op == ">":
            return self._less(b, a, at)
        if op == "<=":
            return not self._less(b, a, at)
        if op == ">=":
            return not self._less(a, b, at)
        if op == "<":
            return self._less(a, b, at)
        if type(a) is Tagged or type(b) is Tagged:
            ua, ub = untag(a), untag(b)
            if type(ua) is float and type(ub) is float and (op, 2) not in self.operators:
                return self.binary(op, ua, ub, at)
        return self.operator(op, [a, b], at)

    def _less(self, a, b, at) -> bool:
        if type(a) is float and type(b) is float:
            return a < b
        if type(a) is str and type(b) is str:
            return a < b
        result = self.operator("<", [a, b], at)
        if type(result) is not bool:
            raise error("< must return a boolean", at)
        return result

    def unary(self, op: str, value, at):
        if op == "-":
            if type(value) is float:
                return -value
            return self.operator("-", [value], at)
        if op == "!":
            if type(value) is not bool:
                raise error(f"! of {_describe(value)}, not a boolean", at)
            return not value
        raise error(f"Unknown operator {op}", at)

    # Entry points

    def evaluate(self, source: str, module: Module):
        """Evaluates an expression in a module's scope."""
        node = parse_expression(source)
        return Compiler(module).expression(node)(Scope())


_type_cache: dict[int, object] = {}
# Keeps the type references cached by id alive, so their ids aren't reused
_type_cache_refs: list = []


def _describe(value) -> str:
    tag = tag_of(value)
    name = type_name(value)
    if tag is not None and not isinstance(tag, EnumType):
        return f"{tag.name}"
    if isinstance(value, EnumValue):
        return f"{value.enum.name}"
    return name


# Compiling


class Compiler:
    """Compiles a module's syntax trees into closures: an expression's takes a `Scope` and returns its value, and a
    statement's takes one and returns None, or a signal (`Return`, `BREAK`, `CONTINUE`, `FALSE`)."""

    def __init__(self, module: Module):
        self.module = module
        self.interpreter = module.interpreter

    def check_type(self, value, type_ref: ast.TypeRef, what: str, at):
        if self.interpreter.type_matches(value, type_ref, self.module) == 0:
            raise error(f"{what} should be {type_ref.name}, but is {_describe(value)}", at)
        return value

    # Functions

    def function(self, predicate: bool, params: list[ast.Param], returns, precondition, body, expression: bool,
                 name: str, at):
        """Compiles a function's body: returns `compiled(args, scope, call_at)`."""
        interpreter = self.interpreter
        module = self.module
        names = [param.name for param in params]
        types = [param.type for param in params]
        any_types = any(t is not None for t in types)
        check_precondition = self.block(precondition, True) if precondition is not None else None
        if expression:
            body_expression = self.expression(body)
        else:
            run_body = self.block(body, predicate)
        count = len(params)

        def compiled(args, closure_scope, call_at):
            if len(args) != count:
                raise error(f"{name} takes {count} arguments, not {len(args)}", call_at)
            scope = Scope(closure_scope)
            variables = scope.vars
            for i in range(count):
                variables[names[i]] = args[i]
            if any_types and closure_scope is not None:
                # A declared function's types were checked choosing its overload
                for i in range(count):
                    if types[i] is not None and interpreter.type_matches(args[i], types[i], module) == 0:
                        raise error(f"{name}'s {names[i]} should be {types[i].name}, but is {_describe(args[i])}", call_at)
            if check_precondition is not None:
                signal = check_precondition(scope)
                if signal is FALSE:
                    raise error(f"The precondition of {name} failed", call_at)
            if expression:
                result = body_expression(scope)
            else:
                signal = run_body(scope)
                if predicate:
                    if signal is FALSE:
                        return False
                    if type(signal) is Return:
                        return signal.value
                    return True
                result = signal.value if type(signal) is Return else None
            if returns is not None and interpreter.type_matches(result, returns, module) == 0:
                raise error(f"{name} should return {returns.name}, but returned {_describe(result)}", call_at)
            return result

        return compiled

    # Statements

    def block(self, node, predicate: bool):
        if type(node) is not ast.Block:
            return self.statement(node, predicate)
        statements = [self.statement(s, predicate) for s in node.statements]

        def run(scope):
            inner = Scope(scope)
            for statement in statements:
                signal = statement(inner)
                if signal is not None:
                    return signal
            return None

        return run

    def statement(self, node, predicate: bool):
        method = getattr(self, "s_" + type(node).__name__)
        return method(node, predicate)

    def s_Block(self, node, predicate):
        return self.block(node, predicate)

    def s_ExpressionStatement(self, node, predicate):
        value = self.expression(node.value)
        if not predicate:
            def run(scope):
                value(scope)
            return run

        def check(scope):
            if value(scope) is not True:
                return FALSE
            return None

        return check

    def s_VarDecl(self, node, predicate):
        name = node.name
        const = node.const
        value = self.expression(node.value) if node.value is not None else None
        type_ref = node.type
        at = node.at

        def run(scope):
            result = value(scope) if value is not None else None
            if type_ref is not None and value is not None:
                self.check_type(result, type_ref, name, at)
            scope.declare(name, result, const)

        return run

    def s_Assign(self, node, predicate):
        op = node.op
        value = self.expression(node.value)
        at = node.at
        interpreter = self.interpreter
        if op == "=":
            compute = value
        elif op in ("||=", "&&=", "??="):
            current = self.expression(node.target)
            logical = self.e_Logical(_logical(op[:-1], node.target, node.value, at))
            compute = logical
        else:
            current = self.expression(node.target)
            binary_op = op[:-1]

            def compute(scope):
                return interpreter.binary(binary_op, current(scope), value(scope), at)

        store = self.assigner(node.target)

        def run(scope):
            store(scope, compute(scope))

        return run

    def assigner(self, target):
        """Returns `store(scope, value)`, which sets `target` (a name, field, index, or box) to `value`: an
        array's or map's is set by setting the whole array or map, changed (they're values)."""
        kind = type(target)
        at = target.at
        if kind is ast.Name:
            name = target.name

            def store(scope, value):
                scope.assign(name, value, at)

            return store
        if kind is ast.BoxGet:
            get_box = self.expression(target.target)

            def store(scope, value):
                box = get_box(scope)
                if type(box) is not Box:
                    raise error(f"[] of {_describe(box)}, not a box", at)
                box.value = value

            return store
        if kind is ast.Member:
            get_container = self.expression(target.target)
            store_container = self.assigner(target.target)
            name = target.name

            def store(scope, value):
                container = get_container(scope)
                if type(container) is FSMap:
                    store_container(scope, container.with_entry(name, value))
                elif container is None:
                    raise error(f"Can't set {name} of undefined", at)
                else:
                    raise error(f"Can't set the field {name} of {_describe(container)}", at)

            return store
        if kind is ast.Index:
            get_container = self.expression(target.target)
            store_container = self.assigner(target.target)
            get_index = self.expression(target.index)

            def store(scope, value):
                container = get_container(scope)
                index = get_index(scope)
                if type(container) is FSMap:
                    store_container(scope, container.with_entry(index, value))
                elif type(container) is FSArray:
                    store_container(scope, container.with_item(_array_index(container, index, at), value))
                else:
                    raise error(f"Can't index {_describe(container)}", at)

            return store
        raise error("Can't assign to this", at)

    def s_If(self, node, predicate):
        condition = self.expression(node.condition)
        then = self.block(node.then, predicate)
        otherwise = self.block(node.otherwise, predicate) if node.otherwise is not None else None
        at = node.at

        def run(scope):
            test = condition(scope)
            if test is True:
                return then(scope)
            if test is not False:
                raise error(f"if needs a boolean, not {_describe(test)}", at)
            if otherwise is not None:
                return otherwise(scope)
            return None

        return run

    def s_For(self, node, predicate):
        init = self.statement(node.init, predicate) if node.init is not None else None
        condition = self.expression(node.condition) if node.condition is not None else None
        update = self.statement(node.update, False) if node.update is not None else None
        body = self.block(node.body, predicate)
        at = node.at

        def run(scope):
            loop = Scope(scope)
            if init is not None:
                init(loop)
            while True:
                if condition is not None:
                    test = condition(loop)
                    if test is False:
                        break
                    if test is not True:
                        raise error(f"for needs a boolean condition, not {_describe(test)}", at)
                signal = body(loop)
                if signal is not None:
                    if signal is BREAK:
                        break
                    if signal is not CONTINUE:
                        return signal
                if update is not None:
                    update(loop)
            return None

        return run

    def s_ForIn(self, node, predicate):
        iterable = self.expression(node.iterable)
        body = self.block(node.body, predicate)
        key_name = node.key
        value_name = node.value
        declared = node.declared
        at = node.at

        def run(scope):
            container = untag(iterable(scope))
            kind = type(container)
            if kind is FSArray:
                pairs = ((float(i), item) for i, item in enumerate(container.items))
            elif kind is FSMap:
                if key_name is None:
                    pairs = ((None, FSMap.of([("key", k), ("value", v)])) for k, v in container.sorted_items())
                else:
                    pairs = iter(container.sorted_items())
            else:
                raise error(f"Can't iterate over {_describe(container)}", at)
            for key, value in pairs:
                loop = Scope(scope)
                if declared:
                    loop.vars[value_name] = value
                    if key_name is not None:
                        loop.vars[key_name] = key
                else:
                    scope.assign(value_name, value, at)
                    if key_name is not None:
                        scope.assign(key_name, key, at)
                signal = body(loop)
                if signal is not None:
                    if signal is BREAK:
                        break
                    if signal is not CONTINUE:
                        return signal
            return None

        return run

    def s_While(self, node, predicate):
        condition = self.expression(node.condition)
        body = self.block(node.body, predicate)
        do = node.do
        at = node.at

        def run(scope):
            first = do
            while True:
                if not first:
                    test = condition(scope)
                    if test is False:
                        break
                    if test is not True:
                        raise error(f"while needs a boolean, not {_describe(test)}", at)
                first = False
                signal = body(scope)
                if signal is not None:
                    if signal is BREAK:
                        break
                    if signal is not CONTINUE:
                        return signal
            return None

        return run

    def s_Return(self, node, predicate):
        value = self.expression(node.value) if node.value is not None else None

        def run(scope):
            return Return(value(scope) if value is not None else None)

        return run

    def s_Break(self, node, predicate):
        return lambda scope: BREAK

    def s_Continue(self, node, predicate):
        return lambda scope: CONTINUE

    def s_Throw(self, node, predicate):
        value = self.expression(node.value)
        interpreter = self.interpreter
        at = node.at

        def run(scope):
            raise FSError(value(scope), list(interpreter.trace) + [f"{at[0]}:{at[1]}"])

        return run

    def s_Try(self, node, predicate):
        body = self.block(node.body, predicate)
        catch_body = self.block(node.catch_body, predicate) if node.catch_body is not None else None
        catch_name = node.catch_name
        interpreter = self.interpreter

        def run(scope):
            depth = len(interpreter.trace)
            try:
                return body(scope)
            except FSError as caught:
                del interpreter.trace[depth:]
                if catch_body is None:
                    return None
                inner = Scope(scope)
                if catch_name is not None:
                    inner.vars[catch_name] = caught.value
                return catch_body(inner)
            except RecursionError:
                del interpreter.trace[depth:]
                raise

        return run

    # Expressions

    def expression(self, node):
        return getattr(self, "e_" + type(node).__name__)(node)

    def e_Literal(self, node):
        value = node.value
        return lambda scope: value

    def e_Name(self, node):
        name = node.name
        module = self.module
        at = node.at

        def get(scope):
            value = scope.lookup(name)
            if value is not _MISSING:
                return value
            symbol = module.resolve(name)
            if symbol is _MISSING:
                raise error(f"{name} isn't defined", at)
            if type(symbol) is ConstSymbol:
                return symbol.get()
            return symbol

        return get

    def e_NamespaceName(self, node):
        module = self.module
        namespace_name = node.namespace
        name = node.name
        at = node.at

        def get(scope):
            namespace = module.namespaces.get(namespace_name)
            if namespace is None:
                raise error(f"{namespace_name} isn't a namespace", at)
            return namespace.resolve(name, at)

        return get

    def e_Member(self, node):
        target = self.expression(node.target)
        name = node.name
        safe = node.safe
        at = node.at

        def get(scope):
            container = target(scope)
            kind = type(container)
            if kind is FSMap:
                entry = container.entries.get((3, name))
                return entry[1] if entry is not None else None
            if container is None:
                if safe:
                    return None
                raise error(f"Can't get {name} of undefined", at)
            if kind is EnumType:
                member = container.members.get(name)
                if member is None:
                    raise error(f"{name} isn't a member of {container.name}", at)
                return member
            if kind is Tagged:
                inner = container.value
                if type(inner) is FSMap:
                    return inner.get_str(name)
            raise error(f"Can't get the field {name} of {_describe(container)}", at)

        return get

    def e_Index(self, node):
        target = self.expression(node.target)
        index = self.expression(node.index)
        safe = node.safe
        at = node.at

        def get(scope):
            container = untag(target(scope))
            key = index(scope)
            kind = type(container)
            if kind is FSArray:
                return container.items[_array_index(container, key, at)]
            if kind is FSMap:
                return container.get(key)
            if container is None and safe:
                return None
            raise error(f"Can't index {_describe(container)}", at)

        return get

    def e_BoxGet(self, node):
        target = self.expression(node.target)
        safe = node.safe
        at = node.at

        def get(scope):
            box = target(scope)
            if type(box) is Box:
                return box.value
            if box is None and safe:
                return None
            raise error(f"[] of {_describe(box)}, not a box", at)

        return get

    def e_Call(self, node):
        function = self.expression(node.function)
        args = [self.expression(arg) for arg in node.args]
        interpreter = self.interpreter
        at = node.at

        def call(scope):
            return interpreter.call(function(scope), [arg(scope) for arg in args], at)

        return call

    def e_BuiltinCall(self, node):
        builtin = self.interpreter.builtins.get(node.name)
        args = [self.expression(arg) for arg in node.args]
        interpreter = self.interpreter
        name = node.name
        at = node.at

        def call(scope):
            if builtin is None:
                raise error(f"@{name} isn't available in the evaluator", at)
            try:
                return builtin(interpreter, [arg(scope) for arg in args], at)
            except FSError:
                raise
            except (TypeError, ValueError, IndexError, KeyError, AttributeError, ZeroDivisionError, OverflowError) as e:
                raise error(f"@{name} failed: {e}", at)

        return call

    def e_Unary(self, node):
        operand = self.expression(node.operand)
        op = node.op
        interpreter = self.interpreter
        at = node.at
        if op == "-":
            def negate(scope):
                value = operand(scope)
                if type(value) is float:
                    return -value
                return interpreter.unary("-", value, at)
            return negate
        return lambda scope: interpreter.unary(op, operand(scope), at)

    def e_Binary(self, node):
        left = self.expression(node.left)
        right = self.expression(node.right)
        op = node.op
        binary = self.interpreter.binary
        at = node.at
        return lambda scope: binary(op, left(scope), right(scope), at)

    def e_Logical(self, node):
        left = self.expression(node.left)
        right = self.expression(node.right)
        op = node.op
        at = node.at
        if op == "??":
            def coalesce(scope):
                value = left(scope)
                return right(scope) if value is None else value
            return coalesce

        def check(value):
            if type(value) is not bool:
                raise error(f"{op} needs booleans, not {_describe(value)}", at)
            return value

        if op == "&&":
            return lambda scope: check(left(scope)) and check(right(scope))
        return lambda scope: check(left(scope)) or check(right(scope))

    def e_Ternary(self, node):
        condition = self.expression(node.condition)
        then = self.expression(node.then)
        otherwise = self.expression(node.otherwise)
        at = node.at

        def run(scope):
            test = condition(scope)
            if test is True:
                return then(scope)
            if test is False:
                return otherwise(scope)
            raise error(f"? needs a boolean, not {_describe(test)}", at)

        return run

    def e_Is(self, node):
        value = self.expression(node.value)
        type_ref = node.type
        interpreter = self.interpreter
        module = self.module
        return lambda scope: interpreter.type_matches(value(scope), type_ref, module) != 0

    def e_As(self, node):
        value = self.expression(node.value)
        type_ref = node.type
        interpreter = self.interpreter
        module = self.module
        at = node.at
        return lambda scope: interpreter.cast(value(scope), type_ref, module, at)

    def e_MapLiteral(self, node):
        entries = [(self.expression(k), self.expression(v)) for k, v in node.entries]

        def build(scope):
            result = {}
            for key, value in entries:
                k = key(scope)
                v = value(scope)
                # Maps don't hold undefined
                if v is not None:
                    result[key_of(k)] = (k, v)
            return FSMap(result)

        return build

    def e_ArrayLiteral(self, node):
        items = [self.expression(item) for item in node.items]
        return lambda scope: FSArray(tuple(item(scope) for item in items))

    def e_Lambda(self, node):
        compiled = self.function(False, node.params, node.returns, node.precondition, node.body, node.expression,
                                 "function expression", node.at)
        module = self.module
        return lambda scope: Closure(node, scope, module, compiled)

    def e_NewBox(self, node):
        value = self.expression(node.value)
        return lambda scope: Box(value(scope))

    def e_Switch(self, node):
        subject = self.expression(node.subject)
        entries = [(self.expression(k), self.expression(v)) for k, v in node.entries]

        def run(scope):
            value = subject(scope)
            for key, result in entries:
                if equal(key(scope), value):
                    return result(scope)
            return None

        return run

    def e_TryExpression(self, node):
        value = self.expression(node.value)
        interpreter = self.interpreter

        def run(scope):
            depth = len(interpreter.trace)
            try:
                return value(scope)
            except FSError:
                del interpreter.trace[depth:]
                return None

        return run


def _logical(op, left, right, at):
    node = ast.Logical(op, left, right)
    node.at = at
    return node


def _array_index(array: FSArray, index, at) -> int:
    if type(index) is not float or index != int(index):
        raise error(f"An array index must be a whole number, not {to_display(index)}", at)
    i = int(index)
    if i < 0 or i >= len(array.items):
        raise error(f"Index {i} is out of range for an array of {len(array.items)}", at)
    return i
