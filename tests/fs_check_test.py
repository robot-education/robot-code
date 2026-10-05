"""Tests for the offline analysis commands: fs check, fs deps, and fs refs."""

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
