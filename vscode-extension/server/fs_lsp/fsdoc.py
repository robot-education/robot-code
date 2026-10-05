"""Parses FeatureScript doc comments, and renders them the way Onshape's FsDoc does
(https://cad.onshape.com/FsDoc/library.html).

Doc comments are written like the std library's:

    /**
     * Extrudes one or more edges or faces. Use [qCreatedBy] to find what it made.
     * @param id : @autocomplete `id + "extrude1"`
     * @param definition {{
     *      @field entities {Query} : Edges and faces to extrude.
     *      @field endDepth {ValueWithUnits} : @requiredif {`endBound` is `BLIND`.}
     *              How far to extrude.
     *              @eg `1 * inch`
     *      @field startBound {BoundingType} : @optional
     *              The type of start bound.
     * }}
     * @returns {Query} : The new bodies.
     * @throws {GBTErrorStringEnum.BAD_GEOMETRY} : If there's nothing to extrude.
     * @seealso [opRevolve]
     */

`@param`, `@field`, `@returns` (or `@return`), and `@throws` take an optional `{Type}` (or `{{ fields }}`) and a
`:` description; descriptions may say `@optional`, `@requiredif {condition}`, and give examples with `@eg`, `@ex`,
or `@example`. `@autocomplete` text is for completions, so it isn't shown. `@value NAME : description` documents an
enum's values, and `@internal` marks things which aren't part of a module's interface.
"""

from __future__ import annotations

import dataclasses
import re

# Tags which start a new part of a doc comment
_STRUCTURAL = {"param", "field", "returns", "return", "throws", "seealso", "seeAlso", "value", "type", "internal"}
# Tags which describe the part they're in
_INLINE = {"optional", "requiredif", "requiredIf", "eg", "ex", "example", "autocomplete"}
_TAG = re.compile(r"@([A-Za-z]+)")
_NAME = re.compile(r"[A-Za-z_][A-Za-z0-9_.]*")


@dataclasses.dataclass
class DocItem:
    """A documented parameter, map field, return value, thrown error, or enum value."""

    name: str | None = None
    type: str | None = None
    description: str = ""
    optional: bool = False
    required_if: str | None = None
    examples: list[str] = dataclasses.field(default_factory=list)
    # A map's documented fields, written in `{{ }}`
    fields: list[DocItem] = dataclasses.field(default_factory=list)


@dataclasses.dataclass
class DocComment:
    description: str = ""
    examples: list[str] = dataclasses.field(default_factory=list)
    params: list[DocItem] = dataclasses.field(default_factory=list)
    returns: DocItem | None = None
    throws: list[DocItem] = dataclasses.field(default_factory=list)
    see_also: list[str] = dataclasses.field(default_factory=list)
    values: list[DocItem] = dataclasses.field(default_factory=list)
    # Fields documented outside a parameter, as for a type's
    fields: list[DocItem] = dataclasses.field(default_factory=list)
    internal: bool = False

    def param(self, name: str) -> DocItem | None:
        return next((param for param in self.params if param.name == name), None)


def parse_doc(text: str) -> DocComment:
    """Parses a doc comment's text (without its `/**`, `*/`, and leading `*`s)."""
    scanner = _Scanner(text)
    doc = DocComment()
    description = scanner.body(0)
    doc.description, doc.examples = description.description, description.examples
    while not scanner.at_end():
        tag = scanner.take_tag()
        if tag is None:
            # Unreachable, since text only stops at a tag, but don't loop forever
            break
        if tag == "param":
            doc.params.append(scanner.item(tag, 0))
        elif tag in ("returns", "return"):
            doc.returns = scanner.item(tag, 0)
        elif tag == "throws":
            doc.throws.append(scanner.item(tag, 0))
        elif tag in ("seealso", "seeAlso"):
            if see_also := scanner.body(0).description:
                doc.see_also.append(see_also)
        elif tag == "value":
            doc.values.append(scanner.item(tag, 0))
        elif tag == "field":
            doc.fields.append(scanner.item(tag, 0))
        elif tag == "internal":
            doc.internal = True
            if more := scanner.body(0).description:
                doc.description = f"{doc.description}\n\n{more}".strip()
        else:
            if more := scanner.body(0).description:
                doc.description = f"{doc.description}\n\n{more}".strip()
    return doc


class _Scanner:
    def __init__(self, text: str) -> None:
        self.text = text
        self.position = 0

    def at_end(self) -> bool:
        return self.position >= len(self.text)

    def skip_space(self) -> None:
        while not self.at_end() and self.text[self.position].isspace():
            self.position += 1

    def tag_at(self, position: int) -> str | None:
        """The known tag at position, if any (and it starts a word)."""
        match = _TAG.match(self.text, position)
        if match is None or match[1] not in _STRUCTURAL | _INLINE:
            return None
        if position > 0 and not (self.text[position - 1].isspace() or self.text[position - 1] in "{(:"):
            return None
        return match[1]

    def take_tag(self) -> str | None:
        tag = self.tag_at(self.position)
        if tag is not None:
            self.position += len(tag) + 1
        return tag

    def closing(self) -> bool:
        return self.text.startswith("}}", self.position)

    def text_until_tag(self, depth: int) -> str:
        """Text up to the next known tag, or (in a `{{ }}`) its end, skipping over code."""
        start = self.position
        while not self.at_end():
            if self.text.startswith("```", self.position):
                end = self.text.find("```", self.position + 3)
                self.position = len(self.text) if end < 0 else end + 3
                continue
            char = self.text[self.position]
            if char == "`":
                end = self.text.find("`", self.position + 1)
                self.position = len(self.text) if end < 0 else end + 1
                continue
            if char == "@" and self.tag_at(self.position):
                break
            if depth > 0 and self.closing():
                break
            self.position += 1
        return self.text[start : self.position]

    def braced(self) -> str:
        """The text inside a `{...}` at the position, with nested braces."""
        depth = 0
        start = self.position
        while not self.at_end():
            char = self.text[self.position]
            if char == "`":
                end = self.text.find("`", self.position + 1)
                self.position = len(self.text) if end < 0 else end + 1
                continue
            self.position += 1
            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    return self.text[start + 1 : self.position - 1].strip()
        return self.text[start + 1 :].strip()

    def body(self, depth: int) -> DocItem:
        """A description, with what its inline tags say."""
        item = DocItem()
        parts = []
        while True:
            parts.append(self.text_until_tag(depth))
            tag = self.tag_at(self.position)
            if tag not in _INLINE:
                break
            self.take_tag()
            if tag == "optional":
                item.optional = True
            elif tag in ("requiredif", "requiredIf"):
                self.skip_space()
                if self.text.startswith("{", self.position) and not self.text.startswith("{{", self.position):
                    item.required_if = _clean(self.braced())
                else:
                    item.required_if = _clean(self.text_until_tag(depth))
            elif tag in ("eg", "ex", "example"):
                if example := _clean(self.text_until_tag(depth)):
                    item.examples.append(example)
            else:
                # Completion text, which isn't shown
                self.text_until_tag(depth)
        item.description = _clean("".join(parts))
        return item

    def item(self, tag: str, depth: int) -> DocItem:
        """A `@param`, `@field`, `@returns`, `@throws`, or `@value`, after its tag."""
        self.skip_space()
        name = None
        if tag in ("param", "field", "value"):
            match = _NAME.match(self.text, self.position)
            if match:
                name = match[0]
                self.position = match.end()
        self.skip_space()
        type = None
        fields: list[DocItem] = []
        if self.text.startswith("{{", self.position):
            self.position += 2
            fields = self.fields(depth + 1)
            if self.closing():
                self.position += 2
            self.skip_space()
        elif self.text.startswith("{", self.position):
            type = self.braced()
            self.skip_space()
        if self.text.startswith(":", self.position):
            self.position += 1
            item = self.body(depth)
        else:
            item = DocItem()
        item.name, item.type, item.fields = name, type, fields
        return item

    def fields(self, depth: int) -> list[DocItem]:
        """The fields in a `{{ }}`, up to its end."""
        fields: list[DocItem] = []
        while not self.at_end():
            self.text_until_tag(depth)
            if self.closing() or self.at_end():
                break
            tag = self.take_tag()
            if tag is None:
                break
            if tag in _INLINE:
                # Describes the last field, e.g. an example written after it
                self.position -= len(tag) + 1
                extra = self.body(depth)
                if fields:
                    fields[-1].examples.extend(extra.examples)
                    fields[-1].optional = fields[-1].optional or extra.optional
                continue
            fields.append(self.item(tag, depth))
        return fields


def _clean(text: str) -> str:
    """Joins wrapped lines into paragraphs (keeping code blocks as they are)."""
    paragraphs = []
    for block in re.split(r"(```.*?```)", text.strip(), flags=re.S):
        if block.startswith("```"):
            paragraphs.append(block)
            continue
        for paragraph in re.split(r"\n\s*\n", block):
            joined = " ".join(line.strip() for line in paragraph.splitlines() if line.strip())
            if joined:
                paragraphs.append(joined)
    return "\n\n".join(paragraphs)


# Rendering

_REFERENCE = re.compile(r"\[([A-Za-z_][A-Za-z0-9_.]*)(\([^\]\n]*\))?\](?!\()")


def references(text: str) -> str:
    """FsDoc's `[name]` links, as code."""
    return _REFERENCE.sub(lambda match: f"`{match[1]}`", text)


def parameter_types(signature: str) -> dict[str, str]:
    """The type of each parameter in a signature, e.g. `{"context": "Context"}`."""
    inside = signature[signature.find("(") + 1 :] if "(" in signature else ""
    return dict(re.findall(r"([A-Za-z_]\w*)\s+is\s+([A-Za-z_][\w.]*)", inside.split(")")[0]))


def return_type(signature: str) -> str | None:
    match = re.search(r"\)\s*returns\s+([A-Za-z_][\w.]*)", signature)
    return match[1] if match else None


def render_markdown(doc: DocComment, signature: str | None = None) -> str:
    """The doc as markdown laid out like FsDoc: its description, See also, then tables of its parameters, return
    value, and errors."""
    types = parameter_types(signature or "")
    sections = []
    if doc.description:
        sections.append(references(doc.description))
    sections.extend(_examples(doc.examples))
    if doc.see_also:
        sections.append("**See also**\n\n" + "\n\n".join(references(see) for see in doc.see_also))
    rows = []
    for param in doc.params:
        if not (param.description or param.fields or param.examples or param.type):
            # Only completion text, like an id's
            continue
        rows.extend(_rows(param, param.type or types.get(param.name or ""), 0))
    if rows:
        sections.append(_table(["Parameter", "Type", "Additional Info"], rows))
    if doc.fields:
        sections.append(_table(["Field", "Type", "Additional Info"], [row for f in doc.fields for row in _rows(f, f.type, 0)]))
    if doc.values:
        sections.append(_table(["Value", "Description"], [[f"`{value.name}`", _info(value)] for value in doc.values]))
    returns = doc.returns
    if returns is not None or return_type(signature or ""):
        returned = returns or DocItem()
        type = returned.type or return_type(signature or "") or ("map" if returned.fields else "")
        if returned.fields:
            rows = [["", f"`{type}`" if type else "", _info(returned)]]
            rows.extend(row for field in returned.fields for row in _rows(field, field.type, 1))
            sections.append(_table(["Return", "Type", "Description"], rows))
        elif returned.description:
            sections.append(_table(["Return type", "Description"], [[f"`{type}`" if type else "", _info(returned)]]))
    if doc.throws:
        sections.append(_table(["Throws", "Description"], [[f"`{error.type or ''}`", _info(error)] for error in doc.throws]))
    if doc.internal:
        sections.append("*Internal*")
    return "\n\n".join(section for section in sections if section)


def param_markdown(item: DocItem, type: str | None = None) -> str:
    """A parameter's documentation on its own, e.g. for signature help."""
    rows = _rows(item, item.type or type, 0)
    if len(rows) == 1:
        return _info(item)
    return _table(["Parameter", "Type", "Additional Info"], rows)


def _rows(item: DocItem, type: str | None, depth: int) -> list[list[str]]:
    rows = [[_bullet(item.name, depth), f"`{type}`" if type else ("`map`" if item.fields else ""), _info(item)]]
    for field in item.fields:
        rows.extend(_rows(field, field.type, depth + 1))
    return rows


def _bullet(name: str | None, depth: int) -> str:
    code = f"`{name}`" if name else ""
    return ("&nbsp;" * 4 * (depth - 1) + "• " if depth else "") + code


def _info(item: DocItem) -> str:
    parts = []
    if item.optional:
        parts.append("*Optional*")
    if item.required_if:
        parts.append(f"*Required if {references(item.required_if).rstrip('.')}.*")
    if item.description:
        parts.append(references(item.description))
    parts.extend(f"**EXAMPLE** {references(example)}" for example in item.examples)
    return " ".join(parts)


def _examples(examples: list[str]) -> list[str]:
    return [f"**EXAMPLE**\n\n{references(example)}" for example in examples]


def _table(headings: list[str], rows: list[list[str]]) -> str:
    def cell(text: str) -> str:
        # Tables can't hold line breaks or unescaped pipes
        return re.sub(r"\s*\n\s*", " ", text).replace("|", "\\|")

    lines = ["| " + " | ".join(headings) + " |", "|" + " --- |" * len(headings)]
    lines.extend("| " + " | ".join(cell(text) for text in row) + " |" for row in rows)
    return "\n".join(lines)
