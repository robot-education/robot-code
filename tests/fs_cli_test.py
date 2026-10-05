"""Tests for the fs CLI, run against an in-memory stand-in for Onshape."""

import collections
import copy
import itertools
import pathlib
import subprocess

import pytest

from fs_cli import cli
from fs_cli.remote import RemoteStudio, Version, folder_paths
from fs_cli.versions import ReleasedVersion, VersionType, feature_name_for, next_version
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

    def _id(self, prefix: str) -> str:
        return f"{prefix}{next(self.ids)}"

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
            RemoteStudio(id, s["name"], s["mv"], s["folders"])
            for id, s in self.studios(
                instance.document_id, instance.instance_id
            ).items()
        ]

    def pull(self, instance, element_id):
        self.calls["pull"] += 1
        self.pulls += 1
        return self.studios(instance.document_id, instance.instance_id)[element_id][
            "code"
        ]

    def push(self, instance, element_id, code):
        self.calls["push"] += 1
        self.edit(element_id, code, instance.document_id, instance.instance_id)
        return []

    def create(self, instance, name):
        self.calls["create"] += 1
        element_id = self.add(
            name, "", document=instance.document_id, instance=instance.instance_id
        )
        studio = self.studios(instance.document_id, instance.instance_id)[element_id]
        return RemoteStudio(element_id, name, studio["mv"])

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
    assert onshape.calls == {"list_studios": 1, "pull": 2}

    onshape.calls.clear()
    run(onshape, "status")
    run(onshape, "push")
    assert onshape.calls == {"list_studios": 2}

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
    assert "everything in sync" in capsys.readouterr().out
    local(repo, "renamed.fs").write_text("renamed")
    assert run(onshape, "push") == 0
    assert onshape.names() == ["frame.fs"]
    assert onshape.code("frame.fs") == "renamed"


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


def test_contents_failure_falls_back_to_elements(capsys):
    """If the contents endpoint fails, studios are listed from the elements endpoint without folders."""
    from fs_cli.remote import OnshapeRemote
    from onshape_api.paths.paths import url_to_instance_path

    class StubApi:
        def __init__(self):
            self.paths = []

        def get(self, path, **kwargs):
            self.paths.append(path)
            if path.endswith("/contents"):
                raise ApiError("Invalid JSON input.")
            return [
                {
                    "id": "e1",
                    "name": "a.fs",
                    "elementType": "FEATURESTUDIO",
                    "microversionId": "m1",
                }
            ]

    api = StubApi()
    remote = OnshapeRemote(api)
    instance = url_to_instance_path(BACKEND)
    assert remote.list_studios(instance) == [RemoteStudio("e1", "a.fs", "m1")]
    assert "couldn't read the document's folders" in capsys.readouterr().out
    remote.list_studios(instance)
    # Contents isn't retried once it fails
    assert [path.rsplit("/", 1)[-1] for path in api.paths] == [
        "contents",
        "elements",
        "elements",
    ]


def test_contents_lists_feature_studios_with_folders():
    from fs_cli.remote import OnshapeRemote
    from onshape_api.paths.paths import url_to_instance_path

    class StubApi:
        def get(self, path, **kwargs):
            assert path.endswith("/contents") and not kwargs.get("query")
            return {
                "elements": [
                    {
                        "id": "e1",
                        "name": "a.fs",
                        "elementType": "FEATURESTUDIO",
                        "microversionId": "m1",
                    },
                    {
                        "id": "e2",
                        "name": "Part Studio 1",
                        "elementType": "PARTSTUDIO",
                        "microversionId": "m2",
                    },
                ],
                "folders": {
                    "groups": [{"groupName": "Robot", "groups": [{"elementId": "e1"}]}]
                },
            }

    studios = OnshapeRemote(StubApi()).list_studios(url_to_instance_path(BACKEND))
    assert studios == [RemoteStudio("e1", "a.fs", "m1", ("Robot",))]


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
    assert onshape.calls == {"list_studios": 1}


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
    assert run(onshape, "update-std", "--push") == 0
    assert onshape.code("a.fs") == "FeatureScript 2909;\n"


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
