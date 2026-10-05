"""Cross-file analysis of the FeatureScripts in a repo.

Feature Studios import each other by element id (`import(path : "<element id>", ...)`). The fs
CLI records which file each element id is synced with in .fs-state.json, which lets imports be
resolved to files: for Go to Definition and Find References across files, and for diagnostics
about names which aren't defined anywhere and imports which aren't used.

Files are re-read when they change on disk, and open editors can overlay their unsaved contents.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
from typing import Iterable, Literal

from fs_lsp.diagnostics import diagnostics as syntax_diagnostics
from fs_lsp.parser import ParsedProgram, parse
from fs_lsp.scanner import Token
from fs_lsp.stdlib import built_in_type, stdlib
from fs_lsp.symbol_index import Declaration, SymbolIndex

ELEMENT_ID = re.compile(r"[0-9a-f]{24}")
STD_PREFIX = "onshape/std/"
STATE_FILE = ".fs-state.json"

TOP_LEVEL_KINDS = frozenset(
    ["feature", "function", "predicate", "operator", "enum", "type", "variable"]
)
DECLARATION_KEYWORDS = frozenset(
    ["const", "function", "predicate", "operator", "enum", "type"]
)
# Identifiers FeatureScript treats specially which aren't declared anywhere
IMPLICIT_NAMES = frozenset(["silent"])

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
        self, root: pathlib.Path, code_dir: pathlib.Path, state_path: pathlib.Path
    ) -> None:
        self.root = root
        self.code_dir = code_dir
        self.state_path = state_path
        self.overlays: dict[pathlib.Path, str] = {}
        self._modules: dict[pathlib.Path, tuple[object, Module]] = {}
        self._element_ids: tuple[object, dict[str, pathlib.Path]] | None = None

    @classmethod
    def find(cls, path: pathlib.Path) -> Project | None:
        """Returns the project containing path, if it's inside a code folder."""
        from fs_cli.config import ConfigError, load_config

        try:
            config = load_config(path if path.is_dir() else path.parent)
        except ConfigError:
            return None
        project = cls(config.root, config.code_dir, config.state_path)
        return project if project.contains(path) else None

    def contains(self, path: pathlib.Path) -> bool:
        code_dir = self.code_dir.resolve()
        path = path.resolve()
        return path == code_dir or code_dir in path.parents

    # Files

    def files(self) -> list[pathlib.Path]:
        if not self.code_dir.is_dir():
            return []
        paths = {path.resolve() for path in self.code_dir.rglob("*.fs") if path.is_file()}
        paths.update(path for path in self.overlays if self.contains(path))
        return sorted(paths)

    def modules(self) -> list[Module]:
        return [module for path in self.files() if (module := self.module(path))]

    def module(self, path: pathlib.Path) -> Module | None:
        path = path.resolve()
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
        relative = path.relative_to(self.code_dir.resolve()).as_posix()
        module = Module(path, relative, source)
        self._modules[path] = (key, module)
        return module

    def element_ids(self) -> dict[str, pathlib.Path]:
        """Maps element ids to the files synced with them, from the fs CLI's state file."""
        try:
            stat = self.state_path.stat()
            key: object = (stat.st_mtime_ns, stat.st_size)
        except OSError:
            return {}
        if self._element_ids and self._element_ids[0] == key:
            return self._element_ids[1]
        try:
            studios = json.loads(self.state_path.read_text())["studios"]
            ids = {
                element_id: (self.code_dir / studio["file"]).resolve()
                for element_id, studio in studios.items()
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

    def providers(self, module: Module) -> tuple[dict[str, list[Provider]], bool]:
        """The names a module can use from its imports, and whether it can use the std library."""
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
            if imported.namespace:
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
        return sorted(problems, key=lambda problem: problem.start)


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
