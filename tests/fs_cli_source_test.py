"""Tests for the commands which read and edit the FeatureScripts' source: fs doc, fs rename, and fs check --fix."""

import json
import pathlib

import pytest

from fs_cli import cli

STD = pathlib.Path(__file__).parents[1] / "std"
FRAME_ID = "a" * 24


@pytest.fixture
def repo(tmp_path, monkeypatch):
    """A repo with a function used in two files, and a released feature; its std is the checked-in one."""
    (tmp_path / "pyproject.toml").write_text(
        f'[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\nstd = "{STD.as_posix()}"\n'
    )
    code = tmp_path / "featurescripts"
    code.mkdir()
    (code / "utils.fs").write_text(
        "FeatureScript 2960;\n/** Doubles `x`. */\nexport function double(x is number) { return x * 2; }\n"
    )
    (code / "frame.fs").write_text(
        'FeatureScript 2960;\nimport(path : "onshape/std/common.fs", version : "2960.0");\n'
        'import(path : "utils.fs", version : "");\n'
        "export const frame = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width", "UIHInt" : ["REMEMBER_PREVIOUS_VALUE"] }\n'
        "        definition.width is boolean;\n"
        "    }\n    {\n        println(double(1) + double(2));\n    });\n"
    )
    (tmp_path / "fs-studios.json").write_text(
        json.dumps({"version": 1, "studios": {FRAME_ID: "frame.fs"}, "released": [FRAME_ID]})
    )
    monkeypatch.chdir(tmp_path)
    return tmp_path


def test_doc_shows_std_and_project_names(repo, capsys):
    assert cli.main(["doc", "qCapEntity"]) == 0
    out = capsys.readouterr().out
    assert "qCapEntity(featureId is Id" in out and "From std's query.fs" in out
    assert cli.main(["doc", "double"]) == 0
    out = capsys.readouterr().out
    assert "featurescripts/utils.fs" in out and "Doubles `x`." in out
    assert cli.main(["doc", "noSuchName"]) == 1


def test_rename_dry_run_and_released_constants(repo, capsys):
    assert cli.main(["rename", "double", "twice", "-n"]) == 0
    assert "Would rename double to twice: 3 uses in 2 files." in capsys.readouterr().out
    assert "function double" in (repo / "featurescripts" / "utils.fs").read_text()
    # Documents store a released feature's constant as its type
    assert cli.main(["rename", "frame", "widget", "-n"]) == 2
    assert "released feature's constant" in capsys.readouterr().err


def test_rename_and_fix(repo, capsys):
    code = repo / "featurescripts"
    assert cli.main(["rename", "double", "twice"]) == 0
    assert "twice(1) + twice(2)" in (code / "frame.fs").read_text()
    assert "function twice" in (code / "utils.fs").read_text()
    capsys.readouterr()
    cli.main(["check", "--fix", "featurescripts/frame.fs"])
    out = capsys.readouterr().out
    assert "Fixed 1 problem" in out, out
    assert '"UIHint" : ["REMEMBER_PREVIOUS_VALUE"]' in (code / "frame.fs").read_text()
