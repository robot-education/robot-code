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
    (tmp_path / ".fs-state.json").write_text(
        json.dumps(
            {
                "version": 3,
                "studios": {
                    element_id: {"file": file, "hash": "", "microversion_id": ""}
                    for element_id, file in ids.items()
                },
            }
        )
    )
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
    }


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


def test_unknown_imports_need_state(project):
    project.state_path.unlink()
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
