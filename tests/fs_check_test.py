"""Tests for the offline analysis commands: fs check, fs format, fs deps, and fs refs."""

import json

import pytest

from fs_cli import cli

UTILS_ID = "a" * 24


@pytest.fixture
def repo(tmp_path, monkeypatch):
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    code_dir = tmp_path / "featurescripts"
    (code_dir / "core").mkdir(parents=True)
    (code_dir / "core" / "utils.fs").write_text(
        "FeatureScript 1;\nexport function double(x is number) { return x * 2; }\n"
    )
    (code_dir / "feature.fs").write_text(
        f'FeatureScript 1;\nimport(path : "{UTILS_ID}", version : "v");\nexport const a = double(1);\n'
    )
    (tmp_path / "fs-studios.json").write_text(
        json.dumps({"version": 1, "studios": {UTILS_ID: "core/utils.fs"}})
    )
    monkeypatch.chdir(tmp_path)
    monkeypatch.delenv("API_ACCESS_KEY", raising=False)
    return tmp_path


def test_check(repo, capsys):
    assert cli.main(["check"]) == 0
    assert "No problems found." in capsys.readouterr().out

    (repo / "featurescripts" / "feature.fs").write_text(
        f'FeatureScript 1;\nimport(path : "{UTILS_ID}", version : "v");\nexport const a = triple(1);\n'
    )
    assert cli.main(["check", "feature"]) == 1
    out = capsys.readouterr().out
    assert "featurescripts/feature.fs:3:18: error: triple isn't defined" in out
    assert "featurescripts/feature.fs:2:1: warning: Nothing from core/utils.fs is used." in out
    assert "1 error and 1 warning in 1 file." in out

    # Targets can be folders
    assert cli.main(["check", "featurescripts/core"]) == 0
    assert cli.main(["check", "nothing"]) == 2


def test_local_files_are_only_checked_when_named(repo, capsys):
    example = repo / "featurescripts" / "core" / "example.local.fs"
    example.write_text("FeatureScript 1;\nexport const b = triple(1);\n")
    assert cli.main(["check"]) == 0
    assert cli.main(["check", "featurescripts/core"]) == 0
    assert cli.main(["format", "--check"]) == 0
    capsys.readouterr()
    assert cli.main(["check", "featurescripts/core/example.local.fs"]) == 1
    assert "triple isn't defined" in capsys.readouterr().out


def test_format(repo, capsys):
    utils = repo / "featurescripts" / "core" / "utils.fs"
    formatted = utils.read_text()
    messy = formatted.replace("(x is number)", "( x is number )") + "\n\n\n"
    utils.write_text(messy)
    generated = repo / "featurescripts" / "tables.gen.fs"
    generated.write_text(messy)

    assert cli.main(["format", "--check"]) == 1
    assert capsys.readouterr().out == "featurescripts/core/utils.fs\n1 file would be formatted.\n"
    assert utils.read_text() == messy

    assert cli.main(["format"]) == 0
    assert capsys.readouterr().out == "featurescripts/core/utils.fs\nFormatted 1 file.\n"
    assert utils.read_text() == formatted
    # Generated files are formatted by what generates them
    assert generated.read_text() == messy

    assert cli.main(["format", "--check"]) == 0
    assert "Everything is formatted." in capsys.readouterr().out


def test_deps(repo, capsys):
    assert cli.main(["deps", "utils"]) == 0
    assert capsys.readouterr().out == (
        "core/utils.fs\n  imports:\n  imported by:\n    feature.fs\n"
    )
    assert cli.main(["deps", "featurescripts/feature.fs"]) == 0
    assert "  imports:\n    core/utils.fs\n" in capsys.readouterr().out


def test_refs(repo, capsys):
    assert cli.main(["refs", "double"]) == 0
    assert capsys.readouterr().out == (
        "double (function) defined at featurescripts/core/utils.fs:2:17\n"
        "  featurescripts/feature.fs:3:18: export const a = double(1);\n"
    )
    assert cli.main(["refs", "triple"]) == 1


def test_refs_to_std(repo, capsys):
    (repo / "std").mkdir()
    (repo / "std" / "geomOperations.fs").write_text(
        "FeatureScript 1;\nexport const opExtrude = function(context is Context, id is Id, definition is map)\n{\n};\n"
    )
    (repo / "featurescripts" / "core" / "utils.fs").write_text(
        'FeatureScript 1;\nimport(path : "onshape/std/common.fs", version : "1.0");\n'
        "export function f(context is Context, id is Id) { opExtrude(context, id, {}); }\n"
    )
    assert cli.main(["refs", "opExtrude"]) == 0
    assert capsys.readouterr().out == (
        "opExtrude (variable) defined at std/geomOperations.fs:2:14\n"
        "  featurescripts/core/utils.fs:3:51: export function f(context is Context, id is Id) { opExtrude(context, id, {}); }\n"
    )


def test_check_moves_studio_files_out_of_old_state(repo, capsys):
    # Even if fs-studios.json was already made (e.g. on another machine)
    (repo / "fs-studios.json").write_text(json.dumps({"version": 1, "studios": {}}))
    (repo / ".fs-state.json").write_text(
        json.dumps(
            {
                "version": 3,
                "studios": {
                    UTILS_ID: {"file": "core/utils.fs", "hash": "h", "microversion_id": "m"}
                },
            }
        )
    )
    assert cli.main(["check"]) == 0
    assert "No problems found." in capsys.readouterr().out
    assert json.loads((repo / "fs-studios.json").read_text())["studios"] == {
        UTILS_ID: "core/utils.fs"
    }
    assert json.loads((repo / ".fs-state.json").read_text())["studios"] == {
        UTILS_ID: {"hash": "h", "microversion_id": "m"}
    }
