"""Tests for the FeatureScript language server, ported from gatrall/featurescript-language-support."""

import pathlib

import pytest

from fs_lsp.completion import EnumCompletion, FeatureMapCompletion, completion_data
from fs_lsp.diagnostics import diagnostics
from fs_lsp.doc_comments import leading_doc_comment
from fs_lsp.hover import hover_markdown
from fs_lsp.navigation import document_symbols, folding_ranges
from fs_lsp.parser import parse
from fs_lsp.semantic import build_semantic_tokens, encode
from fs_lsp.symbol_index import SymbolIndex

FIXTURES = pathlib.Path(__file__).parent / "fixtures"


def fixture(name: str) -> str:
    return (FIXTURES / name).read_text()


def node_types(name: str) -> set[str]:
    return {node.type for node in parse(fixture(name)).nodes}


# Parser


def test_parser_headers_imports_and_features():
    parsed = parse(fixture("semantic.fs"))
    types = {node.type for node in parsed.nodes}
    assert {
        "VersionDirective",
        "ImportDeclaration",
        "NamespacedImportDeclaration",
        "FeatureDeclaration",
    } <= types
    assert parsed.imports_stdlib


def test_parser_annotations_maps_blocks_preconditions():
    assert {
        "AnnotationStatement",
        "AnnotationMap",
        "MapLiteral",
        "Block",
        "PreconditionBlock",
    } <= node_types("annotations.fs")


def test_parser_predicates_types_operators_enums():
    assert {"PredicateDeclaration", "TypeDeclaration"} <= node_types("predicates.fs")
    assert "OperatorDeclaration" in node_types("operators.fs")
    assert {"EnumDeclaration", "EnumMember"} <= node_types("semantic.fs")


def test_parser_lambdas_safe_access_and_type_expressions():
    assert {"ArrowFunction", "FunctionExpression"} <= node_types("operators.fs")
    assert {
        "SafeMemberAccess",
        "SafeIndexAccess",
        "BoxAccess",
        "SafeBoxAccess",
        "TypeConversion",
        "TypeCheck",
    } <= node_types("maps-vs-blocks.fs")


@pytest.mark.parametrize("name", sorted(p.name for p in FIXTURES.glob("*.fs")))
def test_parser_handles_every_prefix(name):
    """The parser must never crash or hang on incomplete code."""
    source = fixture(name)
    for end in range(0, len(source), 7):
        parsed = parse(source[:end])
        SymbolIndex(parsed)
        encode(build_semantic_tokens(parsed))
        document_symbols(parsed)
        folding_ranges(parsed)


# Semantic tokens


@pytest.fixture(scope="module")
def semantic_tokens():
    parsed = parse(fixture("semantic.fs"))
    return [
        (entry.token.value, entry.type, entry.modifiers)
        for entry in build_semantic_tokens(parsed)
    ]


def find(tokens, value, type=None, modifier=None):
    for token in tokens:
        if (
            token[0] == value
            and (type is None or token[1] == type)
            and (modifier is None or modifier in token[2])
        ):
            return token
    raise AssertionError(f"Missing semantic token {value} {type or ''}")


def test_semantic_declarations_and_parameters(semantic_tokens):
    assert {"declaration", "readonly"} <= set(
        find(semantic_tokens, "slot", "feature")[2]
    )
    assert "declaration" in find(semantic_tokens, "helper", "function")[2]
    find(semantic_tokens, "canBePerson", "predicate", "declaration")
    assert "declaration" in find(semantic_tokens, "Person", "type")[2]
    assert "declaration" in find(semantic_tokens, "MyOption", "enum")[2]
    assert "declaration" in find(semantic_tokens, "definition", "parameter")[2]


def test_semantic_keys_properties_and_namespaces(semantic_tokens):
    assert find(semantic_tokens, '"Feature Type Name"')[1] == "annotationKey"
    assert find(semantic_tokens, '"entities"')[1] == "mapKey"
    assert find(semantic_tokens, "unquotedKey")[1] == "mapKey"
    find(semantic_tokens, "width", "property")
    assert find(semantic_tokens, "foo")[1] == "namespace"


def test_semantic_stdlib_symbols_and_enum_members(semantic_tokens):
    for value, type, modifiers in [
        ("Context", "type", {"defaultLibrary"}),
        ("defineFeature", "function", {"defaultLibrary"}),
        ("opExtrude", "function", {"defaultLibrary"}),
        ("qCreatedBy", "function", {"defaultLibrary"}),
        ("LENGTH_BOUNDS", "variable", {"readonly", "defaultLibrary"}),
        ("inch", "variable", {"readonly", "defaultLibrary"}),
        ("BoundingType", "enum", {"defaultLibrary"}),
        ("THROUGH_ALL", "enumMember", {"readonly", "defaultLibrary"}),
        ("ONE", "enumMember", {"readonly"}),
    ]:
        assert modifiers <= set(find(semantic_tokens, value, type)[2]), value
    assert any(
        value == "ONE" and type == "enumMember" and "declaration" not in modifiers
        for value, type, modifiers in semantic_tokens
    )


def test_semantic_encoding_is_relative():
    parsed = parse("FeatureScript 2909;\nconst a = 1;")
    data = encode(build_semantic_tokens(parsed))
    assert len(data) % 5 == 0
    # Second line's first token ("const") starts at column 0 of a new line
    lines = [data[i] for i in range(0, len(data), 5)]
    assert lines.count(1) == 1


# Symbol index

SYMBOL_SOURCE = "\n".join(
    [
        "FeatureScript 2909;",
        'import(path : "onshape/std/geometry.fs", version : "2909.0");',
        "export enum MyOption",
        "{",
        "    ONE,",
        "    TWO",
        "}",
        "export type Person typecheck canBePerson;",
        "export predicate canBePerson(value)",
        "{",
        "    value is map;",
        "}",
        "function helper(context is Context, query is Query) returns Query",
        "{",
        "    const local = query;",
        "    return local;",
        "}",
        "const shared = 1;",
        "function scopeA(x is number)",
        "{",
        "    const shared = x;",
        "    return shared;",
        "}",
        "function scopeB(x is number)",
        "{",
        "    return shared;",
        "}",
        "const keyName = 1;",
        "const ordinaryMap = { keyName : keyName };",
        'annotation { keyName : "Ignored" }',
        'annotation { "Feature Type Name" : "Slot" }',
        "export const slot = defineFeature(function(context is Context, id is Id, definition is map)",
        "precondition",
        "{",
        '    annotation { "Name" : "Width" }',
        "    definition.width is number;",
        "    definition.mode is MyOption;",
        "}",
        "{",
        "    helper(context, definition.width);",
        "    const selected = MyOption.ONE;",
        "    const again = slot;",
        "});",
    ]
)


@pytest.fixture(scope="module")
def index():
    return SymbolIndex(parse(SYMBOL_SOURCE))


def offset_of(needle: str, after: int | str = 0) -> int:
    offset = SYMBOL_SOURCE.index(needle)
    return offset + (len(after) if isinstance(after, str) else after)


def definition_at(index, needle, after=0):
    return index.definition_at(offset_of(needle, after))


def test_index_functions_features_and_typecheck_predicates(index):
    assert definition_at(
        index, "helper(context, definition.width)"
    ).token.offset == offset_of("helper(context is Context")
    assert definition_at(index, "slot;").token.offset == offset_of(
        "slot = defineFeature"
    )
    assert definition_at(
        index, "typecheck canBePerson", "typecheck "
    ).token.offset == offset_of("predicate canBePerson", "predicate ")


def test_index_enum_members(index):
    one = definition_at(index, "MyOption.ONE", "MyOption.")
    assert one.kind == "enumMember"
    assert one.parent == "MyOption"
    assert one.token.offset == offset_of("ONE,")


def test_index_prefers_inner_scopes(index):
    assert definition_at(
        index, "const local = query", "const local = "
    ).token.offset == offset_of("query is Query")
    assert definition_at(index, "return local", "return ").token.offset == offset_of(
        "local = query"
    )
    assert definition_at(index, "return shared;", "return ").token.offset == offset_of(
        "shared = x"
    )
    prefix = "scopeB(x is number)\n{\n    return "
    assert definition_at(index, prefix + "shared", prefix).token.offset == offset_of(
        "shared = 1"
    )


def test_index_definition_properties(index):
    width = definition_at(
        index, "helper(context, definition.width)", "helper(context, definition."
    )
    assert width.kind == "definitionProperty"
    assert width.token.offset == offset_of("definition.width is number", "definition.")
    references = index.references_at(
        offset_of("definition.width is number", "definition."), True
    )
    assert len([r for r in references if r.token.value == "width"]) == 2


def test_index_ignores_map_and_annotation_keys(index):
    assert definition_at(index, "ordinaryMap = { keyName", "ordinaryMap = { ") is None
    assert definition_at(
        index, "keyName : keyName", "keyName : "
    ).token.offset == offset_of("keyName = 1")
    assert definition_at(index, "annotation { keyName", "annotation { ") is None


def test_index_include_declaration(index):
    declaration = offset_of("helper(context is Context")
    with_declaration = index.references_at(declaration, True)
    without = index.references_at(declaration, False)
    assert any(r.token.offset == declaration for r in with_declaration)
    assert any(
        r.token.offset == offset_of("helper(context, definition.width)")
        for r in with_declaration
    )
    assert not any(r.token.offset == declaration for r in without)


# Hover


def test_hover_local_and_stdlib():
    parsed = parse(SYMBOL_SOURCE)
    index = SymbolIndex(parsed)
    markdown, token = hover_markdown(
        parsed, index, offset_of("helper(context, definition.width)")
    )
    assert token.value == "helper"
    assert "FeatureScript function" in markdown
    assert "function helper(context is Context" in markdown

    markdown, _ = hover_markdown(parsed, index, offset_of("MyOption\n{"))
    assert "MyOption variants (2)" in markdown

    markdown, _ = hover_markdown(parsed, index, offset_of("defineFeature"))
    assert "stdlib function" in markdown


# Doc comments


def test_doc_line_comments():
    lines = [
        "FeatureScript 2909;",
        "/// First line.",
        "// Second line.",
        "function helper(context is Context)",
        "{",
        "}",
    ]
    assert leading_doc_comment(lines, 3) == "First line.\nSecond line."


def test_doc_block_comments():
    lines = [
        "FeatureScript 2909;",
        "/**",
        " * Builds a slot.",
        " * Uses a path query.",
        " */",
        "export const slot = defineFeature(function(context is Context, id is Id, definition is map)",
        "{",
        "});",
    ]
    assert leading_doc_comment(lines, 5) == "Builds a slot.\nUses a path query."


def test_doc_skips_annotations():
    lines = [
        "FeatureScript 2909;",
        "/// Width parameter docs.",
        "annotation",
        "{",
        '    "Name" : "Width"',
        "}",
        "definition.width is number;",
    ]
    assert leading_doc_comment(lines, 6) == "Width parameter docs."


# Completion

HEADER = "\n".join(
    [
        "FeatureScript 2909;",
        'import(path : "onshape/std/geometry.fs", version : "2909.0");',
    ]
)


def completion_at(source: str, marker: str = "<>"):
    offset = source.index(marker)
    clean = source[:offset] + source[offset + len(marker) :]
    return completion_data(parse(clean), offset)


def test_completion_enum_members():
    source = HEADER + "\nexport enum MyOption\n{\n    ONE,\n    TWO\n}"
    stdlib_data = completion_at(source + "\nconst a = BoundingType.<>;")
    assert isinstance(stdlib_data, EnumCompletion)
    assert any(m.name == "THROUGH_ALL" for m in stdlib_data.members)

    local_data = completion_at(source + "\nconst b = MyOption.<>;")
    assert isinstance(local_data, EnumCompletion)
    assert [m.name for m in local_data.members] == ["ONE", "TWO"]


def test_completion_stdlib_feature_fields():
    prefix = HEADER + "\nfunction run(context is Context, id is Id)\n{"

    def fields(call: str) -> set[str]:
        data = completion_at(f"{prefix}\n    {call}\n}}")
        assert isinstance(data, FeatureMapCompletion)
        return {field.name for field in data.fields}

    assert {"entities", "endBound"} <= fields(
        'extrude(context, id + "extrude", { <> });'
    )
    assert "profileSketch" in fields('frame(context, id + "frame", { <> });')
    assert {"keepTools", "operationType"} <= fields(
        'enclose(context, id + "enclose", { <> });'
    )

    quoted = completion_at(
        f'{prefix}\n    extrude(context, id + "extrude", {{ "<> }});\n}}'
    )
    assert isinstance(quoted, FeatureMapCompletion)
    assert quoted.replacement_end == quoted.replacement_start + 1


def test_completion_local_feature_fields():
    source = "\n".join(
        [
            "FeatureScript 2909;",
            "export const slot = defineFeature(function(context is Context, id is Id, definition is map)",
            "precondition",
            "{",
            "    definition.width is number;",
            "    isLength(definition.depth, LENGTH_BOUNDS);",
            "}",
            "{",
            "});",
            "function run(context is Context, id is Id)",
            "{",
            '    slot(context, id + "slot", { "width" : 1, <> });',
            "}",
        ]
    )
    data = completion_at(source)
    assert isinstance(data, FeatureMapCompletion)
    names = {field.name for field in data.fields}
    assert "width" not in names
    assert "depth" in names


def test_completion_not_in_comments_or_strings():
    source = HEADER + '\n// BoundingType.<>\nconst text = "BoundingType.<>";'
    assert completion_at(source) is None
    assert (
        completion_at(source.replace("// BoundingType.<>", "// BoundingType.BLIND"))
        is None
    )


# Diagnostics


def messages(source: str) -> list[str]:
    return [d.message for d in diagnostics(parse(source))]


def test_diagnostics_clean_fixtures():
    for path in FIXTURES.glob("*.fs"):
        found = messages(path.read_text())
        if path.name == "operators.fs":
            # Deliberately uses the invalid ++ and -- operators
            assert len(found) == 2 and all("operator" in m for m in found)
        else:
            assert found == [], path.name


def test_diagnostics_brackets_and_operators():
    assert any("never closed" in m for m in messages("function f() {\n  g(1;\n}"))
    assert any("Unmatched" in m for m in messages("const a = 1);"))
    assert any("no ++ operator" in m for m in messages("i++;"))
    assert any("Unterminated string" in m for m in messages('const a = "abc'))
    assert any("Unterminated comment" in m for m in messages("/* abc"))


# Navigation


def test_document_symbols():
    symbols = {s.name: s for s in document_symbols(parse(SYMBOL_SOURCE))}
    assert {"MyOption", "Person", "canBePerson", "helper", "slot"} <= set(symbols)
    assert [c.name for c in symbols["MyOption"].children] == ["ONE", "TWO"]


def test_folding_ranges():
    ranges = folding_ranges(parse(SYMBOL_SOURCE))
    assert any(r.start_line == 2 and r.end_line == 6 for r in ranges)


# Stdlib index generation

STDLIB_SOURCE = "\n".join(
    [
        "FeatureScript 2909;",
        "export enum MyEnum",
        "{",
        "    ONE,",
        "    TWO",
        "}",
        "export predicate extraFields(booleanDefinition is map)",
        "{",
        '    annotation { "Name" : "Merge scope" }',
        "    booleanDefinition.mergeScope is Query;",
        "}",
        "/**",
        " * Build a test feature.",
        " *",
        " * @param definition {{",
        " *      @field width {ValueWithUnits} : @optional Width field docs.",
        " * }}",
        " */",
        "export const testFeature = defineFeature(function(context is Context, id is Id, definition is map)",
        "precondition",
        "{",
        '    annotation { "Name" : "Entities", "Description" : "Input faces" }',
        "    definition.entities is Query;",
        '    annotation { "Name" : "Depth" }',
        "    isLength(definition.depth, LENGTH_BOUNDS);",
        "    extraFields(definition);",
        "}",
        "{",
        "});",
    ]
)


def test_stdlib_metadata_extraction():
    from fs_lsp.tools.update_stdlib import SourceFile, generate

    symbols, metadata = generate([SourceFile("fixture.fs", STDLIB_SOURCE)])
    assert {"name": "testFeature", "kind": "function"}.items() <= next(
        s for s in symbols if s["name"] == "testFeature"
    ).items()

    my_enum = next(e for e in metadata["enums"] if e["name"] == "MyEnum")
    assert [m["name"] for m in my_enum["members"]] == ["ONE", "TWO"]

    feature = next(f for f in metadata["features"] if f["name"] == "testFeature")
    assert feature["description"] == "Build a test feature."
    fields = {field["name"]: field for field in feature["fields"]}
    assert fields["width"]["source"] == "docblock"
    assert fields["width"]["type"] == "ValueWithUnits"
    assert fields["width"]["required"] is False
    assert fields["entities"]["source"] == "precondition"
    assert fields["entities"]["label"] == "Entities"
    assert fields["entities"]["description"] == "Input faces"
    assert fields["depth"]["type"] == "ValueWithUnits"
    assert fields["mergeScope"]["source"] == "predicate"
    assert fields["mergeScope"]["predicate"] == "extraFields"


def test_bundled_stdlib_data_loads():
    from fs_lsp.stdlib import stdlib

    index = stdlib()
    assert "extrude" in index.features
    assert "BoundingType" in index.enum_names


# The checked-in std library


def test_std_library_parses_cleanly():
    """The Onshape std (std/) is a large real-world corpus: it should produce no diagnostics."""
    std = pathlib.Path(__file__).resolve().parents[3] / "std"
    files = sorted(std.glob("*.fs"))
    assert len(files) > 200
    for path in files:
        parsed = parse(path.read_text())
        assert [d.message for d in diagnostics(parsed)] == [], path.name
