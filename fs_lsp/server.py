"""The FeatureScript language server.

Run with `uv run fs-lsp` (stdio). The VS Code extension in vscode-extension/ starts it
automatically.
"""

from __future__ import annotations

import asyncio
import logging

from lsprotocol import types as lsp
from pygls.lsp.server import LanguageServer
from pygls.workspace import TextDocument

from fs_lsp import __version__
from fs_lsp.completion import completion_data, completion_items
from fs_lsp.diagnostics import diagnostics
from fs_lsp.hover import hover_markdown
from fs_lsp.navigation import document_symbols, folding_ranges, token_range
from fs_lsp.parser import ParsedProgram, parse
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

    def analysis(self, document: TextDocument) -> Analysis:
        cached = self.analyses.get(document.uri)
        if cached is not None and cached.version == document.version:
            return cached
        analysis = Analysis(document.version, parse(document.source))
        self.analyses[document.uri] = analysis
        return analysis

    def document(self, uri: str) -> TextDocument:
        return self.workspace.get_text_document(uri)

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
        document = self.document(uri)
        self.text_document_publish_diagnostics(
            lsp.PublishDiagnosticsParams(
                uri=uri,
                version=document.version,
                diagnostics=diagnostics(self.analysis(document).parsed),
            )
        )


server = FeatureScriptServer()


@server.feature(lsp.TEXT_DOCUMENT_DID_OPEN)
def did_open(ls: FeatureScriptServer, params: lsp.DidOpenTextDocumentParams) -> None:
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
    analysis = ls.analysis(ls.document(params.text_document.uri))
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
        return None
    target = token_range(declaration.token)
    return [
        lsp.LocationLink(
            target_uri=document.uri,
            target_range=target,
            target_selection_range=target,
            origin_selection_range=token_range(origin or declaration.token),
        )
    ]


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
    index = ls.analysis(document).index
    offset = document.offset_at_position(params.position)
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
    result = hover_markdown(
        analysis.parsed, analysis.index, document.offset_at_position(params.position)
    )
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


def main() -> None:
    logging.basicConfig(level=logging.WARNING)
    server.start_io()


if __name__ == "__main__":
    main()
