"""Completions which need the project (what a file's imports declare), or know FeatureScript's annotations:

- Annotation keys (`annotation { "Na|`), and UIHint names in a "UIHint" value (`"UIHint" : ["SHOW_|`).
- Members of enums declared in the project (`LightenEndType.|`): `completion.py` knows the file's own and std's.
- The parameters a feature's precondition declares, after `definition.`.
- Names: what's in scope where the cursor is, what the file's imports declare, and what std declares (its visible
  modules'), with keywords. Std's documentation is filled in when an item is chosen (`resolve_item`).
"""

from __future__ import annotations

import re
from typing import TYPE_CHECKING

from lsprotocol import types as lsp

from fs_lsp.fsdoc import parse_doc, render_markdown
from fs_lsp.scanner import KEYWORDS
from fs_lsp.stdlib import stdlib

if TYPE_CHECKING:
    from fs_lsp.project import Module, Project

STD_PREFIX = "onshape/std/"

_KINDS = {
    "function": lsp.CompletionItemKind.Function,
    "predicate": lsp.CompletionItemKind.Function,
    "feature": lsp.CompletionItemKind.Function,
    "operator": lsp.CompletionItemKind.Operator,
    "enum": lsp.CompletionItemKind.Enum,
    "enumMember": lsp.CompletionItemKind.EnumMember,
    "type": lsp.CompletionItemKind.Class,
    "constant": lsp.CompletionItemKind.Constant,
    "unit": lsp.CompletionItemKind.Unit,
    "variable": lsp.CompletionItemKind.Variable,
    "parameter": lsp.CompletionItemKind.Variable,
    "property": lsp.CompletionItemKind.Field,
}

_IDENTIFIER_BEFORE = re.compile(r"[A-Za-z_][A-Za-z0-9_]*$")


def project_completions(project: Project, module: Module, source: str, offset: int) -> list[lsp.CompletionItem] | None:
    """Completions at offset in source (module's unsaved text), or None if none of these apply there."""
    if _in_comment_or_string(source, offset):
        return None
    prefix = _IDENTIFIER_BEFORE.search(source[:offset])
    start = prefix.start() if prefix else offset
    before = source[:start].rstrip()
    if before.endswith("."):
        target = _IDENTIFIER_BEFORE.search(before[:-1].rstrip())
        return _member_completions(project, module, target.group(0) if target else None, start, offset)
    return _name_completions(project, module, start, offset)


# Annotations


def annotation_completions(source: str, offset: int) -> list[lsp.CompletionItem] | None:
    """Keys at a key's position in an annotation's map, or UIHint names in its "UIHint" value."""
    stack = _open_brackets(source, offset)
    # The annotation's map, or a list in it
    if stack and stack[-1][0] == "[":
        stack = stack[:-1]
    if not stack or stack[-1][0] != "{" or not re.search(r"\bannotation\s*$", source[: stack[-1][1]]):
        return None
    open_brace = stack[-1][1]
    inside = source[open_brace + 1 : offset]
    entry = _last_top_level_entry(inside)
    key = re.fullmatch(r'\s*("?)([A-Za-z ]*)', entry)
    if key is not None:
        from fs_lsp.project import ANNOTATION_KEYS

        start = offset - len(key.group(2)) - len(key.group(1))
        end = offset + (1 if source[offset : offset + 1] == '"' else 0)
        return [
            lsp.CompletionItem(
                label=name,
                kind=lsp.CompletionItemKind.Property,
                detail="annotation key",
                filter_text=f'"{name}',
                text_edit=lsp.TextEdit(_range(source, start, end), f'"{name}" : '),
            )
            for name in sorted(ANNOTATION_KEYS)
        ]
    hint = re.fullmatch(r'\s*"UIHint"\s*:\s*(?:\[[^\]]*?(?:,\s*)?)?(?:UIHint\.)?("?)([A-Z_]*)', entry)
    if hint is not None:
        start = offset - len(hint.group(2))
        quoted = bool(hint.group(1))
        return [
            lsp.CompletionItem(
                label=member.name,
                kind=lsp.CompletionItemKind.EnumMember,
                detail="UIHint",
                text_edit=lsp.TextEdit(_range(source, start, offset), member.name + ('"' if quoted and source[offset : offset + 1] != '"' else "")),
            )
            for member in stdlib().enums.get("UIHint", [])
        ]
    return None


def _open_brackets(source: str, offset: int) -> list[tuple[str, int]]:
    """The brackets open at offset, innermost last, with their offsets (strings and comments skipped)."""
    stack: list[tuple[str, int]] = []
    index = 0
    while index < offset:
        char = source[index]
        if char in "\"'":
            end = source.find(char, index + 1)
            while end != -1 and source[end - 1] == "\\":
                end = source.find(char, end + 1)
            if end == -1 or end >= offset:
                break
            index = end + 1
            continue
        if source.startswith("//", index):
            end = source.find("\n", index)
            index = offset if end == -1 else end
            continue
        if source.startswith("/*", index):
            end = source.find("*/", index + 2)
            index = offset if end == -1 else end + 2
            continue
        if char in "([{":
            stack.append((char, index))
        elif char in ")]}" and stack:
            stack.pop()
        index += 1
    return stack


def _last_top_level_entry(text: str) -> str:
    """What follows a map's last top-level comma (or its start)."""
    depth = 0
    start = 0
    quote = None
    for index, char in enumerate(text):
        if quote:
            if char == quote:
                quote = None
        elif char in "\"'":
            quote = char
        elif char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        elif char == "," and depth == 0:
            start = index + 1
    return text[start:]


def _in_comment_or_string(source: str, offset: int) -> bool:
    line_start = source.rfind("\n", 0, offset) + 1
    line = source[line_start:offset]
    if "//" in re.sub(r'"[^"]*"', "", line):
        return True
    return re.sub(r'"[^"\n]*"', "", line).count('"') % 2 == 1


# Members


def _member_completions(project: Project, module: Module, target: str | None, start: int, offset: int) -> list[lsp.CompletionItem] | None:
    if target is None:
        return None
    source = module.parsed.source
    if target == "definition":
        names = _definition_parameters(project, module)
        return [
            lsp.CompletionItem(
                label=name,
                kind=lsp.CompletionItemKind.Field,
                detail="feature parameter",
                sort_text=f"{index:04d}",
                text_edit=lsp.TextEdit(_range(source, start, offset), name),
            )
            for index, name in enumerate(names)
        ] or None
    providers, _ = project.providers(module)
    for provider in providers.get(target, []):
        if provider.declaration.kind == "enum":
            members = provider.module.parsed.enum_members.get(target, [])
            return [
                lsp.CompletionItem(
                    label=member,
                    kind=lsp.CompletionItemKind.EnumMember,
                    detail=f"{target} ({provider.module.relative})",
                    text_edit=lsp.TextEdit(_range(source, start, offset), member),
                )
                for member in members
            ] or None
    return None


def _definition_parameters(project: Project, module: Module) -> list[str]:
    """The parameters the file's features' preconditions declare (directly or through predicates), in order."""
    names: list[str] = []
    for node in module.parsed.nodes:
        if node.type != "PreconditionBlock":
            continue
        for name, _, _ in project._declared_parameters(module, node.start, node.end, "definition", frozenset()):
            if name not in names:
                names.append(name)
    return names


# Names


def _name_completions(project: Project, module: Module, start: int, offset: int) -> list[lsp.CompletionItem]:
    source = module.parsed.source
    replace = _range(source, start, offset)
    items: dict[str, lsp.CompletionItem] = {}

    def add(name: str, kind: str, detail: str, rank: int, data: dict | None = None) -> None:
        if name in items:
            return
        items[name] = lsp.CompletionItem(
            label=name,
            kind=_KINDS.get(kind, lsp.CompletionItemKind.Text),
            detail=detail,
            sort_text=f"{rank}_{name}",
            text_edit=lsp.TextEdit(replace, name),
            data=data,
        )

    # In scope here (locals, parameters, and the file's declarations)
    for declaration in module.index.declarations:
        if declaration.kind == "property" or declaration.name == "_":
            continue
        if declaration.scope_start <= offset <= declaration.scope_end and declaration.visible_from <= offset:
            add(declaration.name, declaration.kind, declaration.kind, 0)
    providers, sees_std = project.providers(module)
    for name, found in providers.items():
        provider = found[0]
        add(name, provider.declaration.kind, f"{provider.declaration.kind} ({provider.module.relative})", 1)
    if sees_std:
        library = stdlib()
        visible: set[str] = set()
        for imported in module.imports:
            if imported.is_std:
                visible |= library.visible_modules(imported.path.removeprefix(STD_PREFIX))
        for symbol in library.symbols:
            if symbol.parent is not None or symbol.kind == "enumMember" or (symbol.module and symbol.module not in visible):
                continue
            if symbol.doc and symbol.doc.startswith("@internal"):
                continue
            add(symbol.name, symbol.kind, f"{symbol.kind} ({symbol.module})", 2, {"std": symbol.name, "module": symbol.module})
    for keyword in sorted(KEYWORDS):
        add(keyword, "keyword", "keyword", 3)
        items[keyword].kind = lsp.CompletionItemKind.Keyword
    return list(items.values())


def resolve_item(item: lsp.CompletionItem) -> lsp.CompletionItem:
    """Fills in a std name's signature and documentation."""
    data = item.data if isinstance(item.data, dict) else None
    if data and data.get("std"):
        symbol = next((s for s in stdlib().lookup(data["std"]) if s.module == data.get("module")), None)
        if symbol is not None:
            if symbol.signature:
                item.detail = symbol.signature
            if symbol.doc:
                item.documentation = lsp.MarkupContent(kind=lsp.MarkupKind.Markdown, value=render_markdown(parse_doc(symbol.doc), symbol.signature))
    return item


def _range(source: str, start: int, end: int) -> lsp.Range:
    return lsp.Range(_position(source, start), _position(source, end))


def _position(source: str, offset: int) -> lsp.Position:
    line = source.count("\n", 0, offset)
    line_start = source.rfind("\n", 0, offset) + 1
    return lsp.Position(line, len(source[line_start:offset].encode("utf-16-le")) // 2)
