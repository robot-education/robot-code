"""Tests for the fs CLI, run against an in-memory stand-in for Onshape."""

import itertools
import pathlib
import subprocess

import pytest

from fs_cli import cli
from fs_cli.remote import RemoteStudio
from fs_cli.workspace import Status, update_std_version

URL = "https://cad.onshape.com/documents/d1/w/w1"


class FakeOnshape:
    """Mimics the parts of Onshape the CLI uses. Every edit bumps a studio's microversion."""

    def __init__(self) -> None:
        self.studios: dict[str, dict] = {}
        self.ids = itertools.count()
        self.pulls = 0

    def add(self, name: str, code: str) -> str:
        element_id = f"e{next(self.ids)}"
        self.studios[element_id] = {
            "name": name,
            "code": code,
            "mv": f"m{next(self.ids)}",
        }
        return element_id

    def edit(self, element_id: str, code: str) -> None:
        self.studios[element_id].update(code=code, mv=f"m{next(self.ids)}")

    def code(self, name: str) -> str:
        return next(s["code"] for s in self.studios.values() if s["name"] == name)

    # Remote protocol

    def list_studios(self, instance):
        return [RemoteStudio(id, s["name"], s["mv"]) for id, s in self.studios.items()]

    def pull(self, instance, element_id):
        self.pulls += 1
        return self.studios[element_id]["code"]

    def push(self, instance, element_id, code):
        self.edit(element_id, code)
        return []

    def create(self, instance, name):
        element_id = self.add(name, "")
        return RemoteStudio(element_id, name, self.studios[element_id]["mv"])

    def latest_std_version(self):
        return "2909"


@pytest.fixture
def repo(tmp_path, monkeypatch) -> pathlib.Path:
    (tmp_path / "featurescripts.toml").write_text(f'[documents.robot]\nurl = "{URL}"\n')
    (tmp_path / "featurescripts" / "robot").mkdir(parents=True)
    monkeypatch.chdir(tmp_path)
    return tmp_path


@pytest.fixture
def onshape() -> FakeOnshape:
    return FakeOnshape()


def run(onshape: FakeOnshape, *args: str) -> int:
    return cli.main(list(args), remote=onshape)


def local(repo: pathlib.Path, name: str) -> pathlib.Path:
    return repo / "featurescripts" / "robot" / name


def git(repo: pathlib.Path, *args: str) -> None:
    subprocess.run(
        ["git", "-c", "user.name=t", "-c", "user.email=t@t", *args],
        cwd=repo,
        check=True,
        capture_output=True,
    )


def test_pull_then_push_round_trip(repo, onshape, capsys):
    onshape.add("frame.fs", "FeatureScript 1;\n")
    assert run(onshape, "pull") == 0
    assert local(repo, "frame.fs").read_text() == "FeatureScript 1;\n"

    local(repo, "frame.fs").write_text("FeatureScript 2;\n")
    assert run(onshape) == 0  # push is the default
    assert onshape.code("frame.fs") == "FeatureScript 2;\n"

    capsys.readouterr()
    assert run(onshape, "push") == 0
    assert "Nothing to push." in capsys.readouterr().out


def test_unchanged_studios_are_not_downloaded(repo, onshape):
    onshape.add("frame.fs", "a")
    run(onshape, "pull")
    pulls = onshape.pulls
    run(onshape, "status")
    run(onshape, "push")
    assert onshape.pulls == pulls


def test_push_creates_new_studios(repo, onshape):
    local(repo, "new.fs").write_text("new")
    assert run(onshape, "push") == 0
    assert onshape.code("new.fs") == "new"


def test_push_refuses_to_overwrite_onshape_changes(repo, onshape, capsys):
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    onshape.edit(element_id, "edited in onshape")

    assert run(onshape, "push") == 1
    assert "changed in Onshape" in capsys.readouterr().out
    assert onshape.code("frame.fs") == "edited in onshape"

    assert run(onshape, "pull") == 0
    assert local(repo, "frame.fs").read_text() == "edited in onshape"


def test_conflicts_require_force(repo, onshape):
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    onshape.edit(element_id, "remote")
    local(repo, "frame.fs").write_text("local")

    assert run(onshape, "push") == 1
    assert run(onshape, "pull") == 1
    assert onshape.code("frame.fs") == "remote"
    assert local(repo, "frame.fs").read_text() == "local"

    assert run(onshape, "push", "--force") == 0
    assert onshape.code("frame.fs") == "local"


def test_pull_keeps_local_changes(repo, onshape):
    onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").write_text("local")
    assert run(onshape, "pull") == 1
    assert local(repo, "frame.fs").read_text() == "local"
    assert run(onshape, "pull", "--force") == 0
    assert local(repo, "frame.fs").read_text() == "a"


def test_sync_pushes_and_pulls(repo, onshape):
    remote_id = onshape.add("remote.fs", "r")
    onshape.add("local.fs", "l")
    run(onshape, "pull")
    onshape.edit(remote_id, "r2")
    local(repo, "local.fs").write_text("l2")

    assert run(onshape, "sync") == 0
    assert local(repo, "remote.fs").read_text() == "r2"
    assert onshape.code("local.fs") == "l2"


def test_fresh_clone_uses_git_history(repo, onshape):
    """Without sync state, Onshape matching a committed version means local edits are safe to push."""
    onshape.add("frame.fs", "v1")
    local(repo, "frame.fs").write_text("v1")
    git(repo, "init", "-q")
    git(repo, "add", ".")
    git(repo, "commit", "-qm", "v1")

    local(repo, "frame.fs").write_text("v2")
    assert run(onshape, "push") == 0
    assert onshape.code("frame.fs") == "v2"


def test_fresh_clone_without_history_is_a_conflict(repo, onshape):
    onshape.add("frame.fs", "remote")
    local(repo, "frame.fs").write_text("local")
    assert run(onshape, "push") == 1
    assert onshape.code("frame.fs") == "remote"


def test_targets_limit_scope(repo, onshape):
    local(repo, "a.fs").write_text("a")
    local(repo, "b.fs").write_text("b")
    assert run(onshape, "push", "featurescripts/robot/a.fs") == 0
    assert [s["name"] for s in onshape.studios.values()] == ["a.fs"]
    assert run(onshape, "push", "robot") == 0
    assert sorted(s["name"] for s in onshape.studios.values()) == ["a.fs", "b.fs"]


def test_unknown_target(repo, onshape, capsys):
    assert run(onshape, "push", "nope") == 2
    assert "not a configured document" in capsys.readouterr().err


def test_status(repo, onshape, capsys):
    onshape.add("remote.fs", "r")
    local(repo, "new.fs").write_text("n")
    run(onshape, "status")
    out = capsys.readouterr().out
    assert "remote.fs" in out and Status.REMOTE_ONLY.value in out
    assert "new.fs" in out and Status.LOCAL_ONLY.value in out


def test_studio_names_without_extension(repo, onshape):
    onshape.add("Robot frame", "x")
    run(onshape, "pull")
    assert local(repo, "Robot frame.fs").read_text() == "x"
    local(repo, "Robot frame.fs").write_text("y")
    run(onshape, "push")
    assert onshape.code("Robot frame") == "y"


def test_update_std_version():
    code = (
        "FeatureScript 1234;\n"
        'import(path : "onshape/std/common.fs", version : "1234.0");\n'
        'import(path : "abc", version : "0123456789abcdef01234567");\n'
    )
    assert update_std_version(code, "2909") == (
        "FeatureScript 2909;\n"
        'import(path : "onshape/std/common.fs", version : "2909.0");\n'
        'import(path : "abc", version : "0123456789abcdef01234567");\n'
    )


def test_update_std_command(repo, onshape):
    local(repo, "a.fs").write_text("FeatureScript 1000;\n")
    assert run(onshape, "update-std", "--push") == 0
    assert onshape.code("a.fs") == "FeatureScript 2909;\n"


@pytest.mark.parametrize(
    "argv, command",
    [
        ([], "push"),
        (["-l"], "push"),
        (["robot"], "push"),
        (["--force"], "push"),
        (["pull"], "pull"),
    ],
)
def test_push_is_the_default_command(argv, command):
    assert cli.parse_args(argv).command == command
