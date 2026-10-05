"""Tests for parsing doc comments and rendering them like FsDoc."""

from fs_lsp.fsdoc import parse_doc, render_markdown
from fs_lsp.scanner import scan
from fs_lsp.signatures import call_at, parse_signature, source_signature

DOC = """Extrudes faces. Use [qCreatedBy] to find what it made.
@param id : @autocomplete `id + "extrude1"`
@param definition {{
     @field entities {Query} : Faces to extrude.
     @field endDepth {ValueWithUnits} : @requiredif {`endBound` is `BLIND`.}
             How far to extrude.
             @eg `1 * inch`
     @field startBound {BoundingType} : @optional
             The type of start bound.
     @field options {{
          @field draft {boolean} : Whether to draft.
     }}
}}
@returns {Query} : The new bodies.
@throws {GBTErrorStringEnum.BAD_GEOMETRY} : If there's nothing to extrude.
@seealso [opRevolve]
"""


def test_parse_doc():
    doc = parse_doc(DOC)
    assert doc.description == "Extrudes faces. Use [qCreatedBy] to find what it made."
    id, definition = doc.params
    assert (id.name, id.description) == ("id", "")
    entities, depth, start, options = definition.fields
    assert (entities.name, entities.type, entities.description) == ("entities", "Query", "Faces to extrude.")
    assert depth.required_if == "`endBound` is `BLIND`."
    assert depth.description == "How far to extrude."
    assert depth.examples == ["`1 * inch`"]
    assert start.optional and start.description == "The type of start bound."
    assert [field.name for field in options.fields] == ["draft"]
    assert (doc.returns.type, doc.returns.description) == ("Query", "The new bodies.")
    assert doc.throws[0].type == "GBTErrorStringEnum.BAD_GEOMETRY"
    assert doc.see_also == ["[opRevolve]"]


def test_render_like_fsdoc():
    markdown = render_markdown(parse_doc(DOC), "extrude(context is Context, id is Id, definition is map)")
    assert markdown.startswith("Extrudes faces. Use `qCreatedBy` to find what it made.")
    assert "**See also**\n\n`opRevolve`" in markdown
    # The id has only completion text, so it isn't listed; the definition's type comes from the signature
    assert "| `id` |" not in markdown
    assert "| `definition` | `map` |  |" in markdown
    assert "| • `endDepth` | `ValueWithUnits` | *Required if `endBound` is `BLIND`.* How far to extrude. **EXAMPLE** `1 * inch` |" in markdown
    assert "| • `startBound` | `BoundingType` | *Optional* The type of start bound. |" in markdown
    assert "| &nbsp;&nbsp;&nbsp;&nbsp;• `draft` | `boolean` | Whether to draft. |" in markdown
    assert "| Return type | Description |\n| --- | --- |\n| `Query` | The new bodies. |" in markdown
    assert "| `GBTErrorStringEnum.BAD_GEOMETRY` | If there's nothing to extrude. |" in markdown


def test_enum_values_and_code():
    doc = parse_doc("A direction.\n@value UP : Toward `{ y : 1 }`, up.\n@value DOWN : Down.")
    assert [(value.name, value.description) for value in doc.values] == [
        ("UP", "Toward `{ y : 1 }`, up."),
        ("DOWN", "Down."),
    ]
    assert "| `UP` | Toward `{ y : 1 }`, up. |" in render_markdown(doc)


def test_signatures():
    signature = parse_signature("opExtrude", "export const opExtrude = function(context is Context, id is Id, definition is map)")
    assert signature.label == "opExtrude(context is Context, id is Id, definition is map)"
    start = signature.label.index("id is Id")
    assert signature.parameter_offsets()[1] == (start, start + len("id is Id"))
    feature = parse_signature(
        "extrude", "export const extrude = defineFeature(function(context is Context, id is Id, definition is map)"
    )
    assert feature.parameters == ["context is Context", "id is Id", "definition is map"]
    returning = parse_signature("f", "export function f(a is map, b) returns Query")
    assert returning.label == "f(a is map, b) returns Query"

    source = "/**\n * Adds.\n * @param a : The first.\n */\nexport function add(a is number,\n    b is number) returns number\n{\n}"
    signature = source_signature(source, "add", 4)
    assert signature.label == "add(a is number, b is number) returns number"
    assert signature.parameter_doc(0) == "The first."
    assert signature.parameter_doc(1) is None


def test_call_at():
    source = 'f(a, g(b, { "x" : [1, 2], "y" : 3 }), c)'
    tokens = scan(source).tokens
    call = call_at(tokens, source.index('"y"'))
    assert (call.callee.value, call.argument) == ("g", 1)
    call = call_at(tokens, source.index("c)"))
    assert (call.callee.value, call.argument) == ("f", 2)
    assert call_at(tokens, 0) is None
