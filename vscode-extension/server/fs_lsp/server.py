"""The FeatureScript language server.

Run with `uv run fs-lsp` (stdio). The VS Code extension in vscode-extension/ starts it
automatically.
"""

from __future__ import annotations

import asyncio
import logging
import pathlib

from lsprotocol import types as lsp
from pygls.lsp.server import LanguageServer
from pygls.uris import from_fs_path, to_fs_path
from pygls.workspace import TextDocument

from fs_cli.renames import path_import_edits, relative_paths, rename_studio_files
from fs_lsp import __version__
from fs_lsp.completion import completion_data, completion_items
from fs_lsp.diagnostics import diagnostics
from fs_lsp.hover import declaration_markdown, hover_markdown
from fs_lsp.navigation import document_symbols, folding_ranges, token_range
from fs_lsp.parser import ParsedProgram, parse
from fs_lsp.project import Module, Problem, Project
from fs_lsp.scanner import LineMap, Token
from fs_lsp.semantic import TOKEN_MODIFIERS, TOKEN_TYPES, build_semantic_tokens, encode
from fs_lsp.symbol_index import SymbolIndex

DIAGNOSTICS_DELAY = 0.3


class Analysis:
    """Parsed state of one version of a document, computed lazily."""

    def __init__(self, version: int | None, parsed: ParsedProgram) -> None:
        self.version = version
        self.parsed = parsed
        self._index: SymbolIndex | None = None
        self._semantic_tokens: list[int] | None = None

    @property
    def index(self) -> SymbolIndex:
        if self._index is None:
            self._index = SymbolIndex(self.parsed)
        return self._index

    @property
    def semantic_tokens(self) -> list[int]:
        if self._semantic_tokens is None:
            self._semantic_tokens = encode(build_semantic_tokens(self.parsed))
        return self._semantic_tokens


class FeatureScriptServer(LanguageServer):
    def __init__(self) -> None:
        super().__init__("featurescript-language-server", __version__)
        self.analyses: dict[str, Analysis] = {}
        self.pending_diagnostics: dict[str, asyncio.TimerHandle] = {}
        self.projects: list[Project] = []
        self.open_uris: set[str] = set()

    def analysis(self, document: TextDocument) -> Analysis:
        cached = self.analyses.get(document.uri)
        if cached is not None and cached.version == document.version:
            return cached
        analysis = Analysis(document.version, parse(document.source))
        self.analyses[document.uri] = analysis
        return analysis

    def document(self, uri: str) -> TextDocument:
        return self.workspace.get_text_document(uri)

    def project_module(self, uri: str) -> tuple[Project, Module] | None:
        """The project containing a document (with its unsaved contents), if any."""
        path = _path(uri)
        if path is None:
            return None
        project = next((p for p in self.projects if p.contains(path)), None)
        if project is None:
            project = Project.find(path)
            if project is None:
                return None
            self.projects.append(project)
        if uri in self.open_uris:
            project.overlays[path.resolve()] = self.document(uri).source
        module = project.module(path)
        return (project, module) if module else None

    def close(self, uri: str) -> None:
        self.open_uris.discard(uri)
        path = _path(uri)
        if path is not None:
            for project in self.projects:
                project.overlays.pop(path.resolve(), None)

    def schedule_diagnostics(self, uri: str) -> None:
        """Publishes diagnostics once the user pauses typing."""
        self.cancel_diagnostics(uri)
        self.pending_diagnostics[uri] = asyncio.get_running_loop().call_later(
            DIAGNOSTICS_DELAY, self.publish_diagnostics, uri
        )

    def cancel_diagnostics(self, uri: str) -> None:
        pending = self.pending_diagnostics.pop(uri, None)
        if pending:
            pending.cancel()

    def publish_diagnostics(self, uri: str) -> None:
        self.pending_diagnostics.pop(uri, None)
        found = self.project_module(uri)
        self._publish(uri, found)
        if found:
            # Other files' diagnostics depend on what this one exports
            for other in sorted(self.open_uris - {uri}):
                other_found = self.project_module(other)
                if other_found and other_found[0] is found[0]:
                    self._publish(other, other_found)

    def _publish(self, uri: str, found: tuple[Project, Module] | None) -> None:
        document = self.document(uri)
        if found:
            project, module = found
            results = [_diagnostic(module, problem) for problem in project.check(module)]
        else:
            results = diagnostics(self.analysis(document).parsed)
        self.text_document_publish_diagnostics(
            lsp.PublishDiagnosticsParams(
                uri=uri, version=document.version, diagnostics=results
            )
        )


    # Renames

    def renames(self, files: list[lsp.FileRename]) -> list[tuple[Project, dict[str, str]]]:
        """Groups files and folders renamed in code folders by project, as renames from the old path
        to the new one (relative to the code folder)."""
        groups: dict[int, tuple[Project, dict[str, str]]] = {}
        for file in files:
            old, new = _path(file.old_uri), _path(file.new_uri)
            if old is None or new is None:
                continue
            project = next((p for p in self.projects if p.contains(old)), None) or Project.find(old)
            if project is None:
                continue
            if project not in self.projects:
                self.projects.append(project)
            old_path, new_path = relative_paths(project.code_dir, [old, new])
            if old_path is None or new_path is None:
                # Moved out of the code folder, which is like deleting it
                continue
            groups.setdefault(id(project), (project, {}))[1][old_path] = new_path
        return list(groups.values())

    def rename_edits(self, files: list[lsp.FileRename]) -> dict[str, list[lsp.TextEdit]]:
        """Edits to the imports by path of files being renamed, in every file importing them."""
        changes: dict[str, list[lsp.TextEdit]] = {}
        for project, renames in self.renames(files):
            for path in project.files():
                source = project.overlays.get(path)
                if source is None:
                    source = path.read_text(encoding="utf-8", errors="replace")
                edits = path_import_edits(source, renames)
                if not edits:
                    continue
                lines = LineMap(source)
                changes[from_fs_path(str(path)) or path.as_uri()] = [
                    lsp.TextEdit(
                        range=lsp.Range(
                            start=lsp.Position(*lines.position(start)), end=lsp.Position(*lines.position(end))
                        ),
                        new_text=new,
                    )
                    for start, end, new in edits
                ]
        return changes

    def record_renames(self, files: list[lsp.FileRename]) -> None:
        """Keeps renamed files synced with their Feature Studios, in fs-studios.json."""
        for project, renames in self.renames(files):
            rename_studio_files(project.studios_path, renames)


server = FeatureScriptServer()

# Files and folders, so renaming a folder renames the files in it
RENAMED_FILES = lsp.FileOperationRegistrationOptions(
    filters=[
        lsp.FileOperationFilter(
            scheme="file",
            pattern=lsp.FileOperationPattern(glob="**/*.fs", matches=lsp.FileOperationPatternKind.File),
        ),
        lsp.FileOperationFilter(
            scheme="file",
            pattern=lsp.FileOperationPattern(glob="**", matches=lsp.FileOperationPatternKind.Folder),
        ),
    ]
)


@server.feature(lsp.WORKSPACE_WILL_RENAME_FILES, RENAMED_FILES)
def will_rename_files(ls: FeatureScriptServer, params: lsp.RenameFilesParams) -> lsp.WorkspaceEdit | None:
    changes = ls.rename_edits(params.files)
    return lsp.WorkspaceEdit(changes=changes) if changes else None


@server.feature(lsp.WORKSPACE_DID_RENAME_FILES, RENAMED_FILES)
def did_rename_files(ls: FeatureScriptServer, params: lsp.RenameFilesParams) -> None:
    ls.record_renames(params.files)


@server.feature(lsp.TEXT_DOCUMENT_DID_OPEN)
def did_open(ls: FeatureScriptServer, params: lsp.DidOpenTextDocumentParams) -> None:
    ls.open_uris.add(params.text_document.uri)
    ls.publish_diagnostics(params.text_document.uri)


@server.feature(lsp.TEXT_DOCUMENT_DID_CHANGE)
def did_change(
    ls: FeatureScriptServer, params: lsp.DidChangeTextDocumentParams
) -> None:
    ls.schedule_diagnostics(params.text_document.uri)


@server.feature(lsp.TEXT_DOCUMENT_DID_CLOSE)
def did_close(ls: FeatureScriptServer, params: lsp.DidCloseTextDocumentParams) -> None:
    uri = params.text_document.uri
    ls.analyses.pop(uri, None)
    ls.cancel_diagnostics(uri)
    ls.close(uri)
    ls.text_document_publish_diagnostics(
        lsp.PublishDiagnosticsParams(uri=uri, diagnostics=[])
    )


@server.feature(
    lsp.TEXT_DOCUMENT_SEMANTIC_TOKENS_FULL,
    lsp.SemanticTokensLegend(token_types=TOKEN_TYPES, token_modifiers=TOKEN_MODIFIERS),
)
def semantic_tokens(
    ls: FeatureScriptServer, params: lsp.SemanticTokensParams
) -> lsp.SemanticTokens:
    uri = params.text_document.uri
    analysis = ls.analysis(ls.document(uri))
    found = ls.project_module(uri)
    if found:
        # Depends on the files this one imports, so it isn't cached
        project, module = found
        tokens = build_semantic_tokens(analysis.parsed, project.imported_names(module))
        return lsp.SemanticTokens(data=encode(tokens))
    return lsp.SemanticTokens(data=analysis.semantic_tokens)


@server.feature(lsp.TEXT_DOCUMENT_DOCUMENT_SYMBOL)
def document_symbol(
    ls: FeatureScriptServer, params: lsp.DocumentSymbolParams
) -> list[lsp.DocumentSymbol]:
    return document_symbols(ls.analysis(ls.document(params.text_document.uri)).parsed)


@server.feature(lsp.TEXT_DOCUMENT_FOLDING_RANGE)
def folding_range(
    ls: FeatureScriptServer, params: lsp.FoldingRangeParams
) -> list[lsp.FoldingRange]:
    return folding_ranges(ls.analysis(ls.document(params.text_document.uri)).parsed)


def _definition_links(
    ls: FeatureScriptServer, params: lsp.TextDocumentPositionParams
) -> list[lsp.LocationLink] | None:
    document = ls.document(params.text_document.uri)
    index = ls.analysis(document).index
    offset = document.offset_at_position(params.position)
    origin = index.token_at(offset)
    declaration = index.definition_at(offset)
    if declaration is None:
        return _project_definition_links(ls, document.uri, offset)
    target = token_range(declaration.token)
    return [
        lsp.LocationLink(
            target_uri=document.uri,
            target_range=target,
            target_selection_range=target,
            origin_selection_range=token_range(origin or declaration.token),
        )
    ]


def _project_definition_links(
    ls: FeatureScriptServer, uri: str, offset: int
) -> list[lsp.LocationLink] | None:
    """Definitions in other files: imported declarations, and the files imports refer to."""
    found = ls.project_module(uri)
    if not found:
        return None
    project, module = found
    imported = module.import_at(offset)
    if imported:
        target_module = project.resolve(imported)
        if target_module is None:
            return None
        start = lsp.Range(lsp.Position(0, 0), lsp.Position(0, 0))
        return [
            lsp.LocationLink(
                target_uri=from_fs_path(str(target_module.path)) or "",
                target_range=start,
                target_selection_range=start,
                origin_selection_range=token_range(imported.token),
            )
        ]
    origin = module.index.token_at(offset)
    links = [
        lsp.LocationLink(
            target_uri=from_fs_path(str(owner.path)) or "",
            target_range=token_range(declaration.token),
            target_selection_range=token_range(declaration.token),
            origin_selection_range=token_range(origin) if origin else None,
        )
        for owner, declaration in project.definitions(module, offset)
    ]
    return links or None


@server.feature(lsp.TEXT_DOCUMENT_DEFINITION)
def definition(
    ls: FeatureScriptServer, params: lsp.DefinitionParams
) -> list[lsp.LocationLink] | None:
    return _definition_links(ls, params)


@server.feature(lsp.TEXT_DOCUMENT_DECLARATION)
def declaration(
    ls: FeatureScriptServer, params: lsp.DeclarationParams
) -> list[lsp.LocationLink] | None:
    return _definition_links(ls, params)


@server.feature(lsp.TEXT_DOCUMENT_REFERENCES)
def references(
    ls: FeatureScriptServer, params: lsp.ReferenceParams
) -> list[lsp.Location]:
    document = ls.document(params.text_document.uri)
    offset = document.offset_at_position(params.position)
    found = ls.project_module(document.uri)
    if found:
        project, module = found
        return [
            lsp.Location(uri=from_fs_path(str(owner.path)) or "", range=token_range(token))
            for owner, token in project.references(
                module, offset, params.context.include_declaration
            )
        ]
    index = ls.analysis(document).index
    return [
        lsp.Location(uri=document.uri, range=token_range(reference.token))
        for reference in index.references_at(offset, params.context.include_declaration)
    ]


@server.feature(lsp.TEXT_DOCUMENT_DOCUMENT_HIGHLIGHT)
def document_highlight(
    ls: FeatureScriptServer, params: lsp.DocumentHighlightParams
) -> list[lsp.DocumentHighlight]:
    document = ls.document(params.text_document.uri)
    index = ls.analysis(document).index
    offset = document.offset_at_position(params.position)
    return [
        lsp.DocumentHighlight(range=token_range(reference.token))
        for reference in index.references_at(offset, True)
    ]


@server.feature(lsp.TEXT_DOCUMENT_HOVER)
def hover(ls: FeatureScriptServer, params: lsp.HoverParams) -> lsp.Hover | None:
    document = ls.document(params.text_document.uri)
    analysis = ls.analysis(document)
    offset = document.offset_at_position(params.position)
    token = analysis.index.token_at(offset)
    result = None
    if token is not None and analysis.index.declaration_for_token(token) is None:
        result = _project_hover(ls, document.uri, offset)
    if result is None:
        result = hover_markdown(analysis.parsed, analysis.index, offset)
    if result is None:
        return None
    markdown, token = result
    return lsp.Hover(
        contents=lsp.MarkupContent(kind=lsp.MarkupKind.Markdown, value=markdown),
        range=token_range(token),
    )


@server.feature(
    lsp.TEXT_DOCUMENT_COMPLETION,
    lsp.CompletionOptions(trigger_characters=[".", '"', "'", "{", ","]),
)
def completion(
    ls: FeatureScriptServer, params: lsp.CompletionParams
) -> list[lsp.CompletionItem] | None:
    document = ls.document(params.text_document.uri)
    analysis = ls.analysis(document)
    data = completion_data(
        analysis.parsed, document.offset_at_position(params.position)
    )
    if data is None:
        return None
    line_map = analysis.parsed.line_map
    replace_range = lsp.Range(
        lsp.Position(*line_map.position(data.replacement_start)),
        lsp.Position(*line_map.position(data.replacement_end)),
    )
    return completion_items(data, replace_range)


def _project_hover(
    ls: FeatureScriptServer, uri: str, offset: int
) -> tuple[str, Token] | None:
    found = ls.project_module(uri)
    if not found:
        return None
    project, module = found
    imported = module.import_at(offset)
    if imported:
        target = project.resolve(imported)
        if target is None:
            return None
        exported, _ = project.exported_names(target)
        names = ", ".join(f"`{name}`" for name in sorted(exported)[:30])
        more = f" and {len(exported) - 30} more" if len(exported) > 30 else ""
        markdown = f"**{target.relative}**"
        if names:
            markdown += f"\n\nExports {names}{more}"
        return markdown, imported.token
    token = module.index.token_at(offset)
    definitions = project.definitions(module, offset)
    if token is None or not definitions:
        return None
    sections = [
        declaration_markdown(owner.parsed, declaration)
        + f"\n\nFrom `{owner.relative}`"
        for owner, declaration in definitions[:5]
    ]
    return "\n\n---\n\n".join(sections), token


@server.feature(lsp.WORKSPACE_SYMBOL)
def workspace_symbol(
    ls: FeatureScriptServer, params: lsp.WorkspaceSymbolParams
) -> list[lsp.SymbolInformation]:
    query = params.query.lower()
    results = []
    for project in ls.projects or _workspace_projects(ls):
        for module in project.modules():
            uri = from_fs_path(str(module.path)) or ""
            for name, declarations in module.top_level.items():
                if query not in name.lower():
                    continue
                for declaration in declarations:
                    results.append(
                        lsp.SymbolInformation(
                            name=name,
                            kind=_SYMBOL_KINDS.get(declaration.kind, lsp.SymbolKind.Variable),
                            location=lsp.Location(uri=uri, range=token_range(declaration.token)),
                            container_name=module.relative,
                        )
                    )
    return results


_SYMBOL_KINDS = {
    "feature": lsp.SymbolKind.Class,
    "function": lsp.SymbolKind.Function,
    "predicate": lsp.SymbolKind.Function,
    "operator": lsp.SymbolKind.Operator,
    "enum": lsp.SymbolKind.Enum,
    "type": lsp.SymbolKind.Struct,
    "variable": lsp.SymbolKind.Constant,
}


def _workspace_projects(ls: FeatureScriptServer) -> list[Project]:
    for folder in ls.workspace.folders.values():
        path = _path(folder.uri)
        if path is not None and (project := Project.find(path / "featurescripts")):
            ls.projects.append(project)
    return ls.projects


def _path(uri: str) -> pathlib.Path | None:
    path = to_fs_path(uri)
    return pathlib.Path(path) if path else None


def _diagnostic(module: Module, problem: Problem) -> lsp.Diagnostic:
    return lsp.Diagnostic(
        range=lsp.Range(
            lsp.Position(*module.position(problem.start)),
            lsp.Position(*module.position(problem.end)),
        ),
        message=problem.message,
        severity=(
            lsp.DiagnosticSeverity.Error
            if problem.severity == "error"
            else lsp.DiagnosticSeverity.Warning
        ),
        code=problem.code,
        source="featurescript",
        tags=[lsp.DiagnosticTag.Unnecessary] if problem.unnecessary else None,
    )


def main() -> None:
    logging.basicConfig(level=logging.WARNING)
    server.start_io()


if __name__ == "__main__":
    main()
