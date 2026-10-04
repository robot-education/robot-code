"""Completions for enum members (`BoundingType.|`) and feature definition-map keys
(`extrude(context, id, { | })`)."""

from __future__ import annotations

import dataclasses
import re

from lsprotocol import types as lsp

from fs_lsp.parser import AstNode, ParsedProgram
from fs_lsp.scanner import Token, is_identifier_token
from fs_lsp.stdlib import (
    EnumMember,
    FeatureField,
    dedupe_fields,
    stdlib,
    top_level_fields,
)

VALIDATOR_TYPES = {
    "isLength": "ValueWithUnits",
    "isAngle": "ValueWithUnits",
    "isInteger": "number",
    "isReal": "number",
    "isRealInRange": "number",
    "isNonNegativeInteger": "number",
    "isPositiveInteger": "number",
}

NON_ENUM_TYPES = frozenset(
    [
        "Context",
        "Id",
        "Query",
        "Vector",
        "Transform",
        "Line",
        "Plane",
        "ValueWithUnits",
        "LengthBoundSpec",
        "PartStudioData",
        "boolean",
        "number",
        "string",
        "array",
        "map",
        "box",
        "function",
    ]
)

_ENUM_TYPE_NAME = re.compile(r"^[A-Z][A-Za-z0-9_]*$")


@dataclasses.dataclass
class EnumCompletion:
    enum_name: str
    members: list[EnumMember]
    replacement_start: int
    replacement_end: int


@dataclasses.dataclass
class FeatureMapCompletion:
    feature_name: str
    fields: list[FeatureField]
    replacement_start: int
    replacement_end: int


def completion_data(
    parsed: ParsedProgram, offset: int
) -> EnumCompletion | FeatureMapCompletion | None:
    state = _lexical_state(parsed.source, offset)
    if state == "comment":
        return None
    tokens = [token for token in parsed.tokens if token.kind != "eof"]
    map_context = _feature_map_context(parsed, tokens, offset)
    if map_context:
        return map_context
    if state == "string":
        return None
    return _enum_context(parsed, tokens, offset)


def enum_members_for(parsed: ParsedProgram, enum_name: str) -> list[EnumMember]:
    members = [EnumMember(name) for name in parsed.enum_members.get(enum_name, [])]
    if parsed.imports_stdlib:
        members.extend(stdlib().enums.get(enum_name, []))
    unique = {}
    for member in members:
        unique.setdefault(member.name, member)
    return sorted(unique.values(), key=lambda member: member.name)


def feature_fields_for(parsed: ParsedProgram, feature_name: str) -> list[FeatureField]:
    fields = _local_feature_fields(parsed, feature_name)
    if parsed.imports_stdlib:
        feature = stdlib().features.get(feature_name)
        if feature:
            fields.extend(top_level_fields(feature))
    return dedupe_fields(fields)


# LSP items


def completion_items(
    data: EnumCompletion | FeatureMapCompletion, replace_range: lsp.Range
) -> list[lsp.CompletionItem]:
    if isinstance(data, EnumCompletion):
        return [
            lsp.CompletionItem(
                label=member.name,
                kind=lsp.CompletionItemKind.EnumMember,
                detail=f"{data.enum_name} enum member",
                sort_text=f"0_{member.name}",
                text_edit=lsp.TextEdit(replace_range, member.name),
                documentation=_markdown(
                    f"```featurescript\n{data.enum_name}.{member.name}\n```",
                    member.doc,
                    f"Module: {member.module}" if member.module else None,
                ),
            )
            for member in data.members
        ]
    return [
        lsp.CompletionItem(
            label=field.name,
            kind=lsp.CompletionItemKind.Field,
            detail=(
                f"{data.feature_name} field: {field.type}"
                if field.type
                else f"{data.feature_name} field"
            ),
            sort_text=f"0_{field.name}",
            insert_text_format=lsp.InsertTextFormat.Snippet,
            text_edit=lsp.TextEdit(replace_range, f'"{field.name}" : $0'),
            documentation=_field_markdown(field, data.feature_name),
        )
        for field in data.fields
    ]


def _field_markdown(field: FeatureField, feature_name: str) -> lsp.MarkupContent:
    details = []
    if field.label:
        details.append(f"Label: {field.label}")
    if field.required is not None:
        details.append(f"Required: {'yes' if field.required else 'no'}")
    if field.condition:
        details.append(f"Condition: {field.condition}")
    if field.defaultValue:
        details.append(f"Default: {field.defaultValue}")
    if field.predicate:
        details.append(f"From predicate: {field.predicate}")
    details.append(f"Feature: {feature_name}")
    return _markdown(
        f'```featurescript\n"{field.name}" : {field.type or "value"}\n```',
        "  \n".join(escape_markdown(detail) for detail in details),
        field.description,
    )


def _markdown(*sections: str | None) -> lsp.MarkupContent:
    return lsp.MarkupContent(
        kind=lsp.MarkupKind.Markdown,
        value="\n\n".join(section for section in sections if section),
    )


def escape_markdown(value: str) -> str:
    return re.sub(r"([\\`*_{}\[\]()#+\-.!|])", r"\\\1", value)


# Context detection


def _enum_context(
    parsed: ParsedProgram, tokens: list[Token], offset: int
) -> EnumCompletion | None:
    current_index = _token_index_at_cursor(tokens, offset)
    dot_index = -1
    replacement_start = replacement_end = offset

    if current_index > 0 and is_identifier_token(tokens[current_index]):
        previous = tokens[current_index - 1]
        if previous.value in (".", "?."):
            dot_index = current_index - 1
            replacement_start = tokens[current_index].offset
            replacement_end = tokens[current_index].end

    if dot_index < 0:
        index = _last_token_ending_by(tokens, offset)
        if index >= 0 and tokens[index].value in (".", "?."):
            dot_index = index

    if dot_index < 1:
        return None
    enum_token = tokens[dot_index - 1]
    if not is_identifier_token(enum_token):
        return None
    members = enum_members_for(parsed, enum_token.value)
    if not members:
        return None
    return EnumCompletion(enum_token.value, members, replacement_start, replacement_end)


def _feature_map_context(
    parsed: ParsedProgram, tokens: list[Token], offset: int
) -> FeatureMapCompletion | None:
    open_index = _current_open_brace(tokens, offset)
    if open_index < 0 or not _expecting_map_key(tokens, open_index, offset):
        return None
    call = _call_for_map(tokens, open_index)
    if not call or call[1] != 2:
        return None
    feature_name = call[0]
    fields = feature_fields_for(parsed, feature_name)
    if not fields:
        return None
    existing = _existing_top_level_keys(tokens, open_index)
    available = [field for field in fields if field.name not in existing]
    if not available:
        return None
    start, end = _map_key_replacement(tokens, open_index, offset)
    return FeatureMapCompletion(feature_name, available, start, end)


def _local_feature_fields(
    parsed: ParsedProgram, feature_name: str
) -> list[FeatureField]:
    node = next(
        (
            node
            for node in parsed.nodes
            if node.type == "FeatureDeclaration" and node.name == feature_name
        ),
        None,
    )
    return _fields_from_definition_tokens(parsed.tokens, node) if node else []


def _fields_from_definition_tokens(
    tokens: list[Token], node: AstNode
) -> list[FeatureField]:
    scoped = [
        token
        for token in tokens
        if node.start <= token.offset and token.end <= node.end
    ]

    def at(index: int) -> Token | None:
        return scoped[index] if index < len(scoped) else None

    def value(index: int) -> str | None:
        token = at(index)
        return token.value if token else None

    fields = []
    for index, token in enumerate(scoped):
        if token.value == "definition":
            prop, type_token = at(index + 2), at(index + 4)
            if (
                value(index + 1) in (".", "?.")
                and is_identifier_token(prop)
                and value(index + 3) == "is"
                and is_identifier_token(type_token)
            ):
                assert prop and type_token
                fields.append(_definition_field(prop.value, type_token.value))
        if (
            token.value in VALIDATOR_TYPES
            and value(index + 1) == "("
            and value(index + 2) == "definition"
        ):
            prop = at(index + 4)
            if value(index + 3) in (".", "?.") and is_identifier_token(prop):
                assert prop
                fields.append(
                    _definition_field(prop.value, VALIDATOR_TYPES[token.value])
                )
    return dedupe_fields(fields)


def _definition_field(name: str, type: str | None) -> FeatureField:
    field = FeatureField(name=name, source="precondition", type=type)
    if type and _ENUM_TYPE_NAME.match(type) and type not in NON_ENUM_TYPES:
        field.enumType = type
    return field


def _current_open_brace(tokens: list[Token], offset: int) -> int:
    stack: list[int] = []
    for index, token in enumerate(tokens):
        if token.offset >= offset:
            break
        if token.value == "{":
            stack.append(index)
        elif token.value == "}" and stack:
            stack.pop()
    return stack[-1] if stack else -1


def _expecting_map_key(tokens: list[Token], open_index: int, offset: int) -> bool:
    expecting_key = True
    paren = bracket = brace = 0
    for token in tokens[open_index + 1 :]:
        if token.offset >= offset:
            break
        value = token.value
        if value == "{":
            brace += 1
        elif value == "}":
            if brace == 0:
                break
            brace -= 1
        elif value == "(":
            paren += 1
        elif value == ")":
            paren = max(0, paren - 1)
        elif value == "[":
            bracket += 1
        elif value == "]":
            bracket = max(0, bracket - 1)
        elif paren == 0 and bracket == 0 and brace == 0:
            if value == ":":
                expecting_key = False
            elif value == ",":
                expecting_key = True
    return expecting_key


def _call_for_map(tokens: list[Token], open_index: int) -> tuple[str, int] | None:
    """Returns the name of the function call containing the map, and which argument it is."""
    depth = 0
    call_open = -1
    for index in range(open_index - 1, -1, -1):
        value = tokens[index].value
        if value == ")":
            depth += 1
        elif value == "(":
            if depth == 0:
                call_open = index
                break
            depth -= 1
    if call_open < 1:
        return None
    call_token = tokens[call_open - 1]
    if not is_identifier_token(call_token):
        return None
    return call_token.value, _argument_index(tokens, call_open, open_index)


def _argument_index(tokens: list[Token], open_paren: int, target: int) -> int:
    argument = paren = bracket = brace = 0
    for token in tokens[open_paren + 1 : target]:
        value = token.value
        if value == "(":
            paren += 1
        elif value == ")":
            paren = max(0, paren - 1)
        elif value == "[":
            bracket += 1
        elif value == "]":
            bracket = max(0, bracket - 1)
        elif value == "{":
            brace += 1
        elif value == "}":
            brace = max(0, brace - 1)
        elif value == "," and paren == 0 and bracket == 0 and brace == 0:
            argument += 1
    return argument


def _existing_top_level_keys(tokens: list[Token], open_index: int) -> set[str]:
    keys = set()
    close = _matching_brace(tokens, open_index)
    stop = len(tokens) if close < 0 else close
    paren = bracket = brace = 0
    for index in range(open_index + 1, stop):
        token = tokens[index]
        value = token.value
        if value == "{":
            brace += 1
        elif value == "}":
            brace = max(0, brace - 1)
        elif value == "(":
            paren += 1
        elif value == ")":
            paren = max(0, paren - 1)
        elif value == "[":
            bracket += 1
        elif value == "]":
            bracket = max(0, bracket - 1)
        if paren or bracket or brace:
            continue
        next_token = tokens[index + 1] if index + 1 < len(tokens) else None
        if (
            (token.kind == "string" or is_identifier_token(token))
            and next_token is not None
            and next_token.value == ":"
        ):
            keys.add(_unquote(value))
    return keys


def _map_key_replacement(
    tokens: list[Token], open_index: int, offset: int
) -> tuple[int, int]:
    index = _token_index_at_cursor(tokens, offset)
    if index > open_index:
        token = tokens[index]
        if token.kind == "string":
            quote = token.value[0]
            closed = len(token.value) > 1 and token.value.endswith(quote)
            return token.offset, token.end if closed else offset
        if is_identifier_token(token):
            return token.offset, token.end
    return offset, offset


def _matching_brace(tokens: list[Token], open_index: int) -> int:
    depth = 0
    for index in range(open_index, len(tokens)):
        if tokens[index].value == "{":
            depth += 1
        elif tokens[index].value == "}":
            depth -= 1
            if depth == 0:
                return index
    return -1


def _token_index_at_cursor(tokens: list[Token], offset: int) -> int:
    for index, token in enumerate(tokens):
        if token.offset <= offset <= token.end and token.end > token.offset:
            return index
    return -1


def _last_token_ending_by(tokens: list[Token], offset: int) -> int:
    for index in range(len(tokens) - 1, -1, -1):
        if tokens[index].end <= offset:
            return index
    return -1


def _unquote(value: str) -> str:
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
        return value[1:-1]
    return value


def _lexical_state(source: str, offset: int) -> str:
    """Returns "comment", "string", or "normal" for the text just before offset."""
    state = "normal"
    quote = ""
    index = 0
    limit = min(offset, len(source))
    while index < limit:
        char = source[index]
        following = source[index + 1] if index + 1 < len(source) else ""
        if state == "line_comment":
            if char == "\n":
                state = "normal"
        elif state == "block_comment":
            if char == "*" and following == "/":
                index += 1
                state = "normal"
        elif state == "string":
            if char == "\\":
                index += 1
            elif char == quote:
                state = "normal"
        elif char == "/" and following == "/":
            index += 1
            state = "line_comment"
        elif char == "/" and following == "*":
            index += 1
            state = "block_comment"
        elif char in "\"'":
            quote = char
            state = "string"
        index += 1
    if state in ("line_comment", "block_comment"):
        return "comment"
    return state
