"""Tests for cross-file analysis (fs_lsp.project) on a small repo."""

import json
import pathlib

import pytest

from fs_lsp.project import Project

STD = 'import(path : "onshape/std/common.fs", version : "2909.0");'
UTILS_ID = "a" * 24
SHAPES_ID = "b" * 24
REEXPORT_ID = "c" * 24

FILES = {
    "core/utils.fs": f"""FeatureScript 2909;
{STD}

/** Doubles a number. */
export function double(value is number) returns number
{{
    return value * 2;
}}

function hidden() {{}}
""",
    "core/shapes.fs": f"""FeatureScript 2909;
{STD}

export enum Shape
{{
    CIRCLE,
    SQUARE
}}

export operator+(a is Shape, b is Shape) {{ return a; }}
""",
    "core/reexport.fs": f"""FeatureScript 2909;
export import(path : "{UTILS_ID}", version : "v");
""",
    "feature.fs": f"""FeatureScript 2909;
{STD}
import(path : "{REEXPORT_ID}", version : "v");
import(path : "{SHAPES_ID}", version : "v");
import(path : "{"d" * 24}", version : "v");

export function run(context is Context)
{{
    const x = double(1);
    const shape = Shape.CIRCLE;
    for (var i, value in [1, 2])
    {{
        println(i ~ value ~ missing);
    }}
    try
    {{
        hidden();
    }}
    catch (error)
    {{
        throw error;
    }}
}}
""",
}


@pytest.fixture
def project(tmp_path) -> Project:
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    for path, code in FILES.items():
        file = tmp_path / "featurescripts" / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(code)
    ids = {UTILS_ID: "core/utils.fs", SHAPES_ID: "core/shapes.fs", REEXPORT_ID: "core/reexport.fs"}
    (tmp_path / "fs-studios.json").write_text(json.dumps({"version": 1, "studios": ids}))
    project = Project.find(tmp_path / "featurescripts" / "feature.fs")
    assert project is not None
    return project


def module(project: Project, path: str):
    return project.module(project.code_dir / path)


def offset_of(project: Project, path: str, text: str, skip: int = 0) -> int:
    source = module(project, path).parsed.source
    return source.index(text, source.index(text) + skip if skip else 0)


def test_problems(project):
    feature = module(project, "feature.fs")
    problems = {
        (problem.code, feature.parsed.source[problem.start : problem.end])
        for problem in project.check(feature)
    }
    assert problems == {
        # Loop and catch variables are declared; hidden isn't exported
        ("undefined", "missing"),
        ("undefined", "hidden"),
        ("unknown-import", f'"{"d" * 24}"'),
        # ... and set but never used
        ("unused-variable", "x"),
        ("unused-variable", "shape"),
    }


def test_std_names_need_their_module_visible(project):
    # ModifyFilletType is in modifyFillet.fs, which common.fs doesn't export; max's overloads are in math.fs, which it
    # does
    path = project.code_dir / "fillet.fs"
    body = "export function f() { return [ModifyFilletType.CHANGE_RADIUS, max(1, 2), max([1, 2])]; }\n"
    path.write_text(f"FeatureScript 2909;\n{STD}\n{body}")
    undefined = [p.message for p in project.check(project.module(path)) if p.code == "undefined"]
    assert undefined == ["ModifyFilletType isn't defined in this file or anything it imports."]
    path.write_text(f'FeatureScript 2909;\n{STD}\nimport(path : "onshape/std/modifyFillet.fs", version : "2909.0");\n{body}')
    assert not [p for p in project.check(project.module(path)) if p.code == "undefined"]


def test_unused_imports(project, tmp_path):
    feature = module(project, "feature.fs")
    (project.code_dir / "feature.fs").write_text(
        feature.parsed.source.replace("double(1)", "1")
    )
    problems = project.check(module(project, "feature.fs"))
    unused = [problem for problem in problems if problem.code == "unused-import"]
    assert [problem.message for problem in unused] == [
        "Nothing from core/reexport.fs is used."
    ]
    # Re-exports are never unused
    assert project.check(module(project, "core/reexport.fs")) == []


def test_unknown_imports_need_studios(project):
    project.studios_path.unlink()
    feature = module(project, "feature.fs")
    assert not any(p.code == "unknown-import" for p in project.check(feature))


def test_definitions_follow_reexports(project):
    feature = module(project, "feature.fs")
    [(owner, declaration)] = project.definitions(
        feature, offset_of(project, "feature.fs", "double")
    )
    assert (owner.relative, declaration.name) == ("core/utils.fs", "double")

    [(owner, member)] = project.definitions(
        feature, offset_of(project, "feature.fs", "CIRCLE")
    )
    assert (owner.relative, member.kind) == ("core/shapes.fs", "enumMember")


def test_references_across_files(project):
    utils = module(project, "core/utils.fs")
    references = project.references(utils, offset_of(project, "core/utils.fs", "double"))
    assert sorted(owner.relative for owner, _ in references) == [
        "core/utils.fs",
        "feature.fs",
    ]
    [(module_, declaration, references)] = project.references_to_name("double")
    assert [owner.relative for owner, _ in references] == ["feature.fs"]


def test_overlays(project):
    path = (project.code_dir / "feature.fs").resolve()
    project.overlays[path] = module(project, "feature.fs").parsed.source.replace(
        "missing", "1"
    )
    problems = project.check(module(project, "feature.fs"))
    assert not any(p.code == "undefined" and p.message.startswith("missing") for p in problems)


def test_importers(project):
    importers = project.importers(module(project, "core/utils.fs"))
    assert [m.relative for m in importers] == ["core/reexport.fs"]


def test_imports_by_path(project):
    path = project.code_dir / "byPath.fs"
    path.write_text(
        'FeatureScript 2909;\nimport(path : "core/utils.fs", version : "");\n'
        'import(path : "core/nope.fs", version : "");\nexport const a = double(1);\n'
    )
    by_path = project.module(path)
    problems = [(p.code, p.message) for p in project.check(by_path)]
    assert problems == [("unknown-import", "core/nope.fs doesn't exist in the code folder.")]
    [(owner, _)] = project.definitions(by_path, by_path.parsed.source.index("double"))
    assert owner.relative == "core/utils.fs"


def test_images_imported_by_path(project):
    (project.code_dir / "icon.svg").write_text("<svg/>")
    path = project.code_dir / "withIcon.fs"
    path.write_text(
        'FeatureScript 2909;\nexport Icon::import(path : "icon.svg", version : "");\n'
        'export Missing::import(path : "core/nope.svg", version : "");\n'
    )
    problems = [(p.code, p.message) for p in project.check(project.module(path))]
    assert problems == [("unknown-import", "core/nope.svg doesn't exist in the code folder.")]


def test_part_studios_of_other_documents(project):
    # Imported by document, version, and element id: nothing here can check them
    path = project.code_dir / "withPartStudio.fs"
    path.write_text(
        'FeatureScript 2909;\nexport Block::import(path : "5e3874e07384706ec3840340/5657eb187a0b8ed8fb95125a/'
        'c0895459c41bc1da9850fd7e", version : "4173ef57af8115dabb532b5d");\n'
    )
    assert project.check(project.module(path)) == []


def test_bare_map_keys(project):
    path = project.code_dir / "keys.fs"
    path.write_text(
        'FeatureScript 2909;\nconst KEY = "k";\n'
        'const a = { KEY : 1, (KEY) : 2, "KEY" : 3, other : 4 };\n'
    )
    problems = [p for p in project.check(project.module(path)) if p.code == "bare-key"]
    assert len(problems) == 1 and problems[0].message.startswith('This key is the string "KEY"')


def test_variables_are_visible_after_their_declaration(project):
    path = project.code_dir / "scope.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "function twice(x) { return x * 2; }\n"
        "export function run()\n{\n"
        "    if (true)\n    {\n    }\n"
        # After a block, and calling the function of the same name
        "    const twice = twice(1);\n"
        "    return twice;\n}\n"
    )
    module = project.module(path)
    assert project.check(module) == []
    source = module.parsed.source
    [(_, declaration)] = project.definitions(module, source.index("twice(1)"))
    assert declaration.kind == "function"
    [(_, declaration)] = project.definitions(module, source.index("return twice") + 7)
    assert declaration.kind == "variable"


def test_imported_names_are_highlighted(project):
    from fs_lsp.semantic import build_semantic_tokens

    (project.code_dir / "core" / "shapes.fs").write_text(
        'FeatureScript 2909;\nexport enum Shape { CIRCLE }\nexport const SIDES = 4;\n'
    )
    path = project.code_dir / "use.fs"
    path.write_text(
        f'FeatureScript 2909;\nimport(path : "{SHAPES_ID}", version : "v");\n'
        "export const a = [SIDES, Shape.CIRCLE];\n"
    )
    module = project.module(path)
    tokens = {
        token.token.value: (token.type, token.modifiers)
        for token in build_semantic_tokens(module.parsed, project.imported_names(module))
    }
    assert tokens["SIDES"] == ("variable", ("readonly",))
    assert tokens["Shape"][0] == "enum"
    assert tokens["CIRCLE"] == ("enumMember", ("readonly",))


def test_imports_and_namespaces_are_highlighted(project):
    from fs_lsp.semantic import build_semantic_tokens

    path = project.code_dir / "icons.fs"
    path.write_text(
        'FeatureScript 2909;\nIcon::import(path : "abc", version : "def");\n'
        "export const a = [Icon::BLOB_DATA, Icon::build];\n"
        "export function b() { try silent { return 1; } }\n"
    )
    module = project.module(path)
    tokens = {(t.token.value, t.token.line): t.type for t in build_semantic_tokens(module.parsed)}
    assert tokens[("path", 1)] == tokens[("version", 1)] == "property"
    assert tokens[("BLOB_DATA", 2)] == "variable" and tokens[("build", 2)] == "function"
    assert tokens[("silent", 3)] == "keyword"


def test_lints(project):
    path = project.code_dir / "lint.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'const SIZES = { "a" : true };\n'
        "predicate isBig(definition is map) { definition.size > 1; }\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        "        if (SIZES[definition.size]) { }\n"
        "        if (isBig(definition) && definition.flag) { }\n"
        "        for (var item in definition.items) { if (item.on) { } }\n"
        "    }\n    {\n"
        "        if (definition.flag == true) { }\n"
        "    });\n"
    )
    problems = [(p.code, module_text) for p in project.check(project.module(path))
                for module_text in [project.module(path).parsed.source[p.start:p.end]]]
    assert ("precondition", "SIZES") in problems
    assert not any(code == "precondition" and text != "SIZES" for code, text in problems)
    assert ("boolean-comparison", "== true") in problems


def test_parameter_enums_must_be_exported(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\nexport enum Shape { CIRCLE }\n"
        "export predicate shapePredicate(definition is map) { definition.other is Shape; }\n"
    )
    feature = (
        "FeatureScript 2909;\n{imports}"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {{\n        definition.shape is Shape;\n        shapePredicate(definition);\n    }}\n"
        "    {{\n    }});\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(feature.format(imports=f'import(path : "{SHAPES_ID}", version : "v");\n'))
    problems = [p for p in project.check(project.module(path)) if p.code == "unexported-parameter-enum"]
    source = project.module(path).parsed.source
    # Reported where it's used directly, and at the predicate call
    assert sorted(source[p.start:p.end] for p in problems) == ["Shape", "shapePredicate"]

    path.write_text(feature.format(imports=f'export import(path : "{SHAPES_ID}", version : "v");\n'))
    assert not [p for p in project.check(project.module(path)) if p.code == "unexported-parameter-enum"]


def test_duplicate_parameters(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export predicate offsetPredicate(definition is map) { definition.offset is boolean; }\n"
        "export predicate stockPredicate(settings is map, name is string)\n"
        "{\n    annotation { \"Name\" : name }\n    offsetPredicate(settings);\n}\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\nimport(path : "{SHAPES_ID}", version : "v");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        "        if (definition.edge)\n        {\n"
        "            isLength(definition.offset, LENGTH_BOUNDS);\n"
        "            definition.width is boolean;\n"
        "        }\n        else\n        {\n"
        '            stockPredicate(definition, "strip");\n'
        "        }\n"
        "        if (definition.offset is boolean) { }\n"
        "    }\n    {\n    });\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "duplicate-parameter"]
    # Reported at the predicate call which declares it again, even in the other branch of an if
    assert [source[p.start:p.end] for p in problems] == ["stockPredicate"]
    assert "offset" in problems[0].message and "line 8" in problems[0].message


def test_editing_logic_keys(project):
    # A helper in another file which sets keys (one set to undefined before returning), keys built from strings, and
    # maps which don't go back into the result
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export function withUnits(definition is map) returns map\n{\n"
        "    definition.units = 1;\n    definition.scratch = 2;\n    return definition;\n}\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\nimport(path : "{SHAPES_ID}", version : "v");\n'
        'annotation { "Feature Type Name" : "F", "Editing Logic Function" : "fEdit", '
        '"Manipulator Change Function" : "fManipulators" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        "        definition.depth is boolean;\n"
        "        definition.firstEndDiameter is boolean;\n"
        "    }\n    {\n    });\n"
        "function setWidth(settings is map) returns map\n{\n    settings.width = 1;\n    return settings;\n}\n"
        "function oldWidth(definition is map) returns map\n{\n    definition.unused = 1;\n    return definition;\n}\n"
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map) returns map\n{\n"
        "    definition.depth = true;\n"
        "    oldDefinition.ignored = 1;\n"
        '    definition["end" ~ "Diameter"] = 1;\n'
        '    definition["end" ~ "Radius"] = 1;\n'
        "    definition = withUnits(definition);\n"
        "    const other = oldWidth(definition);\n"
        "    definition.scratch = undefined;\n"
        "    return setWidth(definition);\n}\n"
        "export function fManipulators(context is Context, definition is map, newManipulators is map) returns map\n{\n"
        '    definition["length"] = 1;\n    return definition;\n}\n'
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code == "unknown-parameter"]
    reported = sorted((module.parsed.source[p.start : p.end], p.message.split(",")[0]) for p in problems)
    assert reported == [
        ('"Radius"', 'fEdit sets a key ending "Radius"'),
        # Set in another file: reported at the annotation naming the function
        ('"fEdit"', "fEdit sets units (in withUnits)"),
        ('"length"', "fManipulators sets length"),
        ("width", "fEdit sets width (in setWidth)"),
    ]
    assert all("Unknown parameter" in p.message for p in problems)


def set_by_feature(project, logic: str, set_by: str = "isWide", hint: str = ', "UIHint" : ["ALWAYS_HIDDEN"]'):
    path = project.code_dir / "setBy.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'annotation { "Feature Type Name" : "F", "Editing Logic Function" : "fEdit" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n        definition.size is boolean;\n"
        f'        annotation {{ "Name" : "Wide"{hint}, "Set By" : "{set_by}" }}\n'
        "        definition.wide is boolean;\n    }\n    {\n    });\n"
        "export function isWide(definition is map) returns boolean\n{\n    return definition.size;\n}\n"
        "export function wideCount(definition is map) returns number\n{\n    return 1;\n}\n"
        f"{logic}"
    )
    module = project.module(path)
    return module, [p for p in project.check(module) if p.code.startswith("set-by")]


def test_set_by(project):
    # Editing logic setting the hidden parameter from its function, first thing: nothing to report
    logic = (
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map\n"
        "{\n    definition.wide = isWide(definition);\n    return definition;\n}\n"
    )
    assert set_by_feature(project, logic)[1] == []
    # Unknown, or of the wrong type, and shown
    _, problems = set_by_feature(project, logic, "isNarrow")
    assert [p.message for p in problems] == ["wide is set by isNarrow, which isn't a function this file sees."]
    _, problems = set_by_feature(project, logic.replace("isWide(", "wideCount("), "wideCount", hint="")
    assert [p.message.split(",")[0] for p in problems] == [
        "wideCount must return boolean",
        "wide is set by wideCount",
    ]
    assert problems[1].severity == "warning"
    # Set too late (after a return), or in a branch, and without isCreating (so editing logic only runs on creation)
    late = (
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map) returns map\n"
        "{\n    if (oldDefinition == {})\n    {\n        return definition;\n    }\n"
        "    if (true)\n    {\n        definition.wide = isWide(definition);\n    }\n    return definition;\n}\n"
    )
    module, problems = set_by_feature(project, late)
    source = module.parsed.source
    assert [(source[p.start : p.end], p.code) for p in problems] == [("fEdit", "set-by"), ("fEdit", "set-by-missing")]
    assert problems[1].message == "fEdit must set wide first thing, before anything returns: definition.wide = isWide(definition);"


def test_set_by_fix(project):
    from fs_lsp.quick_fixes import apply_fixes, quick_fixes

    logic = (
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map\n"
        "{\n    return definition;\n}\n"
    )
    module, problems = set_by_feature(project, logic)
    fixed, count = apply_fixes(module.parsed.source, quick_fixes(project, module, problems))
    assert count == 1
    assert fixed.endswith("{\n    definition.wide = isWide(definition);\n    return definition;\n}\n")


def test_buttons(project):
    from fs_lsp.quick_fixes import apply_fixes, quick_fixes

    path = project.code_dir / "buttons.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'annotation { "Feature Type Name" : "F", "Editing Logic Function" : "fEdit" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Reset" }\n        isButton(definition.reset);\n'
        '        annotation { "Name" : "Recalculate" }\n        isButton(definition.recalculate);\n'
        '        annotation { "Name" : "Unused" }\n        isButton(definition.unused);\n'
        "    }\n    {\n        if (definition.reset == undefined) { }\n    });\n"
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,\n"
        "    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map\n{\n"
        '    if (clickedButton == "reset")\n    {\n        definition.recalculate = true;\n    }\n'
        "    return clicked(definition, clickedButton);\n}\n"
        "function clicked(definition is map, button is string) returns map\n{\n"
        '    if ("recalclate" == button)\n    {\n        return definition;\n    }\n    return definition;\n}\n'
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code.startswith("button")]
    source = module.parsed.source
    line = lambda p: module.position(p.start)[0]  # noqa: E731
    assert sorted((line(p), source[p.start : p.end], p.code, p.severity) for p in problems) == [
        # Misspelled where clickedButton's passed on, so Recalculate does nothing; Unused does nothing; and buttons
        # have no value, read in the body or set by editing logic
        (8, "recalculate", "button", "warning"),
        (10, "unused", "button", "warning"),
        (13, "reset", "button-value", "error"),
        (20, "recalculate", "button-value", "error"),
        (26, '"recalclate"', "button", "error"),
    ]
    misspelled = next(p for p in problems if p.severity == "error" and p.code == "button")
    assert misspelled.message == (
        'recalclate isn\'t a button of f, so clickedButton is never "recalclate". Did you mean "recalculate"?'
    )
    fixed, count = apply_fixes(source, quick_fixes(project, module, [misspelled]))
    assert count == 1 and '"recalculate" == button' in fixed


def test_unknown_definition_keys(project):
    # Reads of keys a feature's definition can't have: not parameters, nor set anywhere it reaches. A helper two
    # features share may read a parameter only one has
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export function shared(definition is map)\n{\n    return definition.depth;\n}\n"
        "export function withUnits(definition is map) returns map\n{\n"
        '    definition.units = 1;\n    return mergeMaps(definition, { "scale" : 2 });\n}\n'
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\nimport(path : "{SHAPES_ID}", version : "v");\n'
        'annotation { "Feature Type Name" : "F" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n        definition.depth is boolean;\n        if (definition.dpeth) { }\n    }\n"
        "    {\n        definition = withUnits(definition);\n        shared(definition);\n"
        '        definition.offset = 1;\n        definition["angle"] = 2;\n'
        "        const a = definition.units + definition.offset + definition.angle + definition?.optional;\n"
        "        const b = other.missing + definition.missing;\n    });\n"
        'annotation { "Feature Type Name" : "G" }\n'
        "export const g = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n        definition.width is boolean;\n    }\n    {\n        shared(definition);\n    });\n"
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code == "unknown-key"]
    source = module.parsed.source
    assert [source[p.start : p.end] for p in problems] == ["dpeth", "missing"]
    assert problems[0].message == (
        "dpeth isn't a parameter of f, or set on its definition anywhere it reaches: did you mean depth?"
    )
    assert source.index("definition.missing") + len("definition.") == problems[1].start
    shapes = project.module(project.code_dir / "core" / "shapes.fs")
    assert [p for p in project.check(shapes) if p.code == "unknown-key"] == []


def test_tolerant_parameters(project):
    # A copy of std's source, where std's own predicates are read from
    std = project.root / "std"
    std.mkdir()
    (std / "extrudeCommon.fs").write_text(
        "FeatureScript 2909;\n"
        "export predicate extrudeBoundParametersPredicate(definition is map)\n{\n"
        '    annotation { "Name" : "Depth", "UIHint" : UIHint.CAN_BE_TOLERANT }\n'
        "    isLength(definition.depth, LENGTH_BOUNDS);\n}\n"
    )
    project.std_dir = std
    path = project.code_dir / "feature2.fs"
    path.write_text(
        'FeatureScript 2909;\nimport(path : "onshape/std/extrudeCommon.fs", version : "1.0");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "CAN_BE_TOLERANT"] }\n'
        "        isLength(definition.width, LENGTH_BOUNDS);\n"
        "        extrudeBoundParametersPredicate(definition);\n"
        "    }\n    {\n    });\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "tolerant-parameter"]
    # The hint itself, and the call to a std predicate declaring a tolerant parameter
    assert [source[p.start:p.end] for p in problems] == ['"CAN_BE_TOLERANT"', "extrudeBoundParametersPredicate"]


def test_nested_predicates_in_conditions(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "predicate isCustom(definition is map) { definition.source == 1; }\n"
        "predicate isCustomTube(definition is map) { isCustom(definition); definition.profile == 2; }\n"
        "predicate tubePredicate(definition is map) { if (isCustomTube(definition)) { definition.wall is boolean; } }\n"
        "predicate isItem(value is map) { isCustom(value); }\n"
        "predicate canBeItem(value) { if (isItem(value)) { value.size is number; } }\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        "        if (isCustom(definition) || isCustomTube(definition)) { }\n"
        "        tubePredicate(definition);\n"
        "    }\n    {\n"
        "        if (isCustomTube(definition)) { }\n"
        "    });\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "nested-predicate"]
    # In the precondition and the predicate it calls, but not in the feature's body or a type predicate
    assert [(source[p.start:p.end], module(project, "feature2.fs").position(p.start)[0]) for p in problems] == [
        ("isCustomTube", 3),
        ("isCustomTube", 9),
    ]
    assert "isCustom" in problems[0].message


def test_feature_annotation_must_be_right_before_define_feature(project):
    path = project.code_dir / "feature3.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'annotation { "Feature Type Name" : "Good" }\n'
        "export const good = defineFeature(function(context is Context, id is Id, definition is map) {});\n"
        'annotation { "Feature Type Name" : "Bad" }\n'
        "// A comment is fine, but a declaration takes the annotation\n"
        "const BOUNDS = 1;\n"
        "export const bad = defineFeature(function(context is Context, id is Id, definition is map) {});\n"
    )
    problems = [p for p in project.check(project.module(path)) if p.code == "feature-annotation"]
    assert [module(project, "feature3.fs").position(p.start)[0] for p in problems] == [3]
    assert "BOUNDS" in problems[0].message


def test_parameter_fields_in_preconditions(project):
    path = project.code_dir / "feature4.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "predicate hasGrooves(definition is map) { definition.shaft[\"type\"] == \"e-clip\"; }\n"
        "predicate isPreset(definition is map) { definition.preset; }\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        "        if (definition.shaft.length != undefined || hasGrooves(definition) || isPreset(definition)) { }\n"
        "    }\n    {\n"
        "        if (definition.shaft.length != undefined) { }\n"
        "    });\n"
    )
    problems = [p for p in project.check(project.module(path)) if p.code == "parameter-field"]
    # In the predicate and the precondition, but not the feature's body
    assert [module(project, "feature4.fs").position(p.start)[0] for p in problems] == [1, 6]


def test_duplicate_top_level_symbols(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\nexport const SIZE = 1;\nexport function area(x is number) returns number { return x; }\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\n{STD}\nimport(path : "{SHAPES_ID}", version : "v");\n'
        # Std constants visible through common.fs, and the project's constants, can't be redeclared
        "const LENGTH_BOUNDS = 1;\n"
        "const SIZE = 2;\n"
        "const WIDTH = 1;\n"
        "const WIDTH = 2;\n"
        # Overloads, std names common.fs doesn't export, and std enum members are fine
        "function area(x is string) returns string { return x; }\n"
        "enum TransformType { ONE }\n"
        "const BLACK = 3;\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "duplicate-symbol"]
    assert [(source[p.start:p.end], p.message.split(": ")[1].split(" already")[0]) for p in problems] == [
        ("LENGTH_BOUNDS", "the std library (valueBounds.fs)"),
        ("SIZE", "core/shapes.fs"),
        ("WIDTH", "this file (line 6)"),
    ]


def test_groups_in_array_items(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f"FeatureScript 2909;\n{STD}\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Group Name" : "Outside" }\n        {\n            definition.a is boolean;\n        }\n'
        "        definition.items is array;\n"
        "        for (var item in definition.items)\n        {\n"
        '            annotation { "Group Name" : "Inside" }\n            {\n                item.b is boolean;\n            }\n'
        "        }\n"
        "    }\n    {\n    });\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "array-group"]
    assert [(source[p.start : p.end], project.module(path).position(p.start)[0]) for p in problems] == [
        ('"Group Name"', 12)
    ]


def test_duplicate_overloads(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export function area(x is number) returns number { return x; }\n"
        "export predicate sizePredicate(definition is map) { definition.size is boolean; }\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\n{STD}\nimport(path : "{SHAPES_ID}", version : "v");\n'
        # Overloads with the same parameter types as the project's, or each other, can't be told apart
        "predicate sizePredicate(definition is map) { definition.size is boolean; }\n"
        "function volume(x is number, y) returns number { return x; }\n"
        "function volume(x is number, y) returns number { return x; }\n"
        # Different parameter types are fine
        "function area(x is string) returns string { return x; }\n"
        "function volume(x is number) returns number { return x; }\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "duplicate-overload"]
    assert [(source[p.start:p.end], p.message.split(": ")[0], p.message.split(": ")[1].split(" already")[0]) for p in problems] == [
        ("sizePredicate", "Duplicate predicate sizePredicate(map)", "core/shapes.fs"),
        ("volume", "Duplicate function volume(number, )", "this file (line 5)"),
    ]


def test_std_parameter_enums_must_be_exported(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        'export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2909.0");\n'
    )
    feature = (
        "FeatureScript 2909;\n" + STD + "\n{imports}"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {{\n        definition.axis is MateConnectorAxisType;\n    }}\n"
        "    {{\n    }});\n"
    )
    path = project.code_dir / "feature2.fs"

    def problems(imports: str) -> list[str]:
        path.write_text(feature.format(imports=imports))
        return sorted(p.code for p in project.check(project.module(path)) if p.code != "unused-import")

    assert problems("") == ["unexported-parameter-enum"]
    assert problems('export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2909.0");\n') == []
    # Through a project file which re-exports the std module
    assert problems(f'export import(path : "{SHAPES_ID}", version : "v");\n') == []
    # Exporting common.fs exports the enum, but everything else in std too
    assert problems('export import(path : "onshape/std/common.fs", version : "2909.0");\n') == ["exported-common"]


def test_annotation_strings_resolve(project, tmp_path):
    (project.code_dir / "core" / "utils.fs").write_text(
        "FeatureScript 2909;\nexport function sharedChange(context is Context, definition is map, newManipulators is map) returns map\n"
        "{\n    return definition;\n}\n"
    )
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f'FeatureScript 2909;\n{STD}\nimport(path : "{UTILS_ID}", version : "v");\n'
        'annotation { "Feature Type Name" : "F", "Manipulator Change Function" : "localChange",\n'
        '        "Editing Logic Function" : "sharedChange" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        '    precondition\n    {\n        annotation { "Name" : "Flag", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }\n'
        "        definition.flag is boolean;\n    }\n    {\n    });\n"
        "export function localChange(context is Context, definition is map, newManipulators is map) returns map\n"
        "{\n    return definition;\n}\n"
    )
    (tmp_path / "std").mkdir()
    (tmp_path / "std" / "uihint.gen.fs").write_text(
        "FeatureScript 2909;\nexport enum UIHint\n{\n    REMEMBER_PREVIOUS_VALUE,\n    SHOW_LABEL\n}\n"
    )
    module = project.module(path)
    source = module.parsed.source

    def definitions(text: str) -> list[tuple[str, str]]:
        return [(owner.relative, declaration.name) for owner, declaration in project.definitions(module, source.index(text) + 2)]

    assert definitions('"localChange"') == [("feature2.fs", "localChange")]
    assert definitions('"sharedChange"') == [("core/utils.fs", "sharedChange")]
    # Other strings in annotations aren't names
    assert definitions('"F"') == []

    [(owner, member)] = project.std_definitions(module, source.index('"SHOW_LABEL"') + 2)
    assert owner.path.name == "uihint.gen.fs" and owner.position(member.token.offset) == (4, 4)
    assert project.std_definitions(module, source.index('"Flag"') + 2) == []


def test_functions_used_as_values(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "function name(k is number) returns string { return '' ~ k; }\n"
        "predicate canBeThing(value) { value is map; }\n"
        "export type Thing typecheck canBeThing;\n"
        "const named = function(k) { return name(k); };\n"
        "export function run() returns array\n"
        "{\n"
        "    const a = mapArray([1, 2], name);\n"
        "    const b = mapArray([1, 2], named);\n"
        "    const c = mapArray([1, 2], function(k) { return name(k); });\n"
        "    return concatenateArrays([a, b, c]);\n"
        "}\n"
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code == "function-value"]
    # Only `name` passed as a value: not calls, function values, or the type's typecheck predicate
    assert [(module.parsed.source[p.start:p.end], module.position(p.start)[0]) for p in problems] == [("name", 7)]


def test_std_calls_values_and_redeclarations(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'import(path : "onshape/std/common.fs", version : "2909.0");\n'
        "export function run(context is Context, points is array, ends is Query, plane is Plane) returns array\n"
        "{\n"
        "    const lengths = mapArray(points, norm);\n"
        "    const kept = mapArray(points, point => norm(point));\n"
        "    const world = planeToWorld3D(plane, vector(0, 0, 0) * meter);\n"
        "    const piped = ends->qUnion(ends);\n"
        "    var ends = [];\n"
        "    return filter(kept, ends => ends > 0);\n"
        "}\n"
        'annotation { "Feature Type Name" : "F" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n"
        "    {\n"
        '        annotation { "Name" : "Angle", "Description" : "In degrees: 20\u00b0." }\n'
        "        definition.flag is boolean;\n"
        "    }\n"
        "    {\n"
        "    });\n",
        encoding="utf-8",
    )
    module = project.module(path)

    def found(code: str) -> list[tuple[str, int]]:
        return [
            (module.parsed.source[p.start:p.end], module.position(p.start)[0])
            for p in project.check(module)
            if p.code == code
        ]

    # A std function passed as a value; not one called in a lambda
    assert found("function-value") == [("norm", 4)]
    # planeToWorld3D takes just a plane; `a->f(b)` passes a too, so qUnion has two
    assert found("std-arity") == [("planeToWorld3D", 6)]
    # A variable named as a parameter; not a lambda's parameter
    assert found("redeclared") == [("ends", 8)]
    assert [line for _, line in found("annotation-ascii")] == [15]


def test_write_only_variables_and_unused_std_imports(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'import(path : "onshape/std/common.fs", version : "2909.0");\n'
        'import(path : "onshape/std/modifyFillet.fs", version : "2909.0");\n'
        'import(path : "onshape/std/chamfer.fs", version : "2909.0");\n'
        "export function run(items is array) returns array\n"
        "{\n"
        "    var ids = [];\n"
        "    var count = 0;\n"
        "    var kept = [];\n"
        "    for (var item in items)\n"
        "    {\n"
        "        ids = append(ids, item);\n"
        "        count += 1;\n"
        "        kept = append(kept, item);\n"
        "    }\n"
        "    const kind = ModifyFilletType.CHANGE_RADIUS;\n"
        "    const type = ChamferType.EQUAL_OFFSETS;\n"
        "    return [kept, kind, type];\n"
        "}\n"
    )
    module = project.module(path)
    problems = project.check(module)

    def found(code: str) -> list[tuple[str, int]]:
        return [(module.parsed.source[p.start:p.end].split("\n")[0], module.position(p.start)[0]) for p in problems if p.code == code]

    # Only ever set from themselves; not one returned
    assert found("unused-variable") == [("ids", 6), ("count", 7)]
    # modifyFillet.fs gives ModifyFilletType, which common.fs doesn't; chamfer.fs gives ChamferType, which it does
    assert [line for _, line in found("unused-import")] == [3]


def test_snapshots_see_files_once(project):
    new = project.code_dir / "new.fs"
    with project.snapshot():
        before = project.files()
        assert project.module(new) is None
        new.write_text("FeatureScript 2909;\nexport const n = 1;\n")
        # Inside a snapshot, what's been looked up stays as it was
        assert project.files() == before and project.module(new) is None
    # Afterwards, changes are seen again
    assert new.resolve() in project.files()
    assert project.module(new) is not None


def test_keyword_keys(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f"FeatureScript 2909;\n{STD}\n"
        "export function f(bound is map) returns map\n"
        "{\n"
        '    const a = { "type" : 1, "kind" : 2 };\n'
        '    // fs check: ignore keyword-key (std needs it)\n'
        '    const b = { "type" : 1 };\n'
        '    return { "c" : bound.type + bound["type"] + a.kind + b["type"] };\n'
        "}\n"
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code == "keyword-key"]
    assert [(module.position(p.start)[0], p.severity, module.parsed.source[p.start : p.end]) for p in problems] == [
        # A keyword as a map key, unless ignored
        (4, "warning", '"type"'),
        # After a ".", which Onshape can't parse
        (7, "error", "type"),
    ]

    # Generated files need keys like "default"
    generated = project.code_dir / "tables.gen.fs"
    generated.write_text(f'FeatureScript 2909;\n{STD}\nexport const t = {{ "default" : "a" }};\n')
    assert [p for p in project.check(project.module(generated)) if p.code == "keyword-key"] == []


REPO_STD = pathlib.Path(__file__).parents[3] / "std"


@pytest.mark.skipif(not REPO_STD.is_dir(), reason="needs the copy of std")
def test_std_navigation(project):
    """Std's declarations are found in its source, parsed as they're needed, and std's files navigate like ours."""
    project.std_dir = REPO_STD
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f"FeatureScript 2909;\n{STD}\n"
        f'import(path : "{UTILS_ID}", version : "v");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Faces", "Filter" : EntityType.FACE, "UIHint" : ["ALWAYS_HIDDEN"] }\n'
        "        definition.faces is Query;\n"
        "    }\n    {\n"
        '        opDeleteBodies(context, id + "delete", { "entities" : qCreatedBy(id, EntityType.BODY) });\n'
        "        double(1);\n"
        "    });\n"
    )
    feature = project.module(path)

    def definitions(text: str, skip: int = 0) -> list[tuple[str, str, str]]:
        offset = offset_of(project, "feature2.fs", text, skip)
        found = project.definitions(feature, offset) or project.std_definitions(feature, offset)
        return [(owner.relative, declaration.name, declaration.kind) for owner, declaration in found]

    assert definitions("Context") == [("onshape/std/context.fs", "Context", "type")]
    assert definitions("opDeleteBodies") == [("onshape/std/geomOperations.fs", "opDeleteBodies", "variable")]
    assert definitions("FACE") == [("onshape/std/query.fs", "FACE", "enumMember")]
    assert definitions("BODY") == [("onshape/std/query.fs", "BODY", "enumMember")]
    assert definitions("ALWAYS_HIDDEN") == [("onshape/std/uihint.gen.fs", "ALWAYS_HIDDEN", "enumMember")]
    # Ours come first
    assert definitions("double") == [("core/utils.fs", "double", "function")]
    # Keys and the like aren't references
    assert definitions('"entities"') == []

    # Every use in the project, and std's own
    references = project.references(feature, offset_of(project, "feature2.fs", "opDeleteBodies"))
    assert ("feature2.fs", "opDeleteBodies") in {(owner.relative, token.value) for owner, token in references}
    assert "onshape/std/geomOperations.fs" in {owner.relative for owner, _ in references}

    # Std's files see each other, and aren't the project's
    extrude = project.std_module("onshape/std/extrude.fs")
    assert extrude is not None and extrude.relative == "onshape/std/extrude.fs"
    offset = extrude.parsed.source.index("opExtrude(")
    assert [owner.relative for owner, _ in project.std_definitions(extrude, offset)] == ["onshape/std/geomOperations.fs"]
    assert extrude not in project.modules()


def test_unused_variables(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f"FeatureScript 2909;\n{STD}\n"
        "export const TOP = 1;\n"
        "export function f(rows is map, unusedParameter) returns number\n"
        "{\n"
        "    const unused = 1;\n"
        "    var onlySet = 1;\n"
        "    onlySet = 2;\n"
        "    var total = 0;\n"
        "    for (var key, row in rows)\n"
        "    {\n"
        "        total += row;\n"
        "    }\n"
        "    for (var _, row in rows)\n"
        "    {\n"
        "        total += row;\n"
        "    }\n"
        "    return total;\n"
        "}\n"
    )
    module = project.module(path)
    problems = [p for p in project.check(module) if p.code == "unused-variable"]
    # Not top-level constants, parameters, or _
    assert [module.parsed.source[p.start : p.end] for p in problems] == ["unused", "onlySet", "key"]


def test_horizontal_enums_go_at_the_top(project):
    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export enum Mode { A, B }\n"
        'export predicate modePredicate(definition is map)\n{\n    annotation { "Name" : "Mode", "UIHint" : ["HORIZONTAL_ENUM"] }\n'
        "    definition.mode is Mode;\n}\n"
    )
    feature = (
        f'FeatureScript 2909;\nexport import(path : "{SHAPES_ID}", version : "v");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {{\n{body}    }}\n    {{\n    }});\n"
    )
    hidden = '        annotation { "Name" : "Hidden", "UIHint" : "ALWAYS_HIDDEN" }\n        definition.hidden is boolean;\n'
    shown = '        annotation { "Name" : "Shown" }\n        definition.shown is boolean;\n'
    horizontal = '        annotation { "Name" : "Other", "UIHint" : ["HORIZONTAL_ENUM"] }\n        definition.other is Mode;\n'
    path = project.code_dir / "feature2.fs"

    def problems(body: str) -> list[str]:
        path.write_text(feature.format(body=body))
        source = project.module(path).parsed.source
        return [source[p.start:p.end] for p in project.check(project.module(path)) if p.code == "horizontal-enum-order"]

    # At the top (after hidden parameters, and through predicates), they're fine
    assert problems(hidden + "        modePredicate(definition);\n" + horizontal + shown) == []
    # After anything shown, they're reported, at the predicate call for one a predicate declares
    assert problems(shown + horizontal + "        modePredicate(definition);\n") == ["other", "modePredicate"]


def test_horizontal_enums_go_in_no_groups_or_arrays(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        'FeatureScript 2909;\nexport enum Mode { A, B }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Group Name" : "Options" }\n        {\n'
        '            annotation { "Name" : "In a group", "UIHint" : ["HORIZONTAL_ENUM"] }\n'
        "            definition.grouped is Mode;\n        }\n"
        '        annotation { "Name" : "Items", "Item name" : "item" }\n        definition.items is array;\n'
        "        for (var item in definition.items)\n        {\n"
        '            annotation { "Name" : "In an item", "UIHint" : ["HORIZONTAL_ENUM"] }\n'
        "            item.mode is Mode;\n        }\n"
        "    }\n    {\n    });\n"
    )
    messages = [p.message for p in project.check(project.module(path)) if p.code == "horizontal-enum-order"]
    assert sorted(messages) == [
        "Horizontal enums go at the top of the dialog, not in array parameters' items.",
        "Horizontal enums go at the top of the dialog, not in groups.",
    ]


def test_array_items_parameters_are_feature_parameters(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Edges" }\n        definition.edges is Query;\n'
        '        annotation { "Name" : "Items", "Item name" : "item" }\n        definition.items is array;\n'
        "        for (var item in definition.items)\n        {\n"
        '            annotation { "Name" : "Edges" }\n            item.edges is Query;\n'
        '            annotation { "Name" : "Width" }\n            isLength(item.width, LENGTH_BOUNDS);\n'
        "        }\n"
        "    }\n    {\n    });\n"
    )
    source = project.module(path).parsed.source
    problems = [p for p in project.check(project.module(path)) if p.code == "duplicate-parameter"]
    # An item's parameter can't share a name with any other parameter
    assert [source[p.start:p.end] for p in problems] == ["edges"]
    assert "line 6" in problems[0].message


def test_unknown_annotation_keys(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Items", "Item name" : "item", "Driving query" : "edges" }\n'
        "        definition.items is array;\n"
        "        for (var item in definition.items)\n        {\n"
        '            annotation { "Name" : "Edges", "UIHInt" : ["ALWAYS_HIDDEN"], "Filter" : { "x" : 1 } }\n'
        "            item.edges is Query;\n        }\n"
        "    }\n    {\n    });\n"
    )
    problems = [p for p in project.check(project.module(path)) if p.code == "annotation-key"]
    # Keys of maps inside an annotation (like "x") aren't its keys
    assert [p.message for p in problems] == [
        'Onshape doesn\'t know the annotation key "Driving query", so it does nothing. Did you mean "Driven query"?',
        'Onshape doesn\'t know the annotation key "UIHInt", so it does nothing. Did you mean "UIHint"?',
    ]


def test_quick_fixes(project):
    from fs_lsp.quick_fixes import apply_fixes, quick_fixes

    (project.code_dir / "core" / "unused.fs").write_text(f"FeatureScript 2909;\n{STD}\nexport function nothing() {{}}\n")
    path = project.code_dir / "feature2.fs"
    path.write_text(
        f"FeatureScript 2909;\n{STD}\n"
        'import(path : "core/unused.fs", version : "");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width", "UIHInt" : ["REMEMBER_PREVIOUS_VALUE"] }\n'
        "        definition.width is boolean;\n"
        "    }\n    {\n        println(double(1) ~ double(2));\n    });\n"
    )
    module = project.module(path)
    fixes = quick_fixes(project, module, project.check(module))
    assert sorted(fix.title for fix in fixes) == [
        'Change to "UIHint"',
        "Import core/utils.fs",
        "Import core/utils.fs",
        "Remove the unused import",
    ]
    fixed, count = apply_fixes(module.parsed.source, fixes)
    # The second use's import is the same fix, so it's applied once
    assert count == 3
    assert '"UIHint" : ["REMEMBER_PREVIOUS_VALUE"]' in fixed
    assert "core/unused.fs" not in fixed
    assert fixed.count('import(path : "core/utils.fs", version : "");') == 1
    path.write_text(fixed)
    module = project.module(path)
    assert [p.code for p in project.check(module)] == []


def test_project_completions(project):
    from fs_lsp.project_completion import annotation_completions, project_completions

    path = project.code_dir / "feature2.fs"
    source = (
        f"FeatureScript 2909;\n{STD}\n"
        f'import(path : "{SHAPES_ID}", version : "v");\n'
        f'import(path : "{UTILS_ID}", version : "v");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width" }\n'
        "        definition.width is boolean;\n"
        "    }\n    {\n        const shape = Shape.SQUARE;\n        const w = definition.width;\n        dou(1);\n    });\n"
    )
    path.write_text(source)
    module = project.module(path)

    def labels(marker: str, after: int) -> list[str]:
        items = project_completions(project, module, source, source.index(marker) + after)
        return [item.label for item in items or []]

    # An imported enum's members, a feature's parameters, and names (from imports and std)
    assert labels("Shape.SQUARE", len("Shape.")) == ["CIRCLE", "SQUARE"]
    assert labels("definition.width;", len("definition.")) == ["width"]
    names = labels("dou(1)", 3)
    assert "double" in names and "opExtrude" in names and "shape" in names and "hidden" not in names
    # Std's internal names too, marked as such
    internal = next(item for item in project_completions(project, module, source, source.index("dou(1)") + 3) if item.label == "opExtractSurface")
    assert internal.detail.endswith(", internal)")
    keys = annotation_completions('annotation { "Name" : "x", "Dri', len('annotation { "Name" : "x", "Dri'))
    assert "Driven query" in [item.label for item in keys]


def test_definition_completions_and_hover(project):
    # Each feature's own parameters where its definition reaches: its body, its editing logic, a helper it passes the
    # definition to (in another file), and a predicate its precondition calls; with their types and names
    from fs_lsp.project_completion import definition_hover, project_completions

    (project.code_dir / "core" / "shapes.fs").write_text(
        "FeatureScript 2909;\n"
        "export function helper(definition is map) returns map\n{\n    return definition.depth;\n}\n"
        "export function unused(definition is map) returns map\n{\n    return definition.depth;\n}\n"
    )
    path = project.code_dir / "feature2.fs"
    source = (
        f'FeatureScript 2909;\nimport(path : "{SHAPES_ID}", version : "v");\n'
        "predicate depthPredicate(definition is map)\n{\n"
        '    if (definition.width)\n    {\n        annotation { "Name" : "Hidden", "UIHint" : ["ALWAYS_HIDDEN"] }\n'
        "        definition.hidden is boolean;\n    }\n"
        '    annotation { "Name" : "Depth" }\n    isLength(definition.depth, LENGTH_BOUNDS);\n}\n'
        'annotation { "Feature Type Name" : "F", "Editing Logic Function" : "fEdit" }\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width" }\n        definition.width is boolean;\n'
        "        depthPredicate(definition);\n"
        "    }\n    {\n        helper(definition);\n    });\n"
        'annotation { "Feature Type Name" : "G" }\n'
        "export const g = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n        definition.count is boolean;\n    }\n    {\n        definition.count;\n    });\n"
        "export function fEdit(context is Context, id is Id, oldDefinition is map, definition is map) returns map\n{\n"
        "    definition.width = true;\n    return definition;\n}\n"
    )
    path.write_text(source)
    module = project.module(path)

    def details(marker: str) -> list[tuple[str, str]]:
        items = project_completions(project, module, source, source.index(marker) + len("definition."))
        return [(item.label, item.detail) for item in items or []]

    f = [("width", "boolean - Width"), ("hidden", "boolean - Hidden"), ("depth", "length - Depth")]
    assert details("definition.width = true") == f
    assert details("definition.width)") == f
    assert details("definition.count;\n    });") == [("count", "boolean")]
    shapes = project.module(project.code_dir / "core" / "shapes.fs")
    helper = shapes.parsed.source
    assert [item.label for item in project_completions(project, shapes, helper, helper.index("definition.depth") + 11)] == [
        "width",
        "hidden",
        "depth",
    ]
    # Nothing passes unused a feature's definition
    assert project_completions(project, shapes, helper, helper.rindex("definition.depth") + 11) is None

    hover = definition_hover(project, module, source.index("definition.depth, LENGTH") + len("definition.d"))
    assert hover is not None
    assert "isLength(definition.depth, ...)" in hover and "**Depth**" in hover and "Parameter of `f`" in hover
    hidden = definition_hover(project, module, source.index("definition.hidden") + len("definition.h"))
    assert hidden is not None and "ALWAYS_HIDDEN" in hidden
    assert definition_hover(project, module, source.index("helper(definition)") + 2) is None


def test_rename_edits(project):
    from fs_lsp.project import RenameError

    feature = project.module(project.code_dir / "feature.fs")
    source = feature.parsed.source
    edits = project.rename_edits(feature, source.index("double(1)"), "twice")
    # Its declaration, and its use through the re-exporting file
    assert sorted(path.name for path in edits) == ["feature.fs", "utils.fs"]
    for bad, message in [("1x", "isn't a valid name"), ("hidden", "already declares")]:
        with pytest.raises(RenameError, match=message):
            project.rename_edits(feature, source.index("double(1)"), bad)
    with pytest.raises(RenameError, match="std's"):
        project.rename_edits(feature, source.index("println"), "say")


def test_operations_under_an_id_must_be_contiguous(project):
    path = project.code_dir / "feature2.fs"
    path.write_text(
        "FeatureScript 2909;\n"
        'import(path : "onshape/std/common.fs", version : "2909.0");\n'
        "function piece(context is Context, id is Id)\n"
        "{\n"
        '    opExtrude(context, id + "extrude", {});\n'
        "}\n"
        "function tag(context is Context, id is Id)\n"
        "{\n"
        "}\n"
        "export function run(context is Context, id is Id, count is number)\n"
        "{\n"
        '    const cellId = id + "cell";\n'
        "    piece(context, cellId);\n"
        '    piece(context, id + "start");\n'
        '    opBoolean(context, cellId + "join", {});\n'
        "    for (var i = 0; i < count; i += 1)\n"
        "    {\n"
        '        opPattern(context, id + "copies" + ("c" ~ i), {});\n'
        '        opBoolean(context, id + "joins" + ("j" ~ i), {});\n'
        "    }\n"
        '    tag(context, id + "start");\n'
        "    reportFeatureInfo(context, id, \"\");\n"
        "    if (count == 0)\n"
        "    {\n"
        '        opBoolean(context, id + "end", {});\n'
        '        throw regenError("");\n'
        "    }\n"
        '    opBoolean(context, id + "other", {});\n'
        '    opBoolean(context, id + "end", {});\n'
        "}\n"
    )
    module = project.module(path)
    found = [
        (module.parsed.source[p.start:p.end], module.position(p.start)[0])
        for p in project.check(module)
        if p.code == "id-order"
    ]
    # "cell" resumes after "start", through its alias; a loop's body repeats, so "copies" and "joins" each resume after
    # the other. Not `tag` (it makes no operations), the id alone, or "end" after a branch which throws
    assert found == [("cellId", 14), ("id", 17), ("id", 18)]
