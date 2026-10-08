"""Cross-file analysis of the FeatureScripts in a repo.

Feature Studios import each other by element id (`import(path : "<element id>", ...)`). The fs
CLI records which file each element id is synced with in fs-studios.json (checked in), which lets
imports be resolved to files: for Go to Definition and Find References across files, and for diagnostics
about names which aren't defined anywhere and imports which aren't used.

Files are re-read when they change on disk, and open editors can overlay their unsaved contents.
"""

from __future__ import annotations

import contextlib
import dataclasses
import json
import pathlib
import re
from typing import Iterable, Literal

from fs_lsp.diagnostics import diagnostics as syntax_diagnostics
from fs_lsp.parser import AstNode, ParsedProgram, parse
from fs_lsp.scanner import KEYWORDS, Token
from fs_lsp.semantic import ImportedNames
from fs_lsp.stdlib import built_in_type, stdlib
from fs_lsp.symbol_index import Declaration, SymbolIndex

ELEMENT_ID = re.compile(r"[0-9a-f]{24}")
STD_PREFIX = "onshape/std/"
COMMON_STD = STD_PREFIX + "common.fs"

TOP_LEVEL_KINDS = frozenset(
    ["feature", "function", "predicate", "operator", "enum", "type", "variable"]
)
DECLARATION_KEYWORDS = frozenset(
    ["const", "function", "predicate", "operator", "enum", "type"]
)
# Identifiers FeatureScript treats specially which aren't declared anywhere
IMPLICIT_NAMES = frozenset(["silent"])
# Kinds of top-level declarations which can't share a name with anything else; functions, predicates, and operators
# can be overloaded
UNIQUE_KINDS = frozenset(["variable", "enum", "type"])
STD_UNIQUE_KINDS = frozenset(["constant", "enum", "type", "unit"])
# Kinds of declarations which may share names, as overloads with different parameter types
CALLABLE_KINDS = frozenset(["function", "predicate"])
# Annotation keys whose values name a function (in the file, or one it imports) by its name
FUNCTION_ANNOTATION_KEYS = frozenset(["Manipulator Change Function", "Editing Logic Function"])
# Annotation keys whose values (or values in an array) name a member of a std enum, e.g. `"UIHint" : ["SHOW_LABEL"]`
ENUM_ANNOTATION_KEYS = {"UIHint": "UIHint"}
# Std predicates which declare the parameter they're given, e.g. `isLength(definition.width, LENGTH_BOUNDS)`
PARAMETER_PREDICATES = frozenset(["isLength", "isAngle", "isInteger", "isReal", "isAnything"])

Severity = Literal["error", "warning"]


@dataclasses.dataclass(slots=True)
class Import:
    path: str
    token: Token  # The path string
    start: int
    end: int
    exported: bool
    namespace: str | None

    @property
    def is_std(self) -> bool:
        return self.path.startswith(STD_PREFIX)

    @property
    def element_id(self) -> str | None:
        return self.path if ELEMENT_ID.fullmatch(self.path) else None

    @property
    def code_path(self) -> str | None:
        """The imported file's path in the code folder, for imports by path (see `fs push`)."""
        if self.is_std or self.namespace or not self.path.endswith(".fs"):
            return None
        return self.path


@dataclasses.dataclass(slots=True)
class Problem:
    start: int
    end: int
    severity: Severity
    message: str
    code: str
    unnecessary: bool = False


@dataclasses.dataclass(slots=True)
class Usage:
    """How a top-level declaration is used across the project."""

    module: Module
    declaration: Declaration
    exported: bool
    local_uses: int
    other_files: list[Module]
    named_in_strings: bool

    @property
    def unused(self) -> bool:
        return not (self.local_uses or self.other_files or self.named_in_strings)


@dataclasses.dataclass(frozen=True, slots=True)
class Provider:
    """A declaration visible in a module through one of its imports."""

    via: Import  # The import in the module which makes it visible
    module: Module
    declaration: Declaration


class Module:
    """One parsed .fs file."""

    def __init__(self, path: pathlib.Path, relative: str, source: str) -> None:
        self.path = path
        self.relative = relative
        self.parsed: ParsedProgram = parse(source)
        self.index = SymbolIndex(self.parsed)
        self.lines = source.split("\n")
        self.imports = list(self._imports())
        self.top_level: dict[str, list[Declaration]] = {}
        self.exports: dict[str, list[Declaration]] = {}
        for declaration in self.index.declarations:
            if not self._is_top_level(declaration):
                continue
            self.top_level.setdefault(declaration.name, []).append(declaration)
            if self._is_exported(declaration):
                self.exports.setdefault(declaration.name, []).append(declaration)

    def __repr__(self) -> str:
        return f"Module({self.relative})"

    def position(self, offset: int) -> tuple[int, int]:
        """The 0-based (line, UTF-16 character) of an offset."""
        return self.parsed.line_map.position(offset)

    def line_text(self, line: int) -> str:
        return self.lines[line] if 0 <= line < len(self.lines) else ""

    def import_at(self, offset: int) -> Import | None:
        for imported in self.imports:
            if imported.token.offset <= offset <= imported.token.end:
                return imported
        return None

    def _imports(self) -> Iterable[Import]:
        for node in self.parsed.nodes:
            if node.type not in ("ImportDeclaration", "NamespacedImportDeclaration"):
                continue
            tokens = [
                token
                for token in self.index.tokens
                if node.start <= token.offset < node.end
            ]
            path_token = next(
                (
                    tokens[i + 2]
                    for i in range(len(tokens) - 2)
                    if tokens[i].value == "path"
                    and tokens[i + 1].value == ":"
                    and tokens[i + 2].kind == "string"
                ),
                None,
            )
            if path_token is None:
                continue
            before = self.index.previous_token(tokens[0]) if tokens else None
            yield Import(
                path=path_token.value[1:-1],
                token=path_token,
                start=node.start,
                end=node.end,
                exported=before is not None and before.value == "export",
                namespace=node.name if node.type == "NamespacedImportDeclaration" else None,
            )

    def _is_top_level(self, declaration: Declaration) -> bool:
        return (
            declaration.kind in TOP_LEVEL_KINDS
            and declaration.scope_start == self.parsed.start
            and declaration.scope_end == self.parsed.end
        )

    def _is_exported(self, declaration: Declaration) -> bool:
        keyword = self.index.previous_token(declaration.token)
        if keyword is None or keyword.value not in DECLARATION_KEYWORDS:
            return False
        export = self.index.previous_token(keyword)
        return export is not None and export.value == "export"


class Project:
    """The FeatureScripts in a repo's code folder (see the fs CLI's [tool.fs] config)."""

    def __init__(
        self, root: pathlib.Path, code_dir: pathlib.Path, studios_path: pathlib.Path,
        std_dir: pathlib.Path | None = None,
    ) -> None:
        self.root = root
        self.code_dir = code_dir
        self.studios_path = studios_path
        # A copy of the std library's source (see `fs pull-std`), if there is one
        self.std_dir = std_dir
        self.overlays: dict[pathlib.Path, str] = {}
        self._modules: dict[pathlib.Path, tuple[object, Module]] = {}
        self._element_ids: tuple[object, dict[str, pathlib.Path]] | None = None
        self._precondition_predicate_cache: tuple[tuple[int, ...], set[tuple[pathlib.Path, int]]] | None = None
        # Whether each std predicate declares parameters which can be toleranced (see _std_tolerant)
        self._std_tolerant_cache: dict[str, bool] = {}
        # While a snapshot is taken (see `snapshot`): how deep, and its files, resolved paths, and modules
        self._snapshot_depth = 0
        self._snapshot_files: list[pathlib.Path] | None = None
        self._snapshot_paths: dict[pathlib.Path, pathlib.Path] = {}
        self._snapshot_modules: dict[pathlib.Path, Module | None] = {}
        self._snapshot_providers: dict[int, tuple[Module, tuple[dict[str, list[Provider]], bool]]] = {}

    @classmethod
    def find(cls, path: pathlib.Path) -> Project | None:
        """Returns the project containing path, if it's inside a code folder."""
        from fs_cli.config import ConfigError, load_config

        try:
            config = load_config(path if path.is_dir() else path.parent)
        except ConfigError:
            return None
        project = cls(config.root, config.code_dir, config.studios_path, config.std_dir)
        return project if project.contains(path) else None

    @contextlib.contextmanager
    def snapshot(self):
        """Treats the files as unchanged while it's taken, so they're listed, resolved, and checked for changes once
        (rather than by every lookup, which made checking a file take seconds). Take one around each batch of work,
        like a request or publishing diagnostics."""
        self._snapshot_depth += 1
        try:
            yield self
        finally:
            self._snapshot_depth -= 1
            if self._snapshot_depth == 0:
                self._snapshot_files = None
                self._snapshot_paths.clear()
                self._snapshot_modules.clear()
                self._snapshot_providers.clear()

    def _resolve(self, path: pathlib.Path) -> pathlib.Path:
        if not self._snapshot_depth:
            return path.resolve()
        resolved = self._snapshot_paths.get(path)
        if resolved is None:
            resolved = self._snapshot_paths[path] = path.resolve()
        return resolved

    def contains(self, path: pathlib.Path) -> bool:
        code_dir = self._resolve(self.code_dir)
        path = self._resolve(path)
        return path == code_dir or code_dir in path.parents

    # Files

    def files(self) -> list[pathlib.Path]:
        if self._snapshot_files is not None:
            return self._snapshot_files
        if not self.code_dir.is_dir():
            return []
        paths = {path.resolve() for path in self.code_dir.rglob("*.fs") if path.is_file()}
        paths.update(path for path in self.overlays if self.contains(path))
        files = sorted(paths)
        if self._snapshot_depth:
            self._snapshot_files = files
        return files

    def modules(self) -> list[Module]:
        return [module for path in self.files() if (module := self.module(path))]

    def module(self, path: pathlib.Path) -> Module | None:
        if self._snapshot_depth and path in self._snapshot_modules:
            return self._snapshot_modules[path]
        module = self._module(path)
        if self._snapshot_depth:
            self._snapshot_modules[path] = module
        return module

    def _module(self, path: pathlib.Path) -> Module | None:
        path = self._resolve(path)
        if path in self.overlays:
            source = self.overlays[path]
            key: object = ("overlay", hash(source))
        else:
            try:
                stat = path.stat()
            except OSError:
                return None
            key = (stat.st_mtime_ns, stat.st_size)
            source = None
        cached = self._modules.get(path)
        if cached and cached[0] == key:
            return cached[1]
        if source is None:
            source = path.read_text(encoding="utf-8", errors="replace")
        relative = path.relative_to(self._resolve(self.code_dir)).as_posix()
        module = Module(path, relative, source)
        self._modules[path] = (key, module)
        return module

    def element_ids(self) -> dict[str, pathlib.Path]:
        """Maps element ids to the files synced with them, from the fs CLI's fs-studios.json."""
        try:
            stat = self.studios_path.stat()
            key: object = (stat.st_mtime_ns, stat.st_size)
        except OSError:
            return {}
        if self._element_ids and self._element_ids[0] == key:
            return self._element_ids[1]
        try:
            studios = json.loads(self.studios_path.read_text())["studios"]
            ids = {
                element_id: (self.code_dir / file).resolve()
                for element_id, file in studios.items()
            }
        except (OSError, ValueError, KeyError, TypeError):
            ids = {}
        self._element_ids = (key, ids)
        return ids

    # Imports

    def resolve(self, imported: Import) -> Module | None:
        if imported.code_path:
            return self.module(self.code_dir / imported.code_path)
        element_id = imported.element_id
        if element_id is None:
            return None
        path = self.element_ids().get(element_id)
        return self.module(path) if path else None

    def exported_names(
        self, module: Module, seen: set[pathlib.Path] | None = None
    ) -> tuple[dict[str, list[tuple[Module, Declaration]]], bool]:
        """Everything a module exports, including what it re-exports with `export import`.

        Returns the declarations by name, and whether it re-exports the std library.
        """
        seen = seen if seen is not None else set()
        seen.add(module.path)
        names: dict[str, list[tuple[Module, Declaration]]] = {
            name: [(module, declaration) for declaration in declarations]
            for name, declarations in module.exports.items()
        }
        exports_std = False
        for imported in module.imports:
            if not imported.exported or imported.namespace:
                continue
            if imported.is_std:
                exports_std = True
                continue
            target = self.resolve(imported)
            if target is None or target.path in seen:
                continue
            more, more_std = self.exported_names(target, seen)
            exports_std = exports_std or more_std
            for name, declarations in more.items():
                names.setdefault(name, []).extend(declarations)
        return names, exports_std

    def exported_std_modules(self, module: Module, seen: set[pathlib.Path] | None = None) -> set[str]:
        """The std modules (e.g. "tool.fs") a module re-exports with `export import`, directly or through the files
        it re-exports, and what they re-export in turn."""
        seen = seen if seen is not None else set()
        seen.add(module.path)
        library = stdlib()
        modules: set[str] = set()
        for imported in module.imports:
            if not imported.exported or imported.namespace:
                continue
            if imported.is_std:
                modules |= library.visible_modules(imported.path.removeprefix(STD_PREFIX))
                continue
            target = self.resolve(imported)
            if target is not None and target.path not in seen:
                modules |= self.exported_std_modules(target, seen)
        return modules

    def providers(self, module: Module) -> tuple[dict[str, list[Provider]], bool]:
        """The names a module can use from its imports, and whether it can use the std library."""
        if self._snapshot_depth:
            cached = self._snapshot_providers.get(id(module))
            if cached is None or cached[0] is not module:
                cached = self._snapshot_providers[id(module)] = (module, self._providers(module))
            return cached[1]
        return self._providers(module)

    def _providers(self, module: Module) -> tuple[dict[str, list[Provider]], bool]:
        names: dict[str, list[Provider]] = {}
        sees_std = False
        for imported in module.imports:
            if imported.namespace:
                continue
            if imported.is_std:
                sees_std = True
                continue
            target = self.resolve(imported)
            if target is None:
                continue
            exported, exports_std = self.exported_names(target)
            sees_std = sees_std or exports_std
            for name, declarations in exported.items():
                names.setdefault(name, []).extend(
                    Provider(imported, owner, declaration)
                    for owner, declaration in declarations
                )
        return names, sees_std

    def imported_names(self, module: Module) -> ImportedNames:
        """The names a module can use from its imports, for semantic highlighting."""
        providers, _ = self.providers(module)
        imported = ImportedNames()
        for name, provided in providers.items():
            provider = provided[0]
            declaration = provider.declaration
            if declaration.kind == "variable":
                variable = provider.module.parsed.variables.get(name)
                imported.names[name] = ("variable", bool(variable and variable.readonly))
            elif declaration.kind in ("feature", "function", "predicate", "enum", "type"):
                imported.names[name] = (declaration.kind, False)  # type: ignore[assignment]
            if declaration.kind == "enum":
                imported.enum_members[name] = set(provider.module.parsed.enum_members.get(name, []))
        return imported

    def importers(self, module: Module) -> list[Module]:
        """The modules which import module directly."""
        return [
            other
            for other in self.modules()
            if any(self.resolve(imported) is module for imported in other.imports)
        ]

    # Navigation

    def definitions(
        self, module: Module, offset: int
    ) -> list[tuple[Module, Declaration]]:
        """The declarations the token at offset refers to, in this file or one it imports."""
        unique = {}
        for owner, declaration in self._definitions(module, offset):
            unique.setdefault((owner.path, declaration.token.offset), (owner, declaration))
        return list(unique.values())

    def _definitions(
        self, module: Module, offset: int
    ) -> list[tuple[Module, Declaration]]:
        token = module.index.token_at(offset)
        if token is None:
            return []
        if token.kind == "string":
            return self._annotation_function(module, token)
        local = module.index.declaration_for_token(token)
        if local:
            return [(module, local)]
        if not _is_reference(module, token):
            return []
        index = module.index
        providers, _ = self.providers(module)
        previous = index.previous_token(token)
        if previous is not None and previous.value in (".", "?."):
            # A member of an enum declared in another file
            parent = index.previous_token(previous)
            if parent is None:
                return []
            return [
                (provider.module, member)
                for provider in providers.get(parent.value, [])
                if provider.declaration.kind == "enum"
                for member in provider.module.index.declarations_by_name.get(
                    token.value, []
                )
                if member.kind == "enumMember" and member.parent == parent.value
            ]
        return [
            (provider.module, provider.declaration)
            for provider in providers.get(token.value, [])
        ]

    def _annotation_function(self, module: Module, token: Token) -> list[tuple[Module, Declaration]]:
        """The function a string in an annotation names, e.g. `"Manipulator Change Function" : "stockManipulatorChange"`."""
        found = annotation_string(module, token)
        if found is None or found[0] not in FUNCTION_ANNOTATION_KEYS:
            return []
        name = found[1]
        local = [(module, declaration) for declaration in module.top_level.get(name, []) if declaration.kind == "function"]
        if local:
            return local
        providers, _ = self.providers(module)
        return [
            (provider.module, provider.declaration)
            for provider in providers.get(name, [])
            if provider.declaration.kind == "function"
        ]

    def std_definition(self, module: Module, offset: int) -> tuple[pathlib.Path, int, int, int] | None:
        """Where in the std library's source the token at offset is declared, as (path, line, start character, end
        character): for now, the std enum member a string in an annotation names, e.g. `"UIHint" : ["SHOW_LABEL"]`."""
        token = module.index.token_at(offset)
        found = annotation_string(module, token) if token is not None else None
        if found is None or found[0] not in ENUM_ANNOTATION_KEYS or self.std_dir is None:
            return None
        enum, member = ENUM_ANNOTATION_KEYS[found[0]], found[1]
        for symbol in stdlib().lookup(member):
            if symbol.kind == "enumMember" and symbol.parent == enum and symbol.module:
                location = _std_enum_member(self.std_dir / symbol.module, enum, member)
                if location is not None:
                    return location
        return None

    def references(
        self, module: Module, offset: int, include_declaration: bool = True
    ) -> list[tuple[Module, Token]]:
        """Every use of the declaration at offset, across every file which can see it."""
        results: list[tuple[Module, Token]] = []
        for owner, declaration in self.definitions(module, offset):
            results.extend(self._references_to(owner, declaration, include_declaration))
        return _dedupe(results)

    def references_to_name(self, name: str) -> list[tuple[Module, Declaration, list[tuple[Module, Token]]]]:
        """Every top-level declaration called name, with its references."""
        return [
            (module, declaration, self._references_to(module, declaration, False))
            for module in self.modules()
            for declaration in module.top_level.get(name, [])
        ]

    def _references_to(
        self, owner: Module, declaration: Declaration, include_declaration: bool
    ) -> list[tuple[Module, Token]]:
        results = [
            (owner, reference.token)
            for reference in owner.index.references_by_key.get(declaration.key, [])
            if include_declaration or reference.token.offset != declaration.token.offset
        ]
        if declaration.kind == "enumMember":
            exported = declaration.parent is not None and declaration.parent in owner.exports
        else:
            exported = declaration.name in owner.exports and any(
                d is declaration for d in owner.exports[declaration.name]
            )
        if not exported:
            return results
        for other in self.modules():
            if other is owner:
                continue
            providers, _ = self.providers(other)
            if declaration.kind == "enumMember":
                visible = any(
                    provider.module is owner
                    for provider in providers.get(declaration.parent or "", [])
                )
                if not visible:
                    continue
                for token in other.index.tokens:
                    previous = other.index.previous_token(token)
                    parent = other.index.previous_token(previous) if previous else None
                    if (
                        token.value == declaration.name
                        and previous is not None
                        and previous.value in (".", "?.")
                        and parent is not None
                        and parent.value == declaration.parent
                    ):
                        results.append((other, token))
                continue
            if not any(
                provider.declaration is declaration
                for provider in providers.get(declaration.name, [])
            ):
                continue
            for token in other.index.tokens:
                if token.value != declaration.name or not _is_reference(other, token):
                    continue
                local = other.index.declaration_for_token(token)
                if local is None or local.kind in TOP_LEVEL_KINDS:
                    # A top-level declaration of the same name may be another overload
                    results.append((other, token))
        return results

    # Usage

    def usages(self, modules: Iterable[Module] | None = None) -> list[Usage]:
        """How each top-level declaration (other than features and operators) in modules is used.

        A name in a string (such as `"Editing Logic Function" : "myEditLogic"`) counts as a use.
        """
        strings = {
            token.value[1:-1]
            for module in self.modules()
            for token in module.index.tokens
            if token.kind == "string"
        }
        usages = []
        for module in modules if modules is not None else self.modules():
            for name, declarations in module.top_level.items():
                for declaration in declarations:
                    if declaration.kind in ("feature", "operator"):
                        continue
                    references = self._references_to(module, declaration, False)
                    usages.append(
                        Usage(
                            module,
                            declaration,
                            any(d is declaration for d in module.exports.get(name, [])),
                            sum(1 for owner, _ in references if owner is module),
                            sorted(
                                {owner.path: owner for owner, _ in references if owner is not module}.values(),
                                key=lambda owner: owner.relative,
                            ),
                            name in strings,
                        )
                    )
        return usages

    # Diagnostics

    def check(self, module: Module) -> list[Problem]:
        """Syntax problems, names which aren't defined anywhere, and unused or unknown imports."""
        problems = [
            Problem(
                module.parsed.line_map.offset(diagnostic.range.start.line, diagnostic.range.start.character),
                module.parsed.line_map.offset(diagnostic.range.end.line, diagnostic.range.end.character),
                "error",
                diagnostic.message,
                "syntax",
            )
            for diagnostic in syntax_diagnostics(module.parsed)
        ]
        providers, sees_std = self.providers(module)
        library = stdlib()
        used: set[int] = set()  # ids of Imports
        index = module.index
        for token in index.tokens:
            if not _is_reference(module, token):
                continue
            previous = index.previous_token(token)
            if previous is not None and previous.value in (".", "?."):
                continue
            name = token.value
            for provider in providers.get(name, []):
                used.add(id(provider.via))
            if (
                name in providers
                or index.declaration_for_token(token)
                or (sees_std and library.lookup(name))
                or built_in_type(name)
                or name in IMPLICIT_NAMES
            ):
                continue
            problems.append(
                Problem(
                    token.offset,
                    token.end,
                    "error",
                    f"{name} isn't defined in this file or anything it imports.",
                    "undefined",
                )
            )

        problems.extend(self._bare_key_problems(module, providers))
        problems.extend(_duplicate_symbol_problems(module, providers))
        problems.extend(_duplicate_overload_problems(module, providers))
        problems.extend(_boolean_comparison_problems(module))
        problems.extend(_keyword_key_problems(module))
        problems.extend(_function_value_problems(module, providers))
        problems.extend(_precondition_problems(module, providers))
        problems.extend(self._parameter_enum_problems(module, providers))
        problems.extend(self._precondition_predicate_problems(module, providers))
        problems.extend(self._duplicate_parameter_problems(module))
        problems.extend(self._array_group_problems(module))
        problems.extend(self._tolerant_parameter_problems(module))
        problems.extend(self._nested_predicate_problems(module, providers))
        for usage in self.usages([module]):
            if not usage.exported and usage.unused:
                token = usage.declaration.token
                problems.append(
                    Problem(
                        token.offset,
                        token.end,
                        "warning",
                        f"{token.value} isn't used anywhere.",
                        "unused",
                        unnecessary=True,
                    )
                )

        known_ids = self.element_ids()
        for imported in module.imports:
            if imported.exported and imported.path == COMMON_STD and not imported.namespace:
                problems.append(
                    Problem(
                        imported.start,
                        imported.end,
                        "error",
                        "Don't export common.fs: everything importing this file would see all of std through it. "
                        "To export a std enum a parameter uses, export import just the std module declaring it.",
                        "exported-common",
                    )
                )
            if imported.namespace:
                if not (imported.is_std or imported.element_id or (self.code_dir / imported.path).is_file()):
                    # An image (or a studio) imported by path, which `fs push` uploads and resolves
                    problems.append(
                        Problem(
                            imported.token.offset,
                            imported.token.end,
                            "error",
                            f"{imported.path} doesn't exist in the code folder.",
                            "unknown-import",
                        )
                    )
                if not imported.exported and not any(
                    token.value == imported.namespace
                    and token.offset >= imported.end
                    for token in index.tokens
                ):
                    problems.append(_unused(imported, imported.namespace))
                continue
            if imported.is_std or not (imported.element_id or imported.code_path):
                continue
            target = self.resolve(imported)
            if target is None and imported.code_path:
                problems.append(
                    Problem(
                        imported.token.offset,
                        imported.token.end,
                        "error",
                        f"{imported.code_path} doesn't exist in the code folder.",
                        "unknown-import",
                    )
                )
                continue
            if target is None:
                if known_ids:
                    problems.append(
                        Problem(
                            imported.token.offset,
                            imported.token.end,
                            "error",
                            f"No Feature Studio with element id {imported.element_id} is synced; "
                            "it may have been deleted (see `fs status`).",
                            "unknown-import",
                        )
                    )
                continue
            if imported.exported or id(imported) in used:
                continue
            exported, _ = self.exported_names(target)
            if not exported or any(
                declaration.kind == "operator"
                for declarations in exported.values()
                for _, declaration in declarations
            ):
                # Operators are used without naming them
                continue
            problems.append(_unused(imported, target.relative))
        problems = [problem for problem in problems if not _is_ignored(module, problem)]
        return sorted(problems, key=lambda problem: problem.start)


    def _parameter_enum_problems(
        self, module: Module, providers: dict[str, list[Provider]]
    ) -> list[Problem]:
        """Enums used as a feature's parameter types which its file doesn't export.

        Onshape requires them to be exported by the feature's file: declared there with `export`, or re-exported
        with `export import` (of the project file, or the std module, declaring it).
        """
        exported, _ = self.exported_names(module)
        exported_std: set[str] | None = None
        library = stdlib()
        problems = []
        for node in module.parsed.nodes:
            if node.type != "PreconditionBlock":
                continue
            feature = module.index.enclosing(node.token, frozenset(["FeatureDeclaration"]))
            if feature is None:
                continue
            for name, report_at in self._parameter_types(module, node.start, node.end, set()):
                if not self._is_project_enum(module, name, providers):
                    std_modules = [symbol.module for symbol in library.lookup(name) if symbol.kind == "enum"]
                    if not std_modules or None in std_modules:
                        continue
                    if exported_std is None:
                        exported_std = self.exported_std_modules(module)
                    if any(std_module in exported_std for std_module in std_modules):
                        continue
                    problems.append(
                        Problem(
                            report_at.offset,
                            report_at.end,
                            "error",
                            f"{name} is a parameter type of {feature.name}, so this file must export it: export import "
                            f'"{STD_PREFIX}{std_modules[0]}", which declares it (not common.fs, which would export all '
                            "of std).",
                            "unexported-parameter-enum",
                        )
                    )
                    continue
                if any(d.kind == "enum" for _, d in exported.get(name, [])):
                    continue
                problems.append(
                    Problem(
                        report_at.offset,
                        report_at.end,
                        "error",
                        f"{name} is a parameter type of {feature.name}, so this file must export it "
                        f"(declare it with export, or export import the file declaring it).",
                        "unexported-parameter-enum",
                    )
                )
        return _dedupe_problems(problems)

    def _parameter_types(
        self, module: Module, start: int, end: int, seen: set[tuple[pathlib.Path, int]]
    ) -> list[tuple[str, Token]]:
        """The types after `x.y is` in a region, and in the predicates it calls (reported at the call)."""
        tokens = module.index.tokens
        found: list[tuple[str, Token]] = []
        providers, _ = self.providers(module)
        for position, token in enumerate(tokens):
            if not start <= token.offset < end:
                continue
            following = tokens[position + 1] if position + 1 < len(tokens) else None
            if (
                token.value == "is"
                and position >= 2
                and tokens[position - 2].value == "."
                and following is not None
                and following.kind == "identifier"
            ):
                found.append((following.value, following))
            elif token.kind == "identifier" and following is not None and following.value == "(":
                targets = [
                    (module, d)
                    for d in [module.index.declaration_for_token(token)]
                    if d is not None and d.kind == "predicate"
                ] or [
                    (provider.module, provider.declaration)
                    for provider in providers.get(token.value, [])
                    if provider.declaration.kind == "predicate"
                ]
                for owner, declaration in targets:
                    key = (owner.path, declaration.token.offset)
                    if key in seen:
                        continue
                    seen.add(key)
                    body = next(
                        (
                            n
                            for n in owner.parsed.nodes
                            if n.type == "PredicateDeclaration" and n.start <= declaration.token.offset < n.end
                        ),
                        None,
                    )
                    if body is not None:
                        found.extend(
                            (name, token)
                            for name, _ in self._parameter_types(owner, body.start, body.end, seen)
                        )
        return found

    def _precondition_predicate_problems(
        self, module: Module, providers: dict[str, list[Provider]]
    ) -> list[Problem]:
        """Predicates a feature's precondition uses (directly or through other predicates) which its file can't see.

        Onshape inlines every predicate a precondition calls, looking each one up from the feature's file. A
        predicate another file declares without exporting it (or which isn't exported to the feature's file)
        makes Onshape's precondition analysis fail, so it can't show the feature's parameters.
        """
        problems = []
        for node in module.parsed.nodes:
            if node.type != "PreconditionBlock":
                continue
            feature = module.index.enclosing(
                node.token, frozenset(["FeatureDeclaration"])
            )
            if feature is None:
                continue
            for owner, declaration, report_at in self._called_predicates(
                module, node.start, node.end, set()
            ):
                if owner.path == module.path or declaration.name in providers:
                    continue
                problems.append(
                    Problem(
                        report_at.offset,
                        report_at.end,
                        "error",
                        f"{feature.name}'s precondition uses the predicate {declaration.name}, which "
                        f"{owner.path.name} doesn't export to this file, so Onshape can't analyze the precondition. "
                        f"Export it.",
                        "unexported-predicate",
                    )
                )
        return _dedupe_problems(problems)

    def _called_predicates(
        self, module: Module, start: int, end: int, seen: set[tuple[pathlib.Path, int]]
    ) -> list[tuple[Module, Declaration, Token]]:
        """The project predicates called in a region and, transitively, by them.

        Each is returned with the module declaring it, and the call in the original region it's reached through.
        """
        tokens = module.index.tokens
        providers, _ = self.providers(module)
        found: list[tuple[Module, Declaration, Token]] = []
        for position, token in enumerate(tokens):
            if not start <= token.offset < end or token.kind != "identifier":
                continue
            following = tokens[position + 1] if position + 1 < len(tokens) else None
            if following is None or following.value != "(":
                continue
            local = module.index.declaration_for_token(token)
            targets = (
                [(module, local)]
                if local is not None and local.kind == "predicate"
                else [
                    (provider.module, provider.declaration)
                    for provider in providers.get(token.value, [])
                    if provider.declaration.kind == "predicate"
                ]
            )
            if not targets and not stdlib().lookup(token.value):
                # Not visible here: find it anywhere in the project
                targets = [
                    (other, declaration)
                    for other in self.modules()
                    for declaration in other.index.declarations_by_name.get(
                        token.value, []
                    )
                    if declaration.kind == "predicate"
                    and declaration.scope_start == other.parsed.start
                ]
            for owner, declaration in targets[:1]:
                key = (owner.path, declaration.token.offset)
                if key in seen:
                    continue
                seen.add(key)
                found.append((owner, declaration, token))
                body = next(
                    (
                        n
                        for n in owner.parsed.nodes
                        if n.type == "PredicateDeclaration"
                        and n.start <= declaration.token.offset < n.end
                    ),
                    None,
                )
                if body is not None:
                    found.extend(
                        (inner_owner, inner, token)
                        for inner_owner, inner, _ in self._called_predicates(
                            owner, body.start, body.end, seen
                        )
                    )
        return found

    def _duplicate_parameter_problems(self, module: Module) -> list[Problem]:
        """Parameters a feature's precondition declares more than once, directly or through predicates.

        Onshape rejects these ("Duplicate feature parameter"), even when they're in different branches of an if.
        """
        problems = []
        for node in module.parsed.nodes:
            if node.type != "PreconditionBlock":
                continue
            feature = module.index.enclosing(node.token, frozenset(["FeatureDeclaration"]))
            if feature is None:
                continue
            first: dict[str, Token] = {}
            for name, report_at in self._declared_parameters(module, node.start, node.end, "definition", frozenset()):
                if name not in first:
                    first[name] = report_at
                    continue
                line = module.position(first[name].offset)[0] + 1
                problems.append(
                    Problem(
                        report_at.offset,
                        report_at.end,
                        "error",
                        f"Duplicate feature parameter {name}: {feature.name}'s precondition already declares it "
                        f"(line {line}). Onshape rejects this even in different branches of an if.",
                        "duplicate-parameter",
                    )
                )
        return _dedupe_problems(problems)

    def _array_group_problems(self, module: Module) -> list[Problem]:
        """Groups ("Group Name" annotations) in array parameters' items, which Onshape rejects ("Parameter groups not
        permitted inside array parameters"). Only those directly in a precondition's loops are found."""
        problems = []
        tokens = module.index.tokens
        for node in module.parsed.nodes:
            if node.type != "PreconditionBlock":
                continue
            inside = [index for index, token in enumerate(tokens) if node.start <= token.offset < node.end]
            for index in inside:
                if tokens[index].value != "for" or tokens[index].kind == "string":
                    continue
                # The loop's body: the block after its parenthesized header
                after_header = _matching_index(tokens, index + 1)
                if after_header is None or after_header + 1 >= len(tokens) or tokens[after_header + 1].value != "{":
                    continue
                end = _matching_index(tokens, after_header + 1)
                for token in tokens[after_header + 1 : end]:
                    if token.kind == "string" and token.value == '"Group Name"':
                        problems.append(
                            Problem(
                                token.offset,
                                token.end,
                                "error",
                                "Onshape doesn't allow groups in array parameters' items.",
                                "array-group",
                            )
                        )
        return _dedupe_problems(problems)

    def _tolerant_parameter_problems(self, module: Module) -> list[Problem]:
        """Parameters which allow field tolerancing ("UIHint" CAN_BE_TOLERANT), which ours never do (see
        docs/featurescript-style.md): in annotations, and declared by std predicates the file calls (like std's
        extrudeBoundParametersPredicate)."""
        problems = []
        index = module.index
        annotations = [node for node in module.parsed.nodes if node.type == "AnnotationMap"]
        for position, token in enumerate(index.tokens):
            if token.value in ("CAN_BE_TOLERANT", '"CAN_BE_TOLERANT"') and any(
                node.start <= token.offset < node.end for node in annotations
            ):
                problems.append(
                    Problem(
                        token.offset,
                        token.end,
                        "warning",
                        "Don't allow field tolerancing (CAN_BE_TOLERANT): our parameters never do.",
                        "tolerant-parameter",
                    )
                )
                continue
            if (
                token.kind != "identifier"
                or position + 1 >= len(index.tokens)
                or index.tokens[position + 1].value != "("
                or index.declaration_for_token(token) is not None
            ):
                continue
            previous = index.previous_token(token)
            if previous is not None and previous.value in (".", "function", "predicate"):
                continue
            if self._std_tolerant(token.value):
                problems.append(
                    Problem(
                        token.offset,
                        token.end,
                        "warning",
                        f"std's {token.value} declares parameters which allow field tolerancing (CAN_BE_TOLERANT), "
                        "which ours never do. Use a copy without it, like core/stdExtrude.fs's extrudeBoundsPredicate.",
                        "tolerant-parameter",
                    )
                )
        return problems

    def _std_tolerant(self, name: str, seen: frozenset[str] = frozenset()) -> bool:
        """Whether a std predicate declares a parameter which allows field tolerancing, directly or through the std
        predicates it calls. Needs the copy of std's source (`std_dir`)."""
        if name in self._std_tolerant_cache:
            return self._std_tolerant_cache[name]
        result = False
        if self.std_dir is not None and name not in seen:
            for symbol in stdlib().lookup(name):
                if symbol.kind != "predicate" or not symbol.module:
                    continue
                path = self.std_dir / symbol.module
                body = _std_predicate_body(path, name)
                if body is None:
                    continue
                if "CAN_BE_TOLERANT" in body or any(
                    self._std_tolerant(called, seen | {name})
                    for called in set(re.findall(r"\b([A-Za-z_]\w*)\s*\(", body))
                    if called != name and any(s.kind == "predicate" for s in stdlib().lookup(called))
                ):
                    result = True
                    break
        self._std_tolerant_cache[name] = result
        return result

    def _declared_parameters(
        self, module: Module, start: int, end: int, map_name: str, path: frozenset[tuple[pathlib.Path, int]]
    ) -> list[tuple[str, Token]]:
        """The parameters a region of a precondition declares on map_name, in order, and through the predicates it
        passes map_name to (reported at the call).

        A declaration is a statement `map_name.x is Type;` or `isLength(map_name.x, ...);` (see PARAMETER_PREDICATES).
        """
        tokens = module.index.tokens
        providers, _ = self.providers(module)
        found: list[tuple[str, Token]] = []

        def value(position: int) -> str | None:
            return tokens[position].value if position < len(tokens) else None

        for position, token in enumerate(tokens):
            if not start <= token.offset < end or token.kind != "identifier":
                continue
            if position and tokens[position - 1].value not in (";", "{", "}"):
                continue  # Not the start of a statement
            if token.value == map_name and value(position + 1) == "." and value(position + 3) == "is":
                found.append((tokens[position + 2].value, tokens[position + 2]))
            elif value(position + 1) != "(":
                continue
            elif token.value in PARAMETER_PREDICATES:
                if value(position + 2) == map_name and value(position + 3) == "." and value(position + 5) in (",", ")"):
                    found.append((tokens[position + 4].value, tokens[position + 4]))
            else:
                for owner, declaration in self._predicate_targets(module, token, providers)[:1]:
                    key = (owner.path, declaration.token.offset)
                    body = _predicate_node(owner, declaration)
                    if key in path or body is None:
                        continue
                    names = _parameter_names(owner, body)
                    inner = next(
                        (
                            names[index]
                            for index, argument in enumerate(_call_arguments(tokens, position + 1))
                            if index < len(names) and [t.value for t in argument] == [map_name]
                        ),
                        None,
                    )
                    if inner is not None:
                        found.extend(
                            (name, token)
                            for name, _ in self._declared_parameters(owner, body.start, body.end, inner, path | {key})
                        )
        return found

    def _nested_predicate_problems(self, module: Module, providers: dict[str, list[Provider]]) -> list[Problem]:
        """Predicates in a precondition's if conditions which call other predicates.

        Onshape inlines a predicate in a condition, but not the predicates it calls ("Nesting predicates are not
        allowed in if statements in preconditions"). Conditions in feature preconditions, and in predicates which
        some feature's precondition calls, are checked.
        """
        in_preconditions = self._precondition_predicates()
        regions = []
        for node in module.parsed.nodes:
            if node.type == "PreconditionBlock":
                if module.index.enclosing(node.token, frozenset(["FeatureDeclaration"])) is not None:
                    regions.append(node)
            elif node.type == "PredicateDeclaration" and (module.path, node.start) in in_preconditions:
                regions.append(node)

        tokens = module.index.tokens
        problems = []
        for region in regions:
            for open_index, close_index in _if_conditions(tokens, region.start, region.end):
                for position in range(open_index + 1, close_index):
                    token = tokens[position]
                    if token.kind != "identifier" or tokens[position + 1].value != "(":
                        continue
                    for owner, declaration in self._predicate_targets(module, token, providers)[:1]:
                        body = _predicate_node(owner, declaration)
                        nested = None if body is None else self._first_predicate_call(owner, declaration, body)
                        if nested is None:
                            continue
                        problems.append(
                            Problem(
                                token.offset,
                                token.end,
                                "error",
                                f"{declaration.name} calls the predicate {nested}, and Onshape doesn't allow nesting "
                                f"predicates in a precondition's if conditions. Write out {nested}'s condition in "
                                f"{declaration.name} instead.",
                                "nested-predicate",
                            )
                        )
        return _dedupe_problems(problems)

    def _precondition_predicates(self) -> set[tuple[pathlib.Path, int]]:
        """The (path, start) of every PredicateDeclaration which a feature's precondition calls, directly or not."""
        modules = self.modules()
        key = tuple(id(module) for module in modules)
        if self._precondition_predicate_cache is not None and self._precondition_predicate_cache[0] == key:
            return self._precondition_predicate_cache[1]
        found = set()
        for module in modules:
            for node in module.parsed.nodes:
                if node.type != "PreconditionBlock":
                    continue
                if module.index.enclosing(node.token, frozenset(["FeatureDeclaration"])) is None:
                    continue
                for owner, declaration, _ in self._called_predicates(module, node.start, node.end, set()):
                    body = _predicate_node(owner, declaration)
                    if body is not None:
                        found.add((owner.path, body.start))
        self._precondition_predicate_cache = (key, found)
        return found

    def _first_predicate_call(self, module: Module, declaration: Declaration, node: AstNode) -> str | None:
        """The name of the first predicate (in the project or std) called in a predicate's body."""
        tokens = module.index.tokens
        providers, _ = self.providers(module)
        for position, token in enumerate(tokens):
            if not node.start <= token.offset < node.end or token.kind != "identifier":
                continue
            if position + 1 >= len(tokens) or tokens[position + 1].value != "(":
                continue
            if token.offset == declaration.token.offset:
                continue  # The predicate's own name
            if self._predicate_targets(module, token, providers) or (
                module.index.declaration_for_token(token) is None
                and any(symbol.kind == "predicate" for symbol in stdlib().lookup(token.value))
            ):
                return token.value
        return None

    def _predicate_targets(
        self, module: Module, token: Token, providers: dict[str, list[Provider]]
    ) -> list[tuple[Module, Declaration]]:
        """The project predicates a call in module may be to, with the modules declaring them."""
        local = module.index.declaration_for_token(token)
        if local is not None:
            return [(module, local)] if local.kind == "predicate" else []
        return [
            (provider.module, provider.declaration)
            for provider in providers.get(token.value, [])
            if provider.declaration.kind == "predicate"
        ]

    def _is_project_enum(self, module: Module, name: str, providers: dict[str, list[Provider]]) -> bool:
        """Whether name is an enum declared in the project, rather than in std (or not an enum)."""
        return (
            name in module.parsed.enums
            or any(p.declaration.kind == "enum" for p in providers.get(name, []))
            or any(name in other.parsed.enums for other in self.modules())
        )

    def _bare_key_problems(
        self, module: Module, providers: dict[str, list[Provider]]
    ) -> list[Problem]:
        """Map keys written as bare names which are also constants or variables in scope.

        `{ KEY : value }` uses the string "KEY", not KEY's value (that's `{ (KEY) : value }`), which is
        easy to get wrong when KEY is a constant.
        """
        index = module.index
        problems = []
        for hint in module.parsed.hints:
            token = hint.token
            if hint.type != "mapKey" or token.kind != "identifier":
                continue
            local = index._choose(
                token,
                lambda d: d.kind in ("variable", "parameter")
                and d.visible_from <= token.offset <= d.scope_end,
            )
            if local is None and not any(
                provider.declaration.kind == "variable" for provider in providers.get(token.value, [])
            ):
                continue
            problems.append(
                Problem(
                    token.offset,
                    token.end,
                    "warning",
                    f'This key is the string "{token.value}"; write ({token.value}) to use the value of {token.value}.',
                    "bare-key",
                )
            )
        return problems


def _duplicate_symbol_problems(module: Module, providers: dict[str, list[Provider]]) -> list[Problem]:
    """Top-level constants, enums, and types whose names are already declared in the file or anything it imports.

    Onshape rejects these ("Duplicate top level symbol name"), even when neither is exported. Functions, predicates,
    and operators may share names, as overloads. Std is checked against the std modules the file imports (and what
    they re-export).
    """
    library = stdlib()
    std_modules: set[str] = set()
    for imported in module.imports:
        if imported.is_std and not imported.namespace:
            std_modules |= library.visible_modules(imported.path.removeprefix(STD_PREFIX))
    problems = []
    for name, declarations in module.top_level.items():
        ordered = sorted(declarations, key=lambda declaration: declaration.token.offset)
        for position, declaration in enumerate(ordered):
            unique = declaration.kind in UNIQUE_KINDS
            earlier = next(
                (other for other in ordered[:position] if unique or other.kind in UNIQUE_KINDS), None
            )
            if earlier is not None:
                where = f"this file (line {module.position(earlier.token.offset)[0] + 1})"
            else:
                imported = next(
                    (
                        provider
                        for provider in providers.get(name, [])
                        if unique or provider.declaration.kind in UNIQUE_KINDS
                    ),
                    None,
                )
                std = [
                    symbol
                    for symbol in library.lookup(name)
                    if symbol.module in std_modules
                    and symbol.parent is None
                    and symbol.kind != "enumMember"
                    and (unique or symbol.kind in STD_UNIQUE_KINDS)
                ]
                if imported is not None:
                    where = imported.module.relative
                elif std:
                    where = f"the std library ({std[0].module})" if std[0].module else "the std library"
                else:
                    continue
            problems.append(
                Problem(
                    declaration.token.offset,
                    declaration.token.end,
                    "error",
                    f"Duplicate top level symbol name {name}: {where} already declares it. Rename it.",
                    "duplicate-symbol",
                )
            )
    return problems


def _duplicate_overload_problems(module: Module, providers: dict[str, list[Provider]]) -> list[Problem]:
    """Functions and predicates with the same name and parameter types as others in the file or anything it imports.

    They're overloads Onshape can't choose between, so it rejects calls to them ("More than one matching predicate
    declaration"), often by failing to analyze a precondition calling one. Std isn't checked.
    """
    problems = []
    for name, declarations in module.top_level.items():
        ordered = sorted(
            (declaration for declaration in declarations if declaration.kind in CALLABLE_KINDS),
            key=lambda declaration: declaration.token.offset,
        )
        for position, declaration in enumerate(ordered):
            types = _parameter_types(module, declaration)
            earlier = next((other for other in ordered[:position] if _parameter_types(module, other) == types), None)
            if earlier is not None:
                where = f"this file (line {module.position(earlier.token.offset)[0] + 1})"
            else:
                imported = next(
                    (
                        provider
                        for provider in providers.get(name, [])
                        if provider.declaration.kind in CALLABLE_KINDS
                        and _parameter_types(provider.module, provider.declaration) == types
                    ),
                    None,
                )
                if imported is None:
                    continue
                where = imported.module.relative
            problems.append(
                Problem(
                    declaration.token.offset,
                    declaration.token.end,
                    "error",
                    f"Duplicate {declaration.kind} {name}({', '.join(types)}): {where} already declares one with the "
                    "same parameter types, so Onshape can't tell which to call. Remove or rename one.",
                    "duplicate-overload",
                )
            )
    return problems


def _parameter_types(module: Module, declaration: Declaration) -> tuple[str, ...]:
    """The types of a function's or predicate's parameters (empty for untyped ones), which tell overloads apart."""
    tokens = module.index.tokens
    index = module.index.token_index_by_offset.get(declaration.token.offset)
    if index is None or index + 1 >= len(tokens) or tokens[index + 1].value != "(":
        return ()
    parameters: list[list[str]] = [[]]
    depth = 0
    for token in tokens[index + 2 :]:
        if token.kind == "string":
            parameters[-1].append(token.value)
            continue
        if token.value in ("(", "[", "{"):
            depth += 1
        elif token.value in (")", "]", "}"):
            if depth == 0:
                break
            depth -= 1
        elif token.value == "," and depth == 0:
            parameters.append([])
            continue
        parameters[-1].append(token.value)
    if parameters == [[]]:
        return ()
    return tuple(" ".join(values[values.index("is") + 1 :]) if "is" in values else "" for values in parameters)


def _function_value_problems(module: Module, providers: dict[str, list[Provider]]) -> list[Problem]:
    """Functions and predicates declared with `function` or `predicate` and used as values, e.g. `mapArray(a, f)`.

    FeatureScript only allows calling them ("Cannot reference function f as a variable"); declare one which is passed
    around as a const set to a function, `const f = function(...) { ... };`, instead. Std's names aren't checked, since many of std's
    "functions" are consts which can be passed around.
    """
    index = module.index
    problems = []
    for token in index.tokens:
        if not _is_reference(module, token):
            continue
        previous = index.previous_token(token)
        following = index.next_token(token)
        if following is not None and following.value == "(":
            continue
        if previous is not None and previous.value in (".", "?.", "typecheck", "is", "as", "returns"):
            continue
        local = index.declaration_for_token(token)
        kinds = (
            [local.kind]
            if local is not None
            else [provider.declaration.kind for provider in providers.get(token.value, [])]
        )
        if not kinds or any(kind not in ("function", "predicate") for kind in kinds):
            continue
        kind = kinds[0]
        problems.append(
            Problem(
                token.offset,
                token.end,
                "error",
                f"{token.value} is a {kind}, which FeatureScript can only call, not use as a value; declare it as a "
                f"const set to a function instead (`const {token.value} = function(...) {{ ... }};`).",
                "function-value",
            )
        )
    return problems


_IGNORE = re.compile(r"//\s*fs check: ignore ([\w, -]+)")


def _is_ignored(module: Module, problem: Problem) -> bool:
    """Whether a problem is ignored by a comment on its line, or alone on the line before it:
    `// fs check: ignore keyword-key` (codes separated by commas), with why, e.g. `std's hole attributes need it`."""
    line = module.position(problem.start)[0]
    for text in (module.line_text(line), module.line_text(line - 1)):
        match = _IGNORE.search(text)
        if match and problem.code in {code.strip() for code in match.group(1).split(",")}:
            return True
    return False


def _keyword_key_problems(module: Module) -> list[Problem]:
    """Keywords used as map keys. `x.type` is a syntax error in Onshape (it expects a name after the `.`), so a key like
    `"type"` can only be read as `x["type"]`; avoid such keys unless a format needs them (like a lookup table's
    `"default"`, so generated files aren't checked for those)."""
    problems = []
    tokens = module.index.tokens
    generated = module.path.name.endswith(".gen.fs")
    for index, token in enumerate(tokens):
        previous = tokens[index - 1] if index else None
        following = tokens[index + 1] if index + 1 < len(tokens) else None
        if token.kind == "keyword" and previous is not None and previous.value in (".", "?."):
            problems.append(
                Problem(
                    token.offset,
                    token.end,
                    "error",
                    f'{token.value} is a keyword, so Onshape can\'t read it after a "."; write ["{token.value}"], or '
                    f"better, rename the key.",
                    "keyword-key",
                )
            )
        elif (
            not generated
            and token.kind == "string"
            and token.value[1:-1] in KEYWORDS
            and previous is not None
            and previous.value in ("{", ",")
            and following is not None
            and following.value == ":"
        ):
            problems.append(
                Problem(
                    token.offset,
                    token.end,
                    "warning",
                    f'The key {token.value} is a keyword, so it can only be read as ["{token.value[1:-1]}"], not '
                    f".{token.value[1:-1]}; use another name unless something needs this one.",
                    "keyword-key",
                )
            )
    return problems


def _boolean_comparison_problems(module: Module) -> list[Problem]:
    """Comparisons with true or false, which are redundant: `x == true` is `x`."""
    problems = []
    tokens = module.index.tokens
    for index, token in enumerate(tokens):
        if token.value not in ("==", "!="):
            continue
        for neighbor in (tokens[index - 1] if index else None, tokens[index + 1] if index + 1 < len(tokens) else None):
            if neighbor is not None and neighbor.value in ("true", "false"):
                problems.append(
                    Problem(
                        token.offset,
                        neighbor.end if neighbor.offset > token.offset else token.end,
                        "warning",
                        f"Comparing with {neighbor.value} is redundant; use the value (or ! it) directly.",
                        "boolean-comparison",
                    )
                )
    return problems


def _precondition_problems(module: Module, providers: dict[str, list[Provider]]) -> list[Problem]:
    """Conditions Onshape can't evaluate in a feature's precondition (or a predicate, which may be used in one).

    Onshape works out which parameters to show without running the precondition, so its `if` conditions can
    only use parameters (`definition.x`, including loop variables over array parameters), enum values,
    literals, and predicates (which it inlines). Constants and other functions don't work.
    """
    index = module.index
    nodes = module.parsed.nodes
    regions = []
    for node in nodes:
        if node.type == "PreconditionBlock":
            feature = index.enclosing(node.token, frozenset(["FeatureDeclaration"]))
            if feature is not None:
                regions.append((node.start, node.end, {"definition"}))
        elif node.type == "PredicateDeclaration":
            parameters = {
                declaration.name
                for declaration in index.declarations
                if declaration.kind == "parameter" and declaration.scope_start == node.start
            }
            regions.append((node.start, node.end, parameters))

    enums = set(module.parsed.enums) | {
        name for name, provided in providers.items() if provided[0].declaration.kind == "enum"
    } | stdlib().enum_names
    problems = []
    tokens = index.tokens
    for start, end, allowed in regions:
        for position, token in enumerate(tokens):
            if token.value != "if" or not start <= token.offset < end:
                continue
            if position + 1 >= len(tokens) or tokens[position + 1].value != "(":
                continue
            depth, cursor = 0, position + 1
            while cursor < len(tokens):
                value = tokens[cursor].value
                depth += value == "("
                depth -= value == ")"
                if depth == 0:
                    break
                current = tokens[cursor]
                cursor += 1
                if current.kind != "identifier":
                    continue
                previous = tokens[cursor - 2]
                following = tokens[cursor] if cursor < len(tokens) else None
                if previous.value in (".", "?.", "is", "as", "::") or (following is not None and following.value == "::"):
                    continue  # A property, a type, or a namespace
                if current.value in allowed:
                    continue
                if current.value in enums and following is not None and following.value == ".":
                    continue
                local = index.declaration_for_token(current)
                if local is not None and (
                    local.kind == "predicate"
                    or (local.kind in ("parameter", "variable") and local.scope_start != module.parsed.start)
                ):
                    continue  # A predicate, or a local (e.g. loop) variable
                if any(p.declaration.kind == "predicate" for p in providers.get(current.value, [])):
                    continue
                if any(symbol.kind == "predicate" for symbol in stdlib().lookup(current.value)):
                    continue
                if local is None and current.value not in providers and not stdlib().lookup(current.value):
                    continue  # Undefined, which is reported separately
                problems.append(
                    Problem(
                        current.offset,
                        current.end,
                        "warning",
                        f"Onshape can't evaluate {current.value} in a precondition's condition; only parameters "
                        "(definition.x), enum values, literals, and predicates work there.",
                        "precondition",
                    )
                )
    return problems


def annotation_string(module: Module, token: Token) -> tuple[str, str] | None:
    """The key and value (without quotes) of a string value in an annotation, e.g. `"Editing Logic Function" : "name"`,
    or of one of the strings in an array value, e.g. `"UIHint" : ["SHOW_LABEL"]`."""
    if token.kind != "string" or not any(
        node.type == "AnnotationMap" and node.start <= token.offset < node.end for node in module.parsed.nodes
    ):
        return None
    index = module.index
    previous = index.previous_token(token)
    if previous is not None and previous.value in ("[", ","):
        # An item of an array: its key is before the array
        while previous is not None and previous.value != "[":
            if previous.value in (":", "{", "}"):
                return None
            previous = index.previous_token(previous)
        previous = index.previous_token(previous) if previous is not None else None
    if previous is None or previous.value != ":":
        return None
    key = index.previous_token(previous)
    if key is None or key.kind != "string":
        return None
    return key.value[1:-1], token.value[1:-1]


def _std_enum_member(path: pathlib.Path, enum: str, member: str) -> tuple[pathlib.Path, int, int, int] | None:
    """Where an enum member is declared in a std source file."""
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").split("\n")
    except OSError:
        return None
    in_enum = False
    for number, line in enumerate(lines):
        if re.search(rf"\benum\s+{re.escape(enum)}\b", line):
            in_enum = True
        elif in_enum:
            match = re.match(rf"\s*({re.escape(member)})\b", line)
            if match:
                return path, number, match.start(1), match.end(1)
            if line.strip().startswith("}"):
                return None
    return None


def _std_predicate_body(path: pathlib.Path, name: str) -> str | None:
    """The source of a predicate's body in a std file, without its comments, or None if it isn't there."""
    try:
        text = path.read_text(encoding="utf-8")
    except OSError:
        return None
    text = re.sub(r"//[^\n]*|/\*.*?\*/", "", text, flags=re.S)
    match = re.search(r"\bpredicate\s+" + re.escape(name) + r"\s*\([^)]*\)\s*\{", text)
    if match is None:
        return None
    depth, position = 1, match.end()
    while depth and position < len(text):
        depth += {"{": 1, "}": -1}.get(text[position], 0)
        position += 1
    return text[match.end() : position - 1]


def _predicate_node(module: Module, declaration: Declaration) -> AstNode | None:
    """The PredicateDeclaration node of a predicate's declaration."""
    return next(
        (
            node
            for node in module.parsed.nodes
            if node.type == "PredicateDeclaration" and node.start <= declaration.token.offset < node.end
        ),
        None,
    )


def _parameter_names(module: Module, node: AstNode) -> list[str]:
    """The names of a function or predicate's parameters, in order."""
    return [
        declaration.name
        for declaration in sorted(module.index.declarations, key=lambda declaration: declaration.token.offset)
        if declaration.kind == "parameter" and declaration.scope_start == node.start
    ]


def _call_arguments(tokens: list[Token], open_index: int) -> list[list[Token]]:
    """The tokens of each argument of a call whose ( is at open_index."""
    arguments: list[list[Token]] = [[]]
    depth = 0
    for token in tokens[open_index:]:
        if token.value in ("(", "[", "{", "?["):
            depth += 1
            if depth == 1:
                continue
        elif token.value in (")", "]", "}"):
            depth -= 1
            if depth == 0:
                break
        elif token.value == "," and depth == 1:
            arguments.append([])
            continue
        arguments[-1].append(token)
    return arguments if arguments[0] else []


def _if_conditions(tokens: list[Token], start: int, end: int) -> Iterable[tuple[int, int]]:
    """The indexes of the parentheses around each if's condition in a region."""
    for position, token in enumerate(tokens):
        if token.value != "if" or not start <= token.offset < end:
            continue
        if position + 1 >= len(tokens) or tokens[position + 1].value != "(":
            continue
        depth = 0
        for cursor in range(position + 1, len(tokens)):
            depth += tokens[cursor].value == "("
            depth -= tokens[cursor].value == ")"
            if depth == 0:
                yield position + 1, cursor
                break


def _matching_index(tokens: list[Token], index: int) -> int | None:
    """The index of the bracket closing the one at `index`, or None if there's no opening bracket there."""
    if index >= len(tokens) or tokens[index].kind == "string" or tokens[index].value not in ("(", "[", "{"):
        return None
    depth = 0
    for position in range(index, len(tokens)):
        token = tokens[position]
        if token.kind == "string":
            continue
        if token.value in ("(", "[", "{"):
            depth += 1
        elif token.value in (")", "]", "}"):
            depth -= 1
            if depth == 0:
                return position
    return None


def _dedupe_problems(problems: list[Problem]) -> list[Problem]:
    seen = set()
    unique = []
    for problem in problems:
        key = (problem.start, problem.message)
        if key not in seen:
            seen.add(key)
            unique.append(problem)
    return unique


def _unused(imported: Import, name: str) -> Problem:
    return Problem(
        imported.start,
        imported.end,
        "warning",
        f"Nothing from {name} is used.",
        "unused-import",
        unnecessary=True,
    )


def _is_reference(module: Module, token: Token) -> bool:
    """True for identifiers which refer to a declaration (not keys, namespaces, or declarations)."""
    index = module.index
    if token.kind != "identifier" or token.offset in index.ignored_offsets:
        return False
    declaration = index.declaration_by_offset.get(token.offset)
    if declaration is not None and declaration.token.offset == token.offset:
        return False
    previous = index.previous_token(token)
    if previous is not None and previous.value == "::":
        return False
    following = index.next_token(token)
    if following is not None and following.value == "::":
        return False
    return not any(
        imported.start <= token.offset < imported.end for imported in module.imports
    )


def _dedupe(results: list[tuple[Module, Token]]) -> list[tuple[Module, Token]]:
    seen = set()
    unique = []
    for module, token in results:
        key = (module.path, token.offset)
        if key not in seen:
            seen.add(key)
            unique.append((module, token))
    return unique
