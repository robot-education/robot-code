"""Tests for the commands which read and edit the FeatureScripts' source: fs doc, fs rename, and fs check --fix."""

import json
import shutil

from fs_cli import cli


def test_doc_shows_std_and_project_names(capsys):
    assert cli.main(["doc", "qCapEntity"]) == 0
    out = capsys.readouterr().out
    assert "qCapEntity(featureId is Id" in out and "From std's query.fs" in out
    assert cli.main(["doc", "getRibGroups"]) == 0
    assert "featurescripts/lighten/robotLighten.fs" in capsys.readouterr().out
    assert cli.main(["doc", "noSuchName"]) == 1


def test_rename_dry_run_and_released_constants(capsys):
    assert cli.main(["rename", "getRibGroups", "ribGroupsToBuild", "-n"]) == 0
    assert "Would rename getRibGroups to ribGroupsToBuild: 3 uses in 1 files." in capsys.readouterr().out
    # Documents store a released feature's constant as its type
    assert cli.main(["rename", "robotFrame", "frame", "-n"]) == 2
    assert "released feature's constant" in capsys.readouterr().err


def test_rename_and_fix_in_a_repo(tmp_path, monkeypatch, capsys):
    (tmp_path / "pyproject.toml").write_text('[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n')
    shutil.copytree("std", tmp_path / "std")
    code = tmp_path / "featurescripts"
    code.mkdir()
    (code / "utils.fs").write_text('FeatureScript 2960;\nexport function double(x is number) { return x * 2; }\n')
    (code / "feature.fs").write_text(
        'FeatureScript 2960;\nimport(path : "utils.fs", version : "");\n'
        "export const f = defineFeature(function(context is Context, id is Id, definition is map)\n"
        "    precondition\n    {\n"
        '        annotation { "Name" : "Width", "UIHInt" : ["REMEMBER_PREVIOUS_VALUE"] }\n'
        "        definition.width is boolean;\n"
        "    }\n    {\n        println(double(1));\n    });\n"
    )
    (tmp_path / "fs-studios.json").write_text(json.dumps({"version": 1, "studios": {}}))
    monkeypatch.chdir(tmp_path)
    assert cli.main(["rename", "double", "twice"]) == 0
    assert "twice(1)" in (code / "feature.fs").read_text() and "function twice" in (code / "utils.fs").read_text()
    capsys.readouterr()
    cli.main(["check", "--fix", "featurescripts/feature.fs"])
    out = capsys.readouterr().out
    assert "Fixed 1 problem" in out, out
    assert '"UIHint" : ["REMEMBER_PREVIOUS_VALUE"]' in (code / "feature.fs").read_text()
