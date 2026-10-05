"""Tests for the fs CLI, run against an in-memory stand-in for Onshape."""

import collections
import copy
import itertools
import json
import pathlib
import re
import subprocess

import pytest

from fs_cli import cli
from fs_cli.remote import RemoteImage, RemoteStudio, Version, folder_paths
from fs_cli.versions import ReleasedVersion, VersionType, feature_name_for, next_version
from fs_cli.std import StdMetadata
from fs_cli.workspace import Status, update_std_version
from onshape_api.exceptions import ApiError
from semver import Version as SemVersion

BACKEND = "https://cad.onshape.com/documents/back/w/bw"
FRONTEND = "https://cad.onshape.com/documents/front/w/fw"
BETA = "https://cad.onshape.com/documents/beta/w/betaw"


class FakeOnshape:
    """Mimics the parts of Onshape the CLI uses. Every edit bumps a studio's microversion."""

    def __init__(self) -> None:
        # (document id, instance id) -> element id -> studio
        self.instances: dict[tuple[str, str], dict[str, dict]] = {}
        self.versions_by_document: dict[str, list[Version]] = {}
        self.ids = itertools.count()
        self.pulls = 0
        # Every API call made, by method
        self.calls: collections.Counter[str] = collections.Counter()
        self.folders_error: Exception | None = None
        # (document id, instance id) -> element id -> image tab
        self.image_tabs: dict[tuple[str, str], dict[str, dict]] = {}

    def _id(self, prefix: str) -> str:
        # Like Onshape's: 24 hex digits
        return f"{prefix.encode().hex()}{next(self.ids):022x}"

    def studios(self, document: str = "back", instance: str = "bw") -> dict[str, dict]:
        return self.instances.setdefault((document, instance), {})

    def add(
        self,
        name: str,
        code: str,
        folders: tuple[str, ...] = (),
        features: list[str] | None = None,
        document: str = "back",
        instance: str = "bw",
    ) -> str:
        element_id = self._id("e")
        self.studios(document, instance)[element_id] = {
            "name": name,
            "code": code,
            "mv": self._id("m"),
            "folders": folders,
            "features": features or [],
        }
        return element_id

    def images(self, document: str = "back", instance: str = "bw") -> dict[str, dict]:
        return self.image_tabs.setdefault((document, instance), {})

    def add_image(self, name: str, data: bytes, document="back", instance="bw") -> str:
        element_id = self._id("i")
        self.images(document, instance)[element_id] = {"name": name, "data": data, "mv": self._id("m")}
        return element_id

    def edit(self, element_id: str, code: str, document="back", instance="bw") -> None:
        self.studios(document, instance)[element_id].update(code=code, mv=self._id("m"))

    def code(self, name: str, document="back", instance="bw") -> str:
        studio = next(
            s for s in self.studios(document, instance).values() if s["name"] == name
        )
        return studio["code"]

    def names(self, document="back", instance="bw") -> list[str]:
        return sorted(s["name"] for s in self.studios(document, instance).values())

    # Remote protocol

    def list_studios(self, instance):
        self.calls["list_studios"] += 1
        return [
            RemoteStudio(id, s["name"], s["mv"])
            for id, s in self.studios(
                instance.document_id, instance.instance_id
            ).items()
        ]

    def studio_folders(self, instance):
        self.calls["studio_folders"] += 1
        if self.folders_error:
            raise self.folders_error
        return {
            id: s["folders"]
            for id, s in self.studios(
                instance.document_id, instance.instance_id
            ).items()
        }

    def pull(self, instance, element_id):
        self.calls["pull"] += 1
        self.pulls += 1
        return self.studios(instance.document_id, instance.instance_id)[element_id][
            "code"
        ]

    def push(self, instance, element_id, code):
        self.calls["push"] += 1
        self.edit(element_id, code, instance.document_id, instance.instance_id)
        # Like Onshape, update the version of imports of the pushed studio in the studios importing it
        studios = self.studios(instance.document_id, instance.instance_id)
        version = studios[element_id]["mv"]
        for other_id, other in studios.items():
            if other_id != element_id and f'"{element_id}"' in other["code"]:
                code = re.sub(rf'("{element_id}", version : ")\w+(")', rf"\g<1>{version}\g<2>", other["code"])
                self.edit(other_id, code, instance.document_id, instance.instance_id)
        return []

    def create(self, instance, name):
        self.calls["create"] += 1
        element_id = self.add(
            name, "", document=instance.document_id, instance=instance.instance_id
        )
        studio = self.studios(instance.document_id, instance.instance_id)[element_id]
        return RemoteStudio(element_id, name, studio["mv"])

    def delete(self, instance, element_id):
        self.calls["delete"] += 1
        self.studios(instance.document_id, instance.instance_id).pop(element_id, None)
        self.images(instance.document_id, instance.instance_id).pop(element_id, None)

    def rename(self, instance, element_id, name):
        self.calls["rename"] += 1
        self.studios(instance.document_id, instance.instance_id)[element_id]["name"] = name

    def feature_names(self, instance, element_id):
        self.calls["feature_names"] += 1
        return self.studios(instance.document_id, instance.instance_id)[element_id][
            "features"
        ]

    def versions(self, instance):
        self.calls["versions"] += 1
        return list(self.versions_by_document.get(instance.document_id, []))

    def create_version(self, instance, name, description):
        self.calls["create_version"] += 1
        version = Version(self._id("v"), name, description)
        self.versions_by_document.setdefault(instance.document_id, []).append(version)
        self.instances[(instance.document_id, version.id)] = copy.deepcopy(
            self.studios(instance.document_id, instance.instance_id)
        )
        return version

    def latest_std_version(self):
        self.calls["latest_std_version"] += 1
        return "2909"

    def list_images(self, instance):
        self.calls["list_images"] += 1
        images = self.images(instance.document_id, instance.instance_id)
        return [RemoteImage(id, image["name"], image["mv"]) for id, image in images.items()]

    def download_image(self, instance, element_id):
        self.calls["download_image"] += 1
        return self.images(instance.document_id, instance.instance_id)[element_id]["data"]

    def upload_image(self, instance, name, data):
        self.calls["upload_image"] += 1
        element_id = self.add_image(name, data, instance.document_id, instance.instance_id)
        return RemoteImage(element_id, name, self.images(instance.document_id, instance.instance_id)[element_id]["mv"])

    def update_image(self, instance, element_id, name, data):
        self.calls["update_image"] += 1
        image = self.images(instance.document_id, instance.instance_id)[element_id]
        image.update(data=data, mv=self._id("m"))
        return RemoteImage(element_id, name, image["mv"])


@pytest.fixture
def repo(tmp_path, monkeypatch) -> pathlib.Path:
    (tmp_path / "pyproject.toml").write_text(
        f'[tool.fs]\nbackend = "{BACKEND}"\nfrontend = "{FRONTEND}"\nfrontend_beta = "{BETA}"\n'
    )
    (tmp_path / "featurescripts").mkdir()
    monkeypatch.chdir(tmp_path)
    return tmp_path


@pytest.fixture
def onshape() -> FakeOnshape:
    return FakeOnshape()


def run(onshape: FakeOnshape, *args: str) -> int:
    return cli.main(list(args), remote=onshape)


def local(repo: pathlib.Path, path: str) -> pathlib.Path:
    return repo / "featurescripts" / path


def write(repo: pathlib.Path, path: str, code: str) -> None:
    local(repo, path).parent.mkdir(parents=True, exist_ok=True)
    local(repo, path).write_text(code)


def git(repo: pathlib.Path, *args: str) -> None:
    subprocess.run(
        ["git", "-c", "user.name=t", "-c", "user.email=t@t", *args],
        cwd=repo,
        check=True,
        capture_output=True,
    )


# Syncing


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
    write(repo, "new.fs", "new")
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


def test_dry_run_changes_nothing(repo, onshape):
    onshape.add("remote.fs", "r")
    write(repo, "local.fs", "l")
    assert run(onshape, "sync", "--dry-run") == 0
    assert onshape.names() == ["remote.fs"]
    assert not local(repo, "remote.fs").exists()


def test_fresh_clone_uses_git_history(repo, onshape):
    """Without sync state, Onshape matching a committed version means local edits are safe to push."""
    onshape.add("frame.fs", "v1")
    write(repo, "frame.fs", "v1")
    git(repo, "init", "-q")
    git(repo, "add", ".")
    git(repo, "commit", "-qm", "v1")

    local(repo, "frame.fs").write_text("v2")
    assert run(onshape, "push") == 0
    assert onshape.code("frame.fs") == "v2"


def test_studio_files_are_checked_in(repo, onshape, capsys):
    """Which file each studio is synced with is kept apart from the local sync state."""
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    assert json.loads((repo / "fs-studios.json").read_text()) == {
        "version": 1,
        "studios": {element_id: "frame.fs"},
        "released": [],
    }
    state = json.loads((repo / ".fs-state.json").read_text())
    assert "file" not in state["studios"][element_id]

    # Moving the file locally updates it
    write(repo, "core/frame.fs", "a")
    local(repo, "frame.fs").unlink()
    run(onshape, "status")
    assert json.loads((repo / "fs-studios.json").read_text())["studios"] == {element_id: "core/frame.fs"}

    # A fresh clone keeps the studio paired with its file, though another file has the same name
    (repo / ".fs-state.json").unlink()
    write(repo, "other/frame.fs", "b")
    capsys.readouterr()
    run(onshape, "status", "--all")
    out = capsys.readouterr().out
    assert re.search(r"core/frame\.fs\s+in sync", out)
    assert re.search(r"other/frame\.fs\s+new locally", out)


def test_old_state_files_move_to_studios_file(repo, onshape):
    element_id = onshape.add("frame.fs", "a")
    write(repo, "core/frame.fs", "a")
    (repo / ".fs-state.json").write_text(
        json.dumps(
            {
                "version": 3,
                "studios": {element_id: {"file": "core/frame.fs", "hash": "", "microversion_id": ""}},
            }
        )
    )
    run(onshape, "status")
    assert json.loads((repo / "fs-studios.json").read_text())["studios"] == {element_id: "core/frame.fs"}
    assert json.loads((repo / ".fs-state.json").read_text())["version"] == 4


def test_fresh_clone_without_history_is_a_conflict(repo, onshape):
    onshape.add("frame.fs", "remote")
    write(repo, "frame.fs", "local")
    assert run(onshape, "push") == 1
    assert onshape.code("frame.fs") == "remote"


def test_deleted_in_onshape_is_recreated_by_push(repo, onshape, capsys):
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    del onshape.studios()[element_id]
    capsys.readouterr()
    run(onshape, "status")
    assert Status.DELETED_IN_ONSHAPE.value in capsys.readouterr().out
    assert run(onshape, "push") == 0
    assert onshape.code("frame.fs") == "a"


def test_push_deletes_tabs_of_deleted_files(repo, onshape, capsys):
    onshape.add("frame.fs", "a")
    onshape.add("plate.fs", "b")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    capsys.readouterr()
    run(onshape, "status")
    assert Status.DELETED_LOCALLY.value in capsys.readouterr().out

    assert run(onshape, "push", "--dry-run") == 0
    assert "Would delete the frame.fs tab" in capsys.readouterr().out
    assert onshape.names() == ["frame.fs", "plate.fs"]

    assert run(onshape, "push", "--yes") == 0
    assert onshape.names() == ["plate.fs"]
    capsys.readouterr()
    assert run(onshape, "status") == 0
    assert "everything in sync" in capsys.readouterr().out


def test_deleting_tabs_requires_confirmation(repo, onshape, monkeypatch):
    onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    write(repo, "plate.fs", "b")
    monkeypatch.setattr("sys.stdin.isatty", lambda: False)
    assert run(onshape, "push") == 2
    # Nothing happens until it's confirmed
    assert onshape.names() == ["frame.fs"]
    assert onshape.calls["create"] == 0

    monkeypatch.setattr("sys.stdin.isatty", lambda: True)
    monkeypatch.setattr("builtins.input", lambda prompt: "y")
    assert run(onshape, "push") == 0
    assert onshape.names() == ["plate.fs"]


def test_tabs_still_imported_are_not_deleted(repo, onshape, capsys):
    utils = onshape.add("utils.fs", "a")
    onshape.add("frame.fs", f'import(path : "{utils}", version : "v");\n')
    run(onshape, "pull")
    local(repo, "utils.fs").unlink()
    capsys.readouterr()
    assert run(onshape, "push", "--yes") == 1
    assert "still imported by frame.fs" in capsys.readouterr().out
    assert onshape.names() == ["frame.fs", "utils.fs"]

    # Deleting both at once is fine
    local(repo, "frame.fs").unlink()
    assert run(onshape, "push", "--yes") == 0
    assert onshape.names() == []


def test_deleted_locally_but_changed_in_onshape(repo, onshape, capsys):
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    onshape.edit(element_id, "b")
    capsys.readouterr()
    assert run(onshape, "push", "--yes") == 1
    assert "changed in Onshape" in capsys.readouterr().out
    assert onshape.names() == ["frame.fs"]

    assert run(onshape, "push", "--force", "--yes") == 0
    assert onshape.names() == []


def test_deleted_locally_with_only_a_new_microversion(repo, onshape):
    element_id = onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    onshape.edit(element_id, "a")  # e.g. an imported tab changed
    assert run(onshape, "push", "--yes") == 0
    assert onshape.names() == []


def test_pull_force_restores_deleted_files(repo, onshape):
    onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    assert run(onshape, "pull") == 0
    assert not local(repo, "frame.fs").exists()
    assert run(onshape, "pull", "--force") == 0
    assert local(repo, "frame.fs").read_text() == "a"


def test_sync_deletes_tabs_of_deleted_files(repo, onshape):
    onshape.add("frame.fs", "a")
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    assert run(onshape, "sync", "--yes") == 0
    assert onshape.names() == []


def test_push_resolves_imports_by_path(repo, onshape, capsys):
    utils_id = onshape.add("utils.fs", "u")
    run(onshape, "pull")
    write(repo, "core/shapes.fs", "s")
    write(
        repo,
        "frame.fs",
        'import(path : "utils.fs", version : "");\nimport(path : "core/shapes.fs", version : "");\n',
    )
    assert run(onshape, "push") == 0
    shapes_id = next(id for id, s in onshape.studios().items() if s["name"] == "shapes.fs")
    utils_mv = onshape.studios()[utils_id]["mv"]
    code = onshape.code("frame.fs")
    assert f'import(path : "{utils_id}", version : "{utils_mv}");' in code
    assert f'import(path : "{shapes_id}", version : "' in code
    # The local file is resolved too, so it's in sync
    assert local(repo, "frame.fs").read_text() == code
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out


def test_imports_by_path_must_be_pushable(repo, onshape, capsys):
    write(repo, "shapes.fs", "s")
    write(repo, "frame.fs", 'import(path : "shapes.fs", version : "");\n')
    write(repo, "plate.fs", 'import(path : "missing.fs", version : "");\n')
    assert run(onshape, "push", "frame.fs", "plate.fs") == 2
    err = capsys.readouterr().err
    assert "frame.fs imports shapes.fs, which isn't in Onshape yet; push it too." in err
    assert "plate.fs imports missing.fs, which doesn't exist." in err
    assert onshape.names() == []


def test_import_version_updates_after_a_push_are_not_downloaded(repo, onshape, capsys):
    utils = onshape.add("utils.fs", "u")
    frame = onshape.add("frame.fs", f'import(path : "{utils}", version : "aaa");\n')
    run(onshape, "pull")
    local(repo, "utils.fs").write_text("u2")
    assert run(onshape, "push") == 0
    # Onshape updated frame.fs's import of utils.fs, changing its microversion
    assert '"aaa"' not in onshape.code("frame.fs")

    pulls = onshape.pulls
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out
    assert onshape.pulls == pulls

    # Edits in Onshape (outside a push) are still downloaded and noticed
    onshape.edit(frame, f'import(path : "{utils}", version : "bbb");\nchanged\n')
    capsys.readouterr()
    run(onshape, "status")
    assert Status.REMOTE_CHANGES.value in capsys.readouterr().out


def test_studio_names_without_extension(repo, onshape):
    onshape.add("Robot frame", "x")
    run(onshape, "pull")
    assert local(repo, "Robot frame.fs").read_text() == "x"
    local(repo, "Robot frame.fs").write_text("y")
    run(onshape, "push")
    assert onshape.code("Robot frame") == "y"


# Folders


def test_first_pull_mirrors_folders(repo, onshape):
    onshape.add("frame.fs", "f", folders=("Robot", "Structure"))
    onshape.add("util.fs", "u")
    assert run(onshape, "pull") == 0
    assert local(repo, "Robot/Structure/frame.fs").read_text() == "f"
    assert local(repo, "util.fs").read_text() == "u"

    local(repo, "Robot/Structure/frame.fs").write_text("f2")
    assert run(onshape, "push") == 0
    assert onshape.code("frame.fs") == "f2"


def test_api_calls_are_minimal(repo, onshape):
    onshape.add("a.fs", "a", folders=("Robot",))
    onshape.add("b.fs", "b")
    run(onshape, "pull")
    # Folders are only looked up because these studios are new to the repo
    assert onshape.calls == {"list_studios": 1, "studio_folders": 1, "pull": 2, "list_images": 1}

    onshape.calls.clear()
    run(onshape, "status")
    run(onshape, "push")
    # Pushing only lists images when there are any
    assert onshape.calls == {"list_studios": 2, "list_images": 1}

    onshape.calls.clear()
    local(repo, "Robot/a.fs").write_text("a2")
    run(onshape, "push")
    # List, push, then list again to record the new microversion
    assert onshape.calls == {"list_studios": 2, "push": 1}


def test_onshape_folders_are_ignored_after_the_first_pull(repo, onshape):
    element_id = onshape.add("frame.fs", "f", folders=("Old",))
    run(onshape, "pull")
    onshape.studios()[element_id]["folders"] = ("New",)
    onshape.edit(element_id, "f2")
    assert run(onshape, "pull") == 0
    assert local(repo, "Old/frame.fs").read_text() == "f2"
    assert not local(repo, "New").exists()


def test_local_moves_and_renames_are_followed(repo, onshape, capsys):
    onshape.add("frame.fs", "f", folders=("Robot",))
    run(onshape, "pull")

    local(repo, "Robot/frame.fs").rename(local(repo, "frame.fs"))
    local(repo, "frame.fs").write_text("moved")
    assert run(onshape, "push") == 0
    assert onshape.names() == ["frame.fs"]
    assert onshape.code("frame.fs") == "moved"

    local(repo, "frame.fs").rename(local(repo, "renamed.fs"))
    capsys.readouterr()
    run(onshape, "status")
    assert "renamed.fs  in sync, renamed from frame.fs" in capsys.readouterr().out
    local(repo, "renamed.fs").write_text("renamed")
    assert run(onshape, "push") == 0
    assert onshape.names() == ["frame.fs"]
    assert onshape.code("frame.fs") == "renamed"


FRAME = "".join(f"line {i}\n" for i in range(20))


def test_renamed_and_edited_files_are_followed(repo, onshape, capsys):
    """A file renamed and edited before the next push is paired with its studio by similarity to its
    last committed contents, rather than deleting the tab and creating another."""
    element_id = onshape.add("frame.fs", FRAME)
    run(onshape, "pull")
    git(repo, "init", "-q")
    git(repo, "add", ".")
    git(repo, "commit", "-qm", "frame")

    local(repo, "frame.fs").unlink()
    write(repo, "core/tube.fs", FRAME.replace("line 3", "changed"))
    write(repo, "other.fs", "something else entirely")
    calls = onshape.calls["pull"]
    assert run(onshape, "push", "--yes") == 0
    assert onshape.calls["pull"] == calls  # Compared with git, not Onshape
    assert onshape.code("frame.fs") == FRAME.replace("line 3", "changed")
    assert sorted(onshape.names()) == ["frame.fs", "other.fs"]
    assert json.loads((repo / "fs-studios.json").read_text())["studios"][element_id] == "core/tube.fs"


def test_renames_of_uncommitted_files_compare_with_onshape(repo, onshape):
    element_id = onshape.add("frame.fs", FRAME)
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    write(repo, "tube.fs", FRAME + "added\n")
    assert run(onshape, "push", "--yes") == 0
    assert onshape.names() == ["frame.fs"]
    assert json.loads((repo / "fs-studios.json").read_text())["studios"][element_id] == "tube.fs"


def test_dissimilar_files_arent_renames(repo, onshape, capsys):
    onshape.add("frame.fs", FRAME)
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    write(repo, "tube.fs", "nothing like it")
    capsys.readouterr()
    run(onshape, "status")
    out = capsys.readouterr().out
    assert "frame.fs" in out and Status.DELETED_LOCALLY.value in out
    assert "tube.fs" in out and Status.LOCAL_ONLY.value in out


def test_fresh_clone_matches_files_by_name(repo, onshape):
    """Without sync state, a studio pairs with the one local file of the same name, wherever it is."""
    onshape.add("frame.fs", "f", folders=("Robot",))
    write(repo, "Elsewhere/frame.fs", "f")
    git(repo, "init", "-q")
    assert run(onshape, "status") == 0
    write(repo, "Elsewhere/frame.fs", "f2")
    assert run(onshape, "push") == 0
    assert onshape.names() == ["frame.fs"]
    assert onshape.code("frame.fs") == "f2"


def test_new_files_in_folders_stay_put(repo, onshape, capsys):
    write(repo, "Robot/new.fs", "n")
    assert run(onshape, "push") == 0
    assert onshape.code("new.fs") == "n"
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out


class StubApi:
    """Records requests; answers from a dict of path suffix -> response (or exception)."""

    def __init__(self, responses):
        self.responses = responses
        self.requests = []

    def get(self, path, **kwargs):
        self.requests.append(path.rsplit("/", 1)[-1])
        response = self.responses[path.rsplit("/", 1)[-1]]
        if isinstance(response, Exception):
            raise response
        return response


ELEMENTS = [
    {"id": "e1", "name": "a.fs", "elementType": "FEATURESTUDIO", "microversionId": "m1"}
]


def test_studio_folders_from_contents():
    from fs_cli.remote import OnshapeRemote
    from onshape_api.paths.paths import url_to_instance_path

    contents = {
        "elements": ELEMENTS,
        "folders": {
            "groups": [{"groupName": "Robot", "groups": [{"elementId": "e1"}]}]
        },
    }
    api = StubApi({"contents": contents})
    folders = OnshapeRemote(api).studio_folders(url_to_instance_path(BACKEND))
    assert folders == {"e1": ("Robot",)}


def test_folder_lookup_failure_falls_back_to_top_level(repo, onshape, capsys):
    onshape.add("frame.fs", "f", folders=("Robot",))
    onshape.folders_error = ApiError("Invalid JSON input.")
    assert run(onshape, "pull") == 0
    assert local(repo, "frame.fs").read_text() == "f"
    assert "couldn't read the document's folders" in capsys.readouterr().out


# Same-document imports

A_ID = "a" * 24
OLD = "0" * 24
NEW = "1" * 24


def importing(version: str, body: str = "") -> str:
    return (
        f'FeatureScript 2909;\nimport(path : "{A_ID}", version : "{version}");\n{body}'
    )


def test_import_version_changes_are_not_conflicts(repo, onshape, capsys):
    """Onshape may bump the import versions in tabs which import a changed tab."""
    element_id = onshape.add("b.fs", importing(OLD))
    run(onshape, "pull")
    onshape.edit(element_id, importing(NEW))

    capsys.readouterr()
    run(onshape, "status")
    out = capsys.readouterr().out
    assert "import versions updated in Onshape" in out
    assert local(repo, "b.fs").read_text() == importing(OLD)

    assert run(onshape, "push") == 0
    assert "Updated import versions in b.fs" in capsys.readouterr().out
    assert local(repo, "b.fs").read_text() == importing(NEW)
    assert onshape.code("b.fs") == importing(NEW)

    onshape.calls.clear()
    run(onshape, "status")
    assert onshape.calls == {"list_studios": 1, "list_images": 1}


def test_pushing_keeps_onshapes_import_versions(repo, onshape):
    element_id = onshape.add("b.fs", importing(OLD))
    run(onshape, "pull")
    onshape.edit(element_id, importing(NEW))
    local(repo, "b.fs").write_text(importing(OLD, "// local edit\n"))

    assert run(onshape, "push") == 0
    assert onshape.code("b.fs") == importing(NEW, "// local edit\n")
    assert local(repo, "b.fs").read_text() == importing(NEW, "// local edit\n")


def test_folder_paths_from_contents():
    tree = {
        "groupName": "",
        "groups": [
            {"btType": "BTDocumentElementReference-2484", "elementId": "a"},
            {
                "btType": "BTElementGroup-1458",
                "groupName": "Robot",
                "groups": [
                    {"elementId": "b"},
                    {"groupName": "Inner", "groups": [{"elementId": "c"}]},
                ],
            },
        ],
    }
    assert folder_paths(tree) == {"a": (), "b": ("Robot",), "c": ("Robot", "Inner")}
    assert folder_paths(None) == {}


# Targets


def test_targets_limit_scope(repo, onshape):
    write(repo, "a.fs", "a")
    write(repo, "Robot/b.fs", "b")
    write(repo, "Robot/c.fs", "c")
    assert run(onshape, "push", "featurescripts/a.fs") == 0
    assert onshape.names() == ["a.fs"]
    assert run(onshape, "push", "featurescripts/Robot") == 0
    assert onshape.names() == ["a.fs", "b.fs", "c.fs"]


def test_targets_by_studio_name(repo, onshape):
    write(repo, "Robot/frame.fs", "f")
    write(repo, "other.fs", "o")
    assert run(onshape, "push", "frame") == 0
    assert onshape.names() == ["frame.fs"]


def test_unknown_target(repo, onshape, capsys):
    assert run(onshape, "push", "nope") == 2
    assert 'No FeatureScript matches "nope"' in capsys.readouterr().err


def test_status(repo, onshape, capsys):
    onshape.add("remote.fs", "r")
    write(repo, "new.fs", "n")
    run(onshape, "status")
    out = capsys.readouterr().out
    assert "remote.fs" in out and Status.REMOTE_ONLY.value in out
    assert "new.fs" in out and Status.LOCAL_ONLY.value in out


@pytest.mark.parametrize(
    "argv, command",
    [
        ([], "push"),
        (["-l"], "push"),
        (["frame.fs"], "push"),
        (["--force"], "push"),
        (["pull"], "pull"),
        (["release", "frame", "--minor"], "release"),
    ],
)
def test_push_is_the_default_command(argv, command):
    assert cli.parse_args(argv).command == command


def test_missing_config(tmp_path, monkeypatch, onshape, capsys):
    monkeypatch.chdir(tmp_path)
    assert run(onshape, "status") == 2
    assert "[tool.fs]" in capsys.readouterr().err


# Updating the std


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
    write(repo, "a.fs", "FeatureScript 1000;\n")
    assert run(onshape, "update-std", "--latest", "--push") == 0
    assert onshape.code("a.fs") == "FeatureScript 2909;\n"


def test_update_std_uses_the_checked_in_std_version(repo, onshape, capsys):
    write(repo, "a.fs", "FeatureScript 1000;\n")
    assert run(onshape, "update-std") == 2
    assert "fs pull-std" in capsys.readouterr().err
    StdMetadata(version="2960.0").save(repo / "std")
    assert run(onshape, "update-std") == 0
    assert local(repo, "a.fs").read_text() == "FeatureScript 2960;\n"
    assert "latest_std_version" not in onshape.calls


# Pulling the std


def test_pull_std_from_onshape_only_downloads_changed_files(repo, onshape, monkeypatch):
    from onshape_api.model.constants import STD_PATH

    std, workspace = STD_PATH.document_id, STD_PATH.instance_id
    geometry = onshape.add(
        "geometry.fs", "geometry v1", document=std, instance=workspace
    )
    onshape.add("math.fs", "math v1", document=std, instance=workspace)
    onshape.versions_by_document[std] = []
    onshape.create_version(STD_PATH, "2960.0", "")
    regenerated = []
    monkeypatch.setattr(
        "fs_lsp.tools.update_stdlib.regenerate",
        lambda path: regenerated.append(path) or "ok",
    )

    onshape.calls.clear()
    assert run(onshape, "pull-std", "--from-onshape", "-y") == 0
    assert (repo / "std" / "geometry.fs").read_text() == "geometry v1"
    assert StdMetadata.load(repo / "std").version == "2960.0"
    assert onshape.calls["pull"] == 2
    assert regenerated == [repo / "std"]

    # A new version where only geometry.fs changed
    onshape.edit(geometry, "geometry v2", document=std, instance=workspace)
    onshape.create_version(STD_PATH, "2970.0", "")
    onshape.calls.clear()
    assert run(onshape, "pull-std", "--from-onshape", "-y") == 0
    assert (repo / "std" / "geometry.fs").read_text() == "geometry v2"
    assert onshape.calls == {"versions": 1, "list_studios": 1, "pull": 1}

    onshape.calls.clear()
    assert run(onshape, "pull-std", "--from-onshape", "-y") == 0
    assert "pull" not in onshape.calls


# Releasing


def test_feature_names():
    assert feature_name_for("robotFrame.fs") == "Robot frame"
    assert feature_name_for("robotFrameBeta") == "Robot frame beta"


def test_next_version():
    v = SemVersion.parse
    previous = ReleasedVersion("Robot frame", v("1.2.3"))
    assert next_version(None, VersionType.MINOR, False) == v("0.1.0")
    assert next_version(previous, VersionType.PATCH, False) == v("1.2.4")
    assert next_version(previous, VersionType.MAJOR, True) == v("2.0.0-beta.1")
    beta = ReleasedVersion("Robot frame", v("2.0.0-beta.1"))
    assert next_version(beta, None, True) == v("2.0.0-beta.2")
    assert next_version(beta, VersionType.MAJOR, False) == v("2.0.0")
    with pytest.raises(ValueError):
        next_version(previous, None, False)
    with pytest.raises(ValueError):
        next_version(previous, None, True)
    with pytest.raises(ValueError):
        next_version(beta, VersionType.MINOR, True)


def released_frame(repo, onshape) -> str:
    element_id = onshape.add(
        "robotFrame.fs", "frame", folders=("Robot",), features=["robotFrame"]
    )
    run(onshape, "pull")
    return element_id


def test_release(repo, onshape):
    element_id = released_frame(repo, onshape)
    assert run(onshape, "release", "robotFrame", "--minor", "-y", "-d", "First") == 0

    [version] = onshape.versions_by_document["back"]
    assert version.name == "Robot frame - v0.1.0"
    released = onshape.studios("back", version.id)[element_id]
    code = onshape.code("robotFrame.fs", "front", "fw")
    assert code.startswith("FeatureScript 2909;\n")
    assert " * Robot frame - v0.1.0\n" in code
    assert (
        f'export import(path : "back/{version.id}/{element_id}", version : "{released["mv"]}");'
        in code
    )
    assert "front" not in onshape.versions_by_document

    assert (
        run(
            onshape,
            "release",
            "featurescripts/Robot/robotFrame.fs",
            "--patch",
            "-y",
            "--publish",
        )
        == 0
    )
    assert [v.name for v in onshape.versions_by_document["front"]] == [
        "Robot frame - v0.1.1"
    ]
    assert onshape.names("front", "fw") == ["robotFrame.fs"]


def test_release_dry_run(repo, onshape, capsys):
    released_frame(repo, onshape)
    assert run(onshape, "release", "robotFrame", "--major", "--dry-run") == 0
    assert "Robot frame - v1.0.0" in capsys.readouterr().out
    assert "back" not in onshape.versions_by_document


def test_release_requires_pushed_code(repo, onshape, capsys):
    released_frame(repo, onshape)
    local(repo, "Robot/robotFrame.fs").write_text("unpushed")
    assert run(onshape, "release", "robotFrame", "--minor", "-y") == 2
    assert "run `fs push`" in capsys.readouterr().err


def test_beta_release(repo, onshape, capsys):
    onshape.add("robotFrameBeta.fs", "b", features=["robotFrameBeta"])
    run(onshape, "pull")
    assert run(onshape, "release", "robotFrameBeta", "--minor", "-y") == 2
    assert "--beta" in capsys.readouterr().err
    assert run(onshape, "release", "robotFrameBeta", "--minor", "--beta", "-y") == 0
    assert onshape.names("beta", "betaw") == ["robotFrameBeta.fs"]
    assert (
        onshape.versions_by_document["back"][0].name
        == "Robot frame beta - v0.1.0-beta.1"
    )


def test_release_requires_confirmation_when_not_interactive(repo, onshape, capsys):
    released_frame(repo, onshape)
    assert run(onshape, "release", "robotFrame", "--minor") == 2
    assert "--yes" in capsys.readouterr().err


def test_sync_versions(repo, onshape):
    onshape.versions_by_document["back"] = [
        Version("1", "Robot frame - v1.0.0"),
        Version("2", "Unrelated version"),
        Version("3", "Robot frame - v1.1.0", "Things"),
        Version("4", "Robot bore - v0.1.0"),
    ]
    onshape.versions_by_document["front"] = [Version("9", "Robot frame - v1.0.0")]
    assert run(onshape, "sync-versions", "-y") == 0
    assert [v.name for v in onshape.versions_by_document["front"]] == [
        "Robot frame - v1.0.0",
        "Robot frame - v1.1.0",
        "Robot bore - v0.1.0",
    ]
    assert onshape.versions_by_document["front"][1].description == "Things"


# Renaming with fs mv


def studio_files(repo: pathlib.Path) -> dict[str, str]:
    return json.loads((repo / "fs-studios.json").read_text())["studios"]


def test_mv_follows_files_and_imports_by_path(repo, onshape):
    utils = onshape.add("utils.fs", "u")
    onshape.add("feature.fs", 'import(path : "core/old.fs", version : "");')
    run(onshape, "pull")
    write(repo, "uses.fs", 'import(path : "utils.fs", version : "");\nimport(path : "onshape/std/common.fs", version : "1");')

    assert cli.main(["mv", "featurescripts/utils.fs", "featurescripts/core/new.fs"]) == 0
    assert local(repo, "core/new.fs").read_text() == "u"
    assert not local(repo, "utils.fs").exists()
    assert studio_files(repo)[utils] == "core/new.fs"
    assert local(repo, "uses.fs").read_text().startswith('import(path : "core/new.fs", version : "");')
    assert "onshape/std/common.fs" in local(repo, "uses.fs").read_text()

    # The tab is kept
    assert run(onshape, "push", "--yes") == 0
    assert sorted(onshape.names()) == ["feature.fs", "uses.fs", "utils.fs"]


def test_mv_moves_folders(repo, onshape):
    utils = onshape.add("utils.fs", "u", folders=("Core",))
    run(onshape, "pull")
    assert cli.main(["mv", "featurescripts/Core", "featurescripts/Shared"]) == 0
    assert studio_files(repo)[utils] == "Shared/utils.fs"
    assert local(repo, "Shared/utils.fs").read_text() == "u"


def test_mv_records_moves_already_made(repo, onshape):
    utils = onshape.add("utils.fs", "u")
    run(onshape, "pull")
    local(repo, "utils.fs").rename(local(repo, "moved.fs"))
    assert cli.main(["mv", "featurescripts/utils.fs", "featurescripts/moved.fs"]) == 0
    assert studio_files(repo)[utils] == "moved.fs"


def test_push_suggests_mv_for_renames_it_cant_tell(repo, onshape, capsys):
    onshape.add("frame.fs", FRAME)
    run(onshape, "pull")
    local(repo, "frame.fs").unlink()
    write(repo, "tube.fs", "nothing like it")
    capsys.readouterr()
    run(onshape, "push", "--dry-run")
    out = capsys.readouterr().out
    assert "Would delete the frame.fs tab" in out
    assert "renamed to tube.fs rather than deleted, run `fs mv OLD NEW` first" in out


# Released FeatureScripts


def released_ids(repo: pathlib.Path) -> list[str]:
    return json.loads((repo / "fs-studios.json").read_text())["released"]


def test_release_marks_released(repo, onshape):
    element_id = released_frame(repo, onshape)
    assert released_ids(repo) == []
    assert run(onshape, "release", "robotFrame", "--minor", "-y") == 0
    assert released_ids(repo) == [element_id]


def test_push_keeps_released_tabs_of_deleted_files(repo, onshape, capsys):
    element_id = released_frame(repo, onshape)
    assert cli.main(["released", "robotFrame"]) == 0
    local(repo, "Robot/robotFrame.fs").unlink()
    capsys.readouterr()
    assert run(onshape, "push", "--yes") == 1
    assert "it's released" in capsys.readouterr().out
    assert element_id in onshape.studios()
    run(onshape, "status")
    assert "released" in capsys.readouterr().out


def test_push_doesnt_recreate_released_tabs(repo, onshape, capsys):
    element_id = released_frame(repo, onshape)
    assert cli.main(["released", "featurescripts/Robot/robotFrame.fs"]) == 0
    onshape.delete(onshape_backend(), element_id)
    capsys.readouterr()
    assert run(onshape, "push") == 1
    assert "restore the tab from the document's history" in capsys.readouterr().out.lower()
    assert onshape.names() == []
    # Still recorded, so the next push doesn't recreate it either
    assert studio_files(repo)[element_id] == "Robot/robotFrame.fs"
    assert run(onshape, "push") == 1

    assert cli.main(["released", "robotFrame", "--remove"]) == 0
    assert run(onshape, "push") == 0
    assert onshape.names() == ["robotFrame.fs"]


def onshape_backend():
    from onshape_api.paths.paths import url_to_instance_path

    return url_to_instance_path(BACKEND)


def test_released_lists_marks_and_detects(repo, onshape, capsys):
    frame = released_frame(repo, onshape)
    bore = onshape.add("robotBore.fs", "bore")
    old = onshape.add("robotOld.fs", "old")
    onshape.add("utils.fs", "utils")
    run(onshape, "pull")
    capsys.readouterr()
    assert cli.main(["released"]) == 0
    assert "Nothing is released" in capsys.readouterr().out

    onshape.versions_by_document["back"] = [
        Version("1", "Robot frame - v1.0.0"),
        Version("2", "Robot bore - v0.1.0-beta.1"),
        Version("3", "Robot old - v1.0.0"),
        Version("4", "DEPRECATED Robot old"),
    ]
    calls = sum(onshape.calls.values())
    assert run(onshape, "released", "--detect") == 0
    assert sum(onshape.calls.values()) - calls == 2
    assert sorted(released_ids(repo)) == sorted([frame, bore])
    assert old not in released_ids(repo)

    assert cli.main(["released", "robotBore", "--remove"]) == 0
    assert released_ids(repo) == [frame]
    capsys.readouterr()
    assert cli.main(["released"]) == 0
    assert capsys.readouterr().out == "Robot/robotFrame.fs\n"
    assert cli.main(["released", "nothing"]) == 2


def test_mv_keeps_released(repo, onshape):
    element_id = released_frame(repo, onshape)
    cli.main(["released", "robotFrame"])
    assert cli.main(["mv", "featurescripts/Robot/robotFrame.fs", "featurescripts/frame.fs"]) == 0
    assert released_ids(repo) == [element_id]
    assert studio_files(repo)[element_id] == "frame.fs"


# Matching tabs with files by hand


def test_tabs_lists_what_isnt_paired(repo, onshape, capsys):
    onshape.add("frame.fs", "frame")
    onshape.add("gone.fs", "gone")
    released = onshape.add("robotFrame.fs", "rf")
    run(onshape, "pull")
    cli.main(["released", "robotFrame"])
    onshape.add("remoteOnly.fs", "r")
    cli.main(["mv", "featurescripts/frame.fs", "featurescripts/tube.fs"])
    cli.main(["mv", "featurescripts/robotFrame.fs", "featurescripts/robotTube.fs"])
    local(repo, "gone.fs").unlink()
    write(repo, "new.fs", "new")
    capsys.readouterr()
    listings = onshape.calls["list_studios"]
    assert run(onshape, "tabs") == 0
    assert onshape.calls["list_studios"] - listings == 1
    out = capsys.readouterr().out
    assert "Tabs only in Onshape" in out and "remoteOnly.fs  (" in out
    assert "Tabs whose files were deleted" in out and "was gone.fs" in out
    assert "Files not in Onshape" in out and "  new.fs" in out
    assert "  tube.fs  (tab frame.fs)" in out
    assert "  robotTube.fs  (tab robotFrame.fs)  [released]" in out

    assert run(onshape, "tabs", "--rename", "--yes") == 0
    out = capsys.readouterr().out
    assert "Not renaming robotFrame.fs: it's released" in out
    assert sorted(onshape.names()) == ["gone.fs", "remoteOnly.fs", "robotFrame.fs", "tube.fs"]
    assert onshape.studios()[released]["name"] == "robotFrame.fs"
    assert run(onshape, "tabs") == 0
    assert "tube.fs" not in capsys.readouterr().out


def test_tabs_rename_needs_confirmation(repo, onshape, capsys):
    onshape.add("frame.fs", "frame")
    run(onshape, "pull")
    cli.main(["mv", "featurescripts/frame.fs", "featurescripts/tube.fs"])
    assert run(onshape, "tabs", "--rename", "--dry-run") == 0
    assert "Would rename the frame.fs tab to tube.fs" in capsys.readouterr().out
    assert run(onshape, "tabs", "--rename") == 2
    assert onshape.names() == ["frame.fs"]


def test_link_pairs_a_file_with_a_tab(repo, onshape, capsys):
    tab = onshape.add("frame.fs", "same")
    write(repo, "tube.fs", "same")
    run(onshape, "status")
    assert "only in Onshape" in capsys.readouterr().out

    assert run(onshape, "link", "featurescripts/tube.fs", "frame.fs") == 0
    assert studio_files(repo)[tab] == "tube.fs"
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out
    # Pushing doesn't create a tab for the file
    assert run(onshape, "push") == 0
    assert onshape.names() == ["frame.fs"]

    # Relinking the file moves it to the other tab
    other = onshape.add("other.fs", "o")
    assert run(onshape, "link", "featurescripts/tube.fs", other) == 0
    assert studio_files(repo) == {other: "tube.fs"}
    assert "no longer synced with the frame.fs tab" in capsys.readouterr().out

    assert run(onshape, "link", "featurescripts/tube.fs", "missing") == 2
    assert run(onshape, "link", "elsewhere/tube.fs", "frame.fs") == 2


def test_unlink(repo, onshape, capsys):
    tab = onshape.add("frame.fs", "f")
    run(onshape, "pull")
    assert cli.main(["unlink", "frame"]) == 0
    assert tab not in studio_files(repo)
    assert cli.main(["unlink", "frame"]) == 2


# Deprecating

FEATURE = 'annotation { "Feature Type Name" : "Robot frame" }\nexport const robotFrame = defineFeature();\n'


def deprecatable_frame(repo, onshape) -> str:
    element_id = onshape.add("robotFrame.fs", FEATURE, features=["Robot frame"])
    run(onshape, "pull")
    assert run(onshape, "release", "robotFrame", "--minor", "-y", "--publish") == 0
    return element_id


def test_deprecate(repo, onshape, capsys):
    element_id = deprecatable_frame(repo, onshape)
    frontend_id = next(iter(onshape.studios("front", "fw")))
    assert run(onshape, "deprecate", "robotFrame", "-y", "--publish", "-d", "Use robot tube") == 0

    version = onshape.versions_by_document["back"][-1]
    assert version.name == "DEPRECATED Robot frame"
    deprecated = onshape.studios("back", version.id)[element_id]
    assert '"Feature Type Name" : "DEPRECATED Robot frame"' in deprecated["code"]
    # The frontend studio is kept (with its id), renamed, and imports the deprecated version
    frontend = onshape.studios("front", "fw")
    assert list(frontend) == [frontend_id]
    assert frontend[frontend_id]["name"] == "DEPRECATED robotFrame.fs"
    assert f'"back/{version.id}/{element_id}", version : "{deprecated["mv"]}"' in frontend[frontend_id]["code"]
    assert [v.name for v in onshape.versions_by_document["front"]] == ["Robot frame - v0.1.0", "DEPRECATED Robot frame"]
    # Then it's gone from the backend
    assert onshape.names() == []
    assert not local(repo, "robotFrame.fs").exists()
    assert released_ids(repo) == []
    assert studio_files(repo) == {}


def test_deprecate_keep_backend(repo, onshape):
    element_id = deprecatable_frame(repo, onshape)
    assert run(onshape, "deprecate", "robotFrame", "-y", "--keep-backend") == 0
    assert onshape.names() == ["DEPRECATED robotFrame.fs"]
    assert "DEPRECATED Robot frame" in local(repo, "robotFrame.fs").read_text()
    assert released_ids(repo) == []
    assert onshape.names("front", "fw") == ["DEPRECATED robotFrame.fs"]
    assert [v.name for v in onshape.versions_by_document["front"]] == ["Robot frame - v0.1.0"]
    assert studio_files(repo)[element_id] == "robotFrame.fs"


def test_deprecate_checks_first(repo, onshape, capsys):
    onshape.add("robotFrame.fs", FEATURE, features=["Robot frame"])
    run(onshape, "pull")
    assert run(onshape, "deprecate", "robotFrame", "-y") == 2
    assert "isn't released" in capsys.readouterr().err

    assert run(onshape, "release", "robotFrame", "--minor", "-y") == 0
    write(repo, "uses.fs", 'import(path : "' + next(iter(onshape.studios())) + '", version : "");')
    run(onshape, "push")
    assert run(onshape, "deprecate", "robotFrame", "-y") == 2
    assert "is imported by uses.fs" in capsys.readouterr().err

    versions = len(onshape.versions_by_document["back"])
    assert run(onshape, "deprecate", "robotFrame", "--dry-run", "--keep-backend") == 0
    out = capsys.readouterr().out
    assert "Rename it DEPRECATED robotFrame.fs" in out
    assert "Rename its tab in the backend document DEPRECATED robotFrame.fs" in out
    assert "By hand: rename any folders" in out
    assert "Delete its tab" not in out
    assert len(onshape.versions_by_document["back"]) == versions


def test_rename_element_sets_the_name_property():
    from onshape_api.endpoints.metadata import rename_element
    from onshape_api.paths.paths import ElementPath

    class FakeApi:
        def __init__(self):
            self.requests = []

        def get(self, path, **kwargs):
            self.requests.append(("GET", path, None))
            return {
                "jsonType": "metadata-element",
                "properties": [
                    {"name": "Description", "propertyId": "d1", "value": "", "editable": True},
                    {"name": "Name", "propertyId": "n1", "value": "old.fs", "editable": True},
                ],
            }

        def post(self, path, body="", **kwargs):
            self.requests.append(("POST", path, body))

    api = FakeApi()
    rename_element(api, ElementPath.from_path(onshape_backend(), "e1"), "new.fs")
    assert api.requests == [
        ("GET", "/metadata/d/back/w/bw/e/e1", None),
        ("POST", "/metadata/d/back/w/bw/e/e1", {"properties": [{"propertyId": "n1", "value": "new.fs"}]}),
    ]


# COTS research


class FakeFrcDesign:
    """Answers FRCDesign's API from canned responses."""

    def __init__(self, responses: dict[str, object]) -> None:
        self.responses = responses
        self.paths: list[str] = []

    def get(self, url, params=None, timeout=None):
        path = url.removeprefix("https://app.frcdesign.org/api")
        self.paths.append(path)

        class Response:
            status_code = 200 if path in self.responses else 404
            text = ""

            def json(_):
                return self.responses[path]

        return Response()


def test_cots_ranks_parts_and_shows_options():
    from fs_cli.cots import FrcDesign, find

    session = FakeFrcDesign(
        {
            "/library-version/library/frc-design-lib": {"version": 3},
            "/library-data/library/frc-design-lib": {
                "groups": {"g": {"name": "Extrusions & Shafts"}},
                "insertables": {
                    "a": {"id": "a", "elementId": "ea", "groupId": "g", "name": "Hex Shaft (WCP)", "vendors": ["WCP"]},
                    "b": {"id": "b", "elementId": "eb", "groupId": "g", "name": "Hex Shaft (REV)", "vendors": ["REV"]},
                    "c": {"id": "c", "elementId": "ec", "groupId": "g", "name": "Box Tube", "vendors": []},
                },
            },
            "/analytics/parts/library/frc-design-lib": [
                {"path": {"elementId": "ea"}, "insertCount": 800},
                {"path": {"elementId": "eb"}, "insertCount": 300},
            ],
            "/analytics/insertable/library/frc-design-lib/element/ea": {
                "parameters": [
                    {"name": "Type", "type": "enum", "path": [], "total": 3,
                     "values": [{"label": "1/2\" Hex", "count": 1}, {"label": "1/2\" Rounded Hex", "count": 2}]},
                    {"name": "Length", "type": "quantity"},
                ]
            },
            "/configuration/insertable/a": {"records": [{"partNumber": "WCP-0914", "name": "1/2\" Rounded Hex", "url": "u"}]},
        }
    )
    library = FrcDesign("frc", session=session)
    parts = find(library.parts(), "hex shaft")
    assert [(part.name, part.uses) for part in parts] == [("Hex Shaft (WCP)", 800), ("Hex Shaft (REV)", 300)]
    assert library.options(parts[0]) == ['Type (3): 1/2" Rounded Hex = 2, 1/2" Hex = 1', "Length (quantity)"]
    assert library.records(parts[0])[0]["partNumber"] == "WCP-0914"


# Images

ICON = b"<svg>robot</svg>"


def image_import(element_id: str, version: str) -> str:
    return f'Icon::import(path : "{element_id}", version : "{version}");\n'


def test_pull_places_images_beside_their_importers(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    version = onshape.images()[icon]["mv"]
    onshape.add("robotFeature.fs", image_import(icon, version), folders=("core",))
    onshape.add_image("unused.svg", b"<svg/>")
    assert run(onshape, "pull") == 0
    assert "Pulled core/robotIcon.svg" in capsys.readouterr().out
    assert local(repo, "core/robotIcon.svg").read_bytes() == ICON
    assert local(repo, "unused.svg").read_bytes() == b"<svg/>"
    assert json.loads((repo / "fs-studios.json").read_text())["images"][icon] == "core/robotIcon.svg"

    onshape.calls.clear()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out
    # Unchanged images aren't downloaded
    assert onshape.calls == {"list_studios": 1, "list_images": 1}


def test_push_uploads_images_imported_by_path(repo, onshape, capsys):
    write(repo, "grid/gridIcon.svg", "<svg>grid</svg>")
    write(repo, "grid/grid.fs", 'Icon::import(path : "grid/gridIcon.svg", version : "");\n')
    assert run(onshape, "push") == 0
    [(icon, image)] = onshape.images().items()
    assert (image["name"], image["data"]) == ("gridIcon.svg", b"<svg>grid</svg>")
    assert onshape.code("grid.fs") == image_import(icon, image["mv"])
    assert local(repo, "grid/grid.fs").read_text() == image_import(icon, image["mv"])
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out


def test_imported_images_must_exist(repo, onshape, capsys):
    write(repo, "grid.fs", 'Icon::import(path : "missing.svg", version : "");\n')
    assert run(onshape, "push") == 2
    assert "grid.fs imports missing.svg, which doesn't exist." in capsys.readouterr().err


def test_pushing_a_changed_image_updates_its_imports(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    old = onshape.images()[icon]["mv"]
    onshape.add("robotFeature.fs", image_import(icon, old))
    run(onshape, "pull")
    local(repo, "robotIcon.svg").write_bytes(b"<svg>new</svg>")
    capsys.readouterr()
    run(onshape, "status")
    assert re.search(rf"robotIcon.svg +{Status.LOCAL_CHANGES.value}", capsys.readouterr().out)

    assert run(onshape, "push") == 0
    new = onshape.images()[icon]["mv"]
    assert new != old and onshape.images()[icon]["data"] == b"<svg>new</svg>"
    # The importer was in sync, but now imports the new version
    assert onshape.code("robotFeature.fs") == image_import(icon, new)
    assert local(repo, "robotFeature.fs").read_text() == image_import(icon, new)
    capsys.readouterr()
    run(onshape, "status")
    assert "everything in sync" in capsys.readouterr().out


def test_images_changed_in_onshape_are_pulled(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    run(onshape, "pull")
    onshape.images()[icon].update(data=b"<svg>edited</svg>", mv="f" * 24)
    capsys.readouterr()
    assert run(onshape, "push") == 1
    assert "Skipping robotIcon.svg: changed in Onshape" in capsys.readouterr().out
    assert run(onshape, "pull") == 0
    assert local(repo, "robotIcon.svg").read_bytes() == b"<svg>edited</svg>"


def test_deleted_images_are_deleted_once_not_imported(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    onshape.add("robotFeature.fs", image_import(icon, onshape.images()[icon]["mv"]))
    run(onshape, "pull")
    local(repo, "robotIcon.svg").unlink()
    capsys.readouterr()
    run(onshape, "push", "-y")
    assert "Skipping deleting robotIcon.svg in Onshape: it's still imported by robotFeature.fs." in capsys.readouterr().out
    assert icon in onshape.images()

    local(repo, "robotFeature.fs").write_text("x")
    assert run(onshape, "push", "-y") == 0
    assert onshape.images() == {}
    assert "images" not in json.loads((repo / "fs-studios.json").read_text())


def test_images_deleted_in_onshape_are_recreated(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    onshape.add("robotFeature.fs", image_import(icon, onshape.images()[icon]["mv"]))
    run(onshape, "pull")
    del onshape.images()[icon]
    capsys.readouterr()
    run(onshape, "status")
    assert re.search(rf"robotIcon.svg +{Status.DELETED_IN_ONSHAPE.value}", capsys.readouterr().out)

    assert run(onshape, "push") == 0
    [(new_icon, image)] = onshape.images().items()
    assert new_icon != icon and image["data"] == ICON
    # Its importer imports the new tab
    assert onshape.code("robotFeature.fs") == image_import(new_icon, image["mv"])
    assert local(repo, "robotFeature.fs").read_text() == image_import(new_icon, image["mv"])


def test_moved_images_are_followed(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    run(onshape, "pull")
    write(repo, "core/robotIcon.svg", "")
    local(repo, "robotIcon.svg").rename(local(repo, "core/robotIcon.svg"))
    capsys.readouterr()
    run(onshape, "status")
    assert "renamed from robotIcon.svg" in capsys.readouterr().out
    assert run(onshape, "push") == 0
    assert json.loads((repo / "fs-studios.json").read_text())["images"] == {icon: "core/robotIcon.svg"}
    assert onshape.calls["update_image"] == 0


def test_images_can_be_targeted(repo, onshape, capsys):
    onshape.add_image("robotIcon.svg", ICON)
    onshape.add("a.fs", "a")
    assert run(onshape, "pull", "robotIcon.svg") == 0
    assert local(repo, "robotIcon.svg").is_file() and not local(repo, "a.fs").exists()
    assert run(onshape, "pull", "nope.svg") == 2
    assert 'No image matches "nope.svg"' in capsys.readouterr().err


def test_mv_follows_images(repo, onshape, capsys):
    icon = onshape.add_image("robotIcon.svg", ICON)
    run(onshape, "pull")
    write(repo, "a.fs", 'Icon::import(path : "robotIcon.svg", version : "");\n')
    assert run(onshape, "mv", "featurescripts/robotIcon.svg", "featurescripts/core/robotIcon.svg") == 0
    assert json.loads((repo / "fs-studios.json").read_text())["images"] == {icon: "core/robotIcon.svg"}
    assert local(repo, "a.fs").read_text() == 'Icon::import(path : "core/robotIcon.svg", version : "");\n'


class RecordingApi:
    """Records each request's method, path, and arguments; answers with `response`."""

    def __init__(self, response):
        self.response = response
        self.requests = []

    def get(self, path, **kwargs):
        self.requests.append(("get", path, kwargs))
        return self.response

    def post(self, path, body="", **kwargs):
        self.requests.append(("post", path, {"body": body, **kwargs}))
        return self.response


def test_onshape_remote_images():
    from fs_cli.remote import OnshapeRemote
    from onshape_api.paths.paths import url_to_instance_path

    instance = url_to_instance_path(BACKEND)
    elements = [
        {"id": "i1", "name": "icon.svg", "elementType": "BLOB", "microversionId": "m1", "dataType": "image/svg+xml"},
        {"id": "i2", "name": "part.step", "elementType": "BLOB", "microversionId": "m2", "dataType": "application/step"},
    ]
    api = RecordingApi(elements)
    assert OnshapeRemote(api).list_images(instance) == [RemoteImage("i1", "icon.svg", "m1", "image/svg+xml")]
    assert api.requests[0][2]["query"]["elementType"] == "BLOB"

    api = RecordingApi({"id": "i3", "name": "new.svg", "microversionId": "m3"})
    assert OnshapeRemote(api).upload_image(instance, "new.svg", b"<svg/>") == RemoteImage("i3", "new.svg", "m3")
    method, path, kwargs = api.requests[0]
    assert (method, path) == ("post", "/blobelements/d/back/w/bw")
    assert kwargs["headers"]["Content-Type"].startswith("multipart/form-data; boundary=")
    assert b'name="file"; filename="new.svg"' in kwargs["body"] and b"<svg/>" in kwargs["body"]

    OnshapeRemote(api).update_image(instance, "i3", "new.svg", b"<svg>2</svg>")
    assert api.requests[1][1] == "/blobelements/d/back/w/bw/e/i3"
    with pytest.raises(ValueError, match="isn't an image"):
        OnshapeRemote(api).upload_image(instance, "part.step", b"")
